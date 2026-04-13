import { apiGet } from './client';
import type { PagedResponse, SuspenseGroup, SuspenseLine, PagedRequest } from '../types';

export async function getNoProductGroups(params: PagedRequest): Promise<PagedResponse<SuspenseGroup>> {
  return apiGet<PagedResponse<SuspenseGroup>>('/group/no-product', params as Record<string, unknown>);
}

export async function getNoRightsGroups(params: PagedRequest): Promise<PagedResponse<SuspenseGroup>> {
  return apiGet<PagedResponse<SuspenseGroup>>('/group/no-rights', params as Record<string, unknown>);
}

export async function getSavedGroups(params: PagedRequest): Promise<PagedResponse<SuspenseGroup>> {
  return apiGet<PagedResponse<SuspenseGroup>>('/group/saved', params as Record<string, unknown>);
}

export async function getGroupById(id: number): Promise<SuspenseGroup> {
  return apiGet<SuspenseGroup>(`/group/${id}`);
}

export async function getGroupSuspenses(
  id: number,
  params: PagedRequest
): Promise<PagedResponse<SuspenseLine>> {
  return apiGet<PagedResponse<SuspenseLine>>(`/group/${id}/suspenses`, params as Record<string, unknown>);
}

export async function getPostponedGroups(params: PagedRequest): Promise<PagedResponse<SuspenseGroup>> {
  return apiGet<PagedResponse<SuspenseGroup>>('/postponed', params as Record<string, unknown>);
}
