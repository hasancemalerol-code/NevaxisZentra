"use client";

import { createContext, useContext, type ReactNode } from "react";

type FactoryCopilotShellContextValue = Readonly<{
  canAccess: boolean;
  open: () => void;
  /** A page header launcher registers itself so the shell does not show a second, floating launcher. */
  registerHeaderAction: () => () => void;
}>;

const FactoryCopilotShellContext = createContext<FactoryCopilotShellContextValue | null>(null);

type FactoryCopilotShellProviderProps = Readonly<{
  canAccess: boolean;
  onOpen: () => void;
  registerHeaderAction: () => () => void;
  children: ReactNode;
}>;

export function FactoryCopilotShellProvider({
  canAccess,
  onOpen,
  registerHeaderAction,
  children,
}: FactoryCopilotShellProviderProps) {
  const value: FactoryCopilotShellContextValue = {
    canAccess,
    open: onOpen,
    registerHeaderAction,
  };

  return <FactoryCopilotShellContext.Provider value={value}>{children}</FactoryCopilotShellContext.Provider>;
}

export function useFactoryCopilotShell(): FactoryCopilotShellContextValue | null {
  return useContext(FactoryCopilotShellContext);
}
