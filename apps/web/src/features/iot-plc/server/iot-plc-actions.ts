"use server";

import "./iot-plc-server-boundary";

import { revalidatePath } from "next/cache";

import {
  createIotPlcEndpoint,
  createIotPlcProfile,
  createIotPlcSignal,
  evaluateIotPlcConnectivity,
  evaluateIotPlcSignalQuality,
  updateIotPlcEndpoint,
  updateIotPlcProfile,
  updateIotPlcSignal,
} from "./iot-plc-api-client";
import {
  readIotPlcAssessmentDetail,
  readIotPlcEndpointDetail,
  readIotPlcSignalDetail,
  readIotPlcWorkspace,
} from "./iot-plc-queries";
import { denyUnlessGranted } from "../../admin-access/server/require-current-permission";
import { resolveIotPlcRequestContext } from "./iot-plc-request-context";

type ActionFailure = Readonly<{ status: "failure"; message: string }>;

export async function refreshIotPlcWorkspaceAction() {
  const authorization = await denyUnlessGranted("iot-plc.read");
  if (authorization.status === "failure") return authorization;

  revalidatePath("/iot-plc");
  const result = await readIotPlcWorkspace();
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message };
  }

  return {
    status: "success" as const,
    workspace: result.workspace,
    canManage: result.canManage,
  };
}

export async function loadIotPlcEndpointDetailAction(reference: string) {
  const authorization = await denyUnlessGranted("iot-plc.read");
  if (authorization.status === "failure") return authorization;

  const result = await readIotPlcEndpointDetail(reference);
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message };
  }

  return { status: "success" as const, detail: result.detail };
}

export async function loadIotPlcSignalDetailAction(reference: string) {
  const authorization = await denyUnlessGranted("iot-plc.read");
  if (authorization.status === "failure") return authorization;

  const result = await readIotPlcSignalDetail(reference);
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message };
  }

  return { status: "success" as const, detail: result.detail };
}

export async function loadIotPlcAssessmentDetailAction(
  assessmentId: string,
  kind: "connectivity" | "signal",
) {
  const authorization = await denyUnlessGranted("iot-plc.read");
  if (authorization.status === "failure") return authorization;

  const result = await readIotPlcAssessmentDetail(assessmentId, kind);
  if (result.status === "failure") {
    return { status: "failure" as const, message: result.message };
  }

  return { status: "success" as const, detail: result.detail };
}

async function resolveManageContext(): Promise<
  | Readonly<{ status: "success"; context: import("../../production-execution/server/production-order-request-context").ProductionOrderRequestContext }>
  | ActionFailure
> {
  const authorization = await denyUnlessGranted("iot-plc.manage");
  if (authorization.status === "failure") return authorization;

  const contextResult = await resolveIotPlcRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message };
  }

  return { status: "success", context: contextResult.context };
}

export async function createIotPlcEndpointAction(input: {
  endpointCode: string;
  displayName: string;
  protocolType: string;
  connectionDirection: string;
  endpointStatus: string;
  configurationProvenance: string;
  connectionProfileJson: string;
}) {
  const resolved = await resolveManageContext();
  if (resolved.status === "failure") return resolved;

  const result = await createIotPlcEndpoint(resolved.context, {
    endpointCode: input.endpointCode,
    displayName: input.displayName,
    protocolType: input.protocolType,
    connectionDirection: input.connectionDirection,
    endpointStatus: input.endpointStatus,
    configurationProvenance: input.configurationProvenance,
    externalSecretReference: null,
    connectionProfileJson: input.connectionProfileJson,
  });

  if (result.status === "failure") return { status: "failure" as const, message: result.message };
  revalidatePath("/iot-plc");
  return { status: "success" as const, id: result.value.id, version: result.value.version };
}

export async function updateIotPlcEndpointAction(
  reference: string,
  input: {
    endpointCode: string;
    displayName: string;
    protocolType: string;
    connectionDirection: string;
    endpointStatus: string;
    configurationProvenance: string;
    connectionProfileJson: string;
    expectedVersion: number;
  },
) {
  const resolved = await resolveManageContext();
  if (resolved.status === "failure") return resolved;

  const result = await updateIotPlcEndpoint(resolved.context, reference, {
    ...input,
    externalSecretReference: null,
  });

  if (result.status === "failure") return { status: "failure" as const, message: result.message };
  revalidatePath("/iot-plc");
  return { status: "success" as const, id: result.value.id, version: result.value.version };
}

export async function createIotPlcSignalAction(input: {
  endpointReference: string;
  signalCode: string;
  displayName: string;
  sourceAddress: string;
  dataType: string;
  engineeringUnit: string | null;
  semanticType: string;
  mappedAssetType: string;
  mappedCanonicalAssetId: string | null;
  isEnabled: boolean;
  samplingExpectation: string | null;
  sourceProvenance: string;
}) {
  const resolved = await resolveManageContext();
  if (resolved.status === "failure") return resolved;

  const result = await createIotPlcSignal(resolved.context, input);
  if (result.status === "failure") return { status: "failure" as const, message: result.message };
  revalidatePath("/iot-plc");
  return { status: "success" as const, id: result.value.id, version: result.value.version };
}

export async function updateIotPlcSignalAction(
  reference: string,
  input: {
    displayName: string;
    sourceAddress: string;
    dataType: string;
    engineeringUnit: string | null;
    semanticType: string;
    mappedAssetType: string;
    mappedCanonicalAssetId: string | null;
    isEnabled: boolean;
    samplingExpectation: string | null;
    sourceProvenance: string;
    expectedVersion: number;
  },
) {
  const resolved = await resolveManageContext();
  if (resolved.status === "failure") return resolved;

  const result = await updateIotPlcSignal(resolved.context, reference, input);
  if (result.status === "failure") return { status: "failure" as const, message: result.message };
  revalidatePath("/iot-plc");
  return { status: "success" as const, id: result.value.id, version: result.value.version };
}

export async function updateIotPlcProfileAction(
  reference: string,
  input: {
    displayName: string;
    expectedObservationIntervalSeconds: number;
    freshnessToleranceSeconds: number;
    staleThresholdSeconds: number;
    maximumClockSkewToleranceSeconds: number;
    minimumExpectedObservationCount: number;
    minimumGoodQualityRatio: number;
    maximumBadQualityRatio: number;
    maximumUncertainQualityRatio: number;
    maximumSourceConflictRatio: number;
    sequenceGapEvaluationEnabled: boolean;
    status: string;
    sourceProvenance: string;
    expectedVersion: number;
  },
) {
  const resolved = await resolveManageContext();
  if (resolved.status === "failure") return resolved;

  const result = await updateIotPlcProfile(resolved.context, reference, input);
  if (result.status === "failure") return { status: "failure" as const, message: result.message };
  revalidatePath("/iot-plc");
  return { status: "success" as const, id: result.value.id, version: result.value.version };
}

export async function createIotPlcProfileAction(input: {
  profileCode: string;
  displayName: string;
  expectedObservationIntervalSeconds: number;
  freshnessToleranceSeconds: number;
  staleThresholdSeconds: number;
  maximumClockSkewToleranceSeconds: number;
  minimumExpectedObservationCount: number;
  minimumGoodQualityRatio: number;
  maximumBadQualityRatio: number;
  maximumUncertainQualityRatio: number;
  maximumSourceConflictRatio: number;
  sequenceGapEvaluationEnabled: boolean;
  status: string;
  sourceProvenance: string;
}) {
  const resolved = await resolveManageContext();
  if (resolved.status === "failure") return resolved;

  const result = await createIotPlcProfile(resolved.context, input);
  if (result.status === "failure") return { status: "failure" as const, message: result.message };
  revalidatePath("/iot-plc");
  return { status: "success" as const, id: result.value.id, version: result.value.version };
}

export async function evaluateIotPlcConnectivityAction(input: {
  endpointReference: string;
  evaluationWindowStartUtc: string;
  evaluationWindowEndUtc: string;
  evaluationTimeUtc: string;
  profileReference: string;
  evidenceMode: string;
  sourceProvenance: string;
  idempotencyKey: string;
}) {
  const resolved = await resolveManageContext();
  if (resolved.status === "failure") return resolved;

  const result = await evaluateIotPlcConnectivity(resolved.context, input);
  if (result.status === "failure") return { status: "failure" as const, message: result.message };
  revalidatePath("/iot-plc");
  return { status: "success" as const, id: result.value.id, version: result.value.version };
}

export async function evaluateIotPlcSignalQualityAction(input: {
  signalReference: string;
  evaluationWindowStartUtc: string;
  evaluationWindowEndUtc: string;
  evaluationTimeUtc: string;
  profileReference: string;
  evidenceMode: string;
  sourceProvenance: string;
  idempotencyKey: string;
}) {
  const resolved = await resolveManageContext();
  if (resolved.status === "failure") return resolved;

  const result = await evaluateIotPlcSignalQuality(resolved.context, input);
  if (result.status === "failure") return { status: "failure" as const, message: result.message };
  revalidatePath("/iot-plc");
  return { status: "success" as const, id: result.value.id, version: result.value.version };
}
