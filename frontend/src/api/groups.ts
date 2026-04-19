import { apiGet } from './client';
import type { PagedResponse, SuspenseGroup, SuspenseLine, PagedRequest } from '../types';

export interface GroupListRequest {
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortDirection?: 'asc' | 'desc';
  /** Только группы, созданные текущим аккаунтом */
  onlyMine?: boolean;
  artist?: string;
  isrc?: string;
  barcode?: string;
  title?: string;
  territoryCode?: string;
  docNumber?: string;
  countMin?: number;
  countMax?: number;
  revenueMin?: number;
  revenueMax?: number;
}

export async function getNoProductGroups(params: GroupListRequest): Promise<PagedResponse<SuspenseGroup>> {
  return apiGet<PagedResponse<SuspenseGroup>>('/group/no-product', params as Record<string, unknown>);
}

export async function getNoRightsGroups(params: GroupListRequest): Promise<PagedResponse<SuspenseGroup>> {
  return apiGet<PagedResponse<SuspenseGroup>>('/group/no-rights', params as Record<string, unknown>);
}

export async function getSavedGroups(params: GroupListRequest): Promise<PagedResponse<SuspenseGroup>> {
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

export async function getPostponedGroups(params: GroupListRequest): Promise<PagedResponse<SuspenseGroup>> {
  return apiGet<PagedResponse<SuspenseGroup>>('/postponed', params as Record<string, unknown>);
}
