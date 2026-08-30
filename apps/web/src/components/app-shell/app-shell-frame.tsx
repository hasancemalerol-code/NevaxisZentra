"use client";

import { useState, type ReactNode } from "react";
import { usePathname } from "next/navigation";

import { Sidebar } from "./sidebar";
import { TopBar } from "./top-bar";
import type { ShellSearchItem } from "./shell-types";
import type { SerializableNavigationGroup } from "@/lib/navigation/navigation-types";

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
}: AppShellFrameProps) {
  const pathname = usePathname();
  const [collapsed, setCollapsed] = useState(false);

  function toggleSidebar() {
    setCollapsed((current) => !current);
  }

  return (
    <div className="app-shell" data-sidebar-collapsed={collapsed}>
      <a className="skip-link" href="#main-content">Ana içeriğe geç</a>
      <Sidebar pathname={pathname} collapsed={collapsed} onToggle={toggleSidebar} navigation={navigation} />
      <div className="app-workspace">
        <TopBar
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
          {children}
        </main>
      </div>
    </div>
  );
}
