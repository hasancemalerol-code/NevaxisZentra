"use client";

import Link from "next/link";
import { useState, useTransition } from "react";
import {
  ArrowUpRight,
  CheckCircle2,
  LoaderCircle,
  RefreshCw,
  ShieldAlert,
  Users,
} from "lucide-react";

import { getExecutiveEmptyStateMessage } from "../application/executive-command-center-builder";
import {
  EXECUTIVE_COMMAND_CENTER_PARTIAL_NOTE,
  EXECUTIVE_COMMAND_CENTER_SUBTITLE,
  EXECUTIVE_COMMAND_CENTER_TITLE,
  EXECUTIVE_PRIORITY_SECTION_LABELS,
} from "../domain/executive-command-center-labels";
import { refreshExecutiveCommandCenterAction } from "../server/executive-command-center-actions";
import type {
  ExecutiveCommandCenterWorkspacePayload,
  ExecutiveOutcomeCard,
  ExecutivePriorityItem,
} from "../types/executive-command-center-types";
import { PresentationModeReturnLink } from "@/features/presentation-mode/components/presentation-mode-return-link";

import "@/features/commercial-intelligence/components/commercial-intelligence-workspace.css";
import "./executive-command-center.css";

type ExecutiveCommandCenterViewProps = Readonly<{
  workspace: ExecutiveCommandCenterWorkspacePayload;
}>;

function OutcomeCard({ card }: Readonly<{ card: ExecutiveOutcomeCard }>) {
  return (
    <article
      className={`commint-kpi-card commint-kpi-card__tone--${card.tone}`}
      data-testid={`executive-outcome-${card.id}`}
    >
      <p className="commint-kpi-card__label">{card.label}</p>
      <p className="commint-kpi-card__value">{card.value}</p>
      <p className="commint-kpi-card__detail">{card.detail}</p>
      <Link href={card.drillDownHref} className="commint-table__link" style={{ marginTop: "0.5rem", display: "inline-flex", gap: "0.25rem" }}>
        {card.drillDownLabel}
        <ArrowUpRight aria-hidden="true" size={14} />
      </Link>
    </article>
  );
}

function PriorityItem({ item }: Readonly<{ item: ExecutivePriorityItem }>) {
  return (
    <article className="executive-stack-item" data-testid={`executive-priority-${item.id}`}>
      <div className="commint-panel__header" style={{ marginBottom: 0 }}>
        <div>
          <p className="commint-executive__eyebrow">{EXECUTIVE_PRIORITY_SECTION_LABELS[item.section]}</p>
          <h3 style={{ margin: 0, fontSize: "1rem" }}>{item.outcome}</h3>
        </div>
        <span className="commint-kpi-card__badge">{item.status}</span>
      </div>
      <p className="commint-kpi-card__detail">{item.impact}</p>
      <div className="executive-stack-item__meta">
        <span>Sahip: {item.owner}</span>
        <span>Sonraki: {item.nextAction}</span>
        {item.timeToImpact ? <span>Etki: {item.timeToImpact}</span> : null}
        <span>Doğrulama: {item.verificationState}</span>
      </div>
      <Link href={item.drillDownHref} className="commint-table__link">
        Detay
        <ArrowUpRight aria-hidden="true" size={14} />
      </Link>
    </article>
  );
}

export function ExecutiveCommandCenterView({ workspace: initialWorkspace }: ExecutiveCommandCenterViewProps) {
  const [workspace, setWorkspace] = useState(initialWorkspace);
  const [message, setMessage] = useState<string | null>(null);
  const [isPending, startTransition] = useTransition();

  function handleRefresh() {
    setMessage(null);
    startTransition(async () => {
      const result = await refreshExecutiveCommandCenterAction();
      if (result.status === "failure") {
        setMessage(result.message);
        return;
      }
      setWorkspace(result.workspace);
    });
  }

  const risksEmpty = getExecutiveEmptyStateMessage(workspace, "risks");
  const approvalsEmpty = getExecutiveEmptyStateMessage(workspace, "approvals");
  const verifiedEmpty = getExecutiveEmptyStateMessage(workspace, "verified");

  const sections = [
    "critical-now",
    "decision-today",
    "active-recovery",
    "pending-verification",
    "verified-today",
  ] as const;

  return (
    <main className="commint-page" data-testid="executive-command-center-view">
      <PresentationModeReturnLink />
      <header className="commint-executive">
        <div className="commint-executive__intro">
          <p className="commint-executive__eyebrow">YÖNETİCİ KOMUTA KATMANI</p>
          <h1>{EXECUTIVE_COMMAND_CENTER_TITLE}</h1>
          <p className="commint-executive__subtitle">{EXECUTIVE_COMMAND_CENTER_SUBTITLE}</p>
        </div>
        <div className="commint-executive__actions">
          <button
            type="button"
            className="commint-button commint-button--ghost"
            disabled={isPending}
            onClick={handleRefresh}
            data-testid="executive-command-center-refresh"
          >
            {isPending ? <LoaderCircle aria-hidden="true" size={16} className="commint-spin" /> : <RefreshCw aria-hidden="true" size={16} />}
            Yenile
          </button>
        </div>
      </header>

      {message ? <div className="commint-error-banner" role="alert">{message}</div> : null}
      {workspace.scopeState === "partial" ? (
        <div className="commint-error-banner" style={{ background: "rgba(59, 130, 246, 0.08)" }}>
          {EXECUTIVE_COMMAND_CENTER_PARTIAL_NOTE}
        </div>
      ) : null}

      <section className="executive-narrative" data-testid="executive-narrative">
        {workspace.narrative}
      </section>

      <section style={{ marginTop: "1.5rem" }}>
        <h2 className="executive-section-title">Outcome Özeti</h2>
        <div className="commint-kpi-grid" data-testid="executive-outcome-cards">
          {workspace.outcomeCards.map((card) => <OutcomeCard key={card.id} card={card} />)}
        </div>
      </section>

      <section className="executive-command-grid" style={{ marginTop: "1.5rem" }}>
        <div>
          <h2 className="executive-section-title">Öncelik Yığını</h2>
          <div className="executive-stack" data-testid="executive-priority-stack">
            {workspace.priorityStack.length === 0 ? (
              <p className="commint-kpi-card__detail">{risksEmpty}</p>
            ) : (
              sections.flatMap((section) =>
                workspace.priorityStack
                  .filter((item) => item.section === section)
                  .slice(0, 3)
                  .map((item) => <PriorityItem key={item.id} item={item} />),
              )
            )}
          </div>
        </div>

        <div>
          <h2 className="executive-section-title">Öngörülen Kritik Riskler</h2>
          <div className="executive-stack" data-testid="executive-predictive-risks">
            {workspace.predictiveRisks.length === 0 ? (
              <p className="commint-kpi-card__detail">{risksEmpty}</p>
            ) : (
              workspace.predictiveRisks.map((risk) => (
                <article key={risk.id} className="executive-stack-item" data-testid={`executive-risk-${risk.id}`}>
                  <p className="commint-executive__eyebrow">{risk.machineCode}{risk.orderReference ? ` · ${risk.orderReference}` : ""}</p>
                  <p className="commint-kpi-card__detail">{risk.observedSummary}</p>
                  <p><strong>Öngörü:</strong> {risk.predictedSummary}</p>
                  <div className="executive-stack-item__meta">
                    <span>Ufuk: {risk.horizon}</span>
                    <span>Güven: {risk.confidenceBand}</span>
                  </div>
                  <p className="commint-kpi-card__detail">{risk.recommendedAction}</p>
                  <Link href={risk.drillDownHref} className="commint-table__link">
                    Risk detayı
                    <ArrowUpRight aria-hidden="true" size={14} />
                  </Link>
                </article>
              ))
            )}
          </div>
        </div>

        <div>
          <h2 className="executive-section-title">İnsan Kararları</h2>
          <div className="executive-stack" data-testid="executive-approvals">
            {workspace.approvals.length === 0 ? (
              <p className="commint-kpi-card__detail">{approvalsEmpty}</p>
            ) : (
              workspace.approvals.slice(0, 5).map((approval) => (
                <article key={approval.id} className="executive-stack-item">
                  <p className="commint-executive__eyebrow">{approval.urgency}</p>
                  <h3 style={{ margin: 0, fontSize: "1rem" }}>{approval.decision}</h3>
                  <p className="commint-kpi-card__detail">{approval.businessOutcome}</p>
                  <div className="executive-stack-item__meta">
                    <span>Beklenen: {approval.expectedBenefit}</span>
                    <span>Tradeoff: {approval.tradeoff}</span>
                    <span>İsteyen: {approval.requester}</span>
                  </div>
                  {approval.recoveryContext ? (
                    <p className="commint-kpi-card__detail">Bağlam: {approval.recoveryContext}</p>
                  ) : null}
                  <Link href={approval.drillDownHref} className="commint-table__link">
                    Kararı aç
                    <ArrowUpRight aria-hidden="true" size={14} />
                  </Link>
                </article>
              ))
            )}
          </div>
        </div>
      </section>

      <section className="executive-command-grid--two executive-command-grid" style={{ marginTop: "1.5rem" }}>
        <div className="commint-panel">
          <div className="commint-panel__header">
            <div>
              <p className="commint-executive__eyebrow">DİJİTAL İŞ GÜCÜ</p>
              <h3>Operasyonel durum</h3>
            </div>
            <Users aria-hidden="true" size={18} />
          </div>
          <div className="commint-kpi-grid" style={{ gridTemplateColumns: "repeat(3, minmax(0, 1fr))" }} data-testid="executive-digital-workforce">
            <div><p className="commint-kpi-card__label">Aktif çalışan</p><p className="commint-kpi-card__value">{workspace.digitalWorkforce.activeWorkers}</p></div>
            <div><p className="commint-kpi-card__label">Aktif vaka</p><p className="commint-kpi-card__value">{workspace.digitalWorkforce.activeCases}</p></div>
            <div><p className="commint-kpi-card__label">Onay bekliyor</p><p className="commint-kpi-card__value">{workspace.digitalWorkforce.waitingApproval}</p></div>
            <div><p className="commint-kpi-card__label">Bloklu</p><p className="commint-kpi-card__value">{workspace.digitalWorkforce.blocked}</p></div>
            <div><p className="commint-kpi-card__label">Başarısız</p><p className="commint-kpi-card__value">{workspace.digitalWorkforce.failed}</p></div>
            <div><p className="commint-kpi-card__label">Verified</p><p className="commint-kpi-card__value">{workspace.digitalWorkforce.verifiedOutcomes}</p></div>
          </div>
          <Link href={workspace.digitalWorkforce.drillDownHref} className="commint-table__link" style={{ marginTop: "1rem", display: "inline-flex", gap: "0.25rem" }}>
            Dijital İş Gücü
            <ArrowUpRight aria-hidden="true" size={14} />
          </Link>
        </div>

        <div className="commint-panel">
          <div className="commint-panel__header">
            <div>
              <p className="commint-executive__eyebrow">VERIFIED VALUE</p>
              <h3>Doğrulanmış sonuçlar</h3>
            </div>
            <CheckCircle2 aria-hidden="true" size={18} />
          </div>
          <div className="executive-stack" data-testid="executive-verified-value">
            {workspace.verifiedValue.length === 0 ? (
              <p className="commint-kpi-card__detail">{verifiedEmpty}</p>
            ) : (
              workspace.verifiedValue.map((item) => (
                <article key={item.id} className="executive-stack-item">
                  <p className="commint-executive__eyebrow">{item.outcomeLabel}</p>
                  <h3 style={{ margin: 0, fontSize: "1rem" }}>{item.orderReference}</h3>
                  <p className="commint-kpi-card__detail">{item.summary}</p>
                  <p className="commint-kpi-card__detail">{item.verifiedAtLabel}</p>
                  <Link href={item.drillDownHref} className="commint-table__link">
                    Vaka detayı
                    <ArrowUpRight aria-hidden="true" size={14} />
                  </Link>
                </article>
              ))
            )}
          </div>
        </div>
      </section>

      <section className="executive-command-grid--two executive-command-grid" style={{ marginTop: "1.5rem" }}>
        <div className="commint-panel">
          <div className="commint-panel__header">
            <div>
              <p className="commint-executive__eyebrow">OPERASYONEL ETKİ</p>
              <h3>Kayıp / fırsat görünümü</h3>
            </div>
            <ShieldAlert aria-hidden="true" size={18} />
          </div>
          <div className="executive-stack" data-testid="executive-operational-exposure">
            {workspace.operationalExposure.map((item) => (
              <article key={item.id} className="executive-stack-item">
                <p className="commint-kpi-card__label">{item.label}</p>
                <p className="commint-kpi-card__value" style={{ fontSize: "1.2rem" }}>{item.value}</p>
                <p className="commint-kpi-card__detail">{item.detail}</p>
                <Link href={item.drillDownHref} className="commint-table__link">
                  İncele
                  <ArrowUpRight aria-hidden="true" size={14} />
                </Link>
              </article>
            ))}
          </div>
        </div>

        <div className="commint-panel">
          <div className="commint-panel__header">
            <div>
              <p className="commint-executive__eyebrow">OPERASYONEL ZAMAN ÇİZELGESİ</p>
              <h3>Bugünkü akış</h3>
            </div>
          </div>
          <div className="executive-timeline" data-testid="executive-timeline">
            {workspace.timeline.length === 0 ? (
              <p className="commint-kpi-card__detail">Henüz zaman çizelgesi kaydı yok.</p>
            ) : (
              workspace.timeline.map((entry) => (
                <div key={entry.id} className="executive-timeline__row">
                  <span className="executive-timeline__time">{entry.timeLabel}</span>
                  <div>
                    <strong>{entry.label}</strong>
                    <p className="commint-kpi-card__detail">{entry.description}</p>
                    {entry.drillDownHref ? (
                      <Link href={entry.drillDownHref} className="commint-table__link">
                        Aç
                        <ArrowUpRight aria-hidden="true" size={14} />
                      </Link>
                    ) : null}
                  </div>
                </div>
              ))
            )}
          </div>
        </div>
      </section>

      <footer style={{ marginTop: "1.5rem" }} className="commint-table__secondary">
        <p>
          Karar yönetimi: <Link href="/fabrika-karar-merkezi">Fabrika Karar Merkezi</Link>
          {" · "}
          Görevler: <Link href="/">AI Görev Merkezi</Link>
          {" · "}
          Operasyon: <Link href="/operasyon/yonetim-merkezi">Operasyon Yönetim Merkezi</Link>
        </p>
      </footer>
    </main>
  );
}
