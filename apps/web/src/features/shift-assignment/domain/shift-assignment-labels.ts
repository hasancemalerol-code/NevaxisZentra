import {
  formatShiftWorkforceCount,
  formatShiftWorkforceDateTime,
  getAvailabilityLabel,
  getCompetencyCodeLabel,
  getFreshnessLabel,
  LOCAL_PILOT_DISCLAIMER,
} from "../../shift-workforce/domain/shift-workforce-labels";
import type {
  ShiftAssignmentEligibilityOutcome,
  ShiftAssignmentStatus,
} from "./shift-assignment-types";

export const SHIFT_ASSIGNMENT_RULE_SET_VERSION = "SHIFT-OPERATOR-ELIGIBILITY-V1";

export const PILOT_SHIFT_ASSIGNMENT_CONTEXT = {
  productionOrderReference: "PILOT-PO-PP-001",
  machineAssetId: "bb000001-0096-4000-8000-000000000096",
  productionShiftId: "bb000001-0040-4000-8000-000000000040",
  requiredCompetencyCode: "injection-machine-operation",
} as const;

export const SHIFT_ASSIGNMENT_CONTEXT_OPTIONS = [
  {
    productionOrders: [
      {
        value: "PILOT-PO-PP-001",
        label: "PILOT-PO-PP-001 — Pilot pres üretim emri",
      },
    ],
    machines: [
      {
        value: PILOT_SHIFT_ASSIGNMENT_CONTEXT.machineAssetId,
        label: "Pres Makinesi — Pilot",
      },
    ],
    shifts: [
      {
        value: PILOT_SHIFT_ASSIGNMENT_CONTEXT.productionShiftId,
        label: "Gündüz Vardiyası (V1)",
      },
    ],
    competencyCodes: [
      {
        value: "injection-machine-operation",
        label: "Enjeksiyon makinesi operasyonu",
      },
      {
        value: "mold-change-setup",
        label: "Kalıp değişimi ve kurulum",
      },
      {
        value: "quality-check",
        label: "Kalite kontrol",
      },
    ],
  },
] as const;

export const SHIFT_ASSIGNMENT_STATUS_FILTER_OPTIONS = [
  { value: "", label: "Tüm durumlar" },
  { value: "draft", label: "Taslak" },
  { value: "approved", label: "Onaylandı" },
  { value: "cancelled", label: "İptal edildi" },
] as const;

const OUTCOME_LABELS: Record<ShiftAssignmentEligibilityOutcome, string> = {
  eligible: "Uygun",
  conditional: "Koşullu",
  ineligible: "Uygun değil",
  "insufficient-data": "Yetersiz veri",
};

const STATUS_LABELS: Record<ShiftAssignmentStatus, string> = {
  draft: "Taslak",
  approved: "Onaylandı",
  cancelled: "İptal edildi",
};

const REASON_CODE_LABELS: Record<string, string> = {
  "employment-inactive": "Personel pasif",
  "availability-leave": "İzinli",
  "availability-absent": "Devamsız",
  "availability-unknown": "Müsaitlik bilinmiyor",
  "source-stale": "Bayat kaynak verisi",
  "source-unknown": "Kaynak tazeliği bilinmiyor",
  "competency-missing": "Gerekli yetkinlik eksik",
  "competency-pending-verification": "Yetkinlik doğrulaması bekliyor",
  "competency-expiring": "Yetkinlik süresi yakında doluyor",
  "competency-expired": "Yetkinlik süresi dolmuş",
  "shift-reference-mismatch": "Vardiya referansı uyuşmuyor",
  "assignment-overlap": "Çakışan onaylı atama",
  "facility-scope-mismatch": "Tesis kapsamı dışında",
  "required-data-missing": "Gerekli veri eksik",
  "all-criteria-met": "Tüm kriterler karşılandı",
};

const SEVERITY_LABELS: Record<string, string> = {
  pass: "Uygun",
  warn: "Uyarı",
  fail: "Engel",
  info: "Bilgi",
};

const AUDIT_EVENT_LABELS: Record<string, string> = {
  "assignment-created": "Atama oluşturuldu",
  "assignment-approved": "Atama onaylandı",
  "assignment-cancelled": "Atama iptal edildi",
};

export function formatShiftAssignmentCount(value: number | null | undefined): string {
  return formatShiftWorkforceCount(value);
}

export function formatShiftAssignmentDateTime(value?: string | null): string {
  return formatShiftWorkforceDateTime(value);
}

export function getEligibilityOutcomeLabel(
  outcome: string,
  label?: string,
): string {
  if (label) {
    return label;
  }

  return OUTCOME_LABELS[outcome as ShiftAssignmentEligibilityOutcome] ?? "Bilinmiyor";
}

export function getAssignmentStatusLabel(status: string, label?: string): string {
  if (label) {
    return label;
  }

  return STATUS_LABELS[status as ShiftAssignmentStatus] ?? "Bilinmiyor";
}

export function getReasonCodeLabel(code: string, label?: string): string {
  if (label) {
    return label;
  }

  return REASON_CODE_LABELS[code] ?? "Değerlendirme nedeni";
}

export function getSeverityLabel(severity: string, label?: string): string {
  if (label) {
    return label;
  }

  return SEVERITY_LABELS[severity] ?? "Bilgi";
}

export function getAuditEventTypeLabel(eventType: string, label?: string): string {
  if (label) {
    return label;
  }

  return AUDIT_EVENT_LABELS[eventType] ?? "Kayıt olayı";
}

export function getEvidenceSourceLabel(source: string, label?: string): string {
  if (label) {
    return label;
  }

  if (source === "local-development-pilot") {
    return LOCAL_PILOT_DISCLAIMER;
  }

  switch (source) {
    case "shift-workforce":
      return "Vardiya personel kaydı";
    case "competency-enrichment":
      return "Operasyonel yetkinlik";
    case "assignment-registry":
      return "Atama kaydı";
    case "eligibility-engine":
      return "Uygunluk kuralları";
    default:
      return "Kaynak";
  }
}

export {
  getAvailabilityLabel,
  getCompetencyCodeLabel,
  getFreshnessLabel,
  LOCAL_PILOT_DISCLAIMER,
};

export function outcomeBadgeClass(outcome: string): string {
  switch (outcome) {
    case "eligible":
      return "shift-workforce-badge shift-workforce-badge--available";
    case "conditional":
      return "shift-workforce-badge shift-workforce-badge--pending";
    case "ineligible":
      return "shift-workforce-badge shift-workforce-badge--expired";
    case "insufficient-data":
      return "shift-workforce-badge shift-workforce-badge--unknown";
    default:
      return "shift-workforce-badge shift-workforce-badge--unknown";
  }
}

export function assignmentStatusBadgeClass(status: string): string {
  switch (status) {
    case "approved":
      return "shift-workforce-badge shift-workforce-badge--verified";
    case "draft":
      return "shift-workforce-badge shift-workforce-badge--pending";
    case "cancelled":
      return "shift-workforce-badge shift-workforce-badge--inactive";
    default:
      return "shift-workforce-badge shift-workforce-badge--unknown";
  }
}

export function severityBadgeClass(severity: string): string {
  switch (severity) {
    case "pass":
      return "shift-workforce-badge shift-workforce-badge--available";
    case "warn":
      return "shift-workforce-badge shift-workforce-badge--pending";
    case "fail":
      return "shift-workforce-badge shift-workforce-badge--expired";
    default:
      return "shift-workforce-badge shift-workforce-badge--unknown";
  }
}

export function canApproveCandidate(outcome: string): boolean {
  return outcome === "eligible" || outcome === "conditional";
}

export function canCreateDraftCandidate(outcome: string): boolean {
  return outcome === "eligible" || outcome === "conditional";
}
