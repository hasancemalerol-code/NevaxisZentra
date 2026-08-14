export function hasDashboardPermission(
  grantedPermissionKeys: readonly string[],
  permissionKey: string | undefined,
): boolean {
  if (!permissionKey) {
    return true;
  }

  return grantedPermissionKeys.includes(permissionKey);
}
