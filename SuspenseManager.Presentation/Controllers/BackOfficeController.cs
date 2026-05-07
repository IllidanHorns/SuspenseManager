using Application.Interfaces;
using Common.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Models;
using SuspenseManager.Middleware;

namespace SuspenseManager.Controllers;

/// <summary>
/// Контроллер для работы сотрудника бэк-офиса:
/// задания, обработка групп, управление каталогом.
/// </summary>
[ApiController]
[Route("api/backoffice")]
[Authorize]
public class BackOfficeController : ControllerBase
{
    private readonly IBackOfficeService _boService;
    private readonly IGroupProcessingService _processingService;
    private readonly ILogger<BackOfficeController> _logger;

    public BackOfficeController(
        IBackOfficeService boService,
        IGroupProcessingService processingService,
        ILogger<BackOfficeController> logger)
    {
        _boService = boService;
        _processingService = processingService;
        _logger = logger;
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Задания
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Список активных заданий бэк-офиса.
    /// Опциональный фильтр: taskType=120 (нет продукта) или taskType=320 (нет прав).
    /// </summary>
    [HttpGet("tasks")]
    [RequirePermission(PermissionCodes.BackofficeView)]
    public async Task<IActionResult> GetTasks([FromQuery] BackOfficeTasksRequest request, CancellationToken ct)
    {
        var (items, totalCount, totalPages) = await _boService.GetTasksAsync(request, ct);
        var paged = new PagedResponse<BackOfficeTaskDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = request.PageNumber,
            PageSize = request.PageSize,
        };
        return Ok(ApiResponse<PagedResponse<BackOfficeTaskDto>>.Success(paged));
    }

    /// <summary>Детальная карточка задания с полной информацией о группе.</summary>
    [HttpGet("tasks/{taskId:int}")]
    [RequirePermission(PermissionCodes.BackofficeView)]
    public async Task<IActionResult> GetTask(int taskId, CancellationToken ct)
    {
        var task = await _boService.GetTaskAsync(taskId, ct);
        return Ok(ApiResponse<BackOfficeTask>.Success(task));
    }

    /// <summary>
    /// Вернуть группу оператору (120→15 или 320→16).
    /// Задание закрывается.
    /// </summary>
    [HttpPost("tasks/{taskId:int}/return")]
    [RequirePermission(PermissionCodes.BackofficeReturn)]
    public async Task<IActionResult> ReturnGroup(int taskId, CancellationToken ct)
    {
        var group = await _boService.ReturnGroupAsync(taskId, ct);
        _logger.LogInformation("BO: группа возвращена оператору. TaskId={TaskId}, GroupId={GroupId}", taskId, group.Id);
        return Ok(ApiResponse<SuspenseGroup>.Success(group, "Группа возвращена операторам", "BO_GROUP_RETURNED"));
    }

    /// <summary>
    /// Удалить группу и все строки суспенсов (мягкое удаление).
    /// Используется когда стримы не принадлежат компании и могут быть проигнорированы.
    /// </summary>
    [HttpPost("tasks/{taskId:int}/delete-group")]
    [RequirePermission(PermissionCodes.BackofficeDelete)]
    public async Task<IActionResult> DeleteGroup(int taskId, CancellationToken ct)
    {
        await _boService.DeleteGroupAsync(taskId, ct);
        _logger.LogInformation("BO: группа и строки удалены. TaskId={TaskId}", taskId);
        return Ok(ApiResponse<object>.Success(new { taskId }, "Группа и строки удалены", "BO_GROUP_DELETED"));
    }

    /// <summary>
    /// Привязать продукт к группе 120 (нет продукта в BO).
    /// Если у продукта есть права → 88, задание закрывается.
    /// Если прав нет → 320, задание остаётся открытым.
    /// </summary>
    [HttpPost("tasks/{taskId:int}/link-product")]
    [RequirePermission(PermissionCodes.BackofficeLinkProduct)]
    public async Task<IActionResult> LinkProduct(int taskId, [FromBody] BoLinkProductDto dto, CancellationToken ct)
    {
        var group = await _boService.LinkProductAsync(taskId, dto.ProductId, ct);
        _logger.LogInformation(
            "BO: продукт привязан. TaskId={TaskId}, GroupId={GroupId}, ProductId={ProductId}, NewStatus={Status}",
            taskId, group.Id, dto.ProductId, group.BusinessStatus);
        return Ok(ApiResponse<SuspenseGroup>.Success(group, "Продукт привязан", "BO_PRODUCT_LINKED"));
    }

    /// <summary>
    /// Валидировать группу 320 (нет прав в BO).
    /// Проверяет права продукта в каталоге; если нет — создаёт из метаправ группы.
    /// При успехе → 88, задание закрывается.
    /// </summary>
    [HttpPost("tasks/{taskId:int}/complete")]
    [RequirePermission(PermissionCodes.BackofficeValidate)]
    public async Task<IActionResult> CompleteTask(int taskId, CancellationToken ct)
    {
        var group = await _boService.CompleteTaskAsync(taskId, ct);
        _logger.LogInformation("BO: задание завершено. TaskId={TaskId}, GroupId={GroupId}", taskId, group.Id);
        return Ok(ApiResponse<SuspenseGroup>.Success(group, "Задание завершено, группа переведена в статус 88", "BO_COMPLETED"));
    }

    /// <summary>Поиск возможных продуктов для привязки к группе 120.</summary>
    [HttpGet("tasks/{taskId:int}/possible-products")]
    [RequirePermission(PermissionCodes.BackofficeLinkProduct)]
    public async Task<IActionResult> GetPossibleProducts(
        int taskId, [FromQuery] PagedRequest request, CancellationToken ct)
    {
        var result = await _boService.GetPossibleProductsAsync(taskId, request, ct);
        return Ok(ApiResponse<PagedResponse<CatalogProduct>>.Success(result));
    }

    /// <summary>Поиск прав в каталоге для копирования в группу 320.</summary>
    [HttpGet("tasks/{taskId:int}/search-rights")]
    [RequirePermission(PermissionCodes.BackofficeCopyRights)]
    public async Task<IActionResult> SearchRights(
        int taskId,
        [FromQuery] string? artist,
        [FromQuery] string? isrc,
        [FromQuery] string? productName,
        [FromQuery] string? barcode,
        [FromQuery] string? rightsTerritoryCode,
        [FromQuery] string? rightsDocNumber,
        [FromQuery] string? combineMode,
        CancellationToken ct)
    {
        var combineAnd = string.Equals(combineMode, "and", StringComparison.OrdinalIgnoreCase);
        var rights = await _boService.SearchCatalogRightsAsync(
            taskId, artist, isrc, productName, barcode,
            rightsTerritoryCode, rightsDocNumber, combineAnd, ct);
        return Ok(ApiResponse<List<CatalogProductRights>>.Success(rights));
    }

    /// <summary>
    /// Скопировать права с другого продукта каталога в продукт группы 320.
    /// При успехе → 88, задание закрывается.
    /// </summary>
    [HttpPost("tasks/{taskId:int}/copy-rights")]
    [RequirePermission(PermissionCodes.BackofficeCopyRights)]
    public async Task<IActionResult> CopyRights(int taskId, [FromBody] BoCopyRightsDto dto, CancellationToken ct)
    {
        var group = await _boService.CopyRightsToProductAsync(taskId, dto.RightsId, ct);
        _logger.LogInformation("BO: права скопированы, группа валидирована. TaskId={TaskId}", taskId);
        return Ok(ApiResponse<SuspenseGroup>.Success(group, "Права добавлены, группа переведена в статус 88", "BO_RIGHTS_COPIED"));
    }

    // ──────────────────────────────────────────────────────────────────────────
    // Редактирование метаданных и метаправ группы — через существующий сервис
    // (те же эндпоинты, что использует оператор, но из BO-контекста)
    // ──────────────────────────────────────────────────────────────────────────

    /// <summary>Обновить метаданные группы (доступно для 120 и 320).</summary>
    [HttpPut("tasks/{taskId:int}/groups/{groupId:int}/metadata")]
    [RequirePermission(PermissionCodes.BackofficeLinkProduct)]
    public async Task<IActionResult> UpdateMetadata(
        int taskId, int groupId, [FromBody] UpdateGroupMetadataDto dto, CancellationToken ct)
    {
        var task = await _boService.GetTaskAsync(taskId, ct);
        if (task.GroupId != groupId)
            return BadRequest(ApiResponse<object>.Fail(400,
                "Группа не принадлежит данному заданию", "GROUP_TASK_MISMATCH"));

        var meta = await _processingService.UpdateMetadataAsync(groupId, dto, ct);
        return Ok(ApiResponse<GroupMetadata>.Success(meta, "Метаданные обновлены", "METADATA_UPDATED"));
    }

    /// <summary>Обновить метаправа группы (доступно для 320).</summary>
    [HttpPut("tasks/{taskId:int}/groups/{groupId:int}/meta-rights")]
    [RequirePermission(PermissionCodes.BackofficeCopyRights)]
    public async Task<IActionResult> UpdateMetaRights(
        int taskId, int groupId, [FromBody] UpdateGroupMetaRightsDto dto, CancellationToken ct)
    {
        var task = await _boService.GetTaskAsync(taskId, ct);
        if (task.GroupId != groupId)
            return BadRequest(ApiResponse<object>.Fail(400,
                "Группа не принадлежит данному заданию", "GROUP_TASK_MISMATCH"));

        var rights = await _processingService.UpdateMetaRightsAsync(groupId, dto, ct);
        return Ok(ApiResponse<GroupMetaRights>.Success(rights, "Метаправа обновлены", "META_RIGHTS_UPDATED"));
    }
}
