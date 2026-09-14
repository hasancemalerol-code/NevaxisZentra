"use client";

import { useRouter } from "next/navigation";
import { useTransition, useState } from "react";

import { Badge } from "@/components/ui/badge";

import {
  formatGoldenDemoNumber,
  getGoldenDemoEventStatusLabel,
  getGoldenDemoPlanActionStatusLabel,
  GOLDEN_DEMO_P1_SUBTITLE,
  GOLDEN_DEMO_P1_TITLE,
} from "../domain/golden-demo-p1-labels";
import type { GoldenDemoP1WorkspaceApi } from "../server/golden-demo-p1-api-client";
import type { GoldenDemoP2PresentationStatusApi } from "@/features/golden-demo-p2/server/golden-demo-p2-api-client";
import { GoldenDemoP2PresentationPanel } from "@/features/golden-demo-p2/components/golden-demo-p2-presentation-panel";
import type { GoldenDemoP2BDigitalFactoryTeamApi } from "@/features/golden-demo-p2b/server/golden-demo-p2b-api-client";
import { GoldenDemoP2BDigitalFactoryPanel } from "@/features/golden-demo-p2b/components/golden-demo-p2b-digital-factory-panel";
import type { GoldenDemoP3OutcomeApi } from "@/features/golden-demo-p3/server/golden-demo-p3-api-client";
import { GoldenDemoP3OutcomePanel } from "@/features/golden-demo-p3/components/golden-demo-p3-outcome-panel";
import "@/features/commercial-intelligence/components/commercial-intelligence-workspace.css";
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

function readMetrics(workspace: GoldenDemoP1WorkspaceApi): Metrics {
  return workspace.metrics as Metrics;
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
    <main className="commint-page" data-testid="golden-demo-p1-workspace">
      <header className="commint-executive">
        <div className="commint-executive__intro">
          <p className="commint-executive__eyebrow">ALTIN DEMO · INJ-001</p>
          <h1>{GOLDEN_DEMO_P1_TITLE}</h1>
          <p className="commint-executive__subtitle">{GOLDEN_DEMO_P1_SUBTITLE}</p>
          <p>{workspace.event.title}</p>
          <Badge>{getGoldenDemoEventStatusLabel(workspace.event.status)}</Badge>
        </div>
      </header>

      <section className="commint-kpi-grid" data-testid="golden-demo-recovery-kpi">
        <article className="commint-kpi-card">
          <p className="commint-kpi-card__label">+X İYİ PARÇA / VARDİYA</p>
          <p className="commint-kpi-card__value" data-testid="golden-demo-recovery-good-parts">
            +{formatGoldenDemoNumber(workspace.recovery.productionRecoveryGoodParts)}
          </p>
        </article>
        <article className="commint-kpi-card">
          <p className="commint-kpi-card__label">Cycle Loss</p>
          <p className="commint-kpi-card__value">
            {workspace.cycleLoss.hasBaseline
              ? `+${formatGoldenDemoNumber(workspace.cycleLoss.cycleLossSeconds, 1)} sn`
              : "INSUFFICIENT BASELINE"}
          </p>
        </article>
        <article className="commint-kpi-card">
          <p className="commint-kpi-card__label">Hedef Gerçekleşme</p>
          <p className="commint-kpi-card__value">
            {formatGoldenDemoNumber(metrics.targetAttainmentPercent ?? 0, 1)}%
          </p>
        </article>
        <article className="commint-kpi-card">
          <p className="commint-kpi-card__label">Güven</p>
          <p className="commint-kpi-card__value">{workspace.recovery.confidenceLevel}</p>
        </article>
      </section>

      <p className="commint-table__secondary">{workspace.recovery.safetyMessage}</p>
      {message ? <p role="status">{message}</p> : null}
      {pending ? <p role="status">İşlem devam ediyor…</p> : null}

      {presentation ? (
        <GoldenDemoP2PresentationPanel presentation={presentation} eventId={eventId} />
      ) : null}

      <section className="commint-panel" data-testid="golden-demo-machine-intelligence">
        <h2>Makine Zekası — {workspace.event.machineCode}</h2>
        <div className="commint-kpi-grid">
          <article className="commint-kpi-card">
            <p className="commint-kpi-card__label">Vardiya Hedefi</p>
            <p className="commint-kpi-card__value">{metrics.shiftTargetGoodParts}</p>
          </article>
          <article className="commint-kpi-card">
            <p className="commint-kpi-card__label">Gerçekleşen İyi Parça</p>
            <p className="commint-kpi-card__value">{metrics.actualGoodParts}</p>
          </article>
          <article className="commint-kpi-card">
            <p className="commint-kpi-card__label">Projeksiyon (Vardiya Sonu)</p>
            <p className="commint-kpi-card__value">{metrics.projectedEndOfShiftGoodParts}</p>
          </article>
          <article className="commint-kpi-card">
            <p className="commint-kpi-card__label">İyi Parça / Saat</p>
            <p className="commint-kpi-card__value">
              {formatGoldenDemoNumber(metrics.throughputGoodPartsPerHour ?? 0, 1)}
            </p>
          </article>
          <article className="commint-kpi-card">
            <p className="commint-kpi-card__label">Cycle Time</p>
            <p className="commint-kpi-card__value">
              {formatGoldenDemoNumber(metrics.currentCycleSeconds ?? 0, 1)} sn
            </p>
          </article>
          <article className="commint-kpi-card">
            <p className="commint-kpi-card__label">Referans Cycle</p>
            <p className="commint-kpi-card__value">
              {metrics.referenceCycleSeconds != null
                ? `${formatGoldenDemoNumber(metrics.referenceCycleSeconds, 1)} sn`
                : "—"}
            </p>
          </article>
          <article className="commint-kpi-card">
            <p className="commint-kpi-card__label">Mikro-Duruş</p>
            <p className="commint-kpi-card__value">
              {metrics.microStopCount} / {formatGoldenDemoNumber(metrics.microStopDurationSeconds ?? 0, 0)} sn
            </p>
          </article>
          <article className="commint-kpi-card">
            <p className="commint-kpi-card__label">Red Oranı</p>
            <p className="commint-kpi-card__value">
              {formatGoldenDemoNumber(metrics.rejectRatePercent ?? 0, 1)}%
            </p>
          </article>
        </div>
        {workspace.baseline ? (
          <p data-testid="golden-demo-reference-source">
            Referans Kaynağı: {workspace.baseline.referenceSourceLabel}
          </p>
        ) : (
          <p>{workspace.cycleLoss.baselineFailureReason}</p>
        )}
        {metrics.unavailableMetrics
          ? Object.entries(metrics.unavailableMetrics).map(([key, label]) => (
              <p key={key} className="commint-table__secondary">
                {key}: {label}
              </p>
            ))
          : null}
      </section>

      <section className="commint-panel" data-testid="golden-demo-cycle-loss">
        <h2>Cycle Loss Ayrıştırma</h2>
        {workspace.cycleLoss.hasBaseline ? (
          <>
            <p>
              Referans {formatGoldenDemoNumber(workspace.cycleLoss.referenceCycleSeconds, 1)} sn · Mevcut{" "}
              {formatGoldenDemoNumber(workspace.cycleLoss.currentCycleSeconds, 1)} sn · Kayıp{" "}
              +{formatGoldenDemoNumber(workspace.cycleLoss.cycleLossSeconds, 1)} sn
            </p>
            <p>
              Mikro-duruş eşdeğer kaybı (ayrı):{" "}
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
          <p>{workspace.cycleLoss.baselineFailureReason}</p>
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
              ({contributor.contributorKind}) — {formatGoldenDemoNumber(contributor.measuredDeviationSeconds, 1)} sn ·{" "}
              {contributor.confidence} güven · {contributor.suggestedNextStep}
            </li>
          ))}
        </ul>
      </section>

      <section className="commint-panel" data-testid="golden-demo-recovery-opportunity">
        <h2>Üretim Kurtarma Fırsatı</h2>
        <p>Mevcut beklenen vardiya çıktısı: {workspace.recovery.currentExpectedShiftGoodParts}</p>
        <p>Kurtarılmış beklenen vardiya çıktısı: {workspace.recovery.recoveredExpectedShiftGoodParts}</p>
        <p>
          Kurtarma fırsatı: +{workspace.recovery.productionRecoveryGoodParts} iyi parça (
          {formatGoldenDemoNumber(workspace.recovery.productionRecoveryPercent, 1)}%)
        </p>
        <ul>
          {workspace.recovery.assumptions.map((assumption) => (
            <li key={assumption}>{assumption}</li>
          ))}
        </ul>
      </section>

      <section className="commint-panel" data-testid="golden-demo-what-if">
        <h2>What-If Senaryoları</h2>
        <ul>
          {workspace.whatIfScenarios.map((scenario) => (
            <li key={scenario.scenarioCode}>
              <strong>{scenario.title}</strong>
              {scenario.isRecommended ? " — RECOMMENDED" : ""}
              <p>Vardiya sonu projeksiyon: {scenario.projectedEndOfShiftGoodParts}</p>
              <p>{scenario.riskExposure}</p>
              {scenario.recommendationReason ? <p>{scenario.recommendationReason}</p> : null}
            </li>
          ))}
        </ul>
      </section>

      {digitalFactoryTeam ? (
        <GoldenDemoP2BDigitalFactoryPanel
          team={digitalFactoryTeam}
          pending={pending}
          onApprove={() => runAction(() => goldenDemoApprovePlanAction(eventId))}
        />
      ) : null}

      <section className="commint-panel" data-testid="golden-demo-recovery-plan">
        <h2>Önerilen Kurtarma Planı</h2>
        {workspace.plan ? <p>{workspace.plan.recommendationSummary}</p> : null}
        <ul>
          {workspace.planActions.map((action) => (
            <li key={action.id}>
              {action.sortOrder}. {action.title} — {action.responsibleRole} ·{" "}
              {getGoldenDemoPlanActionStatusLabel(action.status)}
            </li>
          ))}
        </ul>
        <div className="commint-action-row">
          <button
            type="button"
            disabled={pending || !isApproved || workspace.event.status !== "active"}
            onClick={() => runAction(() => goldenDemoExecutePlanAction(eventId))}
          >
            {pending ? "Başlatılıyor…" : "Müdahaleyi Başlat"}
          </button>
          <button
            type="button"
            disabled={pending || workspace.event.status !== "intervention-in-progress"}
            onClick={() => runAction(() => goldenDemoCompleteInterventionAction(eventId))}
          >
            {pending ? "Tamamlanıyor…" : "Müdahaleyi Tamamla"}
          </button>
          <button
            type="button"
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
