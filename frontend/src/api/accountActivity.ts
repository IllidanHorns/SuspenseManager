import { apiGet } from './client';
import type { PagedRequest, PagedResponse } from '../types';

export interface AccountActivityItem {
  kind: 'group' | 'line';
  logId: number;
  entityId: number;
  groupId: number | null;
  statusFrom: number | null;
  statusFromName: string | null;
  statusTo: number;
  statusToName: string | null;
  operationTime: string;
}

export async function getAccountActivity(
  accountId: number,
  params: PagedRequest
): Promise<PagedResponse<AccountActivityItem>> {
  return apiGet<PagedResponse<AccountActivityItem>>(
    `/audit/accounts/${accountId}/activity`,
    params as Record<string, unknown>
  );
}
