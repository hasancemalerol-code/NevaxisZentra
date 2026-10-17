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
import { enumLabel } from "@/lib/presentation/enum-labels";
import { deferredSurfacesEnabled } from "@/lib/runtime/deferred-surfaces";

export function ShiftOperationsActiveShiftView({
  shift,
  openItems,
  metrics,
}: Readonly<{
  shift: ShiftInstanceApiSummary;
  openItems: readonly ShiftOpenItemApiSummary[];
  metrics: ShiftMetricsApi | null;
}>) {
  const showWorkforce = deferredSurfacesEnabled();
  const end = new Date(shift.plannedEndUtc);
  const remainingMs = end.getTime() - Date.now();
  const remainingHours = Math.max(0, Math.round(remainingMs / 3_600_000 * 10) / 10);

  return (
    <>
      <p className="shift-ops-meta">
        <strong>{shift.name}</strong>
        <span className="shift-ops-status" data-tone={shift.status}>
          {SHIFT_STATUS_LABELS[shift.status as keyof typeof SHIFT_STATUS_LABELS] ?? shift.status}
        </span>
        <span>{remainingHours > 0 ? `Kalan süre: yaklaşık ${remainingHours} saat` : "Planlanan bitiş saati geçti"}</span>
        <span>Amir: {shift.supervisorLabel ?? "Amir atanmadı"}</span>
        <span>Ekip: {shift.teamLabel ?? "Ekip atanmadı"}</span>
      </p>

      <div className="shift-ops-grid">
        <section className="shift-ops-card"><h2>Üretim</h2><p>Planlanan üretim bağlamı aktif vardiyada izleniyor.</p></section>
        <section className="shift-ops-card"><h2>Makine Durumu</h2><p>Üretim kaynaklarından makine özetleri.</p></section>
        <section className="shift-ops-card">
          <h2>Açık Konular</h2>
          <p>{metrics?.openItems ?? openItems.length} açık kalem</p>
        </section>
        {deferredSurfacesEnabled() ? (
          <section className="shift-ops-card">
            <h2>Dijital İş Gücü</h2>
            <p>{metrics?.digitalWorkforceCases ?? 0} aktif vaka</p>
            <Link href="/dijital-is-gucu">Kontrol merkezi</Link>
          </section>
        ) : null}
        <section className="shift-ops-card">
          <h2>Bekleyen Onaylar</h2>
          <p>{metrics?.pendingApprovals ?? 0}</p>
        </section>
        <section className="shift-ops-card">
          <h2>Kritik Riskler</h2>
          <p>{metrics?.criticalItems ?? 0}</p>
        </section>
      </div>

      <section className="shift-ops-card">
        <h2>Açık Konular</h2>
        {openItems.filter((item) => showWorkforce || item.category !== "digital-workforce").length === 0 ? (<p className="shift-ops-empty">Bu vardiyada açık konu yok.</p>) : null}
        <ul className="shift-ops-item-list">
          {openItems.filter((item) => showWorkforce || item.category !== "digital-workforce").slice(0, 12).map((item) => (
            <li className="shift-ops-item" data-priority={item.handoverPriority} key={item.id}>
              <strong>{item.title}</strong>
              <p>
                {(OPEN_ITEM_CATEGORY_LABELS as Record<string, string | undefined>)[item.category] ?? enumLabel(item.category)}
                {" · "}
                {(HANDOVER_PRIORITY_LABELS as Record<string, string | undefined>)[item.handoverPriority] ?? enumLabel(item.handoverPriority)}
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
