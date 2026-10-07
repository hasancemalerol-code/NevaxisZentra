import type {
  OperationalValueBreakdownItemApi,
  OperationalValueCaseDetailApi,
  OperationalValueControlCenterApi,
  OperationalValueEconomicInputSnapshotApi,
  OperationalValueReportPreviewApi,
  OperationalValueTrendPointApi,
} from "../server/operational-value-api-client";

/**
 * Wire contract of GET /api/operational-value/{control-center,cases/{id},report-preview} as produced by the API
 * (packages/NevaxisZentra.Persistence/OperationalValue/OperationalValueModels.cs, camelCased by System.Text.Json).
 * The view model above is richer than the wire contract in places; this mapper fills only what the API reports and
 * leaves the rest undefined so the view renders "—" rather than invented figures.
 */
export type RawOperationalValueSummary = Readonly<{
  verifiedOutcomes: number;
  goodOutputGainUnits: number;
  downtimeAvoidedMinutes: number;
  lateOrdersProtected: number;
  humanTouchesReduced: number;
  intelligenceCost: number;
  verifiedEconomicValue: number | null;
  costPerVerifiedOutcome: number | null;
}>;

export type RawOperationalValueEconomicInputs = Readonly<{
  configurationId: string;
  versionCode: string;
  currency: string;
  unitContributionValue: number | null;
  scrapUnitCost: number | null;
  downtimeCostPerHour: number | null;
  laborCostPerHour: number | null;
  overtimeCostPerHour?: number | null;
  changeoverCostPerMinute: number | null;
  energyUnitCost?: number | null;
  lateDeliveryPenalty: number | null;
  effectiveFromUtc: string;
  updatedAtUtc: string;
}>;

export type RawOperationalValueControlCenter = Readonly<{
  summary: RawOperationalValueSummary;
  trend: readonly Readonly<{
    periodLabel: string;
    verifiedOutcomeCount: number;
    goodOutputGainUnits: number;
    downtimeAvoidedMinutes: number;
    verifiedEconomicValue: number | null;
    intelligenceCost: number;
    costPerVerifiedOutcome: number | null;
  }>[];
  breakdown: readonly Readonly<{
    dimensionType: string;
    dimensionKey: string;
    label: string;
    verifiedCount: number;
    operationalDeltaTotal: number;
    unit: string;
    economicValueTotal: number | null;
    intelligenceCostTotal: number;
  }>[];
  recentValues: readonly Readonly<{
    valueRecordId: string;
    valueCategory: string;
    workflowType: string;
    lineCode: string | null;
  }>[];
  economicInputs: RawOperationalValueEconomicInputs | null;
  generatedAtUtc: string;
}>;

export type RawOperationalValueCaseDetail = Readonly<{
  valueRecordId: string;
  problemSummary: string;
  baseline: Readonly<{ value: number; unit: string; source: string; window: string; method: string }>;
  actual: Readonly<{ value: number; unit: string; source: string }>;
  verificationOutcome: string;
  deltaValue: number;
  unit: string;
  status: string;
  attributionClass: string;
  confidenceScore: number;
  economicConversionStatus: string;
  economicValue: number | null;
  intelligenceCost: number | null;
  netValue: number | null;
  roiPercent: number | null;
  evidenceRefs: readonly string[];
  createdAtUtc: string;
}>;

export type RawOperationalValueReportPreview = Readonly<{
  periodLabel: string;
  verifiedInterventions: number;
  goodOutputGainUnits: number;
  downtimeAvoidedMinutes: number;
  lateOrdersProtected: number;
  verifiedEconomicValue: number | null;
  currency: string | null;
  intelligenceCost: number;
  netValue: number | null;
  roiPercent: number | null;
  methodologySummary: string;
}>;

const optional = <T>(value: T | null | undefined): T | undefined => (value === null ? undefined : value);

function economicStatus(
  inputs: RawOperationalValueEconomicInputs | null,
  verifiedEconomicValue: number | null,
): string {
  if (!inputs) {
    return "NOT_CONFIGURED";
  }

  return verifiedEconomicValue !== null ? "CONVERTED" : "OPERATIONAL_ONLY";
}

export function mapEconomicInputs(
  raw: RawOperationalValueEconomicInputs | null,
): OperationalValueEconomicInputSnapshotApi | undefined {
  if (!raw) {
    return undefined;
  }

  return {
    inputVersionId: raw.configurationId,
    versionCode: raw.versionCode,
    currency: raw.currency,
    effectiveFromUtc: raw.effectiveFromUtc,
    // The API returns the active configuration only.
    isActive: true,
    unitContributionValue: optional(raw.unitContributionValue),
    scrapUnitCost: optional(raw.scrapUnitCost),
    downtimeCostPerHour: optional(raw.downtimeCostPerHour),
    laborCostPerHour: optional(raw.laborCostPerHour),
    overtimeCostPerHour: optional(raw.overtimeCostPerHour),
    changeoverCostPerMinute: optional(raw.changeoverCostPerMinute),
    energyUnitCost: optional(raw.energyUnitCost),
    lateDeliveryPenalty: optional(raw.lateDeliveryPenalty),
    updatedAtUtc: raw.updatedAtUtc,
  };
}

export function mapCaseDetail(
  raw: RawOperationalValueCaseDetail,
  context: Readonly<{ valueCategory: string; workflowType: string; lineCode?: string }>,
): OperationalValueCaseDetailApi {
  return {
    caseId: raw.valueRecordId,
    title: context.lineCode ? `${context.workflowType} · ${context.lineCode}` : context.workflowType,
    problemSummary: raw.problemSummary,
    workflowType: context.workflowType,
    lineCode: context.lineCode,
    baselineValue: raw.baseline.value,
    baselineMethod: raw.baseline.method,
    baselineSource: raw.baseline.source,
    actualValue: raw.actual.value,
    deltaValue: raw.deltaValue,
    unit: raw.unit,
    valueCategory: context.valueCategory,
    status: raw.status,
    attributionClass: raw.attributionClass,
    confidence: raw.confidenceScore,
    economicValue: optional(raw.economicValue),
    economicConversionStatus: raw.economicConversionStatus,
    intelligenceCost: raw.intelligenceCost ?? 0,
    netValue: optional(raw.netValue),
    roiPercent: optional(raw.roiPercent),
    evidence: raw.evidenceRefs.map((reference) => ({ kind: "reference", label: reference, detail: reference })),
    verificationOutcome: raw.verificationOutcome,
    verifiedAtUtc: raw.createdAtUtc,
  };
}

function mapBreakdown(
  items: RawOperationalValueControlCenter["breakdown"],
  dimensionType: string,
): OperationalValueBreakdownItemApi[] {
  return items
    .filter((item) => item.dimensionType.toLowerCase() === dimensionType)
    .map((item) => ({
      dimensionType: item.dimensionType,
      dimensionId: item.dimensionKey,
      label: item.label,
      valueCategory: dimensionType === "category" ? item.dimensionKey : undefined,
      operationalValue: item.operationalDeltaTotal,
      unit: item.unit,
      caseCount: item.verifiedCount,
      economicValue: optional(item.economicValueTotal),
    }));
}

function mapTrend(items: RawOperationalValueControlCenter["trend"]): OperationalValueTrendPointApi[] {
  return items.map((point) => ({
    periodLabel: point.periodLabel,
    verifiedOutcomes: point.verifiedOutcomeCount,
    goodOutputGainedUnits: point.goodOutputGainUnits,
    downtimeAvoidedMinutes: point.downtimeAvoidedMinutes,
    verifiedEconomicValue: optional(point.verifiedEconomicValue),
    intelligenceCost: point.intelligenceCost,
    costPerVerifiedOutcome: optional(point.costPerVerifiedOutcome),
  }));
}

export function mapReportPreview(
  raw: RawOperationalValueReportPreview,
  inputs: RawOperationalValueEconomicInputs | null,
): OperationalValueReportPreviewApi {
  const improvements: string[] = [];
  if (raw.goodOutputGainUnits > 0) improvements.push(`${raw.goodOutputGainUnits} adet iyi çıktı kazanımı`);
  if (raw.downtimeAvoidedMinutes > 0) improvements.push(`${raw.downtimeAvoidedMinutes} dk önlenen duruş`);
  if (raw.lateOrdersProtected > 0) improvements.push(`${raw.lateOrdersProtected} geciken sipariş korundu`);

  return {
    periodLabel: raw.periodLabel,
    verifiedOutcomes: raw.verifiedInterventions,
    operationalImprovements: improvements,
    verifiedEconomicValue: optional(raw.verifiedEconomicValue),
    intelligenceCost: raw.intelligenceCost,
    netValue: optional(raw.netValue),
    roiPercent: optional(raw.roiPercent),
    methodologySummary: raw.methodologySummary,
    currency: optional(raw.currency) ?? inputs?.currency,
    economicConversionStatus: economicStatus(inputs, raw.verifiedEconomicValue),
  };
}

export function mapControlCenter(
  raw: RawOperationalValueControlCenter,
  report: RawOperationalValueReportPreview,
  featuredCase: OperationalValueCaseDetailApi | undefined,
  capabilities: Readonly<{ canManageEconomicInputs: boolean; canExport: boolean }>,
): OperationalValueControlCenterApi {
  const inputs = raw.economicInputs;
  const summary = raw.summary;

  return {
    summary: {
      verifiedOutcomes: summary.verifiedOutcomes,
      goodOutputGainedUnits: summary.goodOutputGainUnits,
      downtimeAvoidedMinutes: summary.downtimeAvoidedMinutes,
      lateOrdersProtected: summary.lateOrdersProtected,
      humanTouchesReduced: summary.humanTouchesReduced,
      intelligenceCost: summary.intelligenceCost,
      verifiedEconomicValue: optional(summary.verifiedEconomicValue),
      economicConversionStatus: economicStatus(inputs, summary.verifiedEconomicValue),
      currency: inputs?.currency,
    },
    trend: mapTrend(raw.trend),
    breakdownByCategory: mapBreakdown(raw.breakdown, "category"),
    breakdownByLine: mapBreakdown(raw.breakdown, "line"),
    breakdownByWorkflow: mapBreakdown(raw.breakdown, "workflow"),
    featuredCase,
    recentCases: featuredCase ? [featuredCase] : [],
    activeEconomicInputs: mapEconomicInputs(inputs),
    intelligenceCost: {
      totalIntelligenceCost: summary.intelligenceCost,
      costPerVerifiedOutcome: optional(summary.costPerVerifiedOutcome),
    },
    reportPreview: mapReportPreview(report, inputs),
    canManageEconomicInputs: capabilities.canManageEconomicInputs,
    canExport: capabilities.canExport,
    generatedAtUtc: raw.generatedAtUtc,
  };
}
