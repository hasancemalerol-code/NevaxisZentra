"use client";

import Link from "next/link";
import { useMemo, useState } from "react";

import { useDigitalWorkforceRuntime } from "../application/digital-workforce-runtime";
import {
  DIGITAL_ACTIVITY,
  DIGITAL_COST_CONTROL,
  DIGITAL_OUTCOMES,
  DIGITAL_WORK_CASES,
  DIGITAL_WORKERS,
  DIGITAL_WORKFORCE_KPIS,
  LIVE_INCIDENT_CASE_ID,
  VERIFIED_SHOWCASE_CASE_ID,
  getDigitalWorker,
} from "../data/digital-workforce-demo-catalog";
import {
  AUTONOMY_DESCRIPTIONS,
  AUTONOMY_EXPLAINER,
  AUTONOMY_LABELS,
  AUTONOMY_MODE_ORDER,
  DIGITAL_WORKFORCE_COST_NOTE,
  DIGITAL_WORKFORCE_FULL_NOTE,
  DIGITAL_WORKFORCE_SUBTITLE,
  DIGITAL_WORKFORCE_TITLE,
  DIGITAL_WORKFORCE_VERIFIED_CLOSED,
  STAGE_LABELS,
  WORKER_STATUS_LABELS,
} from "../domain/digital-workforce-labels";
import { resolveWorkerUiId } from "../domain/digital-workforce-api-ids";
import type { DigitalAutonomyMode, DigitalWorkerDefinition, DigitalWorkerId } from "../domain/digital-workforce-types";
import {
  DigitalWorkforceManagementTable,
  DigitalWorkforcePermissionDrawer,
} from "./digital-workforce-management-table";
import type { DigitalWorkforceControlCenterSnapshot } from "../server/digital-workforce-api-client";
import {
  AutonomyChips,
  DemoBadge,
  LifecycleTrack,
  StatusChip,
  VerifiedBadge,
  WorkCaseCard,
} from "./digital-workforce-primitives";

function buildKpiItems(snapshot: DigitalWorkforceControlCenterSnapshot | null) {
  if (!snapshot) {
    return [
      ["Toplam Dijital Çalışan", "7"],
      ["Aktif Çalışan", String(DIGITAL_WORKFORCE_KPIS.activeWorkers)],
      ["Devam Eden İş", String(DIGITAL_WORKFORCE_KPIS.activeCases)],
      ["Bekleyen Onay", String(DIGITAL_WORKFORCE_KPIS.pendingApprovals)],
      ["Bugün Tamamlanan", String(DIGITAL_WORKFORCE_KPIS.completedToday)],
      ["Doğrulanarak Kapatılan", String(DIGITAL_WORKFORCE_KPIS.verifiedClosures)],
      ["Müdahale Gerektiren", "1"],
      ["Tahmini Zaman Kazancı", DIGITAL_WORKFORCE_KPIS.estimatedTimeSaved],
      ["AI Maliyeti", DIGITAL_WORKFORCE_KPIS.aiCostToday],
    ] as const;
  }

  const activeWorkers = snapshot.workers.filter(
    (worker) => worker.status === "running" && !worker.isPaused && !worker.isKilled,
  ).length;
  const completedToday = snapshot.workers.reduce((sum, worker) => sum + worker.completedToday, 0);

  return [
    ["Toplam Dijital Çalışan", String(snapshot.workers.length)],
    ["Aktif Çalışan", String(activeWorkers)],
    ["Devam Eden İş", String(snapshot.openCases)],
    ["Kuyrukta Aksiyon", String(snapshot.queuedExecutions ?? 0)],
    ["Çalışan Aksiyon", String(snapshot.runningExecutions ?? 0)],
    ["Bekleyen Onay", String(snapshot.pendingApprovals)],
    ["Blocked", String(snapshot.blockedExecutions ?? 0)],
    ["Başarısız Aksiyon", String(snapshot.failedExecutions ?? 0)],
    ["Bugün Tamamlanan", String(completedToday)],
    ["Doğrulama Oranı", `${Math.round(snapshot.verificationRate * 100)}%`],
    ["AI Maliyeti", `₺${snapshot.totalAiCostToday.toFixed(2)}`],
  ] as const;
}

export function DigitalWorkforceControlCenterView() {
  const runtime = useDigitalWorkforceRuntime();
  const apiSnapshot = runtime.snapshot;
  const [selectedId, setSelectedId] = useState<DigitalWorkerId | null>(null);
  const [permissionId, setPermissionId] = useState<DigitalWorkerId | null>(null);
  const selected = selectedId ? getDigitalWorker(selectedId) : undefined;
  const permissionWorker = permissionId ? getDigitalWorker(permissionId) : undefined;
  const liveCase = DIGITAL_WORK_CASES.find((item) => item.id === LIVE_INCIDENT_CASE_ID);
  const verifiedCase = DIGITAL_WORK_CASES.find((item) => item.id === VERIFIED_SHOWCASE_CASE_ID);
  const pendingApprovals = runtime.approvals.filter((item) => item.status === "pending");
  const globalMode = runtime.globalMode;
  const kpiItems = useMemo(() => buildKpiItems(apiSnapshot), [apiSnapshot]);

  const activeFlowIds = useMemo(
    () => new Set(liveCase?.participatingWorkerIds ?? []),
    [liveCase],
  );

  return (
    <>
      <header className="fact-workspace__header">
        <div>
          <p className="fact-workspace__context">DİJİTAL İŞ GÜCÜ · OPERASYON KONTROL</p>
          <h1 className="fact-workspace__title">{DIGITAL_WORKFORCE_TITLE}</h1>
          <p className="fact-workspace__meta">{DIGITAL_WORKFORCE_SUBTITLE}</p>
        </div>
        <div className="fact-status-strip__item" style={{ minWidth: "96px" }}>
          <span className="fact-status-strip__label">Aktif çalışan</span>
          <strong className="fact-status-strip__value">7</strong>
        </div>
      </header>

      <section aria-label="Özet göstergeler" className="fact-status-strip">
        {kpiItems.map(([label, value]) => (
          <article className="fact-status-strip__item" key={label}>
            <span className="fact-status-strip__label">{label}</span>
            <strong className="fact-status-strip__value">{value}</strong>
          </article>
        ))}
      </section>

      <section className="dwf-panel" aria-labelledby="dwf-mode-title">
        <h2 id="dwf-mode-title">Genel Çalışma Modu</h2>
        <div className="dwf-mode" style={{ marginTop: "0.7rem" }}>
          {AUTONOMY_MODE_ORDER.map((mode) => (
            <button
              data-active={globalMode === mode}
              key={mode}
              onClick={() => runtime.setGlobalMode(mode)}
              type="button"
            >
              {AUTONOMY_LABELS[mode]}
              <small>{AUTONOMY_DESCRIPTIONS[mode]}</small>
            </button>
          ))}
        </div>
        <p className="dwf-note" style={{ marginTop: "0.7rem" }}>{DIGITAL_WORKFORCE_FULL_NOTE}</p>
        <div className="dwf-actions" style={{ marginTop: "0.7rem" }}>
          <button className="dwf-btn dwf-btn--ghost" onClick={runtime.startAll} type="button">Tümünü Başlat</button>
          <button className="dwf-btn dwf-btn--ghost" onClick={runtime.pauseAll} type="button">Tümünü Duraklat</button>
          <button className="dwf-btn dwf-btn--ghost" onClick={runtime.suggestModeAll} type="button">Öneri Moduna Al</button>
          <button className="dwf-btn dwf-btn--ghost" onClick={runtime.applyPolicies} type="button">Politikaları Uygula</button>
          <button className="dwf-btn dwf-btn--danger dwf-btn--ghost" onClick={runtime.emergencyStop} type="button">Acil Durdur</button>
        </div>
      </section>

      <section className="dwf-panel" aria-labelledby="dwf-explainer-title">
        <h2 id="dwf-explainer-title">Çalışma seviyeleri</h2>
        <div className="dwf-outcome-grid" style={{ marginTop: "0.65rem" }}>
          {AUTONOMY_MODE_ORDER.map((mode) => (
            <article className="dwf-kpi" key={mode}>
              <span>{AUTONOMY_LABELS[mode]}</span>
              <strong style={{ fontSize: "0.95rem" }}>{AUTONOMY_EXPLAINER[mode]}</strong>
            </article>
          ))}
        </div>
      </section>

      <section aria-labelledby="dwf-workers-title">
        <div className="dwf-panel" style={{ marginBottom: "0.55rem" }}>
          <h2 id="dwf-workers-title">Dijital Çalışanlar</h2>
          <p>Her kart bağımsız çalışma seviyesine sahiptir. Genel kontrol uygulanmadıkça bir çalışanın modu diğerlerini değiştirmez.</p>
        </div>
        <div className="dwf-card-grid">
          {DIGITAL_WORKERS.map((worker) => (
            <WorkerCard
              key={worker.id}
              onManagePermissions={() => setPermissionId(worker.id)}
              onOpen={() => setSelectedId(worker.id)}
              worker={worker}
            />
          ))}
        </div>
      </section>

      <DigitalWorkforceManagementTable onManagePermissions={setPermissionId} />

      <section className="dwf-split">
        <article className="dwf-rate">
          <span>Doğrulanmış Kapanış Oranı</span>
          <strong>%92</strong>
          <p>24 tamamlanan iş · 22 doğrulandı · 2 takipte</p>
          <p className="dwf-meta">İşi başlatmak değil, sonucu doğrulamak. Demo / tahmini.</p>
        </article>
        <section className="dwf-panel">
          <h2>Değer / maliyet</h2>
          <div className="dwf-kpi-grid">
            <article className="dwf-kpi"><span>AI maliyeti</span><strong>$18,40</strong><span>{DIGITAL_WORKFORCE_COST_NOTE}</span></article>
            <article className="dwf-kpi"><span>Tahmini zaman kazancı</span><strong>31,5 sa</strong><span>Tahmini / demo</span></article>
            <article className="dwf-kpi"><span>Doğrulanmış kapanış</span><strong>22</strong><span>Demo / tahmini</span></article>
          </div>
        </section>
      </section>

      <section className="dwf-panel">
        <h2>Dijital Çalışan Performansı</h2>
        <p>Oyunlaştırma yok; operasyon yöneticisi değeri görür.</p>
        <div className="dwf-table-wrap">
          <table className="dwf-table">
            <thead>
              <tr>
                <th>Çalışan</th>
                <th>Doğrulanmış kapanış</th>
                <th>Ort. kapanış</th>
                <th>Escalation</th>
                <th>Bekleyen iş</th>
                <th>Tahmini kazanç</th>
                <th>AI maliyeti</th>
              </tr>
            </thead>
            <tbody>
              {DIGITAL_WORKERS.map((worker) => (
                <tr key={worker.id}>
                  <td>{worker.name.replace(" Dijital Çalışanı", "")}</td>
                  <td>{worker.kpis.verifiedClosed}</td>
                  <td>{worker.kpis.averageClosureLabel}</td>
                  <td>{worker.kpis.escalations}</td>
                  <td>{worker.queueCount}</td>
                  <td>{worker.kpis.estimatedTimeSavedLabel}</td>
                  <td>{worker.kpis.aiCostTodayLabel}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>

      <section className="dwf-panel" aria-labelledby="dwf-flow-title">
        <h2 id="dwf-flow-title">Canlı Operasyon Akışı</h2>
        <p>Fabrika Operasyon Koordinatörü uzman çalışanları birleştirir; uzman mantığını kopyalamaz.</p>
        <div className="dwf-flow" style={{ marginTop: "0.7rem" }}>
          <div className="dwf-flow__hub">
            <strong>Fabrika Operasyon Koordinatörü</strong>
            <p>IM-07 zinciri: bakım çözümü doğrulanıyor, malzeme onayı bekleniyor.</p>
          </div>
          <div className="dwf-flow__nodes">
            {DIGITAL_WORKERS.filter((worker) => !worker.isCoordinator).map((worker) => (
              <article
                className="dwf-flow__node"
                data-active={activeFlowIds.has(worker.id)}
                key={worker.id}
              >
                <strong>{worker.name.replace(" Dijital Çalışanı", "")}</strong>
                <small>{WORKER_STATUS_LABELS[runtime.getWorkerStatus(worker.id, worker.defaultStatus)]}</small>
              </article>
            ))}
          </div>
          <div className="dwf-chain">
            <span className="dwf-pill">Üretim Kurtarma</span>
            <span className="dwf-meta">← bağımlılık →</span>
            <span className="dwf-pill">Bakım Çözüm</span>
            <span className="dwf-meta">← bağımlılık →</span>
            <span className="dwf-pill">Malzeme Açığı</span>
          </div>
        </div>
      </section>

      <div className="dwf-split">
        <section className="dwf-panel" aria-labelledby="dwf-live-title">
          <h2 id="dwf-live-title">Şu An Dijital İş Gücü</h2>
          {liveCase ? (
            <>
              <h3 style={{ marginTop: "0.65rem" }}>{liveCase.title}</h3>
              <p>3 Dijital Çalışan çalışıyor — bakım çözümü doğrulanıyor.</p>
              <LifecycleTrack completed={liveCase.lifecycleCompleted} current={liveCase.currentStage} />
              <div className="dwf-actions" style={{ marginTop: "0.7rem" }}>
                <Link className="dwf-btn dwf-btn--primary dwf-btn--ghost" href={`/dijital-is-gucu/${liveCase.primaryWorkerId}?case=${liveCase.id}`}>
                  Canlı Akışı Gör
                </Link>
              </div>
            </>
          ) : null}
        </section>
        <section className="dwf-panel" aria-labelledby="dwf-activity-title">
          <h2 id="dwf-activity-title">Canlı Aktivite</h2>
          <ol className="dwf-timeline" style={{ marginTop: "0.7rem" }}>
            {DIGITAL_ACTIVITY.slice(0, 6).map((event) => {
              const worker = getDigitalWorker(event.workerId);
              return (
                <li key={event.id}>
                  <strong>{event.timeLabel}</strong>
                  <div>
                    <span className="dwf-meta">{worker?.name}</span>
                    <p>{event.message}</p>
                  </div>
                </li>
              );
            })}
          </ol>
        </section>
      </div>

      <section className="dwf-panel" aria-labelledby="dwf-verified-title">
        <h2 id="dwf-verified-title">Doğrulanmış İş Kapanışı</h2>
        {verifiedCase ? (
          <div style={{ marginTop: "0.65rem" }}>
            <div className="dwf-inline">
              <VerifiedBadge />
              <span className="dwf-badge dwf-badge--verified">{DIGITAL_WORKFORCE_VERIFIED_CLOSED}</span>
              <DemoBadge />
            </div>
            <p><strong>Problem:</strong> {verifiedCase.problem}</p>
            <p><strong>Aksiyon:</strong> {verifiedCase.actions.join(" · ")}</p>
            <p><strong>Doğrulama:</strong> {verifiedCase.verification.join(" · ")}</p>
            <p><strong>Sonuç:</strong> {verifiedCase.result}</p>
          </div>
        ) : null}
      </section>

      <section className="dwf-panel" aria-labelledby="dwf-outcomes-title">
        <h2 id="dwf-outcomes-title">İş Sonuçları</h2>
        <div className="dwf-outcome-grid" style={{ marginTop: "0.65rem" }}>
          {DIGITAL_OUTCOMES.map((item) => (
            <article className="dwf-kpi" key={item.id}>
              <span>{item.label}</span>
              <strong>{item.value}</strong>
              <span>{item.detail}</span>
              {item.verified ? <VerifiedBadge /> : <DemoBadge label="Devam ediyor" />}
            </article>
          ))}
        </div>
      </section>

      <section className="dwf-panel" aria-labelledby="dwf-cost-title">
        <h2 id="dwf-cost-title">AI Kullanım ve Maliyet Kontrolü</h2>
        <p>{DIGITAL_WORKFORCE_COST_NOTE}. Gerçek müşteri tasarrufu iddia edilmez.</p>
        <div className="dwf-kpi-grid" style={{ marginTop: "0.65rem" }}>
          <article className="dwf-kpi"><span>Bugün</span><strong>{DIGITAL_COST_CONTROL.todayLabel}</strong></article>
          <article className="dwf-kpi"><span>Aylık tahmin</span><strong>{DIGITAL_COST_CONTROL.monthEstimateLabel}</strong></article>
          <article className="dwf-kpi"><span>Aylık bütçe</span><strong>{DIGITAL_COST_CONTROL.monthlyBudgetLabel}</strong></article>
          <article className="dwf-kpi"><span>Kullanım</span><strong>{DIGITAL_COST_CONTROL.budgetUsageLabel}</strong></article>
        </div>
      </section>

      <section className="dwf-panel" aria-labelledby="dwf-approval-title">
        <div className="dwf-row">
          <h2 id="dwf-approval-title">Bekleyen Onaylar</h2>
          <span className="dwf-status" data-tone="awaiting-approval">{pendingApprovals.length}</span>
        </div>
        {pendingApprovals.length === 0 ? <p>Bekleyen onay yok.</p> : null}
        {pendingApprovals.map((item) => (
          <article className="dwf-approval" key={item.id} style={{ marginTop: "0.7rem", padding: "0.8rem" }}>
            <h3>{item.action}</h3>
            <p><strong>Dijital Çalışan:</strong> {item.workerCode}</p>
            <p><strong>Neden:</strong> {item.businessReason}</p>
            <p><strong>Etki:</strong> {item.impact}</p>
            <p><strong>Risk:</strong> {item.riskLevel}</p>
            {item.amount !== null ? <p><strong>Tutar / limit:</strong> ₺{item.amount.toFixed(2)}</p> : null}
            <div className="dwf-actions" style={{ marginTop: "0.7rem" }}>
              <button className="dwf-btn" onClick={() => runtime.decideApproval(item.id, "approved", item.concurrencyToken)} type="button">Onayla</button>
              <button className="dwf-btn dwf-btn--ghost" onClick={() => runtime.decideApproval(item.id, "rejected", item.concurrencyToken)} type="button">Reddet</button>
              <button className="dwf-btn dwf-btn--ghost" onClick={() => runtime.decideApproval(item.id, "modified", item.concurrencyToken)} type="button">Değiştir</button>
              {resolveWorkerUiId(item.workerId) ? (
                <Link className="dwf-btn dwf-btn--ghost" href={`/dijital-is-gucu/${resolveWorkerUiId(item.workerId)}?case=${item.caseId}`}>Çalışanı aç</Link>
              ) : (
                <Link className="dwf-btn dwf-btn--ghost" href="/dijital-is-gucu/onaylar">Tüm onaylar</Link>
              )}
            </div>
          </article>
        ))}
      </section>

      {liveCase ? <WorkCaseCard item={liveCase} /> : null}

      {selected ? (
        <WorkerDrawer
          onClose={() => setSelectedId(null)}
          worker={selected}
        />
      ) : null}
      {permissionWorker ? (
        <DigitalWorkforcePermissionDrawer
          onClose={() => setPermissionId(null)}
          worker={permissionWorker}
        />
      ) : null}
    </>
  );
}

function WorkerCard({
  worker,
  onOpen,
  onManagePermissions,
}: Readonly<{
  worker: DigitalWorkerDefinition;
  onOpen: () => void;
  onManagePermissions: () => void;
}>) {
  const runtime = useDigitalWorkforceRuntime();
  const mode = runtime.getWorkerMode(worker.id);
  const status = runtime.getWorkerStatus(worker.id, worker.defaultStatus);

  return (
    <article className="dwf-card" onClick={onOpen} onKeyDown={(event) => event.key === "Enter" && onOpen()} role="button" tabIndex={0}>
      <div className="dwf-card__top">
        <div>
          <span className="dwf-meta">{worker.code}</span>
          <h3>{worker.name}</h3>
        </div>
        <StatusChip status={status} />
      </div>
      <p>{worker.mission}</p>
      <p><strong>Şu An Ne Yapıyor?</strong> {runtime.isPaused(worker.id) ? "Duraklatıldı" : worker.currentAssignment ?? "Kuyruk boş"}</p>
      <p className="dwf-meta">
        {worker.currentStage ? `Aşama: ${STAGE_LABELS[worker.currentStage]} · ` : ""}
        Başlangıç: {worker.startedAtLabel ?? "—"} · Sonraki: {worker.nextAction}
        {worker.approvalRequiredNow ? " · Onay gerekli" : ""}
      </p>
      <div
        onClick={(event) => event.stopPropagation()}
        onKeyDown={(event) => event.stopPropagation()}
        style={{ marginTop: "0.55rem" }}
      >
        <AutonomyChips value={mode} onChange={(next) => runtime.setWorkerMode(worker.id, next)} />
      </div>
      <div className="dwf-inline" style={{ marginTop: "0.5rem" }}>
        <span className="dwf-pill">{worker.facilityLabel}</span>
        {worker.lastOutcomeVerified ? <VerifiedBadge /> : null}
      </div>
      <div className="dwf-stats">
        <div><span>Kuyruk</span><strong>{worker.queueCount}</strong></div>
        <div><span>Onay</span><strong>{worker.pendingApprovalCount}</strong></div>
        <div><span>Doğrulanan</span><strong>{worker.kpis.verifiedClosed}</strong></div>
      </div>
      <p className="dwf-meta">Son sonuç: {worker.lastOutcome}</p>
      <p className="dwf-meta">AI maliyeti bugün: {worker.aiCostTodayLabel} · {DIGITAL_WORKFORCE_COST_NOTE}</p>
      <div className="dwf-actions" style={{ marginTop: "0.55rem" }} onClick={(event) => event.stopPropagation()}>
        <button className="dwf-btn dwf-btn--ghost" onClick={onManagePermissions} type="button">Yetkileri Yönet</button>
      </div>
    </article>
  );
}

function WorkerDrawer({
  worker,
  onClose,
}: Readonly<{
  worker: DigitalWorkerDefinition;
  onClose: () => void;
}>) {
  const runtime = useDigitalWorkforceRuntime();
  const mode = runtime.getWorkerMode(worker.id);
  const status = runtime.getWorkerStatus(worker.id, worker.defaultStatus);
  const currentCase = DIGITAL_WORK_CASES.find((item) => item.primaryWorkerId === worker.id && item.status !== "closed");

  return (
    <div className="dwf-drawer" role="dialog" aria-label={worker.name}>
      <div className="dwf-drawer__panel">
        <div className="dwf-row">
          <h2>{worker.name}</h2>
          <button className="dwf-btn dwf-btn--ghost" onClick={onClose} type="button">Kapat</button>
        </div>
        <div className="dwf-inline" style={{ margin: "0.6rem 0" }}>
          <StatusChip status={status} />
          <span className="dwf-pill">{AUTONOMY_LABELS[mode]}</span>
        </div>
        <p><strong>Şu an:</strong> {worker.currentAssignment ?? "Boşta"}</p>
        {worker.currentStage ? <p className="dwf-meta">Aşama: {STAGE_LABELS[worker.currentStage]}</p> : null}
        {currentCase ? <LifecycleTrack completed={currentCase.lifecycleCompleted} current={currentCase.currentStage} /> : null}
        <p style={{ marginTop: "0.7rem" }}>Son aktivite: {worker.lastOutcome}</p>
        <div style={{ marginTop: "0.8rem" }}>
          <AutonomyChips value={mode} onChange={(next: DigitalAutonomyMode) => runtime.setWorkerMode(worker.id, next)} />
        </div>
        <p className="dwf-meta" style={{ marginTop: "0.5rem" }}>Bekleyen onay: {worker.pendingApprovalCount}</p>
        <div className="dwf-actions" style={{ marginTop: "0.8rem" }}>
          <button className="dwf-btn" onClick={() => runtime.toggleWorkerPause(worker.id)} type="button">
            {runtime.isPaused(worker.id) ? "Başlat" : "Duraklat"}
          </button>
          <Link className="dwf-btn dwf-btn--primary dwf-btn--ghost" href={`/dijital-is-gucu/${worker.id}`}>
            Ayrıntıyı aç
          </Link>
        </div>
      </div>
    </div>
  );
}
