import Link from "next/link";
import type { ReactNode } from "react";

type MaintenanceDashboardKpiProps = Readonly<{
  label: string;
  value: number | string;
  description: string;
  icon: ReactNode;
  tone: "in-progress" | "ready" | "paused" | "critical";
  href?: string;
}>;

export function MaintenanceDashboardKpi({
  label,
  value,
  description,
  icon,
  tone,
  href,
}: MaintenanceDashboardKpiProps) {
  const className = `maintenance-dashboard-kpi maintenance-dashboard-kpi--${tone}`;
  const content = (
    <>
      <span className="maintenance-dashboard-kpi__icon" aria-hidden="true">
        {icon}
      </span>
      <div className="maintenance-dashboard-kpi__content">
        <span className="maintenance-dashboard-kpi__label">{label}</span>
        <strong className="maintenance-dashboard-kpi__value">{value}</strong>
        <small className="maintenance-dashboard-kpi__desc">{description}</small>
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
