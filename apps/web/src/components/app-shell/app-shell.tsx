"use client";

import { useState, type ReactNode } from "react";
import { usePathname } from "next/navigation";

import { Sidebar } from "./sidebar";
import { TopBar } from "./top-bar";

type AppShellFrameProps = Readonly<{
  children: ReactNode;
  pathname: string;
  collapsed: boolean;
  onToggle: () => void;
}>;

export function AppShellFrame({ children, pathname, collapsed, onToggle }: AppShellFrameProps) {
  return (
    <div className="app-shell" data-sidebar-collapsed={collapsed}>
      <a className="skip-link" href="#main-content">Ana içeriğe geç</a>
      <Sidebar pathname={pathname} collapsed={collapsed} onToggle={onToggle} />
      <div className="app-workspace">
        <TopBar />
        <main className="content-viewport" id="main-content" tabIndex={-1}>{children}</main>
      </div>
    </div>
  );
}

export function AppShell({ children }: Readonly<{ children: ReactNode }>) {
  const pathname = usePathname();
  const [collapsed, setCollapsed] = useState(false);

  function toggleSidebar() {
    setCollapsed((current) => !current);
  }

  return (
    <AppShellFrame pathname={pathname} collapsed={collapsed} onToggle={toggleSidebar}>
      {children}
    </AppShellFrame>
  );
}
