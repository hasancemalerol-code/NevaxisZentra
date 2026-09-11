"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import type { ReactNode } from "react";

import { useDigitalWorkforceRuntime } from "../application/digital-workforce-runtime";
import {
  AUTONOMY_LABELS,
  AUTONOMY_MODE_ORDER,
  CASE_STATUS_LABELS,
  DIGITAL_WORKFORCE_SUBNAV,
  DIGITAL_WORKFORCE_VERIFIED_BADGE,
  STAGE_LABELS,
  STAGE_ORDER,
  WORKER_STATUS_LABELS,
} from "../domain/digital-workforce-labels";
import type {
  DigitalAutonomyMode,
  DigitalWorkCase,
  DigitalWorkCaseStatus,
  DigitalWorkerRuntimeStatus,
  DigitalWorkStage,
} from "../domain/digital-workforce-types";

export function DigitalWorkforceSubnav() {
  const pathname = usePathname();
  const runtime = useDigitalWorkforceRuntime();
  const pendingCount = runtime.snapshot?.pendingApprovals
    ?? runtime.approvals.filter((item) => item.status === "pending").length;

  return (
    <nav aria-label="Dijital iş gücü bölümleri" className="dwf-shell__nav">
      {DIGITAL_WORKFORCE_SUBNAV.map((item) => {
        const active = "exact" in item && item.exact
          ? pathname === item.href
          : pathname === item.href || pathname.startsWith(`${item.href}/`);
        const showBadge = item.href === "/dijital-is-gucu/onaylar" && pendingCount > 0;
        return (
          <Link data-active={active} href={item.href} key={item.href}>
            {item.label}
            {showBadge ? <span className="dwf-nav-badge">{pendingCount}</span> : null}
          </Link>
        );
      })}
    </nav>
  );
}

export function StatusChip({
  status,
}: Readonly<{ status: DigitalWorkerRuntimeStatus }>) {
  return (
    <span className="dwf-status" data-tone={status}>
      {WORKER_STATUS_LABELS[status]}
    </span>
  );
}

export function CaseStatusChip({ status }: Readonly<{ status: DigitalWorkCaseStatus }>) {
  return <span className="dwf-pill">{CASE_STATUS_LABELS[status]}</span>;
}

export function VerifiedBadge() {
  return <span className="dwf-badge dwf-badge--verified">{DIGITAL_WORKFORCE_VERIFIED_BADGE}</span>;
}

export function DemoBadge({ label = "Demo" }: Readonly<{ label?: string }>) {
  return <span className="dwf-badge dwf-badge--demo">{label}</span>;
}

export function AutonomyChips({
  value,
  onChange,
}: Readonly<{
  value: DigitalAutonomyMode;
  onChange: (mode: DigitalAutonomyMode) => void;
}>) {
  return (
    <div className="dwf-mode" role="group" aria-label="Çalışma modu">
      {AUTONOMY_MODE_ORDER.map((mode) => (
        <button
          data-active={value === mode}
          key={mode}
          onClick={() => onChange(mode)}
          type="button"
        >
          {AUTONOMY_LABELS[mode]}
        </button>
      ))}
    </div>
  );
}

export function LifecycleTrack({
  current,
  completed,
}: Readonly<{
  current: DigitalWorkStage;
  completed: readonly DigitalWorkStage[];
}>) {
  return (
    <div aria-label="İş yaşam döngüsü" className="dwf-lifecycle">
      {STAGE_ORDER.map((stage) => {
        const state = completed.includes(stage)
          ? "done"
          : stage === current
            ? "current"
            : "todo";
        return (
          <span data-state={state} key={stage}>
            {state === "done" ? "✓" : state === "current" ? "→" : "○"} {STAGE_LABELS[stage]}
          </span>
        );
      })}
    </div>
  );
}

export function WorkCaseCard({
  item,
  action,
}: Readonly<{
  item: DigitalWorkCase;
  action?: ReactNode;
}>) {
  return (
    <article className="dwf-case" style={{ padding: "0.85rem" }}>
      <div className="dwf-row">
        <h3>{item.title}</h3>
        <div className="dwf-inline">
          <CaseStatusChip status={item.status} />
          {item.verified ? <VerifiedBadge /> : <DemoBadge />}
        </div>
      </div>
      <p>{item.description}</p>
      <p className="dwf-meta">{item.businessImpact}</p>
      <LifecycleTrack completed={item.lifecycleCompleted} current={item.currentStage} />
      {item.handoffs.length > 0 ? (
        <div className="dwf-chain">
          {item.handoffs.map((handoff) => (
            <span className="dwf-pill" key={handoff}>Handoff · {handoff}</span>
          ))}
        </div>
      ) : null}
      {action}
    </article>
  );
}
