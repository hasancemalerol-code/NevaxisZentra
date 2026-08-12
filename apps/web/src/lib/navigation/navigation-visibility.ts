import { resolveMaterialNavigationVisibility } from "@/features/materials/server/material-reader";
import { resolveMoldNavigationVisibility } from "@/features/molds/server/mold-reader";
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
  const [moldVisible, materialVisible] = await Promise.all([
    resolveMoldNavigationVisibility(),
    resolveMaterialNavigationVisibility(),
  ]);

  return primaryNavigation.map((group) => ({
    label: group.label,
    items: group.items
      .filter((item) => {
        if (item.permissionKey === "mold.read") {
          return moldVisible;
        }

        if (item.permissionKey === "material.read") {
          return materialVisible;
        }

        return true;
      })
      .map(toSerializableItem),
  }));
}

export function flattenNavigationItems(
  groups: readonly SerializableNavigationGroup[],
): readonly SerializableNavigationItem[] {
  return groups.flatMap((group) => group.items);
}
