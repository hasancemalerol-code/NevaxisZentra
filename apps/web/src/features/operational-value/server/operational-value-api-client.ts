import "./operational-value-server-boundary";

import type { ProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

export type OperationalValueSummaryMetricsApi = Readonly<{
  verifiedOutcomes: number;
  goodOutputGainedUnits: number;
  downtimeAvoidedMinutes: number;
  lateOrdersProtected: number;
  humanTouchesReduced: number;
  intelligenceCost: number;
  verifiedEconomicValue?: number;
  economicConversionStatus: string;
  currency?: string;
}>;

export type OperationalValueTrendPointApi = Readonly<{
  periodLabel: string;
  periodStartUtc: string;
  verifiedOutcomes: number;
  goodOutputGainedUnits: number;
  downtimeAvoidedMinutes: number;
  verifiedEconomicValue?: number;
  intelligenceCost: number;
  costPerVerifiedOutcome?: number;
}>;

export type OperationalValueBreakdownItemApi = Readonly<{
  dimensionType: string;
  dimensionId: string;
  label: string;
  valueCategory: string;
  operationalValue: number;
  unit: string;
  caseCount: number;
  economicValue?: number;
  attributionClass: string;
}>;

export type OperationalValueEvidenceItemApi = Readonly<{
  kind: string;
  label: string;
  detail: string;
  occurredAtUtc?: string;
}>;

export type OperationalValueCaseDetailApi = Readonly<{
  caseId: string;
  title: string;
  problemSummary: string;
  workflowType: string;
  lineCode?: string;
  baselineValue: number;
  baselineMethod: string;
  baselineSource: string;
  actualValue: number;
  deltaValue: number;
  unit: string;
  valueCategory: string;
  status: string;
  attributionClass: string;
  confidence: number;
  economicValue?: number;
  economicConversionStatus: string;
  intelligenceCost: number;
  netValue?: number;
  roiPercent?: number;
  paybackStatus?: string;
  evidence: readonly OperationalValueEvidenceItemApi[];
  verificationOutcome: string;
  verifiedAtUtc?: string;
}>;

export type OperationalValueEconomicInputSnapshotApi = Readonly<{
  inputVersionId: string;
  versionCode: string;
  currency: string;
  effectiveFromUtc: string;
  effectiveToUtc?: string;
  isActive: boolean;
  unitContributionValue?: number;
  scrapUnitCost?: number;
  downtimeCostPerHour?: number;
  laborCostPerHour?: number;
  overtimeCostPerHour?: number;
  changeoverCostPerMinute?: number;
  energyUnitCost?: number;
  lateDeliveryPenalty?: number;
  updatedAtUtc: string;
}>;

export type OperationalValueIntelligenceCostLinkApi = Readonly<{
  totalIntelligenceCost: number;
  costPerVerifiedOutcome?: number;
  modelCallCount: number;
  deterministicRouteCount: number;
  deterministicRouteRate: number;
  frontierModelCallRate: number;
  frontierCostPerVerifiedOutcome?: number;
  hasValidCounterfactual: boolean;
  message: string;
}>;

export type OperationalValueReportPreviewApi = Readonly<{
  periodLabel: string;
  periodStartUtc: string;
  periodEndUtc: string;
  verifiedOutcomes: number;
  operationalImprovements: readonly string[];
  verifiedEconomicValue?: number;
  intelligenceCost: number;
  netValue?: number;
  roiPercent?: number;
  paybackStatus: string;
  methodologySummary: string;
  currency?: string;
  economicConversionStatus: string;
}>;

export type OperationalValueControlCenterApi = Readonly<{
  summary: OperationalValueSummaryMetricsApi;
  trend: readonly OperationalValueTrendPointApi[];
  breakdownByCategory: readonly OperationalValueBreakdownItemApi[];
  breakdownByLine: readonly OperationalValueBreakdownItemApi[];
  breakdownByWorkflow: readonly OperationalValueBreakdownItemApi[];
  featuredCase?: OperationalValueCaseDetailApi;
  recentCases: readonly OperationalValueCaseDetailApi[];
  activeEconomicInputs?: OperationalValueEconomicInputSnapshotApi;
  intelligenceCost: OperationalValueIntelligenceCostLinkApi;
  reportPreview: OperationalValueReportPreviewApi;
  canManageEconomicInputs: boolean;
  canExport: boolean;
  generatedAtUtc: string;
}>;

async function operationalValueFetch<T>(
  context: ProductionOrderRequestContext,
  path: string,
  init?: RequestInit,
): Promise<{ status: "success"; value: T } | { status: "failure"; code: string; message: string }> {
  const url = new URL(`${context.apiBaseUrl}${path}`);
  url.searchParams.set("facilityId", context.facilityId);

  const response = await fetch(url, {
    ...init,
    headers: {
      Accept: "application/json",
      "Content-Type": "application/json",
      ...context.requestHeaders,
      ...(init?.headers ?? {}),
    },
    cache: "no-store",
  });

  if (!response.ok) {
    const body = (await response.json().catch(() => ({}))) as { message?: string; code?: string };
    return {
      status: "failure",
      code: body.code ?? `http-${response.status}`,
      message: body.message ?? `Operational value API failed (${response.status})`,
    };
  }

  return { status: "success", value: (await response.json()) as T };
}

export async function readOperationalValueControlCenter(
  context: ProductionOrderRequestContext,
): Promise<
  { status: "success"; value: OperationalValueControlCenterApi }
  | { status: "failure"; code: string; message: string }
> {
  return operationalValueFetch<OperationalValueControlCenterApi>(
    context,
    "/api/operational-value/control-center",
  );
}

export async function readOperationalValueCaseDetail(
  context: ProductionOrderRequestContext,
  caseId: string,
): Promise<
  { status: "success"; value: OperationalValueCaseDetailApi }
  | { status: "failure"; code: string; message: string }
> {
  return operationalValueFetch<OperationalValueCaseDetailApi>(
    context,
    `/api/operational-value/cases/${encodeURIComponent(caseId)}`,
  );
}

export async function readOperationalValueEconomicInputs(
  context: ProductionOrderRequestContext,
): Promise<
  { status: "success"; value: OperationalValueEconomicInputSnapshotApi }
  | { status: "failure"; code: string; message: string }
> {
  return operationalValueFetch<OperationalValueEconomicInputSnapshotApi>(
    context,
    "/api/operational-value/economic-inputs",
  );
}

export async function readOperationalValueReportPreview(
  context: ProductionOrderRequestContext,
  period?: string,
): Promise<
  { status: "success"; value: OperationalValueReportPreviewApi }
  | { status: "failure"; code: string; message: string }
> {
  const path = period
    ? `/api/operational-value/report-preview?period=${encodeURIComponent(period)}`
    : "/api/operational-value/report-preview";
  return operationalValueFetch<OperationalValueReportPreviewApi>(context, path);
}
