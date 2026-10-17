import { getCommercialWorkspaceEvidenceLabel, resolveCommercialProvenance } from "../domain/commercial-intelligence-labels";
import "./commercial-intelligence-server-boundary";

import {
  buildQuotationRiskCustomerPositionRows,
  buildQuotationRiskDistribution,
  buildQuotationRiskKpiCards,
  buildQuotationRiskTemporalChartData,
  filterQuotationRiskCustomerComparisons,
  filterQuotationRiskTemporalComparisons,
  mapQuotationRiskCustomerResult,
  selectLatestQuotationRiskCustomerComparison,
  selectLatestQuotationRiskTemporalComparison,
} from "../components/commercial-quotation-risk-presentation";
import { localizeCommercialIntelligenceMaterialLabel } from "../components/commercial-intelligence-presentation";
import {
  formatCompactPeriod,
  formatCurrency,
  formatDateTime,
  formatIncidenceDeltaPoints,
  formatIncidenceFraction,
  formatRatioAsPercent,
  formatRemainingValidityDays,
} from "../domain/commercial-intelligence-formatters";
import {
  getCommercialIntelligenceEvidenceStatusLabel,
  getCommercialIntelligenceGrossNetBasisLabel,
  getCommercialIntelligencePriceBasisLabel,
  getCommercialIntelligenceProfileStatusLabel,
  getCommercialIntelligenceSeverityLabel,
  getCommercialIntelligenceSourceEvidenceKindLabel,
} from "../domain/commercial-intelligence-labels";
import {
  getCommercialQuotationRiskComparisonTypeLabel,
  getCommercialQuotationRiskDriverReasonLabel,
  getCommercialQuotationRiskOutcomeLabel,
  inferQuotationRiskEvidenceStatus,
  localizeCommercialQuotationRiskProvenance,
} from "../domain/commercial-quotation-risk-labels";
import type {
  CommercialQuotationRiskAssessmentDetail,
  CommercialQuotationRiskAssessmentSummary,
  CommercialQuotationRiskComparisonDetail,
  CommercialQuotationRiskComparisonSummary,
  CommercialQuotationRiskCustomerResult,
  CommercialQuotationRiskProfileSummary,
  CommercialQuotationRiskWorkspacePayload,
} from "../types/commercial-quotation-risk-types";
import {
  getCommercialQuotationRiskAssessment,
  getCommercialQuotationRiskComparison,
  getCommercialQuotationRiskWorkspace,
  listCommercialQuotationRiskAssessments,
  listCommercialQuotationRiskComparisons,
  listCommercialQuotationRiskProfiles,
  type RawCommercialQuotationRiskAssessmentDetail,
  type RawCommercialQuotationRiskAssessmentSummary,
  type RawCommercialQuotationRiskComparisonDetail,
  type RawCommercialQuotationRiskComparisonSummary,
  type RawCommercialQuotationRiskProfile,
} from "./commercial-quotation-risk-api-client";
import { resolveCommercialIntelligenceRequestContext } from "./commercial-intelligence-request-context";
import {
  fetchEffectiveAccessCapabilities,
  hasGrantedPermission,
} from "../../admin-access/server/admin-access-effective-access";

type ReadFailure = Readonly<{ status: "failure"; message: string; code?: string }>;

export type CommercialQuotationRiskWorkspaceResult =
  | Readonly<{ status: "success"; workspace: CommercialQuotationRiskWorkspacePayload; canManage: boolean }>
  | ReadFailure;

export type CommercialQuotationRiskAssessmentDetailResult =
  | Readonly<{ status: "success"; detail: CommercialQuotationRiskAssessmentDetail }>
  | ReadFailure;

export type CommercialQuotationRiskComparisonDetailResult =
  | Readonly<{ status: "success"; detail: CommercialQuotationRiskComparisonDetail }>
  | ReadFailure;

function mapAssessmentSummary(item: RawCommercialQuotationRiskAssessmentSummary): CommercialQuotationRiskAssessmentSummary {
  const evidenceStatus = inferQuotationRiskEvidenceStatus(item.outcome);
  return {
    id: item.id,
    assessmentReference: item.assessmentReference,
    profileId: item.profileId,
    profileCode: item.profileCode,
    evidenceObservationId: item.evidenceObservationId,
    quotationReference: item.quotationReference,
    customerReference: item.customerReference,
    customerLabel: item.customerReference,
    materialId: item.materialId,
    materialLabel: localizeCommercialIntelligenceMaterialLabel(item.materialId),
    outcome: item.outcome,
    outcomeLabel: getCommercialQuotationRiskOutcomeLabel(item.outcome),
    quotedComparableUnitPrice: item.quotedComparableUnitPrice,
    quotedPriceLabel: formatCurrency(item.quotedComparableUnitPrice, item.currencyCode),
    referenceMedianUnitPrice: item.referenceMedianUnitPrice,
    referencePriceLabel: formatCurrency(item.referenceMedianUnitPrice, item.currencyCode),
    signedPriceVarianceRatio: item.signedPriceVarianceRatio,
    varianceLabel: formatRatioAsPercent(item.signedPriceVarianceRatio),
    candidateObservationCount: item.candidateObservationCount,
    comparableObservationCount: item.comparableObservationCount,
    excludedObservationCount: item.excludedObservationCount,
    paymentTermDays: item.paymentTermDays,
    remainingValidityDays: item.remainingValidityDays,
    remainingValidityLabel: formatRemainingValidityDays(item.remainingValidityDays),
    currencyCode: item.currencyCode,
    unitOfMeasure: item.unitOfMeasure,
    referenceWindowStartUtc: item.referenceWindowStartUtc,
    referenceWindowEndUtc: item.referenceWindowEndUtc,
    windowLabel: formatCompactPeriod(item.referenceWindowStartUtc, item.referenceWindowEndUtc),
    leadingDriverCode: item.leadingDriverCode,
    leadingDriverLabel: getCommercialQuotationRiskDriverReasonLabel(item.leadingDriverCode),
    evidenceStatus,
    evidenceLabel: getCommercialIntelligenceEvidenceStatusLabel(evidenceStatus),
    ruleSetVersion: item.ruleSetVersion,
    evaluatedAtUtc: item.evaluatedAtUtc,
    evaluatedLabel: formatDateTime(item.evaluatedAtUtc),
    isPilotData: item.isPilotData,
  };
}

function mapComparisonSummary(item: RawCommercialQuotationRiskComparisonSummary): CommercialQuotationRiskComparisonSummary {
  return {
    id: item.id,
    comparisonReference: item.comparisonReference,
    profileId: item.profileId,
    profileCode: item.profileCode,
    comparisonType: item.comparisonType,
    comparisonTypeLabel: getCommercialQuotationRiskComparisonTypeLabel(item.comparisonType),
    materialId: item.materialId,
    materialLabel: item.materialId ? localizeCommercialIntelligenceMaterialLabel(item.materialId) : "—",
    customerReference: item.customerReference,
    customerLabel: item.customerReference ?? "—",
    comparisonOutcome: item.comparisonOutcome,
    outcomeLabel: getCommercialQuotationRiskOutcomeLabel(item.comparisonOutcome),
    evidenceStatus: item.evidenceStatus,
    evidenceLabel: getCommercialIntelligenceEvidenceStatusLabel(item.evidenceStatus),
    candidateAssessmentCount: item.candidateAssessmentCount,
    comparableAssessmentCount: item.comparableAssessmentCount,
    excludedAssessmentCount: item.excludedAssessmentCount,
    referenceRiskIncidence: item.referenceRiskIncidence,
    referenceIncidenceLabel: formatIncidenceFraction(item.referenceRiskIncidence),
    currentRiskIncidence: item.currentRiskIncidence,
    currentIncidenceLabel: formatIncidenceFraction(item.currentRiskIncidence),
    riskIncidenceDelta: item.riskIncidenceDelta,
    incidenceDeltaLabel: formatIncidenceDeltaPoints(item.riskIncidenceDelta),
    referenceWindowStartUtc: item.referenceWindowStartUtc,
    referenceWindowEndUtc: item.referenceWindowEndUtc,
    referenceWindowLabel: formatCompactPeriod(item.referenceWindowStartUtc, item.referenceWindowEndUtc),
    currentWindowStartUtc: item.currentWindowStartUtc,
    currentWindowEndUtc: item.currentWindowEndUtc,
    currentWindowLabel: formatCompactPeriod(item.currentWindowStartUtc, item.currentWindowEndUtc),
    leadingDriverCode: item.leadingDriverCode,
    leadingDriverLabel: getCommercialQuotationRiskDriverReasonLabel(item.leadingDriverCode),
    ruleSetVersion: item.ruleSetVersion,
    evaluatedAtUtc: item.evaluatedAtUtc,
    evaluatedLabel: formatDateTime(item.evaluatedAtUtc),
    isPilotData: item.isPilotData,
    realQuotationMappingComplete: item.realQuotationMappingComplete,
  };
}

function mapProfile(profile: RawCommercialQuotationRiskProfile): CommercialQuotationRiskProfileSummary {
  const evidenceTokens = profile.allowedEvidenceStatuses.split(",").map((token) => token.trim()).filter(Boolean);
  const sourceTokens = profile.allowedSalesEvidenceKinds.split(",").map((token) => token.trim()).filter(Boolean);
  return {
    id: profile.id,
    profileCode: profile.profileCode,
    displayName: profile.name,
    status: profile.status,
    statusLabel: getCommercialIntelligenceProfileStatusLabel(profile.status),
    referenceWindowDurationDays: profile.referenceWindowDurationDays,
    minimumComparableObservations: profile.minimumComparableObservations,
    maximumNegativePriceVarianceRatio: profile.maximumNegativePriceVarianceRatio,
    maximumNegativeVarianceLabel: formatRatioAsPercent(profile.maximumNegativePriceVarianceRatio),
    maximumPositivePriceVarianceRatio: profile.maximumPositivePriceVarianceRatio,
    maximumPositiveVarianceLabel: formatRatioAsPercent(profile.maximumPositivePriceVarianceRatio),
    maximumPaymentTermDays: profile.maximumPaymentTermDays,
    expiryWarningDays: profile.expiryWarningDays,
    maximumEvidenceAgeDays: profile.maximumEvidenceAgeDays,
    maximumRiskIncidenceDelta: profile.maximumRiskIncidenceDelta,
    maximumRiskIncidenceDeltaLabel: formatIncidenceFraction(profile.maximumRiskIncidenceDelta),
    allowedEvidenceStatuses: profile.allowedEvidenceStatuses,
    allowedEvidenceLabels: evidenceTokens.map(getCommercialIntelligenceEvidenceStatusLabel),
    allowedSalesEvidenceKinds: profile.allowedSalesEvidenceKinds,
    allowedSourceEvidenceLabels: sourceTokens.map(getCommercialIntelligenceSourceEvidenceKindLabel),
    requiredTaxInclusion: profile.requiredTaxInclusion,
    requiredTaxLabel: getCommercialIntelligencePriceBasisLabel(profile.requiredTaxInclusion),
    requiredDiscountInclusion: profile.requiredDiscountInclusion,
    requiredDiscountLabel: getCommercialIntelligencePriceBasisLabel(profile.requiredDiscountInclusion),
    requiredFreightInclusion: profile.requiredFreightInclusion,
    requiredFreightLabel: getCommercialIntelligencePriceBasisLabel(profile.requiredFreightInclusion),
    requiredGrossNetBasis: profile.requiredGrossNetBasis,
    requiredGrossNetLabel: getCommercialIntelligenceGrossNetBasisLabel(profile.requiredGrossNetBasis),
    ruleSetVersion: profile.ruleSetVersion,
    sourceProvenance: localizeCommercialQuotationRiskProvenance(profile.sourceProvenance),
    version: profile.version,
    isExecutable: profile.status === "approved",
    isPilotData: profile.isPilotData,
    compatibilitySummary: `Vergi ${getCommercialIntelligencePriceBasisLabel(profile.requiredTaxInclusion)}, iskonto ${getCommercialIntelligencePriceBasisLabel(profile.requiredDiscountInclusion)}, navlun ${getCommercialIntelligencePriceBasisLabel(profile.requiredFreightInclusion)}, brüt/net ${getCommercialIntelligenceGrossNetBasisLabel(profile.requiredGrossNetBasis)}`,
  };
}

function mapAssessmentDetail(item: RawCommercialQuotationRiskAssessmentDetail): CommercialQuotationRiskAssessmentDetail {
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
      reasonLabel: getCommercialQuotationRiskDriverReasonLabel(driver.reasonCode),
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

  return {
    summary,
    profileVersion: item.profileVersion,
    grossNetBasis: item.grossNetBasis,
    grossNetBasisLabel: getCommercialIntelligenceGrossNetBasisLabel(item.grossNetBasis),
    taxBasis: item.taxBasis,
    taxBasisLabel: getCommercialIntelligencePriceBasisLabel(item.taxBasis),
    discountBasis: item.discountBasis,
    discountBasisLabel: getCommercialIntelligencePriceBasisLabel(item.discountBasis),
    freightBasis: item.freightBasis,
    freightBasisLabel: getCommercialIntelligencePriceBasisLabel(item.freightBasis),
    sourceProvenance: localizeCommercialQuotationRiskProvenance(item.sourceProvenance),
    drivers,
  };
}

function mapComparisonDetail(item: RawCommercialQuotationRiskComparisonDetail): CommercialQuotationRiskComparisonDetail {
  const summary = mapComparisonSummary(item);
  const customerResults = buildQuotationRiskCustomerPositionRows(
    item.customerResults.map((result) => mapQuotationRiskCustomerResult(result)),
  );
  const drivers = item.drivers
    .slice()
    .sort((left, right) => left.sortOrder - right.sortOrder)
    .map((driver) => ({
      id: driver.id,
      comparisonResultId: driver.comparisonResultId,
      reasonCode: driver.reasonCode,
      reasonLabel: getCommercialQuotationRiskDriverReasonLabel(driver.reasonCode),
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

  return {
    summary,
    profileVersion: item.profileVersion,
    riskBearingAssessmentCount: item.riskBearingAssessmentCount,
    insufficientDataCount: item.insufficientDataCount,
    sourceConflictCount: item.sourceConflictCount,
    priceRiskCount: item.priceRiskCount,
    expiryRiskCount: item.expiryRiskCount,
    paymentTermRiskCount: item.paymentTermRiskCount,
    referenceCandidateAssessmentCount: item.referenceCandidateAssessmentCount,
    referenceComparableAssessmentCount: item.referenceComparableAssessmentCount,
    referenceExcludedAssessmentCount: item.referenceExcludedAssessmentCount,
    referenceRiskBearingAssessmentCount: item.referenceRiskBearingAssessmentCount,
    currentCandidateAssessmentCount: item.currentCandidateAssessmentCount,
    currentComparableAssessmentCount: item.currentComparableAssessmentCount,
    currentExcludedAssessmentCount: item.currentExcludedAssessmentCount,
    currentRiskBearingAssessmentCount: item.currentRiskBearingAssessmentCount,
    sourceProvenance: localizeCommercialQuotationRiskProvenance(item.sourceProvenance),
    customerResults,
    drivers,
  };
}

function reportingPeriodLabel(
  assessments: readonly CommercialQuotationRiskAssessmentSummary[],
  comparisons: readonly CommercialQuotationRiskComparisonSummary[],
): string {
  const timestamps = [
    ...assessments.flatMap((item) => [Date.parse(item.referenceWindowStartUtc), Date.parse(item.referenceWindowEndUtc)]),
    ...comparisons.flatMap((item) => [
      Date.parse(item.currentWindowStartUtc),
      Date.parse(item.currentWindowEndUtc),
      item.referenceWindowStartUtc ? Date.parse(item.referenceWindowStartUtc) : NaN,
      item.referenceWindowEndUtc ? Date.parse(item.referenceWindowEndUtc) : NaN,
    ]),
  ].filter((value) => Number.isFinite(value));
  if (timestamps.length === 0) return "—";
  return formatCompactPeriod(new Date(Math.min(...timestamps)).toISOString(), new Date(Math.max(...timestamps)).toISOString());
}

function freshnessLabel(
  assessments: readonly CommercialQuotationRiskAssessmentSummary[],
  comparisons: readonly CommercialQuotationRiskComparisonSummary[],
): string {
  const timestamps = [
    ...assessments.map((item) => Date.parse(item.evaluatedAtUtc)),
    ...comparisons.map((item) => Date.parse(item.evaluatedAtUtc)),
  ].filter((value) => Number.isFinite(value));
  if (timestamps.length === 0) return "—";
  return formatDateTime(new Date(Math.max(...timestamps)).toISOString());
}

function resolveSelectedTemporalComparison(
  comparisons: readonly CommercialQuotationRiskComparisonSummary[],
  requestedComparisonId?: string | null,
): CommercialQuotationRiskComparisonSummary | null {
  const temporal = filterQuotationRiskTemporalComparisons(comparisons);
  const fallback = selectLatestQuotationRiskTemporalComparison(comparisons);
  if (!requestedComparisonId) return fallback;
  return temporal.find((item) => item.id === requestedComparisonId) ?? fallback;
}

function resolveSelectedCustomerComparison(
  comparisons: readonly CommercialQuotationRiskComparisonSummary[],
  requestedComparisonId?: string | null,
): CommercialQuotationRiskComparisonSummary | null {
  const customer = filterQuotationRiskCustomerComparisons(comparisons);
  const fallback = selectLatestQuotationRiskCustomerComparison(comparisons);
  if (!requestedComparisonId) return fallback;
  return customer.find((item) => item.id === requestedComparisonId) ?? fallback;
}

export async function readCommercialQuotationRiskWorkspace(
  requestedAssessmentId?: string | null,
  requestedComparisonId?: string | null,
): Promise<CommercialQuotationRiskWorkspaceResult> {
  const contextResult = await resolveCommercialIntelligenceRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message, code: contextResult.code };
  }

  const access = await fetchEffectiveAccessCapabilities();
  const canManage = hasGrantedPermission(access, "commercial-intelligence.manage");

  const [workspaceResult, profilesResult, assessmentsResult, comparisonsResult] = await Promise.all([
    getCommercialQuotationRiskWorkspace(contextResult.context),
    listCommercialQuotationRiskProfiles(contextResult.context),
    listCommercialQuotationRiskAssessments(contextResult.context, { page: 1, pageSize: 200 }),
    listCommercialQuotationRiskComparisons(contextResult.context, { page: 1, pageSize: 200 }),
  ]);

  if (workspaceResult.status === "failure") return { status: "failure", message: workspaceResult.message, code: workspaceResult.code };
  if (profilesResult.status === "failure") return { status: "failure", message: profilesResult.message, code: profilesResult.code };
  if (assessmentsResult.status === "failure") return { status: "failure", message: assessmentsResult.message, code: assessmentsResult.code };
  if (comparisonsResult.status === "failure") return { status: "failure", message: comparisonsResult.message, code: comparisonsResult.code };

  const assessments = assessmentsResult.value.items.map(mapAssessmentSummary);
  const comparisons = comparisonsResult.value.items.map(mapComparisonSummary);
  const profiles = profilesResult.value.items.map(mapProfile);

  const selectedTemporal = resolveSelectedTemporalComparison(comparisons, requestedComparisonId);
  const selectedCustomer = resolveSelectedCustomerComparison(comparisons, requestedComparisonId);

  let customerRows: CommercialQuotationRiskCustomerResult[] = [];
  let temporalDetail: CommercialQuotationRiskComparisonDetail | null = null;

  if (selectedCustomer) {
    const detailResult = await getCommercialQuotationRiskComparison(contextResult.context, selectedCustomer.id);
    if (detailResult.status === "success") {
      temporalDetail = null;
      customerRows = [...mapComparisonDetail(detailResult.value).customerResults];
    }
  }

  if (selectedTemporal) {
    const detailResult = await getCommercialQuotationRiskComparison(contextResult.context, selectedTemporal.id);
    if (detailResult.status === "success") {
      temporalDetail = mapComparisonDetail(detailResult.value);
    }
  }

  const hasPilot = assessments.some((item) => item.isPilotData)
    || comparisons.some((item) => item.isPilotData)
    || profiles.some((item) => item.isPilotData);

  const kpis = buildQuotationRiskKpiCards({
    assessments: requestedAssessmentId
      ? assessments.filter((item) => item.id === requestedAssessmentId)
      : assessments,
    temporalComparison: selectedTemporal,
  });

  const temporalTrendUnavailable = !selectedTemporal
    ? "Zamansal risk karşılaştırması yok"
    : selectedTemporal.comparisonOutcome === "source-conflict"
      ? "Kaynak çakışması"
      : selectedTemporal.comparisonOutcome === "insufficient-data"
        ? "Karşılaştırma için yeterli veri yok"
        : null;

  return {
    status: "success",
    canManage,
    workspace: {
      facilityLabel: hasPilot ? "Pilot tesis" : "Yetkili tesis",
      reportingPeriodLabel: reportingPeriodLabel(assessments, comparisons),
      freshnessLabel: freshnessLabel(assessments, comparisons),
      evidenceLabel: getCommercialWorkspaceEvidenceLabel(hasPilot),
      provenanceDisclaimer: resolveCommercialProvenance(workspaceResult.value.provenanceDisclaimer, hasPilot, localizeCommercialQuotationRiskProvenance),
      ruleSetVersion: workspaceResult.value.ruleSetVersion,
      comparisonRuleSetVersion: workspaceResult.value.comparisonRuleSetVersion,
      defaultMinimumComparableObservations: workspaceResult.value.defaultMinimumComparableObservations,
      hasDevelopmentPilotData: hasPilot,
      realQuotationMappingComplete: workspaceResult.value.realQuotationMappingComplete,
      customerSegmentMappingComplete: false,
      selectedTemporalComparisonId: selectedTemporal?.id ?? null,
      selectedCustomerComparisonId: selectedCustomer?.id ?? null,
      kpis,
      riskDistribution: buildQuotationRiskDistribution(assessments),
      temporalChart: buildQuotationRiskTemporalChartData(comparisons),
      temporalTrend: {
        comparisonId: selectedTemporal?.id ?? null,
        comparisonReference: selectedTemporal?.comparisonReference ?? null,
        referenceWindowLabel: selectedTemporal?.referenceWindowLabel ?? "—",
        currentWindowLabel: selectedTemporal?.currentWindowLabel ?? "—",
        referenceIncidenceLabel: selectedTemporal?.referenceIncidenceLabel ?? "Hesaplanamadı",
        currentIncidenceLabel: selectedTemporal?.currentIncidenceLabel ?? "Hesaplanamadı",
        incidenceDeltaLabel: selectedTemporal?.incidenceDeltaLabel ?? "Hesaplanamadı",
        outcomeLabel: selectedTemporal?.outcomeLabel ?? null,
        leadingDriverLabel: selectedTemporal?.leadingDriverLabel ?? null,
        referenceComparableCount: temporalDetail?.referenceComparableAssessmentCount ?? null,
        referenceExcludedCount: temporalDetail?.referenceExcludedAssessmentCount ?? null,
        currentComparableCount: temporalDetail?.currentComparableAssessmentCount ?? null,
        currentExcludedCount: temporalDetail?.currentExcludedAssessmentCount ?? null,
        priceRiskCount: temporalDetail?.priceRiskCount ?? null,
        expiryRiskCount: temporalDetail?.expiryRiskCount ?? null,
        paymentTermRiskCount: temporalDetail?.paymentTermRiskCount ?? null,
        unavailableReason: temporalTrendUnavailable,
        evidenceLabel: selectedTemporal?.evidenceLabel ?? null,
      },
      customerComparison: {
        rows: customerRows,
        comparisonId: selectedCustomer?.id ?? null,
        comparisonReference: selectedCustomer?.comparisonReference ?? null,
        outcomeLabel: selectedCustomer?.outcomeLabel ?? null,
        leadingDriverLabel: selectedCustomer?.leadingDriverLabel ?? null,
        unavailableReason: customerRows.length === 0 ? "Müşteri risk konumu karşılaştırması yok" : null,
      },
      assessments,
      comparisons,
      profiles,
    },
  };
}

export async function readCommercialQuotationRiskAssessmentDetail(
  assessmentId: string,
): Promise<CommercialQuotationRiskAssessmentDetailResult> {
  const contextResult = await resolveCommercialIntelligenceRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message, code: contextResult.code };
  }

  const result = await getCommercialQuotationRiskAssessment(contextResult.context, assessmentId);
  if (result.status === "failure") {
    return { status: "failure", message: result.message, code: result.code };
  }

  return { status: "success", detail: mapAssessmentDetail(result.value) };
}

export async function readCommercialQuotationRiskComparisonDetail(
  comparisonId: string,
): Promise<CommercialQuotationRiskComparisonDetailResult> {
  const contextResult = await resolveCommercialIntelligenceRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message, code: contextResult.code };
  }

  const result = await getCommercialQuotationRiskComparison(contextResult.context, comparisonId);
  if (result.status === "failure") {
    return { status: "failure", message: result.message, code: result.code };
  }

  return { status: "success", detail: mapComparisonDetail(result.value) };
}
