import type {
  AiTaskPlaybookCategory,
  AiTaskPlaybookConditionField,
  AiTaskPlaybookConditionOperator,
  AiTaskPlaybookDependencyState,
  AiTaskPlaybookDependencyType,
  AiTaskPlaybookRunStatus,
  AiTaskPlaybookRunStepStatus,
  AiTaskPlaybookStepType,
  AiTaskPlaybookTriggerSource,
} from "../domain/ai-task-playbook";

export const AI_TASK_PLAYBOOK_CATEGORY_LABELS: Readonly<
  Record<AiTaskPlaybookCategory, string>
> = {
  "machine-incident": "Makine Arızası",
  "quality-nonconformance": "Kalite Uygunsuzluğu",
  "material-shortage": "Malzeme Eksikliği",
  "critical-order-delay": "Kritik Sipariş Gecikmesi",
  "energy-anomaly": "Enerji Anomalisi",
  "shift-handover-critical": "Kritik Vardiya Teslimi",
};

export const AI_TASK_PLAYBOOK_STEP_TYPE_LABELS: Readonly<
  Record<AiTaskPlaybookStepType, string>
> = {
  task: "Görev",
  approval: "Onay",
  verification: "Doğrulama",
  "decision-gate": "Karar Kapısı",
};

export const AI_TASK_PLAYBOOK_DEPENDENCY_TYPE_LABELS: Readonly<
  Record<AiTaskPlaybookDependencyType, string>
> = {
  "required-predecessor": "Zorunlu öncül",
  "optional-predecessor": "İsteğe bağlı öncül",
};

export const AI_TASK_PLAYBOOK_RUN_STATUS_LABELS: Readonly<
  Record<AiTaskPlaybookRunStatus, string>
> = {
  pending: "Bekliyor",
  running: "Devam Ediyor",
  blocked: "Bloke",
  "awaiting-approval": "Onay Bekliyor",
  completed: "Tamamlandı",
  cancelled: "İptal",
  failed: "Başarısız",
};

export const AI_TASK_PLAYBOOK_RUN_STEP_STATUS_LABELS: Readonly<
  Record<AiTaskPlaybookRunStepStatus, string>
> = {
  pending: "Bekliyor",
  ready: "Hazır",
  "in-progress": "Devam Ediyor",
  "awaiting-approval": "Onay Bekliyor",
  blocked: "Bloke",
  completed: "Tamamlandı",
  cancelled: "İptal",
  failed: "Başarısız",
  skipped: "Atlandı",
};

export const AI_TASK_PLAYBOOK_DEPENDENCY_STATE_LABELS: Readonly<
  Record<AiTaskPlaybookDependencyState, string>
> = {
  ready: "Hazır",
  waiting: "Bekliyor",
  blocked: "Bloke",
};

export const AI_TASK_PLAYBOOK_TRIGGER_SOURCE_LABELS: Readonly<
  Record<AiTaskPlaybookTriggerSource, string>
> = {
  manual: "Manuel",
  automation: "Otomasyon",
  "ai-recommendation": "AI Önerisi",
  "shift-handover": "Vardiya Teslimi",
};

export const AI_TASK_PLAYBOOK_CONDITION_FIELD_LABELS: Readonly<
  Record<AiTaskPlaybookConditionField, string>
> = {
  sparePartRequired: "Yedek parça gerekli",
  qualityImpact: "Kalite etkisi",
  downtimeMinutes: "Duruş süresi (dk)",
};

export const AI_TASK_PLAYBOOK_CONDITION_OPERATOR_LABELS: Readonly<
  Record<AiTaskPlaybookConditionOperator, string>
> = {
  equals: "eşittir",
  gte: "büyük veya eşit",
};

export function getAiTaskPlaybookCategoryLabel(category: string): string {
  return AI_TASK_PLAYBOOK_CATEGORY_LABELS[category as AiTaskPlaybookCategory] ?? category;
}

export function getAiTaskPlaybookStepTypeLabel(stepType: string): string {
  return AI_TASK_PLAYBOOK_STEP_TYPE_LABELS[stepType as AiTaskPlaybookStepType] ?? stepType;
}

export function getAiTaskPlaybookDependencyTypeLabel(dependencyType: string): string {
  return AI_TASK_PLAYBOOK_DEPENDENCY_TYPE_LABELS[dependencyType as AiTaskPlaybookDependencyType] ?? dependencyType;
}

export function getAiTaskPlaybookRunStatusLabel(status: string): string {
  return AI_TASK_PLAYBOOK_RUN_STATUS_LABELS[status as AiTaskPlaybookRunStatus] ?? status;
}

export function getAiTaskPlaybookRunStepStatusLabel(status: string): string {
  return AI_TASK_PLAYBOOK_RUN_STEP_STATUS_LABELS[status as AiTaskPlaybookRunStepStatus] ?? status;
}

export function getAiTaskPlaybookDependencyStateLabel(state: string): string {
  return AI_TASK_PLAYBOOK_DEPENDENCY_STATE_LABELS[state as AiTaskPlaybookDependencyState] ?? state;
}

export function getAiTaskPlaybookTriggerSourceLabel(source: string): string {
  return AI_TASK_PLAYBOOK_TRIGGER_SOURCE_LABELS[source as AiTaskPlaybookTriggerSource] ?? source;
}

export function getAiTaskPlaybookConditionFieldLabel(field: string): string {
  return AI_TASK_PLAYBOOK_CONDITION_FIELD_LABELS[field as AiTaskPlaybookConditionField] ?? field;
}

export function getAiTaskPlaybookConditionOperatorLabel(operator: string): string {
  return AI_TASK_PLAYBOOK_CONDITION_OPERATOR_LABELS[operator as AiTaskPlaybookConditionOperator] ?? operator;
}

export function getAiTaskPlaybookRunStatusTone(
  status: string,
): "neutral" | "information" | "success" | "warning" | "danger" | "ai" {
  switch (status) {
    case "completed":
      return "success";
    case "running":
      return "information";
    case "awaiting-approval":
      return "warning";
    case "blocked":
    case "failed":
      return "danger";
    case "pending":
      return "neutral";
    case "cancelled":
      return "neutral";
    default:
      return "neutral";
  }
}

export function getAiTaskPlaybookRunStepStatusTone(
  status: string,
): "neutral" | "information" | "success" | "warning" | "danger" | "ai" {
  switch (status) {
    case "completed":
      return "success";
    case "in-progress":
      return "information";
    case "ready":
      return "ai";
    case "awaiting-approval":
      return "warning";
    case "blocked":
    case "failed":
      return "danger";
    case "pending":
    case "cancelled":
    case "skipped":
      return "neutral";
    default:
      return "neutral";
  }
}

export function getAiTaskPlaybookRunStepVisualStateClass(status: string): string {
  switch (status) {
    case "completed":
      return "ai-task-playbook-step--completed";
    case "in-progress":
      return "ai-task-playbook-step--in-progress";
    case "ready":
      return "ai-task-playbook-step--ready";
    case "pending":
      return "ai-task-playbook-step--pending";
    case "blocked":
      return "ai-task-playbook-step--blocked";
    case "awaiting-approval":
      return "ai-task-playbook-step--awaiting-approval";
    case "cancelled":
    case "skipped":
      return "ai-task-playbook-step--cancelled";
    case "failed":
      return "ai-task-playbook-step--failed";
    default:
      return "ai-task-playbook-step--pending";
  }
}

export function formatAiTaskPlaybookProgress(
  completed: number,
  total: number,
): string {
  if (total <= 0) return "—";
  return `${completed}/${total}`;
}
