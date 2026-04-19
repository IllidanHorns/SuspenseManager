import { apiDelete, apiGet } from './client';
import type { PagedResponse, SuspenseLine, PagedRequest } from '../types';

export type SuspenseListParams = PagedRequest & {
  /** Только строки в группах, созданных текущим аккаунтом (режим «Все») */
  onlyMine?: boolean;
};

export async function getSuspenses(params: SuspenseListParams): Promise<PagedResponse<SuspenseLine>> {
  return apiGet<PagedResponse<SuspenseLine>>('/suspense', params as Record<string, unknown>);
}

export async function getUngroupedSuspenses(
  status: 0 | 1,
  params: PagedRequest
): Promise<PagedResponse<SuspenseLine>> {
  return apiGet<PagedResponse<SuspenseLine>>('/suspense/ungrouped', {
    status,
    ...params,
  } as Record<string, unknown>);
}

export async function getSuspenseById(id: number): Promise<SuspenseLine> {
  return apiGet<SuspenseLine>(`/suspense/${id}`);
}

export async function archiveSuspense(id: number): Promise<{ id: number }> {
  return apiDelete<{ id: number }>(`/suspense/${id}`);
}
