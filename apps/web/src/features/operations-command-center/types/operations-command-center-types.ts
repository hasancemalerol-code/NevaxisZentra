export type OperationsCommandCenterSourceAvailability =
  | "available"
  | "partial"
  | "unavailable-permission"
  | "unavailable-subscription"
  | "unavailable-error"
  | "insufficient-data";

export type OperationsCommandCenterModuleCode =
  | "commercial"
  | "production"
  | "quality"
  | "maintenance"
  | "mold"
  | "material"
  | "procurement"
  | "ai-task";

export type OperationsCommandCenterScopeState = "complete" | "partial" | "insufficient";

export type OperationsCommandCenterQueueItem = Readonly<{
  id: string;
  moduleCode: OperationsCommandCenterModuleCode;
  moduleLabel: string;
  sourceReference: string;
  title: string;
  statusLabel: string;
  evidenceLabel: string;
  freshnessLabel: string;
  linkedTaskId?: string;
  linkedTaskHref?: string;
  deepLinkHref: string;
  itemState: "available" | "insufficient-data";
  priorityTier: number;
  severityRank: number;
  sortTimestampUtc: string;
  sortTimestampLabel: string;
  humanReviewRequired: boolean;
}>;

export type OperationsCommandCenterKpiCard = Readonly<{
  id: string;
  label: string;
  value: string;
  hint: string;
  tone: "neutral" | "warning" | "negative";
  scopeState: OperationsCommandCenterScopeState;
}>;

export type OperationsCommandCenterModuleHealth = Readonly<{
  moduleCode: OperationsCommandCenterModuleCode;
  moduleLabel: string;
  availability: OperationsCommandCenterSourceAvailability;
  hint: string;
  countLabel: string | null;
}>;

export type OperationsCommandCenterWorkspacePayload = Readonly<{
  automationDisclaimer: string;
  facilityLabel: string;
  dataConfidenceLabel: string;
  scopeState: OperationsCommandCenterScopeState;
  partialScopeNote: string | null;
  noAccessibleSources: boolean;
  commercialDeepLinkHref: string;
  kpis: readonly OperationsCommandCenterKpiCard[];
  queueItems: readonly OperationsCommandCenterQueueItem[];
  moduleHealth: readonly OperationsCommandCenterModuleHealth[];
  filters: Readonly<{
    windowFrom: string;
    windowTo: string;
    module: string;
    state: string;
  }>;
}>;

export type OperationsCommandCenterFilters = Readonly<{
  windowFrom: string;
  windowTo: string;
  module: string;
  state: string;
}>;
