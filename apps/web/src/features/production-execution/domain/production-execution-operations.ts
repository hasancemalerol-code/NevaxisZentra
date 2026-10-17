import { formatDateTime } from "@/lib/presentation/format";
export type ProductionDowntimeCategory = "planned" | "unplanned";

export type ProductionDowntimeReasonCode =
  | "break"
  | "planned-maintenance"
  | "material-change"
  | "cleaning"
  | "quality-check"
  | "operator-change"
  | "machine-fault"
  | "mold-fault"
  | "material-shortage"
  | "quality-hold"
  | "utility-failure"
  | "operator-unavailable"
  | "process-instability"
  | "other";

export const PLANNED_DOWNTIME_REASONS: ReadonlyArray<{
  code: ProductionDowntimeReasonCode;
  label: string;
}> = [
  { code: "break", label: "Mola" },
  { code: "planned-maintenance", label: "Planlı Bakım" },
  { code: "material-change", label: "Malzeme Değişimi" },
  { code: "cleaning", label: "Temizlik" },
  { code: "quality-check", label: "Kalite Kontrolü" },
  { code: "operator-change", label: "Operatör Değişimi" },
];

export const UNPLANNED_DOWNTIME_REASONS: ReadonlyArray<{
  code: ProductionDowntimeReasonCode;
  label: string;
}> = [
  { code: "machine-fault", label: "Makine Arızası" },
  { code: "mold-fault", label: "Kalıp Arızası" },
  { code: "material-shortage", label: "Malzeme Eksikliği" },
  { code: "quality-hold", label: "Kalite Beklemesi" },
  { code: "utility-failure", label: "Hizmet Arızası" },
  { code: "operator-unavailable", label: "Operatör Yok" },
  { code: "process-instability", label: "Proses Kararsızlığı" },
  { code: "other", label: "Diğer" },
];

export function getDowntimeReasonLabel(code: string): string {
  const all = [...PLANNED_DOWNTIME_REASONS, ...UNPLANNED_DOWNTIME_REASONS];
  return all.find((r) => r.code === code)?.label ?? code;
}

export function getDowntimeCategoryLabel(category: string): string {
  if (category === "planned") return "Planlı";
  if (category === "unplanned") return "Plansız";
  return category;
}

export function requiresDowntimeComment(reasonCode: string): boolean {
  return reasonCode === "other";
}

export type ProductionSetupType =
  | "initial-setup"
  | "mold-change"
  | "material-change"
  | "product-change"
  | "parameter-adjustment"
  | "other";

export const SETUP_TYPES: ReadonlyArray<{
  code: ProductionSetupType;
  label: string;
}> = [
  { code: "initial-setup", label: "İlk Setup" },
  { code: "mold-change", label: "Kalıp Değişimi" },
  { code: "material-change", label: "Malzeme Değişimi" },
  { code: "product-change", label: "Ürün Değişimi" },
  { code: "parameter-adjustment", label: "Parametre Ayarı" },
  { code: "other", label: "Diğer" },
];

export function getSetupTypeLabel(code: string): string {
  return SETUP_TYPES.find((s) => s.code === code)?.label ?? code;
}

export type ProductionDowntimeSummary = Readonly<{
  id: string;
  reasonCode: string;
  category: string;
  startedAtUtc: string;
  endedAtUtc?: string;
  durationSeconds?: number;
  comment?: string;
}>;

export type ProductionSetupSessionSummary = Readonly<{
  id: string;
  setupType: string;
  startedAtUtc: string;
  completedAtUtc?: string;
  durationSeconds?: number;
  comment?: string;
}>;

export type ProductionCycleEntrySummary = Readonly<{
  id: string;
  cycleNumber: number;
  cycleDurationMilliseconds: number;
  quantityProduced?: number;
  recordedAtUtc: string;
  source: string;
  comment?: string;
}>;

export type ProductionRuntimeSummary = Readonly<{
  actualStartAtUtc?: string;
  currentElapsedSeconds: number;
  totalDowntimeSeconds: number;
  totalSetupSeconds: number;
  netRunningSeconds: number;
  manualCycleCount: number;
  averageManualCycleMilliseconds?: number;
  lastCycleAtUtc?: string;
  activeDowntime?: ProductionDowntimeSummary;
  activeSetup?: ProductionSetupSessionSummary;
}>;

export function formatDuration(seconds: number): string {
  if (seconds < 60) return `${Math.round(seconds)} sn`;
  if (seconds < 3600) return `${Math.floor(seconds / 60)} dk`;
  const h = Math.floor(seconds / 3600);
  const m = Math.floor((seconds % 3600) / 60);
  if (m === 0) return `${h} sa`;
  return `${h} sa ${m} dk`;
}

export function formatDurationMs(ms: number): string {
  if (ms < 1000) return `${ms} ms`;
  const seconds = ms / 1000;
  if (seconds < 60) return `${seconds.toFixed(1)} sn`;
  if (seconds < 3600) return `${Math.floor(seconds / 60)} dk ${Math.round(seconds % 60)} sn`;
  const h = Math.floor(seconds / 3600);
  const m = Math.floor((seconds % 3600) / 60);
  return `${h} sa ${m} dk`;
}

export function formatElapsedSince(isoUtc: string): string {
  try {
    const started = new Date(isoUtc).getTime();
    const now = Date.now();
    const seconds = Math.max(0, Math.floor((now - started) / 1000));
    return formatDuration(seconds);
  } catch {
    return "—";
  }
}

export function formatUtcShort(isoUtc: string): string {
  try {
    return formatDateTime(isoUtc);
  } catch {
    return isoUtc;
  }
}
