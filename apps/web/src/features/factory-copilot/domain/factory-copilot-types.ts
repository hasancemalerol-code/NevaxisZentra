export type FactoryCopilotEntryPoint =
  | "BUGUN"
  | "FABRIKA"
  | "PLAN"
  | "ISLER"
  | "SCENARIO"
  | "MACHINE"
  | "LINE"
  | "ORDER";

export type FactoryCopilotContextInput = Readonly<{
  entryPoint: FactoryCopilotEntryPoint;
  lineId?: string | null;
  lineCode?: string | null;
  resourceId?: string | null;
  resourceCode?: string | null;
  orderReference?: string | null;
  digitalWorkCaseId?: string | null;
  scenarioRunId?: string | null;
  searchQuery?: string | null;
}>;

export type FactoryCopilotEvidenceRef = Readonly<{
  kind: string;
  referenceId: string;
  label: string;
  detail?: string | null;
}>;

export type FactoryCopilotActionLink = Readonly<{
  label: string;
  href: string;
  linkType: string;
}>;

export type FactoryCopilotStructuredResponse = Readonly<{
  answer: string;
  whatUsed?: string | null;
  keyFacts: readonly string[];
  causes: readonly string[];
  impact?: string | null;
  recommendedNextStep?: string | null;
  uncertainty?: string | null;
  evidence: readonly FactoryCopilotEvidenceRef[];
  relatedActions: readonly FactoryCopilotActionLink[];
}>;

export type FactoryCopilotContextHeader = Readonly<{
  entryPoint: string;
  factoryLabel?: string | null;
  lineLabel?: string | null;
  resourceLabel?: string | null;
  orderLabel?: string | null;
  workLabel?: string | null;
  scenarioLabel?: string | null;
}>;

export type FactoryCopilotQueryResult = Readonly<{
  intent: string;
  routingClass: string;
  modelInvoked: boolean;
  failureState?: string | null;
  response: FactoryCopilotStructuredResponse;
  provenance: Readonly<{
    contextSnapshotId?: string | null;
    tools: readonly Readonly<{
      toolId: string;
      accessClass: string;
      succeeded: boolean;
      summary: string;
      latencyMs: number;
    }>[];
    recordsReferenced: readonly string[];
    providerId?: string | null;
    modelName?: string | null;
    modelInvoked: boolean;
    routingClass: string;
    routingReason: string;
    contextQualityState: string;
    totalLatencyMs: number;
  }>;
  suggestedQuestions: readonly string[];
  contextHeader: FactoryCopilotContextHeader;
  queryId: string;
  createdAtUtc: string;
}>;

export type FactoryCopilotSuggestedQuestionsResult = Readonly<{
  questions: readonly string[];
  contextHeader: FactoryCopilotContextHeader;
}>;

export type FactoryCopilotQueryRequest = Readonly<{
  queryText: string;
  context: FactoryCopilotContextInput;
  sessionId?: string | null;
}>;
