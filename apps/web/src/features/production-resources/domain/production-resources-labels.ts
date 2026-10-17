import { humanizeEnum } from "@/lib/presentation/enum-labels";

export const PRODUCTION_RESOURCES_ROUTE_PATH = "/uretim/kaynaklar";

export const PRODUCTION_RESOURCES_TITLE = "Makineler ve Kaynaklar";

export const PRODUCTION_RESOURCES_SUBTITLE =
  "Hat, istasyon ve üretim kaynaklarının (makine, montaj, robot, ara stok alanları) birleşik operasyon görünümü.";

export const PRODUCTION_RESOURCES_EMPTY =
  "Seçilen filtrelerle eşleşen üretim kaynağı bulunmuyor.";

export const PRODUCTION_RESOURCES_ZERO_FACTORY = "Henüz üretim kaynağı yok.";
export const PRODUCTION_RESOURCES_ZERO_FACTORY_CTA = "Veri Aktarım Merkezi'nden yükle";

export const PRODUCTION_RESOURCE_TYPE_LABELS: Record<string, string> = {
  machine: "Makine",
  human: "Montaj / İnsan İstasyonu",
  robot: "Robot",
  tool: "Kalıp ve Takım",
  mold: "Kalıp ve Takım",
  conveyor: "Konveyör",
  buffer: "Ara stok alanı",
  quality_station: "Test / Kalite",
  quality: "Test / Kalite",
};

export const PRODUCTION_RESOURCE_TYPE_FILTER_OPTIONS = [
  { value: "", label: "Tümü" },
  { value: "machine", label: "Makineler" },
  { value: "human", label: "Montaj / İnsan İstasyonları" },
  { value: "robot", label: "Robotlar" },
  { value: "buffer", label: "Ara stok alanları" },
  { value: "conveyor", label: "Konveyörler" },
  { value: "quality_station", label: "Test / Kalite" },
  { value: "tool", label: "Kalıp ve Takımlar" },
] as const;

export const PRODUCTION_RESOURCE_STATE_FILTER_OPTIONS = [
  { value: "", label: "Tüm durumlar" },
  { value: "running", label: "Çalışıyor" },
  { value: "idle", label: "Boşta" },
  { value: "waiting", label: "Bekliyor" },
  { value: "stopped", label: "Durdu" },
  { value: "fault", label: "Arıza" },
  { value: "risk", label: "Risk / Kısıt" },
] as const;

export function getProductionResourceTypeLabel(resourceType: string): string {
  return PRODUCTION_RESOURCE_TYPE_LABELS[resourceType] ?? humanizeEnum(resourceType);
}

/** One topology node per station: several resources can share a station, but the station is drawn once. */
export function uniqueTopologyStations<T extends Readonly<{ stationId: string }>>(rows: readonly T[]): T[] {
  const seen = new Set<string>();
  return rows.filter((row) => {
    if (seen.has(row.stationId)) return false;
    seen.add(row.stationId);
    return true;
  });
}
