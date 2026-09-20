"use client";

import { useMemo, useState, type ReactNode } from "react";
import { usePathname, useSearchParams } from "next/navigation";

import { isFactoryCopilotSurface } from "../application/factory-copilot-context";
import { FactoryCopilotDrawer, FactoryCopilotToggle } from "./factory-copilot-drawer";

import "../factory-copilot.css";

type FactoryCopilotShellProps = Readonly<{
  children: ReactNode;
}>;

export function FactoryCopilotShell({ children }: FactoryCopilotShellProps) {
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [open, setOpen] = useState(false);
  const enabled = useMemo(() => isFactoryCopilotSurface(pathname), [pathname]);

  return (
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
  );
}
