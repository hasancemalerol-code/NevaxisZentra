export const NAVIGATION_ICON_KEYS = [
  "list-todo",
  "layout-dashboard",
  "messages-square",
  "shopping-basket",
  "calendar-range",
  "factory",
  "ship",
  "calendar-clock",
  "cpu",
  "boxes",
  "molds",
  "package",
  "wrench",
  "shield-check",
  "zap",
  "cable",
  "package-search",
  "triangle-alert",
  "circle-percent",
  "gauge",
  "file-warning",
  "badge-dollar-sign",
  "users-round",
  "circle-gauge",
  "chart-no-axes-combined",
  "activity",
  "settings",
] as const;

export type NavigationIconKey = (typeof NAVIGATION_ICON_KEYS)[number];

export type SerializableNavigationItem = Readonly<{
  label: string;
  href: string;
  iconKey: NavigationIconKey;
  exact?: boolean;
  permissionKey?: string;
  subscriptionModulePath?: string;
}>;

export type SerializableNavigationGroup = Readonly<{
  label: string;
  items: readonly SerializableNavigationItem[];
}>;

export function isNavigationItemActive(
  item: Pick<SerializableNavigationItem, "href" | "exact">,
  pathname: string,
) {
  return item.exact
    ? pathname === item.href
    : pathname === item.href || pathname.startsWith(`${item.href}/`);
}
