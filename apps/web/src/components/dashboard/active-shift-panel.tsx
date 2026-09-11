import Link from "next/link";

import { readActiveShiftPanel } from "@/features/shift-operations/server/shift-operations-reader";
import { SHIFT_STATUS_LABELS } from "@/features/shift-operations/domain/shift-operations-labels";
import "@/features/shift-operations/components/shift-operations-workspace.css";

export async function ActiveShiftDashboardPanel() {
  const panel = await readActiveShiftPanel();
  if (panel.status === "failure" || !panel.shift) {
    return null;
  }

  const end = new Date(panel.shift.plannedEndUtc);
  const remainingHours = Math.max(0, Math.round((end.getTime() - Date.now()) / 3_600_000 * 10) / 10);
  const nearEnd = remainingHours <= 2;

  return (
    <section className="dashboard-panel shift-ops-card">
      <p className="shift-ops-hero__eyebrow">AKTİF VARDİYA</p>
      <h2>{panel.shift.name}</h2>
      <p>
        {SHIFT_STATUS_LABELS[panel.shift.status as keyof typeof SHIFT_STATUS_LABELS] ?? panel.shift.status}
        {" · "}
        Kalan: ~{remainingHours} saat
      </p>
      <p>Kritik açık konu: {panel.metrics?.criticalItems ?? 0}</p>
      <p>Dijital iş gücü vaka: {panel.metrics?.digitalWorkforceCases ?? 0}</p>
      <p>Teslim hazırlığı: {panel.metrics?.handoverReadinessScore ?? panel.shift.handoverReadinessScore}%</p>
      <Link className="shift-ops-cta" href={nearEnd ? "/vardiyalar/teslim" : "/vardiyalar/aktif"}>
        {nearEnd ? "Vardiya Teslimini Hazırla" : "Aktif Vardiyayı Aç"}
      </Link>
    </section>
  );
}
