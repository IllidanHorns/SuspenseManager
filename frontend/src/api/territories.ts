import { apiGet } from './client';
import type { PagedResponse, Territory } from '../types';

export async function getTerritories(): Promise<PagedResponse<Territory>> {
  return apiGet<PagedResponse<Territory>>('/territory', { pageSize: 500 } as Record<string, unknown>);
}
