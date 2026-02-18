using Application.Interfaces;
using Common.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SuspenseManager.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserService userService, ILogger<UserController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] PagedRequest request, CancellationToken ct)
    {
        var result = await _userService.GetUsersAsync(request, ct);
        return Ok(ApiResponse<PagedResponse<Models.User>>.Success(result));
    }

    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var user = await _userService.GetByIdAsync(id, ct);
        if (user == null)
            return NotFound(ApiResponse<object>.Fail(404, $"Пользователь с ID {id} не найден", "NOT_FOUND"));

        return Ok(ApiResponse<Models.User>.Success(user));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateUserDto dto, CancellationToken ct)
    {
        var user = await _userService.CreateAsync(dto, ct);
        _logger.LogInformation("Пользователь создан: ID={Id}, Email={Email}", user.Id, user.Email);
        return StatusCode(201, ApiResponse<Models.User>.Created(user, "Пользователь создан", "USER_CREATED"));
    }

    [HttpPut("{id:int}")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateUserDto dto, CancellationToken ct)
    {
        var user = await _userService.UpdateAsync(id, dto, ct);
        _logger.LogInformation("Пользователь обновлён: ID={Id}", id);
        return Ok(ApiResponse<Models.User>.Success(user, "Пользователь обновлён", "USER_UPDATED"));
    }

    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id, CancellationToken ct)
    {
        await _userService.DeleteAsync(id, ct);
        _logger.LogInformation("Пользователь удалён (архивирован): ID={Id}", id);
        return Ok(ApiResponse<object>.Success(new { id }, "Пользователь удалён", "USER_DELETED"));
    }
}
