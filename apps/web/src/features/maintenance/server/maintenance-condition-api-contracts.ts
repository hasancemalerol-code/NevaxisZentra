import type {
  MaintenanceConditionAnomaly,
  MaintenanceConditionAssetRow,
  MaintenanceConditionDataFreshness,
  MaintenanceConditionDataSufficiency,
  MaintenanceConditionIntelligenceSignal,
  MaintenanceConditionMetricSnapshot,
  MaintenanceConditionOverview,
  MaintenanceConditionProfile,
  MaintenanceConditionStatus,
  MaintenanceConditionTrendState,
  MaintenanceConditionWorkspace,
} from "../domain/maintenance-condition";

function asRecord(value: unknown): Record<string, unknown> {
  if (typeof value !== "object" || value === null) {
    throw new Error("Invalid response payload.");
  }
  return value as Record<string, unknown>;
}

function asString(value: unknown, field: string): string {
  if (typeof value !== "string" || value.length === 0) {
    throw new Error(`Invalid ${field}.`);
  }
  return value;
}

function asNumber(value: unknown, field: string): number {
  if (typeof value !== "number" || Number.isNaN(value)) {
    throw new Error(`Invalid ${field}.`);
  }
  return value;
}

function asOptionalString(value: unknown): string | undefined {
  return typeof value === "string" ? value : undefined;
}

function asOptionalNumber(value: unknown): number | undefined {
  return typeof value === "number" && !Number.isNaN(value) ? value : undefined;
}

export function parseMaintenanceConditionOverview(payload: unknown): MaintenanceConditionOverview {
  const body = asRecord(payload);
  return {
    monitoredAssetCount: asNumber(body.monitoredAssetCount, "monitoredAssetCount"),
    normalCount: asNumber(body.normalCount, "normalCount"),
    watchCount: asNumber(body.watchCount, "watchCount"),
    degradedCount: asNumber(body.degradedCount, "degradedCount"),
    criticalCount: asNumber(body.criticalCount, "criticalCount"),
    insufficientDataCount: asNumber(body.insufficientDataCount, "insufficientDataCount"),
    lastEvaluatedAtUtc: asOptionalString(body.lastEvaluatedAtUtc),
  };
}

export function parseMaintenanceConditionAssetRow(payload: unknown): MaintenanceConditionAssetRow {
  const body = asRecord(payload);
  return {
    assetReference: asString(body.assetReference, "assetReference"),
    assetName: asOptionalString(body.assetName),
    machineCode: asOptionalString(body.machineCode),
    machineName: asOptionalString(body.machineName),
    machineId: asString(body.machineId, "machineId"),
    conditionStatus: asString(body.conditionStatus, "conditionStatus") as MaintenanceConditionStatus,
    activeAnomalyCount: asNumber(body.activeAnomalyCount, "activeAnomalyCount"),
    highestAnomalySeverity: asOptionalString(body.highestAnomalySeverity),
    dataFreshness: asString(body.dataFreshness, "dataFreshness") as MaintenanceConditionDataFreshness,
    dataSufficiency: asString(body.dataSufficiency, "dataSufficiency") as MaintenanceConditionDataSufficiency,
    lastEvaluatedAtUtc: asOptionalString(body.lastEvaluatedAtUtc),
  };
}

export function parseMaintenanceConditionWorkspace(payload: unknown): MaintenanceConditionWorkspace {
  const body = asRecord(payload);
  const assets = Array.isArray(body.assets) ? body.assets : [];
  return {
    overview: parseMaintenanceConditionOverview(body.overview),
    assets: assets.map(parseMaintenanceConditionAssetRow),
  };
}

export function parseMaintenanceConditionAnomaly(payload: unknown): MaintenanceConditionAnomaly {
  const body = asRecord(payload);
  return {
    id: asString(body.id, "id"),
    metricKey: asString(body.metricKey, "metricKey"),
    metricDisplayName: asString(body.metricDisplayName, "metricDisplayName"),
    detectedAtUtc: asString(body.detectedAtUtc, "detectedAtUtc"),
    observedValue: asNumber(body.observedValue, "observedValue"),
    baselineValue: asNumber(body.baselineValue, "baselineValue"),
    deviationPercent: asNumber(body.deviationPercent, "deviationPercent"),
    severity: asString(body.severity, "severity"),
    direction: asString(body.direction, "direction"),
    evidenceSummary: asString(body.evidenceSummary, "evidenceSummary"),
    sourceFreshness: asString(body.sourceFreshness, "sourceFreshness") as MaintenanceConditionDataFreshness,
    dataQuality: asOptionalString(body.dataQuality),
    status: asString(body.status, "status"),
  };
}

export function parseMaintenanceConditionProfile(payload: unknown): MaintenanceConditionProfile {
  const body = asRecord(payload);
  const context = asRecord(body.context);
  const metricSnapshots = Array.isArray(context.metricSnapshots) ? context.metricSnapshots : [];
  const signals = Array.isArray(body.signals) ? body.signals : [];
  const activeAnomalies = Array.isArray(body.activeAnomalies) ? body.activeAnomalies : [];
  const alarmContext = asRecord(context.alarmContext);
  const downtimeContext = asRecord(context.downtimeContext);
  const maintenanceHistory = asRecord(context.maintenanceHistory);

  return {
    assetReference: asString(body.assetReference, "assetReference"),
    assetName: asOptionalString(body.assetName),
    machineId: asString(body.machineId, "machineId"),
    machineCode: asOptionalString(body.machineCode),
    machineName: asOptionalString(body.machineName),
    conditionStatus: asString(body.conditionStatus, "conditionStatus") as MaintenanceConditionStatus,
    overallConditionScore: asOptionalNumber(body.overallConditionScore),
    lastEvaluatedAtUtc: asOptionalString(body.lastEvaluatedAtUtc),
    dataFreshness: asString(body.dataFreshness, "dataFreshness") as MaintenanceConditionDataFreshness,
    dataSufficiency: asString(body.dataSufficiency, "dataSufficiency") as MaintenanceConditionDataSufficiency,
    activeAnomalyCount: asNumber(body.activeAnomalyCount, "activeAnomalyCount"),
    highestAnomalySeverity: asOptionalString(body.highestAnomalySeverity),
    trendState: asString(body.trendState, "trendState") as MaintenanceConditionTrendState,
    signals: signals.map(parseMaintenanceConditionSignal),
    activeAnomalies: activeAnomalies.map(parseMaintenanceConditionAnomaly),
    context: {
      alarmContext: {
        recentAlarmCount24h: asNumber(alarmContext.recentAlarmCount24h, "recentAlarmCount24h"),
        highestSeverity24h: asOptionalString(alarmContext.highestSeverity24h),
      },
      downtimeContext: {
        episodeCount24h: asNumber(downtimeContext.episodeCount24h, "episodeCount24h"),
        totalDurationMinutes24h: asNumber(
          downtimeContext.totalDurationMinutes24h,
          "totalDurationMinutes24h",
        ),
      },
      maintenanceHistory: {
        recentFailureCount90d: asNumber(
          maintenanceHistory.recentFailureCount90d,
          "recentFailureCount90d",
        ),
        lastFailureCategory: asOptionalString(maintenanceHistory.lastFailureCategory),
        lastMaintenanceCompletedAtUtc: asOptionalString(
          maintenanceHistory.lastMaintenanceCompletedAtUtc,
        ),
      },
      metricSnapshots: metricSnapshots.map((item): MaintenanceConditionMetricSnapshot => {
        const snapshot = asRecord(item);
        return {
          metricKey: asString(snapshot.metricKey, "metricKey"),
          displayName: asString(snapshot.displayName, "displayName"),
          expectedUnit: asOptionalString(snapshot.expectedUnit),
          dataSufficiency: asString(
            snapshot.dataSufficiency,
            "dataSufficiency",
          ) as MaintenanceConditionDataSufficiency,
          dataFreshness: asString(
            snapshot.dataFreshness,
            "dataFreshness",
          ) as MaintenanceConditionDataFreshness,
          trendState: asOptionalString(snapshot.trendState) as MaintenanceConditionTrendState | undefined,
          baselineValue: asOptionalNumber(snapshot.baselineValue),
          observedValue: asOptionalNumber(snapshot.observedValue),
          deviationPercent: asOptionalNumber(snapshot.deviationPercent),
        };
      }),
    },
  };
}

function parseMaintenanceConditionSignal(payload: unknown): MaintenanceConditionIntelligenceSignal {
  const body = asRecord(payload);
  return {
    signalType: asString(body.signalType, "signalType"),
    metricKey: asString(body.metricKey, "metricKey"),
    severity: asString(body.severity, "severity"),
    summary: asString(body.summary, "summary"),
  };
}

export function parseMaintenanceConditionAnomalyList(payload: unknown): readonly MaintenanceConditionAnomaly[] {
  if (!Array.isArray(payload)) {
    throw new Error("Invalid anomaly list.");
  }
  return payload.map(parseMaintenanceConditionAnomaly);
}
