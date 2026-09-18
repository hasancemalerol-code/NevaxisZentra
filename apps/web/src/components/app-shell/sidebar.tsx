import Link from "next/link";
import {
  PanelLeftClose,
  PanelLeftOpen,
} from "lucide-react";

import { ZentraBrandLockup } from "@/components/brand/zentra-brand-lockup";
import { IconButton } from "@/components/ui/button";
import { SidebarNavigation } from "@/components/app-shell/sidebar-navigation";
import { ZENTRA_BRAND } from "@/lib/brand/zentra-brand";
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
          aria-label={`${ZENTRA_BRAND.productName} ${ZENTRA_BRAND.endorsement} ana çalışma alanı`}
          className="brand-link"
          href="/"
        >
          <ZentraBrandLockup className="brand-lockup" />
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
            <PanelLeftOpen aria-hidden="true" />
          ) : (
            <PanelLeftClose aria-hidden="true" />
          )}
        </IconButton>
      </div>

      <SidebarNavigation
        pathname={pathname}
        collapsed={collapsed}
        navigation={navigation}
      />

      <div className="sidebar-footer">
        <span
          aria-hidden="true"
          className="environment-dot"
        />

        <span className="sidebar-footer-copy">
          <strong>Zentra</strong>
          <small>Endüstriyel operasyon</small>
        </span>
      </div>
    </aside>
  );
}
