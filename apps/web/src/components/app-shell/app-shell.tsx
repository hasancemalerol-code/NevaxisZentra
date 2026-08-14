"use client";

import type { ReactNode } from "react";

import { primaryNavigation } from "@/lib/navigation/primary-navigation";
import { buildShellSearchItems, resolveSettingsHref } from "./shell-navigation";
import { AppShellFrame } from "./app-shell-frame";

export function AppShell({ children }: Readonly<{ children: ReactNode }>) {
  return (
    <AppShellFrame
      navigation={primaryNavigation}
      searchItems={buildShellSearchItems(primaryNavigation)}
      settingsHref={resolveSettingsHref(primaryNavigation)}
    >
      {children}
    </AppShellFrame>
  );
}
