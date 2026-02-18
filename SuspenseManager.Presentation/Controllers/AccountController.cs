using Application.Interfaces;
using Common.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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
    public async Task<IActionResult> GetAll([FromQuery] PagedRequest request, CancellationToken ct)
    {
        var result = await _accountService.GetAccountsAsync(request, ct);
        return Ok(ApiResponse<PagedResponse<Models.Account>>.Success(result));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var account = await _accountService.GetByIdAsync(id, ct);
        if (account == null)
            return NotFound(ApiResponse<object>.Fail(404, $"Аккаунт с ID {id} не найден", "NOT_FOUND"));

        return Ok(ApiResponse<Models.Account>.Success(account));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateAccountDto dto, CancellationToken ct)
    {
        var account = await _accountService.CreateAsync(dto, ct);
        _logger.LogInformation("Аккаунт создан: ID={Id}, Login={Login}", account.Id, account.Login);
        return StatusCode(201, ApiResponse<Models.Account>.Created(account, "Аккаунт создан", "ACCOUNT_CREATED"));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAccountDto dto, CancellationToken ct)
    {
        var account = await _accountService.UpdateAsync(id, dto, ct);
        _logger.LogInformation("Аккаунт обновлён: ID={Id}", id);
        return Ok(ApiResponse<Models.Account>.Success(account, "Аккаунт обновлён", "ACCOUNT_UPDATED"));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _accountService.DeleteAsync(id, ct);
        _logger.LogInformation("Аккаунт удалён (архивирован): ID={Id}", id);
        return Ok(ApiResponse<object>.Success(new { id }, "Аккаунт удалён", "ACCOUNT_DELETED"));
    }

    // --- п.29 Управление правами аккаунта ---

    [HttpGet("{id:int}/rights")]
    public async Task<IActionResult> GetRights(int id, CancellationToken ct)
    {
        var rights = await _accountService.GetAccountRightsAsync(id, ct);
        return Ok(ApiResponse<List<Models.Rights>>.Success(rights));
    }

    [HttpPost("{id:int}/rights")]
    public async Task<IActionResult> AddRights(int id, [FromBody] AccountRightsDto dto, CancellationToken ct)
    {
        await _accountService.AddRightsAsync(id, dto.RightIds, ct);
        _logger.LogInformation("Права добавлены: AccountId={Id}, RightIds={RightIds}", id, string.Join(",", dto.RightIds));
        return Ok(ApiResponse<object>.Success(new { id, addedRights = dto.RightIds }, "Права добавлены", "RIGHTS_ADDED"));
    }

    [HttpDelete("{id:int}/rights")]
    public async Task<IActionResult> RemoveRights(int id, [FromBody] AccountRightsDto dto, CancellationToken ct)
    {
        await _accountService.RemoveRightsAsync(id, dto.RightIds, ct);
        _logger.LogInformation("Права удалены: AccountId={Id}, RightIds={RightIds}", id, string.Join(",", dto.RightIds));
        return Ok(ApiResponse<object>.Success(new { id, removedRights = dto.RightIds }, "Права удалены", "RIGHTS_REMOVED"));
    }
}
