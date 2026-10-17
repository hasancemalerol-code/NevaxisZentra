import type { HTMLAttributes } from "react";

import { knownStateLabel } from "@/lib/presentation/enum-labels";

export type OperationalStatusVariant =
  | "running"
  | "warning"
  | "blocked"
  | "starved"
  | "fault"
  | "idle"
  | "pending"
  | "verified"
  | "closed";

const LINE_STATE_MAP: Record<string, OperationalStatusVariant> = {
  RUNNING: "running",
  MIXED_DEGRADED: "warning",
  STARVED: "starved",
  BLOCKED: "blocked",
  STOPPED: "idle",
  FAULTED: "fault",
  OPERATING: "running",
  IDLE: "idle",
  FAULT: "fault",
};

export function mapOperationalStatus(state: string): OperationalStatusVariant {
  return LINE_STATE_MAP[state] ?? "idle";
}

type OperationalBadgeProps = HTMLAttributes<HTMLSpanElement> & Readonly<{
  status: OperationalStatusVariant;
  label: string;
}>;

export function OperationalBadge({ status, label, className = "", ...props }: OperationalBadgeProps) {
  return (
    <span className={`fact-operational-badge fact-operational-badge--${status} ${className}`.trim()} {...props}>
      {knownStateLabel(label) ?? label}
    </span>
  );
}
