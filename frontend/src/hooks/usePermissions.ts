import { useMemo } from 'react';
import { useAuth } from './useAuth';
import {
  canAccessAdmin,
  canAccessAdminPermissions,
  canAccessAdminUsers,
  canAccessAudit,
  canAccessBackoffice,
  canAccessCatalog,
  canAccessDashboard,
  canAccessGroupDetail,
  canAccessGrouping,
  canAccessPostponed,
  canAccessSavedGroups,
  canAccessSuspenses,
  canAccessUpload,
  canCommitGrouping,
  canCreateUpload,
  canPreviewGrouping,
  getDefaultAuthorizedPath,
  hasAllPermissions,
  hasAnyPermission,
  hasPermission,
} from '../utils/permissions';

export function usePermissions() {
  const { permissions } = useAuth();

  return useMemo(
    () => ({
      permissions,
      hasPermission: (permission: string) => hasPermission(permissions, permission),
      hasAnyPermission: (required: readonly string[]) => hasAnyPermission(permissions, required),
      hasAllPermissions: (required: readonly string[]) => hasAllPermissions(permissions, required),
      canAccessDashboard: canAccessDashboard(permissions),
      canAccessUpload: canAccessUpload(permissions),
      canCreateUpload: canCreateUpload(permissions),
      canAccessGrouping: canAccessGrouping(permissions),
      canPreviewGrouping: canPreviewGrouping(permissions),
      canCommitGrouping: canCommitGrouping(permissions),
      canAccessSavedGroups: canAccessSavedGroups(permissions),
      canAccessGroupDetail: canAccessGroupDetail(permissions),
      canAccessPostponed: canAccessPostponed(permissions),
      canAccessSuspenses: canAccessSuspenses(permissions),
      canAccessAudit: canAccessAudit(permissions),
      canAccessBackoffice: canAccessBackoffice(permissions),
      canAccessCatalog: canAccessCatalog(permissions),
      canAccessAdmin: canAccessAdmin(permissions),
      canAccessAdminUsers: canAccessAdminUsers(permissions),
      canAccessAdminPermissions: canAccessAdminPermissions(permissions),
      defaultAuthorizedPath: getDefaultAuthorizedPath(permissions),
    }),
    [permissions]
  );
}
