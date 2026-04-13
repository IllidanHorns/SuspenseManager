import { apiGet, apiPost } from './client';
import type { PagedResponse, GroupingPreviewItem, GroupingCommitRequest, SuspenseGroup } from '../types';

export interface GroupingPreviewParams {
  businessStatus: number;
  groupByColumns: string[];
  pageNumber?: number;
  pageSize?: number;
  sortBy?: string;
  sortDirection?: string;
  [key: string]: unknown;
}

export async function getGroupingPreview(
  params: GroupingPreviewParams
): Promise<PagedResponse<GroupingPreviewItem>> {
  // groupByColumns is an array — serialize as repeated query params
  const { groupByColumns, ...rest } = params;
  const qs = new URLSearchParams();
  Object.entries(rest).forEach(([k, v]) => {
    if (v !== undefined && v !== null && v !== '') qs.append(k, String(v));
  });
  groupByColumns.forEach((col) => qs.append('groupByColumns', col));

  const res = await fetch(`/api/grouping/preview?${qs.toString()}`, {
    headers: { Authorization: `Bearer ${localStorage.getItem('sm_access_token') ?? ''}` },
  });
  const json = await res.json();
  if (!json.data) throw new Error(json.message || 'Ошибка предпросмотра');
  return json.data;
}

export async function commitGroup(dto: GroupingCommitRequest): Promise<SuspenseGroup> {
  return apiPost<SuspenseGroup>('/grouping/commit', dto);
}
