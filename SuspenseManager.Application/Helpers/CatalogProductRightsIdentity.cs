using System.Linq.Expressions;
using Common.DTOs;
using Common.Exceptions;
using Models;

namespace Application.Helpers;

/// <summary>
/// Обязательные поля записи прав в каталоге — совпадают с бизнес-проверкой метаправ при валидации группы.
/// </summary>
public static class CatalogProductRightsIdentity
{
    public const string MandatoryFieldsDescription =
        "компания-отправитель, компания-получатель, территория, код территории (не пустой), дата начала и окончания договора, доля (%)";

    /// <summary>Проверка загруженной сущности (список, фильтр «неполные»).</summary>
    public static bool IsIncomplete(CatalogProductRights r) =>
        r.CompanySenderId == 0 ||
        r.CompanyReceiverId == 0 ||
        r.TerritoryId == 0 ||
        string.IsNullOrWhiteSpace(r.TerritoryCode) ||
        r.DocStart == default ||
        r.DocEnd == default ||
        !IsShareInValidRange(r.Share);

    /// <summary>Для LINQ to Entities (без IsNaN — редкие случаи в БД обрабатывает IsIncomplete в памяти).</summary>
    public static readonly Expression<Func<CatalogProductRights, bool>> IncompletePredicate = p =>
        p.CompanySenderId == 0 ||
        p.CompanyReceiverId == 0 ||
        p.TerritoryId == 0 ||
        string.IsNullOrWhiteSpace(p.TerritoryCode) ||
        p.DocStart == default ||
        p.DocEnd == default ||
        p.Share < 0 ||
        p.Share > 100;

    private static bool IsShareInValidRange(double share) =>
        !double.IsNaN(share) && !double.IsInfinity(share) && share >= 0 && share <= 100;

    /// <summary>Перед созданием записи в каталоге — DTO + территория из БД.</summary>
    public static void EnsureCompleteForCreate(CreateCatalogProductRightsDto dto, Territory territory)
    {
        var missing = CollectMissingParts(
            dto.CompanySenderId,
            dto.CompanyReceiverId,
            dto.TerritoryId,
            territory.TerritoryCode,
            dto.DocStart,
            dto.DocEnd,
            dto.Share);

        if (missing.Count == 0)
            return;

        throw new BusinessException(
            "Нельзя сохранить право в каталог: не заполнены обязательные поля. " +
            $"Не указано: {string.Join(", ", missing)}.",
            "CATALOG_RIGHTS_INCOMPLETE");
    }

    /// <summary>После частичного обновления запись прав должна оставаться полной (null в DTO = «не менять»).</summary>
    public static void EnsureCompleteRightsRecordOrThrow(CatalogProductRights r)
    {
        var missing = CollectMissingParts(
            r.CompanySenderId,
            r.CompanyReceiverId,
            r.TerritoryId,
            r.TerritoryCode,
            r.DocStart,
            r.DocEnd,
            r.Share);

        if (missing.Count == 0)
            return;

        throw new BusinessException(
            "Сохранить нельзя: в записи прав должны быть заполнены обязательные поля. " +
            $"Сейчас не задано: {string.Join(", ", missing)}. " +
            "Если поле не передаётся при редактировании, старое значение сохраняется — очистить обязательное поле нельзя.",
            "CATALOG_RIGHTS_INCOMPLETE");
    }

    private static List<string> CollectMissingParts(
        int senderId,
        int receiverId,
        int territoryId,
        string? territoryCode,
        DateOnly docStart,
        DateOnly docEnd,
        double share)
    {
        var missing = new List<string>();
        if (senderId <= 0) missing.Add("компания-отправитель");
        if (receiverId <= 0) missing.Add("компания-получатель");
        if (territoryId <= 0) missing.Add("территория");
        if (string.IsNullOrWhiteSpace(territoryCode)) missing.Add("код территории");
        if (docStart == default) missing.Add("дата начала договора");
        if (docEnd == default) missing.Add("дата окончания договора");
        if (!IsShareInValidRange(share)) missing.Add("доля (%)");
        return missing;
    }
}
