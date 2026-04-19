using Application.Interfaces;
using Common.DTOs;
using Data;
using Microsoft.EntityFrameworkCore;

namespace Application.Services;

public class RightsCatalogService : IRightsCatalogService
{
    private readonly SuspenseManagerDbContext _db;

    public RightsCatalogService(SuspenseManagerDbContext db)
    {
        _db = db;
    }

    public async Task<IReadOnlyList<RightCatalogItemDto>> GetCatalogAsync(CancellationToken ct = default)
    {
        return await _db.Rights
            .AsNoTracking()
            .Where(r => r.ArchiveLevel == 0)
            .OrderBy(r => r.Module)
            .ThenBy(r => r.Code)
            .Select(r => new RightCatalogItemDto
            {
                Id = r.Id,
                Code = r.Code,
                Name = r.Name,
                Module = r.Module
            })
            .ToListAsync(ct);
    }

    public async Task<IReadOnlyList<RolePresetDto>> GetRolePresetsAsync(CancellationToken ct = default)
    {
        var all = await _db.Rights.AsNoTracking().Where(r => r.ArchiveLevel == 0).ToListAsync(ct);
        var byCode = all.ToDictionary(r => r.Code, StringComparer.Ordinal);

        var full = all.Select(r => r.Id).Distinct().ToList();

        var admin = all.Where(r => r.Code.StartsWith("admin.", StringComparison.Ordinal)).Select(r => r.Id).ToList();

        var op = all
            .Where(r =>
                !r.Code.StartsWith("admin.", StringComparison.Ordinal) &&
                !r.Code.StartsWith("backoffice.", StringComparison.Ordinal))
            .Select(r => r.Id)
            .Distinct()
            .ToList();

        var boCodes = new HashSet<string>(StringComparer.Ordinal);
        foreach (var r in all)
        {
            if (r.Code.StartsWith("backoffice.", StringComparison.Ordinal) ||
                r.Code.StartsWith("catalog.", StringComparison.Ordinal))
                boCodes.Add(r.Code);
        }

        foreach (var c in BackOfficeExtraViewCodes)
        {
            if (byCode.TryGetValue(c, out var right))
                boCodes.Add(right.Code);
        }

        var backOffice = all.Where(r => boCodes.Contains(r.Code)).Select(r => r.Id).Distinct().ToList();

        return new List<RolePresetDto>
        {
            new()
            {
                Id = "full",
                Name = "Полный доступ",
                Description = "Все права из справочника (как у супер-администратора).",
                RightIds = full
            },
            new()
            {
                Id = "admin",
                Name = "Администратор",
                Description = "Только админка: пользователи, аккаунты и назначение прав.",
                RightIds = admin
            },
            new()
            {
                Id = "operator",
                Name = "Оператор",
                Description = "Вся операторская работа: загрузка, группировка, группы, отложенные, каталог, экспорт, аудит. Без модуля бэк-офиса и без админки. Отправка групп в бэк-офис включена.",
                RightIds = op
            },
            new()
            {
                Id = "backoffice",
                Name = "Сотрудник бэк-офиса",
                Description = "Задания бэк-офиса, полный доступ к каталогу, просмотр суспенсов (контекстные права навигации).",
                RightIds = backOffice
            }
        };
    }

    /// <summary>Права на просмотр разделов, нужные для открытия строк суспенсов и контекста.</summary>
    private static readonly string[] BackOfficeExtraViewCodes =
    [
        "uploads.view",
        "grouping.view",
        "groups.no_product.view",
        "groups.no_rights.view",
        "postponed.view"
    ];
}
