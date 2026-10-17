import type { HTMLAttributes, ReactNode } from "react";

export function Card({ className = "", ...props }: HTMLAttributes<HTMLDivElement>) {
  return <div className={`card ${className}`.trim()} {...props} />;
}

type KpiTone = "neutral" | "information" | "success" | "warning";

type KpiCardProps = Readonly<{
  label: string;
  value: string;
  detail: string;
  icon: ReactNode;
  tone?: KpiTone;
  preview?: boolean;
}>;

export function KpiCard({ label, value, detail, icon, tone = "neutral", preview: _preview = false }: KpiCardProps) {
  return (
    <Card className={`kpi-card kpi-${tone}`}>
      <div className="kpi-card-heading">
        <span className="kpi-label">{label}</span>
        <span className="kpi-icon" aria-hidden="true">{icon}</span>
      </div>
      <div className="kpi-value">{value}</div>
      <div className="kpi-footer">
        <span>{detail}</span>
        
      </div>
    </Card>
  );
}
