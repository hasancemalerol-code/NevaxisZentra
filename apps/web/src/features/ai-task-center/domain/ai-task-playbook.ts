export type AiTaskPlaybookCategory =
  | "machine-incident"
  | "quality-nonconformance"
  | "material-shortage"
  | "critical-order-delay"
  | "energy-anomaly"
  | "shift-handover-critical";

export type AiTaskPlaybookStepType =
  | "task"
  | "approval"
  | "verification"
  | "decision-gate";

export type AiTaskPlaybookDependencyType =
  | "required-predecessor"
  | "optional-predecessor";

export type AiTaskPlaybookRunStatus =
  | "pending"
  | "running"
  | "blocked"
  | "awaiting-approval"
  | "completed"
  | "cancelled"
  | "failed";

export type AiTaskPlaybookRunStepStatus =
  | "pending"
  | "ready"
  | "in-progress"
  | "awaiting-approval"
  | "blocked"
  | "completed"
  | "cancelled"
  | "failed"
  | "skipped";

export type AiTaskPlaybookDependencyState =
  | "ready"
  | "waiting"
  | "blocked";

export type AiTaskPlaybookTriggerSource =
  | "manual"
  | "automation"
  | "ai-recommendation"
  | "shift-handover";

export type AiTaskPlaybookConditionField =
  | "sparePartRequired"
  | "qualityImpact"
  | "downtimeMinutes";

export type AiTaskPlaybookConditionOperator = "equals" | "gte";

export type AiTaskPlaybookCondition = Readonly<{
  field: string;
  operator: string;
  value: string;
}>;

export type AiTaskPlaybookStepDefinition = Readonly<{
  stepKey: string;
  name: string;
  description: string | null;
  sortOrder: number;
  stepType: string;
  required: boolean;
  defaultPriority: string;
  assignmentPolicyJson: string | null;
  slaPolicyJson: string | null;
  evidencePolicyJson: string | null;
  closurePolicyJson: string | null;
  condition: AiTaskPlaybookCondition | null;
  targetDurationMinutes: number | null;
}>;

export type AiTaskPlaybookDependencyDefinition = Readonly<{
  stepKey: string;
  dependsOnStepKey: string;
  dependencyType: string;
}>;

export type AiTaskPlaybookSummary = Readonly<{
  id: string;
  playbookKey: string;
  name: string;
  category: string;
  facilityId: string | null;
  version: number;
  enabled: boolean;
  stepCount: number;
  updatedAtUtc: string;
  lastRunAtUtc: string | null;
}>;

export type AiTaskPlaybookStep = Readonly<{
  id: string;
  stepKey: string;
  name: string;
  description: string | null;
  sortOrder: number;
  stepType: string;
  required: boolean;
  defaultPriority: string;
  assignmentPolicyJson: string | null;
  slaPolicyJson: string | null;
  evidencePolicyJson: string | null;
  closurePolicyJson: string | null;
  condition: AiTaskPlaybookCondition | null;
  targetDurationMinutes: number | null;
}>;

export type AiTaskPlaybookDependency = Readonly<{
  id: string;
  stepKey: string;
  dependsOnStepKey: string;
  dependencyType: string;
}>;

export type AiTaskPlaybookDetail = Readonly<{
  id: string;
  playbookKey: string;
  name: string;
  description: string | null;
  category: string;
  triggerContext: string | null;
  facilityId: string | null;
  version: number;
  enabled: boolean;
  effectiveFromUtc: string | null;
  effectiveUntilUtc: string | null;
  steps: readonly AiTaskPlaybookStep[];
  dependencies: readonly AiTaskPlaybookDependency[];
  createdAtUtc: string;
  updatedAtUtc: string;
}>;

export type AiTaskPlaybookRunSummary = Readonly<{
  id: string;
  playbookId: string;
  playbookName: string;
  status: string;
  facilityId: string;
  triggerSource: string;
  sourceTaskId: string | null;
  requiredStepsCompleted: number;
  requiredStepsTotal: number;
  optionalStepsCompleted: number;
  optionalStepsTotal: number;
  blockedStepsCount: number;
  approvalsPending: number;
  atRisk: boolean;
  startedAtUtc: string;
  completedAtUtc: string | null;
}>;

export type AiTaskPlaybookRunStep = Readonly<{
  id: string;
  stepKey: string;
  name: string;
  required: boolean;
  stepType: string;
  status: string;
  dependencyState: string;
  linkedTaskId: string | null;
  blockReason: string | null;
  sortOrder: number;
  readyAtUtc: string | null;
  startedAtUtc: string | null;
  completedAtUtc: string | null;
}>;

export type AiTaskPlaybookRunDetail = Readonly<{
  id: string;
  playbookId: string;
  playbookName: string;
  playbookVersion: number;
  status: string;
  facilityId: string;
  triggerSource: string;
  sourceTaskId: string | null;
  sourceEntityJson: string | null;
  parentTaskId: string | null;
  requiredStepsCompleted: number;
  requiredStepsTotal: number;
  optionalStepsCompleted: number;
  optionalStepsTotal: number;
  blockedStepsCount: number;
  approvalsPending: number;
  atRisk: boolean;
  blockReason: string | null;
  criticalPathJson: string | null;
  steps: readonly AiTaskPlaybookRunStep[];
  startedAtUtc: string;
  completedAtUtc: string | null;
  cancelledAtUtc: string | null;
  cancellationReason: string | null;
}>;

export type AiTaskPlaybookGraphValidationResult = Readonly<{
  isValid: boolean;
  errors: readonly string[];
}>;

export type CreateAiTaskPlaybookInput = Readonly<{
  playbookKey: string;
  name: string;
  description?: string | null;
  category: string;
  triggerContext?: string | null;
  enabled: boolean;
  effectiveFromUtc?: string | null;
  effectiveUntilUtc?: string | null;
  steps: readonly AiTaskPlaybookStepDefinition[];
  dependencies: readonly AiTaskPlaybookDependencyDefinition[];
}>;

export type UpdateAiTaskPlaybookInput = Readonly<{
  expectedVersion: number;
  name: string;
  description?: string | null;
  category: string;
  triggerContext?: string | null;
  enabled: boolean;
  effectiveFromUtc?: string | null;
  effectiveUntilUtc?: string | null;
  steps: readonly AiTaskPlaybookStepDefinition[];
  dependencies: readonly AiTaskPlaybookDependencyDefinition[];
  changeReason: string;
}>;

export type StartAiTaskPlaybookRunInput = Readonly<{
  idempotencyKey?: string | null;
  triggerSource?: string | null;
  sourceTaskId?: string | null;
  sourceEntityJson?: string | null;
  correlationId?: string | null;
  contextValues?: Readonly<Record<string, string>> | null;
}>;

export const AI_TASK_PLAYBOOK_CATEGORIES: readonly AiTaskPlaybookCategory[] = [
  "machine-incident",
  "quality-nonconformance",
  "material-shortage",
  "critical-order-delay",
  "energy-anomaly",
  "shift-handover-critical",
];

export const AI_TASK_PLAYBOOK_STEP_TYPES: readonly AiTaskPlaybookStepType[] = [
  "task",
  "approval",
  "verification",
  "decision-gate",
];

export const AI_TASK_PLAYBOOK_DEPENDENCY_TYPES: readonly AiTaskPlaybookDependencyType[] = [
  "required-predecessor",
  "optional-predecessor",
];

export const AI_TASK_PLAYBOOK_RUN_STATUSES: readonly AiTaskPlaybookRunStatus[] = [
  "pending",
  "running",
  "blocked",
  "awaiting-approval",
  "completed",
  "cancelled",
  "failed",
];

export const AI_TASK_PLAYBOOK_ACTIVE_RUN_STATUSES: readonly AiTaskPlaybookRunStatus[] = [
  "pending",
  "running",
  "blocked",
  "awaiting-approval",
];

export const AI_TASK_PLAYBOOK_HISTORY_RUN_STATUSES: readonly AiTaskPlaybookRunStatus[] = [
  "completed",
  "cancelled",
  "failed",
];

export const AI_TASK_PLAYBOOK_TRIGGER_SOURCES: readonly AiTaskPlaybookTriggerSource[] = [
  "manual",
  "automation",
  "ai-recommendation",
  "shift-handover",
];

export const AI_TASK_PLAYBOOK_CONDITION_FIELDS: readonly AiTaskPlaybookConditionField[] = [
  "sparePartRequired",
  "qualityImpact",
  "downtimeMinutes",
];

export const AI_TASK_PLAYBOOK_CONDITION_OPERATORS: readonly AiTaskPlaybookConditionOperator[] = [
  "equals",
  "gte",
];
