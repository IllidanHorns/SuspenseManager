namespace Common.DTOs;

/// <summary>
/// Базовый запрос с пагинацией, сортировкой и динамическими фильтрами.
/// Используется как query-параметры для всех GET-списков.
/// </summary>
public class PagedRequest
{
    private int _pageNumber = 1;
    private int _pageSize = 20;

    /// <summary>
    /// Номер страницы (начиная с 1)
    /// </summary>
    public int PageNumber
    {
        get => _pageNumber;
        set => _pageNumber = value < 1 ? 1 : value;
    }

    /// <summary>
    /// Размер страницы (20, 50, 100)
    /// </summary>
    public int PageSize
    {
        get => _pageSize;
        set => _pageSize = value switch
        {
            <= 0 => 20,
            > 100 => 100,
            _ => value
        };
    }

    /// <summary>
    /// Поле для сортировки (название свойства)
    /// </summary>
    public string? SortBy { get; set; }

    /// <summary>
    /// Направление сортировки: asc / desc
    /// </summary>
    public string SortDirection { get; set; } = "asc";

    /// <summary>
    /// Динамические фильтры: ключ = имя поля, значение = значение фильтра.
    /// Поддерживает суффиксы: _gt, _lt, _gte, _lte, _contains, _from, _to
    /// </summary>
    public Dictionary<string, string>? Filters { get; set; }
}
