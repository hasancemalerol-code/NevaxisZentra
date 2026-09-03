import type {
  AiTaskAutomationActionType,
  AiTaskAutomationConditionField,
  AiTaskAutomationConditionOperator,
  AiTaskAutomationTriggerType,
} from "../domain/ai-task-automation";

export const AI_TASK_AUTOMATION_TRIGGER_LABELS: Readonly<
  Record<AiTaskAutomationTriggerType, string>
> = {
  "task-created": "Görev oluşturuldu",
  "task-source-linked": "Kaynak bağlantısı",
  "task-priority-changed": "Öncelik değişti",
  "task-escalated": "Eskalasyon",
  "task-reopened": "Yeniden açıldı",
  "task-closure-requested": "Kapanış talebi",
};

export const AI_TASK_AUTOMATION_CONDITION_FIELD_LABELS: Readonly<
  Record<AiTaskAutomationConditionField, string>
> = {
  sourceModule: "Kaynak modül",
  taskCategory: "Görev kategorisi",
  taskType: "Görev tipi",
  facility: "Tesis",
  department: "Departman",
  priority: "Öncelik",
  severity: "Şiddet",
  escalationLevel: "Eskalasyon seviyesi",
  requiresEvidence: "Kanıt gerekli",
  approvalRequired: "Onay gerekli",
  sourceEntityType: "Kaynak varlık tipi",
  originType: "Kaynak türü",
  currentAssignmentState: "Atama durumu",
};

export const AI_TASK_AUTOMATION_CONDITION_OPERATOR_LABELS: Readonly<
  Record<AiTaskAutomationConditionOperator, string>
> = {
  equals: "eşittir",
  "not-equals": "eşit değil",
  in: "içinde",
  "not-in": "içinde değil",
  gte: "büyük veya eşit",
  lte: "küçük veya eşit",
};

export const AI_TASK_AUTOMATION_ACTION_TYPE_LABELS: Readonly<
  Record<AiTaskAutomationActionType, string>
> = {
  assignToMembership: "Üyeye ata",
  assignToTeam: "Takıma ata",
  routeToQueue: "Kuyruğa yönlendir",
  setPriority: "Öncelik ayarla",
  applySlaPolicy: "SLA politikası uygula",
  applyEscalationPolicy: "Eskalasyon politikası uygula",
  requireEvidence: "Kanıt zorunlu",
  requireClosureApproval: "Kapanış onayı zorunlu",
  enableFourEyes: "Çift göz onayı",
  notifyTeamsChannel: "Teams kanalına bildir",
  addTaskTag: "Etiket ekle",
  setDueDatePolicy: "Son tarih politikası",
  applyClosurePolicy: "Kapanış politikası uygula",
};

export function getAiTaskAutomationTriggerLabel(trigger: string): string {
  return AI_TASK_AUTOMATION_TRIGGER_LABELS[trigger as AiTaskAutomationTriggerType] ?? trigger;
}

export function getAiTaskAutomationConditionFieldLabel(field: string): string {
  return AI_TASK_AUTOMATION_CONDITION_FIELD_LABELS[field as AiTaskAutomationConditionField] ?? field;
}

export function getAiTaskAutomationConditionOperatorLabel(operator: string): string {
  return AI_TASK_AUTOMATION_CONDITION_OPERATOR_LABELS[operator as AiTaskAutomationConditionOperator] ?? operator;
}

export function getAiTaskAutomationActionTypeLabel(type: string): string {
  return AI_TASK_AUTOMATION_ACTION_TYPE_LABELS[type as AiTaskAutomationActionType] ?? type;
}

export function formatAiTaskAutomationScope(rule: Readonly<{
  facilityId: string | null;
  triggerType: string;
}>): string {
  const scope = rule.facilityId ? "Tesis" : "Genel";
  return `${scope} · ${getAiTaskAutomationTriggerLabel(rule.triggerType)}`;
}
