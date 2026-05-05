using Application.Interfaces;
using Common.DTOs;
using Microsoft.AspNetCore.Mvc;
using Models;

namespace SuspenseManager.Controllers;

/// <summary>
/// Управление каталогом продуктов и прав — доступно только сотрудникам бэк-офиса.
/// Удаление не предусмотрено: используется мягкое удаление только через другие процессы.
/// </summary>
[ApiController]
[Route("api/catalog")]
public class CatalogController : ControllerBase
{
    private readonly ICatalogService _catalogService;
    private readonly ILogger<CatalogController> _logger;

    public CatalogController(ICatalogService catalogService, ILogger<CatalogController> logger)
    {
        _catalogService = catalogService;
        _logger = logger;
    }

    // ── Типы продуктов ──────────────────────────────────────────────────────────

    /// <summary>Справочник типов продуктов (CD, VINYL, DIGI, CASS). Используется для выпадающего списка.</summary>
    [HttpGet("product-types")]
    public async Task<IActionResult> GetProductTypes(CancellationToken ct)
    {
        var types = await _catalogService.GetProductTypesAsync(ct);
        return Ok(ApiResponse<List<Models.CatalogProductType>>.Success(types));
    }

    // ── Продукты ────────────────────────────────────────────────────────────────

    /// <summary>Список продуктов каталога с пагинацией. Фильтр <c>Filters[IdentityIncomplete]=true</c> — только продукты без полного набора полей (название, исполнитель, ISRC, баркод, каталожный номер).</summary>
    [HttpGet("products")]
    public async Task<IActionResult> GetProducts([FromQuery] PagedRequest request, CancellationToken ct)
    {
        var result = await _catalogService.GetProductsAsync(request, ct);
        return Ok(ApiResponse<PagedResponse<CatalogProduct>>.Success(result));
    }

    /// <summary>Продукт по ID со всеми правами.</summary>
    [HttpGet("products/{id:int}")]
    public async Task<IActionResult> GetProduct(int id, CancellationToken ct)
    {
        var product = await _catalogService.GetProductByIdAsync(id, ct);
        return Ok(ApiResponse<CatalogProduct>.Success(product));
    }

    /// <summary>Создать новый продукт каталога.</summary>
    [HttpPost("products")]
    public async Task<IActionResult> CreateProduct([FromBody] CreateCatalogProductDto dto, CancellationToken ct)
    {
        var product = await _catalogService.CreateProductAsync(dto, ct);
        _logger.LogInformation("Продукт каталога создан: ProductId={ProductId}", product.Id);
        return StatusCode(201, ApiResponse<CatalogProduct>.Created(product, "Продукт создан", "PRODUCT_CREATED"));
    }

    /// <summary>Обновить продукт каталога (частичное обновление — передаются только изменяемые поля).</summary>
    [HttpPut("products/{id:int}")]
    public async Task<IActionResult> UpdateProduct(int id, [FromBody] UpdateCatalogProductDto dto, CancellationToken ct)
    {
        var product = await _catalogService.UpdateProductAsync(id, dto, ct);
        _logger.LogInformation("Продукт каталога обновлён: ProductId={ProductId}", product.Id);
        return Ok(ApiResponse<CatalogProduct>.Success(product, "Продукт обновлён", "PRODUCT_UPDATED"));
    }

    // ── Права на продукт ────────────────────────────────────────────────────────

    /// <summary>Список прав. Опциональный фильтр productId — права конкретного продукта. Фильтр <c>Filters[RightsIncomplete]=true</c> — только записи с незаполненными обязательными полями.</summary>
    [HttpGet("rights")]
    public async Task<IActionResult> GetRights(
        [FromQuery] PagedRequest request,
        [FromQuery] int? productId,
        CancellationToken ct)
    {
        var result = await _catalogService.GetRightsAsync(request, productId, ct);
        return Ok(ApiResponse<PagedResponse<CatalogProductRights>>.Success(result));
    }

    /// <summary>Добавить права на продукт каталога.</summary>
    [HttpPost("rights")]
    public async Task<IActionResult> CreateRights([FromBody] CreateCatalogProductRightsDto dto, CancellationToken ct)
    {
        var rights = await _catalogService.CreateRightsAsync(dto, ct);
        _logger.LogInformation("Права добавлены: RightsId={RightsId}, ProductId={ProductId}", rights.Id, rights.CatalogProductId);
        return StatusCode(201, ApiResponse<CatalogProductRights>.Created(rights, "Права добавлены", "RIGHTS_CREATED"));
    }

    /// <summary>Обновить запись прав (частичное обновление).</summary>
    [HttpPut("rights/{id:int}")]
    public async Task<IActionResult> UpdateRights(int id, [FromBody] UpdateCatalogProductRightsDto dto, CancellationToken ct)
    {
        var rights = await _catalogService.UpdateRightsAsync(id, dto, ct);
        _logger.LogInformation("Права обновлены: RightsId={RightsId}", rights.Id);
        return Ok(ApiResponse<CatalogProductRights>.Success(rights, "Права обновлены", "RIGHTS_UPDATED"));
    }
}
