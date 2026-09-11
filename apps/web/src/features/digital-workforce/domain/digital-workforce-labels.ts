import type {
  DigitalAutonomyMode,
  DigitalEvidenceKind,
  DigitalWorkCaseStatus,
  DigitalWorkerRuntimeStatus,
  DigitalWorkStage,
  DigitalApprovalPolicy,
} from "./digital-workforce-types";

export const DIGITAL_WORKFORCE_EYEBROW = "DİJİTAL İŞ GÜCÜ";
export const DIGITAL_WORKFORCE_TITLE = "Dijital İş Gücü Kontrol Merkezi";
export const DIGITAL_WORKFORCE_SUBTITLE =
  "Fabrikanızdaki dijital çalışanları yönetin, yetkilendirin ve sonuçlarını izleyin.";
export const DIGITAL_WORKFORCE_POSITIONING =
  "CrevaFactOS yalnızca fabrikayı analiz etmez. Belirlenen yetki sınırları içinde işi alır, yürütür, doğrular ve kapatır.";
export const DIGITAL_WORKFORCE_SUMMARY =
  "7 Dijital Çalışan fabrikanın üretim, bakım, malzeme, kalite, tedarik ve kalıp operasyonlarında görev alır. Her çalışan yalnızca izin verilen sistemlere ve kaynaklara erişir. Çalışma seviyesi İzle'den Tam Otonom'a kadar ayrı ayrı ayarlanabilir. İşler sonuç doğrulanmadan kapatılmaz.";
export const DIGITAL_WORKFORCE_FULL_NOTE =
  "Tanımlı güvenlik ve onay sınırları geçerlidir.";
export const DIGITAL_WORKFORCE_MACHINE_SAFETY_NOTE =
  "Makine güvenlik sistemlerini aşamaz.";
export const DIGITAL_WORKFORCE_COST_NOTE = "Demo maliyet verisi";
export const DIGITAL_WORKFORCE_VERIFIED_BADGE = "Doğrulandı";
export const DIGITAL_WORKFORCE_VERIFIED_CLOSED = "DOĞRULANARAK KAPATILDI";

export const AUTONOMY_MODE_ORDER: readonly DigitalAutonomyMode[] = [
  "observe",
  "draft",
  "limited",
  "full",
];

export const AUTONOMY_LABELS: Record<DigitalAutonomyMode, string> = {
  observe: "İZLE",
  draft: "ÖNER",
  limited: "SINIRLI OTONOM",
  full: "TAM OTONOM",
};

export const AUTONOMY_DESCRIPTIONS: Record<DigitalAutonomyMode, string> = {
  observe: "Sadece izler, analiz eder ve riskleri gösterir.",
  draft: "Ne yapılması gerektiğini hazırlar; uygulama için onay bekler.",
  limited: "Belirlediğiniz sınırlar içinde işleri kendi yürütür.",
  full: "Tanımlı güvenlik ve yetki sınırları içinde işi uçtan uca tamamlar.",
};

export const AUTONOMY_EXPLAINER: Record<DigitalAutonomyMode, string> = {
  observe: "Analiz eder.",
  draft: "Öneri ve taslak hazırlar.",
  limited: "Belirlenen limitlerde uygular.",
  full: "Güvenlik sınırları içinde uçtan uca yürütür.",
};

export const WORKER_STATUS_LABELS: Record<DigitalWorkerRuntimeStatus, string> = {
  working: "Çalışıyor",
  observing: "İzliyor",
  "awaiting-approval": "Onay Bekliyor",
  paused: "Duraklatıldı",
  "needs-intervention": "Müdahale Gerekiyor",
  idle: "Boşta",
  "human-controlled": "İnsan Kontrolünde",
};

export const CASE_STATUS_LABELS: Record<DigitalWorkCaseStatus, string> = {
  detected: "Tespit Edildi",
  analyzing: "Analiz Ediliyor",
  planning: "Plan Hazırlanıyor",
  "awaiting-approval": "Onay Bekliyor",
  acting: "Uygulanıyor",
  verifying: "Doğrulanıyor",
  closed: "Kapatıldı",
  escalation: "Escalation Gerekli",
};

export const STAGE_LABELS: Record<DigitalWorkStage, string> = {
  detect: "Tespit Edildi",
  analyze: "Analiz Ediliyor",
  decide: "Karar Veriliyor",
  plan: "Plan Hazırlanıyor",
  approve: "Onay Bekliyor",
  act: "Uygulanıyor",
  verify: "Doğrulanıyor",
  close: "Kapatıldı",
};

export const STAGE_ORDER: readonly DigitalWorkStage[] = [
  "detect",
  "analyze",
  "decide",
  "plan",
  "approve",
  "act",
  "verify",
  "close",
];

export const APPROVAL_POLICY_LABELS: Record<DigitalApprovalPolicy, string> = {
  "always-ask": "Her Zaman Sor",
  "ask-above-threshold": "Eşik Üstünde Sor",
  "auto-allowed": "Otomatik İzinli",
  "never-allowed": "Asla İzinli Değil",
};

export const EVIDENCE_LABELS: Record<DigitalEvidenceKind, string> = {
  erp: "ERP kaydı",
  iot: "IoT sinyali",
  maintenance: "Bakım kaydı",
  quality: "Kalite kaydı",
  approval: "Kullanıcı onayı",
  file: "Dosya",
  operation: "Operasyon sonucu",
};

export const DIGITAL_WORKFORCE_SUBNAV = [
  { href: "/dijital-is-gucu", label: "Kontrol Merkezi", exact: true },
  { href: "/dijital-is-gucu/calisanlar", label: "Dijital Çalışanlar" },
  { href: "/dijital-is-gucu/onaylar", label: "Bekleyen Onaylar" },
  { href: "/dijital-is-gucu/sonuclar", label: "Sonuçlar" },
  { href: "/dijital-is-gucu/tamamlanan", label: "Tamamlanan İşler" },
  { href: "/dijital-is-gucu/aktivite", label: "Aktivite" },
] as const;
