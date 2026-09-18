import Link from "next/link";
import { useCallback, useEffect, useState } from "react";
import { ChevronDown } from "lucide-react";

import { resolveNavigationIcon } from "@/lib/navigation/navigation-icon-registry";
import { isNavigationItemActive } from "@/lib/navigation/navigation-types";
import type { SerializableNavigationGroup } from "@/lib/navigation/navigation-types";

type SidebarNavigationProps = Readonly<{
  pathname: string;
  collapsed: boolean;
  navigation: readonly SerializableNavigationGroup[];
}>;

function groupStorageKey(label: string): string {
  return `factos.nav.group.${label}`;
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

  return (
    <nav
      aria-label="Birincil navigasyon"
      className="sidebar-navigation"
      data-collapsed={collapsed}
    >
      {navigation.map((group, groupIndex) => {
        const groupLabelId = `navigation-group-${groupIndex}`;
        const hasActiveItem = group.items.some((item) => isNavigationItemActive(item, pathname));
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
                const active = isNavigationItemActive(item, pathname);
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
