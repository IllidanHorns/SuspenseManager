using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces;
using Common.DTOs;
using Data;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Models;
using Models.Enums;

namespace Application.Services;

/// <summary>
/// Сервис валидации строк из отчётов стриминговых платформ.
/// Проверяет наличие продукта в каталоге и прав на него.
/// Все строки сохраняются в таблицу SuspenseLines с соответствующим статусом.
/// </summary>
public class ValidationService : IValidationService
{
    private readonly SuspenseManagerDbContext _db;
    private readonly IAuditService _audit;
    private readonly IValidator<SuspenseLineDto> _lineValidator;

    public ValidationService(
        SuspenseManagerDbContext db,
        IAuditService audit,
        IValidator<SuspenseLineDto> lineValidator)
    {
        _db = db;
        _audit = audit;
        _lineValidator = lineValidator;
    }

    public async Task<ValidationResultDto> ValidateBatchAsync(List<SuspenseLineDto> lines, bool numberRowsAsInExcel = true)
    {
        await EnsureAllLinesPassDtoRulesAsync(lines, excelStyleRowNumbers: numberRowsAsInExcel);

        var result = new ValidationResultDto
        {
            TotalRows = lines.Count
        };

        var createdLines = new List<SuspenseLine>();

        foreach (var line in lines)
        {
            var (lineResult, entity) = await ProcessLineAsync(line);
            result.Lines.Add(lineResult);
            createdLines.Add(entity);

            switch ((BusinessStatus)lineResult.BusinessStatus)
            {
                case BusinessStatus.NoProduct:
                    result.NoProductCount++;
                    break;
                case BusinessStatus.NoRights:
                    result.NoRightsCount++;
                    break;
                case BusinessStatus.Validated:
                    result.ValidatedCount++;
                    break;
            }
        }

        await _db.SaveChangesAsync();

        // После SaveChanges у сущностей заполнены Id — синхронизируем с DTO ответа
        for (var i = 0; i < result.Lines.Count; i++)
        {
            result.Lines[i].SuspenseLineId = createdLines[i].Id;
        }

        // Логируем первичный статус каждой строки (после SaveChanges — IDs уже заполнены)
        foreach (var entity in createdLines)
        {
            await _audit.LogLineAsync(entity.Id, null, null, entity.BusinessStatus);
        }

        return result;
    }

    public async Task<ValidationLineResultDto> ValidateSingleAsync(SuspenseLineDto line)
    {
        var (lineResult, entity) = await ProcessLineAsync(line);
        await _db.SaveChangesAsync();
        lineResult.SuspenseLineId = entity.Id;
        await _audit.LogLineAsync(entity.Id, null, null, entity.BusinessStatus);
        return lineResult;
    }

    private async Task<(ValidationLineResultDto Result, SuspenseLine Entity)> ProcessLineAsync(SuspenseLineDto dto)
    {
        var product = await FindProductAsync(dto);

        // Шаг 2: Определение статуса
        int status;
        string cause;
        int? productId = null;

        if (product == null)
        {
            // Продукт не найден в каталоге
            status = (int)BusinessStatus.NoProduct;
            cause = "Продукт не найден в каталоге";
        }
        else
        {
            productId = product.Id;

            // Шаг 3: Поиск прав для найденного продукта
            // Все поля должны совпасть: номер договора + территория + компания-отправитель + компания-получатель
            var rightsFound = await FindRightsAsync(product.Id, dto);

            if (!rightsFound)
            {
                status = (int)BusinessStatus.NoRights;
                cause = "Продукт найден, права не определены";
            }
            else
            {
                status = (int)BusinessStatus.Validated;
                cause = "Валидация пройдена";
            }
        }

        // Шаг 4: Создание записи SuspenseLine (всегда, независимо от статуса)
        var suspenseLine = MapToEntity(dto, status, cause, productId);
        _db.SuspenseLines.Add(suspenseLine);

        return (new ValidationLineResultDto
        {
            SuspenseLineId = suspenseLine.Id,
            BusinessStatus = status,
            CauseSuspense = cause,
            ProductId = productId
        }, suspenseLine);
    }

    /// <summary>
    /// Проверка ограничений DTO (длины полей, decimal, Qty и т.д.) до сохранения в БД.
    /// Для Excel первая строка данных = строка 2 листа (под заголовками).
    /// </summary>
    private async Task EnsureAllLinesPassDtoRulesAsync(List<SuspenseLineDto> lines, bool excelStyleRowNumbers)
    {
        if (lines.Count == 0)
        {
            return;
        }

        var errors = new List<ApiError>();
        for (var i = 0; i < lines.Count; i++)
        {
            var vr = await _lineValidator.ValidateAsync(lines[i]);
            if (vr.IsValid)
            {
                continue;
            }

            var rowLabel = excelStyleRowNumbers ? $"Строка {i + 2}: " : string.Empty;
            foreach (var e in vr.Errors)
            {
                errors.Add(new ApiError
                {
                    Field = e.PropertyName,
                    Message = $"{rowLabel}{e.ErrorMessage}"
                });
            }
        }

        if (errors.Count > 0)
        {
            throw new Common.Exceptions.ValidationException(
                "Данные не прошли проверку (длина полей, форматы чисел и т.д.). Исправьте указанные строки и повторите операцию.",
                errors);
        }
    }

    /// <summary>
    /// Поиск продукта в каталоге по приоритетной цепочке:
    /// 1. ISRC → 2. Barcode → 3. Title + Artist → 4. CatalogNumber
    /// Каждый следующий критерий пробуется только если предыдущий ничего не нашёл.
    /// </summary>
    private async Task<CatalogProduct?> FindProductAsync(SuspenseLineDto dto)
    {
        var baseQuery = _db.CatalogProducts.AsNoTracking().Where(p => p.ArchiveLevel == 0);

        // Приоритет 1: ISRC
        if (!string.IsNullOrWhiteSpace(dto.Isrc))
        {
            var product = await baseQuery.FirstOrDefaultAsync(p => p.Isrc == dto.Isrc);
            if (product != null) return product;
        }

        // Приоритет 2: Barcode
        if (!string.IsNullOrWhiteSpace(dto.Barcode))
        {
            var product = await baseQuery.FirstOrDefaultAsync(p => p.Barcode == dto.Barcode);
            if (product != null) return product;
        }

        // Приоритет 3: Title + Artist (оба поля обязательны для этого критерия)
        if (!string.IsNullOrWhiteSpace(dto.TrackTitle) && !string.IsNullOrWhiteSpace(dto.Artist))
        {
            var product = await baseQuery.FirstOrDefaultAsync(p =>
                p.ProductName == dto.TrackTitle && p.Artist == dto.Artist);
            if (product != null) return product;
        }

        // Приоритет 4: CatalogNumber
        if (!string.IsNullOrWhiteSpace(dto.CatalogNumber))
        {
            var product = await baseQuery.FirstOrDefaultAsync(p => p.CatalogNumber == dto.CatalogNumber);
            if (product != null) return product;
        }

        return null;
    }

    /// <summary>
    /// Поиск прав для продукта по полному совпадению всех атрибутов
    /// </summary>
    private async Task<bool> FindRightsAsync(int productId, SuspenseLineDto dto)
    {
        // Если хотя бы одно ключевое поле прав пустое — права точно не найти
        if (string.IsNullOrWhiteSpace(dto.AgreementNumber) ||
            string.IsNullOrWhiteSpace(dto.TerritoryCode))
        {
            return false;
        }

        // Поиск по компаниям: сначала пробуем по ID, если есть. Иначе по названию.
        var query = _db.CatalogProductRights
            .AsNoTracking()
            .Where(r => r.CatalogProductId == productId && r.ArchiveLevel == 0);

        // Совпадение номера договора
        query = query.Where(r => r.DocNumber == dto.AgreementNumber);

        // Совпадение территории
        query = query.Where(r => r.TerritoryCode == dto.TerritoryCode);

        // Совпадение компании-отправителя
        if (dto.SenderCompanyId.HasValue)
        {
            query = query.Where(r => r.CompanySenderId == dto.SenderCompanyId.Value);
        }
        else if (!string.IsNullOrWhiteSpace(dto.SenderCompany))
        {
            query = query.Where(r => r.CompanySender == dto.SenderCompany);
        }
        else
        {
            return false;
        }

        // Совпадение компании-получателя
        if (dto.RecipientCompanyId.HasValue)
        {
            query = query.Where(r => r.CompanyReceiverId == dto.RecipientCompanyId.Value);
        }
        else if (!string.IsNullOrWhiteSpace(dto.RecipientCompany))
        {
            query = query.Where(r => r.CompanyReceiver == dto.RecipientCompany);
        }
        else
        {
            return false;
        }

        return await query.AnyAsync();
    }

    /// <summary>
    /// Маппинг DTO → Entity
    /// </summary>
    private static SuspenseLine MapToEntity(SuspenseLineDto dto, int status, string cause, int? productId)
    {
        return new SuspenseLine
        {
            Isrc = dto.Isrc,
            Barcode = dto.Barcode,
            CatalogNumber = dto.CatalogNumber,
            SenderCompany = dto.SenderCompany,
            RecipientCompany = dto.RecipientCompany,
            Operator = dto.Operator,
            Artist = dto.Artist,
            TrackTitle = dto.TrackTitle,
            AgreementType = dto.AgreementType,
            AgreementNumber = dto.AgreementNumber,
            TerritoryCode = dto.TerritoryCode,
            Qty = dto.Qty,
            Ppd = dto.Ppd,
            ExchangeCurrency = dto.ExchangeCurrency,
            ExchangeRate = dto.ExchangeRate,
            Genre = dto.Genre,
            SenderCompanyId = dto.SenderCompanyId,
            RecipientCompanyId = dto.RecipientCompanyId,
            ProductId = productId,
            BusinessStatus = status,
            CauseSuspense = cause,
            CreateTime = DateTime.UtcNow,
            ArchiveLevel = 0
        };
    }
}
