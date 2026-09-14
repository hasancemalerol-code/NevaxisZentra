"use client";

import { Badge } from "@/components/ui/badge";
import { formatGoldenDemoNumber } from "@/features/golden-demo-p1/domain/golden-demo-p1-labels";

import type { GoldenDemoP3OutcomeApi } from "../server/golden-demo-p3-api-client";

function formatTimestamp(value?: string | null) {
  if (!value) {
    return "—";
  }

  return new Intl.DateTimeFormat("tr-TR", {
    dateStyle: "short",
    timeStyle: "medium",
  }).format(new Date(value));
}

export function GoldenDemoP3OutcomePanel({ outcome }: Readonly<{ outcome: GoldenDemoP3OutcomeApi }>) {
  const heroClass = outcome.hero.showSuccessHero ? "commint-kpi-card" : "commint-panel";

  return (
    <>
      <section className="commint-panel" data-testid="golden-demo-p3-validation-run">
        <h2>Validation Run</h2>
        <p>{outcome.validationWindow.summary}</p>
        <div className="commint-kpi-grid">
          <article className="commint-kpi-card">
            <p className="commint-kpi-card__label">Status</p>
            <p className="commint-kpi-card__value">{outcome.validationWindow.statusLabel}</p>
          </article>
          <article className="commint-kpi-card">
            <p className="commint-kpi-card__label">Samples Collected</p>
            <p className="commint-kpi-card__value">
              {outcome.validationWindow.samplesCollected ?? "—"} / {outcome.validationWindow.minimumSamplesRequired ?? "—"}
            </p>
          </article>
          <article className="commint-kpi-card">
            <p className="commint-kpi-card__label">Evidence Completeness</p>
            <p className="commint-kpi-card__value">{outcome.validationWindow.evidenceCompleteness}</p>
          </article>
          <article className="commint-kpi-card">
            <p className="commint-kpi-card__label">Intervention vs Outcome</p>
            <p className="commint-kpi-card__value">
              {outcome.interventionStatus} / {outcome.outcomeStatus}
            </p>
          </article>
        </div>
        <p className="commint-table__secondary">
          Validation started: {formatTimestamp(outcome.validationWindow.validationStartedAtUtc)}
        </p>
      </section>

      <section className={heroClass} data-testid="golden-demo-p3-outcome-hero">
        <h1>{outcome.hero.headline}</h1>
        <p>{outcome.hero.subheadline}</p>
        <p className="commint-kpi-card__value" data-testid="golden-demo-p3-recovered-good-parts">
          +{formatGoldenDemoNumber(outcome.hero.recoveredGoodPartsPerShift)} GOOD PARTS / SHIFT
        </p>
        <p className="commint-table__secondary">
          {outcome.hero.recoveredGoodPartsClassification} · Verification: {outcome.hero.verificationResult} · Confidence:{" "}
          {outcome.verificationConfidence}
        </p>
        {outcome.hero.highlights.length > 0 ? (
          <ul>
            {outcome.hero.highlights.map((item) => (
              <li key={item.label}>
                <strong>{item.label}</strong>: {item.beforeValue} → {item.afterValue} · {item.deltaLabel}
              </li>
            ))}
          </ul>
        ) : null}
      </section>

      <section className="commint-panel" data-testid="golden-demo-p3-before-after">
        <h2>Before / After</h2>
        <div className="commint-kpi-grid">
          <article className="commint-kpi-card">
            <p className="commint-kpi-card__label">Baseline Window</p>
            <p>{outcome.baselineWindow.sourceLabel}</p>
            <p className="commint-table__secondary">
              {formatTimestamp(outcome.baselineWindow.windowStartUtc)} → {formatTimestamp(outcome.baselineWindow.windowEndUtc)}
            </p>
            <p>{outcome.baselineWindow.productionContext}</p>
          </article>
          {outcome.validationMeasurementWindow ? (
            <article className="commint-kpi-card">
              <p className="commint-kpi-card__label">Validation Window</p>
              <p>{outcome.validationMeasurementWindow.sourceLabel}</p>
              <p className="commint-table__secondary">
                {formatTimestamp(outcome.validationMeasurementWindow.windowStartUtc)} →{" "}
                {formatTimestamp(outcome.validationMeasurementWindow.windowEndUtc)}
              </p>
              <p>{outcome.validationMeasurementWindow.productionContext}</p>
            </article>
          ) : null}
        </div>
        <table>
          <thead>
            <tr>
              <th>Metric</th>
              <th>Before</th>
              <th>After</th>
              <th>Delta</th>
              <th>Status</th>
              <th>Class</th>
            </tr>
          </thead>
          <tbody>
            {outcome.metricComparisons.map((metric) => (
              <tr key={metric.metricCode}>
                <td>{metric.label}</td>
                <td>{metric.beforeValue ?? "—"}</td>
                <td>{metric.afterValue ?? "—"}</td>
                <td>{metric.deltaLabel ?? "—"}</td>
                <td>{metric.status}</td>
                <td>{metric.classification}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>

      <section className="commint-panel" data-testid="golden-demo-p3-outcome-verification">
        <h2>Outcome Verification</h2>
        <ul>
          {outcome.verificationChecks.map((check) => (
            <li key={check.code}>
              <strong>{check.label}</strong> · {check.status}
              <Badge>{check.classification}</Badge>
              <p>{check.explanation}</p>
            </li>
          ))}
        </ul>
        {outcome.insufficientEvidenceReasons.length > 0 ? (
          <ul data-testid="golden-demo-p3-insufficient-evidence">
            {outcome.insufficientEvidenceReasons.map((reason) => (
              <li key={reason}>{reason}</li>
            ))}
          </ul>
        ) : null}
      </section>

      <section className="commint-panel" data-testid="golden-demo-p3-energy-impact">
        <h2>Energy Impact</h2>
        <p>{outcome.energyImpact.summary}</p>
        <p className="commint-table__secondary">
          {outcome.energyImpact.classification} · {outcome.energyImpact.relationshipNote}
        </p>
      </section>

      <section className="commint-panel" data-testid="golden-demo-p3-business-impact">
        <h2>Business Impact / Profit Leak Summary</h2>
        <p>
          Operational recovery: +{outcome.businessImpact.operationalGoodPartsPerShift} good parts / shift ·{" "}
          {outcome.businessImpact.operationalClassification}
        </p>
        <p>Capacity impact: {formatGoldenDemoNumber(outcome.businessImpact.capacityImpactPercent, 1)}%</p>
        {outcome.businessImpact.estimatedBusinessImpactTry != null ? (
          <p>
            Estimated business impact: ₺{formatGoldenDemoNumber(outcome.businessImpact.estimatedBusinessImpactTry)} / shift ·{" "}
            {outcome.businessImpact.estimatedBusinessClassification}
          </p>
        ) : null}
        {outcome.businessImpact.modeledVerifiedBusinessImpactTry != null ? (
          <p>
            Modeled business impact from verified recovery: ₺
            {formatGoldenDemoNumber(outcome.businessImpact.modeledVerifiedBusinessImpactTry)} / shift ·{" "}
            {outcome.businessImpact.modeledVerifiedBusinessClassification}
          </p>
        ) : null}
        <ul>
          {outcome.businessImpact.categories.map((category) => (
            <li key={category.categoryCode}>
              <strong>{category.label}</strong> · Before {category.beforeLabel} · After {category.afterLabel} ·{" "}
              {category.protectedOrRecoveredLabel} · {category.classification}
            </li>
          ))}
        </ul>
      </section>

      <section className="commint-panel" data-testid="golden-demo-p3-traceability">
        <h2>Outcome Traceability</h2>
        <p>{outcome.traceability.workflowSummary}</p>
        <ul>
          {outcome.traceability.planReferenceId ? <li>Plan: {outcome.traceability.planReferenceId}</li> : null}
          {outcome.traceability.verificationReferenceId ? (
            <li>Verification: {outcome.traceability.verificationReferenceId}</li>
          ) : null}
          {outcome.traceability.interventionReferenceId ? (
            <li>Intervention: {outcome.traceability.interventionReferenceId}</li>
          ) : null}
          {outcome.traceability.sourceAgentReferences.map((reference) => (
            <li key={reference}>{reference}</li>
          ))}
        </ul>
        <p className="commint-table__secondary">
          FactOS coordinated and verified the intervention. Authorized operator/maintenance role executed the physical
          intervention.
        </p>
      </section>
    </>
  );
}
