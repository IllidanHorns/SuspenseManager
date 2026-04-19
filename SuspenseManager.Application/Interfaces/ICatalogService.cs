using Common.DTOs;
using Models;

namespace Application.Interfaces;

public interface ICatalogService
{
    // ── Продукты ────────────────────────────────────────────────────────────────
    Task<PagedResponse<CatalogProduct>> GetProductsAsync(PagedRequest request, CancellationToken ct = default);
    Task<CatalogProduct> GetProductByIdAsync(int id, CancellationToken ct = default);
    Task<CatalogProduct> CreateProductAsync(CreateCatalogProductDto dto, CancellationToken ct = default);
    Task<CatalogProduct> UpdateProductAsync(int id, UpdateCatalogProductDto dto, CancellationToken ct = default);

    // ── Права на продукт ────────────────────────────────────────────────────────
    Task<PagedResponse<CatalogProductRights>> GetRightsAsync(PagedRequest request, int? productId, CancellationToken ct = default);
    Task<CatalogProductRights> CreateRightsAsync(CreateCatalogProductRightsDto dto, CancellationToken ct = default);
    Task<CatalogProductRights> UpdateRightsAsync(int id, UpdateCatalogProductRightsDto dto, CancellationToken ct = default);
}
