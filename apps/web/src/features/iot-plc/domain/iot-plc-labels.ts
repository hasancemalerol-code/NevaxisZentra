import type { IotPlcTabId } from "./iot-plc-types";

export const IOT_PLC_DEVELOPMENT_BANNER =
  "Örnek veri — gerçek PLC veya müşteri verisi değildir.";

export const IOT_PLC_CURRENT_STATUS_NOTE =
  "Mevcut durum yalnızca güncel sağlık kanıtından hesaplanır.";

export const IOT_PLC_CURRENT_EVIDENCE_SCOPE = "Güncel sağlık kanıtı penceresi";

export const IOT_PLC_LAST_KNOWN_OBSERVATIONS_TITLE = "Son bilinen sağlık gözlemleri";

const PROFILE_DISPLAY_LABELS: Record<string, string> = {
  "Development Standard Signal Quality Profile": "Geliştirme Standart Sinyal Kalitesi Profili",
  "Development Strict Profile (High Minimum Count)": "Geliştirme Sıkı Kalite Profili",
};

export const IOT_PLC_TAB_OPTIONS: ReadonlyArray<Readonly<{ id: IotPlcTabId; label: string }>> = [
  { id: "overview", label: "Genel Bakış" },
  { id: "connections", label: "Bağlantılar" },
  { id: "signals", label: "Sinyaller" },
  { id: "quality", label: "Veri Kalitesi" },
];

export const IOT_PLC_KPI_ORDER = [
  "registered-endpoints",
  "online-endpoints",
  "degraded-offline-endpoints",
  "active-signals",
  "fresh-signals",
  "attention-assessments",
] as const;

const PROTOCOL_LABELS: Record<string, string> = {
  "opc-ua": "OPC UA",
  "modbus-tcp": "Modbus TCP",
  mqtt: "MQTT",
  "http-push": "HTTP Push",
  "edge-agent": "Saha ajanı",
  "manual-development-simulator": "Manuel örnek veri",
};

const DIRECTION_LABELS: Record<string, string> = {
  "read-only-pull": "Salt okunur veri alama",
  "inbound-push": "Gelen veri akışı",
  "edge-relay": "Saha aktarımı",
};

const ENDPOINT_STATUS_LABELS: Record<string, string> = {
  active: "Aktif",
  inactive: "Pasif",
  retired: "Emekli",
};

const HEALTH_LABELS: Record<string, string> = {
  online: "Çevrimiçi",
  degraded: "Bozulmuş",
  offline: "Çevrimdışı",
  unknown: "Bilinmiyor",
};

const QUALITY_STATE_LABELS: Record<string, string> = {
  good: "İyi",
  uncertain: "Belirsiz",
  bad: "Kötü",
  stale: "Bayat",
  "source-conflict": "Kaynak uyuşmazlığı",
};

const EVIDENCE_LABELS: Record<string, string> = {
  measured: "Ölçülen",
  calculated: "Hesaplanmış",
  estimated: "Tahmini",
  simulated: "Örnek veri",
  "source-conflict": "Kaynak uyuşmazlığı",
  "insufficient-data": "Veri yetersiz",
};

const OUTCOME_LABELS: Record<string, string> = {
  healthy: "Sağlıklı",
  attention: "Dikkat gerekli",
  degraded: "Bozulmuş",
  unavailable: "Kullanılamıyor",
  "insufficient-data": "Veri yetersiz",
  "source-conflict": "Kaynak uyuşmazlığı",
};

const DRIVER_LABELS: Record<string, string> = {
  "within-quality-profile": "Profil içinde",
  "observation-stale": "Bayat gözlem",
  "observation-freshness-exceeded": "Güncellik eşiği aşıldı",
  "endpoint-offline": "Bağlantı çevrimdışı",
  "endpoint-health-degraded": "Bağlantı sağlığı bozulmuş",
  "endpoint-health-unknown": "Bağlantı sağlığı belirsiz",
  "clock-skew-exceeded": "Saat kayması",
  "source-sequence-gap": "Sıra numarası boşluğu",
  "source-conflict": "Kaynak uyuşmazlığı",
  "insufficient-observations": "Yetersiz gözlem",
  "cadence-missed": "Beklenen kadans kaçırıldı",
  "duplicate-observation-excluded": "Yinelenen gözlem hariç tutuldu",
  "no-recent-observation": "Güncel gözlem yok",
};

const SEMANTIC_TYPE_LABELS: Record<string, string> = {
  state: "Durum",
  measurement: "Ölçüm",
  counter: "Sayaç",
  event: "Olay",
};

const PROFILE_STATUS_LABELS: Record<string, string> = {
  draft: "Taslak",
  approved: "Onaylı",
  retired: "Emekli",
};

export function getIotPlcProfileDisplayName(displayName: string): string {
  return PROFILE_DISPLAY_LABELS[displayName] ?? displayName;
}

export function getIotPlcProtocolLabel(value: string): string {
  return PROTOCOL_LABELS[value] ?? value;
}

export function getIotPlcDirectionLabel(value: string): string {
  return DIRECTION_LABELS[value] ?? value;
}

export function getIotPlcEndpointStatusLabel(value: string): string {
  return ENDPOINT_STATUS_LABELS[value] ?? value;
}

export function getIotPlcHealthLabel(value: string | null | undefined): string {
  if (!value) return "Bilinmiyor";
  return HEALTH_LABELS[value] ?? value;
}

export function getIotPlcQualityStateLabel(value: string | null | undefined): string {
  if (!value) return "—";
  return QUALITY_STATE_LABELS[value] ?? value;
}

export function getIotPlcEvidenceLabel(value: string | null | undefined): string {
  if (!value) return "—";
  return EVIDENCE_LABELS[value] ?? value;
}

export function getIotPlcOutcomeLabel(value: string): string {
  return OUTCOME_LABELS[value] ?? value;
}

export function getIotPlcDriverLabel(value: string | null | undefined): string {
  if (!value) return "—";
  return DRIVER_LABELS[value] ?? value;
}

export function getIotPlcSemanticTypeLabel(value: string): string {
  return SEMANTIC_TYPE_LABELS[value] ?? value;
}

export function getIotPlcProfileStatusLabel(value: string): string {
  return PROFILE_STATUS_LABELS[value] ?? value;
}

export function getIotPlcFindingTitle(driverCode: string): string {
  switch (driverCode) {
    case "endpoint-offline":
      return "Bağlantı çevrimdışı";
    case "endpoint-health-degraded":
      return "Bağlantı bozulmuş";
    case "observation-stale":
      return "Bayat gözlem";
    case "cadence-missed":
      return "Kadans kaçırıldı";
    case "source-sequence-gap":
      return "Sıra numarası boşluğu";
    case "clock-skew-exceeded":
      return "Saat kayması";
    case "source-conflict":
      return "Kaynak uyuşmazlığı";
    case "insufficient-observations":
      return "Veri yetersiz";
    case "observation-freshness-exceeded":
      return "Güncellik eşiği aşıldı";
    case "no-recent-observation":
      return "Güncel gözlem yok";
    default:
      return getIotPlcDriverLabel(driverCode);
  }
}

export function getIotPlcAssessmentTitle(
  kind: "connectivity" | "signal",
  targetLabel: string,
  outcome: string,
): string {
  const kindLabel = kind === "connectivity" ? "Bağlantı" : "Sinyal";
  return `${kindLabel} · ${targetLabel} · ${getIotPlcOutcomeLabel(outcome)}`;
}

export const IOT_PLC_OUTCOME_FILTER_OPTIONS = [
  { value: "", label: "Tüm sonuçlar" },
  { value: "healthy", label: "Sağlıklı" },
  { value: "attention", label: "Dikkat gerekli" },
  { value: "degraded", label: "Bozulmuş" },
  { value: "unavailable", label: "Kullanılamıyor" },
  { value: "insufficient-data", label: "Veri yetersiz" },
  { value: "source-conflict", label: "Kaynak uyuşmazlığı" },
] as const;

export const IOT_PLC_EVIDENCE_FILTER_OPTIONS = [
  { value: "", label: "Tüm kanıt türleri" },
  { value: "simulated", label: "Örnek veri" },
  { value: "measured", label: "Ölçülen" },
] as const;
