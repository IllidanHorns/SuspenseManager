/** Коды прав, дающие доступ к разделу администрирования (прототип). */
export const ADMIN_PERMISSION_CODES = ['admin.users.manage', 'admin.permissions.manage'] as const;

export function hasAdminAccess(permissions: string[]): boolean {
  return permissions.some((p) =>
    ADMIN_PERMISSION_CODES.includes(p as (typeof ADMIN_PERMISSION_CODES)[number])
  );
}
