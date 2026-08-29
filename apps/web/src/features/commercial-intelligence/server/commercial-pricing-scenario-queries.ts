import "./commercial-intelligence-server-boundary";

import type { CommercialPricingScenarioApiFilters } from "../application/commercial-pricing-scenario-filter";
import {
  formatCompactPeriod,
  formatCount,
  formatCurrency,
  formatDateTime,
  formatRatioAsPercent,
} from "../domain/commercial-intelligence-formatters";
import { getCommercialIntelligenceEvidenceStatusLabel } from "../domain/commercial-intelligence-labels";
import {
  getCommercialPricingScenarioAssessmentOutcomeLabel,
  getCommercialPricingScenarioAuditEventLabel,
  getCommercialPricingScenarioCostCoverageLabel,
  getCommercialPricingScenarioDriverSeverityLabel,
  getCommercialPricingScenarioGuardrailLabel,
  getCommercialPricingScenarioLifecycleLabel,
} from "../domain/commercial-pricing-scenario-labels";
import { getCommercialProfitabilityDriverReasonLabel } from "../domain/commercial-profitability-labels";
import { localizeCommercialProfitabilityProvenance } from "../domain/commercial-profitability-labels";
import type {
  CommercialPricingScenarioAuditEvent,
  CommercialPricingScenarioDetail,
  CommercialPricingScenarioEvaluationSnapshot,
  CommercialPricingScenarioSummary,
  CommercialPricingScenarioTableRow,
  CommercialPricingScenarioWorkspacePayload,
} from "../types/commercial-pricing-scenario-types";
import type { CommercialProfitabilityKpiCard } from "../types/commercial-profitability-types";
import { resolveProfitabilityMaterialLabel } from "../components/commercial-profitability-presentation";
import { denyUnlessGranted } from "../../admin-access/server/require-current-permission";
import {
  getCommercialPricingScenario,
  getCommercialPricingScenarioWorkspace,
  listCommercialPricingScenarios,
  type RawCommercialPricingScenarioAuditEvent,
  type RawCommercialPricingScenarioDetail,
  type RawCommercialPricingScenarioDriver,
  type RawCommercialPricingScenarioEvaluationSnapshot,
  type RawCommercialPricingScenarioSummary,
} from "./commercial-pricing-scenario-api-client";
import { resolveCommercialIntelligenceRequestContext } from "./commercial-intelligence-request-context";

export type { CommercialPricingScenarioApiFilters };

const DEFAULT_CURRENCY = "TRY";

type ReadFailure = Readonly<{ status: "failure"; message: string; code?: string }>;

export type CommercialPricingScenarioWorkspaceResult =
  | Readonly<{ status: "success"; workspace: CommercialPricingScenarioWorkspacePayload }>
  | ReadFailure;

export type CommercialPricingScenarioDetailResult =
  | Readonly<{ status: "success"; detail: CommercialPricingScenarioDetail }>
  | ReadFailure;

function buildScenarioKpiCards(kpis: Readonly<{
  draftCount: number;
  needsReviewCount: number;
  eligibleCount: number;
  blockedCount: number;
  approvedCount: number;
}>): readonly CommercialProfitabilityKpiCard[] {
  return [
    {
      id: "scenario-draft",
      label: "Taslak",
      value: formatCount(kpis.draftCount),
      hint: "Değerlendirme bekliyor",
      tone: kpis.draftCount > 0 ? "warning" : "neutral",
    },
    {
      id: "scenario-needs-review",
      label: "İnceleme gerekli",
      value: formatCount(kpis.needsReviewCount),
      hint: "Koruma incelemesi",
      tone: kpis.needsReviewCount > 0 ? "warning" : "neutral",
    },
    {
      id: "scenario-eligible",
      label: "Onaya uygun",
      value: formatCount(kpis.eligibleCount),
      hint: "Koruma geçti",
      tone: kpis.eligibleCount > 0 ? "positive" : "neutral",
    },
    {
      id: "scenario-blocked",
      label: "Engellendi",
      value: formatCount(kpis.blockedCount),
      hint: "Koruma engeli",
      tone: kpis.blockedCount > 0 ? "negative" : "neutral",
    },
    {
      id: "scenario-approved",
      label: "Onaylandı",
      value: formatCount(kpis.approvedCount),
      hint: "İnsan onayı alındı",
      tone: kpis.approvedCount > 0 ? "positive" : "neutral",
    },
  ];
}

function mapAuditEvent(event: RawCommercialPricingScenarioAuditEvent): CommercialPricingScenarioAuditEvent {
  return {
    id: event.id,
    eventType: event.eventType,
    eventTypeLabel: getCommercialPricingScenarioAuditEventLabel(event.eventType),
    payloadJson: event.payloadJson,
    actorSubjectId: event.actorSubjectId,
    occurredAtUtc: event.occurredAtUtc,
    occurredLabel: formatDateTime(event.occurredAtUtc),
  };
}

function mapDriver(driver: RawCommercialPricingScenarioDriver): CommercialPricingScenarioEvaluationSnapshot["drivers"][number] {
  return {
    reasonCode: driver.reasonCode,
    reasonLabel: getCommercialProfitabilityDriverReasonLabel(driver.reasonCode),
    severity: driver.severity,
    severityLabel: getCommercialPricingScenarioDriverSeverityLabel(driver.severity),
    actualValue: driver.actualValue,
    thresholdValue: driver.thresholdValue,
    unit: driver.unit,
    normalizedVariance: driver.normalizedVariance,
    normalizedVarianceLabel: formatRatioAsPercent(driver.normalizedVariance),
    affectedComponentCount: driver.affectedComponentCount,
    supportingText: driver.supportingText,
  };
}

function mapEvaluationSnapshot(
  snapshot: RawCommercialPricingScenarioEvaluationSnapshot,
): CommercialPricingScenarioEvaluationSnapshot {
  return {
    id: snapshot.id,
    scenarioVersionAtEvaluation: snapshot.scenarioVersionAtEvaluation,
    evaluationSequence: snapshot.evaluationSequence,
    guardrailResult: snapshot.guardrailResult,
    guardrailResultLabel: getCommercialPricingScenarioGuardrailLabel(snapshot.guardrailResult),
    assessmentOutcome: snapshot.assessmentOutcome,
    assessmentOutcomeLabel: getCommercialPricingScenarioAssessmentOutcomeLabel(snapshot.assessmentOutcome),
    evidenceStatus: snapshot.evidenceStatus,
    evidenceStatusLabel: getCommercialIntelligenceEvidenceStatusLabel(snapshot.evidenceStatus),
    costCoverageStatus: snapshot.costCoverageStatus,
    costCoverageStatusLabel: getCommercialPricingScenarioCostCoverageLabel(snapshot.costCoverageStatus),
    leadingDriverCode: snapshot.leadingDriverCode,
    leadingDriverLabel: getCommercialProfitabilityDriverReasonLabel(snapshot.leadingDriverCode),
    insufficientDataReason: snapshot.insufficientDataReason,
    baselineUnitPrice: snapshot.baselineUnitPrice,
    baselineUnitPriceLabel: formatCurrency(snapshot.baselineUnitPrice, DEFAULT_CURRENCY),
    proposedUnitPrice: snapshot.proposedUnitPrice,
    proposedUnitPriceLabel: formatCurrency(snapshot.proposedUnitPrice, DEFAULT_CURRENCY),
    totalIncludedUnitCost: snapshot.totalIncludedUnitCost,
    totalIncludedUnitCostLabel: formatCurrency(snapshot.totalIncludedUnitCost, DEFAULT_CURRENCY),
    expectedUnitContribution: snapshot.expectedUnitContribution,
    expectedUnitContributionLabel: formatCurrency(snapshot.expectedUnitContribution, DEFAULT_CURRENCY),
    expectedContributionMarginRatio: snapshot.expectedContributionMarginRatio,
    expectedContributionMarginRatioLabel: formatRatioAsPercent(snapshot.expectedContributionMarginRatio),
    baselineContributionMarginRatio: snapshot.baselineContributionMarginRatio,
    baselineContributionMarginRatioLabel: formatRatioAsPercent(snapshot.baselineContributionMarginRatio),
    contributionMarginDelta: snapshot.contributionMarginDelta,
    contributionMarginDeltaLabel: formatRatioAsPercent(snapshot.contributionMarginDelta),
    profileMinimumContributionMarginRatio: snapshot.profileMinimumContributionMarginRatio,
    profileMinimumContributionMarginRatioLabel: formatRatioAsPercent(snapshot.profileMinimumContributionMarginRatio),
    profileId: snapshot.profileId,
    profileVersion: snapshot.profileVersion,
    ruleSetVersion: snapshot.ruleSetVersion,
    evaluatedAtUtc: snapshot.evaluatedAtUtc,
    evaluatedLabel: formatDateTime(snapshot.evaluatedAtUtc),
    evaluatedBy: snapshot.evaluatedBy,
    drivers: (snapshot.drivers ?? []).map(mapDriver),
  };
}

function mapScenarioSummary(item: RawCommercialPricingScenarioSummary): CommercialPricingScenarioSummary {
  return {
    id: item.id,
    scenarioReference: item.scenarioReference,
    lifecycleStatus: item.lifecycleStatus,
    lifecycleStatusLabel: getCommercialPricingScenarioLifecycleLabel(item.lifecycleStatus),
    guardrailResult: item.guardrailResult,
    guardrailResultLabel: getCommercialPricingScenarioGuardrailLabel(item.guardrailResult),
    profileId: item.profileId,
    materialId: item.materialId,
    materialLabel: resolveProfitabilityMaterialLabel(item.materialId),
    customerReference: item.customerReference,
    baselineUnitPrice: item.baselineUnitPrice,
    baselineUnitPriceLabel: formatCurrency(item.baselineUnitPrice, DEFAULT_CURRENCY),
    proposedUnitPrice: item.proposedUnitPrice,
    proposedUnitPriceLabel: formatCurrency(item.proposedUnitPrice, DEFAULT_CURRENCY),
    proposedDiscountRatio: item.proposedDiscountRatio,
    proposedDiscountRatioLabel: formatRatioAsPercent(item.proposedDiscountRatio),
    validityWindowStartUtc: item.validityWindowStartUtc,
    validityWindowEndUtc: item.validityWindowEndUtc,
    validityWindowLabel: formatCompactPeriod(item.validityWindowStartUtc, item.validityWindowEndUtc),
    linkedAiTaskId: item.linkedAiTaskId,
    evaluationCount: item.evaluationCount,
    createdAtUtc: item.createdAtUtc,
    createdLabel: formatDateTime(item.createdAtUtc),
    updatedAtUtc: item.updatedAtUtc,
    updatedLabel: formatDateTime(item.updatedAtUtc),
    version: item.version,
  };
}

function mapScenarioTableRow(item: CommercialPricingScenarioSummary): CommercialPricingScenarioTableRow {
  return {
    id: item.id,
    scenarioReference: item.scenarioReference,
    lifecycleLabel: item.lifecycleStatusLabel,
    guardrailLabel: item.guardrailResultLabel,
    proposedPriceLabel: item.proposedUnitPriceLabel,
    baselinePriceLabel: item.baselineUnitPriceLabel,
    customerReference: item.customerReference,
    materialLabel: item.materialLabel,
    evaluationCount: item.evaluationCount,
    updatedLabel: item.updatedLabel,
    lifecycleStatus: item.lifecycleStatus,
    guardrailResult: item.guardrailResult,
  };
}

function mapScenarioDetail(item: RawCommercialPricingScenarioDetail): CommercialPricingScenarioDetail {
  const summary = mapScenarioSummary(item);

  return {
    ...summary,
    profileVersion: item.profileVersion,
    salesEvidenceObservationId: item.salesEvidenceObservationId,
    quantity: item.quantity,
    quantityLabel: item.quantity === null ? "—" : formatCount(item.quantity),
    commercialRationale: item.commercialRationale,
    latestEvaluationSnapshotId: item.latestEvaluationSnapshotId,
    ruleSetVersion: item.ruleSetVersion,
    sourceProvenance: localizeCommercialProfitabilityProvenance(item.sourceProvenance),
    automationDisclaimer: item.automationDisclaimer ?? "Bu onay fiyatı otomatik olarak uygulamaz.",
    deepLink: item.deepLink ?? `/ticari-zeka/fiyat-koruma-ve-onay?scenario=${item.id}`,
    latestEvaluation: item.latestEvaluation ? mapEvaluationSnapshot(item.latestEvaluation) : null,
    evaluationHistory: (item.evaluationHistory ?? []).map(mapEvaluationSnapshot),
    auditEvents: (item.auditEvents ?? [])
      .slice()
      .sort((left, right) => Date.parse(right.occurredAtUtc) - Date.parse(left.occurredAtUtc))
      .map(mapAuditEvent),
  };
}

function freshnessLabel(scenarios: readonly CommercialPricingScenarioSummary[]): string {
  const timestamps = scenarios
    .map((item) => Date.parse(item.updatedAtUtc))
    .filter((value) => Number.isFinite(value));
  if (timestamps.length === 0) return "—";
  return formatDateTime(new Date(Math.max(...timestamps)).toISOString());
}

export async function readCommercialPricingScenarioWorkspace(
  requestedScenarioId?: string | null,
  filters: CommercialPricingScenarioApiFilters = { page: 1, pageSize: 25 },
): Promise<CommercialPricingScenarioWorkspaceResult> {
  const authorization = await denyUnlessGranted("commercial-intelligence.read");
  if (authorization.status === "failure") {
    return { status: "failure", message: authorization.message, code: "permission-denied" };
  }

  const contextResult = resolveCommercialIntelligenceRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message, code: contextResult.code };
  }

  const [workspaceResult, scenariosResult] = await Promise.all([
    getCommercialPricingScenarioWorkspace(contextResult.context),
    listCommercialPricingScenarios(contextResult.context, filters),
  ]);

  if (workspaceResult.status === "failure") {
    return { status: "failure", message: workspaceResult.message, code: workspaceResult.code };
  }
  if (scenariosResult.status === "failure") {
    return { status: "failure", message: scenariosResult.message, code: scenariosResult.code };
  }

  const scenarios = scenariosResult.value.items.map(mapScenarioSummary);
  const selected = requestedScenarioId
    ? scenarios.find((item) => item.id === requestedScenarioId) ?? null
    : null;

  return {
    status: "success",
    workspace: {
      facilityLabel: "Yetkili tesis",
      ruleSetVersion: workspaceResult.value.ruleSetVersion,
      provenanceDisclaimer: localizeCommercialProfitabilityProvenance(workspaceResult.value.provenanceDisclaimer),
      automationDisclaimer: workspaceResult.value.automationDisclaimer,
      reportingPeriodLabel: freshnessLabel(scenarios),
      freshnessLabel: freshnessLabel(scenarios),
      scenarios,
      selectedScenarioId: selected?.id ?? requestedScenarioId ?? null,
      kpiCards: buildScenarioKpiCards(workspaceResult.value.kpis),
      tableRows: scenarios.map(mapScenarioTableRow),
      page: filters.page,
      pageSize: filters.pageSize,
      returnedCount: scenarios.length,
      canRead: workspaceResult.value.capabilities.canRead,
      canManage: workspaceResult.value.capabilities.canManage,
      canReview: workspaceResult.value.capabilities.canReview,
      canCreateTask: workspaceResult.value.capabilities.canCreateTask,
    },
  };
}

export async function readCommercialPricingScenarioDetail(
  scenarioId: string,
): Promise<CommercialPricingScenarioDetailResult> {
  const authorization = await denyUnlessGranted("commercial-intelligence.read");
  if (authorization.status === "failure") {
    return { status: "failure", message: authorization.message, code: "permission-denied" };
  }

  const contextResult = resolveCommercialIntelligenceRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message, code: contextResult.code };
  }

  const detailResult = await getCommercialPricingScenario(contextResult.context, scenarioId);
  if (detailResult.status === "failure") {
    return { status: "failure", message: detailResult.message, code: detailResult.code };
  }

  return {
    status: "success",
    detail: mapScenarioDetail(detailResult.value),
  };
}

export { mapScenarioDetail };
