import { formatDateTime } from "@/lib/presentation/format";

export const ENTERPRISE_RELIABILITY_TITLE = "Sistem Durumu";
export const ENTERPRISE_RELIABILITY_SUBTITLE =
  "Platform bileşenleri, entegrasyonlar, saha ajanları ve teknik uyarıların operasyonel görünümü";
export const SYSTEM_STATUS_UNVERIFIED_LABEL = "Doğrulanamadı";

export const SYSTEM_STATUS_ALL_NORMAL_LABEL = "Her şey normal";

export function formatSystemStatusDateTime(value?: string): string {
  return formatDateTime(value);
}

/** Version strings that are build placeholders rather than a reported release. */
const PLACEHOLDER_VERSIONS = new Set(["", "unknown", "0.0.0", "0.0.0.0", "1.0.0.0"]);

/** Customer-facing application version: the reported release, or a plain "no information" statement. */
export function formatApplicationVersion(version: string | null | undefined): string {
  const text = (version ?? "").trim();
  return PLACEHOLDER_VERSIONS.has(text.toLowerCase()) ? "Sürüm bilgisi yok" : text;
}

/** Build detail line, or null when the build is not reported (the caller then shows "Derleme bilgisi yok"). */
export function formatBuildDetail(buildSha: string | null | undefined): string {
  const text = (buildSha ?? "").trim();
  if (!text || text.toLowerCase() === "unknown") {
    return "Derleme bilgisi yok";
  }
  return `Derleme ${text.slice(0, 8)}`;
}

export function getHealthStatusLabel(status: string): string {
  switch (status) {
    case "healthy":
      return "Sağlıklı";
    case "degraded":
      return "Bozulmuş";
    case "unavailable":
      return "Erişilemez";
    case "recovering":
      return "Kurtarılıyor";
    case "unknown":
      return SYSTEM_STATUS_UNVERIFIED_LABEL;
    default:
      return status;
  }
}

export function getHealthStatusBadgeTone(
  status: string,
): "neutral" | "information" | "success" | "warning" | "danger" | "ai" {
  switch (status) {
    case "healthy":
      return "success";
    case "degraded":
    case "recovering":
      return "warning";
    case "unavailable":
      return "danger";
    case "unknown":
      return "neutral";
    default:
      return "information";
  }
}

export function getOperatingModeLabel(mode: string): string {
  switch (mode) {
    case "fully-operational":
      return "Tam Operasyonel";
    case "degraded-read-only":
      return "Kısıtlı Okuma Modu";
    case "degraded-buffering":
      return "Tamponlama Modu";
    case "recovering":
      return "Kurtarma Modu";
    default:
      return mode;
  }
}

export function getOperatingModeBadgeTone(
  mode: string,
): "neutral" | "information" | "success" | "warning" | "danger" | "ai" {
  switch (mode) {
    case "fully-operational":
      return "success";
    case "degraded-read-only":
    case "degraded-buffering":
      return "warning";
    case "recovering":
      return "information";
    default:
      return "neutral";
  }
}

export function isDegradedOperatingMode(mode: string): boolean {
  return mode !== "fully-operational";
}

export function getIntegrationUiStatusLabel(status: string): string {
  switch (status) {
    case "Bağlı":
    case "connected":
      return "Bağlı";
    case "Gecikmeli":
    case "delayed":
      return "Gecikmeli";
    case "Sorunlu":
    case "faulty":
      return "Sorunlu";
    case "Bağlantı Yok":
    case "disconnected":
      return "Bağlantı Yok";
    default:
      return status;
  }
}

export function getIntegrationUiStatusBadgeTone(
  status: string,
): "neutral" | "information" | "success" | "warning" | "danger" | "ai" {
  switch (getIntegrationUiStatusLabel(status)) {
    case "Bağlı":
      return "success";
    case "Gecikmeli":
      return "warning";
    case "Sorunlu":
    case "Bağlantı Yok":
      return "danger";
    default:
      return "neutral";
  }
}

export function getIntegrationHealthStatusLabel(status: string): string {
  switch (status) {
    case "healthy":
      return "Sağlıklı";
    case "degraded":
      return "Bozulmuş";
    case "offline":
      return "Çevrimdışı";
    case "misconfigured":
      return "Yanlış Yapılandırılmış";
    case "authentication-failed":
      return "Kimlik Doğrulama Hatası";
    case "rate-limited":
      return "Hız Sınırı";
    case "stale":
      return "Eski Veri";
    case "sync-failed":
      return "Eşitleme Başarısız";
    case "unknown":
      return "Bilinmiyor";
    case "disabled":
      return "Devre Dışı";
    default:
      return status;
  }
}

export function getIntegrationFreshnessLabel(state: string): string {
  switch (state) {
    case "fresh":
      return "Güncel";
    case "aging":
      return "Yaşlanıyor";
    case "stale":
      return "Gecikmiş";
    case "unknown":
      return "Bilinmiyor";
    case "unavailable":
      return "Erişilemez";
    default:
      return state;
  }
}

export function getCircuitStateLabel(state: string): string {
  switch (state) {
    case "closed":
      return "Kapalı";
    case "open":
      return "Açık";
    case "half-open":
      return "Yarı Açık";
    default:
      return state;
  }
}

export function getComponentKindLabel(kind: string): string {
  switch (kind) {
    case "api":
      return "API";
    case "database":
      return "Veritabanı";
    case "message-broker":
      return "Mesaj Kuyruğu";
    case "ai-provider":
      return "Yapay Zekâ Sağlayıcısı";
    case "integration-connector":
      return "Entegrasyon";
    case "edge-agent":
      return "Saha Ajanı";
    case "machine-telemetry":
      return "Makine Telemetri";
    case "erp-adapter":
      return "ERP bağlantısı";
    case "file-watch":
      return "Dosya İzleme";
    case "webhook-ingestion":
      return "Olay Alımı";
    case "background-jobs":
      return "Arka Plan İşleri";
    default:
      return kind;
  }
}

export function getEdgeConnectivityLabel(status: string): string {
  return getHealthStatusLabel(status);
}

export function getTechnicalAlertSeverityLabel(severity: string): string {
  switch (severity) {
    case "critical":
      return "Kritik";
    case "warning":
      return "Uyarı";
    case "info":
      return "Bilgi";
    default:
      return severity;
  }
}

export function getTechnicalAlertSeverityBadgeTone(
  severity: string,
): "neutral" | "information" | "success" | "warning" | "danger" | "ai" {
  switch (severity) {
    case "critical":
      return "danger";
    case "warning":
      return "warning";
    case "info":
      return "information";
    default:
      return "neutral";
  }
}

export function getTechnicalAlertStatusLabel(status: string): string {
  switch (status) {
    case "open":
      return "Açık";
    case "acknowledged":
      return "Onaylandı";
    case "resolved":
      return "Çözüldü";
    case "suppressed":
      return "Bastırıldı";
    default:
      return status;
  }
}

export function getDependencyLabel(key: "database" | "messageBroker" | "aiProvider" | "backgroundJobs"): string {
  switch (key) {
    case "database":
      return "Veritabanı";
    case "messageBroker":
      return "Mesaj Kuyruğu";
    case "aiProvider":
      return "Yapay Zekâ Sağlayıcısı";
    case "backgroundJobs":
      return "Arka Plan İşleri";
    default:
      return key;
  }
}

type SystemStatusHeadlineInput = Readonly<{
  showsFalseHealth: boolean;
  operatingMode: string;
  alerts: readonly unknown[];
  components: readonly { healthStatus: string }[];
  /** Health statuses of the core services shown on the page (API, database, ...). Unknown ones can not be called "normal". */
  coreStatuses?: readonly string[];
}>;

type HeadlineTone = "neutral" | "information" | "success" | "warning" | "danger" | "ai";

/** True only when every observed component is positively healthy; an empty or unknown observation is not "verified". */
export function isSystemStatusVerified(workspace: Pick<SystemStatusHeadlineInput, "components" | "coreStatuses">): boolean {
  const observed = [...workspace.components.map((component) => component.healthStatus), ...(workspace.coreStatuses ?? [])];
  return observed.length > 0 && observed.every((status) => status === "healthy");
}

/**
 * Overall status, derived truthfully: "Her şey normal" is only claimed when every observed component is healthy.
 * Any unknown component makes the overall state "Doğrulanamadı", never normal.
 */
export function resolveSystemStatusHeadline(workspace: SystemStatusHeadlineInput): {
  label: string;
  tone: HeadlineTone;
} {
  if (workspace.showsFalseHealth) {
    return {
      label: "Sağlık göstergesi tutarsız",
      tone: "warning",
    };
  }

  if (isDegradedOperatingMode(workspace.operatingMode)) {
    return {
      label: getOperatingModeLabel(workspace.operatingMode),
      tone: getOperatingModeBadgeTone(workspace.operatingMode),
    };
  }

  if (workspace.alerts.length > 0) {
    return {
      label: `${workspace.alerts.length} teknik uyarı izleniyor`,
      tone: "warning",
    };
  }

  const observed = [...workspace.components.map((component) => component.healthStatus), ...(workspace.coreStatuses ?? [])];
  if (observed.some((status) => status !== "healthy" && status !== "unknown")) {
    return {
      label: "Bazı bileşenlerde sorun var",
      tone: "warning",
    };
  }

  if (!isSystemStatusVerified(workspace)) {
    return {
      label: SYSTEM_STATUS_UNVERIFIED_LABEL,
      tone: "neutral",
    };
  }

  return {
    label: SYSTEM_STATUS_ALL_NORMAL_LABEL,
    tone: "success",
  };
}

export function canShowAllNormalHeadline(workspace: SystemStatusHeadlineInput): boolean {
  return resolveSystemStatusHeadline(workspace).label === SYSTEM_STATUS_ALL_NORMAL_LABEL;
}
