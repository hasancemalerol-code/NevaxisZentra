export const BULK_DATA_ONBOARDING_ROUTE = "/ayarlar/veri-aktarim-merkezi";
export const BULK_DATA_ONBOARDING_TITLE = "Veri Aktarım Merkezi";
export const BULK_DATA_ONBOARDING_SUBTITLE =
  "Kaynak keşfi, eşleme, doğrulama, karantina ve kontrollü içe aktarma iş akışı.";

export const BULK_ONBOARDING_WORKFLOW_STEPS = [
  "Kaynak",
  "Keşif",
  "Eşleme",
  "Doğrulama",
  "Deneme çalıştırması",
  "Karantina",
  "İçe Aktarma",
  "Mutabakat",
  "Doğrulandı",
] as const;

const JOB_STATUS_LABELS: Readonly<Record<string, string>> = {
  created: "Oluşturuldu",
  discovering: "Keşfediliyor",
  profiling: "Profilleniyor",
  "mapping-required": "Eşleme Gerekli",
  validating: "Doğrulanıyor",
  "ready-for-dry-run": "Deneme İçin Hazır",
  "dry-running": "Deneme Çalışıyor",
  quarantined: "Karantinada",
  "ready-to-commit": "İçe Aktarmaya Hazır",
  importing: "İçe Aktarılıyor",
  reconciling: "Mutabakat Yapılıyor",
  verifying: "Doğrulanıyor",
  completed: "Tamamlandı",
  partial: "Kısmen Tamamlandı",
  failed: "Başarısız",
  cancelled: "İptal Edildi",
};

export function bulkJobStatusLabel(status: string): string {
  return JOB_STATUS_LABELS[status] ?? "Durum bilinmiyor";
}

const PROVIDER_KIND_LABELS: Readonly<Record<string, string>> = {
  "csv-file": "CSV dosyası",
  "xlsx-file": "XLSX dosyası",
  "database-export": "Veritabanı dışa aktarımı",
  "read-only-database": "Salt okunur veritabanı",
  "synthetic-structured": "Yapılandırılmış kaynak",
};

export function bulkProviderKindLabel(kind: string): string {
  return PROVIDER_KIND_LABELS[kind] ?? "Dosya kaynağı";
}

const SAFETY_MODE_LABELS: Readonly<Record<string, string>> = {
  observe: "Gözlem modu",
  draft: "Taslak modu",
};

export function bulkSafetyModeLabel(mode: string): string {
  return SAFETY_MODE_LABELS[mode] ?? "Güvenli mod";
}
