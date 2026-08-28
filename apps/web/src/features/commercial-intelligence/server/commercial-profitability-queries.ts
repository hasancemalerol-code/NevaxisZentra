import "./commercial-intelligence-server-boundary";

import {
  buildProfitabilityKpiCards,
  buildProfitabilityPositionPreviewRows,
  buildProfitabilityTemporalChartData,
  filterProfitabilityCostMixComparisons,
  filterProfitabilityPositionComparisons,
  filterProfitabilityTemporalComparisons,
  selectLatestProfitabilityComparison,
} from "../components/commercial-profitability-presentation";
import { localizeCommercialIntelligenceMaterialLabel } from "../components/commercial-intelligence-presentation";
import {
  formatCompactPeriod,
  formatCurrency,
  formatDateTime,
  formatRatioAsPercent,
} from "../domain/commercial-intelligence-formatters";
import {
  getCommercialIntelligenceEvidenceStatusLabel,
  getCommercialIntelligenceProfileStatusLabel,
  getCommercialIntelligenceSeverityLabel,
} from "../domain/commercial-intelligence-labels";
import {
  getCommercialProfitabilityComparisonTypeLabel,
  getCommercialProfitabilityComponentKindLabel,
  getCommercialProfitabilityDriverReasonLabel,
  getCommercialProfitabilityOutcomeLabel,
  getCommercialProfitabilityPositionStateLabel,
  localizeCommercialProfitabilityProvenance,
} from "../domain/commercial-profitability-labels";
import type {
  CommercialProfitabilityComparisonDetail,
  CommercialProfitabilityComparisonDriver,
  CommercialProfitabilityComparisonResultRow,
  CommercialProfitabilityComparisonSummary,
  CommercialProfitabilityProfileSummary,
  CommercialProfitabilityWorkspacePayload,
} from "../types/commercial-profitability-types";
import {
  getCommercialProfitabilityComparison,
  getCommercialProfitabilityWorkspace,
  listCommercialProfitabilityComparisons,
  listCommercialProfitabilityProfiles,
  type RawCommercialProfitabilityComparisonDetail,
  type RawCommercialProfitabilityComparisonResult,
  type RawCommercialProfitabilityComparisonSummary,
  type RawCommercialProfitabilityProfile,
} from "./commercial-profitability-api-client";
import { resolveCommercialIntelligenceRequestContext } from "./commercial-intelligence-request-context";
import {
  fetchCurrentEffectiveAccess,
  hasGrantedPermission,
} from "../../admin-access/server/admin-access-effective-access";

import type { CommercialProfitabilityComparisonApiFilters } from "../application/commercial-profitability-filter";

export type { CommercialProfitabilityComparisonApiFilters };

type ReadFailure = Readonly<{ status: "failure"; message: string; code?: string }>;

export type CommercialProfitabilityWorkspaceResult =
  | Readonly<{ status: "success"; workspace: CommercialProfitabilityWorkspacePayload; canManage: boolean }>
  | ReadFailure;

export type CommercialProfitabilityComparisonDetailResult =
  | Readonly<{ status: "success"; detail: CommercialProfitabilityComparisonDetail }>
  | ReadFailure;

export function mapComparisonSummary(
  item: RawCommercialProfitabilityComparisonSummary,
): CommercialProfitabilityComparisonSummary {
  return {
    id: item.id,
    comparisonReference: item.comparisonReference,
    profileId: item.profileId,
    profileCode: item.profileCode,
    comparisonType: item.comparisonType,
    comparisonTypeLabel: getCommercialProfitabilityComparisonTypeLabel(item.comparisonType),
    materialId: item.materialId,
    materialLabel: item.materialId ? localizeCommercialIntelligenceMaterialLabel(item.materialId) : "—",
    customerReference: item.customerReference,
    customerLabel: item.customerReference ?? "—",
    comparisonOutcome: item.comparisonOutcome,
    outcomeLabel: getCommercialProfitabilityOutcomeLabel(item.comparisonOutcome),
    evidenceStatus: item.evidenceStatus,
    evidenceLabel: getCommercialIntelligenceEvidenceStatusLabel(item.evidenceStatus),
    aggregationMethod: item.aggregationMethod,
    candidateAssessmentCount: item.candidateAssessmentCount,
    comparableAssessmentCount: item.comparableAssessmentCount,
    excludedAssessmentCount: item.excludedAssessmentCount,
    referenceMedianContributionMarginRatio: item.referenceMedianContributionMarginRatio,
    currentMedianContributionMarginRatio: item.currentMedianContributionMarginRatio,
    referenceMarginLabel: formatRatioAsPercent(item.referenceMedianContributionMarginRatio),
    currentMarginLabel: formatRatioAsPercent(item.currentMedianContributionMarginRatio),
    contributionMarginDelta: item.contributionMarginDelta,
    marginDeltaLabel: formatRatioAsPercent(item.contributionMarginDelta),
    referenceWindowStartUtc: item.referenceWindowStartUtc,
    referenceWindowEndUtc: item.referenceWindowEndUtc,
    referenceWindowLabel: formatCompactPeriod(item.referenceWindowStartUtc, item.referenceWindowEndUtc),
    currentWindowStartUtc: item.currentWindowStartUtc,
    currentWindowEndUtc: item.currentWindowEndUtc,
    currentWindowLabel: formatCompactPeriod(item.currentWindowStartUtc, item.currentWindowEndUtc),
    leadingDriverCode: item.leadingDriverCode,
    leadingDriverLabel: getCommercialProfitabilityDriverReasonLabel(item.leadingDriverCode),
    ruleSetVersion: item.ruleSetVersion,
    evaluatedAtUtc: item.evaluatedAtUtc,
    evaluatedLabel: formatDateTime(item.evaluatedAtUtc),
    isPilotData: item.isPilotData,
    realProfitabilityMappingComplete: item.realProfitabilityMappingComplete,
  };
}

export function mapProfile(profile: RawCommercialProfitabilityProfile): CommercialProfitabilityProfileSummary {
  return {
    id: profile.id,
    profileCode: profile.profileCode,
    displayName: profile.name,
    status: profile.status,
    statusLabel: getCommercialIntelligenceProfileStatusLabel(profile.status),
    minimumContributionMarginRatio: profile.minimumContributionMarginRatio,
    minimumContributionMarginLabel: formatRatioAsPercent(profile.minimumContributionMarginRatio),
    minimumComparableAssessmentCountPerWindow: 0,
    maximumAdverseContributionMarginDelta: 0,
    minimumFavorableContributionMarginDelta: 0,
    maximumCostShareIncrease: 0,
    ruleSetVersion: profile.ruleSetVersion,
    version: profile.version,
    isPilotData: profile.isPilotData,
  };
}

function mapComparisonResultRow(
  result: RawCommercialProfitabilityComparisonResult,
  currencyCode: string,
): CommercialProfitabilityComparisonResultRow {
  return {
    id: result.id,
    resultDimension: result.resultDimension,
    canonicalResultReference: result.canonicalResultReference,
    customerReference: result.customerReference,
    customerLabel: result.customerReference ?? "—",
    materialId: result.materialId,
    materialLabel: result.materialId ? localizeCommercialIntelligenceMaterialLabel(result.materialId) : "—",
    componentKind: result.componentKind,
    componentKindLabel: getCommercialProfitabilityComponentKindLabel(result.componentKind),
    comparableAssessmentCount: result.comparableAssessmentCount,
    excludedAssessmentCount: result.excludedAssessmentCount,
    referenceMarginLabel: formatRatioAsPercent(result.referenceMedianContributionMarginRatio),
    currentMarginLabel: formatRatioAsPercent(result.currentMedianContributionMarginRatio),
    marginDeltaLabel: formatRatioAsPercent(result.contributionMarginDelta),
    referenceUnitContributionLabel: formatCurrency(result.referenceMedianUnitContribution, currencyCode),
    currentUnitContributionLabel: formatCurrency(result.currentMedianUnitContribution, currencyCode),
    referenceUnitCostLabel: formatCurrency(result.referenceMedianUnitCost, currencyCode),
    currentUnitCostLabel: formatCurrency(result.currentMedianUnitCost, currencyCode),
    unitCostChangeRatioLabel: formatRatioAsPercent(result.signedUnitCostChangeRatio),
    referenceCostShareLabel: formatRatioAsPercent(result.referenceMedianCostShare),
    currentCostShareLabel: formatRatioAsPercent(result.currentMedianCostShare),
    costShareDeltaLabel: formatRatioAsPercent(result.costShareDelta),
    positionState: result.positionState,
    positionStateLabel: getCommercialProfitabilityPositionStateLabel(result.positionState),
    contributionRank: result.contributionRank,
    isTied: result.isTied,
    evidenceStatus: result.evidenceStatus,
    evidenceLabel: getCommercialIntelligenceEvidenceStatusLabel(result.evidenceStatus),
  };
}

export function mapComparisonDetail(
  item: RawCommercialProfitabilityComparisonDetail,
  currencyCode: string,
): CommercialProfitabilityComparisonDetail {
  const summary = mapComparisonSummary(item);
  const drivers: readonly CommercialProfitabilityComparisonDriver[] = item.drivers
    .slice()
    .sort((left, right) => left.sortOrder - right.sortOrder)
    .map((driver) => ({
      id: driver.id,
      comparisonResultId: driver.comparisonResultId,
      reasonCode: driver.reasonCode,
      reasonLabel: getCommercialProfitabilityDriverReasonLabel(driver.reasonCode),
      severity: driver.severity,
      severityLabel: getCommercialIntelligenceSeverityLabel(driver.severity),
      actualValue: driver.actualValue,
      referenceValue: driver.referenceValue,
      thresholdValue: driver.thresholdValue,
      unit: driver.unit,
      normalizedVariance: driver.normalizedVariance,
      normalizedVarianceLabel: formatRatioAsPercent(driver.normalizedVariance),
      affectedAssessmentCount: driver.affectedAssessmentCount,
      supportingText: driver.supportingText,
      sortOrder: driver.sortOrder,
      isLeadingDriver: driver.isLeadingDriver,
    }));

  const referenceUnit = item.referenceMedianUnitContribution;
  const currentUnit = item.currentMedianUnitContribution;
  const unitDelta = referenceUnit !== null && currentUnit !== null
    ? currentUnit - referenceUnit
    : null;

  return {
    summary,
    profileVersion: item.profileVersion,
    completeCoverageCount: item.completeCoverageCount,
    partialCoverageCount: item.partialCoverageCount,
    insufficientDataCount: item.insufficientDataCount,
    sourceConflictCount: item.sourceConflictCount,
    negativeContributionCount: item.negativeContributionCount,
    belowThresholdCount: item.belowThresholdCount,
    referenceMedianUnitContributionLabel: formatCurrency(item.referenceMedianUnitContribution, currencyCode),
    currentMedianUnitContributionLabel: formatCurrency(item.currentMedianUnitContribution, currencyCode),
    unitContributionDeltaLabel: formatCurrency(unitDelta, currencyCode),
    sourceProvenance: localizeCommercialProfitabilityProvenance(item.sourceProvenance),
    results: item.results
      .slice()
      .sort((left, right) => left.sortOrder - right.sortOrder)
      .map((result) => mapComparisonResultRow(result, currencyCode)),
    drivers,
  };
}

export function reportingPeriodLabel(comparisons: readonly CommercialProfitabilityComparisonSummary[]): string {
  const timestamps = comparisons.flatMap((item) => [
    Date.parse(item.currentWindowStartUtc),
    Date.parse(item.currentWindowEndUtc),
    item.referenceWindowStartUtc ? Date.parse(item.referenceWindowStartUtc) : NaN,
    item.referenceWindowEndUtc ? Date.parse(item.referenceWindowEndUtc) : NaN,
  ]).filter((value) => Number.isFinite(value));
  if (timestamps.length === 0) return "—";
  return formatCompactPeriod(
    new Date(Math.min(...timestamps)).toISOString(),
    new Date(Math.max(...timestamps)).toISOString(),
  );
}

export function freshnessLabel(comparisons: readonly CommercialProfitabilityComparisonSummary[]): string {
  const timestamps = comparisons
    .map((item) => Date.parse(item.evaluatedAtUtc))
    .filter((value) => Number.isFinite(value));
  if (timestamps.length === 0) return "—";
  return formatDateTime(new Date(Math.max(...timestamps)).toISOString());
}

function resolveCurrencyCode(
  profiles: readonly CommercialProfitabilityProfileSummary[],
  profileId: string | null | undefined,
  rawProfiles: readonly RawCommercialProfitabilityProfile[],
): string {
  const raw = rawProfiles.find((item) => item.id === profileId) ?? rawProfiles[0];
  return raw?.requiredCurrencyCode?.trim() || "TRY";
}

function resolveSelectedComparison(
  comparisons: readonly CommercialProfitabilityComparisonSummary[],
  filter: (items: readonly CommercialProfitabilityComparisonSummary[]) => readonly CommercialProfitabilityComparisonSummary[],
  requestedComparisonId?: string | null,
): CommercialProfitabilityComparisonSummary | null {
  const scoped = filter(comparisons);
  const fallback = scoped[0] ?? selectLatestProfitabilityComparison(scoped);
  if (!requestedComparisonId) return fallback;
  return scoped.find((item) => item.id === requestedComparisonId) ?? fallback;
}

export async function readCommercialProfitabilityWorkspace(
  requestedComparisonId?: string | null,
  filters: CommercialProfitabilityComparisonApiFilters = {},
): Promise<CommercialProfitabilityWorkspaceResult> {
  const contextResult = resolveCommercialIntelligenceRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message, code: contextResult.code };
  }

  const access = await fetchCurrentEffectiveAccess();
  const canManage = hasGrantedPermission(access, "commercial-intelligence.manage");

  const [workspaceResult, profilesResult, comparisonsResult] = await Promise.all([
    getCommercialProfitabilityWorkspace(contextResult.context),
    listCommercialProfitabilityProfiles(contextResult.context),
    listCommercialProfitabilityComparisons(contextResult.context, {
      ...filters,
      page: 1,
      pageSize: 200,
    }),
  ]);

  if (workspaceResult.status === "failure") {
    return { status: "failure", message: workspaceResult.message, code: workspaceResult.code };
  }
  if (profilesResult.status === "failure") {
    return { status: "failure", message: profilesResult.message, code: profilesResult.code };
  }
  if (comparisonsResult.status === "failure") {
    return { status: "failure", message: comparisonsResult.message, code: comparisonsResult.code };
  }

  const comparisons = comparisonsResult.value.items.map(mapComparisonSummary);
  const profiles = profilesResult.value.items.map(mapProfile);
  const hasPilot = comparisons.some((item) => item.isPilotData) || profiles.some((item) => item.isPilotData);

  const selectedTemporal = resolveSelectedComparison(
    comparisons,
    filterProfitabilityTemporalComparisons,
    requestedComparisonId,
  );
  const selectedCostMix = resolveSelectedComparison(
    comparisons,
    filterProfitabilityCostMixComparisons,
    requestedComparisonId,
  );
  const selectedPosition = resolveSelectedComparison(
    comparisons,
    filterProfitabilityPositionComparisons,
    requestedComparisonId,
  );

  return {
    status: "success",
    canManage,
    workspace: {
      facilityLabel: hasPilot ? "Geliştirme pilot tesisi" : "Yetkili tesis",
      evidenceLabel: getCommercialIntelligenceEvidenceStatusLabel("simulated"),
      hasDevelopmentPilotData: hasPilot,
      ruleSetVersion: workspaceResult.value.ruleSetVersion,
      comparisonRuleSetVersion: comparisonsResult.value.ruleSetVersion,
      provenanceDisclaimer: localizeCommercialProfitabilityProvenance(workspaceResult.value.provenanceDisclaimer),
      realProfitabilityMappingComplete: workspaceResult.value.realProfitabilityMappingComplete,
      reportingPeriodLabel: reportingPeriodLabel(comparisons),
      freshnessLabel: freshnessLabel(comparisons),
      profiles,
      comparisons,
      selectedTemporalComparisonId: selectedTemporal?.id ?? null,
      selectedCostMixComparisonId: selectedCostMix?.id ?? null,
      selectedPositionComparisonId: selectedPosition?.id ?? null,
      kpiCards: buildProfitabilityKpiCards(comparisons),
      temporalChart: buildProfitabilityTemporalChartData(comparisons),
      positionPreviewRows: buildProfitabilityPositionPreviewRows(comparisons),
    },
  };
}

export async function readCommercialProfitabilityComparisonDetail(
  comparisonId: string,
): Promise<CommercialProfitabilityComparisonDetailResult> {
  const contextResult = resolveCommercialIntelligenceRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message, code: contextResult.code };
  }

  const [detailResult, profilesResult] = await Promise.all([
    getCommercialProfitabilityComparison(contextResult.context, comparisonId),
    listCommercialProfitabilityProfiles(contextResult.context),
  ]);

  if (detailResult.status === "failure") {
    return { status: "failure", message: detailResult.message, code: detailResult.code };
  }
  if (profilesResult.status === "failure") {
    return { status: "failure", message: profilesResult.message, code: profilesResult.code };
  }

  const currencyCode = resolveCurrencyCode(
    profilesResult.value.items.map(mapProfile),
    detailResult.value.profileId,
    profilesResult.value.items,
  );

  return {
    status: "success",
    detail: mapComparisonDetail(detailResult.value, currencyCode),
  };
}
