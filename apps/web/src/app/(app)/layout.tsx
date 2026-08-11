import type { ReactNode } from "react";

import { AppShellFrame } from "@/components/app-shell/app-shell-frame";
import { resolvePermissionAwareNavigation } from "@/lib/navigation/navigation-visibility";

export default async function ApplicationLayout({ children }: Readonly<{ children: ReactNode }>) {
  const navigation = await resolvePermissionAwareNavigation();

  return <AppShellFrame navigation={navigation}>{children}</AppShellFrame>;
}
