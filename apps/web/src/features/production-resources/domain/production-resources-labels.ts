export const PRODUCTION_RESOURCES_ROUTE_PATH = "/uretim/kaynaklar";

export const PRODUCTION_RESOURCES_TITLE = "Üretim Kaynakları";

export const PRODUCTION_RESOURCES_SUBTITLE =
  "Hat, istasyon ve kaynak durumlarının birleşik operasyon görünümü.";

export const PRODUCTION_RESOURCES_EMPTY =
  "Seçilen filtrelerle eşleşen üretim kaynağı bulunmuyor.";

export const PRODUCTION_RESOURCES_ZERO_FACTORY =
  "Henüz üretim kaynağı tanımlanmadı. Veri Aktarım Merkezi üzerinden toplu yükleme yapabilirsiniz.";

export const PRODUCTION_RESOURCE_TYPE_LABELS: Record<string, string> = {
  machine: "Makine",
  human: "Operatör",
  robot: "Robot / Cobot",
  tool: "Takım",
  conveyor: "Konveyör",
  buffer: "Buffer / WIP",
  quality_station: "Kalite / Test",
};

export const PRODUCTION_RESOURCE_TYPE_FILTER_OPTIONS = [
  { value: "", label: "Tüm tipler" },
  { value: "machine", label: "Makine" },
  { value: "human", label: "Operatör" },
  { value: "robot", label: "Robot" },
  { value: "tool", label: "Takım" },
  { value: "conveyor", label: "Konveyör" },
  { value: "buffer", label: "Buffer / WIP" },
  { value: "quality_station", label: "Kalite / Test" },
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
  return PRODUCTION_RESOURCE_TYPE_LABELS[resourceType] ?? resourceType;
}
