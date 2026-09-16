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
  FactoryDecisionEarlyWarningCardApi,
  FactoryDecisionCenterWorkspaceApi,
} from "../server/factory-decision-center-api-client";
import { StatusStrip } from "@/components/workspace/status-strip";
import { WorkspaceHeader } from "@/components/workspace/workspace-header";
import { PresentationModeReturnLink } from "@/features/presentation-mode/components/presentation-mode-return-link";

import { factoryDecisionResetAction } from "../server/factory-decision-center-actions";

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
    <main className="fact-workspace" data-testid="factory-decision-center-view">
      <PresentationModeReturnLink />
      <WorkspaceHeader
        context="OPERASYONEL KARAR KATMANI"
        title={FACTORY_DECISION_CENTER_TITLE}
        meta={FACTORY_DECISION_CENTER_SUBTITLE}
        actions={
          <button
            type="button"
            className="button button-secondary button-medium"
            disabled={isPending}
            onClick={handleReset}
            data-testid="factory-decision-center-reset"
          >
            <RefreshCw aria-hidden="true" size={16} />
            Senaryoyu Sıfırla
          </button>
        }
      />

      {message ? <div className="commint-error-banner">{message}</div> : null}

      <StatusStrip
        aria-label="Karar merkezi özet"
        items={KPI_CONFIG.map((kpi) => ({
          label: kpi.label,
          value: workspace.summary[kpi.key],
        }))}
      />

      <EarlyWarningSection
        title="ERKEN UYARI / ÖNGÖRÜLEN RİSKLER"
        testId="factory-decision-early-warnings"
        queues={workspace.earlyWarnings}
      />

      <LiveQueueSection
        title="KRİTİK KARARLAR"
        testId="factory-decision-queue-critical"
        items={workspace.liveQueues.criticalDecisions}
        emptyMessage="Kritik karar kuyruğu boş."
      />

      <LiveQueueSection
        title="AKTİF KURTARMA OPERASYONLARI"
        testId="factory-decision-queue-recovery"
        items={workspace.liveQueues.activeRecoveryOperations}
        emptyMessage="Aktif kurtarma operasyonu bulunmuyor."
      />

      <LiveQueueSection
        title="İNSAN ONAYI BEKLEYENLER"
        testId="factory-decision-queue-approval"
        items={workspace.liveQueues.pendingHumanApproval}
        emptyMessage="Bekleyen insan onayı yok."
      />

      <LiveQueueSection
        title="BLOCKED / ESCALATED"
        testId="factory-decision-queue-blocked"
        items={workspace.liveQueues.blockedOrEscalated}
        emptyMessage="Engellenmiş veya yükseltilmiş vaka yok."
      />

      <LiveQueueSection
        title="DOĞRULAMA BEKLEYENLER"
        testId="factory-decision-queue-verification"
        items={workspace.liveQueues.pendingVerification}
        emptyMessage="Doğrulama bekleyen vaka yok."
      />

      <LiveQueueSection
        title="BUGÜN VERIFIED KAPANANLAR"
        testId="factory-decision-queue-verified-today"
        items={workspace.liveQueues.verifiedClosedToday}
        emptyMessage="Bugün doğrulanmış kapanış yok."
      />
    </main>
  );
}

function EarlyWarningCard({ item }: Readonly<{ item: FactoryDecisionEarlyWarningCardApi }>) {
  return (
    <article className="commint-panel" data-testid={`factory-early-warning-${item.riskId}`}>
      <div className="commint-panel__header">
        <div>
          <p className="commint-executive__eyebrow">{item.riskState.toUpperCase()} · {item.confidenceBand.toUpperCase()}</p>
          <h3>{item.machineCode} {item.orderReference ? `· ${item.orderReference}` : ""}</h3>
        </div>
        <span className="commint-kpi-card__badge">{item.horizon}</span>
      </div>
      <div className="commint-kpi-grid" style={{ gridTemplateColumns: "repeat(2, minmax(0, 1fr))" }}>
        <div>
          <p className="commint-kpi-card__label">Mevcut durum</p>
          <p className="commint-kpi-card__detail">{item.observedSummary}</p>
        </div>
        <div>
          <p className="commint-kpi-card__label">Öngörü</p>
          <p className="commint-kpi-card__detail">{item.predictedSummary}</p>
        </div>
        <div>
          <p className="commint-kpi-card__label">Etki süresi</p>
          <p className="commint-kpi-card__detail">{item.timeToImpactLabel}</p>
        </div>
        <div>
          <p className="commint-kpi-card__label">Veri kalitesi</p>
          <p className="commint-kpi-card__detail">{item.dataQuality}</p>
        </div>
      </div>
      <div style={{ marginTop: "1rem" }}>
        <p className="commint-kpi-card__label">Önerilen önlem</p>
        <p className="commint-kpi-card__detail">{item.recommendedActions.join(" · ")}</p>
      </div>
      {item.caseId ? (
        <div className="commint-executive__actions" style={{ marginTop: "1rem" }}>
          <Link href={`/fabrika-karar-merkezi/vakalar/${item.caseId}`} className="commint-button">
            Erken uyarı vakasını aç
            <ArrowUpRight aria-hidden="true" size={16} />
          </Link>
        </div>
      ) : null}
    </article>
  );
}

function EarlyWarningSection({
  title,
  testId,
  queues,
}: Readonly<{
  title: string;
  testId: string;
  queues: FactoryDecisionCenterWorkspaceApi["earlyWarnings"];
}>) {
  const items = [
    ...queues.criticalPredictedRisks,
    ...queues.deliveryRisks,
    ...queues.machineDegradationTrends,
    ...queues.materialDepletionRisks,
    ...queues.qualityDeteriorationTrends,
  ];

  return (
    <section className="commint-panel" style={{ marginTop: "1.5rem" }} data-testid={testId}>
      <div className="commint-panel__header">
        <h2>{title}</h2>
        <span className="commint-kpi-card__badge">{items.length}</span>
      </div>
      {items.length === 0 ? (
        <p className="commint-kpi-card__detail">Aktif öngörülen risk bulunmuyor.</p>
      ) : (
        <div style={{ display: "grid", gap: "1rem" }}>
          {items.map((item) => (
            <EarlyWarningCard key={item.riskId} item={item} />
          ))}
        </div>
      )}
    </section>
  );
}

function LiveQueueSection({
  title,
  testId,
  items,
  emptyMessage,
}: Readonly<{
  title: string;
  testId: string;
  items: readonly FactoryDecisionCaseQueueItemApi[];
  emptyMessage: string;
}>) {
  return (
    <section className="commint-panel" style={{ marginTop: "1.5rem" }} data-testid={testId}>
      <div className="commint-panel__header">
        <h2>{title}</h2>
        <span className="commint-kpi-card__badge">{items.length}</span>
      </div>
      {items.length === 0 ? (
        <p className="commint-kpi-card__detail">{emptyMessage}</p>
      ) : (
        <div style={{ display: "grid", gap: "1rem" }}>
          {items.map((item) => (
            <DecisionCard key={`${testId}-${item.caseId}`} item={item} />
          ))}
        </div>
      )}
    </section>
  );
}
