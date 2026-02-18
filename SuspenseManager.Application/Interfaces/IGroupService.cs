using Common.DTOs;
using Models;

namespace Application.Interfaces;

public interface IGroupService
{
    /// <summary>
    /// Группы со статусом "в группе, нет продукта" (15) — сохранённые группы
    /// </summary>
    Task<PagedResponse<SuspenseGroup>> GetNoProductGroupsAsync(PagedRequest request, CancellationToken ct = default);

    /// <summary>
    /// Группы со статусом "в группе, нет прав" (16) — сохранённые группы
    /// </summary>
    Task<PagedResponse<SuspenseGroup>> GetNoRightsGroupsAsync(PagedRequest request, CancellationToken ct = default);

    /// <summary>
    /// Все сохранённые группы (статус 15 и 16)
    /// </summary>
    Task<PagedResponse<SuspenseGroup>> GetSavedGroupsAsync(PagedRequest request, CancellationToken ct = default);

    /// <summary>
    /// Одна группа по ID с суспенсами и метаданными
    /// </summary>
    Task<SuspenseGroup?> GetByIdAsync(int id, CancellationToken ct = default);

    /// <summary>
    /// Суспенсы конкретной группы
    /// </summary>
    Task<PagedResponse<SuspenseLine>> GetGroupSuspensesAsync(int groupId, PagedRequest request, CancellationToken ct = default);
}
