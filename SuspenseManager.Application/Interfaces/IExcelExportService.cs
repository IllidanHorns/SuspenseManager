namespace Application.Interfaces;

public interface IExcelExportService
{
    /// <summary>
    /// Экспорт суспенсов группы в Excel (п.19)
    /// </summary>
    Task<byte[]> ExportGroupSuspensesAsync(int groupId, CancellationToken ct = default);

    /// <summary>
    /// Экспорт всех групп указанного статуса в Excel (п.18)
    /// </summary>
    Task<byte[]> ExportGroupsAsync(int businessStatus, CancellationToken ct = default);
}
