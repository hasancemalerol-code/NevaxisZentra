export type AiTaskAutomationTriggerType =
  | "task-created"
  | "task-source-linked"
  | "task-priority-changed"
  | "task-escalated"
  | "task-reopened"
  | "task-closure-requested";

export type AiTaskAutomationConditionField =
  | "sourceModule"
  | "taskCategory"
  | "taskType"
  | "facility"
  | "department"
  | "priority"
  | "severity"
  | "escalationLevel"
  | "requiresEvidence"
  | "approvalRequired"
  | "sourceEntityType"
  | "originType"
  | "currentAssignmentState";

export type AiTaskAutomationConditionOperator =
  | "equals"
  | "not-equals"
  | "in"
  | "not-in"
  | "gte"
  | "lte";

export type AiTaskAutomationActionType =
  | "assignToMembership"
  | "assignToTeam"
  | "routeToQueue"
  | "setPriority"
  | "applySlaPolicy"
  | "applyEscalationPolicy"
  | "requireEvidence"
  | "requireClosureApproval"
  | "enableFourEyes"
  | "notifyTeamsChannel"
  | "addTaskTag"
  | "setDueDatePolicy"
  | "applyClosurePolicy";

export type AiTaskAutomationCondition = Readonly<{
  field: string;
  operator: string;
  value: string | null;
  values: readonly string[] | null;
}>;

export type AiTaskAutomationAction = Readonly<{
  type: string;
  value: string | null;
  guidValue: string | null;
  intValue: number | null;
  boolValue: boolean | null;
  values: readonly string[] | null;
}>;

export type AiTaskAutomationRuleSummary = Readonly<{
  id: string;
  ruleKey: string;
  name: string;
  description: string | null;
  facilityId: string | null;
  triggerType: string;
  sortOrder: number;
  enabled: boolean;
  version: number;
  updatedAtUtc: string;
  lastMatchedAtUtc: string | null;
  actionSummaries: readonly string[];
}>;

export type AiTaskAutomationRuleDetail = Readonly<{
  id: string;
  ruleKey: string;
  name: string;
  description: string | null;
  facilityId: string | null;
  triggerType: string;
  sortOrder: number;
  enabled: boolean;
  version: number;
  conditions: readonly AiTaskAutomationCondition[];
  actions: readonly AiTaskAutomationAction[];
  effectiveFromUtc: string | null;
  effectiveUntilUtc: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
  lastMatchedAtUtc: string | null;
  createdBy: string;
  updatedBy: string;
}>;

export type AiTaskAutomationRuleRevision = Readonly<{
  id: string;
  version: number;
  name: string;
  enabled: boolean;
  changedBy: string;
  changedAtUtc: string;
  changeReason: string;
}>;

export type AiTaskAutomationExecution = Readonly<{
  id: string;
  taskId: string;
  ruleId: string;
  ruleName: string;
  ruleVersion: number;
  triggerType: string;
  executedAtUtc: string;
  actionsApplied: readonly string[];
  simulated: boolean;
}>;

export type AiTaskAutomationMatchedRule = Readonly<{
  ruleId: string;
  ruleName: string;
  ruleVersion: number;
  sortOrder: number;
  matchedConditions: readonly AiTaskAutomationCondition[];
}>;

export type AiTaskAutomationSkippedRule = Readonly<{
  ruleId: string;
  ruleName: string;
  reason: string;
}>;

export type AiTaskAutomationProposedChange = Readonly<{
  field: string;
  currentValue: string | null;
  proposedValue: string | null;
  sourceRuleId: string | null;
  sourceRuleName: string | null;
}>;

export type AiTaskAutomationConflict = Readonly<{
  field: string;
  competingValues: readonly string[];
  resolution: string;
  winningRuleId: string | null;
}>;

export type AiTaskAutomationEvaluation = Readonly<{
  triggerType: string;
  matchedRules: readonly AiTaskAutomationMatchedRule[];
  skippedRules: readonly AiTaskAutomationSkippedRule[];
  proposedChanges: readonly AiTaskAutomationProposedChange[];
  conflicts: readonly AiTaskAutomationConflict[];
  actionsApplied: readonly AiTaskAutomationAction[];
  simulated: boolean;
  outcomeSummary: string;
}>;

export type CreateAiTaskAutomationRuleInput = Readonly<{
  ruleKey: string;
  name: string;
  description?: string | null;
  triggerType: string;
  sortOrder: number;
  enabled: boolean;
  conditions: readonly AiTaskAutomationCondition[];
  actions: readonly AiTaskAutomationAction[];
  effectiveFromUtc?: string | null;
  effectiveUntilUtc?: string | null;
}>;

export type UpdateAiTaskAutomationRuleInput = Readonly<{
  expectedVersion: number;
  name: string;
  description?: string | null;
  triggerType: string;
  sortOrder: number;
  enabled: boolean;
  conditions: readonly AiTaskAutomationCondition[];
  actions: readonly AiTaskAutomationAction[];
  effectiveFromUtc?: string | null;
  effectiveUntilUtc?: string | null;
  changeReason: string;
}>;

export const AI_TASK_AUTOMATION_TRIGGER_TYPES: readonly AiTaskAutomationTriggerType[] = [
  "task-created",
  "task-source-linked",
  "task-priority-changed",
  "task-escalated",
  "task-reopened",
  "task-closure-requested",
];

export const AI_TASK_AUTOMATION_CONDITION_FIELDS: readonly AiTaskAutomationConditionField[] = [
  "sourceModule",
  "taskCategory",
  "taskType",
  "facility",
  "department",
  "priority",
  "severity",
  "escalationLevel",
  "requiresEvidence",
  "approvalRequired",
  "sourceEntityType",
  "originType",
  "currentAssignmentState",
];

export const AI_TASK_AUTOMATION_CONDITION_OPERATORS: readonly AiTaskAutomationConditionOperator[] = [
  "equals",
  "not-equals",
  "in",
  "not-in",
  "gte",
  "lte",
];

export const AI_TASK_AUTOMATION_ACTION_TYPES: readonly AiTaskAutomationActionType[] = [
  "assignToMembership",
  "assignToTeam",
  "routeToQueue",
  "setPriority",
  "applySlaPolicy",
  "applyEscalationPolicy",
  "requireEvidence",
  "requireClosureApproval",
  "enableFourEyes",
  "notifyTeamsChannel",
  "addTaskTag",
  "setDueDatePolicy",
  "applyClosurePolicy",
];
