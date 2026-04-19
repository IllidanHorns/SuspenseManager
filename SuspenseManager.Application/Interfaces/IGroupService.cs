using Common.DTOs;
using Models;

namespace Application.Interfaces;

public interface IGroupService
{
    Task<PagedResponse<SuspenseGroup>> GetNoProductGroupsAsync(GroupListRequest request, int currentAccountId, CancellationToken ct = default);
    Task<PagedResponse<SuspenseGroup>> GetNoRightsGroupsAsync(GroupListRequest request, int currentAccountId, CancellationToken ct = default);
    Task<PagedResponse<SuspenseGroup>> GetSavedGroupsAsync(GroupListRequest request, int currentAccountId, CancellationToken ct = default);
    Task<SuspenseGroup?> GetByIdAsync(int id, CancellationToken ct = default);
    Task<PagedResponse<SuspenseLine>> GetGroupSuspensesAsync(int groupId, PagedRequest request, CancellationToken ct = default);
}
