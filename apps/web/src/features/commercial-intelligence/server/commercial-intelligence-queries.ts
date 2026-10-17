import { getCommercialWorkspaceEvidenceLabel, resolveCommercialProvenance } from "../domain/commercial-intelligence-labels";
import "./commercial-intelligence-server-boundary";

import {
  buildCommercialIntelligenceEvidenceSummary,
  buildCommercialIntelligenceKpiCards,
  buildCommercialIntelligencePriorityDeviations,
  buildCommercialIntelligenceSupplierPositionRows,
  buildCommercialIntelligenceTemporalChartData,
  buildCommercialIntelligenceComparisonPrimaryLabel,
  buildCommercialIntelligenceComparisonSecondaryLabel,
  localizeCommercialIntelligenceMaterialLabel,
  localizeCommercialIntelligenceSupplierLabel,
  mapCommercialIntelligenceSupplierResult,
  reconcileCommercialIntelligenceSupplierSpread,
  selectLatestCompatibleSupplierComparison,
} from "../components/commercial-intelligence-presentation";
import {
  formatCompactPeriod,
  formatDateTime,
  formatRatioAsPercent,
} from "../domain/commercial-intelligence-formatters";
import {
  getCommercialIntelligenceComparisonDimensionLabel,
  getCommercialIntelligenceComparisonTypeLabel,
  getCommercialIntelligenceDriverReasonLabel,
  getCommercialIntelligenceEvidenceStatusLabel,
  getCommercialIntelligenceOutcomeLabel,
  getCommercialIntelligencePriceBasisLabel,
  getCommercialIntelligenceProfileStatusLabel,
  getCommercialIntelligenceSeverityLabel,
  getCommercialIntelligenceUnavailableDimensionLabel,
} from "../domain/commercial-intelligence-labels";
import type {
  CommercialIntelligenceAssessmentDetail,
  CommercialIntelligenceAssessmentSummary,
  CommercialIntelligenceComparisonDetail,
  CommercialIntelligenceComparisonSummary,
  CommercialIntelligenceProfileSummary,
  CommercialIntelligenceSupplierResult,
  CommercialIntelligenceWorkspacePayload,
} from "../types/commercial-intelligence-types";
import {
  getCommercialIntelligenceAssessment,
  getCommercialIntelligenceComparison,
  getCommercialIntelligenceWorkspace,
  listCommercialIntelligenceAssessments,
  listCommercialIntelligenceComparisons,
  listCommercialIntelligenceProfiles,
  type RawCommercialIntelligenceAssessmentDetail,
  type RawCommercialIntelligenceAssessmentSummary,
  type RawCommercialIntelligenceComparisonDetail,
  type RawCommercialIntelligenceComparisonSummary,
  type RawCommercialIntelligenceProfile,
} from "./commercial-intelligence-api-client";
import { resolveCommercialIntelligenceRequestContext } from "./commercial-intelligence-request-context";
import {
  fetchEffectiveAccessCapabilities,
  hasGrantedPermission,
} from "../../admin-access/server/admin-access-effective-access";

type ReadFailure = Readonly<{ status: "failure"; message: string; code?: string }>;

export type CommercialIntelligenceWorkspaceResult =
  | Readonly<{ status: "success"; workspace: CommercialIntelligenceWorkspacePayload; canManage: boolean }>
  | ReadFailure;

export type CommercialIntelligenceAssessmentDetailResult =
  | Readonly<{ status: "success"; detail: CommercialIntelligenceAssessmentDetail }>
  | ReadFailure;

export type CommercialIntelligenceComparisonDetailResult =
  | Readonly<{ status: "success"; detail: CommercialIntelligenceComparisonDetail }>
  | ReadFailure;

function mapAssessmentSummary(item: RawCommercialIntelligenceAssessmentSummary): CommercialIntelligenceAssessmentSummary {
  return {
    id: item.id,
    assessmentReference: item.assessmentReference,
    profileId: item.profileId,
    profileCode: item.profileCode,
    materialId: item.materialId,
    materialLabel: localizeCommercialIntelligenceMaterialLabel(item.materialId),
    supplierReference: item.supplierReference,
    supplierLabel: localizeCommercialIntelligenceSupplierLabel(item.supplierReference),
    comparisonDimension: item.comparisonDimension,
    comparisonDimensionLabel: getCommercialIntelligenceComparisonDimensionLabel(item.comparisonDimension),
    assessmentOutcome: item.assessmentOutcome,
    outcomeLabel: getCommercialIntelligenceOutcomeLabel(item.assessmentOutcome),
    currentUnitPrice: item.currentUnitPrice,
    referenceMedianUnitPrice: item.referenceMedianUnitPrice,
    varianceRatio: item.varianceRatio,
    varianceLabel: formatRatioAsPercent(item.varianceRatio),
    comparableObservationCount: item.comparableObservationCount,
    excludedObservationCount: item.excludedObservationCount,
    currencyCode: item.currencyCode,
    normalizedUnit: item.normalizedUnit,
    windowStartUtc: item.windowStartUtc,
    windowEndUtc: item.windowEndUtc,
    windowLabel: formatCompactPeriod(item.windowStartUtc, item.windowEndUtc),
    leadingDriverCode: item.leadingDriverCode,
    leadingDriverLabel: getCommercialIntelligenceDriverReasonLabel(item.leadingDriverCode),
    evidenceStatus: "simulated",
    evidenceLabel: getCommercialWorkspaceEvidenceLabel(item.isPilotData, true),
    ruleSetVersion: item.ruleSetVersion,
    evaluatedAtUtc: item.evaluatedAtUtc,
    evaluatedLabel: formatDateTime(item.evaluatedAtUtc),
    isPilotData: item.isPilotData,
  };
}

function mapComparisonSummary(item: RawCommercialIntelligenceComparisonSummary): CommercialIntelligenceComparisonSummary {
  return {
    id: item.id,
    comparisonReference: item.comparisonReference,
    profileId: item.profileId,
    profileCode: item.profileCode,
    comparisonType: item.comparisonType,
    comparisonTypeLabel: getCommercialIntelligenceComparisonTypeLabel(item.comparisonType),
    materialId: item.materialId,
    materialLabel: localizeCommercialIntelligenceMaterialLabel(item.materialId),
    comparisonOutcome: item.comparisonOutcome,
    outcomeLabel: getCommercialIntelligenceOutcomeLabel(item.comparisonOutcome),
    currentMedianUnitPrice: item.currentMedianUnitPrice,
    referenceMedianUnitPrice: item.referenceMedianUnitPrice,
    signedVarianceRatio: item.signedVarianceRatio,
    signedVarianceLabel: formatRatioAsPercent(item.signedVarianceRatio),
    supplierSpreadRatio: item.supplierSpreadRatio,
    supplierSpreadLabel: formatRatioAsPercent(item.supplierSpreadRatio),
    comparableObservationCount: item.comparableObservationCount,
    excludedObservationCount: item.excludedObservationCount,
    currencyCode: item.currencyCode,
    normalizedUnit: item.normalizedUnit,
    currentWindowStartUtc: item.currentWindowStartUtc,
    currentWindowEndUtc: item.currentWindowEndUtc,
    currentWindowLabel: formatCompactPeriod(item.currentWindowStartUtc, item.currentWindowEndUtc),
    referenceWindowStartUtc: item.referenceWindowStartUtc,
    referenceWindowEndUtc: item.referenceWindowEndUtc,
    referenceWindowLabel: formatCompactPeriod(item.referenceWindowStartUtc, item.referenceWindowEndUtc),
    leadingDriverCode: item.leadingDriverCode,
    leadingDriverLabel: getCommercialIntelligenceDriverReasonLabel(item.leadingDriverCode),
    evidenceStatus: "simulated",
    evidenceLabel: getCommercialWorkspaceEvidenceLabel(item.isPilotData, true),
    ruleSetVersion: item.ruleSetVersion,
    evaluatedAtUtc: item.evaluatedAtUtc,
    evaluatedLabel: formatDateTime(item.evaluatedAtUtc),
    isPilotData: item.isPilotData,
  };
}

function mapProfile(profile: RawCommercialIntelligenceProfile): CommercialIntelligenceProfileSummary {
  const evidenceTokens = profile.allowedEvidenceStatuses
    .split(",")
    .map((token) => token.trim())
    .filter(Boolean);

  return {
    id: profile.id,
    profileCode: profile.profileCode,
    displayName: profile.displayName,
    status: profile.status,
    statusLabel: getCommercialIntelligenceProfileStatusLabel(profile.status),
    comparisonDimension: profile.comparisonDimension,
    comparisonDimensionLabel: getCommercialIntelligenceComparisonDimensionLabel(profile.comparisonDimension),
    referenceWindowDurationDays: profile.referenceWindowDurationDays,
    minimumComparableObservations: profile.minimumComparableObservations,
    maximumPositiveVarianceRatio: profile.maximumPositiveVarianceRatio,
    maximumPositiveVarianceLabel: formatRatioAsPercent(profile.maximumPositiveVarianceRatio),
    supplierSpreadThreshold: profile.supplierSpreadThreshold,
    supplierSpreadThresholdLabel: profile.supplierSpreadThreshold === null
      ? "—"
      : formatRatioAsPercent(profile.supplierSpreadThreshold),
    allowedEvidenceStatuses: profile.allowedEvidenceStatuses,
    allowedEvidenceLabels: evidenceTokens.map(getCommercialIntelligenceEvidenceStatusLabel),
    requiredTaxInclusion: profile.requiredTaxInclusion,
    requiredTaxLabel: getCommercialIntelligencePriceBasisLabel(profile.requiredTaxInclusion),
    requiredDiscountInclusion: profile.requiredDiscountInclusion,
    requiredDiscountLabel: getCommercialIntelligencePriceBasisLabel(profile.requiredDiscountInclusion),
    requiredFreightInclusion: profile.requiredFreightInclusion,
    requiredFreightLabel: getCommercialIntelligencePriceBasisLabel(profile.requiredFreightInclusion),
    referenceCenter: profile.referenceCenter,
    ruleSetVersion: profile.ruleSetVersion,
    sourceProvenance: profile.sourceProvenance,
    version: profile.version,
    isExecutable: profile.status === "approved",
    isPilotData: profile.isPilotData,
    compatibilitySummary: `Vergi ${getCommercialIntelligencePriceBasisLabel(profile.requiredTaxInclusion)}, iskonto ${getCommercialIntelligencePriceBasisLabel(profile.requiredDiscountInclusion)}, navlun ${getCommercialIntelligencePriceBasisLabel(profile.requiredFreightInclusion)}`,
  };
}

function resolveSupplierSpreadThreshold(
  profiles: readonly CommercialIntelligenceProfileSummary[],
): number | null {
  const approved = profiles.find((profile) => profile.isExecutable);
  return approved?.supplierSpreadThreshold ?? null;
}

function reportingPeriodLabel(
  assessments: readonly CommercialIntelligenceAssessmentSummary[],
  comparisons: readonly CommercialIntelligenceComparisonSummary[],
): string {
  const timestamps = [
    ...assessments.flatMap((item) => [Date.parse(item.windowStartUtc), Date.parse(item.windowEndUtc)]),
    ...comparisons.flatMap((item) => [
      Date.parse(item.currentWindowStartUtc),
      Date.parse(item.currentWindowEndUtc),
      item.referenceWindowStartUtc ? Date.parse(item.referenceWindowStartUtc) : NaN,
      item.referenceWindowEndUtc ? Date.parse(item.referenceWindowEndUtc) : NaN,
    ]),
  ].filter((value) => Number.isFinite(value));

  if (timestamps.length === 0) return "—";
  const min = Math.min(...timestamps);
  const max = Math.max(...timestamps);
  return formatCompactPeriod(new Date(min).toISOString(), new Date(max).toISOString());
}

function freshnessLabel(
  assessments: readonly CommercialIntelligenceAssessmentSummary[],
  comparisons: readonly CommercialIntelligenceComparisonSummary[],
): string {
  const timestamps = [
    ...assessments.map((item) => Date.parse(item.evaluatedAtUtc)),
    ...comparisons.map((item) => Date.parse(item.evaluatedAtUtc)),
  ].filter((value) => Number.isFinite(value));
  if (timestamps.length === 0) return "—";
  return formatDateTime(new Date(Math.max(...timestamps)).toISOString());
}

function mapAssessmentDetail(item: RawCommercialIntelligenceAssessmentDetail): CommercialIntelligenceAssessmentDetail {
  const summary = {
    ...mapAssessmentSummary(item),
    evidenceStatus: item.evidenceStatus,
    evidenceLabel: getCommercialIntelligenceEvidenceStatusLabel(item.evidenceStatus),
  };

  const drivers = item.drivers
    .slice()
    .sort((left, right) => left.sortOrder - right.sortOrder)
    .map((driver) => ({
      id: driver.id,
      reasonCode: driver.reasonCode,
      reasonLabel: getCommercialIntelligenceDriverReasonLabel(driver.reasonCode),
      severity: driver.severity,
      severityLabel: getCommercialIntelligenceSeverityLabel(driver.severity),
      actualValue: driver.actualValue,
      thresholdValue: driver.thresholdValue,
      unit: driver.unit,
      normalizedVariance: driver.normalizedVariance,
      normalizedVarianceLabel: formatRatioAsPercent(driver.normalizedVariance),
      affectedObservationCount: driver.affectedObservationCount,
      supportingText: driver.supportingText,
      sortOrder: driver.sortOrder,
      isLeadingDriver: driver.isLeadingDriver,
    }));

  const exclusionReasons = drivers
    .filter((driver) => driver.affectedObservationCount > 0)
    .map((driver) => driver.reasonLabel);

  return {
    summary,
    absoluteVariance: item.absoluteVariance,
    priceBasisTaxInclusion: item.priceBasisTaxInclusion,
    priceBasisTaxLabel: getCommercialIntelligencePriceBasisLabel(item.priceBasisTaxInclusion),
    priceBasisDiscountInclusion: item.priceBasisDiscountInclusion,
    priceBasisDiscountLabel: getCommercialIntelligencePriceBasisLabel(item.priceBasisDiscountInclusion),
    priceBasisFreightInclusion: item.priceBasisFreightInclusion,
    priceBasisFreightLabel: getCommercialIntelligencePriceBasisLabel(item.priceBasisFreightInclusion),
    sourceProvenance: item.sourceProvenance,
    drivers,
    exclusionReasons,
  };
}

function mapComparisonDetail(item: RawCommercialIntelligenceComparisonDetail): CommercialIntelligenceComparisonDetail {
  const summary = {
    ...mapComparisonSummary(item),
    evidenceStatus: item.evidenceStatus,
    evidenceLabel: getCommercialIntelligenceEvidenceStatusLabel(item.evidenceStatus),
  };

  const supplierResults = buildCommercialIntelligenceSupplierPositionRows(
    item.supplierResults.map((result) => mapCommercialIntelligenceSupplierResult(result, item.currencyCode)),
  );

  const drivers = item.drivers
    .slice()
    .sort((left, right) => left.sortOrder - right.sortOrder)
    .map((driver) => ({
      id: driver.id,
      comparisonResultId: driver.comparisonResultId,
      reasonCode: driver.reasonCode,
      reasonLabel: getCommercialIntelligenceDriverReasonLabel(driver.reasonCode),
      severity: driver.severity,
      severityLabel: getCommercialIntelligenceSeverityLabel(driver.severity),
      actualValue: driver.actualValue,
      referenceValue: driver.referenceValue,
      thresholdValue: driver.thresholdValue,
      unit: driver.unit,
      normalizedVariance: driver.normalizedVariance,
      normalizedVarianceLabel: formatRatioAsPercent(driver.normalizedVariance),
      affectedObservationCount: driver.affectedObservationCount,
      supportingText: driver.supportingText,
      sortOrder: driver.sortOrder,
      isLeadingDriver: driver.isLeadingDriver,
    }));

  return {
    summary,
    profileVersion: item.profileVersion,
    priceBasisTaxInclusion: item.priceBasisTaxInclusion,
    priceBasisTaxLabel: getCommercialIntelligencePriceBasisLabel(item.priceBasisTaxInclusion),
    priceBasisDiscountInclusion: item.priceBasisDiscountInclusion,
    priceBasisDiscountLabel: getCommercialIntelligencePriceBasisLabel(item.priceBasisDiscountInclusion),
    priceBasisFreightInclusion: item.priceBasisFreightInclusion,
    priceBasisFreightLabel: getCommercialIntelligencePriceBasisLabel(item.priceBasisFreightInclusion),
    sourceProvenance: item.sourceProvenance,
    supplierResults,
    drivers,
    unavailableDimensions: item.unavailableDimensions ?? [],
    unavailableDimensionLabels: (item.unavailableDimensions ?? []).map(
      getCommercialIntelligenceUnavailableDimensionLabel,
    ),
  };
}

export async function readCommercialIntelligenceWorkspace(): Promise<CommercialIntelligenceWorkspaceResult> {
  const contextResult = await resolveCommercialIntelligenceRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message, code: contextResult.code };
  }

  const access = await fetchEffectiveAccessCapabilities();
  const canManage = hasGrantedPermission(access, "commercial-intelligence.manage");

  const [workspaceResult, profilesResult, assessmentsResult, comparisonsResult] = await Promise.all([
    getCommercialIntelligenceWorkspace(contextResult.context),
    listCommercialIntelligenceProfiles(contextResult.context),
    listCommercialIntelligenceAssessments(contextResult.context, { page: 1, pageSize: 200 }),
    listCommercialIntelligenceComparisons(contextResult.context, { page: 1, pageSize: 200 }),
  ]);

  if (workspaceResult.status === "failure") {
    return { status: "failure", message: workspaceResult.message, code: workspaceResult.code };
  }
  if (profilesResult.status === "failure") {
    return { status: "failure", message: profilesResult.message, code: profilesResult.code };
  }
  if (assessmentsResult.status === "failure") {
    return { status: "failure", message: assessmentsResult.message, code: assessmentsResult.code };
  }
  if (comparisonsResult.status === "failure") {
    return { status: "failure", message: comparisonsResult.message, code: comparisonsResult.code };
  }

  const assessments = assessmentsResult.value.items.map(mapAssessmentSummary);
  const comparisons = comparisonsResult.value.items.map(mapComparisonSummary);
  const profiles = profilesResult.value.items.map(mapProfile);
  const supplierSpreadThreshold = resolveSupplierSpreadThreshold(profiles);

  const latestSupplierComparison = selectLatestCompatibleSupplierComparison(comparisons);

  let supplierRows: CommercialIntelligenceSupplierResult[] = [];
  if (latestSupplierComparison) {
    const detailResult = await getCommercialIntelligenceComparison(
      contextResult.context,
      latestSupplierComparison.id,
    );
    if (detailResult.status === "success") {
      supplierRows = [...buildCommercialIntelligenceSupplierPositionRows(
        detailResult.value.supplierResults.map((result) =>
          mapCommercialIntelligenceSupplierResult(result, detailResult.value.currencyCode),
        ),
      )];
    }
  }

  const temporalChart = buildCommercialIntelligenceTemporalChartData(comparisons);
  const evidenceSummary = buildCommercialIntelligenceEvidenceSummary(
    assessments,
    comparisons,
    supplierSpreadThreshold,
  );
  const priorityDeviations = buildCommercialIntelligencePriorityDeviations(assessments, comparisons);

  const hasPilot = assessments.some((item) => item.isPilotData)
    || comparisons.some((item) => item.isPilotData)
    || profiles.some((item) => item.isPilotData);

  const workspace: CommercialIntelligenceWorkspacePayload = {
    facilityLabel: hasPilot ? "Pilot tesis" : "Yetkili tesis",
    reportingPeriodLabel: reportingPeriodLabel(assessments, comparisons),
    freshnessLabel: freshnessLabel(assessments, comparisons),
    evidenceLabel: hasPilot ? "Örnek veri kanıtı" : "Satın alma fiyat kanıtı",
    provenanceDisclaimer: resolveCommercialProvenance(
      workspaceResult.value.provenanceDisclaimer
        || profilesResult.value.provenanceDisclaimer
        || comparisonsResult.value.provenanceDisclaimer,
      hasPilot,
    ),
    ruleSetVersion: workspaceResult.value.ruleSetVersion
      || assessmentsResult.value.ruleSetVersion
      || comparisonsResult.value.ruleSetVersion,
    referenceCenter: workspaceResult.value.referenceCenter,
    defaultMinimumComparableObservations: workspaceResult.value.defaultMinimumComparableObservations,
    hasDevelopmentPilotData: hasPilot,
    unavailableComparisonDimensions: comparisonsResult.value.unavailableDimensions,
    kpis: buildCommercialIntelligenceKpiCards({
      assessments,
      comparisons,
      supplierSpreadThreshold,
    }),
    temporalChart,
    supplierPosition: {
      rows: supplierRows,
      spreadLabel: reconcileCommercialIntelligenceSupplierSpread(
        supplierRows,
        latestSupplierComparison?.supplierSpreadRatio ?? null,
      ),
      comparisonId: latestSupplierComparison?.id ?? null,
      comparisonReference: latestSupplierComparison?.comparisonReference ?? null,
      primaryLabel: latestSupplierComparison
        ? buildCommercialIntelligenceComparisonPrimaryLabel(latestSupplierComparison)
        : null,
      secondaryLabel: latestSupplierComparison
        ? buildCommercialIntelligenceComparisonSecondaryLabel(latestSupplierComparison)
        : null,
      outcomeLabel: latestSupplierComparison?.outcomeLabel ?? null,
      comparableObservationCount: latestSupplierComparison?.comparableObservationCount ?? 0,
      excludedObservationCount: latestSupplierComparison?.excludedObservationCount ?? 0,
      currencyCode: latestSupplierComparison?.currencyCode ?? null,
      normalizedUnit: latestSupplierComparison?.normalizedUnit ?? null,
      evaluatedLabel: latestSupplierComparison?.evaluatedLabel ?? null,
      unavailableReason: supplierRows.length > 0
        ? null
        : latestSupplierComparison
          ? `${latestSupplierComparison.outcomeLabel}: karşılaştırılabilir tedarikçi sıralaması üretilemedi`
          : "Karşılaştırılabilir tedarikçi konumu çalışması yok",
    },
    priorityDeviations,
    evidenceSummary,
    assessments,
    comparisons,
    profiles,
  };

  return { status: "success", workspace, canManage };
}

export async function readCommercialIntelligenceAssessmentDetail(
  assessmentId: string,
): Promise<CommercialIntelligenceAssessmentDetailResult> {
  const contextResult = await resolveCommercialIntelligenceRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message, code: contextResult.code };
  }

  const result = await getCommercialIntelligenceAssessment(contextResult.context, assessmentId);
  if (result.status === "failure") {
    return { status: "failure", message: result.message, code: result.code };
  }

  return { status: "success", detail: mapAssessmentDetail(result.value) };
}

export async function readCommercialIntelligenceComparisonDetail(
  comparisonId: string,
): Promise<CommercialIntelligenceComparisonDetailResult> {
  const contextResult = await resolveCommercialIntelligenceRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message, code: contextResult.code };
  }

  const result = await getCommercialIntelligenceComparison(contextResult.context, comparisonId);
  if (result.status === "failure") {
    return { status: "failure", message: result.message, code: result.code };
  }

  return { status: "success", detail: mapComparisonDetail(result.value) };
}
