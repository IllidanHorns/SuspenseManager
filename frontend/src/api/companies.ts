import { apiGet } from './client';
import type { PagedResponse, Company } from '../types';

export async function getCompanies(params?: { pageSize?: number }): Promise<PagedResponse<Company>> {
  return apiGet<PagedResponse<Company>>('/company', { pageSize: 200, ...params } as Record<string, unknown>);
}
