// Edge Gateway bounded-context — presentation labels (Turkish UI)

export const EDGE_GATEWAY_VENDOR_LABELS: Record<string, string> = {
  simulator: "Makine Simülatörü",
  "mqtt-bridge": "MQTT Köprüsü",
  "opcua-bridge": "OPC UA Köprüsü",
};

export const EDGE_GATEWAY_STATUS_LABELS: Record<string, string> = {
  unknown: "Bilinmiyor",
  online: "Çevrimiçi",
  offline: "Çevrimdışı",
  degraded: "Bozuk",
  disabled: "Devre dışı",
};

export const MACHINE_STATUS_LABELS: Record<string, string> = {
  unknown: "Bilinmiyor",
  running: "Çalışıyor",
  idle: "Boşta",
  stopped: "Durdurulmuş",
  fault: "Hata",
  maintenance: "Bakımda",
};

export const MACHINE_EVENT_KIND_LABELS: Record<string, string> = {
  heartbeat: "Nabız",
  "status-change": "Durum Değişikliği",
  telemetry: "Telemetri",
  alarm: "Alarm",
  fault: "Hata",
  "cycle-start": "Döngü Başlangıcı",
  "cycle-end": "Döngü Sonu",
  // EDGE-2 canonical typed signals
  "cycle-completed": "Çevrim Tamamlandı (Gözlenen)",
  "counter-observed": "Sayaç Gözlemi",
};

export const TELEMETRY_QUALITY_LABELS: Record<string, string> = {
  good: "İyi",
  uncertain: "Belirsiz",
  bad: "Hatalı",
  unknown: "Bilinmiyor",
};

export const TELEMETRY_METRIC_LABELS: Record<string, string> = {
  cycle_time_ms: "Çevrim Süresi",
  machine_counter: "Makine Sayacı",
  temperature_zone_1: "Sıcaklık (Bölge 1)",
  hydraulic_pressure: "Hidrolik Basınç",
};

export const CONNECTIVITY_LABELS: Record<string, string> = {
  online: "Çevrimiçi",
  offline: "Çevrimdışı",
};

export const MACHINE_ASSIGNMENT_STATUS_LABELS: Record<string, string> = {
  active: "Aktif",
  inactive: "Pasif",
};

export function getEdgeGatewayVendorLabel(vendorKey: string): string {
  return EDGE_GATEWAY_VENDOR_LABELS[vendorKey] ?? vendorKey;
}

export function getEdgeGatewayStatusLabel(status: string): string {
  return EDGE_GATEWAY_STATUS_LABELS[status] ?? status;
}

export function getMachineStatusLabel(status: string): string {
  return MACHINE_STATUS_LABELS[status] ?? status;
}

export function getMachineEventKindLabel(kind: string): string {
  return MACHINE_EVENT_KIND_LABELS[kind] ?? kind;
}

export function getEdgeGatewayStatusClass(status: string): string {
  switch (status) {
    case "online":
      return "edge-status--online";
    case "offline":
      return "edge-status--offline";
    case "degraded":
      return "edge-status--degraded";
    case "disabled":
      return "edge-status--disabled";
    default:
      return "edge-status--unknown";
  }
}

export function getMachineStatusClass(status: string): string {
  switch (status) {
    case "running":
      return "machine-status--running";
    case "idle":
      return "machine-status--idle";
    case "stopped":
      return "machine-status--stopped";
    case "fault":
      return "machine-status--fault";
    case "maintenance":
      return "machine-status--maintenance";
    default:
      return "machine-status--unknown";
  }
}

export function getMachineReadinessLabel(isReady: boolean): string {
  return isReady ? "Üretime hazır" : "Üretime hazır değil";
}

export function getEdgeSimulatorDisclaimer(): string {
  return "Simülatör modu — gerçek PLC/makine bağlantısı yok. Deterministik test verileri.";
}

export function getMachineConnectivityLabel(
  lastSeenAtUtc: string | null | undefined,
): string {
  if (!lastSeenAtUtc) return "Henüz iletişim yok";
  const diffMs = Date.now() - new Date(lastSeenAtUtc).getTime();
  const diffMin = Math.floor(diffMs / 60_000);
  if (diffMin < 1) return "Az önce görüldü";
  if (diffMin < 60) return `${diffMin} dk önce görüldü`;
  const diffHr = Math.floor(diffMin / 60);
  if (diffHr < 24) return `${diffHr} sa önce görüldü`;
  return `${Math.floor(diffHr / 24)} gün önce görüldü`;
}

export function getTelemetryQualityLabel(quality: string): string {
  return TELEMETRY_QUALITY_LABELS[quality] ?? quality;
}

export function getTelemetryQualityClass(quality: string): string {
  switch (quality) {
    case "good":
      return "telemetry-quality--good";
    case "uncertain":
      return "telemetry-quality--uncertain";
    case "bad":
      return "telemetry-quality--bad";
    default:
      return "telemetry-quality--unknown";
  }
}

export function getMetricLabel(metricKey: string): string {
  return TELEMETRY_METRIC_LABELS[metricKey] ?? metricKey;
}

export function getConnectivityLabel(connectivity: string): string {
  return CONNECTIVITY_LABELS[connectivity] ?? connectivity;
}

export function formatDurationMs(ms: number | null | undefined): string {
  if (ms == null) return "—";
  if (ms < 1000) return `${ms} ms`;
  if (ms < 60_000) return `${(ms / 1000).toFixed(1)} s`;
  return `${(ms / 60_000).toFixed(1)} dk`;
}

export const EDGE_CAPABILITIES = [
  "Makine kayıt defteri (CNC, pres, vb.)",
  "Edge gateway kaydı",
  "Makine ↔ gateway atama",
  "Kiracı / tesis izolasyonu (RLS)",
  "Sadece okunur adaptör sözleşmesi",
  "Deterministik makine simülatörü (CNC-001, PRESS-001)",
  "Kanonik makine olayı zarfı (kaynak zaman damgası, alınma zamanı, sıra no.)",
  "Telemetri ingest (idempotency key tabanlı tekilleştirme)",
  "Sıra dışı tespit (SourceSequence karşılaştırması)",
  "Makine son-görülme / hazırlık projeksiyonu",
  "API yeniden başlatmalarında kalıcılık",
  // EDGE-2
  "Yazılı telemetri örnekleri (MetricKey, Numeric/Text/Bool, Birim, Kalite)",
  "Son telemetri projeksiyonu (makine+metrik bazlı)",
  "Sınırlı telemetri geçmişi (from/to/page)",
  "Gözlenen çevrim tamamlama olayı (machine.cycle.completed)",
  "Gözlenen çevrim sayacı projeksiyonu — resmi üretim miktarı DEĞİLDİR",
  "Sayaç gözlemi (machine.counter.observed) + sıfırlama tespiti",
  "Durum geçiş geçmişi (machine_state_transitions)",
  "Makine sinyal projeksiyonu (MachineSignalProjection) — salt okunur",
  "Çoğaltma güvenli sayaç (yinelenen çevrim ObservedCycleCount'u artırmaz)",
  "Sıra dışı güvenli projeksiyonlar (en son sıra gerilemez)",
  "Üretim mutasyonu YOK — EDGE sadece gözlem sahibidir",
] as const;
