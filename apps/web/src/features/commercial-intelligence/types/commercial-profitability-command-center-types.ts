import type { CommercialProfitabilityKpiCard } from "./commercial-profitability-types";

export type CommercialProfitabilityCommandCenterScopeState =
  | "complete"
  | "partial"
  | "insufficient";

export type CommercialProfitabilityCommandCenterKpiCard = CommercialProfitabilityKpiCard &
  Readonly<{
    scopeState?: CommercialProfitabilityCommandCenterScopeState;
  }>;

export type CommercialProfitabilityCommandCenterSourceType =
  | "alert"
  | "pricing-scenario"
  | "ai-task"
  | "evidence-review";

export type CommercialProfitabilityCommandCenterQueueItem = Readonly<{
  id: string;
  sourceType: CommercialProfitabilityCommandCenterSourceType;
  sourceTypeLabel: string;
  title: string;
  severityOrStatusLabel: string;
  evidenceStateLabel: string;
  ownerActionLabel: string;
  humanDecisionRequired: boolean;
  deepLinkHref: string;
  linkedTaskHref: string | null;
  priorityTier: number;
  severityRank: number;
  sortTimestampUtc: string;
  sortTimestampLabel: string;
}>;

export type CommercialProfitabilityCommandCenterLifecycleSnapshot = Readonly<{
  label: string;
  count: number | null;
  scopeState: CommercialProfitabilityCommandCenterScopeState;
  deepLinkHref: string;
}>;

export type CommercialProfitabilityCommandCenterLensLink = Readonly<{
  id: string;
  label: string;
  hint: string;
  deepLinkHref: string;
  previewCount: number | null;
  scopeState: CommercialProfitabilityCommandCenterScopeState;
}>;

export type CommercialProfitabilityCommandCenterDataConfidence = Readonly<{
  scopeLabel: string;
  timeWindowLabel: string;
  facilityLabel: string;
  lastRefreshLabel: string;
  partialAggregationNotes: readonly string[];
  evidenceInsufficiencyNotes: readonly string[];
  sourceConflictNotes: readonly string[];
}>;

export type CommercialProfitabilityCommandCenterWorkspacePayload = Readonly<{
  facilityLabel: string;
  facilityId: string;
  timeWindowLabel: string;
  windowFrom: string;
  windowTo: string;
  ruleSetVersion: string;
  provenanceDisclaimer: string;
  automationDisclaimer: string;
  kpiCards: readonly CommercialProfitabilityCommandCenterKpiCard[];
  priorityQueue: readonly CommercialProfitabilityCommandCenterQueueItem[];
  profitabilityLenses: readonly CommercialProfitabilityCommandCenterLensLink[];
  pricingLifecycleSnapshots: readonly CommercialProfitabilityCommandCenterLifecycleSnapshot[];
  alertLifecycleSnapshots: readonly CommercialProfitabilityCommandCenterLifecycleSnapshot[];
  dataConfidence: CommercialProfitabilityCommandCenterDataConfidence;
  canReadAiTasks: boolean;
  aiTaskScopeState: CommercialProfitabilityCommandCenterScopeState;
}>;
