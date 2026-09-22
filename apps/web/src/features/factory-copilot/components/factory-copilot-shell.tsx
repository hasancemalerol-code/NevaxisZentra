"use client";

import { useMemo, useState, type ReactNode } from "react";
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

  return (
    <FactoryCopilotShellProvider canAccess={enabled} onOpen={() => setOpen(true)}>
      <div className="factory-copilot-shell">
        {children}
        {enabled ? (
          <>
            <FactoryCopilotToggle onOpen={() => setOpen(true)} />
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
