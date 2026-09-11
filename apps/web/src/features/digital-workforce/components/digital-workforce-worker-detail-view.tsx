"use client";

import Link from "next/link";
import { useMemo, useState } from "react";

import { useDigitalWorkforceRuntime } from "../application/digital-workforce-runtime";
import {
  DIGITAL_ACTIVITY,
  DIGITAL_APPROVALS,
  DIGITAL_WORK_CASES,
  casesForWorker,
  historyForWorker,
} from "../data/digital-workforce-demo-catalog";
import {
  APPROVAL_POLICY_LABELS,
  AUTONOMY_LABELS,
  DIGITAL_WORKFORCE_COST_NOTE,
  DIGITAL_WORKFORCE_FULL_NOTE,
  DIGITAL_WORKFORCE_MACHINE_SAFETY_NOTE,
  DIGITAL_WORKFORCE_VERIFIED_CLOSED,
  EVIDENCE_LABELS,
  STAGE_LABELS,
} from "../domain/digital-workforce-labels";
import type { DigitalWorkerDetailSnapshot } from "../server/digital-workforce-api-client";
import type { DigitalWorkerDefinition } from "../domain/digital-workforce-types";
import {
  AutonomyChips,
  DemoBadge,
  LifecycleTrack,
  StatusChip,
  VerifiedBadge,
  WorkCaseCard,
} from "./digital-workforce-primitives";

const TABS = [
  "genel",
  "yapiyor",
  "yapti",
  "isler",
  "tamamlanan",
  "yetkiler",
  "onaylar",
  "sonuclar",
  "aktivite",
  "maliyet",
] as const;

const TAB_LABELS = {
  genel: "Genel Bakış",
  yapiyor: "Ne Yapıyor?",
  yapti: "Ne Yaptı?",
  isler: "Aktif İşler",
  tamamlanan: "Tamamlanan İşler",
  yetkiler: "Yetkiler & Sınırlar",
  onaylar: "Onay Politikası",
  sonuclar: "Sonuçlar",
  aktivite: "Aktivite",
  maliyet: "Maliyet",
} as const;

export function DigitalWorkforceWorkerDetailView({
  worker,
  focusCaseId,
  apiDetail = null,
  apiError = null,
}: Readonly<{
  worker: DigitalWorkerDefinition;
  focusCaseId?: string | null;
  apiDetail?: DigitalWorkerDetailSnapshot | null;
  apiError?: string | null;
}>) {
  const runtime = useDigitalWorkforceRuntime();
  const [tab, setTab] = useState<(typeof TABS)[number]>(focusCaseId ? "isler" : "genel");
  const mode = runtime.getWorkerMode(worker.id);
  const status = runtime.getWorkerStatus(worker.id, worker.defaultStatus);
  const workerCases = useMemo(() => casesForWorker(worker.id), [worker.id]);
  const apiCurrentCase = apiDetail?.currentCase
    ?? apiDetail?.openCases.find((item) => item.id === focusCaseId)
    ?? apiDetail?.openCases[0]
    ?? null;
  const currentCase = workerCases.find((item) => item.id === focusCaseId)
    ?? workerCases.find((item) => item.status !== "closed")
    ?? workerCases[0];
  const closedCases = workerCases.filter((item) => item.status === "closed");
  const apiApprovals = apiDetail?.approvals ?? [];
  const demoApprovals = DIGITAL_APPROVALS.filter((item) => item.workerId === worker.id);
  const apiActivity = apiDetail?.activity ?? [];
  const demoActivity = DIGITAL_ACTIVITY.filter((item) => item.workerId === worker.id);
  const history = historyForWorker(worker.id);
  const historyItems = history.length > 0
    ? history
    : apiActivity.length > 0
      ? apiActivity.map((event) => ({
          id: event.id,
          timeLabel: new Date(event.createdAtUtc).toLocaleString("tr-TR"),
          caseTitle: apiCurrentCase?.title ?? worker.currentAssignment ?? "Operasyon",
          action: event.description,
          why: event.actionType,
          result: event.result,
          evidence: event.sourceClassification,
          statusLabel: event.sourceClassification === "demo" ? "Demo" : "Kayıt",
        }))
      : demoActivity.map((event) => ({
          id: event.id,
          timeLabel: event.timeLabel,
          caseTitle: worker.currentAssignment ?? "Operasyon",
          action: event.message,
          why: "Operasyonel tetik",
          result: "Kayıt",
          evidence: "Demo",
          statusLabel: "Demo",
        }));
  const takenOver = apiCurrentCase?.isHumanControlled ?? false;
  const displayStatus = takenOver ? "human-controlled" : status;

  return (
    <>
      {apiError ? <p className="dwf-api-error">{apiError}</p> : null}
      <header className="dwf-hero">
        <div>
          <p className="dwf-hero__eyebrow">{apiDetail?.code ?? worker.code}</p>
          <h1>{apiDetail?.name ?? worker.name}</h1>
          <p>{worker.isCoordinator ? "Dijital iş gücünün fabrika çapındaki koordinasyonunu yürütür." : worker.mission}</p>
          <div className="dwf-inline" style={{ marginTop: "0.55rem" }}>
            <StatusChip status={displayStatus} />
            <span className="dwf-pill">{AUTONOMY_LABELS[mode]}</span>
            <span className="dwf-pill">{worker.facilityLabel}</span>
            <DemoBadge />
          </div>
        </div>
        <div className="dwf-actions" style={{ alignContent: "start" }}>
          <button className="dwf-btn" onClick={() => runtime.toggleWorkerPause(worker.id)} type="button">
            {runtime.isPaused(worker.id) ? "Başlat" : "Duraklat"}
          </button>
          <Link className="dwf-btn dwf-btn--ghost" href="/dijital-is-gucu">Kontrol Merkezi</Link>
        </div>
      </header>

      <div className="dwf-tabs" role="tablist">
        {TABS.map((item) => (
          <button data-active={tab === item} key={item} onClick={() => setTab(item)} type="button">
            {TAB_LABELS[item]}
          </button>
        ))}
      </div>

      {tab === "genel" ? (
        <section className="dwf-panel">
          <h2>Şu An Ne Yapıyor?</h2>
          <p>
            {runtime.isPaused(worker.id)
              ? "Duraklatıldı — yeni uygulama yok."
              : apiCurrentCase?.title ?? worker.currentAssignment ?? "Kuyruk boş."}
          </p>
          {apiCurrentCase ? <p className="dwf-meta">Aşama: {STAGE_LABELS[apiCurrentCase.currentStage as keyof typeof STAGE_LABELS] ?? apiCurrentCase.currentStage}</p> : null}
          {worker.currentStage ? <p className="dwf-meta">Aşama: {STAGE_LABELS[worker.currentStage]}</p> : null}
          {currentCase ? (
            <>
              <LifecycleTrack completed={currentCase.lifecycleCompleted} current={currentCase.currentStage} />
              {currentCase.handoffs.map((handoff) => (
                <p key={handoff}><strong>Handoff:</strong> {handoff}</p>
              ))}
              {currentCase.escalation ? (
                <div className="dwf-banner dwf-banner--paused" style={{ marginTop: "0.7rem" }}>
                  <strong>Müdahale Gerekiyor</strong>
                  <p>Neden: {currentCase.escalation.reason}</p>
                  <p>Risk: {currentCase.escalation.risk}</p>
                  <p>Denenen: {currentCase.escalation.tried}</p>
                  <p>Karar: {currentCase.escalation.decisionNeeded}</p>
                  <p>Öneri: {currentCase.escalation.recommended}</p>
                  <div className="dwf-actions" style={{ marginTop: "0.55rem" }}>
                    <Link className="dwf-btn dwf-btn--primary dwf-btn--ghost" href="/dijital-is-gucu/onaylar">Onay Merkezi&apos;ne Git</Link>
                  </div>
                </div>
              ) : null}
              {(apiCurrentCase ?? currentCase) && (apiCurrentCase?.status ?? currentCase?.status) !== "closed" ? (
                <div className="dwf-actions" style={{ marginTop: "0.7rem" }}>
                  <button
                    className="dwf-btn"
                    onClick={() => runtime.takeOverCase(apiCurrentCase?.id ?? focusCaseId ?? "")}
                    type="button"
                  >
                    İşi Devral
                  </button>
                  {takenOver ? (
                    <button
                      className="dwf-btn dwf-btn--ghost"
                      onClick={() => runtime.resumeHumanControl(apiCurrentCase?.id ?? focusCaseId ?? "")}
                      type="button"
                    >
                      Otonomiye Devam
                    </button>
                  ) : null}
                  <button className="dwf-btn dwf-btn--ghost" onClick={() => runtime.toggleWorkerPause(worker.id)} type="button">
                    {runtime.isPaused(worker.id) ? "Devam Ettir" : "Duraklat"}
                  </button>
                  <Link className="dwf-btn dwf-btn--ghost" href="/dijital-is-gucu/onaylar">Escalate</Link>
                  <button className="dwf-btn dwf-btn--ghost" onClick={() => setTab("onaylar")} type="button">
                    Onay Politikası Değiştir
                  </button>
                  {takenOver ? <span className="dwf-status" data-tone="human-controlled">İnsan Kontrolünde</span> : null}
                </div>
              ) : null}
            </>
          ) : null}
          <div style={{ marginTop: "1rem" }}>
            <h3>Bağımsız çalışma seviyesi</h3>
            <div style={{ marginTop: "0.55rem" }}>
              <AutonomyChips value={mode} onChange={(next) => runtime.setWorkerMode(worker.id, next)} />
            </div>
            {mode === "full" ? <p className="dwf-note" style={{ marginTop: "0.65rem" }}>{DIGITAL_WORKFORCE_FULL_NOTE}</p> : null}
          </div>
        </section>
      ) : null}

      {tab === "yapiyor" ? (
        <section className="dwf-panel">
          <h2>Şu An Ne Yapıyor?</h2>
          <p>{runtime.isPaused(worker.id) ? "Duraklatıldı." : worker.currentAssignment ?? "Boşta."}</p>
          <p className="dwf-meta">Başlangıç: {worker.startedAtLabel ?? "—"} · Sonraki aksiyon: {worker.nextAction}</p>
          <p className="dwf-meta">{worker.approvalRequiredNow ? "Onay gerekli" : "Şu an onay beklenmiyor"}</p>
          {currentCase ? <LifecycleTrack completed={currentCase.lifecycleCompleted} current={currentCase.currentStage} /> : null}
        </section>
      ) : null}

      {tab === "yapti" ? (
        <section className="dwf-panel">
          <h2>Ne Yaptı?</h2>
          <ol className="dwf-timeline" style={{ marginTop: "0.7rem" }}>
            {historyItems.map((item) => (
              <li key={item.id}>
                <strong>{item.timeLabel}</strong>
                <div>
                  <p><strong>{item.action}</strong></p>
                  <p className="dwf-meta">{item.caseTitle} · Neden: {item.why}</p>
                  <p className="dwf-meta">Sonuç: {item.result} · Kanıt: {item.evidence} · {item.statusLabel}</p>
                </div>
              </li>
            ))}
          </ol>
        </section>
      ) : null}

      {tab === "tamamlanan" ? (
        <section className="dwf-list">
          {closedCases.map((item) => <WorkCaseCard item={item} key={item.id} />)}
          {closedCases.length === 0 ? <p className="dwf-note">Bu çalışan için kapanmış iş yok.</p> : null}
        </section>
      ) : null}

      {tab === "isler" ? (
        <section className="dwf-list">
          {(apiDetail?.openCases.length ? apiDetail.openCases.map((item) => (
            <article className="dwf-case" key={item.id}>
              <h3>{item.title}</h3>
              <p>{item.description}</p>
              <p className="dwf-meta">Durum: {item.status} · Aşama: {item.currentStage}</p>
              <div className="dwf-actions" style={{ marginTop: "0.65rem" }}>
                <button className="dwf-btn dwf-btn--ghost" onClick={() => runtime.takeOverCase(item.id)} type="button">
                  İşi Devral
                </button>
              </div>
            </article>
          )) : workerCases.filter((item) => item.status !== "closed").map((item) => (
            <WorkCaseCard
              action={
                item.status !== "closed" ? (
                  <div className="dwf-actions" style={{ marginTop: "0.65rem" }}>
                    <button className="dwf-btn dwf-btn--ghost" onClick={() => runtime.takeOverCase(item.id)} type="button">
                      İşi Devral
                    </button>
                  </div>
                ) : null
              }
              item={item}
              key={item.id}
            />
          )))}
        </section>
      ) : null}

      {tab === "yetkiler" ? (
        <section className="dwf-panel" id="yetki-ve-guvenlik">
          <h2>Yetki ve Güvenlik Sınırları</h2>
          <p>Teknik politika JSON&apos;u gösterilmez. Tam otonom bile {DIGITAL_WORKFORCE_MACHINE_SAFETY_NOTE.toLocaleLowerCase("tr-TR")}</p>
          <div className="dwf-kpi-grid" style={{ marginTop: "0.7rem" }}>
            <article className="dwf-kpi"><span>Erişebileceği fabrikalar</span><strong style={{ fontSize: "0.95rem" }}>{worker.envelope.facilities.join(", ")}</strong></article>
            <article className="dwf-kpi"><span>Erişebileceği tesisler</span><strong style={{ fontSize: "0.95rem" }}>{worker.envelope.sites.join(", ")}</strong></article>
            <article className="dwf-kpi"><span>Erişebileceği hatlar</span><strong style={{ fontSize: "0.95rem" }}>{worker.envelope.lines.join(", ")}</strong></article>
            <article className="dwf-kpi"><span>Erişebileceği makineler</span><strong style={{ fontSize: "0.95rem" }}>{worker.envelope.machines.join(", ")}</strong></article>
            <article className="dwf-kpi"><span>Erişebileceği modüller</span><strong style={{ fontSize: "0.95rem" }}>{worker.envelope.modules.join(", ")}</strong></article>
            <article className="dwf-kpi"><span>Erişebileceği entegrasyonlar</span><strong style={{ fontSize: "0.95rem" }}>{worker.envelope.integrations.join(", ")}</strong></article>
            <article className="dwf-kpi"><span>Okuyabileceği veriler</span><strong style={{ fontSize: "0.95rem" }}>{worker.envelope.readableData.join(", ")}</strong></article>
            <article className="dwf-kpi"><span>Oluşturabileceği görevler</span><strong style={{ fontSize: "0.95rem" }}>{worker.envelope.creatableTasks.join(", ")}</strong></article>
            <article className="dwf-kpi"><span>Hazırlayabileceği taslaklar</span><strong style={{ fontSize: "0.95rem" }}>{worker.envelope.draftableItems.join(", ")}</strong></article>
            <article className="dwf-kpi"><span>AI bütçesi</span><strong>{worker.kpis.monthlyBudgetLabel}</strong></article>
            <article className="dwf-kpi"><span>Maksimum işlem tutarı</span><strong>{worker.envelope.maxTransactionLabel}</strong></article>
            <article className="dwf-kpi"><span>Maksimum risk</span><strong style={{ fontSize: "0.95rem" }}>{worker.envelope.maxRiskLabel}</strong></article>
            <article className="dwf-kpi"><span>Dış iletişim</span><strong style={{ fontSize: "0.95rem" }}>{worker.envelope.externalCommunication}</strong></article>
            <article className="dwf-kpi"><span>Çalışma saatleri</span><strong style={{ fontSize: "0.95rem" }}>{worker.envelope.workingHours}</strong></article>
            <article className="dwf-kpi"><span>Yükseltme</span><strong style={{ fontSize: "0.95rem" }}>{worker.envelope.escalationRule}</strong></article>
          </div>
          <h3 style={{ marginTop: "1rem" }}>Makine Güvenliği</h3>
          <div className="dwf-kpi-grid">
            <article className="dwf-kpi"><span>PLC Yazma</span><strong>Kapalı</strong></article>
            <article className="dwf-kpi"><span>Makine Komutu</span><strong>Kapalı</strong></article>
            <article className="dwf-kpi"><span>Safety Override</span><strong>Yasak</strong></article>
          </div>
          <p className="dwf-note" style={{ marginTop: "0.65rem" }}>{DIGITAL_WORKFORCE_MACHINE_SAFETY_NOTE}</p>
          <h3 style={{ marginTop: "1rem" }}>Sistem erişimi</h3>
          <ul className="dwf-access">
            {worker.envelope.systems.map((system) => (
              <li data-allowed={system.allowed} key={system.label}>
                <span>{system.label}</span>
                <strong>{system.allowed ? "İzinli" : "Kapalı"}</strong>
              </li>
            ))}
          </ul>
          <h3 style={{ marginTop: "1rem" }}>Yapabileceği işlemler</h3>
          <p>{worker.envelope.allowedActions.join(" · ")}</p>
          <h3 style={{ marginTop: "0.7rem" }}>Yasak işlemler</h3>
          <p>{worker.envelope.forbiddenActions.join(" · ")}</p>
          <h3 style={{ marginTop: "1rem" }}>Onay Gerektiren Aksiyonlar</h3>
          <ul className="dwf-list">
            {worker.envelope.approvalRules.map((rule) => (
              <li className="dwf-case" key={rule.action} style={{ padding: "0.7rem" }}>
                <strong>{rule.action}</strong>
                <p>{APPROVAL_POLICY_LABELS[rule.policy]}{rule.thresholdLabel ? ` · ${rule.thresholdLabel}` : ""}</p>
              </li>
            ))}
          </ul>
        </section>
      ) : null}

      {tab === "onaylar" ? (
        <section className="dwf-panel">
          <h2>Onaylar</h2>
          {apiApprovals.length === 0 && demoApprovals.length === 0 ? <p>Bu çalışana ait bekleyen onay yok.</p> : null}
          {apiApprovals.map((item) => (
            <article className="dwf-approval" key={item.id} style={{ marginTop: "0.7rem", padding: "0.8rem" }}>
              <h3>{item.action}</h3>
              <p>{item.businessReason}</p>
              <div className="dwf-actions" style={{ marginTop: "0.6rem" }}>
                <button className="dwf-btn" onClick={() => runtime.decideApproval(item.id, "approved", item.concurrencyToken)} type="button">Onayla</button>
                <button className="dwf-btn dwf-btn--ghost" onClick={() => runtime.decideApproval(item.id, "rejected", item.concurrencyToken)} type="button">Reddet</button>
              </div>
            </article>
          ))}
          {apiApprovals.length === 0 ? demoApprovals.map((item) => (
            <article className="dwf-approval" key={item.id} style={{ marginTop: "0.7rem", padding: "0.8rem" }}>
              <h3>{item.action}</h3>
              <p>{item.reason}</p>
              <Link className="dwf-btn dwf-btn--primary dwf-btn--ghost" href="/dijital-is-gucu/onaylar" style={{ marginTop: "0.6rem", display: "inline-flex" }}>
                Karar Ver
              </Link>
            </article>
          )) : null}
        </section>
      ) : null}

      {tab === "sonuclar" ? (
        <section className="dwf-panel">
          <h2>Doğrulanmış İş Kapanışı</h2>
          {closedCases.map((item) => (
            <article key={item.id} style={{ marginTop: "0.8rem" }}>
              {item.verified ? <span className="dwf-badge dwf-badge--verified">{DIGITAL_WORKFORCE_VERIFIED_CLOSED}</span> : <DemoBadge />}
              <p><strong>Problem:</strong> {item.problem}</p>
              <p><strong>Aksiyon:</strong> {item.actions.join(" · ")}</p>
              <p><strong>Doğrulama:</strong> {item.verification.join(" · ") || "—"}</p>
              <p><strong>Sonuç:</strong> {item.result ?? item.outcome}</p>
              <h3 style={{ marginTop: "0.7rem" }}>Kanıt</h3>
              <ul className="dwf-access">
                {item.evidence.map((evidence) => (
                  <li key={evidence.label}>
                    <span>{EVIDENCE_LABELS[evidence.kind]}</span>
                    <strong>{evidence.label}</strong>
                  </li>
                ))}
              </ul>
            </article>
          ))}
          {worker.kpis.verifiedClosed === 0 && closedCases.length === 0 ? (
            <p>Bu çalışan için doğrulanmış kapanış henüz yok.</p>
          ) : null}
        </section>
      ) : null}

      {tab === "aktivite" ? (
        <section className="dwf-panel">
          <h2>Aktivite</h2>
          <ol className="dwf-timeline" style={{ marginTop: "0.7rem" }}>
            {(apiActivity.length > 0 ? apiActivity.map((event) => (
              <li key={event.id}>
                <strong>{new Date(event.createdAtUtc).toLocaleString("tr-TR")}</strong>
                <p>{event.description}</p>
                <p className="dwf-meta">{event.actionType} · {event.result}</p>
              </li>
            )) : demoActivity.map((event) => (
              <li key={event.id}>
                <strong>{event.timeLabel}</strong>
                <p>{event.message}</p>
              </li>
            )))}
          </ol>
        </section>
      ) : null}

      {tab === "maliyet" ? (
        <section className="dwf-panel">
          <h2>AI Kullanım ve Maliyet Kontrolü</h2>
          <p>{DIGITAL_WORKFORCE_COST_NOTE}</p>
          <div className="dwf-kpi-grid" style={{ marginTop: "0.7rem" }}>
            <article className="dwf-kpi"><span>Bugün ele aldığı işler</span><strong>{worker.kpis.handledToday}</strong></article>
            <article className="dwf-kpi"><span>Bugün tamamladığı iş</span><strong>{worker.kpis.completedToday}</strong></article>
            <article className="dwf-kpi"><span>Bu hafta</span><strong>{worker.kpis.completedThisWeek}</strong></article>
            <article className="dwf-kpi"><span>Bu ay</span><strong>{worker.kpis.completedThisMonth}</strong></article>
            <article className="dwf-kpi"><span>Başarı oranı</span><strong>{worker.kpis.successRateLabel}</strong></article>
            <article className="dwf-kpi"><span>Doğrulanarak kapatılan</span><strong>{worker.kpis.verifiedClosed}</strong></article>
            <article className="dwf-kpi"><span>Escalation</span><strong>{worker.kpis.escalations}</strong></article>
            <article className="dwf-kpi"><span>Bekleyen onay</span><strong>{worker.kpis.pendingApprovals}</strong></article>
            <article className="dwf-kpi"><span>Ortalama kapanış</span><strong style={{ fontSize: "1rem" }}>{worker.kpis.averageClosureLabel}</strong></article>
            <article className="dwf-kpi"><span>Tahmini zaman kazancı</span><strong style={{ fontSize: "1rem" }}>{worker.kpis.estimatedTimeSavedLabel}</strong></article>
            <article className="dwf-kpi"><span>Bugün</span><strong>{worker.kpis.aiCostTodayLabel}</strong></article>
            <article className="dwf-kpi"><span>Bu ay</span><strong>{worker.kpis.aiCostMonthLabel}</strong></article>
            <article className="dwf-kpi"><span>İş başına</span><strong>{worker.kpis.aiCostPerCaseLabel}</strong></article>
            <article className="dwf-kpi"><span>Aylık AI bütçesi</span><strong>{worker.kpis.monthlyBudgetLabel}</strong></article>
            <article className="dwf-kpi"><span>Bütçe durumu</span><strong>{worker.kpis.budgetState === "normal" ? "Normal" : worker.kpis.budgetState === "near-limit" ? "Sınıra yakın" : "Limit doldu"}</strong></article>
          </div>
          <p className="dwf-note" style={{ marginTop: "0.7rem" }}>
            Limit seçenekleri: Limitsiz, $10, $25, $50, $100, özel. Bu paket yalnızca politika/görünüm temelidir; üretim sürecini kesmez.
          </p>
        </section>
      ) : null}

      {worker.isCoordinator ? <CoordinatorPanel /> : null}
    </>
  );
}

function CoordinatorPanel() {
  const openCases = DIGITAL_WORK_CASES.filter((item) => item.status !== "closed");
  const blockers = DIGITAL_WORK_CASES.filter((item) => item.status === "awaiting-approval" || item.status === "escalation");

  return (
    <section className="dwf-panel">
      <h2>Koordinasyon görünümü</h2>
      <p>Uzman çalışanların işini kopyalamaz; bağımlılık, tıkanma ve uçtan uca kapanışı izler.</p>
      <div className="dwf-kpi-grid" style={{ marginTop: "0.7rem" }}>
        <article className="dwf-kpi"><span>Çok alanlı olaylar</span><strong>{openCases.filter((item) => item.participatingWorkerIds.length > 1).length}</strong></article>
        <article className="dwf-kpi"><span>Bekleyen karar</span><strong>{blockers.length}</strong></article>
        <article className="dwf-kpi"><span>Açık el değiştirme</span><strong>2</strong></article>
        <article className="dwf-kpi"><span>Çapraz kapanış</span><strong>1 / 3</strong><span>Demo</span></article>
      </div>
      <ul className="dwf-list">
        {openCases.map((item) => (
          <li className="dwf-case" key={item.id} style={{ padding: "0.75rem" }}>
            <strong>{item.title}</strong>
            <p>Birincil: {item.primaryWorkerId} · Katılımcılar: {item.participatingWorkerIds.length}</p>
            <p>{item.handoffs[0] ?? "Bağımlılık izleniyor."}</p>
          </li>
        ))}
      </ul>
    </section>
  );
}
