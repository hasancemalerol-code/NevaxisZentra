export const COMMERCIAL_PRICING_SCENARIO_SUBTITLE =
  "Fiyat koruma ve onay; önerilen birim fiyatların kârlılık profiline karşı kayıtlı verilere göre değerlendirilmesini ve insan onaylı iş akışını gösterir.";

export const COMMERCIAL_PRICING_SCENARIO_SCOPE_NOTE =
  "Senaryolar otomatik fiyat uygulamaz; yalnızca değerlendirme, onay geçişleri ve görev taslağı desteklenir.";

export const COMMERCIAL_PRICING_SCENARIO_EMPTY_NOTE =
  "Seçili filtreler için fiyat senaryosu bulunamadı.";

export const COMMERCIAL_PRICING_SCENARIO_EMPTY_MANAGE_HINT =
  " Yeni senaryo oluşturmak için yönetim yetkisi gerekir.";

export const COMMERCIAL_PRICING_SCENARIO_NO_SELECTION_NOTE =
  "Detay görmek için tablodan bir fiyat senaryosu seçin.";

export const COMMERCIAL_PRICING_SCENARIO_AUTOMATION_DISCLAIMER =
  "Bu onay fiyatı otomatik olarak uygulamaz.";

export const COMMERCIAL_PRICING_SCENARIO_PILOT_PROFILE_ID =
  "bb000001-0310-4000-8000-000000000310";

export const COMMERCIAL_PRICING_SCENARIO_PILOT_SALES_EVIDENCE_WITHIN_ID =
  "bb000001-0321-4000-8000-000000000321";

export const COMMERCIAL_PRICING_SCENARIO_PILOT_SALES_EVIDENCE_BELOW_ID =
  "bb000001-0323-4000-8000-000000000323";

const LIFECYCLE_LABELS: Record<string, string> = {
  draft: "Taslak",
  evaluated: "Değerlendirildi",
  "needs-review": "İnceleme gerekli",
  "eligible-for-approval": "Onaya uygun",
  blocked: "Engellendi",
  "insufficient-data": "Yetersiz veri",
  "submitted-for-approval": "Onaya gönderildi",
  approved: "Onaylandı",
  rejected: "Reddedildi",
  withdrawn: "Geri çekildi",
};

const GUARDRAIL_LABELS: Record<string, string> = {
  passes: "Geçti",
  "needs-review": "İnceleme gerekli",
  blocked: "Engellendi",
  "insufficient-data": "Yetersiz veri",
};

const ASSESSMENT_OUTCOME_LABELS: Record<string, string> = {
  "within-threshold": "Eşik içinde",
  "below-threshold": "Eşik altında",
  "negative-contribution": "Negatif katkı",
  "insufficient-data": "Yetersiz veri",
  "source-conflict": "Kaynak çakışması",
};

const COST_COVERAGE_LABELS: Record<string, string> = {
  "complete-required-cost-coverage": "Tam maliyet kapsamı",
  "partial-cost-coverage": "Kısmi maliyet kapsamı",
  "insufficient-cost-evidence": "Yetersiz maliyet kanıtı",
  "source-conflict": "Kaynak çakışması",
};

const AUDIT_EVENT_LABELS: Record<string, string> = {
  created: "Oluşturuldu",
  updated: "Güncellendi",
  evaluated: "Değerlendirildi",
  "submitted-for-approval": "Onaya gönderildi",
  approved: "Onaylandı",
  rejected: "Reddedildi",
  withdrawn: "Geri çekildi",
  "task-linked": "Görev bağlandı",
  "returned-to-draft": "Taslak durumuna döndü",
};

const DRIVER_SEVERITY_LABELS: Record<string, string> = {
  low: "Düşük",
  medium: "Orta",
  high: "Yüksek",
  critical: "Kritik",
};

export function getCommercialPricingScenarioLifecycleLabel(status: string | null | undefined): string {
  if (!status) return "—";
  return LIFECYCLE_LABELS[status] ?? status;
}

export function getCommercialPricingScenarioGuardrailLabel(result: string | null | undefined): string {
  if (!result) return "—";
  return GUARDRAIL_LABELS[result] ?? result;
}

export function getCommercialPricingScenarioAssessmentOutcomeLabel(outcome: string | null | undefined): string {
  if (!outcome) return "—";
  return ASSESSMENT_OUTCOME_LABELS[outcome] ?? outcome;
}

export function getCommercialPricingScenarioCostCoverageLabel(status: string | null | undefined): string {
  if (!status) return "—";
  return COST_COVERAGE_LABELS[status] ?? status;
}

export function getCommercialPricingScenarioAuditEventLabel(eventType: string | null | undefined): string {
  if (!eventType) return "—";
  return AUDIT_EVENT_LABELS[eventType] ?? eventType;
}

export function getCommercialPricingScenarioDriverSeverityLabel(severity: string | null | undefined): string {
  if (!severity) return "—";
  return DRIVER_SEVERITY_LABELS[severity] ?? severity;
}
