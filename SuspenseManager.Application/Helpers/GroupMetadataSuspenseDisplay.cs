using Models;
using Models.Enums;

namespace Application.Helpers;

/// <summary>
/// Подстановка «эффективных» продуктовых полей для отображения строк суспенса в контексте группы:
/// метаданные группы имеют приоритет; для статусов «нет прав» далее — каталог, затем строка.
/// Не изменяет БД.
/// </summary>
public static class GroupMetadataSuspenseDisplay
{
    public static void ApplyToSuspenseLine(
        SuspenseLine line,
        GroupMetadata? meta,
        CatalogProduct? catalog,
        int groupBusinessStatus)
    {
        ArgumentNullException.ThrowIfNull(line);
        var useCatalog = UseCatalogFallback(groupBusinessStatus);

        line.Isrc = Pick(meta?.Isrc, useCatalog ? catalog?.Isrc : null, line.Isrc);
        line.Barcode = Pick(meta?.Barcode, useCatalog ? catalog?.Barcode : null, line.Barcode);
        line.CatalogNumber = Pick(meta?.CatalogNumber, useCatalog ? catalog?.CatalogNumber : null, line.CatalogNumber);
        line.Artist = Pick(meta?.Artist, useCatalog ? catalog?.Artist : null, line.Artist);
        line.TrackTitle = Pick(meta?.Title, useCatalog ? catalog?.ProductName : null, line.TrackTitle);
        line.Genre = Pick(meta?.Genre, useCatalog ? catalog?.Genre : null, line.Genre);
    }

    /// <summary>
    /// Эффективные строковые поля продукта для строки таблицы групп / экспорта (без мутации сущностей).
    /// </summary>
    public static (string? Title, string? Artist, string? Isrc, string? Barcode, string? CatalogNumber, string? Genre)
        GetEffectiveProductDisplay(GroupMetadata? meta, CatalogProduct? catalog, int groupBusinessStatus)
    {
        var useCatalog = UseCatalogFallback(groupBusinessStatus);
        string? catTitle = useCatalog ? catalog?.ProductName : null;
        string? catArtist = useCatalog ? catalog?.Artist : null;
        string? catIsrc = useCatalog ? catalog?.Isrc : null;
        string? catBarcode = useCatalog ? catalog?.Barcode : null;
        string? catCatNum = useCatalog ? catalog?.CatalogNumber : null;
        string? catGenre = useCatalog ? catalog?.Genre : null;

        return (
            Pick(meta?.Title, catTitle, null),
            Pick(meta?.Artist, catArtist, null),
            Pick(meta?.Isrc, catIsrc, null),
            Pick(meta?.Barcode, catBarcode, null),
            Pick(meta?.CatalogNumber, catCatNum, null),
            Pick(meta?.Genre, catGenre, null)
        );
    }

    private static bool UseCatalogFallback(int groupBusinessStatus) =>
        groupBusinessStatus is (int)BusinessStatus.InGroupNoRights
            or (int)BusinessStatus.PostponedNoRights
            or (int)BusinessStatus.BackOfficeNoRights;

    private static string? Pick(string? metaValue, string? catalogValue, string? lineValue)
    {
        if (metaValue != null) return metaValue;
        if (catalogValue != null) return catalogValue;
        return lineValue;
    }
}
