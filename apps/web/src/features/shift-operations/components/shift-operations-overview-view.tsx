import Link from "next/link";

import type {
  ShiftInstanceApiSummary,
  ShiftMetricsApi,
  ShiftOpenItemApiSummary,
} from "../server/shift-operations-api-client";
import { HANDOVER_PRIORITY_LABELS, SHIFT_STATUS_LABELS } from "../domain/shift-operations-labels";

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

  return (
    <>
      <header className="shift-ops-hero">
        <p className="shift-ops-hero__eyebrow">VARDİYA OPERASYONLARI</p>
        <h1>Genel Bakış</h1>
        <p>Planlama, aktif vardiya, teslim ve açık konuların tek merkezden izlenmesi.</p>
      </header>

      <div className="shift-ops-grid">
        <section className="shift-ops-card">
          <h3>Aktif Vardiya</h3>
          {currentShift ? (
            <>
              <p><strong>{currentShift.name}</strong> ({currentShift.shiftCode})</p>
              <span className="shift-ops-status" data-tone={currentShift.status}>
                {SHIFT_STATUS_LABELS[currentShift.status as keyof typeof SHIFT_STATUS_LABELS] ?? currentShift.status}
              </span>
              <p>Amir: {currentShift.supervisorLabel ?? "—"}</p>
              <p>Ekip: {currentShift.teamLabel ?? "—"}</p>
              <Link className="shift-ops-cta" href="/vardiyalar/aktif">Aktif Vardiyayı Aç</Link>
            </>
          ) : (
            <p>Aktif vardiya bulunamadı.</p>
          )}
        </section>

        <section className="shift-ops-card">
          <h3>Teslim Hazırlığı</h3>
          <p className="shift-ops-readiness">{metrics?.handoverReadinessScore ?? 0}%</p>
          <p>Kritik açık konu: {metrics?.criticalItems ?? critical.length}</p>
          <Link className="shift-ops-cta" href="/vardiyalar/teslim">Vardiya Teslimini Hazırla</Link>
        </section>

        <section className="shift-ops-card">
          <h3>Dijital İş Gücü</h3>
          <p>Aktif vaka: {metrics?.digitalWorkforceCases ?? 0}</p>
          <p>Bekleyen onay: {metrics?.pendingApprovals ?? 0}</p>
          <Link href="/dijital-is-gucu">Dijital İş Gücü Merkezi</Link>
        </section>
      </div>

      {critical.length > 0 ? (
        <section className="shift-ops-card">
          <h3>Kritik Açık Konular</h3>
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
