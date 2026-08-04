import Link from "next/link";
import { PanelLeftClose, PanelLeftOpen } from "lucide-react";

import { IconButton } from "@/components/ui/button";
import { primaryNavigation, isNavigationItemActive } from "@/lib/navigation/primary-navigation";

type SidebarProps = Readonly<{
  pathname: string;
  collapsed: boolean;
  onToggle: () => void;
}>;

export function Sidebar({ pathname, collapsed, onToggle }: SidebarProps) {
  return (
    <aside className="app-sidebar" aria-label="Ana uygulama navigasyonu" data-collapsed={collapsed}>
      <div className="sidebar-brand">
        <Link className="brand-link" href="/" aria-label="CrevaFactOS ana çalışma alanı">
          <span className="brand-mark" aria-hidden="true">CF</span>
          <span className="brand-copy">
            <strong>CrevaFactOS</strong>
            <small>Factory Operations</small>
          </span>
        </Link>
        <IconButton
          className="sidebar-toggle"
          label={collapsed ? "Kenar çubuğunu genişlet" : "Kenar çubuğunu daralt"}
          onClick={onToggle}
          aria-expanded={!collapsed}
          title={collapsed ? "Navigasyonu genişlet" : "Navigasyonu daralt"}
        >
          {collapsed ? <PanelLeftOpen /> : <PanelLeftClose />}
        </IconButton>
      </div>

      <nav className="sidebar-navigation" aria-label="Birincil navigasyon">
        {primaryNavigation.map((group) => (
          <div className="navigation-group" key={group.label}>
            <p className="navigation-group-label">{group.label}</p>
            <ul className="navigation-list">
              {group.items.map((item) => {
                const active = isNavigationItemActive(item, pathname);
                const Icon = item.icon;
                return (
                  <li key={item.href}>
                    <Link
                      className="navigation-link"
                      href={item.href}
                      aria-current={active ? "page" : undefined}
                      title={collapsed ? item.label : undefined}
                    >
                      <Icon aria-hidden="true" />
                      <span className="navigation-label">{item.label}</span>
                    </Link>
                  </li>
                );
              })}
            </ul>
          </div>
        ))}
      </nav>

      <div className="sidebar-footer">
        <span className="environment-dot" aria-hidden="true" />
        <span className="sidebar-footer-copy">
          <strong>UI Foundation</strong>
          <small>Operasyon katmanı</small>
        </span>
      </div>
    </aside>
  );
}
