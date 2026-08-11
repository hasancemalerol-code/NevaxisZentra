"use client";

import type { ReactNode } from "react";

import { primaryNavigation } from "@/lib/navigation/primary-navigation";
import { AppShellFrame } from "./app-shell-frame";

export function AppShell({ children }: Readonly<{ children: ReactNode }>) {
  return <AppShellFrame navigation={primaryNavigation}>{children}</AppShellFrame>;
}
