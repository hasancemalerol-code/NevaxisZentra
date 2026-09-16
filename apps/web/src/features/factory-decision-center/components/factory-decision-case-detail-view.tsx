"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState, useTransition } from "react";
import { ArrowLeft, CheckCircle2, ShieldAlert } from "lucide-react";

import {
  getFactoryDecisionSeverityLabel,
  getFactoryDecisionStatusLabel,
} from "../domain/factory-decision-center-labels";
import type { FactoryDecisionCaseDetailApi } from "../server/factory-decision-center-api-client";
import {
  factoryDecisionApproveAction,
  factoryDecisionExecuteAction,
  factoryDecisionVerifyAction,
} from "../server/factory-decision-center-actions";
import "@/features/commercial-intelligence/components/commercial-intelligence-workspace.css";

type FactoryDecisionCaseDetailViewProps = Readonly<{
  detail: FactoryDecisionCaseDetailApi;
}>;

export function FactoryDecisionCaseDetailView({ detail: initialDetail }: FactoryDecisionCaseDetailViewProps) {
  const router = useRouter();
  const [detail, setDetail] = useState(initialDetail);
  const [message, setMessage] = useState<string | null>(null);
  const [isPending, startTransition] = useTransition();

  function runAction(action: () => Promise<{ status: "success" | "failure"; message?: string; value?: FactoryDecisionCaseDetailApi }>) {
    setMessage(null);
    startTransition(async () => {
      const result = await action();
      if (result.status === "failure") {
        setMessage(result.message ?? "İşlem başarısız.");
        return;
      }
      if (result.value) {
        setDetail(result.value);
      }
      router.refresh();
    });
  }

  const showExecute = detail.status === "executing" && !detail.verifiedOutcome.isVerified;
  const showVerify = detail.status === "verifying" && !detail.verifiedOutcome.isVerified;
  const canApprove = detail.humanDecision?.status === "pending";

  return (
    <main className="commint-page" data-testid="factory-decision-case-detail-view">
      <header className="commint-executive">
        <div className="commint-executive__intro">
          <Link href="/fabrika-karar-merkezi" className="commint-button commint-button--ghost" style={{ marginBottom: "0.75rem" }}>
            <ArrowLeft aria-hidden="true" size={16} />
            Fabrika Karar Merkezi
          </Link>
          <p className="commint-executive__eyebrow">{getFactoryDecisionSeverityLabel(detail.severity)}</p>
          <h1>{detail.orderReference}</h1>
          <p className="commint-executive__subtitle">{detail.title}</p>
          <p className="commint-kpi-card__detail">{getFactoryDecisionStatusLabel(detail.status)} · {detail.facilityLabel}</p>
        </div>
      </header>

      {message ? <div className="commint-error-banner">{message}</div> : null}

      {detail.verifiedOutcome.isVerified ? (
        <section className="commint-panel" data-testid="factory-decision-verified-outcome" style={{ borderColor: "rgba(34, 197, 94, 0.35)" }}>
          <div className="commint-panel__header">
            <h2><CheckCircle2 aria-hidden="true" size={20} /> SONUÇ DOĞRULANDI</h2>
          </div>
          <div className="commint-kpi-grid" style={{ gridTemplateColumns: "repeat(4, minmax(0, 1fr))" }}>
            <div><p className="commint-kpi-card__label">Gerekli</p><p className="commint-kpi-card__value">{detail.verifiedOutcome.requiredQuantity}</p></div>
            <div><p className="commint-kpi-card__label">Doğrulanan projeksiyon</p><p className="commint-kpi-card__value">{detail.verifiedOutcome.verifiedProjection}</p></div>
            <div><p className="commint-kpi-card__label">Teslimat taahhüdü</p><p className="commint-kpi-card__value">{detail.verifiedOutcome.deliveryProtected ? "KORUNDU" : "RİSKLİ"}</p></div>
            <div><p className="commint-kpi-card__label">Kurtarma</p><p className="commint-kpi-card__value">+{detail.verifiedOutcome.recoveryGain} iyi parça</p></div>
          </div>
        </section>
      ) : null}

      <section className="commint-panel" data-testid="factory-decision-outcome-risk">
        <div className="commint-panel__header"><h2>1. SONUÇ RİSK ALTINDA</h2></div>
        <p className="commint-executive__subtitle">{detail.businessOutcomeAtRisk}</p>
        <div className="commint-kpi-grid" style={{ gridTemplateColumns: "repeat(5, minmax(0, 1fr))", marginTop: "1rem" }}>
          <div><p className="commint-kpi-card__label">Gerekli</p><p className="commint-kpi-card__value">{detail.outcomeRisk.requiredQuantity}</p></div>
          <div><p className="commint-kpi-card__label">Projeksiyon</p><p className="commint-kpi-card__value">{detail.outcomeRisk.projectedQuantity}</p></div>
          <div><p className="commint-kpi-card__label">Gap</p><p className="commint-kpi-card__value">{detail.outcomeRisk.gapQuantity}</p></div>
          <div><p className="commint-kpi-card__label">Kalan süre</p><p className="commint-kpi-card__value" style={{ fontSize: "1rem" }}>{detail.outcomeRisk.timeRemainingLabel}</p></div>
          <div><p className="commint-kpi-card__label">Güven</p><p className="commint-kpi-card__value">{Math.round(detail.outcomeRisk.confidence * 100)}%</p></div>
        </div>
      </section>

      <section className="commint-panel" data-testid="factory-decision-assessment">
        <div className="commint-panel__header"><h2>2. FACTOS DEĞERLENDİRMESİ</h2></div>
        <p className="commint-kpi-card__label">Birincil neden</p>
        <p className="commint-kpi-card__detail">{detail.assessment.primaryCause}</p>
        <p className="commint-kpi-card__label" style={{ marginTop: "1rem" }}>Katkıda olanlar</p>
        <ul>
          {detail.assessment.contributingFactors.map((factor) => (
            <li key={factor} className="commint-kpi-card__detail">{factor}</li>
          ))}
        </ul>
      </section>

      {detail.recoveryIntelligence ? (
        <section className="commint-panel" data-testid="factory-decision-recovery-intelligence">
          <div className="commint-panel__header"><h2>KURTARMA ZEKÂSI</h2></div>
          <div className="commint-kpi-grid" style={{ gridTemplateColumns: "repeat(4, minmax(0, 1fr))" }}>
            <div>
              <p className="commint-kpi-card__label">Benzer Doğrulanmış Vakalar</p>
              <p className="commint-kpi-card__value">{detail.recoveryIntelligence.similarVerifiedCaseCount}</p>
            </div>
            <div>
              <p className="commint-kpi-card__label">Tahmini Kurtarma Süresi</p>
              <p className="commint-kpi-card__value">
                {detail.recoveryIntelligence.expectedRecoveryMinutesMedian
                  ? `${detail.recoveryIntelligence.expectedRecoveryMinutesMedian} dk`
                  : "—"}
              </p>
            </div>
            <div>
              <p className="commint-kpi-card__label">Kanıt Gücü</p>
              <p className="commint-kpi-card__value" style={{ fontSize: "1rem" }}>
                {formatEvidenceStrength(detail.recoveryIntelligence.evidenceStrength)}
              </p>
            </div>
            <div>
              <p className="commint-kpi-card__label">Bilinen Başarısızlık Modları</p>
              <p className="commint-kpi-card__value">{detail.recoveryIntelligence.knownFailureModes.length}</p>
            </div>
          </div>

          {detail.recoveryIntelligence.similarCases.length > 0 ? (
            <div style={{ marginTop: "1rem" }}>
              <p className="commint-kpi-card__label">Benzer Doğrulanmış Vakalar</p>
              {detail.recoveryIntelligence.similarCases.slice(0, 3).map((item) => (
                <article key={item.learningRecordId} className="commint-kpi-card" style={{ marginTop: "0.75rem" }}>
                  <p className="commint-kpi-card__label">
                    %{item.similarityPercent} BENZER · {item.machineCode} / {item.orderReference}
                  </p>
                  <p className="commint-kpi-card__detail">{item.problemSummary}</p>
                  <p className="commint-kpi-card__detail">
                    Uygulanan strateji: {item.appliedStrategy} · Sonuç: {item.outcomeSummary}
                    {item.recoveryMinutes ? ` · ${item.recoveryMinutes} dk` : ""}
                  </p>
                  <p className="commint-kpi-card__detail">
                    Neden: {item.similarityReasons.join(" · ")}
                  </p>
                </article>
              ))}
            </div>
          ) : null}

          {detail.recoveryIntelligence.planEvidence.length > 0 ? (
            <div style={{ marginTop: "1rem" }}>
              <p className="commint-kpi-card__label">Geçmiş Performans (Plan Karşılaştırması)</p>
              <div style={{ display: "grid", gap: "0.75rem", marginTop: "0.5rem" }}>
                {detail.recoveryIntelligence.planEvidence.map((plan) => (
                  <div key={plan.planKey} className="commint-kpi-card">
                    <p className="commint-kpi-card__label">PLAN {plan.planKey.toUpperCase()}</p>
                    <p className="commint-kpi-card__detail">
                      {plan.verifiedSuccesses}/{plan.similarVerifiedCaseCount} doğrulanmış başarı
                      {plan.medianRecoveryMinutes ? ` · Medyan ${plan.medianRecoveryMinutes} dk` : ""}
                      {` · Örnek güveni: ${plan.sampleConfidence}`}
                    </p>
                    {plan.knownRisks.length > 0 ? (
                      <p className="commint-kpi-card__detail">Bilinen risk: {plan.knownRisks.join(" · ")}</p>
                    ) : null}
                  </div>
                ))}
              </div>
            </div>
          ) : null}
        </section>
      ) : null}

      <section className="commint-panel" data-testid="factory-decision-recovery-plan">
        <div className="commint-panel__header"><h2>3. KURTARMA PLANI</h2></div>
        <ol style={{ display: "grid", gap: "0.75rem", paddingLeft: "1.25rem" }}>
          {detail.recoveryPlan.map((step) => (
            <li key={`${step.order}-${step.actionKey ?? step.title}`}>
              <strong>{String(step.order).padStart(2, "0")}</strong> — {step.title}
              <span className="commint-kpi-card__detail"> · {step.workerName}</span>
            </li>
          ))}
        </ol>
      </section>

      <section className="commint-panel" data-testid="factory-decision-digital-workforce">
        <div className="commint-panel__header"><h2>4. DİJİTAL İŞ GÜCÜ</h2></div>
        <div style={{ display: "grid", gap: "0.75rem" }}>
          {detail.digitalWorkers.map((worker) => (
            <div key={`${worker.workerCode}-${worker.responsibility}`} className="commint-kpi-card">
              <p className="commint-kpi-card__label">{worker.workerName}</p>
              <p className="commint-kpi-card__detail">{worker.responsibility}</p>
              <p className="commint-kpi-card__badge">{worker.status}</p>
            </div>
          ))}
        </div>
      </section>

      {detail.humanDecision ? (
        <section className="commint-panel" data-testid="factory-decision-human-approval">
          <div className="commint-panel__header">
            <h2><ShieldAlert aria-hidden="true" size={20} /> 5. İNSAN KARARI</h2>
          </div>
          <p className="commint-executive__eyebrow">İNSAN KARARI GEREKLİ</p>
          <p className="commint-kpi-card__label">Karar</p>
          <p className="commint-kpi-card__detail">{detail.humanDecision.decision}</p>
          <p className="commint-kpi-card__label" style={{ marginTop: "0.75rem" }}>Etki</p>
          <p className="commint-kpi-card__detail">{detail.humanDecision.impact}</p>
          {canApprove ? (
            <div className="commint-executive__actions" style={{ marginTop: "1rem" }}>
              <button
                type="button"
                className="commint-button"
                disabled={isPending}
                data-testid="factory-decision-approve"
                onClick={() =>
                  runAction(() =>
                    factoryDecisionApproveAction(
                      detail.caseId,
                      detail.humanDecision!.approvalId,
                      true,
                      detail.concurrencyToken,
                    ),
                  )
                }
              >
                Onayla ve Yürüt
              </button>
              <button
                type="button"
                className="commint-button commint-button--ghost"
                disabled={isPending}
                data-testid="factory-decision-reject"
                onClick={() =>
                  runAction(() =>
                    factoryDecisionApproveAction(
                      detail.caseId,
                      detail.humanDecision!.approvalId,
                      false,
                      detail.concurrencyToken,
                    ),
                  )
                }
              >
                Reddet
              </button>
            </div>
          ) : null}
        </section>
      ) : null}

      {detail.executionProgress.length > 0 ? (
        <section className="commint-panel" data-testid="factory-decision-execution">
          <div className="commint-panel__header"><h2>6. CANLI YÜRÜTME</h2></div>
          <div style={{ display: "grid", gap: "0.75rem" }}>
            {detail.executionProgress.map((item) => (
              <div key={`${item.workerCode}-${item.responsibility}`} className="commint-kpi-card">
                <p className="commint-kpi-card__label">{item.workerName}</p>
                <p className="commint-kpi-card__detail">{item.responsibility}</p>
                <p className="commint-kpi-card__badge">{item.status}</p>
              </div>
            ))}
          </div>
          {showExecute ? (
            <div className="commint-executive__actions" style={{ marginTop: "1rem" }}>
              <button
                type="button"
                className="commint-button"
                disabled={isPending}
                data-testid="factory-decision-execute"
                onClick={() =>
                  runAction(() => factoryDecisionExecuteAction(detail.caseId, detail.concurrencyToken))
                }
              >
                Kurtarma Aksiyonlarını Yürüt
              </button>
            </div>
          ) : null}
        </section>
      ) : null}

      <section className="commint-panel" data-testid="factory-decision-evidence">
        <div className="commint-panel__header"><h2>7. KANIT & DOĞRULAMA</h2></div>
        <div style={{ display: "grid", gap: "0.75rem" }}>
          {detail.evidence.map((item) => (
            <div key={`${item.category}-${item.label}`} className="commint-kpi-card">
              <p className="commint-kpi-card__label">{item.label}</p>
              <p className="commint-kpi-card__detail">{item.summary}</p>
              <p className="commint-kpi-card__badge">{item.status}</p>
            </div>
          ))}
        </div>
        {showVerify ? (
          <div className="commint-executive__actions" style={{ marginTop: "1rem" }}>
            <button
              type="button"
              className="commint-button"
              disabled={isPending}
              data-testid="factory-decision-verify"
              onClick={() =>
                runAction(() => factoryDecisionVerifyAction(detail.caseId, detail.concurrencyToken))
              }
            >
              İş Sonucunu Doğrula
            </button>
          </div>
        ) : null}
      </section>
    </main>
  );
}

function formatEvidenceStrength(strength: string): string {
  switch (strength) {
    case "GUC_LU_KANIT":
      return "GÜÇLÜ KANIT";
    case "ORTA_KANIT":
      return "ORTA KANIT";
    case "DUSUK_KANIT":
      return "DÜŞÜK KANIT";
    default:
      return strength;
  }
}
