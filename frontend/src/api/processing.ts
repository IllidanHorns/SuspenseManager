import { apiGet, apiPost, apiPut, apiGetBlob } from './client';
import type {
  GroupMetadata,
  GroupMetaRights,
  UpdateGroupMetadataDto,
  UpdateGroupMetaRightsDto,
  SuspenseGroup,
  CatalogProduct,
  CatalogProductRights,
  PagedResponse,
  PagedRequest,
} from '../types';

export async function getMetadata(groupId: number): Promise<GroupMetadata | null> {
  try {
    return await apiGet<GroupMetadata>(`/groups/${groupId}/metadata`);
  } catch {
    return null;
  }
}

export async function updateMetadata(
  groupId: number,
  dto: UpdateGroupMetadataDto
): Promise<GroupMetadata> {
  return apiPut<GroupMetadata>(`/groups/${groupId}/metadata`, dto);
}

export async function getMetaRights(groupId: number): Promise<GroupMetaRights | null> {
  try {
    return await apiGet<GroupMetaRights>(`/groups/${groupId}/meta-rights`);
  } catch {
    return null;
  }
}

export async function updateMetaRights(
  groupId: number,
  dto: UpdateGroupMetaRightsDto
): Promise<GroupMetaRights> {
  return apiPut<GroupMetaRights>(`/groups/${groupId}/meta-rights`, dto);
}

export async function catalogFast(groupId: number): Promise<CatalogProduct> {
  return apiPost<CatalogProduct>(`/groups/${groupId}/catalog-fast`);
}

export async function getPossibleProducts(
  groupId: number,
  params: PagedRequest
): Promise<PagedResponse<CatalogProduct>> {
  return apiGet<PagedResponse<CatalogProduct>>(
    `/groups/${groupId}/possible-products`,
    params as Record<string, unknown>
  );
}

export async function linkProduct(groupId: number, productId: number): Promise<SuspenseGroup> {
  return apiPost<SuspenseGroup>(`/groups/${groupId}/link-product`, { productId });
}

export async function sendToBackOffice(groupId: number, problemDescription: string): Promise<SuspenseGroup> {
  return apiPost<SuspenseGroup>(`/groups/${groupId}/send-to-backoffice`, { problemDescription });
}

export async function postponeGroup(groupId: number, reason?: string): Promise<SuspenseGroup> {
  return apiPost<SuspenseGroup>(`/groups/${groupId}/postpone`, { reason });
}

export async function ungroupGroup(groupId: number): Promise<{ groupId: number }> {
  return apiPost<{ groupId: number }>(`/groups/${groupId}/ungroup`);
}

export async function returnFromPostponed(groupId: number): Promise<SuspenseGroup> {
  return apiPost<SuspenseGroup>(`/postponed/${groupId}/return`);
}

export async function exportGroupSuspenses(groupId: number): Promise<Blob> {
  return apiGetBlob(`/groups/${groupId}/export-suspenses`);
}

export async function exportGroupsByStatus(status: 15 | 16): Promise<Blob> {
  return apiGetBlob('/groups/export', { status });
}

export async function createGroupRights(groupId: number): Promise<SuspenseGroup> {
  return apiPost<SuspenseGroup>(`/groups/${groupId}/create-rights`);
}

/** Параметры GET /groups/{id}/search-rights */
export interface SearchCatalogRightsParams {
  artist?: string;
  isrc?: string;
  productName?: string;
  barcode?: string;
  rightsTerritoryCode?: string;
  rightsDocNumber?: string;
  /** and — все непустые поля продукта одновременно; or — любое (по умолчанию). */
  combineMode?: 'and' | 'or';
}

export async function searchCatalogRights(
  groupId: number,
  params: SearchCatalogRightsParams
): Promise<CatalogProductRights[]> {
  return apiGet<CatalogProductRights[]>(
    `/groups/${groupId}/search-rights`,
    params as Record<string, unknown>
  );
}

export async function copyRightsToProduct(
  groupId: number,
  rightsId: number
): Promise<SuspenseGroup> {
  return apiPost<SuspenseGroup>(`/groups/${groupId}/copy-rights`, { rightsId });
}
