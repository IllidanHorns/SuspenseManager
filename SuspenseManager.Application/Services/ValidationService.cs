using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Application.Interfaces;
using Common.DTOs;
using Data;
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

    public ValidationService(SuspenseManagerDbContext db)
    {
        _db = db;
    }

    public async Task<ValidationResultDto> ValidateBatchAsync(List<SuspenseLineDto> lines)
    {
        var result = new ValidationResultDto
        {
            TotalRows = lines.Count
        };

        foreach (var line in lines)
        {
            var lineResult = await ProcessLineAsync(line);
            result.Lines.Add(lineResult);

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
        return result;
    }

    public async Task<ValidationLineResultDto> ValidateSingleAsync(SuspenseLineDto line)
    {
        var lineResult = await ProcessLineAsync(line);
        await _db.SaveChangesAsync();
        return lineResult;
    }

    /// <summary>
    /// Обработка одной строки: поиск продукта → поиск прав → присвоение статуса → сохранение
    /// </summary>
    private async Task<ValidationLineResultDto> ProcessLineAsync(SuspenseLineDto dto)
    {
        // Шаг 1: Поиск продукта в каталоге
        // Все поля должны совпасть: ISRC + Barcode + CatalogNumber + ProductFormatCode
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

        return new ValidationLineResultDto
        {
            SuspenseLineId = suspenseLine.Id,
            BusinessStatus = status,
            CauseSuspense = cause,
            ProductId = productId
        };
    }

    /// <summary>
    /// Поиск продукта в каталоге по полному совпадению всех идентификаторов
    /// </summary>
    private async Task<CatalogProduct?> FindProductAsync(SuspenseLineDto dto)
    {
        // Если хотя бы одно ключевое поле пустое — продукт точно не найти по полному совпадению
        if (string.IsNullOrWhiteSpace(dto.Isrc) ||
            string.IsNullOrWhiteSpace(dto.Barcode) ||
            string.IsNullOrWhiteSpace(dto.CatalogNumber) ||
            string.IsNullOrWhiteSpace(dto.ProductFormatCode))
        {
            return null;
        }

        return await _db.CatalogProducts
            .AsNoTracking()
            .Where(p => p.ArchiveLevel == 0)
            .Where(p =>
                p.Isrc == dto.Isrc &&
                p.Barcode == dto.Barcode &&
                p.CatalogNumber == dto.CatalogNumber &&
                p.ProductFormatCode == dto.ProductFormatCode)
            .FirstOrDefaultAsync();
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
