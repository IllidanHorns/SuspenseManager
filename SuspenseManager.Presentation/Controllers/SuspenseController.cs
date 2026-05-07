using System.Security.Claims;
using Application.Interfaces;
using Common.DTOs;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuspenseManager.Middleware;

namespace SuspenseManager.Controllers;

/// <summary>
/// Контроллер суспенсов: ручной ввод, просмотр списка, обновление
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
public class SuspenseController : ControllerBase
{
    private readonly IValidationService _validationService;
    private readonly ISuspenseService _suspenseService;
    private readonly IValidator<SuspenseLineDto> _validator;
    private readonly ILogger<SuspenseController> _logger;

    public SuspenseController(
        IValidationService validationService,
        ISuspenseService suspenseService,
        IValidator<SuspenseLineDto> validator,
        ILogger<SuspenseController> logger)
    {
        _validationService = validationService;
        _suspenseService = suspenseService;
        _validator = validator;
        _logger = logger;
    }

    private int GetCurrentAccountId()
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier)
                    ?? User.FindFirstValue("account_id");
        return int.TryParse(value, out var id) ? id : 0;
    }

    /// <summary>
    /// Список суспенсов с пагинацией, фильтрацией, сортировкой
    /// </summary>
    [HttpGet]
    [RequirePermission(PermissionCodes.UploadsView)]
    public async Task<IActionResult> GetAll([FromQuery] SuspenseListRequest request, CancellationToken ct)
    {
        var result = await _suspenseService.GetSuspensesAsync(request, GetCurrentAccountId(), ct);
        return Ok(ApiResponse<PagedResponse<Models.SuspenseLine>>.Success(result));
    }

    /// <summary>
    /// Суспенс по ID
    /// </summary>
    [HttpGet("{id:int}")]
    [RequirePermission(PermissionCodes.UploadsView)]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var entity = await _suspenseService.GetByIdAsync(id, ct);
        if (entity == null)
        {
            return NotFound(ApiResponse<object>.Fail(404, $"Суспенс с ID {id} не найден", "NOT_FOUND"));
        }

        return Ok(ApiResponse<Models.SuspenseLine>.Success(entity));
    }

    /// <summary>
    /// Несгруппированные суспенсы (статус 0 — нет продукта, статус 1 — нет прав)
    /// </summary>
    [HttpGet("ungrouped")]
    [RequirePermission(PermissionCodes.UploadsView)]
    public async Task<IActionResult> GetUngrouped([FromQuery] int status, [FromQuery] PagedRequest request, CancellationToken ct)
    {
        var result = await _suspenseService.GetUngroupedAsync(status, request, ct);
        return Ok(ApiResponse<PagedResponse<Models.SuspenseLine>>.Success(result));
    }

    /// <summary>
    /// Ручной ввод суспенса через форму
    /// </summary>
    [HttpPost]
    [RequirePermission(PermissionCodes.UploadsCreate)]
    public async Task<IActionResult> Create([FromBody] SuspenseLineDto dto)
    {
        var validationResult = await _validator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            var errors = validationResult.Errors
                .Select(e => new ApiError { Field = e.PropertyName, Message = e.ErrorMessage })
                .ToList();

            return BadRequest(ApiResponse<ValidationLineResultDto>.Fail(
                400, "Ошибки валидации входных данных", "VALIDATION_ERROR", errors));
        }

        _logger.LogInformation(
            "Ручной ввод суспенса: Artist={Artist}, Title={Title}, ISRC={Isrc}",
            dto.Artist, dto.TrackTitle, dto.Isrc);

        var result = await _validationService.ValidateSingleAsync(dto);

        _logger.LogInformation(
            "Суспенс создан: ID={SuspenseLineId}, Status={BusinessStatus}, ProductId={ProductId}",
            result.SuspenseLineId, result.BusinessStatus, result.ProductId);

        var apiResponse = ApiResponse<ValidationLineResultDto>.Created(
            result,
            result.BusinessStatus switch
            {
                0 => "Суспенс создан: продукт не найден в каталоге",
                1 => "Суспенс создан: продукт найден, права не определены",
                88 => "Валидация пройдена успешно",
                _ => "Суспенс создан"
            },
            result.BusinessStatus switch
            {
                0 => "SUSPENSE_NO_PRODUCT",
                1 => "SUSPENSE_NO_RIGHTS",
                88 => "SUSPENSE_VALIDATED",
                _ => "SUSPENSE_CREATED"
            });

        return StatusCode(201, apiResponse);
    }

    /// <summary>
    /// Обновление суспенса (п.26)
    /// </summary>
    [HttpPut("{id:int}")]
    [RequirePermission(PermissionCodes.UploadsCreate)]
    public async Task<IActionResult> Update(int id, [FromBody] SuspenseLineDto dto)
    {
        var entity = await _suspenseService.UpdateAsync(id, dto);

        _logger.LogInformation("Суспенс обновлён: ID={Id}", id);

        return Ok(ApiResponse<Models.SuspenseLine>.Success(entity, "Суспенс обновлён", "SUSPENSE_UPDATED"));
    }

    /// <summary>
    /// Мягкая архивация суспенс-строки (доступно только для статусов 0 и 1)
    /// </summary>
    [HttpDelete("{id:int}")]
    [RequirePermission(PermissionCodes.UploadsCreate)]
    public async Task<IActionResult> Archive(int id, CancellationToken ct)
    {
        await _suspenseService.ArchiveAsync(id, ct);
        _logger.LogInformation("Суспенс архивирован: ID={Id}", id);
        return Ok(ApiResponse<object>.Success(new { id }, "Суспенс архивирован", "SUSPENSE_ARCHIVED"));
    }
}
