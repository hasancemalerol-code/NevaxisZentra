import "./iot-plc-server-boundary";

import {
  formatIotPlcCompactPeriod,
  formatIotPlcCount,
  formatIotPlcDateTime,
} from "../domain/iot-plc-formatters";
import {
  getIotPlcDirectionLabel,
  getIotPlcDriverLabel,
  getIotPlcEndpointStatusLabel,
  getIotPlcEvidenceLabel,
  getIotPlcFindingTitle,
  getIotPlcHealthLabel,
  getIotPlcOutcomeLabel,
  getIotPlcProfileDisplayName,
  getIotPlcProfileStatusLabel,
  getIotPlcProtocolLabel,
  getIotPlcQualityStateLabel,
  getIotPlcSemanticTypeLabel,
  IOT_PLC_KPI_ORDER,
} from "../domain/iot-plc-labels";
import { reconcileIotPlcQualityDistribution, validateIotPlcSignalAssessmentRatioContract } from "../components/iot-plc-presentation";
import type {
  IotPlcAssessmentDetail,
  IotPlcAssessmentSummary,
  IotPlcEndpointDetail,
  IotPlcEndpointSummary,
  IotPlcFinding,
  IotPlcHealthBucket,
  IotPlcHealthSegment,
  IotPlcHealthTimelinePoint,
  IotPlcKpiCard,
  IotPlcObservationSummary,
  IotPlcQualityDistributionSlice,
  IotPlcQualityProfileSummary,
  IotPlcSignalDetail,
  IotPlcSignalSummary,
  IotPlcWorkspacePayload,
} from "../domain/iot-plc-types";
import {
  getIotPlcConnectivityAssessment,
  getIotPlcEndpoint,
  getIotPlcIntelligenceWorkspace,
  getIotPlcSignalAssessment,
  getIotPlcWorkspace,
  listIotPlcConnectionHealth,
  listIotPlcConnectivityAssessments,
  listIotPlcEndpoints,
  listIotPlcProfiles,
  listIotPlcSignalAssessments,
  listIotPlcSignalObservations,
  listIotPlcSignals,
  type RawIotPlcConnectivityAssessment,
  type RawIotPlcEndpoint,
  type RawIotPlcObservation,
  type RawIotPlcProfile,
  type RawIotPlcSignal,
  type RawIotPlcSignalAssessment,
} from "./iot-plc-api-client";
import { resolveIotPlcRequestContext } from "./iot-plc-request-context";
import {
  fetchEffectiveAccessCapabilities,
  hasGrantedPermission,
} from "../../admin-access/server/admin-access-effective-access";

type ReadFailure = Readonly<{
  status: "failure";
  message: string;
  code?: string;
}>;

export type IotPlcWorkspaceResult =
  | Readonly<{ status: "success"; workspace: IotPlcWorkspacePayload; canManage: boolean }>
  | ReadFailure;

export type IotPlcEndpointDetailResult =
  | Readonly<{ status: "success"; detail: IotPlcEndpointDetail }>
  | ReadFailure;

export type IotPlcSignalDetailResult =
  | Readonly<{ status: "success"; detail: IotPlcSignalDetail }>
  | ReadFailure;

export type IotPlcAssessmentDetailResult =
  | Readonly<{ status: "success"; detail: IotPlcAssessmentDetail }>
  | ReadFailure;

const HEALTH_COLORS: Record<IotPlcHealthBucket, string> = {
  online: "#16a34a",
  degraded: "#d97706",
  offline: "#dc2626",
  unknown: "#94a3b8",
};

const QUALITY_COLORS: Record<string, string> = {
  good: "#16a34a",
  uncertain: "#d97706",
  bad: "#dc2626",
  stale: "#64748b",
  "source-conflict": "#7c3aed",
};

function evidenceModeLabel(provenance: string, isPilot: boolean): string {
  if (isPilot || provenance.toLowerCase().includes("simul")) return "Simülasyon";
  if (provenance.toLowerCase().includes("measured")) return "Ölçülmüş";
  return getIotPlcEvidenceLabel(isPilot ? "simulated" : "measured");
}

function mappedAssetLabel(signal: RawIotPlcSignal): string {
  if (signal.mappedAssetType === "unmapped" || !signal.mappedCanonicalAssetId) {
    return "Eşleşmemiş";
  }
  return signal.mappedAssetType === "machine" ? "Makine varlığı" : "Yardımcı ekipman";
}

function classifyEndpointHealth(
  endpoint: RawIotPlcEndpoint,
  staleThresholdSeconds: number | null,
  evaluationTimeMs: number,
): IotPlcHealthBucket {
  if (!endpoint.lastObservedConnectionState || !endpoint.lastObservedAtUtc) {
    return "unknown";
  }

  const ageSeconds = (evaluationTimeMs - new Date(endpoint.lastObservedAtUtc).getTime()) / 1000;
  if (staleThresholdSeconds !== null && ageSeconds > staleThresholdSeconds) {
    return "unknown";
  }

  const state = endpoint.lastObservedConnectionState;
  if (state === "online" || state === "degraded" || state === "offline" || state === "unknown") {
    return state;
  }

  return "unknown";
}

function mapEndpointSummary(
  endpoint: RawIotPlcEndpoint,
  staleThresholdSeconds: number | null,
  evaluationTimeMs: number,
): IotPlcEndpointSummary {
  const healthBucket = classifyEndpointHealth(endpoint, staleThresholdSeconds, evaluationTimeMs);
  return {
    reference: endpoint.endpointCode,
    endpointCode: endpoint.endpointCode,
    displayName: endpoint.displayName,
    protocolType: endpoint.protocolType,
    protocolLabel: getIotPlcProtocolLabel(endpoint.protocolType),
    connectionDirection: endpoint.connectionDirection,
    connectionDirectionLabel: getIotPlcDirectionLabel(endpoint.connectionDirection),
    endpointStatus: endpoint.endpointStatus,
    endpointStatusLabel: getIotPlcEndpointStatusLabel(endpoint.endpointStatus),
    healthState: endpoint.lastObservedConnectionState,
    healthLabel: getIotPlcHealthLabel(healthBucket),
    healthBucket,
    lastEvidenceAtUtc: endpoint.lastObservedAtUtc,
    lastEvidenceLabel: formatIotPlcDateTime(endpoint.lastObservedAtUtc),
    signalCount: endpoint.signalCount,
    evidenceModeLabel: evidenceModeLabel(endpoint.configurationProvenance, endpoint.isPilotData),
    configurationProvenance: endpoint.configurationProvenance,
    version: endpoint.version,
    isPilotData: endpoint.isPilotData,
  };
}

function mapSignalSummary(
  signal: RawIotPlcSignal,
  endpointLabelByCode: ReadonlyMap<string, string>,
  latestObservation: RawIotPlcObservation | null,
  freshnessToleranceSeconds: number | null,
  evaluationTimeMs: number,
): IotPlcSignalSummary {
  let latestQualityState = latestObservation?.qualityState ?? null;
  const latestObservationAtUtc = latestObservation?.observedAtUtc ?? null;

  if (latestObservationAtUtc && freshnessToleranceSeconds !== null) {
    const ageSeconds = (evaluationTimeMs - new Date(latestObservationAtUtc).getTime()) / 1000;
    if (ageSeconds > freshnessToleranceSeconds && latestQualityState === "good") {
      latestQualityState = "stale";
    }
  }

  return {
    reference: signal.signalCode,
    signalCode: signal.signalCode,
    displayName: signal.displayName,
    endpointCode: signal.endpointCode,
    endpointLabel: endpointLabelByCode.get(signal.endpointCode) ?? signal.endpointCode,
    semanticType: signal.semanticType,
    semanticTypeLabel: getIotPlcSemanticTypeLabel(signal.semanticType),
    dataType: signal.dataType,
    engineeringUnit: signal.engineeringUnit,
    mappedAssetLabel: mappedAssetLabel(signal),
    isEnabled: signal.isEnabled,
    enabledLabel: signal.isEnabled ? "Etkin" : "Devre dışı",
    latestObservationAtUtc,
    latestObservationLabel: formatIotPlcDateTime(latestObservationAtUtc),
    latestQualityState,
    latestQualityLabel: getIotPlcQualityStateLabel(latestQualityState),
    latestEvidenceLabel: latestObservation
      ? getIotPlcEvidenceLabel(latestObservation.evidenceState)
      : "—",
    samplingExpectation: signal.samplingExpectation,
    sourceAddress: signal.sourceAddress,
    sourceProvenance: signal.sourceProvenance,
    version: signal.version,
    isPilotData: signal.isPilotData,
  };
}

function buildHealthComposition(endpoints: readonly IotPlcEndpointSummary[]): {
  segments: IotPlcHealthSegment[];
  total: number;
} {
  const total = endpoints.length;
  const counts: Record<IotPlcHealthBucket, number> = {
    online: 0,
    degraded: 0,
    offline: 0,
    unknown: 0,
  };

  for (const endpoint of endpoints) {
    counts[endpoint.healthBucket] += 1;
  }

  const segments: IotPlcHealthSegment[] = (Object.keys(counts) as IotPlcHealthBucket[]).map(
    (bucket) => ({
      bucket,
      label: getIotPlcHealthLabel(bucket),
      count: counts[bucket],
      percent: total > 0 ? counts[bucket] / total : 0,
      color: HEALTH_COLORS[bucket],
    }),
  );

  return { segments, total };
}

function selectSignalAssessmentForQualityDistribution(
  assessments: readonly RawIotPlcSignalAssessment[],
): {
  assessment: RawIotPlcSignalAssessment | null;
  contractViolationCount: number;
} {
  const sorted = [...assessments].sort(
    (left, right) => new Date(right.evaluatedAtUtc).getTime() - new Date(left.evaluatedAtUtc).getTime(),
  );

  let contractViolationCount = 0;
  for (const assessment of sorted) {
    if (assessment.actualObservationCount <= 0) continue;
    const validation = validateIotPlcSignalAssessmentRatioContract(assessment);
    if (!validation.compliant) {
      contractViolationCount += 1;
      continue;
    }
    return { assessment, contractViolationCount };
  }

  return { assessment: null, contractViolationCount };
}

function buildQualityDistribution(
  assessment: RawIotPlcSignalAssessment | null,
): {
  slices: IotPlcQualityDistributionSlice[];
  denominator: number | null;
  label: string | null;
} {
  if (!assessment || assessment.actualObservationCount <= 0) {
    return { slices: [], denominator: null, label: null };
  }

  const denominator = assessment.actualObservationCount;
  const entries: Array<[string, number | null]> = [
    ["good", assessment.goodRatio],
    ["uncertain", assessment.uncertainRatio],
    ["bad", assessment.badRatio],
    ["stale", assessment.staleRatio],
    ["source-conflict", assessment.sourceConflictRatio],
  ];

  const slices = reconcileIotPlcQualityDistribution(
    denominator,
    entries.map(([key, rawRatio]) => ({
      key,
      label: getIotPlcQualityStateLabel(key),
      rawRatio,
      color: QUALITY_COLORS[key] ?? "#64748b",
    })),
  );

  return {
    slices: slices as IotPlcQualityDistributionSlice[],
    denominator,
    label: formatIotPlcCompactPeriod(
      assessment.evaluationWindowStartUtc,
      assessment.evaluationWindowEndUtc,
    ),
  };
}

function isAttentionOutcome(outcome: string): boolean {
  return outcome === "attention"
    || outcome === "degraded"
    || outcome === "unavailable"
    || outcome === "source-conflict"
    || outcome === "insufficient-data";
}

function mapConnectivityAssessmentSummary(
  assessment: RawIotPlcConnectivityAssessment,
  endpointById: ReadonlyMap<string, RawIotPlcEndpoint>,
  profileCodeById: ReadonlyMap<string, string>,
): IotPlcAssessmentSummary {
  const endpoint = endpointById.get(assessment.targetId);
  const targetLabel = endpoint?.displayName ?? "Bağlantı";
  const targetReference = endpoint?.endpointCode ?? "";
  return {
    id: assessment.id,
    assessmentKind: "connectivity",
    targetLabel,
    targetReference,
    evaluationPeriodLabel: formatIotPlcCompactPeriod(
      assessment.evaluationWindowStartUtc,
      assessment.evaluationWindowEndUtc,
    ),
    evaluationWindowStartUtc: assessment.evaluationWindowStartUtc,
    evaluationWindowEndUtc: assessment.evaluationWindowEndUtc,
    evidenceMode: assessment.evidenceMode,
    evidenceLabel: getIotPlcEvidenceLabel(assessment.evidenceMode),
    outcome: assessment.outcome,
    outcomeLabel: getIotPlcOutcomeLabel(assessment.outcome),
    leadingDriverCode: assessment.leadingDriverCode,
    leadingDriverLabel: getIotPlcDriverLabel(assessment.leadingDriverCode),
    profileCode: profileCodeById.get(assessment.qualityProfileId) ?? null,
    evaluatedAtUtc: assessment.evaluatedAtUtc,
    isPilotData: assessment.isPilotData,
  };
}

function mapSignalAssessmentSummary(
  assessment: RawIotPlcSignalAssessment,
  signalById: ReadonlyMap<string, RawIotPlcSignal>,
  profileCodeById: ReadonlyMap<string, string>,
): IotPlcAssessmentSummary {
  const signal = signalById.get(assessment.signalDefinitionId);
  const targetLabel = signal?.displayName ?? "Sinyal";
  const targetReference = signal?.signalCode ?? "";
  return {
    id: assessment.id,
    assessmentKind: "signal",
    targetLabel,
    targetReference,
    evaluationPeriodLabel: formatIotPlcCompactPeriod(
      assessment.evaluationWindowStartUtc,
      assessment.evaluationWindowEndUtc,
    ),
    evaluationWindowStartUtc: assessment.evaluationWindowStartUtc,
    evaluationWindowEndUtc: assessment.evaluationWindowEndUtc,
    evidenceMode: assessment.evidenceMode,
    evidenceLabel: getIotPlcEvidenceLabel(assessment.evidenceMode),
    outcome: assessment.outcome,
    outcomeLabel: getIotPlcOutcomeLabel(assessment.outcome),
    leadingDriverCode: assessment.leadingDriverCode,
    leadingDriverLabel: getIotPlcDriverLabel(assessment.leadingDriverCode),
    profileCode: profileCodeById.get(assessment.qualityProfileId) ?? null,
    evaluatedAtUtc: assessment.evaluatedAtUtc,
    isPilotData: assessment.isPilotData,
  };
}

function buildFindings(
  endpoints: readonly IotPlcEndpointSummary[],
  assessments: readonly IotPlcAssessmentSummary[],
): IotPlcFinding[] {
  const findings: IotPlcFinding[] = [];

  for (const endpoint of endpoints) {
    if (endpoint.healthBucket === "offline") {
      findings.push({
        id: `endpoint-offline-${endpoint.endpointCode}`,
        title: "Bağlantı çevrimdışı",
        detail: `${endpoint.displayName} (${endpoint.endpointCode}) için güncel sağlık kanıtı çevrimdışı.`,
        severity: "danger",
        technicalCode: "endpoint-offline",
      });
    } else if (endpoint.healthBucket === "degraded") {
      findings.push({
        id: `endpoint-degraded-${endpoint.endpointCode}`,
        title: "Bağlantı bozulmuş",
        detail: `${endpoint.displayName} bağlantı sağlığı bozulmuş olarak raporlandı.`,
        severity: "warning",
        technicalCode: "endpoint-health-degraded",
      });
    }
  }

  for (const assessment of assessments) {
    if (!isAttentionOutcome(assessment.outcome) || !assessment.leadingDriverCode) continue;
    findings.push({
      id: `assessment-${assessment.id}`,
      title: getIotPlcFindingTitle(assessment.leadingDriverCode),
      detail: `${assessment.targetLabel}: ${assessment.leadingDriverLabel}`,
      severity: assessment.outcome === "unavailable" || assessment.outcome === "source-conflict"
        ? "danger"
        : "warning",
      technicalCode: assessment.leadingDriverCode,
      targetLabel: assessment.targetLabel,
      leadingDriverLabel: assessment.leadingDriverLabel,
      periodLabel: assessment.evaluationPeriodLabel,
      evidenceLabel: assessment.evidenceLabel,
      outcomeLabel: assessment.outcomeLabel,
    });
  }

  return findings.slice(0, 8);
}

function buildKpis(input: {
  endpoints: readonly IotPlcEndpointSummary[];
  signals: readonly IotPlcSignalSummary[];
  connectivityAssessments: readonly IotPlcAssessmentSummary[];
  signalAssessments: readonly IotPlcAssessmentSummary[];
  registeredCount: number | null;
  activeSignalCount: number | null;
  sourceAvailable: boolean;
}): IotPlcKpiCard[] {
  const onlineCount = input.sourceAvailable
    ? input.endpoints.filter((endpoint) => endpoint.healthBucket === "online").length
    : null;
  const degradedOfflineCount = input.sourceAvailable
    ? input.endpoints.filter(
      (endpoint) => endpoint.healthBucket === "degraded" || endpoint.healthBucket === "offline",
    ).length
    : null;
  const allCurrentUnknown = input.sourceAvailable
    && input.endpoints.length > 0
    && input.endpoints.every((endpoint) => endpoint.healthBucket === "unknown");
  const totalAssessments = input.connectivityAssessments.length + input.signalAssessments.length;
  const attentionAssessments = input.sourceAvailable
    ? [...input.connectivityAssessments, ...input.signalAssessments].filter(
      (assessment) => isAttentionOutcome(assessment.outcome),
    ).length
    : null;
  const activeSignals = input.activeSignalCount;
  const freshSignals = input.sourceAvailable
    ? input.signals.filter(
      (signal) => signal.isEnabled
        && signal.latestQualityState === "good"
        && signal.latestObservationAtUtc,
    ).length
    : null;

  const cards: Record<(typeof IOT_PLC_KPI_ORDER)[number], IotPlcKpiCard> = {
    "registered-endpoints": {
      id: "registered-endpoints",
      label: "Kayıtlı bağlantılar",
      value: input.registeredCount === null ? "—" : formatIotPlcCount(input.registeredCount),
      comparisonLabel: input.sourceAvailable ? "Kayıtlı uç nokta sayısı" : "Veri kullanılamıyor",
      tone: input.sourceAvailable ? "info" : "unavailable",
      badgeLabel: null,
    },
    "online-endpoints": {
      id: "online-endpoints",
      label: "Çevrimiçi bağlantılar",
      value: onlineCount === null ? "—" : formatIotPlcCount(onlineCount),
      comparisonLabel: "Yalnızca güncel çevrimiçi kanıt",
      tone: onlineCount === null
        ? "unavailable"
        : onlineCount > 0
          ? "success"
          : allCurrentUnknown
            ? "unavailable"
            : "warning",
      badgeLabel: onlineCount === null ? null : allCurrentUnknown ? "Kanıt yok" : "Kanıtlı",
    },
    "degraded-offline-endpoints": {
      id: "degraded-offline-endpoints",
      label: "Bozulmuş / çevrimdışı",
      value: degradedOfflineCount === null ? "—" : formatIotPlcCount(degradedOfflineCount),
      comparisonLabel: "Güncel sağlık kanıtına göre",
      tone: degradedOfflineCount === null
        ? "unavailable"
        : degradedOfflineCount > 0
          ? "danger"
          : allCurrentUnknown
            ? "unavailable"
            : "success",
      badgeLabel: null,
    },
    "active-signals": {
      id: "active-signals",
      label: "Aktif sinyaller",
      value: activeSignals === null ? "—" : formatIotPlcCount(activeSignals),
      comparisonLabel: "Etkin sinyal tanımları",
      tone: activeSignals === null ? "unavailable" : "info",
      badgeLabel: null,
    },
    "fresh-signals": {
      id: "fresh-signals",
      label: "Güncel sinyaller",
      value: freshSignals === null ? "—" : formatIotPlcCount(freshSignals),
      comparisonLabel: "İyi kalite ve güncel gözlem",
      tone: freshSignals === null ? "unavailable" : freshSignals > 0 ? "success" : "warning",
      badgeLabel: null,
    },
    "attention-assessments": {
      id: "attention-assessments",
      label: "Dikkat gerektiren değerlendirmeler",
      value: attentionAssessments === null ? "—" : formatIotPlcCount(attentionAssessments),
      comparisonLabel: totalAssessments > 0
        ? `${totalAssessments} kayıtlı değerlendirme · dikkat gerektiren sonuçlar`
        : "Bağlantı ve sinyal değerlendirmeleri",
      tone: attentionAssessments === null
        ? "unavailable"
        : attentionAssessments > 0
          ? "warning"
          : "success",
      badgeLabel: attentionAssessments && attentionAssessments > 0 ? "İncele" : null,
    },
  };

  return IOT_PLC_KPI_ORDER.map((id) => cards[id]);
}

function mapProfileSummary(profile: RawIotPlcProfile): IotPlcQualityProfileSummary {
  return {
    reference: profile.profileCode,
    profileCode: profile.profileCode,
    displayName: getIotPlcProfileDisplayName(profile.displayName),
    status: profile.status,
    statusLabel: getIotPlcProfileStatusLabel(profile.status),
    minimumGoodQualityRatio: profile.minimumGoodQualityRatio,
    maximumBadQualityRatio: profile.maximumBadQualityRatio,
    maximumUncertainQualityRatio: profile.maximumUncertainQualityRatio,
    maximumSourceConflictRatio: profile.maximumSourceConflictRatio,
    freshnessToleranceSeconds: profile.freshnessToleranceSeconds,
    staleThresholdSeconds: profile.staleThresholdSeconds,
    sequenceGapEvaluationEnabled: profile.sequenceGapEvaluationEnabled,
    sourceProvenance: profile.sourceProvenance,
    version: profile.version,
    isPilotData: profile.isPilotData,
  };
}

export async function readIotPlcWorkspace(): Promise<IotPlcWorkspaceResult> {
  const contextResult = await resolveIotPlcRequestContext();
  if (contextResult.status === "failure") {
    return {
      status: "failure",
      message: contextResult.message,
      code: contextResult.code,
    };
  }

  const context = contextResult.context;
  const effectiveAccess = await fetchEffectiveAccessCapabilities();
  const canManage = hasGrantedPermission(effectiveAccess, "iot-plc.manage");
  const evaluationTimeMs = Date.now();
  const partialErrors: string[] = [];

  const [
    workspaceResult,
    intelligenceWorkspaceResult,
    endpointsResult,
    signalsResult,
    profilesResult,
    connectivityAssessmentsResult,
    signalAssessmentsResult,
  ] = await Promise.all([
    getIotPlcWorkspace(context),
    getIotPlcIntelligenceWorkspace(context),
    listIotPlcEndpoints(context),
    listIotPlcSignals(context),
    listIotPlcProfiles(context),
    listIotPlcConnectivityAssessments(context),
    listIotPlcSignalAssessments(context),
  ]);

  if (workspaceResult.status === "failure" && workspaceResult.code === "permission-denied") {
    return { status: "failure", message: workspaceResult.message, code: workspaceResult.code };
  }
  if (workspaceResult.status === "failure" && workspaceResult.code === "unauthenticated") {
    return { status: "failure", message: workspaceResult.message, code: workspaceResult.code };
  }

  const sourceAvailable = workspaceResult.status === "success";
  if (!sourceAvailable) partialErrors.push(workspaceResult.message);
  if (intelligenceWorkspaceResult.status === "failure") {
    partialErrors.push(intelligenceWorkspaceResult.message);
  }
  if (endpointsResult.status === "failure") partialErrors.push(endpointsResult.message);
  if (signalsResult.status === "failure") partialErrors.push(signalsResult.message);
  if (profilesResult.status === "failure") partialErrors.push(profilesResult.message);
  if (connectivityAssessmentsResult.status === "failure") {
    partialErrors.push(connectivityAssessmentsResult.message);
  }
  if (signalAssessmentsResult.status === "failure") {
    partialErrors.push(signalAssessmentsResult.message);
  }

  const rawEndpoints = endpointsResult.status === "success" ? endpointsResult.value.items : [];
  const rawSignals = signalsResult.status === "success" ? signalsResult.value.items : [];
  const rawProfiles = profilesResult.status === "success" ? profilesResult.value.items : [];
  const standardProfile = rawProfiles.find((profile) => profile.profileCode === "QP-IOT-STANDARD") ?? rawProfiles[0] ?? null;
  const staleThresholdSeconds = standardProfile?.staleThresholdSeconds ?? null;
  const freshnessToleranceSeconds = standardProfile?.freshnessToleranceSeconds ?? null;

  const endpointById = new Map(rawEndpoints.map((endpoint) => [endpoint.id, endpoint]));
  const signalById = new Map(rawSignals.map((signal) => [signal.id, signal]));
  const profileCodeById = new Map(rawProfiles.map((profile) => [profile.id, profile.profileCode]));
  const endpointLabelByCode = new Map(
    rawEndpoints.map((endpoint) => [endpoint.endpointCode, endpoint.displayName]),
  );

  const latestObservations = new Map<string, RawIotPlcObservation>();
  if (signalsResult.status === "success") {
    const observationResults = await Promise.all(
      rawSignals
        .filter((signal) => signal.isEnabled)
        .slice(0, 24)
        .map(async (signal) => {
          const result = await listIotPlcSignalObservations(context, signal.signalCode, "1");
          return {
            signalCode: signal.signalCode,
            observation: result.status === "success" ? result.value.items[0] ?? null : null,
          };
        }),
    );
    for (const entry of observationResults) {
      if (entry.observation) latestObservations.set(entry.signalCode, entry.observation);
    }
  }

  const endpoints = rawEndpoints.map((endpoint) =>
    mapEndpointSummary(endpoint, staleThresholdSeconds, evaluationTimeMs),
  );
  const signals = rawSignals.map((signal) =>
    mapSignalSummary(
      signal,
      endpointLabelByCode,
      latestObservations.get(signal.signalCode) ?? null,
      freshnessToleranceSeconds,
      evaluationTimeMs,
    ),
  );

  const rawConnectivity = connectivityAssessmentsResult.status === "success"
    ? connectivityAssessmentsResult.value
    : [];
  const rawSignalAssessments = signalAssessmentsResult.status === "success"
    ? signalAssessmentsResult.value
    : [];

  const connectivityAssessments = rawConnectivity.map((assessment) =>
    mapConnectivityAssessmentSummary(assessment, endpointById, profileCodeById),
  );
  const signalAssessments = rawSignalAssessments.map((assessment) =>
    mapSignalAssessmentSummary(assessment, signalById, profileCodeById),
  );

  const qualitySelection = selectSignalAssessmentForQualityDistribution(rawSignalAssessments);
  const qualityDistributionResult = buildQualityDistribution(qualitySelection.assessment);
  const healthComposition = buildHealthComposition(endpoints);
  const findings = buildFindings(endpoints, [...connectivityAssessments, ...signalAssessments]);

  const timelineEndpoints = endpoints.slice(0, 4);
  const timelineResults = await Promise.all(
    timelineEndpoints.map(async (endpoint) => {
      const result = await listIotPlcConnectionHealth(context, endpoint.endpointCode, "8");
      return { endpoint, items: result.status === "success" ? result.value.items : [] };
    }),
  );
  const healthTimeline: IotPlcHealthTimelinePoint[] = timelineResults.flatMap(({ endpoint, items }) =>
    items.map((item) => ({
      endpointCode: endpoint.endpointCode,
      endpointLabel: endpoint.displayName,
      observedAtUtc: item.observedAtUtc,
      healthState: item.healthState,
      healthLabel: getIotPlcHealthLabel(item.healthState),
      evidenceState: item.evidenceState,
    })),
  ).sort(
    (left, right) => new Date(right.observedAtUtc).getTime() - new Date(left.observedAtUtc).getTime(),
  ).slice(0, 24);

  const hasPilot = (workspaceResult.status === "success" && workspaceResult.value.hasDevelopmentPilotData)
    || (intelligenceWorkspaceResult.status === "success"
      && intelligenceWorkspaceResult.value.hasDevelopmentPilotData);

  const workspace: IotPlcWorkspacePayload = {
    meta: {
      facilityLabel: hasPilot ? "Geliştirme pilot tesisi" : "Yetkili tesis",
      organizationLabel: "dev-pilot-tenant",
      refreshedAtUtc: new Date().toISOString(),
      refreshedLabel: formatIotPlcDateTime(new Date().toISOString()),
      provenanceDisclaimer: workspaceResult.status === "success"
        ? workspaceResult.value.provenanceDisclaimer
        : intelligenceWorkspaceResult.status === "success"
          ? intelligenceWorkspaceResult.value.provenanceDisclaimer
          : "",
      hasDevelopmentPilotData: hasPilot,
      ruleSetVersion: intelligenceWorkspaceResult.status === "success"
        ? intelligenceWorkspaceResult.value.ruleSetVersion
        : "",
      standardProfileStaleSeconds: staleThresholdSeconds,
      standardProfileFreshnessSeconds: freshnessToleranceSeconds,
    },
    kpis: buildKpis({
      endpoints,
      signals,
      connectivityAssessments,
      signalAssessments,
      registeredCount: endpointsResult.status === "success" ? endpointsResult.value.totalCount : null,
      activeSignalCount: workspaceResult.status === "success"
        ? workspaceResult.value.activeSignalCount
        : signals.filter((signal) => signal.isEnabled).length,
      sourceAvailable,
    }),
    healthComposition: healthComposition.segments,
    healthTotal: healthComposition.total,
    qualityDistribution: qualityDistributionResult.slices,
    qualityDenominator: qualityDistributionResult.denominator,
    qualityAssessmentLabel: qualityDistributionResult.label,
    qualityContractViolationCount: qualitySelection.contractViolationCount,
    qualitySelectedAssessmentId: qualitySelection.assessment?.id ?? null,
    findings,
    healthTimeline,
    endpoints,
    signals,
    profiles: rawProfiles.map(mapProfileSummary),
    connectivityAssessments,
    signalAssessments,
    partialErrors,
    sourceAvailable,
  };

  return { status: "success", workspace, canManage };
}

export async function readIotPlcEndpointDetail(reference: string): Promise<IotPlcEndpointDetailResult> {
  const contextResult = await resolveIotPlcRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message, code: contextResult.code };
  }

  const context = contextResult.context;
  const [detailResult, healthResult, profilesResult] = await Promise.all([
    getIotPlcEndpoint(context, reference),
    listIotPlcConnectionHealth(context, reference, "12"),
    listIotPlcProfiles(context),
  ]);

  if (detailResult.status === "failure") {
    return { status: "failure", message: detailResult.message, code: detailResult.code };
  }

  const standardProfile = profilesResult.status === "success"
    ? profilesResult.value.items.find((profile) => profile.profileCode === "QP-IOT-STANDARD") ?? null
    : null;

  const summary = mapEndpointSummary(
    detailResult.value.endpoint,
    standardProfile?.staleThresholdSeconds ?? null,
    Date.now(),
  );

  const endpointLabelByCode = new Map([[summary.endpointCode, summary.displayName]]);
  const mappedSignals = detailResult.value.signals.map((signal) =>
    mapSignalSummary(signal, endpointLabelByCode, null, null, Date.now()),
  );

  const healthHistory: IotPlcHealthTimelinePoint[] = healthResult.status === "success"
    ? healthResult.value.items.map((item) => ({
      endpointCode: summary.endpointCode,
      endpointLabel: summary.displayName,
      observedAtUtc: item.observedAtUtc,
      healthState: item.healthState,
      healthLabel: getIotPlcHealthLabel(item.healthState),
      evidenceState: item.evidenceState,
    }))
    : [];

  return {
    status: "success",
    detail: {
      summary,
      connectionProfileJson: detailResult.value.connectionProfileJson,
      mappedSignals,
      healthHistory,
    },
  };
}

export async function readIotPlcSignalDetail(reference: string): Promise<IotPlcSignalDetailResult> {
  const contextResult = await resolveIotPlcRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message, code: contextResult.code };
  }

  const context = contextResult.context;
  const [signalsResult, observationsResult, assessmentsResult, profilesResult] = await Promise.all([
    listIotPlcSignals(context),
    listIotPlcSignalObservations(context, reference, "8"),
    listIotPlcSignalAssessments(context),
    listIotPlcProfiles(context),
  ]);

  if (signalsResult.status === "failure") {
    return { status: "failure", message: signalsResult.message, code: signalsResult.code };
  }

  const rawSignal = signalsResult.value.items.find((signal) => signal.signalCode === reference);
  if (!rawSignal) {
    return { status: "failure", message: "Sinyal bulunamadı.", code: "not-found" };
  }

  const standardProfile = profilesResult.status === "success"
    ? profilesResult.value.items.find((profile) => profile.profileCode === "QP-IOT-STANDARD") ?? null
    : null;

  const latestObservation = observationsResult.status === "success"
    ? observationsResult.value.items[0] ?? null
    : null;

  const endpointLabelByCode = new Map([[rawSignal.endpointCode, rawSignal.endpointCode]]);
  const summary = mapSignalSummary(
    rawSignal,
    endpointLabelByCode,
    latestObservation,
    standardProfile?.freshnessToleranceSeconds ?? null,
    Date.now(),
  );

  const recentObservations: IotPlcObservationSummary[] = observationsResult.status === "success"
    ? observationsResult.value.items.map((observation) => ({
      observedAtUtc: observation.observedAtUtc,
      receivedAtUtc: observation.receivedAtUtc,
      qualityState: observation.qualityState,
      qualityLabel: getIotPlcQualityStateLabel(observation.qualityState),
      evidenceState: observation.evidenceState,
      evidenceLabel: getIotPlcEvidenceLabel(observation.evidenceState),
      valueLabel: observation.value === null || observation.value === undefined
        ? "—"
        : String(observation.value),
      sequenceReference: observation.sourceSequenceReference,
    }))
    : [];

  const latestAssessment = assessmentsResult.status === "success"
    ? [...assessmentsResult.value]
      .filter((assessment) => assessment.signalDefinitionId === rawSignal.id)
      .sort(
        (left, right) => new Date(right.evaluatedAtUtc).getTime() - new Date(left.evaluatedAtUtc).getTime(),
      )[0] ?? null
    : null;

  const qualityDistributionResult = buildQualityDistribution(latestAssessment);

  return {
    status: "success",
    detail: {
      summary,
      recentObservations,
      qualityDistribution: qualityDistributionResult.slices,
      qualityDistributionAvailable: qualityDistributionResult.denominator !== null,
    },
  };
}

export async function readIotPlcAssessmentDetail(
  assessmentId: string,
  kind: "connectivity" | "signal",
): Promise<IotPlcAssessmentDetailResult> {
  const contextResult = await resolveIotPlcRequestContext();
  if (contextResult.status === "failure") {
    return { status: "failure", message: contextResult.message, code: contextResult.code };
  }

  const context = contextResult.context;
  const [endpointsResult, signalsResult, profilesResult] = await Promise.all([
    listIotPlcEndpoints(context),
    listIotPlcSignals(context),
    listIotPlcProfiles(context),
  ]);

  const endpointById = new Map(
    (endpointsResult.status === "success" ? endpointsResult.value.items : [])
      .map((endpoint) => [endpoint.id, endpoint]),
  );
  const signalById = new Map(
    (signalsResult.status === "success" ? signalsResult.value.items : [])
      .map((signal) => [signal.id, signal]),
  );
  const profileById = new Map(
    (profilesResult.status === "success" ? profilesResult.value.items : [])
      .map((profile) => [profile.id, profile]),
  );
  const profileCodeById = new Map(
    (profilesResult.status === "success" ? profilesResult.value.items : [])
      .map((profile) => [profile.id, profile.profileCode]),
  );

  if (kind === "connectivity") {
    const detailResult = await getIotPlcConnectivityAssessment(context, assessmentId);
    if (detailResult.status === "failure") {
      return { status: "failure", message: detailResult.message, code: detailResult.code };
    }

    const assessment = detailResult.value.assessment;
    const summary = mapConnectivityAssessmentSummary(assessment, endpointById, profileCodeById);
    const profile = profileById.get(assessment.qualityProfileId);
    return {
      status: "success",
      detail: {
        summary,
        actualObservationCount: null,
        expectedObservationCount: null,
        completenessRatio: null,
        goodRatio: null,
        uncertainRatio: null,
        badRatio: null,
        staleRatio: null,
        sourceConflictRatio: null,
        freshnessSeconds: assessment.freshnessSeconds,
        maxClockSkewSeconds: null,
        latestHealthState: assessment.latestHealthState,
        healthObservationAvailabilityRatio: assessment.healthObservationAvailabilityRatio,
        profileLabel: profile?.displayName ?? "—",
        provenance: assessment.sourceProvenance,
        drivers: detailResult.value.drivers.map((driver) => ({
          reasonCode: driver.reasonCode,
          reasonLabel: getIotPlcDriverLabel(driver.reasonCode),
          explanation: driver.explanation,
          isLeadingDriver: driver.isLeadingDriver,
          affectedObservationCount: driver.affectedObservationCount,
        })),
      },
    };
  }

  const detailResult = await getIotPlcSignalAssessment(context, assessmentId);
  if (detailResult.status === "failure") {
    return { status: "failure", message: detailResult.message, code: detailResult.code };
  }

  const assessment = detailResult.value.assessment;
  const summary = mapSignalAssessmentSummary(assessment, signalById, profileCodeById);
  const profile = profileById.get(assessment.qualityProfileId);
  return {
    status: "success",
    detail: {
      summary,
      actualObservationCount: assessment.actualObservationCount,
      expectedObservationCount: assessment.expectedObservationCount,
      completenessRatio: assessment.completenessRatio,
      goodRatio: assessment.goodRatio,
      uncertainRatio: assessment.uncertainRatio,
      badRatio: assessment.badRatio,
      staleRatio: assessment.staleRatio,
      sourceConflictRatio: assessment.sourceConflictRatio,
      freshnessSeconds: assessment.freshnessSeconds,
      maxClockSkewSeconds: assessment.maxClockSkewSeconds,
      latestHealthState: null,
      healthObservationAvailabilityRatio: null,
      profileLabel: profile?.displayName ?? "—",
      provenance: assessment.sourceProvenance,
      drivers: detailResult.value.drivers.map((driver) => ({
        reasonCode: driver.reasonCode,
        reasonLabel: getIotPlcDriverLabel(driver.reasonCode),
        explanation: driver.explanation,
        isLeadingDriver: driver.isLeadingDriver,
        affectedObservationCount: driver.affectedObservationCount,
      })),
    },
  };
}
