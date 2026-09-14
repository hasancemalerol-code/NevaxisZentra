import "./golden-demo-customs-file-server-boundary";

import type { ProductionOrderRequestContext } from "@/features/production-execution/server/production-order-request-context";

export type GoldenDemoCustomsFileWorkspaceApi = Readonly<{
  outcomeId: string;
  workerCode: string;
  workerLabelEn: string;
  workerLabelTr: string;
  assignedOutcomeText: string;
  status: string;
  statusLabel: string;
  orderNumber: string;
  customerName: string;
  destinationCountry: string;
  deliveryAddress: string;
  currency: string;
  incoterm: string;
  packageCount: number;
  shipmentNetWeightKg: number;
  shipmentGrossWeightKg: number;
  shipmentReference: string;
  productLines: readonly Readonly<{
    productCode: string;
    description: string;
    quantity: number;
    unitPrice: number;
    currency: string;
    gtipCode?: string;
    originCountry: string;
  }>[];
  documents: readonly Readonly<{
    id: string;
    documentType: string;
    versionNumber: number;
    isCurrent: boolean;
    status: string;
    content: Readonly<{
      orderReference: string;
      shipmentReference: string;
      packageCount: number;
      netWeightKg: number;
      grossWeightKg: number;
      currency: string;
      incoterm: string;
      destinationCountry: string;
      customerName: string;
    }>;
    createdAtUtc: string;
  }>[];
  requirements: readonly Readonly<{
    requirementCode: string;
    label: string;
    sourceRule: string;
    required: boolean;
    status: string;
    classification: string;
    blocking: boolean;
    explanation: string;
  }>[];
  consistencyChecks: readonly Readonly<{
    checkCode: string;
    label: string;
    status: string;
    explanation: string;
    classification: string;
  }>[];
  blockers: readonly Readonly<{
    id: string;
    blockerCode: string;
    status: string;
    reason: string;
  }>[];
  correctiveWorks: readonly Readonly<{
    id: string;
    title: string;
    status: string;
    responsibleRole: string;
  }>[];
  activityTrace: readonly Readonly<{
    stageCode: string;
    stageLabel: string;
    actor: string;
    action: string;
    status: string;
    evidenceReference: string;
    occurredAtUtc: string;
  }>[];
  approval: Readonly<{
    isApprovalRequired: boolean;
    isApproved: boolean;
    approvingRole: string;
    approvedBySubjectId?: string;
    approvedAtUtc?: string;
  }>;
  package: Readonly<{
    id: string;
    packageVersion: number;
    documentManifest: readonly string[];
    artifactLabel: string;
    assembledAtUtc: string;
  }> | null;
  verification: Readonly<{
    result: string;
    resultLabel: string;
    summary: string;
  }>;
  hero: Readonly<{
    showSuccessHero: boolean;
    headline: string;
    subheadline: string;
    verificationResult: string;
    documentsReadyCount: number;
    documentsTotalCount: number;
    blockingIssueCount: number;
    consistencyStatus: string;
    approvalStatus: string;
    brokerHandoffStatus: string;
    packageId?: string;
    packageVersion?: number;
  }>;
}>;

type FetchResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | Readonly<{ status: "failure"; code: string; message: string }>;

async function customsFetch<T>(
  context: ProductionOrderRequestContext,
  path: string,
  init?: RequestInit,
): Promise<FetchResult<T>> {
  const url = `${context.apiBaseUrl}${path}${path.includes("?") ? "&" : "?"}facilityId=${context.facilityId}`;
  const response = await fetch(url, {
    ...init,
    headers: {
      Accept: "application/json",
      "Content-Type": "application/json",
      ...context.requestHeaders,
      ...init?.headers,
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

export async function getGoldenDemoCustomsFileWorkspace(context: ProductionOrderRequestContext) {
  return customsFetch<GoldenDemoCustomsFileWorkspaceApi>(context, "/api/golden-demo-customs-file/workspace");
}

export async function assignGoldenDemoCustomsOutcome(context: ProductionOrderRequestContext, outcomeId: string) {
  return customsFetch<GoldenDemoCustomsFileWorkspaceApi>(
    context,
    `/api/golden-demo-customs-file/outcomes/${outcomeId}/assign`,
    { method: "POST", body: JSON.stringify({}) },
  );
}

export async function evaluateGoldenDemoCustomsWorker(context: ProductionOrderRequestContext, outcomeId: string) {
  return customsFetch<GoldenDemoCustomsFileWorkspaceApi>(
    context,
    `/api/golden-demo-customs-file/outcomes/${outcomeId}/evaluate`,
    { method: "POST" },
  );
}

export async function completeGoldenDemoCustomsCorrectiveWork(
  context: ProductionOrderRequestContext,
  outcomeId: string,
  workId: string,
) {
  return customsFetch<GoldenDemoCustomsFileWorkspaceApi>(
    context,
    `/api/golden-demo-customs-file/outcomes/${outcomeId}/corrective-work/${workId}/complete`,
    { method: "POST", body: JSON.stringify({ idempotencyKey: crypto.randomUUID() }) },
  );
}

export async function recheckGoldenDemoCustomsEvidence(context: ProductionOrderRequestContext, outcomeId: string) {
  return customsFetch<GoldenDemoCustomsFileWorkspaceApi>(
    context,
    `/api/golden-demo-customs-file/outcomes/${outcomeId}/recheck`,
    { method: "POST" },
  );
}

export async function approveGoldenDemoCustomsPackage(context: ProductionOrderRequestContext, outcomeId: string) {
  return customsFetch<GoldenDemoCustomsFileWorkspaceApi>(
    context,
    `/api/golden-demo-customs-file/outcomes/${outcomeId}/approve`,
    { method: "POST", body: JSON.stringify({}) },
  );
}

export async function assembleGoldenDemoCustomsPackage(context: ProductionOrderRequestContext, outcomeId: string) {
  return customsFetch<GoldenDemoCustomsFileWorkspaceApi>(
    context,
    `/api/golden-demo-customs-file/outcomes/${outcomeId}/assemble`,
    { method: "POST" },
  );
}

export async function verifyGoldenDemoCustomsOutcome(context: ProductionOrderRequestContext, outcomeId: string) {
  return customsFetch<GoldenDemoCustomsFileWorkspaceApi>(
    context,
    `/api/golden-demo-customs-file/outcomes/${outcomeId}/verify`,
    { method: "POST", body: JSON.stringify({ idempotencyKey: crypto.randomUUID() }) },
  );
}

export async function resetGoldenDemoCustomsOutcome(context: ProductionOrderRequestContext, outcomeId: string) {
  return customsFetch<{ resetStatus: string }>(
    context,
    `/api/golden-demo-customs-file/outcomes/${outcomeId}/reset`,
    { method: "POST" },
  );
}
