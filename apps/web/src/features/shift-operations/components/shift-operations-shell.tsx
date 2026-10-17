"use client";

import { PageHeader } from "@/components/ui/page";
import { SubNav } from "@/components/ui/sub-nav";
import { usePathname } from "next/navigation";
import type { ReactNode } from "react";

import { resolveShiftOperationsPageHeader, SHIFT_OPERATIONS_EYEBROW, SHIFT_OPERATIONS_SUBNAV } from "../domain/shift-operations-labels";
import "./shift-operations-workspace.css";
import { customerMessage } from "@/lib/presentation/customer-message";
import { Notice } from "@/components/ui/notice";
import { useScrollFade } from "@/lib/hooks/use-scroll-fade";

export function ShiftOperationsShell({
  children,
  apiError = null,
}: Readonly<{ children: ReactNode; apiError?: string | null }>) {
  const pathname = usePathname();
  const header = resolveShiftOperationsPageHeader(pathname);
  const tabsRef = useScrollFade<HTMLDivElement>(pathname);

  return (
    <div className="shift-ops-page">
      <PageHeader eyebrow={SHIFT_OPERATIONS_EYEBROW} title={header.title} description={header.description} />
      <div className="shift-ops-tabs" ref={tabsRef}>
      <SubNav
        label="Vardiya bölümleri"
        groups={[{
          items: SHIFT_OPERATIONS_SUBNAV.map((item) => ({
            href: item.href,
            label: item.label,
            active: "exact" in item && item.exact
              ? pathname === item.href
              : pathname === item.href || pathname.startsWith(`${item.href}/`),
          })),
        }]}
      />
      </div>
      {apiError ? <Notice tone="danger" alert>{customerMessage(apiError)}</Notice> : null}
      {children}
    </div>
  );
}
