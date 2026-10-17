import type { ReactNode } from "react";

export type SeverityLevel = "critical" | "high" | "medium" | "low" | "info" | "ai" | "neutral";

/** Risk / severity / lifecycle indicator. The colour is never the only signal: the label is always rendered. */
export function SeverityBadge({ level, children }: Readonly<{ level: SeverityLevel; children: ReactNode }>) {
  return <span className={`zx-sev zx-sev--${level}`}>{children}</span>;
}
