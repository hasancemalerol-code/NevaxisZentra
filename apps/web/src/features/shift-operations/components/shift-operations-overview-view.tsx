import Link from "next/link";
import { PlayCircle } from "lucide-react";

import { EmptyPanel } from "@/components/ui/empty-panel";

import type {
  ShiftInstanceApiSummary,
  ShiftMetricsApi,
  ShiftOpenItemApiSummary,
} from "../server/shift-operations-api-client";
import { HANDOVER_PRIORITY_LABELS, SHIFT_STATUS_LABELS } from "../domain/shift-operations-labels";
import { deferredSurfacesEnabled } from "@/lib/runtime/deferred-surfaces";
import { formatPercent } from "@/lib/presentation/format";

export function ShiftOperationsOverviewView({
  currentShift,
  openItems,
  metrics,
}: Readonly<{
  currentShift: ShiftInstanceApiSummary | null;
  openItems: readonly ShiftOpenItemApiSummary[];
  metrics: ShiftMetricsApi | null;
}>) {
  const critical = openItems.filter((item) => item.handoverPriority === "critical");
  const hasReadinessData = Boolean(currentShift) || (metrics?.openItems ?? openItems.length) > 0 || (metrics?.handoverReadinessScore ?? 0) > 0;

  return (
    <>
      <div className="shift-ops-grid">
        <section className="shift-ops-card">
          <h2>Aktif Vardiya</h2>
          {currentShift ? (
            <>
              <p><strong>{currentShift.name}</strong> ({currentShift.shiftCode})</p>
              <span className="shift-ops-status" data-tone={currentShift.status}>
                {SHIFT_STATUS_LABELS[currentShift.status as keyof typeof SHIFT_STATUS_LABELS] ?? currentShift.status}
              </span>
              <p>Amir: {currentShift.supervisorLabel ?? "Amir atanmadı"}</p>
              <p>Ekip: {currentShift.teamLabel ?? "Ekip atanmadı"}</p>
              <Link className="shift-ops-cta" href="/vardiyalar/aktif">Aktif Vardiyayı Aç</Link>
            </>
          ) : (
            <EmptyPanel
              compact
              icon={<PlayCircle />}
              title="Şu anda aktif vardiya yok"
              description="Aktif bir vardiya başladığında amir, ekip ve durum burada görünür."
              action={<Link className="shift-ops-cta" href="/vardiyalar/plan">Vardiya planına git</Link>}
            />
          )}
        </section>

        <section className="shift-ops-card">
          <h2>Teslim Hazırlığı</h2>
          {hasReadinessData ? (
            <div className="shift-ops-kpi">
              <p className="shift-ops-readiness">{formatPercent(metrics?.handoverReadinessScore ?? 0, 0)}</p>
              <p className="shift-ops-kpi__caption">Hazırlık puanı</p>
            </div>
          ) : (
            <p className="shift-ops-muted">Aktif vardiya yok; hazırlık puanı vardiya açıldığında görünür.</p>
          )}
          {hasReadinessData ? (<dl className="shift-ops-facts">
            <div><dt>Kritik açık konu</dt><dd>{hasReadinessData ? (metrics?.criticalItems ?? critical.length) : "—"}</dd></div>
            <div><dt>Toplam açık konu</dt><dd>{hasReadinessData ? (metrics?.openItems ?? openItems.length) : "—"}</dd></div>
          </dl>) : null}
          {hasReadinessData ? <Link className="shift-ops-cta shift-ops-cta--secondary" href="/vardiyalar/teslim">Vardiya Teslimini Hazırla</Link> : null}
        </section>

        {deferredSurfacesEnabled() ? (
          <section className="shift-ops-card">
            <h2>Dijital İş Gücü</h2>
            <p>Aktif vaka: {metrics?.digitalWorkforceCases ?? 0}</p>
            <p>Bekleyen onay: {metrics?.pendingApprovals ?? 0}</p>
            <Link href="/dijital-is-gucu">Dijital İş Gücü Merkezi</Link>
          </section>
        ) : null}
      </div>

      {critical.length > 0 ? (
        <section className="shift-ops-card">
          <h2>Kritik Açık Konular</h2>
          <ul className="shift-ops-item-list">
            {critical.slice(0, 5).map((item) => (
              <li className="shift-ops-item" data-priority="critical" key={item.id}>
                <strong>{item.title}</strong>
                <p>{HANDOVER_PRIORITY_LABELS[item.handoverPriority] ?? item.handoverPriority}</p>
              </li>
            ))}
          </ul>
        </section>
      ) : null}
    </>
  );
}
