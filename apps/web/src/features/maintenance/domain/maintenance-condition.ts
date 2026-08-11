export type MaintenanceConditionStatus =
  | "normal"
  | "watch"
  | "degraded"
  | "critical"
  | "unknown";

export type MaintenanceConditionDataSufficiency =
  | "high"
  | "medium"
  | "low"
  | "insufficient";

export type MaintenanceConditionDataFreshness = "fresh" | "stale" | "unknown";

export type MaintenanceConditionTrendState =
  | "stable"
  | "increasing"
  | "decreasing"
  | "volatile"
  | "insufficient";

export type MaintenanceConditionOverview = Readonly<{
  monitoredAssetCount: number;
  normalCount: number;
  watchCount: number;
  degradedCount: number;
  criticalCount: number;
  insufficientDataCount: number;
  lastEvaluatedAtUtc?: string;
}>;

export type MaintenanceConditionAssetRow = Readonly<{
  assetReference: string;
  assetName?: string;
  machineCode?: string;
  machineName?: string;
  machineId: string;
  conditionStatus: MaintenanceConditionStatus;
  activeAnomalyCount: number;
  highestAnomalySeverity?: string;
  dataFreshness: MaintenanceConditionDataFreshness;
  dataSufficiency: MaintenanceConditionDataSufficiency;
  lastEvaluatedAtUtc?: string;
}>;

export type MaintenanceConditionAnomaly = Readonly<{
  id: string;
  metricKey: string;
  metricDisplayName: string;
  detectedAtUtc: string;
  observedValue: number;
  baselineValue: number;
  deviationPercent: number;
  severity: string;
  direction: string;
  evidenceSummary: string;
  sourceFreshness: MaintenanceConditionDataFreshness;
  dataQuality?: string;
  status: string;
}>;

export type MaintenanceConditionIntelligenceSignal = Readonly<{
  signalType: string;
  metricKey: string;
  severity: string;
  summary: string;
}>;

export type MaintenanceConditionMetricSnapshot = Readonly<{
  metricKey: string;
  displayName: string;
  expectedUnit?: string;
  dataSufficiency: MaintenanceConditionDataSufficiency;
  dataFreshness: MaintenanceConditionDataFreshness;
  trendState?: MaintenanceConditionTrendState;
  baselineValue?: number;
  observedValue?: number;
  deviationPercent?: number;
}>;

export type MaintenanceConditionProfile = Readonly<{
  assetReference: string;
  assetName?: string;
  machineId: string;
  machineCode?: string;
  machineName?: string;
  conditionStatus: MaintenanceConditionStatus;
  overallConditionScore?: number;
  lastEvaluatedAtUtc?: string;
  dataFreshness: MaintenanceConditionDataFreshness;
  dataSufficiency: MaintenanceConditionDataSufficiency;
  activeAnomalyCount: number;
  highestAnomalySeverity?: string;
  trendState: MaintenanceConditionTrendState;
  signals: readonly MaintenanceConditionIntelligenceSignal[];
  activeAnomalies: readonly MaintenanceConditionAnomaly[];
  context: Readonly<{
    alarmContext: Readonly<{
      recentAlarmCount24h: number;
      highestSeverity24h?: string;
    }>;
    downtimeContext: Readonly<{
      episodeCount24h: number;
      totalDurationMinutes24h: number;
    }>;
    maintenanceHistory: Readonly<{
      recentFailureCount90d: number;
      lastFailureCategory?: string;
      lastMaintenanceCompletedAtUtc?: string;
    }>;
    metricSnapshots: readonly MaintenanceConditionMetricSnapshot[];
  }>;
}>;

export type MaintenanceConditionWorkspace = Readonly<{
  overview: MaintenanceConditionOverview;
  assets: readonly MaintenanceConditionAssetRow[];
}>;
