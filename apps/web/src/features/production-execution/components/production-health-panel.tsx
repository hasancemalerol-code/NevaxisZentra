import {
  Activity,
  AlertTriangle,
  CheckCircle2,
  Clock,
  SkipForward,
  Zap,
} from "lucide-react";

import type { ProductionExecutionHealthSummary } from "../domain/production-execution-exceptions";
import { getHealthStatusLabel } from "../domain/production-execution-exceptions";

type HealthPanelProps = Readonly<{
  summary: ProductionExecutionHealthSummary;
}>;

export function ProductionHealthPanel({ summary }: HealthPanelProps) {
  const tone =
    summary.status === "critical"
      ? "critical"
      : summary.status === "attention"
        ? "attention"
        : "normal";

  return (
    <div className={`health-panel health-panel--${tone}`}>
      <div className="health-panel__header">
        <Activity aria-hidden="true" className="health-panel__icon" />
        <div>
          <p className="health-panel__eyebrow">OPERASYON SAĞLIĞI</p>
          <h2 className="health-panel__title">{getHealthStatusLabel(summary.status)}</h2>
        </div>

        {summary.requiresAttention && (
          <div className="health-panel__attention-badge" role="status">
            <AlertTriangle aria-hidden="true" />
            Dikkat gerekiyor
          </div>
        )}
      </div>

      <div className="health-panel__signals">
        {summary.criticalExceptionCount > 0 && (
          <div className="health-panel__signal health-panel__signal--critical">
            <Zap aria-hidden="true" />
            <span>
              <strong>{summary.criticalExceptionCount}</strong> kritik istisna açık
            </span>
          </div>
        )}

        {summary.openExceptionCount > 0 && summary.criticalExceptionCount === 0 && (
          <div className="health-panel__signal health-panel__signal--warning">
            <AlertTriangle aria-hidden="true" />
            <span>
              <strong>{summary.openExceptionCount}</strong> açık istisna
            </span>
          </div>
        )}

        {summary.downtimeIsStale && (
          <div className="health-panel__signal health-panel__signal--warning">
            <Clock aria-hidden="true" />
            <span>Duruş uzun süredir açık — uzlaştırma gerekebilir</span>
          </div>
        )}

        {summary.setupIsStale && (
          <div className="health-panel__signal health-panel__signal--warning">
            <Clock aria-hidden="true" />
            <span>Setup uzun süredir açık — uzlaştırma gerekebilir</span>
          </div>
        )}

        {summary.scheduleOverrun && (
          <div className="health-panel__signal health-panel__signal--warning">
            <SkipForward aria-hidden="true" />
            <span>Planlanan bitiş aşıldı</span>
          </div>
        )}

        {summary.hasActiveDowntime && !summary.downtimeIsStale && (
          <div className="health-panel__signal health-panel__signal--info">
            <Clock aria-hidden="true" />
            <span>Aktif duruş devam ediyor</span>
          </div>
        )}

        {summary.hasActiveSetup && !summary.setupIsStale && (
          <div className="health-panel__signal health-panel__signal--info">
            <Clock aria-hidden="true" />
            <span>Aktif setup devam ediyor</span>
          </div>
        )}

        {!summary.requiresAttention && (
          <div className="health-panel__signal health-panel__signal--ok">
            <CheckCircle2 aria-hidden="true" />
            <span>Operasyonel durum normal</span>
          </div>
        )}
      </div>

      {summary.quantityProgress !== undefined && (
        <div className="health-panel__progress">
          <div className="health-panel__progress-bar" role="meter" aria-valuenow={Math.round(summary.quantityProgress * 100)} aria-valuemin={0} aria-valuemax={100} aria-label="Üretim ilerleme">
            <div
              className="health-panel__progress-fill"
              style={{ width: `${Math.min(100, Math.round(summary.quantityProgress * 100))}%` }}
            />
          </div>
          <span className="health-panel__progress-label">
            {Math.round(summary.quantityProgress * 100)}% tamamlandı
          </span>
        </div>
      )}
    </div>
  );
}
