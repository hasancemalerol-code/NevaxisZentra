export function getMoldMaintenanceTypeLabel(type: string): string {
  switch (type) {
    case "preventive":
      return "Önleyici";
    case "corrective":
      return "Düzeltici";
    case "cleaning":
      return "Temizlik";
    case "inspection":
      return "Kontrol";
    case "repair":
      return "Onarım";
    case "overhaul":
      return "Revizyon Bakım";
    default:
      return "Diğer";
  }
}

export function getMoldMaintenanceStatusLabel(status: string): string {
  switch (status) {
    case "planned":
      return "Planlandı";
    case "in-progress":
      return "Devam Ediyor";
    case "completed":
      return "Tamamlandı";
    case "cancelled":
      return "İptal Edildi";
    default:
      return "Bilinmiyor";
  }
}

export function getMoldRevisionTypeLabel(type: string): string {
  switch (type) {
    case "design-change":
      return "Tasarım Değişikliği";
    case "cavity-change":
      return "Gözenek Değişikliği";
    case "material-change":
      return "Malzeme Değişikliği";
    case "cooling-change":
      return "Soğutma Değişikliği";
    case "runner-change":
      return "Yolluk Değişikliği";
    case "repair-improvement":
      return "Onarım İyileştirmesi";
    case "customer-change":
      return "Müşteri Değişikliği";
    default:
      return "Diğer";
  }
}

export function getMoldTrialResultLabel(result: string): string {
  switch (result) {
    case "pending":
      return "Beklemede";
    case "successful":
      return "Başarılı";
    case "conditionally-approved":
      return "Şartlı Onay";
    case "failed":
      return "Başarısız";
    case "cancelled":
      return "İptal";
    default:
      return "Bilinmiyor";
  }
}

export function getMoldTimelineKindLabel(kind: string): string {
  switch (kind) {
    case "lifecycle":
      return "Yaşam Döngüsü";
    case "counter":
      return "Sayaç";
    case "maintenance":
      return "Bakım";
    case "revision":
      return "Revizyon";
    case "trial":
      return "Deneme";
    default:
      return "Olay";
  }
}

export const MOLD_MAINTENANCE_TYPE_OPTIONS = [
  { value: "preventive", label: "Önleyici" },
  { value: "corrective", label: "Düzeltici" },
  { value: "cleaning", label: "Temizlik" },
  { value: "inspection", label: "Kontrol" },
  { value: "repair", label: "Onarım" },
  { value: "overhaul", label: "Revizyon Bakım" },
  { value: "other", label: "Diğer" },
] as const;

export const MOLD_MAINTENANCE_STATUS_OPTIONS = [
  { value: "planned", label: "Planlandı" },
  { value: "in-progress", label: "Devam Ediyor" },
] as const;

export const MOLD_REVISION_TYPE_OPTIONS = [
  { value: "design-change", label: "Tasarım Değişikliği" },
  { value: "cavity-change", label: "Gözenek Değişikliği" },
  { value: "material-change", label: "Malzeme Değişikliği" },
  { value: "cooling-change", label: "Soğutma Değişikliği" },
  { value: "runner-change", label: "Yolluk Değişikliği" },
  { value: "repair-improvement", label: "Onarım İyileştirmesi" },
  { value: "customer-change", label: "Müşteri Değişikliği" },
  { value: "other", label: "Diğer" },
] as const;

export const MOLD_TRIAL_RESULT_OPTIONS = [
  { value: "pending", label: "Beklemede" },
  { value: "successful", label: "Başarılı" },
  { value: "conditionally-approved", label: "Şartlı Onay" },
  { value: "failed", label: "Başarısız" },
  { value: "cancelled", label: "İptal" },
] as const;
