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
] as const;
