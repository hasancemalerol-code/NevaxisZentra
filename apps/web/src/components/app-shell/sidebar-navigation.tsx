import Link from "next/link";
import { useCallback, useEffect, useRef, useState } from "react";
import { ChevronDown } from "lucide-react";

import { resolveNavigationIcon } from "@/lib/navigation/navigation-icon-registry";
import { resolveActiveWorkflowHome } from "@/lib/navigation/workflow-primary-navigation";
import type { SerializableNavigationGroup } from "@/lib/navigation/navigation-types";

type SidebarNavigationProps = Readonly<{
  pathname: string;
  collapsed: boolean;
  navigation: readonly SerializableNavigationGroup[];
}>;

function groupStorageKey(label: string): string {
  return `factos.nav.group.${label}`;
}

const PRIMARY_HOME_BY_HREF: Readonly<Record<string, string>> = {
  "/": "bugun",
  "/fabrika": "fabrika",
  "/planlama": "plan",
  "/isler": "isler",
  "/uygulamalar": "apps",
};

/**
 * The one active item of a group. The primary rail follows the workflow home of the current route; contextual groups
 * highlight the most specific matching item only, so two items are never "current" at once.
 */
function resolveGroupActiveHref(group: SerializableNavigationGroup, pathname: string): string | null {
  if (!group.homeId) {
    const home = resolveActiveWorkflowHome(pathname);
    // Module screens without their own sidebar entry live inside the Apps catalog, so the catalog home marks where the user is.
    const primary = group.items.find((item) => PRIMARY_HOME_BY_HREF[item.href] === home);
    if (primary) return primary.href;
  }
  let best: string | null = null;
  for (const item of group.items) {
    const matches = item.exact
      ? pathname === item.href
      : pathname === item.href ||
        (item.href !== "/" && pathname.startsWith(`${item.href}/`)) ||
        (item.matchPrefix !== undefined && (pathname === item.matchPrefix || pathname.startsWith(`${item.matchPrefix}/`)));
    if (matches && (best === null || item.href.length > best.length)) best = item.href;
  }
  return best;
}

export function SidebarNavigation({ pathname, collapsed, navigation }: SidebarNavigationProps) {
  const [collapsedGroups, setCollapsedGroups] = useState<Readonly<Record<string, boolean>>>({});

  useEffect(() => {
    const next: Record<string, boolean> = {};
    for (const group of navigation) {
      const stored = window.localStorage.getItem(groupStorageKey(group.label));
      next[group.label] = stored === "collapsed";
    }
    setCollapsedGroups(next);
  }, [navigation]);

  const toggleGroup = useCallback((label: string) => {
    setCollapsedGroups((current) => {
      const nextValue = !current[label];
      window.localStorage.setItem(groupStorageKey(label), nextValue ? "collapsed" : "expanded");
      return { ...current, [label]: nextValue };
    });
  }, []);

  const navRef = useRef<HTMLElement | null>(null);

  // The active item can sit below the fold of a short sidebar: keep it visible inside the scrollable list.
  useEffect(() => {
    const nav = navRef.current;
    const actives = nav ? Array.from(nav.querySelectorAll<HTMLElement>('[aria-current="page"]')) : [];
    // The contextual (most specific) item is the last current one; the primary rail item above it stays visible.
    const active = actives[actives.length - 1];
    if (!nav || !active) return;
    const navBox = nav.getBoundingClientRect();
    const itemBox = active.getBoundingClientRect();
    // Scroll by the smallest amount that reveals the item (with a margin), so the groups above stay in view.
    const margin = 24;
    if (itemBox.bottom > navBox.bottom - margin) {
      nav.scrollTop += itemBox.bottom - navBox.bottom + margin;
    } else if (itemBox.top < navBox.top + margin) {
      nav.scrollTop -= navBox.top + margin - itemBox.top;
    }
  }, [pathname, collapsed, collapsedGroups]);

  const contextActive = navigation.some((group) => Boolean(group.homeId) && resolveGroupActiveHref(group, pathname) !== null);

  return (
    <nav
      ref={navRef}
      aria-label="Birincil navigasyon"
      className="sidebar-navigation"
      data-context-active={contextActive}
      data-collapsed={collapsed}
    >
      {navigation.map((group, groupIndex) => {
        const groupLabelId = `navigation-group-${groupIndex}`;
        const activeHref = resolveGroupActiveHref(group, pathname);
        const hasActiveItem = activeHref !== null;
        const isGroupCollapsed = collapsed ? false : collapsedGroups[group.label] && !hasActiveItem;

        return (
          <section
            aria-labelledby={groupLabelId}
            className="navigation-group"
            data-collapsed={isGroupCollapsed}
            key={group.label}
          >
            <div className="navigation-group-heading">
              <p className="navigation-group-label" id={groupLabelId}>
                {group.label}
              </p>
              {!collapsed ? (
                <button
                  type="button"
                  className="navigation-group-toggle"
                  aria-expanded={!isGroupCollapsed}
                  aria-controls={`navigation-group-items-${groupIndex}`}
                  onClick={() => toggleGroup(group.label)}
                  title={isGroupCollapsed ? "Grubu genişlet" : "Grubu daralt"}
                >
                  <ChevronDown aria-hidden="true" data-collapsed={isGroupCollapsed} />
                </button>
              ) : null}
            </div>

            <ul
              className="navigation-list"
              id={`navigation-group-items-${groupIndex}`}
              hidden={isGroupCollapsed}
            >
              {group.items.map((item) => {
                const active = item.href === activeHref;
                const Icon = resolveNavigationIcon(item.iconKey);

                return (
                  <li key={item.href}>
                    <Link
                      aria-current={active ? "page" : undefined}
                      className="navigation-link"
                      data-active={active}
                      href={item.href}
                      title={collapsed ? item.label : undefined}
                    >
                      <Icon aria-hidden="true" />
                      <span className="navigation-label">{item.label}</span>
                    </Link>
                  </li>
                );
              })}
            </ul>
          </section>
        );
      })}
    </nav>
  );
}
