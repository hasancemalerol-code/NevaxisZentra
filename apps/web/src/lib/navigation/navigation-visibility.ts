import { fetchEffectiveAccessCapabilities } from "@/features/admin-access/server/admin-access-effective-access";
import { isSubscriptionExemptCustomerWorkflowPath } from "@/lib/authorization/acceptance-role-contract";
import {
  isModuleEntitled,
  resolveEntitlementCodeFromPath,
} from "@/features/saas-subscription/domain/saas-subscription-labels";
import { fetchCurrentTenantSubscription } from "@/features/saas-subscription/server/saas-subscription-queries";
import { appsCatalogNavigation } from "@/lib/navigation/apps-catalog-navigation";
import type {
  SerializableNavigationGroup,
  SerializableNavigationItem,
} from "@/lib/navigation/navigation-types";
import {
  resolveContextualNavigationSection,
  WORKFLOW_PRIMARY_HOMES,
} from "@/lib/navigation/workflow-primary-navigation";

function toSerializableItem(item: SerializableNavigationItem): SerializableNavigationItem {
  return {
    label: item.label,
    href: item.href,
    iconKey: item.iconKey,
    ...(item.exact ? { exact: true } : {}),
    ...(item.permissionKey ? { permissionKey: item.permissionKey } : {}),
    ...(item.subscriptionModulePath ? { subscriptionModulePath: item.subscriptionModulePath } : {}),
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

export function isNavigationItemVisible(
  item: SerializableNavigationItem,
  grantedPermissionKeys: readonly string[],
  subscription: Awaited<ReturnType<typeof fetchCurrentTenantSubscription>>,
): boolean {
  if (item.permissionKey && !grantedPermissionKeys.includes(item.permissionKey)) {
    return false;
  }

  if (isSubscriptionExemptCustomerWorkflowPath(item.href)) {
    return true;
  }

  const modulePath = item.subscriptionModulePath ?? item.href;
  if (item.subscriptionModulePath || resolveEntitlementCodeFromPath(item.href)) {
    return hasSubscriptionModuleAccess(modulePath, subscription);
  }

  return true;
}

function filterNavigationItems(
  items: readonly SerializableNavigationItem[],
  access: Awaited<ReturnType<typeof fetchEffectiveAccessCapabilities>>,
  subscription: Awaited<ReturnType<typeof fetchCurrentTenantSubscription>>,
): readonly SerializableNavigationItem[] {
  const grantedPermissionKeys = access?.grantedPermissionKeys ?? [];
  return items
    .filter((item) => isNavigationItemVisible(item, grantedPermissionKeys, subscription))
    .map(toSerializableItem);
}

export async function resolvePermissionAwareNavigation(
  pathname: string,
): Promise<readonly SerializableNavigationGroup[]> {
  const [access, subscription] = await Promise.all([
    fetchEffectiveAccessCapabilities(),
    fetchCurrentTenantSubscription(),
  ]);

  const primaryItems = filterNavigationItems(WORKFLOW_PRIMARY_HOMES, access, subscription);
  const groups: SerializableNavigationGroup[] = [
    {
      label: "Çalışma Alanı",
      items: primaryItems,
    },
  ];

  const contextualSection = resolveContextualNavigationSection(pathname);
  if (contextualSection) {
    const contextualItems = filterNavigationItems(
      contextualSection.items,
      access,
      subscription,
    );

    if (contextualItems.length > 0) {
      groups.push({
        label: contextualSection.label,
        items: contextualItems,
      });
    }
  }

  return groups;
}

export async function resolvePermissionAwareAppsCatalog(): Promise<readonly SerializableNavigationGroup[]> {
  const [access, subscription] = await Promise.all([
    fetchEffectiveAccessCapabilities(),
    fetchCurrentTenantSubscription(),
  ]);

  return appsCatalogNavigation
    .map((group) => ({
      label: group.label,
      items: filterNavigationItems(group.items, access, subscription),
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
