import Link from "next/link";
import {
  PanelLeftClose,
  PanelLeftOpen,
} from "lucide-react";

import { IconButton } from "@/components/ui/button";
import { resolveNavigationIcon } from "@/lib/navigation/navigation-icon-registry";
import { isNavigationItemActive } from "@/lib/navigation/navigation-types";
import type { SerializableNavigationGroup } from "@/lib/navigation/navigation-types";

type SidebarProps = Readonly<{
  pathname: string;
  collapsed: boolean;
  onToggle: () => void;
  navigation: readonly SerializableNavigationGroup[];
}>;

export function Sidebar({
  pathname,
  collapsed,
  onToggle,
  navigation,
}: SidebarProps) {
  return (
    <aside
      aria-label="Ana uygulama navigasyonu"
      className="app-sidebar"
      data-collapsed={collapsed}
      data-group-count={navigation.length}
    >
      <div className="sidebar-brand">
        <Link
          aria-label="CrevaFactOS ana çalışma alanı"
          className="brand-link"
          href="/"
        >
          <span
            aria-hidden="true"
            className="brand-mark"
          >
            CF
          </span>

          <span className="brand-copy">
            <strong>CrevaFactOS</strong>
            <small>Factory Operations</small>
          </span>
        </Link>

        <IconButton
          aria-expanded={!collapsed}
          className="sidebar-toggle"
          label={
            collapsed
              ? "Kenar çubuğunu genişlet"
              : "Kenar çubuğunu daralt"
          }
          onClick={onToggle}
          title={
            collapsed
              ? "Navigasyonu genişlet"
              : "Navigasyonu daralt"
          }
        >
          {collapsed ? (
            <PanelLeftOpen />
          ) : (
            <PanelLeftClose />
          )}
        </IconButton>
      </div>

      <nav
        aria-label="Birincil navigasyon"
        className="sidebar-navigation"
        data-collapsed={collapsed}
      >
        {navigation.map((group) => (
          <section
            aria-labelledby={`navigation-group-${group.label}`}
            className="navigation-group"
            key={group.label}
          >
            <p
              className="navigation-group-label"
              id={`navigation-group-${group.label}`}
            >
              {group.label}
            </p>

            <ul className="navigation-list">
              {group.items.map((item) => {
                const active =
                  isNavigationItemActive(
                    item,
                    pathname,
                  );
                const Icon = resolveNavigationIcon(item.iconKey);

                return (
                  <li key={item.href}>
                    <Link
                      aria-current={
                        active
                          ? "page"
                          : undefined
                      }
                      className="navigation-link"
                      data-active={active}
                      href={item.href}
                      title={
                        collapsed
                          ? item.label
                          : undefined
                      }
                    >
                      <Icon aria-hidden="true" />

                      <span className="navigation-label">
                        {item.label}
                      </span>
                    </Link>
                  </li>
                );
              })}
            </ul>
          </section>
        ))}
      </nav>

      <div className="sidebar-footer">
        <span
          aria-hidden="true"
          className="environment-dot"
        />

        <span className="sidebar-footer-copy">
          <strong>UI Foundation</strong>
          <small>Operasyon katmanı</small>
        </span>
      </div>
    </aside>
  );
}