export const PermissionCodes = {
  uploadsView: 'uploads.view',
  uploadsCreate: 'uploads.create',

  groupingView: 'grouping.view',
  groupingCreate: 'grouping.create',

  groupsNoProductView: 'groups.no_product.view',
  groupsNoProductCatalogFast: 'groups.no_product.catalog_fast',
  groupsNoProductPossibleProducts: 'groups.no_product.possible_products',
  groupsNoProductSendBackoffice: 'groups.no_product.send_backoffice',
  groupsNoProductPostpone: 'groups.no_product.postpone',
  groupsNoProductUngroup: 'groups.no_product.ungroup',

  groupsNoRightsView: 'groups.no_rights.view',
  groupsNoRightsCorrectRights: 'groups.no_rights.correct_rights',
  groupsNoRightsSendBackoffice: 'groups.no_rights.send_backoffice',
  groupsNoRightsPostpone: 'groups.no_rights.postpone',
  groupsNoRightsUngroup: 'groups.no_rights.ungroup',

  postponedView: 'postponed.view',
  postponedReturn: 'postponed.return',

  backofficeView: 'backoffice.view',
  backofficeReturn: 'backoffice.return',
  backofficeValidate: 'backoffice.validate',
  backofficeDelete: 'backoffice.delete',
  backofficeLinkProduct: 'backoffice.link_product',
  backofficeCopyRights: 'backoffice.copy_rights',

  catalogView: 'catalog.view',
  catalogProductsEdit: 'catalog.products.edit',
  catalogRightsEdit: 'catalog.rights.edit',
  catalogCompaniesEdit: 'catalog.companies.edit',
  catalogTerritoriesEdit: 'catalog.territories.edit',

  groupsExport: 'groups.export',

  adminUsersManage: 'admin.users.manage',
  adminPermissionsManage: 'admin.permissions.manage',
} as const;

export type PermissionCode = (typeof PermissionCodes)[keyof typeof PermissionCodes];

export const AnyWorkAccessPermissions: PermissionCode[] = [
  PermissionCodes.uploadsView,
  PermissionCodes.groupingView,
  PermissionCodes.groupsNoProductView,
  PermissionCodes.groupsNoRightsView,
  PermissionCodes.postponedView,
  PermissionCodes.backofficeView,
  PermissionCodes.catalogView,
  PermissionCodes.adminUsersManage,
  PermissionCodes.adminPermissionsManage,
];

export function hasPermission(permissions: string[], permission: string): boolean {
  return permissions.includes(permission);
}

export function hasAnyPermission(permissions: string[], required: readonly string[]): boolean {
  return required.some((permission) => permissions.includes(permission));
}

export function hasAllPermissions(permissions: string[], required: readonly string[]): boolean {
  return required.every((permission) => permissions.includes(permission));
}

export function canAccessDashboard(permissions: string[]): boolean {
  return hasAnyPermission(permissions, AnyWorkAccessPermissions);
}

export function canAccessUpload(permissions: string[]): boolean {
  return hasAnyPermission(permissions, [PermissionCodes.uploadsView, PermissionCodes.uploadsCreate]);
}

export function canCreateUpload(permissions: string[]): boolean {
  return hasPermission(permissions, PermissionCodes.uploadsCreate);
}

export function canAccessGrouping(permissions: string[]): boolean {
  return hasAnyPermission(permissions, [PermissionCodes.groupingView, PermissionCodes.groupingCreate]);
}

export function canPreviewGrouping(permissions: string[]): boolean {
  return hasPermission(permissions, PermissionCodes.groupingView);
}

export function canCommitGrouping(permissions: string[]): boolean {
  return hasPermission(permissions, PermissionCodes.groupingCreate);
}

export function canAccessSavedGroups(permissions: string[]): boolean {
  return hasAnyPermission(permissions, [PermissionCodes.groupsNoProductView, PermissionCodes.groupsNoRightsView]);
}

export function canAccessGroupDetail(permissions: string[]): boolean {
  return canAccessSavedGroups(permissions) || canAccessPostponed(permissions);
}

export function canAccessPostponed(permissions: string[]): boolean {
  return hasPermission(permissions, PermissionCodes.postponedView);
}

export function canAccessSuspenses(permissions: string[]): boolean {
  return hasAnyPermission(permissions, AnyWorkAccessPermissions);
}

export function canAccessAudit(permissions: string[]): boolean {
  return hasAnyPermission(permissions, AnyWorkAccessPermissions);
}

export function canAccessBackoffice(permissions: string[]): boolean {
  return hasPermission(permissions, PermissionCodes.backofficeView);
}

export function canAccessCatalog(permissions: string[]): boolean {
  return hasAnyPermission(permissions, [
    PermissionCodes.catalogView,
    PermissionCodes.catalogProductsEdit,
    PermissionCodes.catalogRightsEdit,
    PermissionCodes.catalogCompaniesEdit,
    PermissionCodes.catalogTerritoriesEdit,
  ]);
}

export function canAccessAdminUsers(permissions: string[]): boolean {
  return hasPermission(permissions, PermissionCodes.adminUsersManage);
}

export function canAccessAdminPermissions(permissions: string[]): boolean {
  return hasPermission(permissions, PermissionCodes.adminPermissionsManage);
}

export function canAccessAdmin(permissions: string[]): boolean {
  return canAccessAdminUsers(permissions) || canAccessAdminPermissions(permissions);
}

export function getDefaultAuthorizedPath(permissions: string[]): string {
  if (canAccessDashboard(permissions)) return '/';
  if (canAccessUpload(permissions)) return '/upload';
  if (canAccessGrouping(permissions)) return '/grouping';
  if (canAccessSavedGroups(permissions)) return '/groups';
  if (canAccessPostponed(permissions)) return '/postponed';
  if (canAccessSuspenses(permissions)) return '/suspenses';
  if (canAccessAudit(permissions)) return '/audit';
  if (canAccessBackoffice(permissions)) return '/backoffice/tasks';
  if (canAccessCatalog(permissions)) return '/catalog';
  if (canAccessAdmin(permissions)) return '/admin';
  return '/settings';
}
