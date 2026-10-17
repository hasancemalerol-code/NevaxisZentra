"use client";

import { Suspense, useCallback, useEffect, useState, type ReactNode } from "react";
import { usePathname } from "next/navigation";

import { FactoryCopilotShell } from "@/features/factory-copilot/components/factory-copilot-shell";
import { Sidebar } from "./sidebar";
import { TopBar } from "./top-bar";
import type { ShellSearchItem } from "./shell-types";
import { resolveActiveWorkflowHome } from "@/lib/navigation/workflow-primary-navigation";
import type { SerializableNavigationGroup } from "@/lib/navigation/navigation-types";
import { DateFieldEnhancer } from "./date-field-enhancer";
import { ResponsiveTables } from "./responsive-tables";
import { SessionExpiryGuard } from "./session-expiry-guard";

type AppShellFrameProps = Readonly<{
  children: ReactNode;
  navigation: readonly SerializableNavigationGroup[];
  accountDisplayName?: string | null;
  emailMasked?: string | null;
  organizationLabel?: string | null;
  searchItems?: readonly ShellSearchItem[];
  settingsHref?: string | null;
  subscriptionGraceWarning?: string | null;
  notificationsSlot?: ReactNode;
  canAccessFactoryCopilot?: boolean;
}>;

export function AppShellFrame({
  children,
  navigation,
  accountDisplayName = null,
  emailMasked = null,
  organizationLabel = null,
  searchItems = [],
  settingsHref = null,
  subscriptionGraceWarning = null,
  notificationsSlot = null,
  canAccessFactoryCopilot = false,
}: AppShellFrameProps) {
  const pathname = usePathname();
  const [collapsed, setCollapsed] = useState(false);
  // Narrow screens (tablet and phone) use an off-canvas drawer with full labels instead of an icon-only rail.
  const [narrow, setNarrow] = useState(false);
  const [drawerOpen, setDrawerOpen] = useState(false);
  const activeHome = resolveActiveWorkflowHome(pathname);
  const visibleNavigation = navigation.filter((group) => !group.homeId || group.homeId === activeHome);

  useEffect(() => {
    const query = window.matchMedia("(max-width: 1099px)");
    const sync = () => {
      setNarrow(query.matches);
      if (!query.matches) setDrawerOpen(false);
    };
    sync();
    query.addEventListener("change", sync);
    return () => query.removeEventListener("change", sync);
  }, []);

  useEffect(() => {
    setDrawerOpen(false);
  }, [pathname]);

  useEffect(() => {
    if (!drawerOpen) return;
    const onKey = (event: KeyboardEvent) => {
      if (event.key === "Escape") setDrawerOpen(false);
    };
    document.addEventListener("keydown", onKey);
    return () => document.removeEventListener("keydown", onKey);
  }, [drawerOpen]);

  const toggleDrawer = useCallback(() => setDrawerOpen((current) => !current), []);

  function toggleSidebar() {
    if (narrow) {
      setDrawerOpen(false);
      return;
    }
    setCollapsed((current) => !current);
  }

  return (
    <div className="app-shell" data-sidebar-collapsed={collapsed && !narrow} data-drawer-open={narrow && drawerOpen}>
      <a className="skip-link" href="#main-content">Ana içeriğe geç</a>
      <SessionExpiryGuard />
      <ResponsiveTables />
      <DateFieldEnhancer />
      {narrow && drawerOpen ? (
        <button type="button" className="app-shell-backdrop" aria-label="Menüyü kapat" onClick={() => setDrawerOpen(false)} />
      ) : null}
      <Sidebar pathname={pathname} collapsed={collapsed && !narrow} onToggle={toggleSidebar} navigation={visibleNavigation} />
      <div className="app-workspace">
        <TopBar
          menuOpen={narrow && drawerOpen}
          onMenuToggle={toggleDrawer}
          accountDisplayName={accountDisplayName}
          emailMasked={emailMasked}
          organizationLabel={organizationLabel}
          searchItems={searchItems}
          settingsHref={settingsHref}
          notificationsSlot={notificationsSlot}
        />
        <main className="content-viewport" id="main-content" tabIndex={-1}>
          {subscriptionGraceWarning ? (
            <div className="app-shell-subscription-banner" role="status">
              {subscriptionGraceWarning}
            </div>
          ) : null}
          <Suspense fallback={children}>
            <FactoryCopilotShell canAccessCopilot={canAccessFactoryCopilot}>{children}</FactoryCopilotShell>
          </Suspense>
        </main>
      </div>
    </div>
  );
}
