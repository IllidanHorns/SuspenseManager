using Common.DTOs;

namespace Application.Interfaces;

public interface IMonitorService
{
    Task<List<OperatorMonitorDto>> GetOperatorSummaryAsync(CancellationToken ct = default);
    Task<List<OperatorGroupDto>>   GetOperatorGroupsAsync(int accountId, CancellationToken ct = default);
}
