"use client";

import { useRouter } from "next/navigation";
import { useTransition, useState } from "react";

import { Badge } from "@/components/ui/badge";
import { GoldenDemoP2PresentationPanel } from "@/features/golden-demo-p2/components/golden-demo-p2-presentation-panel";
import type { GoldenDemoP2PresentationStatusApi } from "@/features/golden-demo-p2/server/golden-demo-p2-api-client";
import { GoldenDemoP2BDigitalFactoryPanel } from "@/features/golden-demo-p2b/components/golden-demo-p2b-digital-factory-panel";
import type { GoldenDemoP2BDigitalFactoryTeamApi } from "@/features/golden-demo-p2b/server/golden-demo-p2b-api-client";
import { GoldenDemoP3OutcomePanel } from "@/features/golden-demo-p3/components/golden-demo-p3-outcome-panel";
import type { GoldenDemoP3OutcomeApi } from "@/features/golden-demo-p3/server/golden-demo-p3-api-client";
import "@/features/commercial-intelligence/components/commercial-intelligence-workspace.css";
import "./golden-demo-p1-workspace.css";

import {
  formatGoldenDemoNumber,
  getGoldenDemoEventStatusLabel,
  getGoldenDemoPlanActionStatusLabel,
} from "../domain/golden-demo-p1-labels";
import {
  translateGoldenDemoApiText,
  type GoldenDemoAgentPresentationContext,
} from "../domain/golden-demo-presentation-copy";
import type { GoldenDemoP1WorkspaceApi } from "../server/golden-demo-p1-api-client";
import { PresentationModeReturnLink } from "@/features/presentation-mode/components/presentation-mode-return-link";
import {
  goldenDemoApprovePlanAction,
  goldenDemoCompleteInterventionAction,
  goldenDemoExecutePlanAction,
  goldenDemoVerifyOutcomeAction,
} from "../server/golden-demo-p1-actions";

type Metrics = Readonly<{
  shiftTargetGoodParts?: number;
  actualGoodParts?: number;
  projectedEndOfShiftGoodParts?: number;
  targetAttainmentPercent?: number;
  rejectQuantity?: number;
  rejectRatePercent?: number;
  throughputGoodPartsPerHour?: number;
  machineState?: string;
  currentCycleSeconds?: number;
  referenceCycleSeconds?: number | null;
  availabilityPercent?: number;
  microStopCount?: number;
  microStopDurationSeconds?: number;
  moldReference?: string | null;
  kwhPerGoodPart?: number | null;
  referenceKwhPerGoodPart?: number | null;
  energyIntensityDeviationPercent?: number | null;
  processParameters?: Readonly<Record<string, number | null>>;
  unavailableMetrics?: Readonly<Record<string, string>>;
}>;

type WorkflowPhase = "tespit" | "karar" | "aksiyon" | "dogrulama";

function readMetrics(workspace: GoldenDemoP1WorkspaceApi): Metrics {
  return workspace.metrics as Metrics;
}

function resolveWorkflowPhase(eventStatus: string): WorkflowPhase {
  switch (eventStatus) {
    case "intervention-in-progress":
      return "aksiyon";
    case "validation-run":
    case "verified":
    case "closed":
      return "dogrulama";
    default:
      return "tespit";
  }
}

function workflowStepClass(activePhase: WorkflowPhase, step: WorkflowPhase, eventStatus: string): string {
  const order: WorkflowPhase[] = ["tespit", "karar", "aksiyon", "dogrulama"];
  const activeIndex = order.indexOf(activePhase);
  const stepIndex = order.indexOf(step);

  if (stepIndex < activeIndex || (step === "dogrulama" && (eventStatus === "verified" || eventStatus === "closed"))) {
    return "golden-demo-workflow-rail__step golden-demo-workflow-rail__step--complete";
  }

  if (stepIndex === activeIndex) {
    return "golden-demo-workflow-rail__step golden-demo-workflow-rail__step--active";
  }

  if (step === "karar" && activePhase === "tespit") {
    return "golden-demo-workflow-rail__step golden-demo-workflow-rail__step--active";
  }

  return "golden-demo-workflow-rail__step";
}

export function GoldenDemoP1WorkspaceView({
  workspace,
  presentation,
  digitalFactoryTeam,
  outcome,
}: Readonly<{
  workspace: GoldenDemoP1WorkspaceApi;
  presentation: GoldenDemoP2PresentationStatusApi | null;
  digitalFactoryTeam: GoldenDemoP2BDigitalFactoryTeamApi | null;
  outcome: GoldenDemoP3OutcomeApi | null;
}>) {
  const [message, setMessage] = useState<string | null>(null);
  const [pending, startTransition] = useTransition();
  const router = useRouter();
  const metrics = readMetrics(workspace);
  const eventId = workspace.event.id;
  const isApproved = workspace.plan?.approvedAtUtc != null;
  const activePhase = resolveWorkflowPhase(workspace.event.status);
  const cycleDeviation =
    metrics.referenceCycleSeconds != null && metrics.currentCycleSeconds != null
      ? metrics.currentCycleSeconds - metrics.referenceCycleSeconds
      : null;
  const recoveryGoodParts = workspace.recovery.productionRecoveryGoodParts;
  const agentPresentationContext: GoldenDemoAgentPresentationContext = {
    recoveryGoodParts,
    cycleLossSeconds: workspace.cycleLoss.cycleLossSeconds,
    referenceCycleSeconds: metrics.referenceCycleSeconds,
    currentCycleSeconds: metrics.currentCycleSeconds,
    topContributorLabel: workspace.contributors[0]?.label,
    topContributorDeviation: workspace.contributors[0]?.measuredDeviationSeconds,
    kwhPerGoodPart: metrics.kwhPerGoodPart,
    hasBaseline: workspace.cycleLoss.hasBaseline,
  };

  function runAction(action: () => Promise<{ status: string; message?: string }>) {
    setMessage(null);
    startTransition(async () => {
      const result = await action();
      if (result.status === "failure") {
        setMessage(result.message ?? "İşlem başarısız.");
      } else {
        setMessage("İşlem tamamlandı.");
        router.refresh();
      }
    });
  }

  return (
    <main className="commint-page golden-demo-p1-page" data-testid="golden-demo-p1-workspace">
      <PresentationModeReturnLink />
      {presentation ? (
        <GoldenDemoP2PresentationPanel presentation={presentation} eventId={eventId} />
      ) : null}

      <section className="golden-demo-hero" data-testid="golden-demo-recovery-kpi">
        <div className="golden-demo-hero__content">
          <p className="commint-executive__eyebrow">ÜRETİM KURTARMA FIRSATI</p>
          <h2 className="golden-demo-hero__title">Üretim Kurtarma Fırsatı</h2>
          <p className="golden-demo-hero__context">
            {workspace.event.machineCode} · Aktif Vardiya
          </p>
          <p className="golden-demo-hero__copy">
            Zentra mevcut üretim davranışını analiz ederek vardiya hedefinin üzerinde doğrulanabilir
            üretim fırsatı tespit etti.
          </p>
        </div>
        <div className="golden-demo-hero__value">
          <p className="golden-demo-hero__value-label">İyi Parça / Vardiya</p>
          <p className="golden-demo-hero__value-number" data-testid="golden-demo-recovery-good-parts">
            +{formatGoldenDemoNumber(workspace.recovery.productionRecoveryGoodParts)}
          </p>
          <span className="golden-demo-hero__status">Hedef Risk Altında</span>
        </div>
      </section>

      <section className="golden-demo-workflow-rail" data-testid="golden-demo-digital-worker-rail">
        <article className={workflowStepClass(activePhase, "tespit", workspace.event.status)}>
          <p className="golden-demo-workflow-rail__label">01 · TESPİT</p>
          <p className="golden-demo-workflow-rail__title">Sorun ve fırsat tespiti</p>
          <p className="golden-demo-workflow-rail__detail">
            Makine zekâsı vardiya performansını analiz eder.
          </p>
        </article>
        <article className={workflowStepClass(activePhase, "karar", workspace.event.status)}>
          <p className="golden-demo-workflow-rail__label">02 · KARAR</p>
          <p className="golden-demo-workflow-rail__title">Kurtarma kararı</p>
          <p className="golden-demo-workflow-rail__detail">
            Dijital Çalışan ekibi fırsatı nicelleştirir ve onay ister.
          </p>
        </article>
        <article className={workflowStepClass(activePhase, "aksiyon", workspace.event.status)}>
          <p className="golden-demo-workflow-rail__label">03 · AKSİYON</p>
          <p className="golden-demo-workflow-rail__title">Müdahale koordinasyonu</p>
          <p className="golden-demo-workflow-rail__detail">
            Planlanan aksiyonlar sahadaki role yönlendirilir.
          </p>
        </article>
        <article className={workflowStepClass(activePhase, "dogrulama", workspace.event.status)}>
          <p className="golden-demo-workflow-rail__label">04 · DOĞRULAMA</p>
          <p className="golden-demo-workflow-rail__title">Sonuç doğrulama</p>
          <p className="golden-demo-workflow-rail__detail">
            Üretim sonucu ölçülür ve vardiya etkisi doğrulanır.
          </p>
        </article>
      </section>

      <section className="commint-panel golden-demo-machine-panel" data-testid="golden-demo-machine-intelligence">
        <div className="golden-demo-machine-panel__header">
          <h2 className="commint-panel__title">Makine Zekâsı — {workspace.event.machineCode}</h2>
          <Badge>{getGoldenDemoEventStatusLabel(workspace.event.status)}</Badge>
        </div>
        <div className="golden-demo-machine-grid">
          <article className="golden-demo-metric golden-demo-machine-grid__emphasis golden-demo-metric--emphasis">
            <p className="golden-demo-metric__label">Vardiya Projeksiyonu / Hedef</p>
            <p className="golden-demo-metric__value">
              {metrics.projectedEndOfShiftGoodParts} / {metrics.shiftTargetGoodParts}
            </p>
            <p className="golden-demo-metric__detail">
              Hedef gerçekleşme {formatGoldenDemoNumber(metrics.targetAttainmentPercent ?? 0, 1)}%
            </p>
          </article>
          <article className="golden-demo-metric">
            <p className="golden-demo-metric__label">Gerçekleşen</p>
            <p className="golden-demo-metric__value">{metrics.actualGoodParts}</p>
          </article>
          <article className="golden-demo-metric golden-demo-metric--warning">
            <p className="golden-demo-metric__label">Çevrim Süresi / Referans</p>
            <p className="golden-demo-metric__value">
              {formatGoldenDemoNumber(metrics.currentCycleSeconds ?? 0, 1)} /{" "}
              {metrics.referenceCycleSeconds != null
                ? formatGoldenDemoNumber(metrics.referenceCycleSeconds, 1)
                : "—"}{" "}
              sn
            </p>
            {cycleDeviation != null && cycleDeviation > 0 ? (
              <p className="golden-demo-metric__detail">+{formatGoldenDemoNumber(cycleDeviation, 1)} sn sapma</p>
            ) : null}
          </article>
          <article className="golden-demo-metric">
            <p className="golden-demo-metric__label">İyi Parça / Saat</p>
            <p className="golden-demo-metric__value">
              {formatGoldenDemoNumber(metrics.throughputGoodPartsPerHour ?? 0, 1)}
            </p>
          </article>
          <article className="golden-demo-metric">
            <p className="golden-demo-metric__label">Mikro Duruş</p>
            <p className="golden-demo-metric__value">
              {metrics.microStopCount} / {formatGoldenDemoNumber(metrics.microStopDurationSeconds ?? 0, 0)} sn
            </p>
          </article>
          <article className="golden-demo-metric">
            <p className="golden-demo-metric__label">Red Oranı</p>
            <p className="golden-demo-metric__value">
              {formatGoldenDemoNumber(metrics.rejectRatePercent ?? 0, 1)}%
            </p>
          </article>
        </div>
        {workspace.baseline ? (
          <p className="golden-demo-reference" data-testid="golden-demo-reference-source">
            Referans kaynağı: {workspace.baseline.referenceSourceLabel}
          </p>
        ) : (
          <p className="golden-demo-reference">
            {translateGoldenDemoApiText(workspace.cycleLoss.baselineFailureReason ?? "", recoveryGoodParts)}
          </p>
        )}
      </section>

      {message ? (
        <p
          role="status"
          className={`golden-demo-feedback ${message.includes("başarısız") ? "golden-demo-feedback--error" : "golden-demo-feedback--success"}`}
        >
          {message}
        </p>
      ) : null}
      {pending ? (
        <p role="status" className="golden-demo-feedback golden-demo-feedback--status">
          İşlem devam ediyor…
        </p>
      ) : null}

      <p className="commint-readonly-note">
        {translateGoldenDemoApiText(workspace.recovery.safetyMessage, recoveryGoodParts)}
      </p>

      {digitalFactoryTeam ? (
        <GoldenDemoP2BDigitalFactoryPanel
          team={digitalFactoryTeam}
          pending={pending}
          presentationContext={agentPresentationContext}
          onApprove={() => runAction(() => goldenDemoApprovePlanAction(eventId))}
        />
      ) : null}

      <div className="golden-demo-details-grid">
        <section className="commint-panel" data-testid="golden-demo-cycle-loss">
          <h2>Çevrim Kaybı Ayrıştırma</h2>
          {workspace.cycleLoss.hasBaseline ? (
            <>
              <p>
                Referans {formatGoldenDemoNumber(workspace.cycleLoss.referenceCycleSeconds, 1)} sn · Mevcut{" "}
                {formatGoldenDemoNumber(workspace.cycleLoss.currentCycleSeconds, 1)} sn · Kayıp{" "}
                +{formatGoldenDemoNumber(workspace.cycleLoss.cycleLossSeconds, 1)} sn
              </p>
              <p>
                Mikro-duruş eşdeğer kaybı:{" "}
                {formatGoldenDemoNumber(workspace.cycleLoss.microStopEquivalentLossSeconds, 0)} sn
              </p>
              <ul>
                {workspace.cycleLoss.phases.map((phase) => (
                  <li key={phase.phaseCode}>
                    {phase.label}: ref {formatGoldenDemoNumber(phase.referenceSeconds, 1)} sn · mevcut{" "}
                    {formatGoldenDemoNumber(phase.currentSeconds, 1)} sn ·{" "}
                    {phase.isLoss
                      ? `kayıp +${formatGoldenDemoNumber(phase.lossSeconds, 1)} sn`
                      : phase.isImprovement
                        ? `iyileşme ${formatGoldenDemoNumber(phase.lossSeconds, 1)} sn`
                        : "stabil"}
                  </li>
                ))}
              </ul>
            </>
          ) : (
            <p>{translateGoldenDemoApiText(workspace.cycleLoss.baselineFailureReason ?? "", recoveryGoodParts)}</p>
          )}
        </section>

        <section className="commint-panel" data-testid="golden-demo-contributors">
          <h2>Ana Üretim Kaybı Katkıda Bulunanlar</h2>
          <ul>
            {workspace.contributors.map((contributor) => (
              <li key={contributor.rank}>
                <strong>
                  {contributor.rank}. {contributor.label}
                </strong>{" "}
                ({translateGoldenDemoApiText(contributor.contributorKind)}) —{" "}
                {formatGoldenDemoNumber(contributor.measuredDeviationSeconds, 1)} sn ·{" "}
                {translateGoldenDemoApiText(contributor.confidence)} güven ·{" "}
                {translateGoldenDemoApiText(contributor.suggestedNextStep)}
              </li>
            ))}
          </ul>
        </section>

        <section className="commint-panel" data-testid="golden-demo-recovery-opportunity">
          <h2>Üretim Kurtarma Fırsatı — Detay</h2>
          <p>Mevcut beklenen vardiya çıktısı: {workspace.recovery.currentExpectedShiftGoodParts}</p>
          <p>Kurtarılmış beklenen vardiya çıktısı: {workspace.recovery.recoveredExpectedShiftGoodParts}</p>
          <p>
            Kurtarma fırsatı: +{workspace.recovery.productionRecoveryGoodParts} iyi parça (
            {formatGoldenDemoNumber(workspace.recovery.productionRecoveryPercent, 1)}%)
          </p>
          <ul>
            {workspace.recovery.assumptions.map((assumption) => (
              <li key={assumption}>{translateGoldenDemoApiText(assumption, recoveryGoodParts)}</li>
            ))}
          </ul>
        </section>

        <section className="commint-panel" data-testid="golden-demo-what-if">
          <h2>Senaryo Karşılaştırması</h2>
          <ul>
            {workspace.whatIfScenarios.map((scenario) => (
              <li key={scenario.scenarioCode}>
                <strong>{scenario.title}</strong>
                {scenario.isRecommended ? " — Önerilen" : ""}
                <p>Vardiya sonu projeksiyon: {scenario.projectedEndOfShiftGoodParts}</p>
                <p>{translateGoldenDemoApiText(scenario.riskExposure, recoveryGoodParts)}</p>
                {scenario.recommendationReason ? (
                  <p>{translateGoldenDemoApiText(scenario.recommendationReason, recoveryGoodParts)}</p>
                ) : null}
              </li>
            ))}
          </ul>
        </section>
      </div>

      <section className="commint-panel" data-testid="golden-demo-recovery-plan">
        <h2 className="commint-panel__title">Önerilen Kurtarma Planı</h2>
        {workspace.plan ? (
          <p>{translateGoldenDemoApiText(workspace.plan.recommendationSummary, recoveryGoodParts)}</p>
        ) : null}
        <ul>
          {workspace.planActions.map((action) => (
            <li key={action.id}>
              {action.sortOrder}. {action.title} — {translateGoldenDemoApiText(action.responsibleRole)} ·{" "}
              {getGoldenDemoPlanActionStatusLabel(action.status)}
            </li>
          ))}
        </ul>
        <div className="golden-demo-action-row">
          <button
            type="button"
            className="commint-button commint-button--primary"
            disabled={pending || !isApproved || workspace.event.status !== "active"}
            onClick={() => runAction(() => goldenDemoExecutePlanAction(eventId))}
          >
            {pending ? "Başlatılıyor…" : "Müdahaleyi Başlat"}
          </button>
          <button
            type="button"
            className="commint-button"
            disabled={pending || workspace.event.status !== "intervention-in-progress"}
            onClick={() => runAction(() => goldenDemoCompleteInterventionAction(eventId))}
          >
            {pending ? "Tamamlanıyor…" : "Müdahaleyi Tamamla"}
          </button>
          <button
            type="button"
            className="commint-button"
            disabled={pending || workspace.event.status !== "validation-run"}
            onClick={() => runAction(() => goldenDemoVerifyOutcomeAction(eventId))}
          >
            {pending ? "Doğrulanıyor…" : "Sonucu Doğrula"}
          </button>
        </div>
      </section>

      {outcome ? <GoldenDemoP3OutcomePanel outcome={outcome} /> : null}
    </main>
  );
}
