// ── Exception Types ────────────────────────────────────────────────────────────

export type ProductionExceptionType =
  | "stale-downtime"
  | "stale-setup"
  | "quantity-anomaly"
  | "cycle-anomaly"
  | "schedule-overrun"
  | "handoff-inconsistency"
  | "operator-attention"
  | "other";

export type ProductionExceptionSeverity = "info" | "warning" | "critical";

export type ProductionExceptionStatus = "open" | "acknowledged" | "resolved";

export type ProductionExecutionExceptionSummary = Readonly<{
  id: string;
  exceptionType: ProductionExceptionType;
  severity: ProductionExceptionSeverity;
  status: ProductionExceptionStatus;
  message: string;
  raisedAtUtc: string;
  raisedBySubjectId: string;
  resolvedAtUtc?: string;
  resolvedBySubjectId?: string;
  resolutionNote?: string;
  version: number;
}>;

// ── Health Summary ─────────────────────────────────────────────────────────────

export type ProductionExecutionHealthStatus = "normal" | "attention" | "critical";

export type ProductionExecutionHealthSummary = Readonly<{
  status: ProductionExecutionHealthStatus;
  openExceptionCount: number;
  criticalExceptionCount: number;
  hasActiveDowntime: boolean;
  downtimeIsStale: boolean;
  hasActiveSetup: boolean;
  setupIsStale: boolean;
  quantityProgress?: number;
  scheduleOverrun: boolean;
  lastOperationalActivityAtUtc?: string;
  requiresAttention: boolean;
}>;

// ── Label helpers (Turkish) ────────────────────────────────────────────────────

export function getExceptionTypeLabel(type: ProductionExceptionType): string {
  switch (type) {
    case "stale-downtime":
      return "Uzun süreli duruş";
    case "stale-setup":
      return "Uzun süreli setup";
    case "quantity-anomaly":
      return "Miktar anomalisi";
    case "cycle-anomaly":
      return "Çevrim anomalisi";
    case "schedule-overrun":
      return "Zamanlama aşımı";
    case "handoff-inconsistency":
      return "Devir tutarsızlığı";
    case "operator-attention":
      return "Operatör dikkati";
    case "other":
      return "Diğer";
  }
}

export function getExceptionSeverityLabel(severity: ProductionExceptionSeverity): string {
  switch (severity) {
    case "info":
      return "Bilgi";
    case "warning":
      return "Uyarı";
    case "critical":
      return "Kritik";
  }
}

export function getExceptionStatusLabel(status: ProductionExceptionStatus): string {
  switch (status) {
    case "open":
      return "Açık";
    case "acknowledged":
      return "Görüldü";
    case "resolved":
      return "Çözüldü";
  }
}

export function getHealthStatusLabel(
  status: ProductionExecutionHealthStatus,
): string {
  switch (status) {
    case "normal":
      return "Normal";
    case "attention":
      return "Dikkat gerekiyor";
    case "critical":
      return "Kritik";
  }
}

export const PRODUCTION_EXCEPTION_TYPES: ProductionExceptionType[] = [
  "stale-downtime",
  "stale-setup",
  "quantity-anomaly",
  "cycle-anomaly",
  "schedule-overrun",
  "handoff-inconsistency",
  "operator-attention",
  "other",
];

export const PRODUCTION_EXCEPTION_SEVERITIES: ProductionExceptionSeverity[] = [
  "info",
  "warning",
  "critical",
];
