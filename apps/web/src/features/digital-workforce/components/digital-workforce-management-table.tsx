"use client";

import Link from "next/link";
import { useMemo, useState } from "react";

import { useDigitalWorkforceRuntime } from "../application/digital-workforce-runtime";
import { DIGITAL_WORKERS } from "../data/digital-workforce-demo-catalog";
import {
  AUTONOMY_LABELS,
  AUTONOMY_MODE_ORDER,
  DIGITAL_WORKFORCE_COST_NOTE,
  DIGITAL_WORKFORCE_FULL_NOTE,
  STAGE_LABELS,
} from "../domain/digital-workforce-labels";
import type {
  DigitalAutonomyMode,
  DigitalWorkerDefinition,
  DigitalWorkerId,
  DigitalWorkerRuntimeStatus,
} from "../domain/digital-workforce-types";
import { AutonomyChips, DemoBadge, StatusChip } from "./digital-workforce-primitives";

type WorkerFilter =
  | "all"
  | "active"
  | "approval"
  | "intervention"
  | "paused"
  | DigitalAutonomyMode;

function matchesFilter(
  worker: DigitalWorkerDefinition,
  status: DigitalWorkerRuntimeStatus,
  mode: DigitalAutonomyMode,
  filter: WorkerFilter,
): boolean {
  if (filter === "all") return true;
  if (filter === "active") return status === "working";
  if (filter === "approval") return status === "awaiting-approval" || worker.approvalRequiredNow;
  if (filter === "intervention") return status === "needs-intervention";
  if (filter === "paused") return status === "paused";
  return mode === filter;
}

export function DigitalWorkforceManagementTable({
  onManagePermissions,
}: Readonly<{
  onManagePermissions: (id: DigitalWorkerId) => void;
}>) {
  const runtime = useDigitalWorkforceRuntime();
  const [filter, setFilter] = useState<WorkerFilter>("all");
  const [facility, setFacility] = useState("all");
  const [line, setLine] = useState("all");

  const rows = useMemo(
    () =>
      DIGITAL_WORKERS.filter((worker) => {
        const status = runtime.getWorkerStatus(worker.id, worker.defaultStatus);
        const mode = runtime.getWorkerMode(worker.id);
        if (!matchesFilter(worker, status, mode, filter)) return false;
        if (facility !== "all" && !worker.envelope.facilities.includes(facility)) return false;
        if (line !== "all" && !worker.envelope.lines.includes(line)) return false;
        return true;
      }),
    [facility, filter, line, runtime],
  );

  return (
    <section className="dwf-panel" aria-labelledby="dwf-table-title">
      <h2 id="dwf-table-title">Yönetim Tablosu</h2>
      <p>Operasyon yöneticisi her çalışanın durumunu, çıktısını ve yetki seviyesini buradan yönetir.</p>
      <div className="dwf-filters">
        {(
          [
            ["all", "Tüm Çalışanlar"],
            ["active", "Aktif"],
            ["approval", "Onay Bekleyen"],
            ["intervention", "Müdahale Gerektiren"],
            ["paused", "Duraklatılmış"],
            ["full", "Tam Otonom"],
            ["limited", "Sınırlı Otonom"],
            ["draft", "Öner"],
            ["observe", "İzle"],
          ] as const
        ).map(([key, label]) => (
          <button data-active={filter === key} key={key} onClick={() => setFilter(key)} type="button">
            {label}
          </button>
        ))}
        <select className="dwf-select" onChange={(event) => setFacility(event.target.value)} value={facility}>
          <option value="all">Fabrika: tümü</option>
          <option value="Pilot Enjeksiyon Tesisi">Pilot Enjeksiyon Tesisi</option>
        </select>
        <select className="dwf-select" onChange={(event) => setLine(event.target.value)} value={line}>
          <option value="all">Hat: tümü</option>
          <option value="Hat A — Enjeksiyon">Hat A — Enjeksiyon</option>
          <option value="Hat B — Montaj hazırlık">Hat B — Montaj hazırlık</option>
        </select>
        <span className="dwf-pill">Tarih: bugün (demo)</span>
        <span className="dwf-pill">İş türü: operasyon (demo)</span>
      </div>
      <div className="dwf-table-wrap">
        <table className="dwf-table">
          <thead>
            <tr>
              <th>Dijital Çalışan</th>
              <th>Durum</th>
              <th>Çalışma Modu</th>
              <th>Aktif İş</th>
              <th>Kuyruk</th>
              <th>Bugün Biten</th>
              <th>Bu Ay Biten</th>
              <th>Doğrulanmış Kapanış</th>
              <th>Bekleyen Onay</th>
              <th>Son Aktivite</th>
              <th>AI Maliyeti</th>
              <th>Yetki Durumu</th>
              <th>Aksiyon</th>
            </tr>
          </thead>
          <tbody>
            {rows.map((worker) => {
              const apiWorker = runtime.getWorker(worker.id);
              const status = runtime.getWorkerStatus(worker.id, worker.defaultStatus);
              const mode = runtime.getWorkerMode(worker.id);
              const activeLabel = apiWorker && apiWorker.activeCaseCount > 0
                ? `${apiWorker.activeCaseCount} aktif vaka`
                : worker.currentAssignment ?? "—";
              return (
                <tr key={worker.id}>
                  <td>
                    <strong>{worker.name.replace(" Dijital Çalışanı", "")}</strong>
                    <div className="dwf-meta">{worker.code}</div>
                  </td>
                  <td><StatusChip status={status} /></td>
                  <td>
                    <select
                      aria-label={`${worker.name} çalışma modu`}
                      onChange={(event) => runtime.setWorkerMode(worker.id, event.target.value as DigitalAutonomyMode)}
                      value={mode}
                    >
                      {AUTONOMY_MODE_ORDER.map((item) => (
                        <option key={item} value={item}>{AUTONOMY_LABELS[item]}</option>
                      ))}
                    </select>
                    {mode === "full" ? <small>{DIGITAL_WORKFORCE_FULL_NOTE}</small> : null}
                  </td>
                  <td>
                    <div>{activeLabel}</div>
                    <div className="dwf-meta">
                      Şu An Ne Yapıyor? {apiWorker?.isHumanControlled ? "İnsan kontrolünde" : activeLabel}
                    </div>
                    {apiWorker?.pendingApprovals ? <div className="dwf-meta">Onay gerekli</div> : null}
                  </td>
                  <td>{apiWorker?.queueCount ?? worker.queueCount}</td>
                  <td>{apiWorker?.completedToday ?? worker.kpis.completedToday}</td>
                  <td>{apiWorker?.completedThisMonth ?? worker.kpis.completedThisMonth}</td>
                  <td>{apiWorker?.verifiedClosures ?? worker.kpis.verifiedClosed}</td>
                  <td>{apiWorker?.pendingApprovals ?? worker.kpis.pendingApprovals}</td>
                  <td>
                    {apiWorker?.lastActivityAt
                      ? new Date(apiWorker.lastActivityAt).toLocaleString("tr-TR")
                      : worker.lastOutcome}
                  </td>
                  <td>
                    {apiWorker ? `₺${apiWorker.aiCostToday.toFixed(2)}` : worker.aiCostTodayLabel}
                    <div className="dwf-meta">{DIGITAL_WORKFORCE_COST_NOTE}</div>
                  </td>
                  <td>{apiWorker?.permissionStatus ?? "Politika sınırlı"}</td>
                  <td>
                    <div className="dwf-actions">
                      <Link className="dwf-btn dwf-btn--ghost" href={`/dijital-is-gucu/${worker.id}`}>Detay</Link>
                      <button className="dwf-btn dwf-btn--ghost" onClick={() => onManagePermissions(worker.id)} type="button">
                        Yetkileri Yönet
                      </button>
                      <button
                        className="dwf-btn dwf-btn--ghost"
                        onClick={() => {
                          const next = AUTONOMY_MODE_ORDER[(AUTONOMY_MODE_ORDER.indexOf(mode) + 1) % AUTONOMY_MODE_ORDER.length];
                          runtime.setWorkerMode(worker.id, next);
                        }}
                        type="button"
                      >
                        Mod Değiştir
                      </button>
                      <button className="dwf-btn dwf-btn--ghost" onClick={() => runtime.toggleWorkerPause(worker.id)} type="button">
                        {runtime.isPaused(worker.id) ? "Başlat" : "Duraklat"}
                      </button>
                    </div>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </section>
  );
}

export function DigitalWorkforcePermissionDrawer({
  worker,
  onClose,
}: Readonly<{
  worker: DigitalWorkerDefinition;
  onClose: () => void;
}>) {
  const runtime = useDigitalWorkforceRuntime();
  const mode = runtime.getWorkerMode(worker.id);

  return (
    <div className="dwf-drawer" role="dialog" aria-label={`${worker.name} yetkileri`}>
      <div className="dwf-drawer__panel dwf-drawer__panel--wide">
        <div className="dwf-row">
          <h2>Yetki ve Güvenlik Sınırları</h2>
          <button className="dwf-btn dwf-btn--ghost" onClick={onClose} type="button">Kapat</button>
        </div>
        <p>{worker.name}</p>
        <AutonomyChips value={mode} onChange={(next) => runtime.setWorkerMode(worker.id, next)} />
        {mode === "full" ? <p className="dwf-note">{DIGITAL_WORKFORCE_FULL_NOTE}</p> : null}
        <div className="dwf-kpi-grid" style={{ marginTop: "0.8rem" }}>
          <article className="dwf-kpi"><span>Fabrikalar</span><strong style={{ fontSize: "0.9rem" }}>{worker.envelope.facilities.join(", ")}</strong></article>
          <article className="dwf-kpi"><span>Tesisler</span><strong style={{ fontSize: "0.9rem" }}>{worker.envelope.sites.join(", ")}</strong></article>
          <article className="dwf-kpi"><span>Hatlar</span><strong style={{ fontSize: "0.9rem" }}>{worker.envelope.lines.join(", ")}</strong></article>
          <article className="dwf-kpi"><span>Makineler</span><strong style={{ fontSize: "0.9rem" }}>{worker.envelope.machines.join(", ")}</strong></article>
          <article className="dwf-kpi"><span>Modüller</span><strong style={{ fontSize: "0.9rem" }}>{worker.envelope.modules.join(", ")}</strong></article>
          <article className="dwf-kpi"><span>Entegrasyonlar</span><strong style={{ fontSize: "0.9rem" }}>{worker.envelope.integrations.join(", ")}</strong></article>
          <article className="dwf-kpi"><span>Okunabilir veri</span><strong style={{ fontSize: "0.9rem" }}>{worker.envelope.readableData.join(", ")}</strong></article>
          <article className="dwf-kpi"><span>Oluşturulabilir görev</span><strong style={{ fontSize: "0.9rem" }}>{worker.envelope.creatableTasks.join(", ")}</strong></article>
          <article className="dwf-kpi"><span>Hazırlanabilir taslak</span><strong style={{ fontSize: "0.9rem" }}>{worker.envelope.draftableItems.join(", ")}</strong></article>
          <article className="dwf-kpi"><span>Maks. tutar</span><strong>{worker.envelope.maxTransactionLabel}</strong></article>
          <article className="dwf-kpi"><span>Maks. risk</span><strong style={{ fontSize: "0.9rem" }}>{worker.envelope.maxRiskLabel}</strong></article>
          <article className="dwf-kpi"><span>Dış iletişim</span><strong style={{ fontSize: "0.9rem" }}>{worker.envelope.externalCommunication}</strong></article>
          <article className="dwf-kpi"><span>Çalışma saatleri</span><strong style={{ fontSize: "0.9rem" }}>{worker.envelope.workingHours}</strong></article>
          <article className="dwf-kpi"><span>AI bütçesi</span><strong>{worker.kpis.monthlyBudgetLabel}</strong></article>
        </div>
        <h3 style={{ marginTop: "0.9rem" }}>Makine Güvenliği</h3>
        <div className="dwf-kpi-grid">
          <article className="dwf-kpi"><span>PLC Yazma</span><strong>Kapalı</strong></article>
          <article className="dwf-kpi"><span>Makine Komutu</span><strong>Kapalı</strong></article>
          <article className="dwf-kpi"><span>Safety Override</span><strong>Yasak</strong></article>
        </div>
        <h3 style={{ marginTop: "0.9rem" }}>Yasak / onaylı işlemler</h3>
        <p>Yasak: {worker.envelope.forbiddenActions.join(" · ")}</p>
        <p>Onay gerekir: {worker.envelope.approvalRequiredActions.join(" · ")}</p>
        <p className="dwf-note">Bu panel sunum/politika görünümüdür. Ham politika JSON&apos;u gösterilmez.</p>
      </div>
    </div>
  );
}
