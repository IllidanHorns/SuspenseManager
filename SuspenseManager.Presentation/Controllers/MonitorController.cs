using Application.Interfaces;
using Common.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuspenseManager.Middleware;

namespace SuspenseManager.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class MonitorController : ControllerBase
{
    private readonly IMonitorService _monitorService;

    public MonitorController(IMonitorService monitorService)
    {
        _monitorService = monitorService;
    }

    /// <summary>
    /// Сводная таблица по операторам: агрегированные показатели по всем их активным группам.
    /// Включает счётчики флагов для быстрой идентификации проблем.
    /// </summary>
    [HttpGet("operators")]
    [RequirePermission(PermissionCodes.MonitorView)]
    public async Task<IActionResult> GetOperators(CancellationToken ct)
    {
        var result = await _monitorService.GetOperatorSummaryAsync(ct);
        return Ok(ApiResponse<List<OperatorMonitorDto>>.Success(result));
    }

    /// <summary>
    /// Список активных групп конкретного оператора с расчётом возраста и флагов.
    /// </summary>
    [HttpGet("operators/{accountId:int}/groups")]
    [RequirePermission(PermissionCodes.MonitorView)]
    public async Task<IActionResult> GetOperatorGroups(int accountId, CancellationToken ct)
    {
        var result = await _monitorService.GetOperatorGroupsAsync(accountId, ct);
        return Ok(ApiResponse<List<OperatorGroupDto>>.Success(result));
    }
}
