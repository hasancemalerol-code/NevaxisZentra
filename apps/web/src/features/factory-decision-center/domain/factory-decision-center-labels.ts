const STATUS_LABELS: Record<string, string> = {
  "awaiting-approval": "İnsan Onayı Bekliyor",
  executing: "Kurtarma Operasyonu",
  verifying: "Doğrulama Bekliyor",
  closed: "Sonuç Doğrulandı",
  planning: "Planlanıyor",
  detected: "Tespit Edildi",
  analyzing: "Analiz Ediliyor",
};

const SEVERITY_LABELS: Record<string, string> = {
  critical: "KRİTİK",
  high: "YÜKSEK",
  medium: "ORTA",
  low: "DÜŞÜK",
};

export function getFactoryDecisionStatusLabel(status: string): string {
  return STATUS_LABELS[status] ?? status;
}

export function getFactoryDecisionSeverityLabel(severity: string): string {
  return SEVERITY_LABELS[severity] ?? severity.toUpperCase();
}

export const FACTORY_DECISION_CENTER_TITLE = "Fabrika Karar Merkezi";
export const FACTORY_DECISION_CENTER_SUBTITLE =
  "Fabrikanın kritik kararlarını, risk altındaki sonuçları ve kurtarma operasyonlarını tek merkezden yönetin.";
