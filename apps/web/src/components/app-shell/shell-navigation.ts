import type { SerializableNavigationGroup } from "@/lib/navigation/navigation-types";

import type { ShellSearchItem } from "./shell-types";

export function buildShellSearchItems(
  groups: readonly SerializableNavigationGroup[],
): readonly ShellSearchItem[] {
  return groups.flatMap((group) =>
    group.items.map((item) => ({
      label: item.label,
      href: item.href,
      groupLabel: group.label,
    })),
  );
}

export function resolveSettingsHref(
  groups: readonly SerializableNavigationGroup[],
): string | null {
  for (const group of groups) {
    for (const item of group.items) {
      if (item.href === "/ayarlar") {
        return item.href;
      }
    }
  }

  return null;
}
