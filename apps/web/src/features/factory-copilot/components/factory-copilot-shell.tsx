"use client";

import { useCallback, useEffect, useMemo, useState, type ReactNode } from "react";
import { usePathname, useSearchParams } from "next/navigation";

import { isFactoryCopilotSurface } from "../application/factory-copilot-context";
import { FactoryCopilotDrawer, FactoryCopilotToggle } from "./factory-copilot-drawer";
import { FactoryCopilotShellProvider } from "./factory-copilot-shell-context";

import "../factory-copilot.css";

type FactoryCopilotShellProps = Readonly<{
  children: ReactNode;
  canAccessCopilot: boolean;
}>;

export function FactoryCopilotShell({ children, canAccessCopilot }: FactoryCopilotShellProps) {
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [open, setOpen] = useState(false);
  const surfaceEnabled = useMemo(() => isFactoryCopilotSurface(pathname), [pathname]);
  const enabled = surfaceEnabled && canAccessCopilot;
  const [headerActions, setHeaderActions] = useState(0);
  const registerHeaderAction = useCallback(() => {
    setHeaderActions((count) => count + 1);
    return () => setHeaderActions((count) => Math.max(0, count - 1));
  }, []);

  // Leaving a Copilot surface must not leave the panel "open" for the next time the user comes back.
  useEffect(() => {
    if (!enabled) setOpen(false);
  }, [enabled]);

  return (
    <FactoryCopilotShellProvider canAccess={enabled} onOpen={() => setOpen(true)} registerHeaderAction={registerHeaderAction}>
      <div className="factory-copilot-shell">
        {children}
        {enabled ? (
          <>
            {headerActions === 0 ? <FactoryCopilotToggle onOpen={() => setOpen(true)} /> : null}
            <FactoryCopilotDrawer
              open={open}
              onClose={() => setOpen(false)}
              pathname={pathname}
              searchParams={searchParams}
            />
          </>
        ) : null}
      </div>
    </FactoryCopilotShellProvider>
  );
}
