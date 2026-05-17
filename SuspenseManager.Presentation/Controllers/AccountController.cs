using System.Collections.Generic;
using Application.Interfaces;
using Common.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuspenseManager.Middleware;

namespace SuspenseManager.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(IAccountService accountService, ILogger<AccountController> logger)
    {
        _accountService = accountService;
        _logger = logger;
    }

    [HttpGet]
    [RequirePermission(PermissionCodes.AdminUsersManage)]
    public async Task<IActionResult> GetAll([FromQuery] PagedRequest request, [FromQuery] string? userLink, CancellationToken ct)
    {
        // Явный query userLink=linked|unlinked: вложенный Filters[UserProfile] из строки запроса к Dictionary в PagedRequest на практике не биндится.
        if (userLink is "linked" or "unlinked")
        {
            request.Filters ??= new Dictionary<string, string>();
            request.Filters["UserProfile"] = userLink;
        }

        var result = await _accountService.GetAccountsAsync(request, ct);
        return Ok(ApiResponse<PagedResponse<Models.Account>>.Success(result));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var account = await _accountService.GetByIdAsync(id, ct);
        if (account == null)
        {
            return NotFound(ApiResponse<object>.Fail(404, $"Аккаунт с ID {id} не найден", "NOT_FOUND"));
        }

        return Ok(ApiResponse<Models.Account>.Success(account));
    }

    [HttpPost]
    [RequirePermission(PermissionCodes.AdminUsersManage)]
    public async Task<IActionResult> Create([FromBody] CreateAccountDto dto, CancellationToken ct)
    {
        var account = await _accountService.CreateAsync(dto, ct);
        _logger.LogInformation("Аккаунт создан: ID={Id}, Login={Login}", account.Id, account.Login);
        return StatusCode(201, ApiResponse<Models.Account>.Created(account, "Аккаунт создан", "ACCOUNT_CREATED"));
    }

    [HttpPut("{id:int}")]
    [RequirePermission(PermissionCodes.AdminUsersManage)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAccountDto dto, CancellationToken ct)
    {
        var account = await _accountService.UpdateAsync(id, dto, ct);
        _logger.LogInformation("Аккаунт обновлён: ID={Id}", id);
        return Ok(ApiResponse<Models.Account>.Success(account, "Аккаунт обновлён", "ACCOUNT_UPDATED"));
    }

    [HttpDelete("{id:int}")]
    [RequirePermission(PermissionCodes.AdminUsersManage)]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _accountService.DeleteAsync(id, ct);
        _logger.LogInformation("Аккаунт удалён (архивирован): ID={Id}", id);
        return Ok(ApiResponse<object>.Success(new { id }, "Аккаунт удалён", "ACCOUNT_DELETED"));
    }

    // --- п.29 Управление правами аккаунта ---

    [HttpGet("{id:int}/rights")]
    [RequirePermission(PermissionCodes.AdminUsersManage)]
    public async Task<IActionResult> GetRights(int id, CancellationToken ct)
    {
        var rights = await _accountService.GetAccountRightsAsync(id, ct);
        return Ok(ApiResponse<List<Models.Rights>>.Success(rights));
    }

    [HttpPost("{id:int}/rights")]
    [RequirePermission(PermissionCodes.AdminPermissionsManage)]
    public async Task<IActionResult> AddRights(int id, [FromBody] AccountRightsDto dto, CancellationToken ct)
    {
        await _accountService.AddRightsAsync(id, dto.RightIds, ct);
        _logger.LogInformation("Права добавлены: AccountId={Id}, RightIds={RightIds}", id, string.Join(",", dto.RightIds));
        return Ok(ApiResponse<object>.Success(new { id, addedRights = dto.RightIds }, "Права добавлены", "RIGHTS_ADDED"));
    }

    [HttpDelete("{id:int}/rights")]
    [RequirePermission(PermissionCodes.AdminPermissionsManage)]
    public async Task<IActionResult> RemoveRights(int id, [FromBody] AccountRightsDto dto, CancellationToken ct)
    {
        await _accountService.RemoveRightsAsync(id, dto.RightIds, ct);
        _logger.LogInformation("Права удалены: AccountId={Id}, RightIds={RightIds}", id, string.Join(",", dto.RightIds));
        return Ok(ApiResponse<object>.Success(new { id, removedRights = dto.RightIds }, "Права удалены", "RIGHTS_REMOVED"));
    }

    /// <summary>Полная замена набора прав аккаунта (админка).</summary>
    [HttpPut("{id:int}/rights")]
    [RequirePermission(PermissionCodes.AdminPermissionsManage)]
    public async Task<IActionResult> ReplaceRights(int id, [FromBody] ReplaceAccountRightsDto dto, CancellationToken ct)
    {
        await _accountService.ReplaceRightsAsync(id, dto.RightIds, ct);
        _logger.LogInformation("Права заменены: AccountId={Id}, Count={Count}", id, dto.RightIds.Count);
        return Ok(ApiResponse<object>.Success(new { id }, "Права обновлены", "RIGHTS_REPLACED"));
    }
}
