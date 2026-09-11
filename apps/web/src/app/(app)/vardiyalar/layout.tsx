import type { ReactNode } from "react";

import { ShiftOperationsShell } from "@/features/shift-operations/components/shift-operations-shell";
import { readShiftOperationsWorkspace } from "@/features/shift-operations/server/shift-operations-reader";

export default async function VardiyalarLayout({ children }: Readonly<{ children: ReactNode }>) {
  const workspace = await readShiftOperationsWorkspace();
  const apiError = workspace.status === "failure" ? workspace.message : null;

  return <ShiftOperationsShell apiError={apiError}>{children}</ShiftOperationsShell>;
}
