import type {
  ProductionPlanApprovalStatus,
  ProductionPlanConfidence,
  ProductionPlanOrigin,
  ProductionPlanPriority,
  ProductionPlanRiskSeverity,
  ProductionPlanRiskType,
  ProductionPlanStatus,
  ProductionPlanTargetSystem,
  ProductionPlanTransfer,
} from "../domain/production-plan";

export const PRODUCTION_PLAN_STATUS_LABELS = {
  draft: "Taslak",
  "needs-review": "İnceleme gerekiyor",
  "ready-for-approval": "Onaya hazır",
  approved: "Onaylandı",
  rejected: "Reddedildi",
  scheduled: "Planlandı",
  "in-progress": "Üretimde",
  completed: "Tamamlandı",
  cancelled: "İptal edildi",
} satisfies Record<
  ProductionPlanStatus,
  string
>;

export const PRODUCTION_PLAN_ORIGIN_LABELS = {
  "approved-order": "Onaylı sipariş",
  "erp-production-request":
    "ERP üretim talebi",
  manual: "Manuel plan",
  "ai-recommendation": "AI plan önerisi",
} satisfies Record<
  ProductionPlanOrigin,
  string
>;

export const PRODUCTION_PLAN_PRIORITY_LABELS = {
  low: "Düşük",
  normal: "Normal",
  high: "Yüksek",
  urgent: "Acil",
} satisfies Record<
  ProductionPlanPriority,
  string
>;

export const PRODUCTION_PLAN_CONFIDENCE_LABELS = {
  low: "Düşük güven",
  medium: "Orta güven",
  high: "Yüksek güven",
} satisfies Record<
  ProductionPlanConfidence,
  string
>;

export const PRODUCTION_PLAN_RISK_TYPE_LABELS = {
  "capacity-shortage": "Kapasite yetersizliği",
  "machine-conflict": "Makine çakışması",
  "mold-conflict": "Kalıp çakışması",
  "material-shortage": "Hammadde yetersizliği",
  "operator-shortage": "Operatör yetersizliği",
  "delivery-risk": "Termin riski",
  "quality-risk": "Kalite riski",
  "maintenance-risk": "Bakım riski",
  "data-missing": "Eksik veri",
  "data-conflict": "Çelişkili veri",
} satisfies Record<
  ProductionPlanRiskType,
  string
>;

export const PRODUCTION_PLAN_RISK_SEVERITY_LABELS = {
  information: "Bilgilendirme",
  warning: "Uyarı",
  critical: "Kritik",
} satisfies Record<
  ProductionPlanRiskSeverity,
  string
>;

export const PRODUCTION_PLAN_APPROVAL_STATUS_LABELS = {
  "not-required": "Onay gerekmiyor",
  pending: "Onay bekliyor",
  approved: "Onaylandı",
  rejected: "Reddedildi",
  expired: "Onay süresi doldu",
} satisfies Record<
  ProductionPlanApprovalStatus,
  string
>;

export const PRODUCTION_PLAN_TRANSFER_STATUS_LABELS = {
  "not-started": "Başlatılmadı",
  pending: "Aktarım bekliyor",
  "in-progress": "Aktarılıyor",
  completed: "Aktarım tamamlandı",
  failed: "Aktarım başarısız",
  uncertain: "Belirsiz",
} satisfies Record<
  ProductionPlanTransfer["status"],
  string
>;

export const PRODUCTION_PLAN_TARGET_SYSTEM_LABELS = {
  erp: "ERP",
  mes: "MES",
  crevafactos: "NEVAXIS FACTOS",
} satisfies Record<
  ProductionPlanTargetSystem,
  string
>;

export function getProductionPlanStatusLabel(
  status: ProductionPlanStatus,
): string {
  return PRODUCTION_PLAN_STATUS_LABELS[status];
}

export function getProductionPlanOriginLabel(
  origin: ProductionPlanOrigin,
): string {
  return PRODUCTION_PLAN_ORIGIN_LABELS[origin];
}

export function getProductionPlanPriorityLabel(
  priority: ProductionPlanPriority,
): string {
  return PRODUCTION_PLAN_PRIORITY_LABELS[
    priority
  ];
}

export function getProductionPlanConfidenceLabel(
  confidence: ProductionPlanConfidence,
): string {
  return PRODUCTION_PLAN_CONFIDENCE_LABELS[
    confidence
  ];
}

export function getProductionPlanRiskTypeLabel(
  riskType: ProductionPlanRiskType,
): string {
  return PRODUCTION_PLAN_RISK_TYPE_LABELS[
    riskType
  ];
}

export function getProductionPlanRiskSeverityLabel(
  severity: ProductionPlanRiskSeverity,
): string {
  return PRODUCTION_PLAN_RISK_SEVERITY_LABELS[
    severity
  ];
}

export function getProductionPlanApprovalStatusLabel(
  status: ProductionPlanApprovalStatus,
): string {
  return PRODUCTION_PLAN_APPROVAL_STATUS_LABELS[
    status
  ];
}

export function getProductionPlanTransferStatusLabel(
  status: ProductionPlanTransfer["status"],
): string {
  return PRODUCTION_PLAN_TRANSFER_STATUS_LABELS[
    status
  ];
}

export function getProductionPlanTargetSystemLabel(
  targetSystem:
    | ProductionPlanTargetSystem
    | undefined,
): string {
  if (!targetSystem) {
    return "Hedef sistem belirlenmedi";
  }

  return PRODUCTION_PLAN_TARGET_SYSTEM_LABELS[
    targetSystem
  ];
}