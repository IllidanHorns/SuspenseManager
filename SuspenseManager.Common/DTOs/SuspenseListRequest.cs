namespace Common.DTOs;

/// <summary>
/// Список суспенс-строк с опцией «только мои» (по создателю текущей группы).
/// </summary>
public class SuspenseListRequest : PagedRequest
{
    /// <summary>
    /// Оставить только строки, входящие в группы, созданные текущим аккаунтом.
    /// Строки без группы не попадают в выборку.
    /// </summary>
    public bool OnlyMine { get; set; }
}
