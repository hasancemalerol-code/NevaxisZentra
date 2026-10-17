/**
 * Turkish customer labels for machine values (status, severity, priority, freshness, category) that screens used to print
 * raw. Unknown values are humanized into readable words, never shown as an identifier.
 */

export type EnumLevel = "critical" | "high" | "medium" | "low" | "info" | "ai" | "neutral";
export type EnumLabel = Readonly<{ label: string; level: EnumLevel }>;

const MAP: Readonly<Record<string, EnumLabel>> = {
  running: { label: "Çalışıyor", level: "low" },
  operating: { label: "Çalışıyor", level: "low" },
  idle: { label: "Boşta", level: "neutral" },
  stopped: { label: "Durdu", level: "neutral" },
  waiting: { label: "Bekliyor", level: "medium" },
  starved: { label: "Aç", level: "medium" },
  faulted: { label: "Arıza", level: "critical" },
  fault: { label: "Arıza", level: "critical" },
  mixed_degraded: { label: "Kısmi aksama", level: "medium" },
  critical: { label: "Kritik", level: "critical" },
  major: { label: "Majör", level: "high" },
  minor: { label: "Minör", level: "medium" },
  high: { label: "Yüksek", level: "high" },
  urgent: { label: "Acil", level: "critical" },
  medium: { label: "Orta", level: "medium" },
  normal: { label: "Normal", level: "neutral" },
  low: { label: "Düşük", level: "low" },
  info: { label: "Bilgi", level: "info" },
  warning: { label: "Uyarı", level: "medium" },
  open: { label: "Açık", level: "info" },
  closed: { label: "Kapalı", level: "neutral" },
  draft: { label: "Taslak", level: "neutral" },
  scheduled: { label: "Planlandı", level: "info" },
  planned: { label: "Planlandı", level: "info" },
  active: { label: "Aktif", level: "low" },
  inactive: { label: "Pasif", level: "neutral" },
  paused: { label: "Duraklatıldı", level: "medium" },
  blocked: { label: "Bloke", level: "critical" },
  overdue: { label: "Gecikmiş", level: "critical" },
  pending: { label: "Bekliyor", level: "medium" },
  "in-progress": { label: "Devam ediyor", level: "info" },
  in_progress: { label: "Devam ediyor", level: "info" },
  completed: { label: "Tamamlandı", level: "low" },
  resolved: { label: "Çözüldü", level: "low" },
  cancelled: { label: "İptal edildi", level: "neutral" },
  canceled: { label: "İptal edildi", level: "neutral" },
  failed: { label: "Başarısız", level: "critical" },
  approved: { label: "Onaylandı", level: "low" },
  rejected: { label: "Reddedildi", level: "critical" },
  verified: { label: "Doğrulandı", level: "low" },
  unverified: { label: "Doğrulanmadı", level: "neutral" },
  "awaiting-approval": { label: "Onay bekliyor", level: "medium" },
  awaiting_approval: { label: "Onay bekliyor", level: "medium" },
  "requires-review": { label: "İnceleme gerekli", level: "medium" },
  requires_review: { label: "İnceleme gerekli", level: "medium" },
  "under-review": { label: "İnceleniyor", level: "info" },
  acknowledged: { label: "Görüldü", level: "info" },
  monitoring: { label: "İzlemede", level: "info" },
  fresh: { label: "Güncel", level: "low" },
  stale: { label: "Eski", level: "medium" },
  unknown: { label: "Bilinmiyor", level: "neutral" },
  good: { label: "İyi", level: "low" },
  bad: { label: "Kötü", level: "critical" },
  uncertain: { label: "Belirsiz", level: "medium" },
  sufficient: { label: "Yeterli", level: "low" },
  insufficient: { label: "Yetersiz", level: "neutral" },
  "insufficient-data": { label: "Yetersiz veri", level: "neutral" },
  conflicted: { label: "Çelişkili", level: "medium" },
  measurement: { label: "Ölçüm", level: "info" },
  "no-completed-work-orders": { label: "Tamamlanmış bakım emri yok", level: "neutral" },
  "work-order-created": { label: "Bakım emri oluşturuldu", level: "neutral" },
  "work_order_created": { label: "Bakım emri oluşturuldu", level: "neutral" },
  "incomplete": { label: "Eksik", level: "neutral" },
  planning: { label: "Planlama", level: "info" },
  procurement: { label: "Satın alma", level: "info" },
  logistics: { label: "Lojistik", level: "info" },
  export: { label: "İhracat", level: "info" },
  energy: { label: "Enerji", level: "info" },
  none: { label: "Yok", level: "neutral" },
  other: { label: "Diğer", level: "neutral" },
  "not-started": { label: "Başlamadı", level: "neutral" },
  "unable-to-evaluate": { label: "Değerlendirilemedi", level: "neutral" },
  "data-quality": { label: "Veri kalitesi", level: "info" },
  maintenance: { label: "Bakım", level: "info" },
  quality: { label: "Kalite", level: "info" },
  production: { label: "Üretim", level: "info" },
  material: { label: "Malzeme", level: "info" },
  mold: { label: "Kalıp", level: "info" },
  shift: { label: "Vardiya", level: "info" },
  promotion_blocked: { label: "Yayın engellendi", level: "critical" },
  evaluation_failed: { label: "Değerlendirme başarısız", level: "critical" },
  rollback_triggered: { label: "Geri alma tetiklendi", level: "high" },
  pass: { label: "Geçti", level: "low" },
  fail: { label: "Kaldı", level: "critical" },
  candidate: { label: "Aday", level: "info" },
  shadow: { label: "Gölge modda", level: "info" },
  limited: { label: "Sınırlı kullanımda", level: "medium" },
  retired: { label: "Emekli", level: "neutral" },
  ready: { label: "Hazır", level: "low" },
  not_ready: { label: "Hazır değil", level: "neutral" },
  deterministic: { label: "Kural tabanlı", level: "info" },
  analytics: { label: "Analiz", level: "info" },
  solver: { label: "Optimizasyon", level: "info" },
  retrieval: { label: "Bilgi arama", level: "info" },
  unavailable: { label: "Kullanılamıyor", level: "critical" },
  degraded: { label: "Bozulmuş", level: "high" },
  online: { label: "Çevrimiçi", level: "low" },
  offline: { label: "Çevrimdışı", level: "critical" },
  complete: { label: "Tam", level: "low" },
  partial: { label: "Kısmi", level: "medium" },
  frontier_model: { label: "Harici model", level: "info" },
  nevaxis_model: { label: "Zentra modeli", level: "info" },
  candidate_vs_production: { label: "Aday ve canlı model karşılaştırması", level: "info" },
  good_output_gain: { label: "İyi çıktı kazanımı", level: "low" },
  downtime_avoided: { label: "Önlenen duruş", level: "low" },
  late_orders_protected: { label: "Korunan geciken sipariş", level: "low" },
  strongly_attributed: { label: "Güçlü ilişkilendirme", level: "low" },
  direct: { label: "Doğrudan", level: "low" },
  verified_operational: { label: "Operasyonel olarak doğrulandı", level: "low" },
  verified_economic: { label: "Ekonomik olarak doğrulandı", level: "low" },
  observing: { label: "İzleniyor", level: "info" },
  predicted: { label: "Öngörüldü", level: "info" },
  converted: { label: "Ekonomik dönüşüm", level: "info" },
};

const TOKEN_TR: Readonly<Record<string, string>> = {
  data: "veri",
  quality: "kalite",
  review: "inceleme",
  approval: "onay",
  awaiting: "bekleyen",
  requires: "gereken",
  progress: "devam",
  error: "hata",
  status: "durum",
  order: "emir",
  task: "görev",
  alert: "uyarı",
  source: "kaynak",
  unavailable: "kullanılamıyor",
  configured: "yapılandırıldı",
};

/** Readable words for a value that has no explicit label: "data_quality" becomes "Veri kalite". */
export function humanizeEnum(value: string | null | undefined): string {
  const text = (value ?? "").trim();
  if (!text) return "—";
  const words = text
    .replace(/([a-z])([A-Z])/g, "$1 $2")
    .split(/[\s_\-.:]+/)
    .filter(Boolean)
    .map((word) => TOKEN_TR[word.toLowerCase()] ?? word.toLowerCase());
  const phrase = words.join(" ");
  return phrase.charAt(0).toLocaleUpperCase("tr-TR") + phrase.slice(1);
}

export function resolveEnumLabel(value: string | null | undefined): EnumLabel {
  const key = (value ?? "").trim().toLowerCase();
  return MAP[key] ?? { label: humanizeEnum(value), level: "neutral" };
}

export function enumLabel(value: string | null | undefined): string {
  return resolveEnumLabel(value).label;
}

const STATUS_WORDS: Readonly<Record<string, string>> = {
  unavailable: "kullanılamıyor",
  degraded: "bozulmuş",
  online: "çevrimiçi",
  offline: "çevrimdışı",
  unknown: "bilinmiyor",
  stale: "eski",
  fresh: "güncel",
  healthy: "sağlıklı",
  critical: "kritik",
  warning: "uyarı",
};

/** Replaces machine status words inside a generated sentence ("Endpoint: degraded") with Turkish ones. */
export function localizeStatusWords(text: string | null | undefined): string {
  return (text ?? "").replace(/\b(unavailable|degraded|online|offline|unknown|stale|fresh|healthy|critical|warning)\b/gi, (word) => STATUS_WORDS[word.toLowerCase()] ?? word);
}

/** Turkish label for a raw machine/line state, or null when the value is not a known state (callers keep their own text). */
export function knownStateLabel(value: string | null | undefined): string | null {
  const key = (value ?? "").trim().toLowerCase();
  const found = MAP[key];
  return found && STATE_KEYS.has(key) ? found.label : null;
}

const STATE_KEYS: ReadonlySet<string> = new Set(["running", "operating", "idle", "stopped", "waiting", "starved", "blocked", "faulted", "fault", "mixed_degraded"]);
