import { resolveEnumLabel } from "@/lib/presentation/enum-labels";

import { SeverityBadge } from "./severity-badge";

/** The one badge for any status, severity, priority, freshness or category value: Turkish label, shared look. */
export function EnumBadge({ value, label }: Readonly<{ value: string | null | undefined; label?: string }>) {
  const resolved = resolveEnumLabel(value);
  return <SeverityBadge level={resolved.level}>{label ?? resolved.label}</SeverityBadge>;
}
