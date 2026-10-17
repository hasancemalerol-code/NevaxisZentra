import type { ReactNode } from "react";

export type MetricTone = "neutral" | "critical" | "high" | "medium" | "low" | "info";

type MetricCardProps = Readonly<{
  label: string;
  value: ReactNode;
  hint?: string;
  icon?: ReactNode;
  tone?: MetricTone;
}>;

/** One KPI. Always rendered inside MetricGrid so a screen's summary row keeps the same rhythm everywhere. */
export function MetricCard({ label, value, hint, icon, tone = "neutral" }: MetricCardProps) {
  return (
    <li className={`zx-metric zx-metric--${tone}`}>
      {icon ? <span className="zx-metric__icon" aria-hidden="true">{icon}</span> : null}
      <div className="zx-metric__body">
        <span className="zx-metric__label">{label}</span>
        <strong className="zx-metric__value">{value}</strong>
        {hint ? <span className="zx-metric__hint">{hint}</span> : null}
      </div>
    </li>
  );
}

export function MetricGrid({ label, children, ...props }: Readonly<{ label: string; children: ReactNode; "data-testid"?: string }>) {
  return (
    <ul className="zx-metric-grid" aria-label={label} {...props}>
      {children}
    </ul>
  );
}
