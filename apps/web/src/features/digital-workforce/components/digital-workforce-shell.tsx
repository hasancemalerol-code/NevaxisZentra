"use client";

import type { ReactNode } from "react";

import { DigitalWorkforceRuntimeProvider, useDigitalWorkforceRuntime } from "../application/digital-workforce-runtime";
import type {
  DigitalWorkActivityApiSummary,
  DigitalWorkApprovalApiSummary,
  DigitalWorkforceControlCenterSnapshot,
} from "../server/digital-workforce-api-client";
import { DigitalWorkforceSubnav } from "./digital-workforce-primitives";
import "./digital-workforce-workspace.css";
import { customerMessage } from "@/lib/presentation/customer-message";

function ShellFrame({ children }: Readonly<{ children: ReactNode }>) {
  const runtime = useDigitalWorkforceRuntime();
  const paused = runtime.snapshot?.globalPauseActive ?? false;
  const killed = runtime.snapshot?.globalKillActive ?? false;

  return (
    <div className="dwf-page" data-paused={paused || killed}>
      <DigitalWorkforceSubnav />
      {killed ? (
        <p className="dwf-banner dwf-banner--emergency" role="status">
          Acil durdur aktif. Tüm dijital çalışanlar durduruldu. Makine güvenliği değişmedi; PLC yazma kapalı kaldı.
        </p>
      ) : null}
      {paused && !killed ? (
        <p className="dwf-banner dwf-banner--paused" role="status">
          Dijital iş gücü duraklatıldı. İzleme devam eder; yeni uygulama başlamaz.
        </p>
      ) : null}
      {runtime.apiError ? <p className="dwf-api-error">{customerMessage(runtime.apiError)}</p> : null}
      {children}
    </div>
  );
}

export function DigitalWorkforceShell({
  children,
  initialSnapshot = null,
  initialApprovals = [],
  initialActivity = [],
  apiError = null,
}: Readonly<{
  children: ReactNode;
  initialSnapshot?: DigitalWorkforceControlCenterSnapshot | null;
  initialApprovals?: readonly DigitalWorkApprovalApiSummary[];
  initialActivity?: readonly DigitalWorkActivityApiSummary[];
  apiError?: string | null;
}>) {
  return (
    <DigitalWorkforceRuntimeProvider
      apiError={apiError}
      initialActivity={initialActivity}
      initialApprovals={initialApprovals}
      initialSnapshot={initialSnapshot}
    >
      <ShellFrame>{children}</ShellFrame>
    </DigitalWorkforceRuntimeProvider>
  );
}
