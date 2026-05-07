using System.Security.Claims;
using Application.Interfaces;
using Common.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SuspenseManager.Middleware;

namespace SuspenseManager.Controllers;

/// <summary>
/// Динамическая группировка суспенсов: предпросмотр и фиксация групп.
/// Позволяет сгруппировать суспенсы по произвольному набору столбцов,
/// просмотреть результат и зафиксировать выбранную группу.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]
[Produces("application/json")]
public class GroupingController : ControllerBase
{
    private readonly IGroupingService _groupingService;

    public GroupingController(IGroupingService groupingService)
    {
        _groupingService = groupingService;
    }

    /// <summary>
    /// Предпросмотр динамической группировки суспенсов.
    /// </summary>
    /// <remarks>
    /// Выполняет GROUP BY по выбранным столбцам с поддержкой фильтрации, сортировки и пагинации.
    ///
    /// **Статус 0 (нет продукта):** все столбцы берутся из SuspenseLine.
    /// Допустимые: Isrc, Barcode, CatalogNumber, Artist, TrackTitle, Genre,
    /// SenderCompany, RecipientCompany, Operator, AgreementType, AgreementNumber, TerritoryCode.
    ///
    /// **Статус 1 (нет прав):** продуктовые поля берутся из CatalogProduct, остальные из SuspenseLine.
    /// ProductId — **обязателен**.
    /// Допустимые: ProductId, Isrc, Barcode, CatalogNumber, ProductName, Artist,
    /// SenderCompany, RecipientCompany, Operator, AgreementType, AgreementNumber, TerritoryCode.
    ///
    /// **Фильтрация:** через Filters — поддерживает суффиксы _contains, _gt, _lt, _gte, _lte, _from, _to.
    ///
    /// **Сортировка:** SortBy = имя столбца или "Count", SortDirection = asc/desc.
    ///
    /// Пример запроса:
    /// `GET /api/grouping/preview?BusinessStatus=0&amp;GroupByColumns=Isrc&amp;GroupByColumns=Artist&amp;SortBy=Count&amp;SortDirection=desc`
    /// </remarks>
    /// <param name="request">Параметры группировки, фильтрации, сортировки и пагинации</param>
    /// <param name="ct">Токен отмены</param>
    /// <returns>Страница сгруппированных записей с количеством в каждой группе</returns>
    /// <response code="200">Группировка выполнена успешно</response>
    /// <response code="400">Невалидный запрос (неверный статус, столбец, отсутствует ProductId для статуса 1)</response>
    [HttpGet("preview")]
    [RequirePermission(PermissionCodes.GroupingView)]
    [ProducesResponseType(typeof(ApiResponse<PagedResponse<GroupingPreviewItem>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Preview([FromQuery] GroupingPreviewRequest request, CancellationToken ct)
    {
        // Читаем произвольные query-параметры как фильтры (все кроме известных системных)
        var knownParams = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "pageNumber", "pageSize", "sortBy", "sortDirection",
            "businessStatus", "groupByColumns",
            "countMin", "countMax", "revenueMin", "revenueMax"
        };

        var filters = HttpContext.Request.Query
            .Where(kv => !knownParams.Contains(kv.Key) && !string.IsNullOrEmpty(kv.Value))
            .ToDictionary(kv => kv.Key + "_contains", kv => kv.Value.ToString());

        if (filters.Count > 0)
            request.Filters = filters;

        var result = await _groupingService.PreviewAsync(request, ct);
        return Ok(ApiResponse<PagedResponse<GroupingPreviewItem>>.Success(result, "Группировка выполнена"));
    }

    /// <summary>
    /// Предпросмотр строк суспенса внутри потенциальной группы (без фиксации).
    /// </summary>
    [HttpPost("preview-lines")]
    [RequirePermission(PermissionCodes.GroupingView)]
    public async Task<IActionResult> PreviewLines([FromBody] GroupLinesPreviewRequest request, CancellationToken ct)
    {
        var result = await _groupingService.PreviewLinesAsync(request, ct);
        return Ok(ApiResponse<PagedResponse<SuspenseLinePreviewDto>>.Success(result, "Строки группы"));
    }

    [HttpPost("export-lines")]
    [RequirePermission(PermissionCodes.GroupsExport)]
    public async Task<IActionResult> ExportLines([FromBody] GroupLinesPreviewRequest request, CancellationToken ct)
    {
        var bytes = await _groupingService.ExportPreviewLinesAsync(request, ct);
        return File(bytes,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "suspenses_preview.xlsx");
    }

    [HttpPost("commit")]
    [RequirePermission(PermissionCodes.GroupingCreate)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ApiResponse<object>), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Commit([FromBody] GroupingCommitRequest request, CancellationToken ct)
    {
        var value = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("account_id");
        if (int.TryParse(value, out var accountId))
            request.AccountId = accountId;

        var group = await _groupingService.CommitAsync(request, ct);
        return Ok(ApiResponse<object>.Success(new
        {
            group.Id,
            group.BusinessStatus,
            group.AccountId,
            group.CatalogProductId,
            group.CreateTime
        }, "Группа зафиксирована"));
    }
}
