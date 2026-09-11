"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import type { ReactNode } from "react";

import { SHIFT_OPERATIONS_SUBNAV } from "../domain/shift-operations-labels";
import "./shift-operations-workspace.css";

export function ShiftOperationsShell({
  children,
  apiError = null,
}: Readonly<{ children: ReactNode; apiError?: string | null }>) {
  const pathname = usePathname();

  return (
    <div className="shift-ops-page">
      <nav aria-label="Vardiya bölümleri" className="shift-ops-shell__nav">
        {SHIFT_OPERATIONS_SUBNAV.map((item) => {
          const active = "exact" in item && item.exact
            ? pathname === item.href
            : pathname === item.href || pathname.startsWith(`${item.href}/`);
          return (
            <Link data-active={active} href={item.href} key={item.href}>
              {item.label}
            </Link>
          );
        })}
      </nav>
      {apiError ? <p role="alert">{apiError}</p> : null}
      {children}
    </div>
  );
}
