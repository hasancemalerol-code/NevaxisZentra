import "./commercial-intelligence-server-boundary";

import {
  buildCommercialSalesPriceComparisonSecondaryLabel,
  buildCommercialSalesPriceCustomerPositionRows,
  buildCommercialSalesPriceKpiCards,
  buildCommercialSalesPriceTemporalChartData,
  filterCommercialSalesPriceCustomerComparisons,
  filterCommercialSalesPriceTemporalComparisons,
  isCompatibleCustomerSalesComparison,
  localizeCommercialIntelligenceMaterialLabel,
  localizeCommercialIntelligenceCustomerLabel,
  mapCommercialSalesPriceCustomerResult,
  reconcileCommercialSalesPriceCustomerSpread,
  selectLatestCompatibleCustomerSalesComparison,
  selectLatestCompatibleTemporalSalesComparison,
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
  getCommercialIntelligenceGrossNetBasisLabel,
  getCommercialIntelligenceOutcomeLabel,
  getCommercialIntelligencePriceBasisLabel,
  getCommercialIntelligenceProfileStatusLabel,
  getCommercialIntelligenceSeverityLabel,
  getCommercialIntelligenceSourceEvidenceKindLabel,
  localizeCommercialSalesPriceProvenance,
} from "../domain/commercial-intelligence-labels";
import type {
  CommercialSalesPriceAssessmentDetail,
  CommercialSalesPriceAssessmentSummary,
  CommercialSalesPriceComparisonDetail,
  CommercialSalesPriceComparisonSummary,
  CommercialSalesPriceCustomerResult,
  CommercialSalesPriceProfileSummary,
  CommercialSalesPriceWorkspacePayload,
} from "../types/commercial-intelligence-types";
import {
  getCommercialSalesPriceAssessment,
  getCommercialSalesPriceComparison,
  getCommercialSalesPriceWorkspace,
  listCommercialSalesPriceAssessments,
  listCommercialSalesPriceComparisons,
  listCommercialSalesPriceProfiles,
  type RawCommercialSalesPriceAssessmentDetail,
  type RawCommercialSalesPriceAssessmentSummary,
  type RawCommercialSalesPriceComparisonDetail,
  type RawCommercialSalesPriceComparisonSummary,
  type RawCommercialSalesPriceProfile,
} from "./commercial-sales-price-api-client";
import { resolveCommercialIntelligenceRequestContext } from "./commercial-intelligence-request-context";
import {
  fetchEffectiveAccessCapabilities,
  hasGrantedPermission,
} from "../../admin-access/server/admin-access-effective-access";

type ReadFailure = Readonly<{ status: "failure"; message: string; code?: string }>;

export type CommercialSalesPriceWorkspaceResult =
  | Readonly<{ status: "success"; workspace: CommercialSalesPriceWorkspacePayload; canManage: boolean }>
  | ReadFailure;

export type CommercialSalesPriceAssessmentDetailResult =
  | Readonly<{ status: "success"; detail: CommercialSalesPriceAssessmentDetail }>
  | ReadFailure;

export type CommercialSalesPriceComparisonDetailResult =
  | Readonly<{ status: "success"; detail: CommercialSalesPriceComparisonDetail }>
  | ReadFailure;

function mapAssessmentSummary(item: RawCommercialSalesPriceAssessmentSummary): CommercialSalesPriceAssessmentSummary {
  return {
    id: item.id,
    assessmentReference: item.assessmentReference,
    profileId: item.profileId,
    profileCode: item.profileCode,
    materialId: item.materialId,
    materialLabel: localizeCommercialIntelligenceMaterialLabel(item.materialId),
    customerReference: item.customerReference,
    customerLabel: localizeCommercialIntelligenceCustomerLabel(item.customerReference),
    sourceEvidenceKind: item.sourceEvidenceKind,
    sourceEvidenceLabel: getCommercialIntelligenceSourceEvidenceKindLabel(item.sourceEvidenceKind),
    comparisonDimension: item.comparisonDimension,
    comparisonDimensionLabel: getCommercialIntelligenceComparisonDimensionLabel(item.comparisonDimension),
    assessmentOutcome: item.assessmentOutcome,
    outcomeLabel: getCommercialIntelligenceOutcomeLabel(item.assessmentOutcome),
    currentUnitPrice: item.currentUnitPrice,
    referenceMedianUnitPrice: item.referenceMedianUnitPrice,
    varianceRatio: item.varianceRatio,
    varianceLabel: formatRatioAsPercent(item.varianceRatio),
    candidateObservationCount: item.candidateObservationCount,
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
    evidenceLabel: getCommercialIntelligenceEvidenceStatusLabel("simulated"),
    ruleSetVersion: item.ruleSetVersion,
    evaluatedAtUtc: item.evaluatedAtUtc,
    evaluatedLabel: formatDateTime(item.evaluatedAtUtc),
    isPilotData: item.isPilotData,
  };
}

function mapComparisonSummary(item: RawCommercialSalesPriceComparisonSummary): CommercialSalesPriceComparisonSummary {
  return {
    id: item.id,
    comparisonReference: item.comparisonReference,
    profileId: item.profileId,
    profileCode: item.profileCode,
    comparisonType: item.comparisonType,
    comparisonTypeLabel: getCommercialIntelligenceComparisonTypeLabel(item.comparisonType),
    materialId: item.materialId,
    materialLabel: localizeCommercialIntelligenceMaterialLabel(item.materialId),
    customerReference: item.customerReference,
    customerLabel: localizeCommercialIntelligenceCustomerLabel(item.customerReference),
    comparisonOutcome: item.comparisonOutcome,
    outcomeLabel: getCommercialIntelligenceOutcomeLabel(item.comparisonOutcome),
    currentMedianUnitPrice: item.currentMedianUnitPrice,
    referenceMedianUnitPrice: item.referenceMedianUnitPrice,
    signedVarianceRatio: item.signedVarianceRatio,
    signedVarianceLabel: formatRatioAsPercent(item.signedVarianceRatio),
    customerSpreadRatio: item.customerSpreadRatio,
    customerSpreadLabel: formatRatioAsPercent(item.customerSpreadRatio),
    candidateObservationCount: item.candidateObservationCount,
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
    evidenceLabel: getCommercialIntelligenceEvidenceStatusLabel("simulated"),
    ruleSetVersion: item.ruleSetVersion,
    evaluatedAtUtc: item.evaluatedAtUtc,
    evaluatedLabel: formatDateTime(item.evaluatedAtUtc),
    isPilotData: item.isPilotData,
  };
}

function mapProfile(profile: RawCommercialSalesPriceProfile): CommercialSalesPriceProfileSummary {
  const evidenceTokens = profile.allowedEvidenceStatuses
    .split(",")
    .map((token) => token.trim())
    .filter(Boolean);
  const sourceEvidenceTokens = profile.allowedSourceEvidenceKinds
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
    customerSpreadThreshold: profile.customerSpreadThreshold,
    customerSpreadThresholdLabel: profile.customerSpreadThreshold === null
      ? "—"
      : formatRatioAsPercent(profile.customerSpreadThreshold),
    allowedEvidenceStatuses: profile.allowedEvidenceStatuses,
    allowedEvidenceLabels: evidenceTokens.map(getCommercialIntelligenceEvidenceStatusLabel),
    allowedSourceEvidenceKinds: profile.allowedSourceEvidenceKinds,
    allowedSourceEvidenceLabels: sourceEvidenceTokens.map(getCommercialIntelligenceSourceEvidenceKindLabel),
    requiredTaxInclusion: profile.requiredTaxInclusion,
    requiredTaxLabel: getCommercialIntelligencePriceBasisLabel(profile.requiredTaxInclusion),
    requiredDiscountInclusion: profile.requiredDiscountInclusion,
    requiredDiscountLabel: getCommercialIntelligencePriceBasisLabel(profile.requiredDiscountInclusion),
    requiredFreightInclusion: profile.requiredFreightInclusion,
    requiredFreightLabel: getCommercialIntelligencePriceBasisLabel(profile.requiredFreightInclusion),
    requiredGrossNetBasis: profile.requiredGrossNetBasis,
    requiredGrossNetLabel: getCommercialIntelligenceGrossNetBasisLabel(profile.requiredGrossNetBasis),
    referenceCenter: profile.referenceCenter,
    ruleSetVersion: profile.ruleSetVersion,
    sourceProvenance: localizeCommercialSalesPriceProvenance(profile.sourceProvenance),
    version: profile.version,
    isExecutable: profile.status === "approved",
    isPilotData: profile.isPilotData,
    compatibilitySummary: `Vergi ${getCommercialIntelligencePriceBasisLabel(profile.requiredTaxInclusion)}, iskonto ${getCommercialIntelligencePriceBasisLabel(profile.requiredDiscountInclusion)}, navlun ${getCommercialIntelligencePriceBasisLabel(profile.requiredFreightInclusion)}, brüt/net ${getCommercialIntelligenceGrossNetBasisLabel(profile.requiredGrossNetBasis)}`,
  };
}

function resolveCustomerSpreadThreshold(
  profiles: readonly CommercialSalesPriceProfileSummary[],
): number | null {
  const approved = profiles.find((profile) => profile.isExecutable);
  return approved?.customerSpreadThreshold ?? null;
}

function reportingPeriodLabel(
  assessments: readonly CommercialSalesPriceAssessmentSummary[],
  comparisons: readonly CommercialSalesPriceComparisonSummary[],
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
  assessments: readonly CommercialSalesPriceAssessmentSummary[],
  comparisons: readonly CommercialSalesPriceComparisonSummary[],
): string {
  const timestamps = [
    ...assessments.map((item) => Date.parse(item.evaluatedAtUtc)),
    ...comparisons.map((item) => Date.parse(item.evaluatedAtUtc)),
  ].filter((value) => Number.isFinite(value));
  if (timestamps.length === 0) return "—";
  return formatDateTime(new Date(Math.max(...timestamps)).toISOString());
}

function mapAssessmentDetail(item: RawCommercialSalesPriceAssessmentDetail): CommercialSalesPriceAssessmentDetail {
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
    priceBasisGrossNet: item.priceBasisGrossNet,
    priceBasisGrossNetLabel: getCommercialIntelligenceGrossNetBasisLabel(item.priceBasisGrossNet),
    priceBasisTaxInclusion: item.priceBasisTaxInclusion,
    priceBasisTaxLabel: getCommercialIntelligencePriceBasisLabel(item.priceBasisTaxInclusion),
    priceBasisDiscountInclusion: item.priceBasisDiscountInclusion,
    priceBasisDiscountLabel: getCommercialIntelligencePriceBasisLabel(item.priceBasisDiscountInclusion),
    priceBasisFreightInclusion: item.priceBasisFreightInclusion,
    priceBasisFreightLabel: getCommercialIntelligencePriceBasisLabel(item.priceBasisFreightInclusion),
    sourceProvenance: localizeCommercialSalesPriceProvenance(item.sourceProvenance),
    drivers,
    exclusionReasons,
  };
}

function mapComparisonDetail(item: RawCommercialSalesPriceComparisonDetail): CommercialSalesPriceComparisonDetail {
  const summary = {
    ...mapComparisonSummary(item),
    evidenceStatus: item.evidenceStatus,
    evidenceLabel: getCommercialIntelligenceEvidenceStatusLabel(item.evidenceStatus),
  };

  const customerResults = buildCommercialSalesPriceCustomerPositionRows(
    item.customerResults.map((result) => mapCommercialSalesPriceCustomerResult(result, item.currencyCode)),
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
    sourceEvidenceKind: item.sourceEvidenceKind,
    sourceEvidenceLabel: getCommercialIntelligenceSourceEvidenceKindLabel(item.sourceEvidenceKind),
    priceBasisGrossNet: item.priceBasisGrossNet,
    priceBasisGrossNetLabel: getCommercialIntelligenceGrossNetBasisLabel(item.priceBasisGrossNet),
    priceBasisTaxInclusion: item.priceBasisTaxInclusion,
    priceBasisTaxLabel: getCommercialIntelligencePriceBasisLabel(item.priceBasisTaxInclusion),
    priceBasisDiscountInclusion: item.priceBasisDiscountInclusion,
    priceBasisDiscountLabel: getCommercialIntelligencePriceBasisLabel(item.priceBasisDiscountInclusion),
    priceBasisFreightInclusion: item.priceBasisFreightInclusion,
    priceBasisFreightLabel: getCommercialIntelligencePriceBasisLabel(item.priceBasisFreightInclusion),
    sourceProvenance: localizeCommercialSalesPriceProvenance(item.sourceProvenance),
    customerResults,
    drivers,
  };
}

function resolveSelectedTemporalComparison(
  comparisons: readonly CommercialSalesPriceComparisonSummary[],
  requestedComparisonId?: string | null,
): CommercialSalesPriceComparisonSummary | null {
  const temporalComparisons = filterCommercialSalesPriceTemporalComparisons(comparisons);
  const defaultComparison = selectLatestCompatibleTemporalSalesComparison(comparisons);
  if (!requestedComparisonId) return defaultComparison;
  const requested = temporalComparisons.find((item) => item.id === requestedComparisonId);
  if (requested) return requested;
  const anyRequested = comparisons.find((item) => item.id === requestedComparisonId);
  if (anyRequested?.comparisonType === "temporal-sales-price-deviation") return anyRequested;
  return defaultComparison;
}

function resolveSelectedCustomerComparison(
  comparisons: readonly CommercialSalesPriceComparisonSummary[],
  requestedComparisonId?: string | null,
): CommercialSalesPriceComparisonSummary | null {
  const customerComparisons = filterCommercialSalesPriceCustomerComparisons(comparisons);
  const defaultComparison = selectLatestCompatibleCustomerSalesComparison(comparisons);
  if (!requestedComparisonId) return defaultComparison;
  const requested = customerComparisons.find((item) => item.id === requestedComparisonId);
  if (requested) return requested;
  const anyRequested = comparisons.find((item) => item.id === requestedComparisonId);
  if (anyRequested?.comparisonType === "customer-price-position") return anyRequested;
  return defaultComparison;
}

export async function readCommercialSalesPriceWorkspace(
  requestedAssessmentId?: string | null,
  requestedComparisonId?: string | null,
): Promise<CommercialSalesPriceWorkspaceResult> {
  const contextResult = await resolveCommercialIntelligenceRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message, code: contextResult.code };
  }

  const access = await fetchEffectiveAccessCapabilities();
  const canManage = hasGrantedPermission(access, "commercial-intelligence.manage");

  const [workspaceResult, profilesResult, assessmentsResult, comparisonsResult] = await Promise.all([
    getCommercialSalesPriceWorkspace(contextResult.context),
    listCommercialSalesPriceProfiles(contextResult.context),
    listCommercialSalesPriceAssessments(contextResult.context, { page: 1, pageSize: 200 }),
    listCommercialSalesPriceComparisons(contextResult.context, { page: 1, pageSize: 200 }),
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
  const customerSpreadThreshold = resolveCustomerSpreadThreshold(profiles);

  const selectedTemporalComparison = resolveSelectedTemporalComparison(comparisons, requestedComparisonId);
  const selectedCustomerComparison = resolveSelectedCustomerComparison(comparisons, requestedComparisonId);

  let customerRows: CommercialSalesPriceCustomerResult[] = [];
  let leadingDriverLabel: string | null = selectedCustomerComparison?.leadingDriverLabel ?? null;

  if (selectedCustomerComparison) {
    const detailResult = await getCommercialSalesPriceComparison(
      contextResult.context,
      selectedCustomerComparison.id,
    );
    if (detailResult.status === "success") {
      customerRows = [...buildCommercialSalesPriceCustomerPositionRows(
        detailResult.value.customerResults.map((result) =>
          mapCommercialSalesPriceCustomerResult(result, detailResult.value.currencyCode),
        ),
      )];
      const leadingDriver = detailResult.value.drivers.find((driver) => driver.isLeadingDriver);
      leadingDriverLabel = leadingDriver
        ? getCommercialIntelligenceDriverReasonLabel(leadingDriver.reasonCode)
        : selectedCustomerComparison.leadingDriverLabel;
    }
  }

  const isCompatibleSelection = selectedCustomerComparison
    ? isCompatibleCustomerSalesComparison(selectedCustomerComparison)
    : false;

  const hasPilot = assessments.some((item) => item.isPilotData)
    || comparisons.some((item) => item.isPilotData)
    || profiles.some((item) => item.isPilotData);

  const temporalChart = buildCommercialSalesPriceTemporalChartData(comparisons);

  const kpis = requestedAssessmentId
    ? (() => {
        const selectedAssessment = assessments.find((item) => item.id === requestedAssessmentId);
        if (!selectedAssessment) {
          return buildCommercialSalesPriceKpiCards({
            assessments,
            comparisons,
            customerSpreadThreshold,
            comparedCustomerCount: customerRows.length,
          });
        }
        return buildCommercialSalesPriceKpiCards({
          assessments: [selectedAssessment],
          comparisons: selectedTemporalComparison ? [selectedTemporalComparison] : [],
          customerSpreadThreshold,
          comparedCustomerCount: customerRows.length,
        });
      })()
    : buildCommercialSalesPriceKpiCards({
        assessments,
        comparisons,
        customerSpreadThreshold,
        comparedCustomerCount: customerRows.length,
      });

  const workspace: CommercialSalesPriceWorkspacePayload = {
    facilityLabel: hasPilot ? "Geliştirme pilot tesisi" : "Yetkili tesis",
    reportingPeriodLabel: reportingPeriodLabel(assessments, comparisons),
    freshnessLabel: freshnessLabel(assessments, comparisons),
    evidenceLabel: hasPilot ? "Simülasyon kanıtı" : "Satış fiyat kanıtı",
    provenanceDisclaimer: localizeCommercialSalesPriceProvenance(
      workspaceResult.value.provenanceDisclaimer
        || profilesResult.value.provenanceDisclaimer,
    ),
    ruleSetVersion: workspaceResult.value.ruleSetVersion
      || assessmentsResult.value.ruleSetVersion
      || comparisonsResult.value.ruleSetVersion,
    referenceCenter: workspaceResult.value.referenceCenter,
    defaultMinimumComparableObservations: workspaceResult.value.defaultMinimumComparableObservations,
    hasDevelopmentPilotData: hasPilot,
    realTransactionMappingComplete: workspaceResult.value.realTransactionMappingComplete,
    customerSpreadThreshold,
    customerSpreadThresholdLabel: customerSpreadThreshold === null
      ? "—"
      : formatRatioAsPercent(customerSpreadThreshold),
    selectedTemporalComparisonId: selectedTemporalComparison?.id ?? null,
    selectedCustomerComparisonId: selectedCustomerComparison?.id ?? null,
    kpis,
    temporalChart,
    temporalComparison: {
      comparisonId: selectedTemporalComparison?.id ?? null,
      comparisonReference: selectedTemporalComparison?.comparisonReference ?? null,
      primaryLabel: selectedTemporalComparison
        ? getCommercialIntelligenceComparisonTypeLabel(selectedTemporalComparison.comparisonType)
        : null,
      secondaryLabel: selectedTemporalComparison
        ? buildCommercialSalesPriceComparisonSecondaryLabel(selectedTemporalComparison)
        : null,
      outcomeLabel: selectedTemporalComparison?.outcomeLabel ?? null,
      signedVarianceLabel: selectedTemporalComparison?.signedVarianceLabel ?? null,
      comparableObservationCount: selectedTemporalComparison?.comparableObservationCount ?? 0,
      excludedObservationCount: selectedTemporalComparison?.excludedObservationCount ?? 0,
      candidateObservationCount: selectedTemporalComparison?.candidateObservationCount ?? 0,
      currencyCode: selectedTemporalComparison?.currencyCode ?? null,
      normalizedUnit: selectedTemporalComparison?.normalizedUnit ?? null,
      evaluatedLabel: selectedTemporalComparison?.evaluatedLabel ?? null,
      unavailableReason: selectedTemporalComparison
        ? null
        : "Karşılaştırılabilir dönemsel satış sapma çalıştırması yok",
    },
    customerComparison: {
      rows: customerRows,
      spreadLabel: reconcileCommercialSalesPriceCustomerSpread(
        customerRows,
        selectedCustomerComparison?.customerSpreadRatio ?? null,
      ),
      comparisonId: selectedCustomerComparison?.id ?? null,
      comparisonReference: selectedCustomerComparison?.comparisonReference ?? null,
      primaryLabel: selectedCustomerComparison
        ? getCommercialIntelligenceComparisonTypeLabel(selectedCustomerComparison.comparisonType)
        : null,
      secondaryLabel: selectedCustomerComparison
        ? buildCommercialSalesPriceComparisonSecondaryLabel(selectedCustomerComparison)
        : null,
      materialLabel: selectedCustomerComparison?.materialLabel ?? null,
      outcomeLabel: selectedCustomerComparison?.outcomeLabel ?? null,
      comparableObservationCount: selectedCustomerComparison?.comparableObservationCount ?? 0,
      excludedObservationCount: selectedCustomerComparison?.excludedObservationCount ?? 0,
      candidateObservationCount: selectedCustomerComparison?.candidateObservationCount ?? 0,
      currencyCode: selectedCustomerComparison?.currencyCode ?? null,
      normalizedUnit: selectedCustomerComparison?.normalizedUnit ?? null,
      evaluatedLabel: selectedCustomerComparison?.evaluatedLabel ?? null,
      leadingDriverLabel,
      isCompatibleSelection,
      unavailableReason: customerRows.length > 0
        ? null
        : selectedCustomerComparison
          ? `${selectedCustomerComparison.outcomeLabel}: karşılaştırılabilir müşteri sıralaması üretilemedi`
          : "Karşılaştırılabilir müşteri karşılaştırması yok",
    },
    assessments,
    comparisons,
    profiles,
  };

  return { status: "success", workspace, canManage };
}

export async function readCommercialSalesPriceAssessmentDetail(
  assessmentId: string,
): Promise<CommercialSalesPriceAssessmentDetailResult> {
  const contextResult = await resolveCommercialIntelligenceRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message, code: contextResult.code };
  }

  const result = await getCommercialSalesPriceAssessment(contextResult.context, assessmentId);
  if (result.status === "failure") {
    return { status: "failure", message: result.message, code: result.code };
  }

  return { status: "success", detail: mapAssessmentDetail(result.value) };
}

export async function readCommercialSalesPriceComparisonDetail(
  comparisonId: string,
): Promise<CommercialSalesPriceComparisonDetailResult> {
  const contextResult = await resolveCommercialIntelligenceRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message, code: contextResult.code };
  }

  const result = await getCommercialSalesPriceComparison(contextResult.context, comparisonId);
  if (result.status === "failure") {
    return { status: "failure", message: result.message, code: result.code };
  }

  return { status: "success", detail: mapComparisonDetail(result.value) };
}
