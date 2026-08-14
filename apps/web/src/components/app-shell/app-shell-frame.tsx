"use client";

import { useState, type ReactNode } from "react";
import { usePathname } from "next/navigation";

import { Sidebar } from "./sidebar";
import { TopBar } from "./top-bar";
import type { SerializableNavigationGroup } from "@/lib/navigation/navigation-types";

type AppShellFrameProps = Readonly<{
  children: ReactNode;
  navigation: readonly SerializableNavigationGroup[];
  accountDisplayName?: string | null;
}>;

export function AppShellFrame({
  children,
  navigation,
  accountDisplayName = null,
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
        <TopBar accountDisplayName={accountDisplayName} />
        <main className="content-viewport" id="main-content" tabIndex={-1}>{children}</main>
      </div>
    </div>
  );
}
