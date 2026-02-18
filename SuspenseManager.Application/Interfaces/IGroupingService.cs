using Common.DTOs;
using Models;

namespace Application.Interfaces;

/// <summary>
/// Сервис динамической группировки суспенсов.
/// Предпросмотр (GROUP BY по выбранным столбцам) и фиксация (создание SuspenseGroup).
/// </summary>
public interface IGroupingService
{
    /// <summary>
    /// Предпросмотр динамической группировки суспенсов.
    /// Выполняет GROUP BY по выбранным столбцам с фильтрацией, сортировкой и пагинацией.
    /// </summary>
    Task<PagedResponse<GroupingPreviewItem>> PreviewAsync(GroupingPreviewRequest request, CancellationToken ct = default);

    /// <summary>
    /// Фиксация (сохранение) группы: создаёт SuspenseGroup, обновляет статусы строк,
    /// создаёт SuspenseGroupLink для каждой строки.
    /// </summary>
    Task<SuspenseGroup> CommitAsync(GroupingCommitRequest request, CancellationToken ct = default);
}
