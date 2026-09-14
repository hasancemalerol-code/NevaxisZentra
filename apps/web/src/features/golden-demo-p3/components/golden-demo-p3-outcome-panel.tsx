"use client";

import { Badge } from "@/components/ui/badge";
import {
  formatGoldenDemoNumber,
  getGoldenDemoClassificationLabel,
  getGoldenDemoEventStatusLabel,
  getGoldenDemoVerificationLabel,
} from "@/features/golden-demo-p1/domain/golden-demo-p1-labels";

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
        <h2>Doğrulama Koşusu</h2>
        <p>{outcome.validationWindow.summary}</p>
        <div className="commint-kpi-grid">
          <article className="commint-kpi-card">
            <p className="commint-kpi-card__label">Durum</p>
            <p className="commint-kpi-card__value">{outcome.validationWindow.statusLabel}</p>
          </article>
          <article className="commint-kpi-card">
            <p className="commint-kpi-card__label">Toplanan Örnek</p>
            <p className="commint-kpi-card__value">
              {outcome.validationWindow.samplesCollected ?? "—"} / {outcome.validationWindow.minimumSamplesRequired ?? "—"}
            </p>
          </article>
          <article className="commint-kpi-card">
            <p className="commint-kpi-card__label">Kanıt Tamlığı</p>
            <p className="commint-kpi-card__value">{outcome.validationWindow.evidenceCompleteness}</p>
          </article>
          <article className="commint-kpi-card">
            <p className="commint-kpi-card__label">Müdahale / Sonuç</p>
            <p className="commint-kpi-card__value">
              {getGoldenDemoEventStatusLabel(outcome.interventionStatus)} /{" "}
              {getGoldenDemoVerificationLabel(outcome.outcomeStatus)}
            </p>
          </article>
        </div>
        <p className="commint-table__secondary">
          Doğrulama başlangıcı: {formatTimestamp(outcome.validationWindow.validationStartedAtUtc)}
        </p>
      </section>

      <section
        className={heroClass}
        data-testid="golden-demo-p3-outcome-hero"
        data-outcome-status={outcome.outcomeStatus}
        data-verified={outcome.hero.showSuccessHero ? "true" : "false"}
      >
        <h1>{outcome.hero.headline}</h1>
        <p>{outcome.hero.subheadline}</p>
        {outcome.hero.showSuccessHero ? (
          <p className="commint-kpi-card__value" data-testid="golden-demo-p3-recovered-good-parts">
            +{formatGoldenDemoNumber(outcome.hero.recoveredGoodPartsPerShift)} GOOD PARTS / SHIFT
          </p>
        ) : (
          <p className="commint-table__secondary">
            Hesaplanan kurtarma fırsatı: +{formatGoldenDemoNumber(outcome.hero.recoveredGoodPartsPerShift)} iyi parça /
            vardiya · {getGoldenDemoClassificationLabel(outcome.hero.recoveredGoodPartsClassification)}
          </p>
        )}
        <p className="commint-table__secondary">
          {getGoldenDemoClassificationLabel(outcome.hero.recoveredGoodPartsClassification)} · Doğrulama:{" "}
          {getGoldenDemoVerificationLabel(outcome.hero.verificationResult)} · Güven: {outcome.verificationConfidence}
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
        <h2>Önce / Sonra</h2>
        <div className="commint-kpi-grid">
          <article className="commint-kpi-card">
            <p className="commint-kpi-card__label">Baseline Penceresi</p>
            <p>{outcome.baselineWindow.sourceLabel}</p>
            <p className="commint-table__secondary">
              {formatTimestamp(outcome.baselineWindow.windowStartUtc)} → {formatTimestamp(outcome.baselineWindow.windowEndUtc)}
            </p>
            <p>{outcome.baselineWindow.productionContext}</p>
          </article>
          {outcome.validationMeasurementWindow ? (
            <article className="commint-kpi-card">
              <p className="commint-kpi-card__label">Doğrulama Penceresi</p>
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
              <th>Metrik</th>
              <th>Önce</th>
              <th>Sonra</th>
              <th>Delta</th>
              <th>Durum</th>
              <th>Sınıf</th>
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
                <td>{getGoldenDemoClassificationLabel(metric.classification)}</td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>

      <section className="commint-panel" data-testid="golden-demo-p3-outcome-verification">
        <h2>Sonuç Doğrulama</h2>
        <ul>
          {outcome.verificationChecks.map((check) => (
            <li key={check.code}>
              <strong>{check.label}</strong> · {check.status}
              <Badge>{getGoldenDemoClassificationLabel(check.classification)}</Badge>
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
        <h2>Enerji Etkisi</h2>
        <p>{outcome.energyImpact.summary}</p>
        <p className="commint-table__secondary">
          {getGoldenDemoClassificationLabel(outcome.energyImpact.classification)} · {outcome.energyImpact.relationshipNote}
        </p>
      </section>

      <section className="commint-panel" data-testid="golden-demo-p3-business-impact">
        <h2>Tahmini İş Etkisi</h2>
        <p>
          Operasyonel kurtarma: +{outcome.businessImpact.operationalGoodPartsPerShift} iyi parça / vardiya ·{" "}
          {getGoldenDemoClassificationLabel(outcome.businessImpact.operationalClassification)}
        </p>
        <p>Kapasite etkisi: {formatGoldenDemoNumber(outcome.businessImpact.capacityImpactPercent, 1)}%</p>
        {outcome.businessImpact.estimatedBusinessImpactTry != null ? (
          <p>
            Tahmini iş etkisi: ₺{formatGoldenDemoNumber(outcome.businessImpact.estimatedBusinessImpactTry)} / vardiya ·{" "}
            {getGoldenDemoClassificationLabel(outcome.businessImpact.estimatedBusinessClassification)}
          </p>
        ) : null}
        {outcome.businessImpact.modeledVerifiedBusinessImpactTry != null ? (
          <p>
            Doğrulanmış kurtarmadan modellenen etki: ₺
            {formatGoldenDemoNumber(outcome.businessImpact.modeledVerifiedBusinessImpactTry)} / vardiya ·{" "}
            {getGoldenDemoClassificationLabel(outcome.businessImpact.modeledVerifiedBusinessClassification)}
          </p>
        ) : null}
        <ul>
          {outcome.businessImpact.categories.map((category) => (
            <li key={category.categoryCode}>
              <strong>{category.label}</strong> · Önce {category.beforeLabel} · Sonra {category.afterLabel} ·{" "}
              {category.protectedOrRecoveredLabel} · {getGoldenDemoClassificationLabel(category.classification)}
            </li>
          ))}
        </ul>
      </section>

      <section className="commint-panel" data-testid="golden-demo-p3-traceability">
        <h2>İzlenebilirlik</h2>
        <p>{outcome.traceability.workflowSummary}</p>
        <ul>
          {outcome.traceability.planReferenceId ? <li>Plan: {outcome.traceability.planReferenceId}</li> : null}
          {outcome.traceability.verificationReferenceId ? (
            <li>Doğrulama: {outcome.traceability.verificationReferenceId}</li>
          ) : null}
          {outcome.traceability.interventionReferenceId ? (
            <li>Müdahale: {outcome.traceability.interventionReferenceId}</li>
          ) : null}
          {outcome.traceability.sourceAgentReferences.map((reference) => (
            <li key={reference}>{reference}</li>
          ))}
        </ul>
        <p className="commint-table__secondary">
          FactOS müdahaleyi koordine etti ve doğruladı. Yetkili operatör/bakım rolü fiziksel müdahaleyi uyguladı.
        </p>
      </section>
    </>
  );
}
