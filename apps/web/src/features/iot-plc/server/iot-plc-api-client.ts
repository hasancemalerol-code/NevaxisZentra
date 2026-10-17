import { stripFixtureRecords } from "@/lib/runtime/fixture-data";
import {
  classifyFailedResponse,
  SESSION_REQUIRED_MESSAGE,
  SUBSCRIPTION_REQUIRED_MESSAGE,
  type ClassifiedApiFailure,
} from "@/lib/api/api-failure-classifier";
import "./iot-plc-server-boundary";

import type { ProductionOrderRequestContext } from "../../production-execution/server/production-order-request-context";
import type { IotPlcWriteResult } from "../domain/iot-plc-types";

export type IotPlcApiFailure = Readonly<{
  status: "failure";
  code:
    | "source-unavailable"
    | "unauthenticated"
    | "permission-denied"
    | "subscription-required"
    | "configuration-error"
    | "invalid-request"
    | "invalid-response"
    | "concurrency-conflict"
    | "not-found";
  message: string;
  retryable: boolean;
}>;

export type IotPlcApiResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | IotPlcApiFailure;

export type RawIotPlcWorkspace = Readonly<{
  activeEndpointCount: number;
  activeSignalCount: number;
  recentObservationCount: number;
  provenanceDisclaimer: string;
  hasDevelopmentPilotData: boolean;
  capabilities: Readonly<{ canRead: boolean; canManage: boolean }>;
}>;

export type RawIotPlcIntelligenceWorkspace = Readonly<{
  approvedProfileCount: number;
  recentConnectivityAssessmentCount: number;
  recentSignalAssessmentCount: number;
  ruleSetVersion: string;
  provenanceDisclaimer: string;
  hasDevelopmentPilotData: boolean;
  capabilities: Readonly<{ canRead: boolean; canManage: boolean }>;
}>;

export type RawIotPlcEndpoint = Readonly<{
  id: string;
  endpointCode: string;
  displayName: string;
  protocolType: string;
  connectionDirection: string;
  endpointStatus: string;
  configurationProvenance: string;
  lastObservedConnectionState: string | null;
  lastObservedAtUtc: string | null;
  version: number;
  signalCount: number;
  isPilotData: boolean;
}>;

export type RawIotPlcSignal = Readonly<{
  id: string;
  endpointId: string;
  endpointCode: string;
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
  version: number;
  isPilotData: boolean;
}>;

export type RawIotPlcObservation = Readonly<{
  observedAtUtc: string;
  receivedAtUtc: string;
  value: unknown;
  qualityState: string;
  evidenceState: string;
  sourceSequenceReference: string | null;
  sourceProvenance: string;
}>;

export type RawIotPlcHealth = Readonly<{
  observedAtUtc: string;
  receivedAtUtc: string;
  healthState: string;
  source: string;
  evidenceState: string;
  reasonCode: string;
  latencyMs: number | null;
  lastSuccessfulObservationAtUtc: string | null;
}>;

export type RawIotPlcProfile = Readonly<{
  id: string;
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
  version: number;
  isPilotData: boolean;
}>;

export type RawIotPlcConnectivityAssessment = Readonly<{
  id: string;
  targetType: string;
  targetId: string;
  evaluationWindowStartUtc: string;
  evaluationWindowEndUtc: string;
  qualityProfileId: string;
  evidenceMode: string;
  outcome: string;
  leadingDriverCode: string | null;
  latestHealthState: string | null;
  healthObservationAvailabilityRatio: number | null;
  freshnessSeconds: number | null;
  evaluatedAtUtc: string;
  sourceProvenance: string;
  isPilotData: boolean;
}>;

export type RawIotPlcSignalAssessment = Readonly<{
  id: string;
  targetType: string;
  targetId: string;
  signalDefinitionId: string;
  evaluationWindowStartUtc: string;
  evaluationWindowEndUtc: string;
  qualityProfileId: string;
  evidenceMode: string;
  outcome: string;
  leadingDriverCode: string | null;
  actualObservationCount: number;
  expectedObservationCount: number;
  completenessRatio: number | null;
  goodRatio: number | null;
  uncertainRatio: number | null;
  badRatio: number | null;
  staleRatio: number | null;
  sourceConflictRatio: number | null;
  freshnessSeconds: number | null;
  maxClockSkewSeconds: number | null;
  evaluatedAtUtc: string;
  sourceProvenance: string;
  isPilotData: boolean;
}>;

export type RawIotPlcDriver = Readonly<{
  reasonCode: string;
  explanation: string;
  isLeadingDriver: boolean;
  affectedObservationCount: number;
}>;

function readBoolean(value: unknown): boolean {
  return value === true;
}

function buildUrl(
  context: ProductionOrderRequestContext,
  path: string,
  query?: Record<string, string | undefined>,
): string {
  const url = new URL(`${context.apiBaseUrl}${path}`);
  url.searchParams.set("facilityId", context.facilityId);
  if (query) {
    for (const [key, value] of Object.entries(query)) {
      if (value !== undefined && value !== "") {
        url.searchParams.set(key, value);
      }
    }
  }
  return url.toString();
}

function mapFailureStatus(status: number, failure?: ClassifiedApiFailure): IotPlcApiFailure["code"] {
  if (failure?.kind === "subscription-required") return "subscription-required";
  if (failure?.kind === "unauthenticated") return "unauthenticated";
  if (status === 401) return "unauthenticated";
  if (status === 403) return "permission-denied";
  if (status === 404) return "not-found";
  if (status === 409) return "concurrency-conflict";
  return "source-unavailable";
}

async function getJson<T>(
  context: ProductionOrderRequestContext,
  path: string,
  query: Record<string, string | undefined> | undefined,
  parser: (payload: unknown) => T,
): Promise<IotPlcApiResult<T>> {
  try {
    const response = await fetch(buildUrl(context, path, query), {
      headers: context.requestHeaders,
      cache: "no-store",
    });

    if (!response.ok) {
      const failure = await classifyFailedResponse(response);
      return {
        status: "failure",
        code: mapFailureStatus(response.status, failure),
        message: failure.kind === "subscription-required" ? SUBSCRIPTION_REQUIRED_MESSAGE : failure.kind === "unauthenticated" && response.status === 403 ? SESSION_REQUIRED_MESSAGE : `IoT/PLC isteği başarısız (${response.status}).`,
        retryable: response.status >= 500,
      };
    }

    const payload: unknown = await response.json();
    const guarded = stripFixtureRecords(parser(payload));
    if (guarded === null) {
      // A fixture record is not a customer record outside isolated environments.
      return { status: "failure", code: "not-found", message: "Kayıt bulunamadı.", retryable: false } as never;
    }
    return { status: "success", value: guarded };
  } catch {
    return {
      status: "failure",
      code: "source-unavailable",
      message: "IoT/PLC API bağlantısı kurulamadı.",
      retryable: true,
    };
  }
}

async function mutateJson<T>(
  context: ProductionOrderRequestContext,
  method: "POST" | "PUT",
  path: string,
  body: unknown,
  parser: (payload: unknown) => T,
): Promise<IotPlcApiResult<T>> {
  try {
    const response = await fetch(buildUrl(context, path), {
      method,
      headers: {
        ...context.requestHeaders,
        "Content-Type": "application/json",
      },
      body: JSON.stringify(body),
      cache: "no-store",
    });

    if (!response.ok) {
      const failure = await classifyFailedResponse(response);
      let message = `IoT/PLC işlemi başarısız (${response.status}).`;
      try {
        const problem = (await response.json()) as { title?: string; detail?: string };
        if (problem.title) message = problem.title;
        else if (problem.detail) message = problem.detail;
      } catch {
        // ignore
      }

      return {
        status: "failure",
        code: mapFailureStatus(response.status, failure),
        message,
        retryable: response.status >= 500,
      };
    }

    const payload: unknown = await response.json();
    const guarded = stripFixtureRecords(parser(payload));
    if (guarded === null) {
      // A fixture record is not a customer record outside isolated environments.
      return { status: "failure", code: "not-found", message: "Kayıt bulunamadı.", retryable: false } as never;
    }
    return { status: "success", value: guarded };
  } catch {
    return {
      status: "failure",
      code: "source-unavailable",
      message: "IoT/PLC API bağlantısı kurulamadı.",
      retryable: true,
    };
  }
}

function parseCapabilities(value: unknown) {
  const item = value as Record<string, unknown>;
  return {
    canRead: readBoolean(item.canRead),
    canManage: readBoolean(item.canManage),
  };
}

function parseEndpoint(item: Record<string, unknown>): RawIotPlcEndpoint {
  return {
    id: String(item.id ?? ""),
    endpointCode: String(item.endpointCode ?? ""),
    displayName: String(item.displayName ?? ""),
    protocolType: String(item.protocolType ?? ""),
    connectionDirection: String(item.connectionDirection ?? ""),
    endpointStatus: String(item.endpointStatus ?? ""),
    configurationProvenance: String(item.configurationProvenance ?? ""),
    lastObservedConnectionState: item.lastObservedConnectionState
      ? String(item.lastObservedConnectionState)
      : null,
    lastObservedAtUtc: item.lastObservedAtUtc ? String(item.lastObservedAtUtc) : null,
    version: Number(item.version ?? 0),
    signalCount: Number(item.signalCount ?? 0),
    isPilotData: readBoolean(item.isPilotData),
  };
}

function parseSignal(item: Record<string, unknown>): RawIotPlcSignal {
  return {
    id: String(item.id ?? ""),
    endpointId: String(item.endpointId ?? ""),
    endpointCode: String(item.endpointCode ?? ""),
    signalCode: String(item.signalCode ?? ""),
    displayName: String(item.displayName ?? ""),
    sourceAddress: String(item.sourceAddress ?? ""),
    dataType: String(item.dataType ?? ""),
    engineeringUnit: item.engineeringUnit ? String(item.engineeringUnit) : null,
    semanticType: String(item.semanticType ?? ""),
    mappedAssetType: String(item.mappedAssetType ?? ""),
    mappedCanonicalAssetId: item.mappedCanonicalAssetId
      ? String(item.mappedCanonicalAssetId)
      : null,
    isEnabled: readBoolean(item.isEnabled),
    samplingExpectation: item.samplingExpectation ? String(item.samplingExpectation) : null,
    sourceProvenance: String(item.sourceProvenance ?? ""),
    version: Number(item.version ?? 0),
    isPilotData: readBoolean(item.isPilotData),
  };
}

function parseObservation(item: Record<string, unknown>): RawIotPlcObservation {
  return {
    observedAtUtc: String(item.observedAtUtc ?? ""),
    receivedAtUtc: String(item.receivedAtUtc ?? ""),
    value: item.value ?? null,
    qualityState: String(item.qualityState ?? ""),
    evidenceState: String(item.evidenceState ?? ""),
    sourceSequenceReference: item.sourceSequenceReference
      ? String(item.sourceSequenceReference)
      : null,
    sourceProvenance: String(item.sourceProvenance ?? ""),
  };
}

function parseHealth(item: Record<string, unknown>): RawIotPlcHealth {
  return {
    observedAtUtc: String(item.observedAtUtc ?? ""),
    receivedAtUtc: String(item.receivedAtUtc ?? ""),
    healthState: String(item.healthState ?? ""),
    source: String(item.source ?? ""),
    evidenceState: String(item.evidenceState ?? ""),
    reasonCode: String(item.reasonCode ?? ""),
    latencyMs: item.latencyMs === null || item.latencyMs === undefined
      ? null
      : Number(item.latencyMs),
    lastSuccessfulObservationAtUtc: item.lastSuccessfulObservationAtUtc
      ? String(item.lastSuccessfulObservationAtUtc)
      : null,
  };
}

function parseProfile(item: Record<string, unknown>): RawIotPlcProfile {
  return {
    id: String(item.id ?? ""),
    profileCode: String(item.profileCode ?? ""),
    displayName: String(item.displayName ?? ""),
    expectedObservationIntervalSeconds: Number(item.expectedObservationIntervalSeconds ?? 0),
    freshnessToleranceSeconds: Number(item.freshnessToleranceSeconds ?? 0),
    staleThresholdSeconds: Number(item.staleThresholdSeconds ?? 0),
    maximumClockSkewToleranceSeconds: Number(item.maximumClockSkewToleranceSeconds ?? 0),
    minimumExpectedObservationCount: Number(item.minimumExpectedObservationCount ?? 0),
    minimumGoodQualityRatio: Number(item.minimumGoodQualityRatio ?? 0),
    maximumBadQualityRatio: Number(item.maximumBadQualityRatio ?? 0),
    maximumUncertainQualityRatio: Number(item.maximumUncertainQualityRatio ?? 0),
    maximumSourceConflictRatio: Number(item.maximumSourceConflictRatio ?? 0),
    sequenceGapEvaluationEnabled: readBoolean(item.sequenceGapEvaluationEnabled),
    status: String(item.status ?? ""),
    sourceProvenance: String(item.sourceProvenance ?? ""),
    version: Number(item.version ?? 0),
    isPilotData: readBoolean(item.isPilotData),
  };
}

function parseConnectivityAssessment(item: Record<string, unknown>): RawIotPlcConnectivityAssessment {
  return {
    id: String(item.id ?? ""),
    targetType: String(item.targetType ?? ""),
    targetId: String(item.targetId ?? ""),
    evaluationWindowStartUtc: String(item.evaluationWindowStartUtc ?? ""),
    evaluationWindowEndUtc: String(item.evaluationWindowEndUtc ?? ""),
    qualityProfileId: String(item.qualityProfileId ?? ""),
    evidenceMode: String(item.evidenceMode ?? ""),
    outcome: String(item.outcome ?? ""),
    leadingDriverCode: item.leadingDriverCode ? String(item.leadingDriverCode) : null,
    latestHealthState: item.latestHealthState ? String(item.latestHealthState) : null,
    healthObservationAvailabilityRatio: item.healthObservationAvailabilityRatio === null
      || item.healthObservationAvailabilityRatio === undefined
      ? null
      : Number(item.healthObservationAvailabilityRatio),
    freshnessSeconds: item.freshnessSeconds === null || item.freshnessSeconds === undefined
      ? null
      : Number(item.freshnessSeconds),
    evaluatedAtUtc: String(item.evaluatedAtUtc ?? ""),
    sourceProvenance: String(item.sourceProvenance ?? ""),
    isPilotData: readBoolean(item.isPilotData),
  };
}

function parseSignalAssessment(item: Record<string, unknown>): RawIotPlcSignalAssessment {
  return {
    id: String(item.id ?? ""),
    targetType: String(item.targetType ?? ""),
    targetId: String(item.targetId ?? ""),
    signalDefinitionId: String(item.signalDefinitionId ?? ""),
    evaluationWindowStartUtc: String(item.evaluationWindowStartUtc ?? ""),
    evaluationWindowEndUtc: String(item.evaluationWindowEndUtc ?? ""),
    qualityProfileId: String(item.qualityProfileId ?? ""),
    evidenceMode: String(item.evidenceMode ?? ""),
    outcome: String(item.outcome ?? ""),
    leadingDriverCode: item.leadingDriverCode ? String(item.leadingDriverCode) : null,
    actualObservationCount: Number(item.actualObservationCount ?? 0),
    expectedObservationCount: Number(item.expectedObservationCount ?? 0),
    completenessRatio: item.completenessRatio === null || item.completenessRatio === undefined
      ? null
      : Number(item.completenessRatio),
    goodRatio: item.goodRatio === null || item.goodRatio === undefined ? null : Number(item.goodRatio),
    uncertainRatio: item.uncertainRatio === null || item.uncertainRatio === undefined
      ? null
      : Number(item.uncertainRatio),
    badRatio: item.badRatio === null || item.badRatio === undefined ? null : Number(item.badRatio),
    staleRatio: item.staleRatio === null || item.staleRatio === undefined
      ? null
      : Number(item.staleRatio),
    sourceConflictRatio: item.sourceConflictRatio === null || item.sourceConflictRatio === undefined
      ? null
      : Number(item.sourceConflictRatio),
    freshnessSeconds: item.freshnessSeconds === null || item.freshnessSeconds === undefined
      ? null
      : Number(item.freshnessSeconds),
    maxClockSkewSeconds: item.maxClockSkewSeconds === null || item.maxClockSkewSeconds === undefined
      ? null
      : Number(item.maxClockSkewSeconds),
    evaluatedAtUtc: String(item.evaluatedAtUtc ?? ""),
    sourceProvenance: String(item.sourceProvenance ?? ""),
    isPilotData: readBoolean(item.isPilotData),
  };
}

function parseWriteResult(payload: unknown): IotPlcWriteResult {
  const item = payload as Record<string, unknown>;
  return {
    id: String(item.id ?? ""),
    version: Number(item.version ?? 0),
    idempotentReplay: readBoolean(item.idempotentReplay),
  };
}

function parseDriver(item: Record<string, unknown>): RawIotPlcDriver {
  return {
    reasonCode: String(item.reasonCode ?? ""),
    explanation: String(item.explanation ?? ""),
    isLeadingDriver: readBoolean(item.isLeadingDriver),
    affectedObservationCount: Number(item.affectedObservationCount ?? 0),
  };
}

export function getIotPlcWorkspace(context: ProductionOrderRequestContext) {
  return getJson(context, "/api/iot-plc/workspace", undefined, (payload) => {
    const item = payload as Record<string, unknown>;
    return {
      activeEndpointCount: Number(item.activeEndpointCount ?? 0),
      activeSignalCount: Number(item.activeSignalCount ?? 0),
      recentObservationCount: Number(item.recentObservationCount ?? 0),
      provenanceDisclaimer: String(item.provenanceDisclaimer ?? ""),
      hasDevelopmentPilotData: readBoolean(item.hasDevelopmentPilotData),
      capabilities: parseCapabilities(item.capabilities),
    } satisfies RawIotPlcWorkspace;
  });
}

export function getIotPlcIntelligenceWorkspace(context: ProductionOrderRequestContext) {
  return getJson(context, "/api/iot-plc/intelligence/workspace", undefined, (payload) => {
    const item = payload as Record<string, unknown>;
    return {
      approvedProfileCount: Number(item.approvedProfileCount ?? 0),
      recentConnectivityAssessmentCount: Number(item.recentConnectivityAssessmentCount ?? 0),
      recentSignalAssessmentCount: Number(item.recentSignalAssessmentCount ?? 0),
      ruleSetVersion: String(item.ruleSetVersion ?? ""),
      provenanceDisclaimer: String(item.provenanceDisclaimer ?? ""),
      hasDevelopmentPilotData: readBoolean(item.hasDevelopmentPilotData),
      capabilities: parseCapabilities(item.capabilities),
    } satisfies RawIotPlcIntelligenceWorkspace;
  });
}

export function listIotPlcEndpoints(
  context: ProductionOrderRequestContext,
  pageSize = "100",
) {
  return getJson(
    context,
    "/api/iot-plc/endpoints",
    { page: "1", pageSize },
    (payload) => {
      const item = payload as Record<string, unknown>;
      const items = Array.isArray(item.items) ? item.items : [];
      return {
        items: items.map((entry) => parseEndpoint(entry as Record<string, unknown>)),
        totalCount: Number(item.totalCount ?? 0),
        capabilities: parseCapabilities(item.capabilities),
      };
    },
  );
}

export function getIotPlcEndpoint(
  context: ProductionOrderRequestContext,
  reference: string,
) {
  return getJson(
    context,
    `/api/iot-plc/endpoints/${encodeURIComponent(reference)}`,
    undefined,
    (payload) => {
      const item = payload as Record<string, unknown>;
      const endpoint = parseEndpoint((item.endpoint ?? {}) as Record<string, unknown>);
      const signals = Array.isArray(item.signals) ? item.signals : [];
      return {
        endpoint,
        connectionProfileJson: String(item.connectionProfileJson ?? "{}"),
        signals: signals.map((entry) => parseSignal(entry as Record<string, unknown>)),
        capabilities: parseCapabilities(item.capabilities),
      };
    },
  );
}

export function listIotPlcSignals(
  context: ProductionOrderRequestContext,
  pageSize = "100",
) {
  return getJson(
    context,
    "/api/iot-plc/signals",
    { page: "1", pageSize },
    (payload) => {
      const item = payload as Record<string, unknown>;
      const items = Array.isArray(item.items) ? item.items : [];
      return {
        items: items.map((entry) => parseSignal(entry as Record<string, unknown>)),
        totalCount: Number(item.totalCount ?? 0),
      };
    },
  );
}

export function listIotPlcSignalObservations(
  context: ProductionOrderRequestContext,
  reference: string,
  pageSize = "5",
) {
  return getJson(
    context,
    `/api/iot-plc/signals/${encodeURIComponent(reference)}/observations`,
    { page: "1", pageSize },
    (payload) => {
      const item = payload as Record<string, unknown>;
      const items = Array.isArray(item.items) ? item.items : [];
      return {
        items: items.map((entry) => parseObservation(entry as Record<string, unknown>)),
        totalCount: Number(item.totalCount ?? 0),
      };
    },
  );
}

export function listIotPlcConnectionHealth(
  context: ProductionOrderRequestContext,
  reference: string,
  pageSize = "12",
) {
  return getJson(
    context,
    `/api/iot-plc/endpoints/${encodeURIComponent(reference)}/connection-health`,
    { page: "1", pageSize },
    (payload) => {
      const item = payload as Record<string, unknown>;
      const items = Array.isArray(item.items) ? item.items : [];
      return {
        items: items.map((entry) => parseHealth(entry as Record<string, unknown>)),
        totalCount: Number(item.totalCount ?? 0),
      };
    },
  );
}

export function listIotPlcProfiles(context: ProductionOrderRequestContext) {
  return getJson(
    context,
    "/api/iot-plc/intelligence/quality-profiles",
    { page: "1", pageSize: "50" },
    (payload) => {
      const item = payload as Record<string, unknown>;
      const items = Array.isArray(item.items) ? item.items : [];
      return {
        items: items.map((entry) => parseProfile(entry as Record<string, unknown>)),
        totalCount: Number(item.totalCount ?? 0),
      };
    },
  );
}

export function listIotPlcConnectivityAssessments(context: ProductionOrderRequestContext) {
  return getJson(
    context,
    "/api/iot-plc/intelligence/connectivity-assessments",
    { page: "1", pageSize: "50" },
    (payload) => {
      const item = payload as Record<string, unknown>;
      const items = Array.isArray(item.items) ? item.items : [];
      return items.map((entry) => parseConnectivityAssessment(entry as Record<string, unknown>));
    },
  );
}

export function listIotPlcSignalAssessments(context: ProductionOrderRequestContext) {
  return getJson(
    context,
    "/api/iot-plc/intelligence/signal-assessments",
    { page: "1", pageSize: "50" },
    (payload) => {
      const item = payload as Record<string, unknown>;
      const items = Array.isArray(item.items) ? item.items : [];
      return items.map((entry) => parseSignalAssessment(entry as Record<string, unknown>));
    },
  );
}

export function getIotPlcConnectivityAssessment(
  context: ProductionOrderRequestContext,
  assessmentId: string,
) {
  return getJson(
    context,
    `/api/iot-plc/intelligence/connectivity-assessments/${assessmentId}`,
    undefined,
    (payload) => {
      const item = payload as Record<string, unknown>;
      const assessment = parseConnectivityAssessment(
        (item.assessment ?? {}) as Record<string, unknown>,
      );
      const drivers = Array.isArray(item.drivers) ? item.drivers : [];
      return {
        assessment,
        drivers: drivers.map((entry) => parseDriver(entry as Record<string, unknown>)),
      };
    },
  );
}

export function getIotPlcSignalAssessment(
  context: ProductionOrderRequestContext,
  assessmentId: string,
) {
  return getJson(
    context,
    `/api/iot-plc/intelligence/signal-assessments/${assessmentId}`,
    undefined,
    (payload) => {
      const item = payload as Record<string, unknown>;
      const assessment = parseSignalAssessment(
        (item.assessment ?? {}) as Record<string, unknown>,
      );
      const drivers = Array.isArray(item.drivers) ? item.drivers : [];
      return {
        assessment,
        drivers: drivers.map((entry) => parseDriver(entry as Record<string, unknown>)),
      };
    },
  );
}

export function createIotPlcEndpoint(
  context: ProductionOrderRequestContext,
  body: Record<string, unknown>,
) {
  return mutateJson(context, "POST", "/api/iot-plc/endpoints", body, parseWriteResult);
}

export function updateIotPlcEndpoint(
  context: ProductionOrderRequestContext,
  reference: string,
  body: Record<string, unknown>,
) {
  return mutateJson(
    context,
    "PUT",
    `/api/iot-plc/endpoints/${encodeURIComponent(reference)}`,
    body,
    parseWriteResult,
  );
}

export function createIotPlcSignal(
  context: ProductionOrderRequestContext,
  body: Record<string, unknown>,
) {
  return mutateJson(context, "POST", "/api/iot-plc/signals", body, parseWriteResult);
}

export function updateIotPlcSignal(
  context: ProductionOrderRequestContext,
  reference: string,
  body: Record<string, unknown>,
) {
  return mutateJson(
    context,
    "PUT",
    `/api/iot-plc/signals/${encodeURIComponent(reference)}`,
    body,
    parseWriteResult,
  );
}

export function createIotPlcProfile(
  context: ProductionOrderRequestContext,
  body: Record<string, unknown>,
) {
  return mutateJson(
    context,
    "POST",
    "/api/iot-plc/intelligence/quality-profiles",
    body,
    parseWriteResult,
  );
}

export function updateIotPlcProfile(
  context: ProductionOrderRequestContext,
  reference: string,
  body: Record<string, unknown>,
) {
  return mutateJson(
    context,
    "PUT",
    `/api/iot-plc/intelligence/quality-profiles/${encodeURIComponent(reference)}`,
    body,
    parseWriteResult,
  );
}

export function evaluateIotPlcConnectivity(
  context: ProductionOrderRequestContext,
  body: Record<string, unknown>,
) {
  return mutateJson(
    context,
    "POST",
    "/api/iot-plc/intelligence/connectivity-assessments/evaluate",
    body,
    parseWriteResult,
  );
}

export function evaluateIotPlcSignalQuality(
  context: ProductionOrderRequestContext,
  body: Record<string, unknown>,
) {
  return mutateJson(
    context,
    "POST",
    "/api/iot-plc/intelligence/signal-assessments/evaluate",
    body,
    parseWriteResult,
  );
}
