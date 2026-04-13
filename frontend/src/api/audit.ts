import { apiGet } from './client';
import type {
  PagedResponse,
  AuditGroupDto,
  AuditLineDto,
  AuditLogEntryDto,
  AuditGroupsRequest,
  AuditLinesRequest,
  PagedRequest,
} from '../types';

export async function getAuditGroups(
  params: AuditGroupsRequest
): Promise<PagedResponse<AuditGroupDto>> {
  return apiGet<PagedResponse<AuditGroupDto>>('/audit/groups', params as Record<string, unknown>);
}

export async function getAuditGroupLogs(
  groupId: number,
  params: PagedRequest
): Promise<PagedResponse<AuditLogEntryDto>> {
  return apiGet<PagedResponse<AuditLogEntryDto>>(
    `/audit/groups/${groupId}/logs`,
    params as Record<string, unknown>
  );
}

export async function getAuditLines(
  params: AuditLinesRequest
): Promise<PagedResponse<AuditLineDto>> {
  return apiGet<PagedResponse<AuditLineDto>>('/audit/suspense-lines', params as Record<string, unknown>);
}

export async function getAuditLineLogs(
  lineId: number,
  params: PagedRequest
): Promise<PagedResponse<AuditLogEntryDto>> {
  return apiGet<PagedResponse<AuditLogEntryDto>>(
    `/audit/suspense-lines/${lineId}/logs`,
    params as Record<string, unknown>
  );
}
