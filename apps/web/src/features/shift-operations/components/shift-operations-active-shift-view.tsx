import Link from "next/link";

import type {
  ShiftInstanceApiSummary,
  ShiftMetricsApi,
  ShiftOpenItemApiSummary,
} from "../server/shift-operations-api-client";
import {
  HANDOVER_PRIORITY_LABELS,
  OPEN_ITEM_CATEGORY_LABELS,
  SHIFT_STATUS_LABELS,
} from "../domain/shift-operations-labels";

export function ShiftOperationsActiveShiftView({
  shift,
  openItems,
  metrics,
}: Readonly<{
  shift: ShiftInstanceApiSummary;
  openItems: readonly ShiftOpenItemApiSummary[];
  metrics: ShiftMetricsApi | null;
}>) {
  const end = new Date(shift.plannedEndUtc);
  const remainingMs = end.getTime() - Date.now();
  const remainingHours = Math.max(0, Math.round(remainingMs / 3_600_000 * 10) / 10);

  return (
    <>
      <header className="shift-ops-hero">
        <p className="shift-ops-hero__eyebrow">AKTİF VARDİYA</p>
        <h1>{shift.name}</h1>
        <p>
          <span className="shift-ops-status" data-tone={shift.status}>
            {SHIFT_STATUS_LABELS[shift.status as keyof typeof SHIFT_STATUS_LABELS] ?? shift.status}
          </span>
          {" · "}
          Kalan süre: ~{remainingHours} saat
        </p>
        <p>Amir: {shift.supervisorLabel ?? "—"} · Ekip: {shift.teamLabel ?? "—"}</p>
      </header>

      <div className="shift-ops-grid">
        <section className="shift-ops-card"><h3>Üretim</h3><p>Planlanan üretim bağlamı aktif vardiyada izleniyor.</p></section>
        <section className="shift-ops-card"><h3>Makine Durumu</h3><p>IoT/üretim kaynaklarından makine özetleri.</p></section>
        <section className="shift-ops-card">
          <h3>Açık Konular</h3>
          <p>{metrics?.openItems ?? openItems.length} açık kalem</p>
        </section>
        <section className="shift-ops-card">
          <h3>Dijital İş Gücü</h3>
          <p>{metrics?.digitalWorkforceCases ?? 0} aktif vaka</p>
          <Link href="/dijital-is-gucu">Kontrol merkezi</Link>
        </section>
        <section className="shift-ops-card">
          <h3>Bekleyen Onaylar</h3>
          <p>{metrics?.pendingApprovals ?? 0}</p>
        </section>
        <section className="shift-ops-card">
          <h3>Kritik Riskler</h3>
          <p>{metrics?.criticalItems ?? 0}</p>
        </section>
      </div>

      <section className="shift-ops-card">
        <h3>Açık Konular</h3>
        <ul className="shift-ops-item-list">
          {openItems.slice(0, 12).map((item) => (
            <li className="shift-ops-item" data-priority={item.handoverPriority} key={item.id}>
              <strong>{item.title}</strong>
              <p>
                {OPEN_ITEM_CATEGORY_LABELS[item.category] ?? item.category}
                {" · "}
                {HANDOVER_PRIORITY_LABELS[item.handoverPriority] ?? item.handoverPriority}
                {item.carryoverCount > 0 ? ` · ${item.carryoverCount} vardiya devri` : ""}
              </p>
            </li>
          ))}
        </ul>
      </section>

      <Link className="shift-ops-cta" href="/vardiyalar/teslim">Vardiya Teslimini Hazırla</Link>
    </>
  );
}
