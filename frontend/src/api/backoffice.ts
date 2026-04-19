import { apiGet, apiPost, apiPut } from './client';
import type { SearchCatalogRightsParams } from './processing';
import type {
  BackOfficeTaskDto,
  BackOfficeTask,
  PagedResponse,
  PagedRequest,
  SuspenseGroup,
  GroupMetadata,
  GroupMetaRights,
  UpdateGroupMetadataDto,
  UpdateGroupMetaRightsDto,
  CatalogProduct,
  CatalogProductRights,
} from '../types';

export async function getBoTasks(params: {
  pageNumber?: number;
  pageSize?: number;
  taskType?: number | null;
}): Promise<PagedResponse<BackOfficeTaskDto>> {
  return apiGet<PagedResponse<BackOfficeTaskDto>>('/backoffice/tasks', params as Record<string, unknown>);
}

export async function getBoTask(taskId: number): Promise<BackOfficeTask> {
  return apiGet<BackOfficeTask>(`/backoffice/tasks/${taskId}`);
}

export async function boReturnGroup(taskId: number): Promise<SuspenseGroup> {
  return apiPost<SuspenseGroup>(`/backoffice/tasks/${taskId}/return`);
}

export async function boDeleteGroup(taskId: number): Promise<void> {
  await apiPost<object>(`/backoffice/tasks/${taskId}/delete-group`);
}

export async function boLinkProduct(taskId: number, productId: number): Promise<SuspenseGroup> {
  return apiPost<SuspenseGroup>(`/backoffice/tasks/${taskId}/link-product`, { productId });
}

export async function boCompleteTask(taskId: number): Promise<SuspenseGroup> {
  return apiPost<SuspenseGroup>(`/backoffice/tasks/${taskId}/complete`);
}

export async function boGetPossibleProducts(
  taskId: number,
  params: PagedRequest
): Promise<PagedResponse<CatalogProduct>> {
  return apiGet<PagedResponse<CatalogProduct>>(
    `/backoffice/tasks/${taskId}/possible-products`,
    params as Record<string, unknown>
  );
}

export async function boSearchRights(
  taskId: number,
  params: SearchCatalogRightsParams
): Promise<CatalogProductRights[]> {
  return apiGet<CatalogProductRights[]>(
    `/backoffice/tasks/${taskId}/search-rights`,
    params as Record<string, unknown>
  );
}

export async function boCopyRights(taskId: number, rightsId: number): Promise<SuspenseGroup> {
  return apiPost<SuspenseGroup>(`/backoffice/tasks/${taskId}/copy-rights`, { rightsId });
}

export async function boUpdateMetadata(
  taskId: number,
  groupId: number,
  dto: UpdateGroupMetadataDto
): Promise<GroupMetadata> {
  return apiPut<GroupMetadata>(`/backoffice/tasks/${taskId}/groups/${groupId}/metadata`, dto);
}

export async function boUpdateMetaRights(
  taskId: number,
  groupId: number,
  dto: UpdateGroupMetaRightsDto
): Promise<GroupMetaRights> {
  return apiPut<GroupMetaRights>(`/backoffice/tasks/${taskId}/groups/${groupId}/meta-rights`, dto);
}
