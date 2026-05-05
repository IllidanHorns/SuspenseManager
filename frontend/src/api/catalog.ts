import { apiGet, apiPost, apiPut } from './client';
import type {
  CatalogProduct,
  CatalogProductType,
  CatalogProductRights,
  Company,
  Territory,
  PagedResponse,
  PagedRequest,
  CreateCatalogProductDto,
  UpdateCatalogProductDto,
  CreateCatalogProductRightsDto,
  UpdateCatalogProductRightsDto,
  CreateCompanyDto,
  UpdateCompanyDto,
  CreateTerritoryDto,
  UpdateTerritoryDto,
} from '../types';

// ── Типы продуктов ────────────────────────────────────────────────────────────

export async function getCatalogProductTypes(): Promise<CatalogProductType[]> {
  const res = await apiGet<CatalogProductType[]>('/catalog/product-types');
  return res;
}

// ── Продукты ──────────────────────────────────────────────────────────────────

export async function getCatalogProducts(params: PagedRequest): Promise<PagedResponse<CatalogProduct>> {
  return apiGet<PagedResponse<CatalogProduct>>('/catalog/products', params as Record<string, unknown>);
}

export async function getCatalogProduct(id: number): Promise<CatalogProduct> {
  return apiGet<CatalogProduct>(`/catalog/products/${id}`);
}

export async function createCatalogProduct(dto: CreateCatalogProductDto): Promise<CatalogProduct> {
  return apiPost<CatalogProduct>('/catalog/products', dto);
}

export async function updateCatalogProduct(id: number, dto: UpdateCatalogProductDto): Promise<CatalogProduct> {
  return apiPut<CatalogProduct>(`/catalog/products/${id}`, dto);
}

// ── Права на продукт ──────────────────────────────────────────────────────────

export async function getCatalogRights(params: PagedRequest & { productId?: number; Filters?: Record<string, string> }): Promise<PagedResponse<CatalogProductRights>> {
  return apiGet<PagedResponse<CatalogProductRights>>('/catalog/rights', params as Record<string, unknown>);
}

export async function createCatalogRights(dto: CreateCatalogProductRightsDto): Promise<CatalogProductRights> {
  return apiPost<CatalogProductRights>('/catalog/rights', dto);
}

export async function updateCatalogRights(id: number, dto: UpdateCatalogProductRightsDto): Promise<CatalogProductRights> {
  return apiPut<CatalogProductRights>(`/catalog/rights/${id}`, dto);
}

// ── Компании ──────────────────────────────────────────────────────────────────

export async function getCatalogCompanies(params?: { pageSize?: number }): Promise<PagedResponse<Company>> {
  return apiGet<PagedResponse<Company>>('/company', { pageSize: 200, ...params } as Record<string, unknown>);
}

export async function createCompany(dto: CreateCompanyDto): Promise<Company> {
  return apiPost<Company>('/company', dto);
}

export async function updateCompany(id: number, dto: UpdateCompanyDto): Promise<Company> {
  return apiPut<Company>(`/company/${id}`, dto);
}

// ── Территории ────────────────────────────────────────────────────────────────

export async function getCatalogTerritories(): Promise<PagedResponse<Territory>> {
  return apiGet<PagedResponse<Territory>>('/territory', { pageSize: 500 } as Record<string, unknown>);
}

export async function createTerritory(dto: CreateTerritoryDto): Promise<Territory> {
  return apiPost<Territory>('/territory', dto);
}

export async function updateTerritory(id: number, dto: UpdateTerritoryDto): Promise<Territory> {
  return apiPut<Territory>(`/territory/${id}`, dto);
}
