import { readShiftOperationsWorkspace } from "@/features/shift-operations/server/shift-operations-reader";

export default async function VardiyalarPerformansPage() {
  const workspace = await readShiftOperationsWorkspace();
  const metrics = workspace.status === "success" ? workspace.metrics : null;

  return (
    <>
      <header className="shift-ops-hero">
        <p className="shift-ops-hero__eyebrow">VARDİYA PERFORMANSI</p>
        <h1>Vardiya Performansı</h1>
      </header>
      <div className="shift-ops-grid">
        <section className="shift-ops-card"><h3>Açık konu</h3><p>{metrics?.openItems ?? 0}</p></section>
        <section className="shift-ops-card"><h3>Çözülen / devredilen</h3><p>{metrics?.carriedOverItems ?? 0}</p></section>
        <section className="shift-ops-card"><h3>Doğrulanmış teslim</h3><p>{metrics?.verifiedHandovers ?? 0}</p></section>
        <section className="shift-ops-card"><h3>Tekrarlayan devir</h3><p>{metrics?.repeatedCarryoverItems ?? 0}</p></section>
        <section className="shift-ops-card"><h3>Teslim hazırlığı</h3><p>{metrics?.handoverReadinessScore ?? 0}%</p></section>
      </div>
    </>
  );
}
