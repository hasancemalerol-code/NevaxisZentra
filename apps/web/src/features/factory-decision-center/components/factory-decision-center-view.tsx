"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState, useTransition } from "react";
import { AlertTriangle, ArrowUpRight, Gauge, RefreshCw, ShieldAlert, Users } from "lucide-react";

import {
  FACTORY_DECISION_CENTER_SUBTITLE,
  FACTORY_DECISION_CENTER_TITLE,
  getFactoryDecisionSeverityLabel,
  getFactoryDecisionStatusLabel,
} from "../domain/factory-decision-center-labels";
import type {
  FactoryDecisionCaseQueueItemApi,
  FactoryDecisionCenterWorkspaceApi,
} from "../server/factory-decision-center-api-client";
import { PresentationModeReturnLink } from "@/features/presentation-mode/components/presentation-mode-return-link";

import { factoryDecisionResetAction } from "../server/factory-decision-center-actions";
import "@/features/commercial-intelligence/components/commercial-intelligence-workspace.css";

type FactoryDecisionCenterViewProps = Readonly<{
  workspace: FactoryDecisionCenterWorkspaceApi;
}>;

const KPI_CONFIG = [
  { key: "criticalDecisions" as const, label: "Kritik Karar", icon: ShieldAlert, tone: "negative" },
  { key: "outcomesAtRisk" as const, label: "Risk Altındaki Sonuç", icon: AlertTriangle, tone: "warning" },
  { key: "pendingHumanApproval" as const, label: "İnsan Onayı Bekleyen", icon: Users, tone: "warning" },
  { key: "recoveryOperations" as const, label: "Kurtarma Operasyonu", icon: Gauge, tone: "neutral" },
  { key: "pendingVerification" as const, label: "Doğrulama Bekleyen", icon: RefreshCw, tone: "neutral" },
];

function DecisionCard({ item }: Readonly<{ item: FactoryDecisionCaseQueueItemApi }>) {
  return (
    <article className="commint-panel" data-testid={`factory-decision-card-${item.orderReference}`}>
      <div className="commint-panel__header">
        <div>
          <p className="commint-executive__eyebrow">{getFactoryDecisionSeverityLabel(item.severity)}</p>
          <h3>{item.orderReference}</h3>
          <p className="commint-executive__subtitle">{item.title}</p>
        </div>
        <span className="commint-kpi-card__badge">{getFactoryDecisionStatusLabel(item.status)}</span>
      </div>

      <div className="commint-kpi-grid" style={{ gridTemplateColumns: "repeat(2, minmax(0, 1fr))" }}>
        <div>
          <p className="commint-kpi-card__label">Müşteri</p>
          <p className="commint-kpi-card__value" style={{ fontSize: "1rem" }}>{item.customerName}</p>
        </div>
        <div>
          <p className="commint-kpi-card__label">Taahhüt</p>
          <p className="commint-kpi-card__value" style={{ fontSize: "1rem" }}>{item.deliveryCommitmentLabel}</p>
        </div>
        <div>
          <p className="commint-kpi-card__label">Risk</p>
          <p className="commint-kpi-card__detail">{item.riskSummary}</p>
        </div>
        <div>
          <p className="commint-kpi-card__label">Mevcut projeksiyon</p>
          <p className="commint-kpi-card__value" style={{ fontSize: "1.1rem" }}>
            {item.projectedQuantity} / {item.requiredQuantity}
          </p>
          <p className="commint-kpi-card__detail">Gap: {item.gapQuantity} iyi parça</p>
        </div>
      </div>

      <div style={{ marginTop: "1rem" }}>
        <p className="commint-kpi-card__label">Önerilen</p>
        <p className="commint-kpi-card__detail">{item.recommendedStrategy}</p>
      </div>

      <div style={{ marginTop: "1rem" }}>
        <p className="commint-kpi-card__label">Dijital İş Gücü</p>
        <p className="commint-kpi-card__detail">{item.digitalWorkerNames.join(" · ")}</p>
      </div>

      {item.decisionRequired ? (
        <div className="commint-error-banner" style={{ marginTop: "1rem", background: "rgba(234, 179, 8, 0.08)" }}>
          <p><strong>İnsan Kararı:</strong> {item.decisionRequired}</p>
        </div>
      ) : null}

      <div className="commint-executive__actions" style={{ marginTop: "1.25rem" }}>
        <Link
          href={`/fabrika-karar-merkezi/vakalar/${item.caseId}`}
          className="commint-button"
          data-testid={`factory-decision-open-${item.orderReference}`}
        >
          Kararı Aç
          <ArrowUpRight aria-hidden="true" size={16} />
        </Link>
      </div>
    </article>
  );
}

export function FactoryDecisionCenterView({ workspace: initialWorkspace }: FactoryDecisionCenterViewProps) {
  const router = useRouter();
  const [workspace, setWorkspace] = useState(initialWorkspace);
  const [message, setMessage] = useState<string | null>(null);
  const [isPending, startTransition] = useTransition();

  function handleReset() {
    setMessage(null);
    startTransition(async () => {
      const result = await factoryDecisionResetAction();
      if (result.status === "failure") {
        setMessage(result.message);
        return;
      }
      setWorkspace(result.value);
      router.refresh();
    });
  }

  return (
    <main className="commint-page" data-testid="factory-decision-center-view">
      <PresentationModeReturnLink />
      <header className="commint-executive">
        <div className="commint-executive__intro">
          <p className="commint-executive__eyebrow">OPERASYONEL KARAR KATMANI</p>
          <h1>{FACTORY_DECISION_CENTER_TITLE}</h1>
          <p className="commint-executive__subtitle">{FACTORY_DECISION_CENTER_SUBTITLE}</p>
        </div>
        <div className="commint-executive__actions">
          <button
            type="button"
            className="commint-button commint-button--ghost"
            disabled={isPending}
            onClick={handleReset}
            data-testid="factory-decision-center-reset"
          >
            <RefreshCw aria-hidden="true" size={16} />
            Senaryoyu Sıfırla
          </button>
        </div>
      </header>

      {message ? <div className="commint-error-banner">{message}</div> : null}

      <section className="commint-kpi-grid" data-testid="factory-decision-center-summary">
        {KPI_CONFIG.map((kpi) => {
          const Icon = kpi.icon;
          return (
            <article
              key={kpi.key}
              className={`commint-kpi-card commint-kpi-card__tone--${kpi.tone}`}
              data-testid={`factory-decision-kpi-${kpi.key}`}
            >
              <div className="commint-kpi-card__header">
                <p className="commint-kpi-card__label">{kpi.label}</p>
                <Icon aria-hidden="true" size={16} />
              </div>
              <p className="commint-kpi-card__value">{workspace.summary[kpi.key]}</p>
            </article>
          );
        })}
      </section>

      <section className="commint-panel" style={{ marginTop: "1.5rem" }}>
        <div className="commint-panel__header">
          <h2>ÖNCELİKLİ KARARLAR</h2>
        </div>
        {workspace.priorityQueue.length === 0 ? (
          <p className="commint-kpi-card__detail">Aktif kritik karar bulunmuyor.</p>
        ) : (
          <div style={{ display: "grid", gap: "1rem" }}>
            {workspace.priorityQueue.map((item) => (
              <DecisionCard key={item.caseId} item={item} />
            ))}
          </div>
        )}
      </section>
    </main>
  );
}
