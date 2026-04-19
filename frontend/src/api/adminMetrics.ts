import { apiGet } from './client';

export interface AdminMetricsDto {
  usersTotal: number;
  accountsTotal: number;
  accountsWithoutUserProfile: number;
  usersWithoutAccount: number;
  rightsTotal: number;
}

export async function getAdminMetrics(): Promise<AdminMetricsDto> {
  return apiGet<AdminMetricsDto>('/admin/metrics');
}
