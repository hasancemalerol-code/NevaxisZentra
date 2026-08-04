import type {
  AiTaskActionType,
  AiTaskApprovalState,
  AiTaskCategory,
  AiTaskPriority,
  AiTaskRiskLevel,
  AiTaskStatus,
} from "../domain/ai-task";

export const AI_TASK_CATEGORY_LABELS: Readonly<
  Record<AiTaskCategory, string>
> = {
  "production-planning": "Üretim Planlama",
  "live-production": "Canlı Üretim",
  maintenance: "Bakım",
  quality: "Kalite",
  "material-risk": "Malzeme Riski",
  "supply-risk": "Tedarik Riski",
  "commercial-intelligence": "Ticari Zekâ",
  energy: "Enerji",
  capacity: "Kapasite",
  "delivery-risk": "Sipariş ve Termin Riski",
  "cost-deviation": "Maliyet Sapması",
  "erp-integration": "ERP Entegrasyonu",
};

export const AI_TASK_PRIORITY_LABELS: Readonly<
  Record<AiTaskPriority, string>
> = {
  low: "Düşük",
  medium: "Orta",
  high: "Yüksek",
  critical: "Kritik",
};

export const AI_TASK_STATUS_LABELS: Readonly<
  Record<AiTaskStatus, string>
> = {
  draft: "Taslak",
  "awaiting-review": "İnceleme Bekliyor",
  "awaiting-approval": "Onay Bekliyor",
  approved: "Onaylandı",
  "in-progress": "Uygulanıyor",
  completed: "Tamamlandı",
  rejected: "Reddedildi",
  deferred: "Ertelendi",
  failed: "Başarısız",
  cancelled: "İptal Edildi",
};

export const AI_TASK_RISK_LEVEL_LABELS: Readonly<
  Record<AiTaskRiskLevel, string>
> = {
  information: "Bilgilendirme",
  low: "Düşük Risk",
  medium: "Orta Risk",
  high: "Yüksek Risk",
  critical: "Kritik Risk",
};

export const AI_TASK_APPROVAL_STATE_LABELS: Readonly<
  Record<AiTaskApprovalState, string>
> = {
  "not-required": "Onay Gerekmiyor",
  required: "Onay Gerekli",
  pending: "Onay Bekliyor",
  approved: "Onaylandı",
  rejected: "Reddedildi",
};

export const AI_TASK_ACTION_TYPE_LABELS: Readonly<
  Record<AiTaskActionType, string>
> = {
  review: "İncele",
  "create-draft": "Taslak Oluştur",
  "update-plan": "Planı Güncelle",
  "assign-user": "Kullanıcı Ata",
  "create-maintenance-request": "Bakım Talebi Oluştur",
  "create-quality-record": "Kalite Kaydı Oluştur",
  "create-purchase-request": "Satın Alma Talebi Oluştur",
  "create-transfer-request": "Transfer Talebi Oluştur",
  "send-notification": "Bildirim Gönder",
  "sync-to-erp": "ERP'ye Aktar",
};

export type AiTaskVisualTone =
  | "neutral"
  | "information"
  | "success"
  | "warning"
  | "danger"
  | "ai";

export const AI_TASK_PRIORITY_TONES: Readonly<
  Record<AiTaskPriority, AiTaskVisualTone>
> = {
  low: "neutral",
  medium: "information",
  high: "warning",
  critical: "danger",
};

export const AI_TASK_RISK_LEVEL_TONES: Readonly<
  Record<AiTaskRiskLevel, AiTaskVisualTone>
> = {
  information: "information",
  low: "neutral",
  medium: "warning",
  high: "warning",
  critical: "danger",
};

export const AI_TASK_STATUS_TONES: Readonly<
  Record<AiTaskStatus, AiTaskVisualTone>
> = {
  draft: "neutral",
  "awaiting-review": "information",
  "awaiting-approval": "warning",
  approved: "success",
  "in-progress": "ai",
  completed: "success",
  rejected: "danger",
  deferred: "warning",
  failed: "danger",
  cancelled: "neutral",
};

export function getAiTaskCategoryLabel(
  category: AiTaskCategory,
): string {
  return AI_TASK_CATEGORY_LABELS[category];
}

export function getAiTaskPriorityLabel(
  priority: AiTaskPriority,
): string {
  return AI_TASK_PRIORITY_LABELS[priority];
}

export function getAiTaskStatusLabel(
  status: AiTaskStatus,
): string {
  return AI_TASK_STATUS_LABELS[status];
}

export function getAiTaskRiskLevelLabel(
  riskLevel: AiTaskRiskLevel,
): string {
  return AI_TASK_RISK_LEVEL_LABELS[riskLevel];
}

export function getAiTaskApprovalStateLabel(
  approvalState: AiTaskApprovalState,
): string {
  return AI_TASK_APPROVAL_STATE_LABELS[approvalState];
}

export function getAiTaskActionTypeLabel(
  actionType: AiTaskActionType,
): string {
  return AI_TASK_ACTION_TYPE_LABELS[actionType];
}