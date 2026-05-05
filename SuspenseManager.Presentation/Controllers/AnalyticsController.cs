using Application.Interfaces;
using Common.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace SuspenseManager.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    public AnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    /// <summary>
    /// Дашборд аналитики. Опциональные параметры <c>dateFrom</c> / <c>dateTo</c> (YYYY-MM-DD) фильтруют
    /// суспенсы по дате создания. Без них возвращаются данные за всё время.
    /// </summary>
    [HttpGet("dashboard")]
    public async Task<IActionResult> GetDashboard(
        [FromQuery] DateTime? dateFrom,
        [FromQuery] DateTime? dateTo,
        CancellationToken ct)
    {
        var result = await _analyticsService.GetDashboardAsync(dateFrom, dateTo, ct);
        return Ok(ApiResponse<DashboardDto>.Success(result));
    }
}
