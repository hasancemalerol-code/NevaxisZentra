import { Clock, TrendingUp, AlertTriangle, Settings, Activity, Timer } from "lucide-react";

import type { ProductionRuntimeSummary } from "../domain/production-execution-operations";
import { formatDuration, formatDurationMs } from "../domain/production-execution-operations";

type RuntimeAnalyticsProps = Readonly<{
  summary: ProductionRuntimeSummary;
}>;

type TileProps = Readonly<{
  label: string;
  value: string;
  subValue?: string;
  icon: React.ReactNode;
  tone?: "neutral" | "success" | "warning" | "primary";
}>;

function AnalyticsTile({ label, value, subValue, icon, tone = "neutral" }: TileProps) {
  return (
    <div className={`runtime-tile runtime-tile--${tone}`}>
      <div className="runtime-tile__icon">{icon}</div>
      <div className="runtime-tile__body">
        <span className="runtime-tile__label">{label}</span>
        <strong className="runtime-tile__value">{value}</strong>
        {subValue && <span className="runtime-tile__sub">{subValue}</span>}
      </div>
    </div>
  );
}

export function ProductionRuntimeAnalytics({ summary }: RuntimeAnalyticsProps) {
  const hasStarted = summary.currentElapsedSeconds > 0 || summary.actualStartAtUtc !== undefined;

  if (!hasStarted) {
    return (
      <div className="runtime-analytics">
        <div className="runtime-analytics__header">
          <p className="ops-panel__eyebrow">ÇALIŞMA ANALİTİĞİ</p>
          <h2>Üretim Süresi</h2>
        </div>
        <p className="runtime-analytics__empty">
          Üretim başladığında çalışma metrikleri burada görünür.
        </p>
      </div>
    );
  }

  const averageCycleLabel =
    summary.averageManualCycleMilliseconds !== undefined
      ? formatDurationMs(summary.averageManualCycleMilliseconds)
      : "—";

  return (
    <div className="runtime-analytics">
      <div className="runtime-analytics__header">
        <p className="ops-panel__eyebrow">ÇALIŞMA ANALİTİĞİ</p>
        <h2>Üretim Süresi</h2>
      </div>

      <div className="runtime-analytics__grid">
        <AnalyticsTile
          label="Geçen Süre"
          value={formatDuration(summary.currentElapsedSeconds)}
          icon={<Clock aria-hidden="true" />}
          tone="neutral"
        />
        <AnalyticsTile
          label="Net Çalışma"
          value={formatDuration(summary.netRunningSeconds)}
          subValue={summary.currentElapsedSeconds > 0 ? `${Math.round((summary.netRunningSeconds / summary.currentElapsedSeconds) * 100)}%` : undefined}
          icon={<TrendingUp aria-hidden="true" />}
          tone="success"
        />
        <AnalyticsTile
          label="Toplam Duruş"
          value={formatDuration(summary.totalDowntimeSeconds)}
          icon={<AlertTriangle aria-hidden="true" />}
          tone={summary.totalDowntimeSeconds > 0 ? "warning" : "neutral"}
        />
        <AnalyticsTile
          label="Setup Süresi"
          value={formatDuration(summary.totalSetupSeconds)}
          icon={<Settings aria-hidden="true" />}
          tone={summary.totalSetupSeconds > 0 ? "primary" : "neutral"}
        />
        <AnalyticsTile
          label="Manuel Çevrim"
          value={String(summary.manualCycleCount)}
          icon={<Activity aria-hidden="true" />}
          tone="neutral"
        />
        <AnalyticsTile
          label="Ort. Çevrim"
          value={averageCycleLabel}
          icon={<Timer aria-hidden="true" />}
          tone="neutral"
        />
      </div>

      {(summary.activeDowntime ?? summary.activeSetup) && (
        <div className="runtime-analytics__active">
          {summary.activeDowntime && (
            <span className="runtime-analytics__active-badge runtime-analytics__active-badge--warning">
              <AlertTriangle aria-hidden="true" />
              Aktif duruş
            </span>
          )}
          {summary.activeSetup && (
            <span className="runtime-analytics__active-badge runtime-analytics__active-badge--primary">
              <Settings aria-hidden="true" />
              Aktif setup
            </span>
          )}
        </div>
      )}
    </div>
  );
}
