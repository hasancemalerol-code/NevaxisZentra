import { fetchCurrentEffectiveAccess, hasGrantedPermission } from "@/features/admin-access/server/admin-access-effective-access";
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

export async function resolvePermissionAwareNavigation(): Promise<readonly SerializableNavigationGroup[]> {
  const access = await fetchCurrentEffectiveAccess();

  return primaryNavigation.map((group) => ({
    label: group.label,
    items: group.items
      .filter((item) => {
        if (!item.permissionKey) {
          return true;
        }

        return hasGrantedPermission(access, item.permissionKey);
      })
      .map(toSerializableItem),
  }));
}

export type { ShellSearchItem } from "@/components/app-shell/shell-types";
export { buildShellSearchItems, resolveSettingsHref } from "@/components/app-shell/shell-navigation";

export function flattenNavigationItems(
  groups: readonly SerializableNavigationGroup[],
): readonly SerializableNavigationItem[] {
  return groups.flatMap((group) => group.items);
}
