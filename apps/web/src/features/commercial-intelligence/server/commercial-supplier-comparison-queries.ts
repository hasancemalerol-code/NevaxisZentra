import "./commercial-intelligence-server-boundary";

import {
  buildCommercialIntelligenceComparisonPrimaryLabel,
  buildCommercialIntelligenceComparisonSecondaryLabel,
  buildCommercialIntelligenceSupplierPositionRows,
  buildCommercialSupplierComparisonKpiCards,
  filterCommercialIntelligenceSupplierComparisons,
  isCompatibleSupplierComparison,
  localizeCommercialIntelligenceMaterialLabel,
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
} from "../domain/commercial-intelligence-labels";
import type {
  CommercialIntelligenceComparisonSummary,
  CommercialIntelligenceProfileSummary,
  CommercialIntelligenceSupplierResult,
  CommercialSupplierComparisonWorkspacePayload,
} from "../types/commercial-intelligence-types";
import {
  getCommercialIntelligenceComparison,
  getCommercialIntelligenceWorkspace,
  listCommercialIntelligenceComparisons,
  listCommercialIntelligenceProfiles,
  type RawCommercialIntelligenceComparisonSummary,
  type RawCommercialIntelligenceProfile,
} from "./commercial-intelligence-api-client";
import { resolveCommercialIntelligenceRequestContext } from "./commercial-intelligence-request-context";
import {
  fetchCurrentEffectiveAccess,
  hasGrantedPermission,
} from "../../admin-access/server/admin-access-effective-access";

type ReadFailure = Readonly<{ status: "failure"; message: string; code?: string }>;

export type CommercialSupplierComparisonWorkspaceResult =
  | Readonly<{ status: "success"; workspace: CommercialSupplierComparisonWorkspacePayload; canManage: boolean }>
  | ReadFailure;

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
    evidenceLabel: getCommercialIntelligenceEvidenceStatusLabel("simulated"),
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

function reportingPeriodLabel(comparisons: readonly CommercialIntelligenceComparisonSummary[]): string {
  const timestamps = comparisons.flatMap((item) => [
    Date.parse(item.currentWindowStartUtc),
    Date.parse(item.currentWindowEndUtc),
  ]).filter((value) => Number.isFinite(value));

  if (timestamps.length === 0) return "—";
  const min = Math.min(...timestamps);
  const max = Math.max(...timestamps);
  return formatCompactPeriod(new Date(min).toISOString(), new Date(max).toISOString());
}

function freshnessLabel(comparisons: readonly CommercialIntelligenceComparisonSummary[]): string {
  const timestamps = comparisons
    .map((item) => Date.parse(item.evaluatedAtUtc))
    .filter((value) => Number.isFinite(value));
  if (timestamps.length === 0) return "—";
  return formatDateTime(new Date(Math.max(...timestamps)).toISOString());
}

export async function readCommercialSupplierComparisonWorkspace(
  requestedComparisonId?: string | null,
): Promise<CommercialSupplierComparisonWorkspaceResult> {
  const contextResult = await resolveCommercialIntelligenceRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message, code: contextResult.code };
  }

  const access = await fetchCurrentEffectiveAccess();
  const canManage = hasGrantedPermission(access, "commercial-intelligence.manage");

  const [workspaceResult, profilesResult, comparisonsResult] = await Promise.all([
    getCommercialIntelligenceWorkspace(contextResult.context),
    listCommercialIntelligenceProfiles(contextResult.context),
    listCommercialIntelligenceComparisons(contextResult.context, {
      comparisonType: "supplier-price-position",
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
  const supplierComparisons = filterCommercialIntelligenceSupplierComparisons(comparisons);
  const profiles = profilesResult.value.items.map(mapProfile);
  const supplierSpreadThreshold = resolveSupplierSpreadThreshold(profiles);
  const defaultComparison = selectLatestCompatibleSupplierComparison(comparisons);
  const selectedComparison = requestedComparisonId
    ? supplierComparisons.find((item) => item.id === requestedComparisonId) ?? defaultComparison
    : defaultComparison;
  const isCompatibleSelection = selectedComparison
    ? isCompatibleSupplierComparison(selectedComparison)
    : false;

  let supplierRows: CommercialIntelligenceSupplierResult[] = [];
  let leadingDriverLabel: string | null = selectedComparison?.leadingDriverLabel ?? null;

  if (selectedComparison) {
    const detailResult = await getCommercialIntelligenceComparison(
      contextResult.context,
      selectedComparison.id,
    );
    if (detailResult.status === "success") {
      supplierRows = [...buildCommercialIntelligenceSupplierPositionRows(
        detailResult.value.supplierResults.map((result) =>
          mapCommercialIntelligenceSupplierResult(result, detailResult.value.currencyCode),
        ),
      )];
      const leadingDriver = detailResult.value.drivers.find((driver) => driver.isLeadingDriver);
      leadingDriverLabel = leadingDriver
        ? getCommercialIntelligenceDriverReasonLabel(leadingDriver.reasonCode)
        : selectedComparison.leadingDriverLabel;
    }
  }

  const hasPilot = comparisons.some((item) => item.isPilotData) || profiles.some((item) => item.isPilotData);

  const workspace: CommercialSupplierComparisonWorkspacePayload = {
    facilityLabel: hasPilot ? "Geliştirme pilot tesisi" : "Yetkili tesis",
    reportingPeriodLabel: reportingPeriodLabel(supplierComparisons),
    freshnessLabel: freshnessLabel(supplierComparisons),
    evidenceLabel: hasPilot ? "Simülasyon kanıtı" : "Satın alma fiyat kanıtı",
    provenanceDisclaimer: workspaceResult.value.provenanceDisclaimer
      || profilesResult.value.provenanceDisclaimer
      || comparisonsResult.value.provenanceDisclaimer,
    ruleSetVersion: workspaceResult.value.ruleSetVersion || comparisonsResult.value.ruleSetVersion,
    defaultMinimumComparableObservations: workspaceResult.value.defaultMinimumComparableObservations,
    hasDevelopmentPilotData: hasPilot,
    supplierSpreadThreshold,
    supplierSpreadThresholdLabel: supplierSpreadThreshold === null
      ? "—"
      : formatRatioAsPercent(supplierSpreadThreshold),
    selectedComparisonId: selectedComparison?.id ?? null,
    isCompatibleSelection,
    supplierPosition: {
      rows: supplierRows,
      spreadLabel: reconcileCommercialIntelligenceSupplierSpread(
        supplierRows,
        selectedComparison?.supplierSpreadRatio ?? null,
      ),
      comparisonId: selectedComparison?.id ?? null,
      comparisonReference: selectedComparison?.comparisonReference ?? null,
      primaryLabel: selectedComparison
        ? buildCommercialIntelligenceComparisonPrimaryLabel(selectedComparison)
        : null,
      secondaryLabel: selectedComparison
        ? buildCommercialIntelligenceComparisonSecondaryLabel(selectedComparison)
        : null,
      materialLabel: selectedComparison?.materialLabel ?? null,
      outcomeLabel: selectedComparison?.outcomeLabel ?? null,
      comparableObservationCount: selectedComparison?.comparableObservationCount ?? 0,
      excludedObservationCount: selectedComparison?.excludedObservationCount ?? 0,
      currencyCode: selectedComparison?.currencyCode ?? null,
      normalizedUnit: selectedComparison?.normalizedUnit ?? null,
      evaluatedLabel: selectedComparison?.evaluatedLabel ?? null,
      leadingDriverLabel,
      unavailableReason: supplierRows.length > 0
        ? null
        : selectedComparison
          ? `${selectedComparison.outcomeLabel}: karşılaştırılabilir tedarikçi sıralaması üretilemedi`
          : "Karşılaştırılabilir tedarikçi karşılaştırması yok",
    },
    kpis: buildCommercialSupplierComparisonKpiCards({
      selectedComparison,
      supplierRows,
      supplierComparisons,
      isCompatibleSelection,
    }),
    comparisons: supplierComparisons,
    profiles,
  };

  return { status: "success", workspace, canManage };
}
