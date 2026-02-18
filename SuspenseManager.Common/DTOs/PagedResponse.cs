namespace Common.DTOs;

/// <summary>
/// Стандартный ответ с пагинацией
/// </summary>
public class PagedResponse<T>
{
    /// <summary>
    /// Элементы текущей страницы
    /// </summary>
    public List<T> Items { get; set; } = [];

    /// <summary>
    /// Общее количество записей (без пагинации)
    /// </summary>
    public int TotalCount { get; set; }

    /// <summary>
    /// Текущая страница
    /// </summary>
    public int PageNumber { get; set; }

    /// <summary>
    /// Размер страницы
    /// </summary>
    public int PageSize { get; set; }

    /// <summary>
    /// Общее количество страниц
    /// </summary>
    public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 0;
}
