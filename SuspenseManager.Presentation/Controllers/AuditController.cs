using System.Security.Claims;
using Application.Interfaces;
using Common.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SuspenseManager.Controllers;

/// <summary>
/// Аудит: история изменений статусов групп и строк суспенсов
/// </summary>
[ApiController]
[Route("api/audit")]
[Authorize]
public class AuditController : ControllerBase
{
    private readonly IAuditService _auditService;

    public AuditController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    // ── Группы ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Список активных групп с инфо о создателе и последнем изменении статуса.
    /// Поддерживает фильтрацию по статусу, дате создания, "только мои".
    /// </summary>
    [HttpGet("groups")]
    public async Task<IActionResult> GetGroups([FromQuery] AuditGroupsRequest request, CancellationToken ct)
    {
        var currentAccountId = GetCurrentAccountId();
        var result = await _auditService.GetGroupsAsync(request, currentAccountId, ct);
        return Ok(ApiResponse<PagedResponse<AuditGroupDto>>.Success(result));
    }

    /// <summary>
    /// История переходов статусов конкретной группы (от новых к старым).
    /// Открывается по клику на группу в списке аудита.
    /// </summary>
    [HttpGet("groups/{groupId:int}/logs")]
    public async Task<IActionResult> GetGroupLogs(
        int groupId,
        [FromQuery] PagedRequest request,
        CancellationToken ct)
    {
        var result = await _auditService.GetGroupLogsAsync(groupId, request, ct);
        return Ok(ApiResponse<PagedResponse<AuditLogEntryDto>>.Success(result));
    }

    // ── Строки суспенсов ────────────────────────────────────────────────────

    /// <summary>
    /// Список строк суспенсов в активных группах с последним изменением статуса.
    /// Поддерживает фильтрацию по статусу, группе, "только мои".
    /// </summary>
    [HttpGet("suspense-lines")]
    public async Task<IActionResult> GetLines([FromQuery] AuditLinesRequest request, CancellationToken ct)
    {
        var currentAccountId = GetCurrentAccountId();
        var result = await _auditService.GetLinesAsync(request, currentAccountId, ct);
        return Ok(ApiResponse<PagedResponse<AuditLineDto>>.Success(result));
    }

    /// <summary>
    /// История переходов статусов конкретной строки суспенса (от новых к старым).
    /// Открывается по клику на строку в списке аудита.
    /// </summary>
    [HttpGet("suspense-lines/{lineId:int}/logs")]
    public async Task<IActionResult> GetLineLogs(
        int lineId,
        [FromQuery] PagedRequest request,
        CancellationToken ct)
    {
        var result = await _auditService.GetLineLogsAsync(lineId, request, ct);
        return Ok(ApiResponse<PagedResponse<AuditLogEntryDto>>.Success(result));
    }

    /// <summary>
    /// Лента последних действий аккаунта (переходы статусов по группам и строкам из логов аудита).
    /// </summary>
    [HttpGet("accounts/{accountId:int}/activity")]
    public async Task<IActionResult> GetAccountActivity(
        int accountId,
        [FromQuery] PagedRequest request,
        CancellationToken ct)
    {
        var result = await _auditService.GetAccountActivityAsync(accountId, request, ct);
        return Ok(ApiResponse<PagedResponse<AccountActivityItemDto>>.Success(result));
    }

    // ── Вспомогательные методы ───────────────────────────────────────────────

    private int GetCurrentAccountId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("account_id");
        return int.TryParse(value, out var id) ? id : 0;
    }
}
