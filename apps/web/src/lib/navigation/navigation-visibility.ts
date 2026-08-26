import { fetchCurrentEffectiveAccess, hasGrantedPermission } from "@/features/admin-access/server/admin-access-effective-access";
import {
  isModuleEntitled,
  resolveEntitlementCodeFromPath,
} from "@/features/saas-subscription/domain/saas-subscription-labels";
import { fetchCurrentTenantSubscription } from "@/features/saas-subscription/server/saas-subscription-queries";
import { primaryNavigation } from "@/lib/navigation/primary-navigation";
import type {
  SerializableNavigationGroup,
  SerializableNavigationItem,
} from "@/lib/navigation/navigation-types";

function toSerializableItem(item: SerializableNavigationItem): SerializableNavigationItem {
  return {
    label: item.label,
    href: item.href,
    iconKey: item.iconKey,
    ...(item.exact ? { exact: true } : {}),
    ...(item.permissionKey ? { permissionKey: item.permissionKey } : {}),
  };
}

function hasSubscriptionModuleAccess(
  modulePath: string | undefined,
  subscription: Awaited<ReturnType<typeof fetchCurrentTenantSubscription>>,
): boolean {
  if (!modulePath) {
    return true;
  }

  if (subscription.status !== "success") {
    return true;
  }

  const entitlementCode = resolveEntitlementCodeFromPath(modulePath);
  if (!entitlementCode) {
    return true;
  }

  return isModuleEntitled(subscription.value, entitlementCode);
}

export async function resolvePermissionAwareNavigation(): Promise<readonly SerializableNavigationGroup[]> {
  const [access, subscription] = await Promise.all([
    fetchCurrentEffectiveAccess(),
    fetchCurrentTenantSubscription(),
  ]);

  return primaryNavigation
    .map((group) => ({
      label: group.label,
      items: group.items
        .filter((item) => {
          if (item.permissionKey && !hasGrantedPermission(access, item.permissionKey)) {
            return false;
          }

          const modulePath = item.subscriptionModulePath ?? item.href;
          if (item.subscriptionModulePath || resolveEntitlementCodeFromPath(item.href)) {
            return hasSubscriptionModuleAccess(modulePath, subscription);
          }

          return true;
        })
        .map(toSerializableItem),
    }))
    .filter((group) => group.items.length > 0);
}

export type { ShellSearchItem } from "@/components/app-shell/shell-types";
export { buildShellSearchItems, resolveSettingsHref } from "@/components/app-shell/shell-navigation";

export function flattenNavigationItems(
  groups: readonly SerializableNavigationGroup[],
): readonly SerializableNavigationItem[] {
  return groups.flatMap((group) => group.items);
}
