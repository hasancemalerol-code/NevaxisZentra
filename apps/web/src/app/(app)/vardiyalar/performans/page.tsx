import Link from "next/link";
import { Activity } from "lucide-react";

import { EmptyPanel } from "@/components/ui/empty-panel";
import { readShiftOperationsWorkspace } from "@/features/shift-operations/server/shift-operations-reader";
import { formatPercent } from "@/lib/presentation/format";

export default async function VardiyalarPerformansPage() {
  const workspace = await readShiftOperationsWorkspace();
  const metrics = workspace.status === "success" ? workspace.metrics : null;
  const hasActivity = Boolean(
    metrics && (metrics.openItems > 0 || metrics.carriedOverItems > 0 || metrics.verifiedHandovers > 0 || metrics.repeatedCarryoverItems > 0),
  );

  return (
    <>
      {hasActivity ? (
      <div className="shift-ops-grid shift-ops-kpi-grid">
        <section className="shift-ops-card shift-ops-kpi-card"><h2>Açık konu</h2><p className="shift-ops-kpi-value">{metrics?.openItems ?? 0}</p></section>
        <section className="shift-ops-card shift-ops-kpi-card"><h2>Devredilen</h2><p className="shift-ops-kpi-value">{metrics?.carriedOverItems ?? 0}</p></section>
        <section className="shift-ops-card shift-ops-kpi-card"><h2>Doğrulanmış teslim</h2><p className="shift-ops-kpi-value">{metrics?.verifiedHandovers ?? 0}</p></section>
        <section className="shift-ops-card shift-ops-kpi-card"><h2>Tekrarlayan devir</h2><p className="shift-ops-kpi-value">{metrics?.repeatedCarryoverItems ?? 0}</p></section>
        <section className="shift-ops-card shift-ops-kpi-card"><h2>Teslim hazırlığı</h2><p className="shift-ops-kpi-value">{formatPercent(metrics?.handoverReadinessScore ?? 0, 0)}</p></section>
      </div>
      ) : null}
      {hasActivity ? null : (
        <EmptyPanel
          compact
          icon={<Activity />}
          title="Henüz ölçülecek vardiya hareketi yok"
          description="Göstergeler, vardiyalar çalıştıkça ve teslim edildikçe dolar."
          action={<Link className="shift-ops-cta" href="/vardiyalar/aktif">Aktif vardiyayı gör</Link>}
        />
      )}
    </>
  );
}
