"use client";

import { createContext, useContext, type ReactNode } from "react";

type FactoryCopilotShellContextValue = Readonly<{
  canAccess: boolean;
  open: () => void;
}>;

const FactoryCopilotShellContext = createContext<FactoryCopilotShellContextValue | null>(null);

type FactoryCopilotShellProviderProps = Readonly<{
  canAccess: boolean;
  onOpen: () => void;
  children: ReactNode;
}>;

export function FactoryCopilotShellProvider({
  canAccess,
  onOpen,
  children,
}: FactoryCopilotShellProviderProps) {
  const value: FactoryCopilotShellContextValue = {
    canAccess,
    open: onOpen,
  };

  return <FactoryCopilotShellContext.Provider value={value}>{children}</FactoryCopilotShellContext.Provider>;
}

export function useFactoryCopilotShell(): FactoryCopilotShellContextValue | null {
  return useContext(FactoryCopilotShellContext);
}
