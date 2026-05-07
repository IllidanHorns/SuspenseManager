import { apiGet } from './client';

export interface OperatorMonitorDto {
  accountId: number;
  login: string;
  fullName?: string;
  activeGroupsCount: number;
  postponedGroupsCount: number;
  backOfficeGroupsCount: number;
  totalSuspensesCount: number;
  oldestGroupAgeDays: number;
  warningGroupsCount: number;
  criticalGroupsCount: number;
}

export interface OperatorGroupDto {
  groupId: number;
  businessStatus: number;
  suspenseCount: number;
  ageDays: number;
  daysSinceLastActivity: number;
  lastActivityTime: string;
  flagLevel: 'none' | 'warning' | 'critical';
  flagReason?: string;
  artist?: string;
  title?: string;
  isrc?: string;
}

export function getOperatorSummary(): Promise<OperatorMonitorDto[]> {
  return apiGet<OperatorMonitorDto[]>('/monitor/operators');
}

export function getOperatorGroups(accountId: number): Promise<OperatorGroupDto[]> {
  return apiGet<OperatorGroupDto[]>(`/monitor/operators/${accountId}/groups`);
}
