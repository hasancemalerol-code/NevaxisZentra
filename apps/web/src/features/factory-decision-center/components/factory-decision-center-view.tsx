"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState, useTransition } from "react";
import { AlertTriangle, ArrowUpRight, CheckCircle2, Gauge, RefreshCw, ShieldAlert, Users } from "lucide-react";

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
import { StatusBadge } from "@/components/ui/badge";
import { PageHeader } from "@/components/ui/page";
import { StatusStrip } from "@/components/workspace/status-strip";
import { PresentationModeReturnLink } from "@/features/presentation-mode/components/presentation-mode-return-link";

import { factoryDecisionResetAction } from "../server/factory-decision-center-actions";
import { customerMessage } from "@/lib/presentation/customer-message";
import { enumLabel } from "@/lib/presentation/enum-labels";

import "./factory-decision-center.css";

type FactoryDecisionCenterViewProps = Readonly<{
  workspace: FactoryDecisionCenterWorkspaceApi;
  /** Scenario reset is a demo/simulation action: the server page enables it only in Demo/Development. */
  scenarioResetEnabled?: boolean;
  /** Digital Workforce is deferred: only an explicitly allowed deployment shows it (decided on the server). */
  showWorkforce?: boolean;
}>;

const KPI_CONFIG = [
  { key: "criticalDecisions" as const, label: "Kritik Karar", icon: ShieldAlert, tone: "negative" },
  { key: "outcomesAtRisk" as const, label: "Risk Altındaki Sonuç", icon: AlertTriangle, tone: "warning" },
  { key: "pendingHumanApproval" as const, label: "İnsan Onayı Bekleyen", icon: Users, tone: "warning" },
  { key: "recoveryOperations" as const, label: "Kurtarma Operasyonu", icon: Gauge, tone: "neutral" },
  { key: "pendingVerification" as const, label: "Doğrulama Bekleyen", icon: RefreshCw, tone: "neutral" },
];

function DecisionCard({ item, showWorkforce }: Readonly<{ item: FactoryDecisionCaseQueueItemApi; showWorkforce: boolean }>) {
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
          <p className="commint-kpi-card__label">Mevcut öngörü</p>
          <p className="commint-kpi-card__value" style={{ fontSize: "1.1rem" }}>
            {item.projectedQuantity} / {item.requiredQuantity}
          </p>
          <p className="commint-kpi-card__detail">Eksik: {item.gapQuantity} iyi parça</p>
        </div>
      </div>

      <div style={{ marginTop: "1rem" }}>
        <p className="commint-kpi-card__label">Önerilen</p>
        <p className="commint-kpi-card__detail">{item.recommendedStrategy}</p>
      </div>

      {showWorkforce ? (
        <div style={{ marginTop: "1rem" }}>
          <p className="commint-kpi-card__label">Dijital İş Gücü</p>
          <p className="commint-kpi-card__detail">{item.digitalWorkerNames.join(" · ")}</p>
        </div>
      ) : null}

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

export function FactoryDecisionCenterView({ workspace: initialWorkspace, scenarioResetEnabled = false, showWorkforce = false }: FactoryDecisionCenterViewProps) {
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

  const earlyWarningItems = [
    ...workspace.earlyWarnings.criticalPredictedRisks,
    ...workspace.earlyWarnings.deliveryRisks,
    ...workspace.earlyWarnings.machineDegradationTrends,
    ...workspace.earlyWarnings.materialDepletionRisks,
    ...workspace.earlyWarnings.qualityDeteriorationTrends,
  ];

  return (
    <div className="fact-workspace fdc-page" data-testid="factory-decision-center-view">
      <PresentationModeReturnLink />
      <PageHeader
        eyebrow="Karar Merkezi"
        title={FACTORY_DECISION_CENTER_TITLE}
        description={FACTORY_DECISION_CENTER_SUBTITLE}
        actions={
          scenarioResetEnabled ? (
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
          ) : undefined
        }
      />

      {message ? <div className="commint-error-banner">{customerMessage(message)}</div> : null}

      {KPI_CONFIG.some((kpi) => Number(workspace.summary[kpi.key]) > 0) ? (
      <StatusStrip
        aria-label="Karar merkezi özet"
        items={KPI_CONFIG.map((kpi) => ({
          label: kpi.label,
          value: workspace.summary[kpi.key],
        }))}
      />
      ) : null}

      {earlyWarningItems.length > 0 ? (
        <EarlyWarningSection
          title="Erken Uyarı / Öngörülen Riskler"
          testId="factory-decision-early-warnings"
          items={earlyWarningItems}
        />
      ) : null}

      <LiveQueueSections
        showWorkforce={showWorkforce}
        queues={QUEUE_CONFIG.map((queue) => ({ ...queue, items: workspace.liveQueues[queue.key] }))}
      />
    </div>
  );
}

const QUEUE_CONFIG = [
  { key: "criticalDecisions" as const, title: "Kritik Kararlar", testId: "factory-decision-queue-critical", emptyMessage: "Kritik karar kuyruğu boş." },
  { key: "activeRecoveryOperations" as const, title: "Aktif Kurtarma Operasyonları", testId: "factory-decision-queue-recovery", emptyMessage: "Aktif kurtarma operasyonu bulunmuyor." },
  { key: "pendingHumanApproval" as const, title: "İnsan Onayı Bekleyenler", testId: "factory-decision-queue-approval", emptyMessage: "Bekleyen insan onayı yok." },
  { key: "blockedOrEscalated" as const, title: "Engellenen / Üst Seviyeye Aktarılan", testId: "factory-decision-queue-blocked", emptyMessage: "Engellenmiş veya üst seviyeye aktarılmış vaka yok." },
  { key: "pendingVerification" as const, title: "Doğrulama Bekleyenler", testId: "factory-decision-queue-verification", emptyMessage: "Doğrulama bekleyen vaka yok." },
  { key: "verifiedClosedToday" as const, title: "Bugün Doğrulanarak Kapananlar", testId: "factory-decision-queue-verified-today", emptyMessage: "Bugün doğrulanarak kapanan vaka yok." },
];

type QueueSection = Readonly<{
  title: string;
  testId: string;
  emptyMessage: string;
  items: readonly FactoryDecisionCaseQueueItemApi[];
}>;

type QueueTone = "normal" | "attention" | "critical";

function queueStatus(testId: string, count: number): { tone: QueueTone; label: string } {
  if (count === 0) {
    return { tone: "normal", label: "Bekleyen yok" };
  }
  if (testId === "factory-decision-queue-verified-today") {
    return { tone: "normal", label: "Tamamlandı" };
  }
  if (testId === "factory-decision-queue-critical" || testId === "factory-decision-queue-blocked") {
    return { tone: "critical", label: "Müdahale gerekli" };
  }
  return { tone: "attention", label: "İşlem bekliyor" };
}

/** Every queue is summarised as one equal card (title, count, status); queues with cases also expand into full sections below. */
function LiveQueueSections({
  queues,
  showWorkforce,
}: Readonly<{ queues: readonly QueueSection[]; showWorkforce: boolean }>) {
  const filled = queues.filter((queue) => queue.items.length > 0);
  const allClear = filled.length === 0;

  return (
    <>
      <section className="fdc-queues" aria-label="Karar kuyrukları" data-testid="factory-decision-queue-overview">
        <div className="fdc-queues__head">
          <span className="fdc-queues__icon" aria-hidden="true"><CheckCircle2 /></span>
          <div>
            <h2 className="fdc-queues__title">{allClear ? "Bekleyen karar yok" : "Karar kuyrukları"}</h2>
            <p className="fdc-queues__copy">
              {allClear
                ? "Karar kuyruklarının tamamı şu anda boş; yeni bir vaka açıldığında burada görünür."
                : "Her kuyruktaki güncel vaka sayısı ve durumu."}
            </p>
          </div>
        </div>
        <ul className="fdc-queues__grid" data-testid="factory-decision-clear-queues">
          {queues.map((queue) => {
            const status = queueStatus(queue.testId, queue.items.length);
            return (
              <li
                className="fdc-queue-card"
                key={queue.testId}
                data-testid={queue.items.length === 0 ? queue.testId : `${queue.testId}-summary`}
                title={queue.items.length === 0 ? queue.emptyMessage : undefined}
              >
                <h3 className="fdc-queue-card__title">{queue.title}</h3>
                <p className="fdc-queue-card__count">{queue.items.length}</p>
                <StatusBadge status={status.tone} label={status.label} />
              </li>
            );
          })}
        </ul>
      </section>
      {filled.map((queue) => (
        <LiveQueueSection key={queue.testId} title={queue.title} testId={queue.testId} items={queue.items} showWorkforce={showWorkforce} />
      ))}
    </>
  );
}

function EarlyWarningCard({ item }: Readonly<{ item: FactoryDecisionEarlyWarningCardApi }>) {
  return (
    <article className="commint-panel" data-testid={`factory-early-warning-${item.riskId}`}>
      <div className="commint-panel__header">
        <div>
          <p className="commint-executive__eyebrow">{enumLabel(item.riskState)} · {enumLabel(item.confidenceBand)}</p>
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
  items,
}: Readonly<{
  title: string;
  testId: string;
  items: readonly FactoryDecisionEarlyWarningCardApi[];
}>) {
  return (
    <section className="commint-panel" style={{ marginTop: "1.5rem" }} data-testid={testId}>
      <div className="commint-panel__header">
        <h2>{title}</h2>
        <span className="commint-kpi-card__badge">{items.length}</span>
      </div>
      <div style={{ display: "grid", gap: "1rem" }}>
        {items.map((item) => (
          <EarlyWarningCard key={item.riskId} item={item} />
        ))}
      </div>
    </section>
  );
}

function LiveQueueSection({
  title,
  testId,
  items,
  showWorkforce,
}: Readonly<{
  title: string;
  testId: string;
  items: readonly FactoryDecisionCaseQueueItemApi[];
  showWorkforce: boolean;
}>) {
  return (
    <section className="commint-panel" style={{ marginTop: "1.5rem" }} data-testid={testId}>
      <div className="commint-panel__header">
        <h2>{title}</h2>
        <span className="commint-kpi-card__badge">{items.length}</span>
      </div>
      <div style={{ display: "grid", gap: "1rem" }}>
        {items.map((item) => (
          <DecisionCard key={`${testId}-${item.caseId}`} item={item} showWorkforce={showWorkforce} />
        ))}
      </div>
    </section>
  );
}
