using Application.Interfaces;
using Common.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace SuspenseManager.Controllers;

/// <summary>
/// Контроллер обработки групп: метаданные, каталогизация, привязка продукта,
/// бэк-офис, откладывание, разгруппировка, экспорт, отложенные
/// </summary>
[ApiController]
[Route("api/groups")]
public class GroupProcessingController : ControllerBase
{
    private readonly IGroupProcessingService _processingService;
    private readonly IExcelExportService _excelExportService;
    private readonly ILogger<GroupProcessingController> _logger;

    public GroupProcessingController(
        IGroupProcessingService processingService,
        IExcelExportService excelExportService,
        ILogger<GroupProcessingController> logger)
    {
        _processingService = processingService;
        _excelExportService = excelExportService;
        _logger = logger;
    }

    // --- п.22 Выгрузка метаданных группы ---

    [HttpGet("{groupId:int}/metadata")]
    public async Task<IActionResult> GetMetadata(int groupId, CancellationToken ct)
    {
        var meta = await _processingService.GetMetadataAsync(groupId, ct);
        if (meta == null)
        {
            return Ok(ApiResponse<object>.Success(null!, "Метаданные не заданы"));
        }

        return Ok(ApiResponse<Models.GroupMetadata>.Success(meta));
    }

    // --- п.23 Выгрузка метаправ группы ---

    [HttpGet("{groupId:int}/meta-rights")]
    public async Task<IActionResult> GetMetaRights(int groupId, CancellationToken ct)
    {
        var metaRights = await _processingService.GetMetaRightsAsync(groupId, ct);
        if (metaRights == null)
        {
            return Ok(ApiResponse<object>.Success(null!, "Метаправа не заданы"));
        }

        return Ok(ApiResponse<Models.GroupMetaRights>.Success(metaRights));
    }

    // --- п.14 Обновление метаданных продукта (нет продукта) ---

    [HttpPut("{groupId:int}/metadata")]
    public async Task<IActionResult> UpdateMetadata(int groupId, [FromBody] UpdateGroupMetadataDto dto, CancellationToken ct)
    {
        var meta = await _processingService.UpdateMetadataAsync(groupId, dto, ct);
        _logger.LogInformation("Метаданные обновлены: GroupId={GroupId}", groupId);
        return Ok(ApiResponse<Models.GroupMetadata>.Success(meta, "Метаданные обновлены", "METADATA_UPDATED"));
    }

    // --- п.15 Обновление метаправ (нет прав) ---

    [HttpPut("{groupId:int}/meta-rights")]
    public async Task<IActionResult> UpdateMetaRights(int groupId, [FromBody] UpdateGroupMetaRightsDto dto, CancellationToken ct)
    {
        var metaRights = await _processingService.UpdateMetaRightsAsync(groupId, dto, ct);
        _logger.LogInformation("Метаправа обновлены: GroupId={GroupId}", groupId);
        return Ok(ApiResponse<Models.GroupMetaRights>.Success(metaRights, "Метаправа обновлены", "META_RIGHTS_UPDATED"));
    }

    // --- п.16 Быстрая каталогизация ---

    /// <summary>
    /// Быстрая каталогизация — создаёт продукт из данных группы (метаданные > первый суспенс).
    /// Группа переводится в статус 16 (нет прав). Body не требуется.
    /// </summary>
    [HttpPost("{groupId:int}/catalog-fast")]
    public async Task<IActionResult> QuickCatalog(int groupId, CancellationToken ct)
    {
        var product = await _processingService.QuickCatalogAsync(groupId, ct);
        _logger.LogInformation("Быстрая каталогизация: GroupId={GroupId}, ProductId={ProductId}", groupId, product.Id);
        return StatusCode(201, ApiResponse<Models.CatalogProduct>.Created(product, "Продукт создан, группа переведена в статус 'нет прав'", "CATALOG_FAST_DONE"));
    }

    // --- п.17 Возможные продукты ---

    [HttpGet("{groupId:int}/possible-products")]
    public async Task<IActionResult> GetPossibleProducts(int groupId, [FromQuery] PagedRequest request, CancellationToken ct)
    {
        var result = await _processingService.GetPossibleProductsAsync(groupId, request, ct);
        return Ok(ApiResponse<PagedResponse<Models.CatalogProduct>>.Success(result));
    }

    // --- п.25 Привязка группы к продукту ---

    [HttpPost("{groupId:int}/link-product")]
    public async Task<IActionResult> LinkProduct(int groupId, [FromBody] LinkProductDto dto, CancellationToken ct)
    {
        var group = await _processingService.LinkProductAsync(groupId, dto, ct);
        _logger.LogInformation("Продукт привязан: GroupId={GroupId}, ProductId={ProductId}", groupId, dto.ProductId);
        return Ok(ApiResponse<Models.SuspenseGroup>.Success(group, "Продукт привязан, группа переведена в статус 'нет прав'", "PRODUCT_LINKED"));
    }

    // --- п.20 Отправка в бэк-офис ---

    [HttpPost("{groupId:int}/send-to-backoffice")]
    public async Task<IActionResult> SendToBackOffice(int groupId, [FromBody] SendToBackOfficeDto dto, CancellationToken ct)
    {
        var group = await _processingService.SendToBackOfficeAsync(groupId, dto, ct);
        _logger.LogInformation("Группа отправлена в бэк-офис: GroupId={GroupId}", groupId);
        return Ok(ApiResponse<Models.SuspenseGroup>.Success(group, "Группа передана в бэк-офис", "SENT_TO_BACKOFFICE"));
    }

    // --- п.21 Отложить ---

    [HttpPost("{groupId:int}/postpone")]
    public async Task<IActionResult> Postpone(int groupId, [FromBody] PostponeGroupDto dto, CancellationToken ct)
    {
        var group = await _processingService.PostponeAsync(groupId, dto, ct);
        _logger.LogInformation("Группа отложена: GroupId={GroupId}", groupId);
        return Ok(ApiResponse<Models.SuspenseGroup>.Success(group, "Группа отложена", "POSTPONED"));
    }

    // --- п.24 Разгруппировка ---

    [HttpPost("{groupId:int}/ungroup")]
    public async Task<IActionResult> Ungroup(int groupId, CancellationToken ct)
    {
        await _processingService.UngroupAsync(groupId, ct);
        _logger.LogInformation("Группа разгруппирована: GroupId={GroupId}", groupId);
        return Ok(ApiResponse<object>.Success(new { groupId }, "Группа разгруппирована", "UNGROUPED"));
    }

    // --- п.19 Экспорт суспенсов группы в Excel ---

    [HttpGet("{groupId:int}/export-suspenses")]
    public async Task<IActionResult> ExportGroupSuspenses(int groupId, CancellationToken ct)
    {
        var bytes = await _excelExportService.ExportGroupSuspensesAsync(groupId, ct);
        var fileName = $"Suspenses_Group_{groupId}_{DateTime.UtcNow:yyyy-MM-dd}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    // --- п.18 Экспорт всех групп в Excel ---

    [HttpGet("export")]
    public async Task<IActionResult> ExportGroups([FromQuery] int status, CancellationToken ct)
    {
        var bytes = await _excelExportService.ExportGroupsAsync(status, ct);
        var fileName = $"Groups_Status_{status}_{DateTime.UtcNow:yyyy-MM-dd}.xlsx";
        return File(bytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
    }

    // --- Отложенные группы ---

    [HttpGet("/api/postponed")]
    public async Task<IActionResult> GetPostponed([FromQuery] PagedRequest request, CancellationToken ct)
    {
        var result = await _processingService.GetPostponedGroupsAsync(request, ct);
        return Ok(ApiResponse<PagedResponse<Models.SuspenseGroup>>.Success(result));
    }

    [HttpPost("/api/postponed/{groupId:int}/return")]
    public async Task<IActionResult> ReturnFromPostponed(int groupId, CancellationToken ct)
    {
        var group = await _processingService.ReturnFromPostponedAsync(groupId, ct);
        _logger.LogInformation("Группа возвращена из отложенных: GroupId={GroupId}", groupId);
        return Ok(ApiResponse<Models.SuspenseGroup>.Success(group, "Группа возвращена в обработку", "RETURNED_FROM_POSTPONED"));
    }
}
