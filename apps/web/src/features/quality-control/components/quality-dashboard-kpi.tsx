import Link from "next/link";
import type { ReactNode } from "react";

type QualityDashboardKpiProps = Readonly<{
  label: string;
  value: number | string;
  description: string;
  icon: ReactNode;
  tone: "in-progress" | "ready" | "paused";
  href?: string;
}>;

export function QualityDashboardKpi({
  label,
  value,
  description,
  icon,
  tone,
  href,
}: QualityDashboardKpiProps) {
  const className = `quality-dashboard-kpi quality-dashboard-kpi--${tone}`;
  const content = (
    <>
      <span className="quality-dashboard-kpi__icon" aria-hidden="true">
        {icon}
      </span>
      <div className="quality-dashboard-kpi__content">
        <span className="quality-dashboard-kpi__label">{label}</span>
        <strong className="quality-dashboard-kpi__value">{value}</strong>
        <small className="quality-dashboard-kpi__desc">{description}</small>
      </div>
    </>
  );

  if (href) {
    return (
      <Link href={href} className={className}>
        {content}
      </Link>
    );
  }

  return <div className={className}>{content}</div>;
}
