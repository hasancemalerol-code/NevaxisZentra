import "./golden-demo-p3-server-boundary";

import type { ProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

export type GoldenDemoP3OutcomeApi = Readonly<{
  productionEventId: string;
  machineCode: string;
  interventionStatus: string;
  outcomeStatus: string;
  verificationConfidence: string;
  validationWindow: Readonly<{
    status: string;
    statusLabel: string;
    validationStartedAtUtc?: string | null;
    samplesCollected?: number | null;
    minimumSamplesRequired?: number | null;
    evidenceCompleteness: string;
    summary: string;
    readyForVerification: boolean;
  }>;
  baselineWindow: Readonly<{
    windowKind: string;
    windowStartUtc: string;
    windowEndUtc: string;
    goodParts: number;
    rejectParts: number;
    sourceLabel: string;
    productionContext: string;
    cycleSeconds?: number | null;
    goodPartsPerHour?: number | null;
    availabilityPercent?: number | null;
    microStopCount: number;
    kwhPerGoodPart?: number | null;
  }>;
  validationMeasurementWindow?: Readonly<{
    windowKind: string;
    windowStartUtc: string;
    windowEndUtc: string;
    goodParts: number;
    rejectParts: number;
    sourceLabel: string;
    productionContext: string;
    cycleSeconds?: number | null;
    goodPartsPerHour?: number | null;
    availabilityPercent?: number | null;
    microStopCount: number;
    kwhPerGoodPart?: number | null;
  }> | null;
  hero: Readonly<{
    headline: string;
    subheadline: string;
    recoveredGoodPartsPerShift: number;
    recoveredGoodPartsClassification: string;
    verificationResult: string;
    showSuccessHero: boolean;
    highlights: readonly Readonly<{
      label: string;
      beforeValue: string;
      afterValue: string;
      deltaLabel: string;
      status: string;
    }>[];
  }>;
  metricComparisons: readonly Readonly<{
    metricCode: string;
    label: string;
    beforeValue?: string | null;
    afterValue?: string | null;
    deltaLabel?: string | null;
    status: string;
    classification: string;
  }>[];
  verificationChecks: readonly Readonly<{
    code: string;
    label: string;
    passed: boolean;
    measuredValue?: string | null;
    referenceValue?: string | null;
    status: string;
    explanation: string;
    classification: string;
  }>[];
  insufficientEvidenceReasons: readonly string[];
  energyImpact: Readonly<{
    hasData: boolean;
    classification: string;
    beforeKwhPerGoodPart?: number | null;
    afterKwhPerGoodPart?: number | null;
    changePercent?: number | null;
    summary: string;
    relationshipNote: string;
  }>;
  businessImpact: Readonly<{
    operationalGoodPartsPerShift: number;
    operationalClassification: string;
    capacityImpactPercent: number;
    estimatedBusinessImpactTry?: number | null;
    estimatedBusinessClassification: string;
    modeledVerifiedBusinessImpactTry?: number | null;
    modeledVerifiedBusinessClassification: string;
    categories: readonly Readonly<{
      categoryCode: string;
      label: string;
      beforeLabel: string;
      afterLabel: string;
      protectedOrRecoveredLabel: string;
      classification: string;
    }>[];
  }>;
  traceability: Readonly<{
    planReferenceId?: string | null;
    verificationReferenceId?: string | null;
    interventionReferenceId?: string | null;
    sourceAgentReferences: readonly string[];
    workflowSummary: string;
  }>;
}>;

type FetchResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | Readonly<{ status: "failure"; code: string; message: string }>;

async function goldenDemoP3Fetch<T>(
  context: ProductionOrderRequestContext,
  path: string,
): Promise<FetchResult<T>> {
  const url = `${context.apiBaseUrl}${path}${path.includes("?") ? "&" : "?"}facilityId=${context.facilityId}`;
  const response = await fetch(url, {
    headers: {
      Accept: "application/json",
      ...context.requestHeaders,
    },
    cache: "no-store",
  });

  const text = await response.text();
  let json: unknown;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    return { status: "failure", code: "invalid-json", message: "API yanıtı okunamadı." };
  }

  if (!response.ok) {
    const message =
      typeof json === "object" && json !== null && "message" in json
        ? String((json as { message: string }).message)
        : `HTTP ${response.status}`;
    return { status: "failure", code: `http-${response.status}`, message };
  }

  return { status: "success", value: json as T };
}

export async function getGoldenDemoP3Outcome(context: ProductionOrderRequestContext) {
  return goldenDemoP3Fetch<GoldenDemoP3OutcomeApi>(context, "/api/golden-demo-p3/outcome");
}
