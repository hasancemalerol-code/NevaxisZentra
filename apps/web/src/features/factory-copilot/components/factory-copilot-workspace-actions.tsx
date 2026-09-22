"use client";

import type { ReactNode } from "react";

import { FactoryCopilotHeaderAction } from "./factory-copilot-header-action";

type FactoryCopilotWorkspaceActionsProps = Readonly<{
  children?: ReactNode;
}>;

export function FactoryCopilotWorkspaceActions({ children }: FactoryCopilotWorkspaceActionsProps) {
  return (
    <>
      <FactoryCopilotHeaderAction />
      {children}
    </>
  );
}
