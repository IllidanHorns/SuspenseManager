using System.Security.Claims;
using Application.Interfaces;
using Common.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SuspenseManager.Controllers;

/// <summary>
/// Контроллер групп: просмотр сохранённых групп, нет продукта, нет прав, суспенсы группы
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class GroupController : ControllerBase
{
    private readonly IGroupService _groupService;

    public GroupController(IGroupService groupService)
    {
        _groupService = groupService;
    }

    private int GetCurrentAccountId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("account_id");
        return int.TryParse(value, out var id) ? id : 0;
    }

    /// <summary>
    /// Группы "нет продукта" (статус 15) — п.8
    /// </summary>
    [HttpGet("no-product")]
    public async Task<IActionResult> GetNoProduct([FromQuery] GroupListRequest request, CancellationToken ct)
    {
        var result = await _groupService.GetNoProductGroupsAsync(request, GetCurrentAccountId(), ct);
        return Ok(ApiResponse<PagedResponse<Models.SuspenseGroup>>.Success(result));
    }

    /// <summary>
    /// Группы "нет прав" (статус 16) — п.8
    /// </summary>
    [HttpGet("no-rights")]
    public async Task<IActionResult> GetNoRights([FromQuery] GroupListRequest request, CancellationToken ct)
    {
        var result = await _groupService.GetNoRightsGroupsAsync(request, GetCurrentAccountId(), ct);
        return Ok(ApiResponse<PagedResponse<Models.SuspenseGroup>>.Success(result));
    }

    /// <summary>
    /// Все сохранённые группы (статус 15 и 16) — п.10
    /// </summary>
    [HttpGet("saved")]
    public async Task<IActionResult> GetSaved([FromQuery] GroupListRequest request, CancellationToken ct)
    {
        var result = await _groupService.GetSavedGroupsAsync(request, GetCurrentAccountId(), ct);
        return Ok(ApiResponse<PagedResponse<Models.SuspenseGroup>>.Success(result));
    }

    /// <summary>
    /// Группа по ID с метаданными
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var group = await _groupService.GetByIdAsync(id, ct);
        if (group == null)
        {
            return NotFound(ApiResponse<object>.Fail(404, $"Группа с ID {id} не найдена", "NOT_FOUND"));
        }

        return Ok(ApiResponse<Models.SuspenseGroup>.Success(group));
    }

    /// <summary>
    /// Суспенсы конкретной группы с пагинацией
    /// </summary>
    [HttpGet("{id:int}/suspenses")]
    public async Task<IActionResult> GetGroupSuspenses(int id, [FromQuery] PagedRequest request, CancellationToken ct)
    {
        var result = await _groupService.GetGroupSuspensesAsync(id, request, ct);
        return Ok(ApiResponse<PagedResponse<Models.SuspenseLine>>.Success(result));
    }
}
