using System.Linq.Expressions;
using Common.Exceptions;
using Models;

namespace Application.Helpers;

/// <summary>
/// Поля идентичности продукта в каталоге — те же, что для быстрой каталогизации и привязки «возможных продуктов».
/// </summary>
public static class CatalogProductIdentity
{
    /// <summary>Проверка в памяти (после загрузки сущности).</summary>
    public static bool IsIncomplete(CatalogProduct p) =>
        string.IsNullOrWhiteSpace(p.ProductName) ||
        string.IsNullOrWhiteSpace(p.Artist) ||
        string.IsNullOrWhiteSpace(p.Isrc) ||
        string.IsNullOrWhiteSpace(p.Barcode) ||
        string.IsNullOrWhiteSpace(p.CatalogNumber);

    /// <summary>Условие «не все обязательные поля заполнены» — для LINQ to Entities.</summary>
    public static readonly Expression<Func<CatalogProduct, bool>> IncompletePredicate = p =>
        string.IsNullOrWhiteSpace(p.ProductName) ||
        string.IsNullOrWhiteSpace(p.Artist) ||
        string.IsNullOrWhiteSpace(p.Isrc) ||
        string.IsNullOrWhiteSpace(p.Barcode) ||
        string.IsNullOrWhiteSpace(p.CatalogNumber);

    public static List<string> CollectMissingIdentityLabels(CatalogProduct p)
    {
        var missing = new List<string>();
        if (string.IsNullOrWhiteSpace(p.ProductName)) missing.Add("название");
        if (string.IsNullOrWhiteSpace(p.Artist)) missing.Add("исполнитель");
        if (string.IsNullOrWhiteSpace(p.Isrc)) missing.Add("ISRC");
        if (string.IsNullOrWhiteSpace(p.Barcode)) missing.Add("баркод");
        if (string.IsNullOrWhiteSpace(p.CatalogNumber)) missing.Add("каталожный номер");
        return missing;
    }

    /// <summary>После любого изменения карточки в каталоге обязательные поля не могут стать пустыми.</summary>
    public static void EnsureCompleteProductOrThrow(CatalogProduct p)
    {
        var missing = CollectMissingIdentityLabels(p);
        if (missing.Count == 0)
            return;

        throw new BusinessException(
            "Сохранить нельзя: в карточке должны быть заполнены обязательные поля данных продукта. " +
            $"Сейчас не задано: {string.Join(", ", missing)}. " +
            "Если поле не передаётся при редактировании, старое значение сохраняется — очистить обязательное поле нельзя.",
            "CATALOG_PRODUCT_INCOMPLETE");
    }
}
