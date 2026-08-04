import type { HTMLAttributes } from "react";

type BadgeTone = "neutral" | "information" | "success" | "warning" | "danger" | "ai";

type BadgeProps = HTMLAttributes<HTMLSpanElement> & Readonly<{ tone?: BadgeTone }>;

export function Badge({ tone = "neutral", className = "", ...props }: BadgeProps) {
  return <span className={`badge badge-${tone} ${className}`.trim()} {...props} />;
}

type Status = "normal" | "attention" | "critical" | "offline" | "analysis";

const statusLabels: Record<Status, string> = {
  normal: "Normal",
  attention: "Dikkat",
  critical: "Kritik",
  offline: "Çevrimdışı",
  analysis: "Analiz",
};

export function StatusBadge({ status, label }: Readonly<{ status: Status; label?: string }>) {
  return (
    <Badge className="status-badge" tone={status === "normal" ? "success" : status === "attention" ? "warning" : status === "critical" ? "danger" : status === "analysis" ? "ai" : "neutral"}>
      <span className="status-dot" aria-hidden="true" />
      {label ?? statusLabels[status]}
    </Badge>
  );
}
