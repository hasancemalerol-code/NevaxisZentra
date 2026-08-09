import type {
  QualityCorrectiveActionDetail,
  QualityInspectionSummary,
  QualityNcrSummary,
} from "../domain/quality-control";
import { getQualityStatusLabel } from "../domain/quality-control";

export function getQualityReleaseStatusLabel(status: string): string {
  switch (status) {
    case "released":
      return "Serbest Bırakıldı";
    case "rejected":
      return "Reddedildi";
    case "ready-for-review":
      return "İnceleme Bekliyor";
    case "not-ready":
      return "Bloke";
    case "pending":
      return "Bekliyor";
    default:
      return getQualityStatusLabel(status);
  }
}

export function getQualityOverallResultLabel(result: string): string {
  switch (result) {
    case "pass":
      return "Başarılı";
    case "fail":
      return "Başarısız";
    case "pending":
      return "Bekliyor";
    default:
      return getQualityStatusLabel(result);
  }
}

export function formatQualityAge(isoTimestamp: string): string {
  const timestamp = Date.parse(isoTimestamp);
  if (Number.isNaN(timestamp)) {
    return "—";
  }

  const diffMs = Date.now() - timestamp;
  const hours = Math.floor(diffMs / (1000 * 60 * 60));
  if (hours < 1) {
    return "Az önce";
  }

  if (hours < 24) {
    return `${hours} sa`;
  }

  const days = Math.floor(hours / 24);
  return `${days} gün`;
}

export function isCorrectiveActionOverdue(
  action: Pick<QualityCorrectiveActionDetail, "status" | "dueAtUtc">,
): boolean {
  if (!action.dueAtUtc) {
    return false;
  }

  if (action.status === "completed" || action.status === "cancelled") {
    return false;
  }

  return Date.parse(action.dueAtUtc) < Date.now();
}

export type QualityCriticalAlert = Readonly<{
  id: string;
  kind: "failed-inspection" | "critical-ncr" | "quality-hold" | "overdue-action" | "release-blocked";
  title: string;
  reference: string;
  reason: string;
  statusLabel: string;
  href: string;
  tone: "danger" | "information" | "neutral";
}>;

const OPEN_NCR_STATUSES = new Set(["open", "under-review", "action-required"]);

export function isOpenNcrStatus(status: string): boolean {
  return OPEN_NCR_STATUSES.has(status);
}

export function buildQualityCriticalAlerts(input: Readonly<{
  inspections: readonly QualityInspectionSummary[];
  ncrs: readonly QualityNcrSummary[];
  correctiveActions: readonly Readonly<{
    id: string;
    title: string;
    ncrId: string;
    ncrNumber: string;
    status: string;
    dueAtUtc?: string;
  }>[];
}>): readonly QualityCriticalAlert[] {
  const alerts: QualityCriticalAlert[] = [];

  for (const inspection of input.inspections) {
    if (inspection.overallResult === "fail") {
      alerts.push({
        id: `fail-${inspection.id}`,
        kind: "failed-inspection",
        title: "Başarısız kalite kontrolü",
        reference: inspection.inspectionNumber,
        reason: inspection.productionOrderReference ?? inspection.productionOrderId,
        statusLabel: getQualityOverallResultLabel(inspection.overallResult),
        href: `/kalite/kontroller/${inspection.id}`,
        tone: "danger",
      });
    }

    if (inspection.releaseStatus === "rejected") {
      alerts.push({
        id: `release-${inspection.id}`,
        kind: "release-blocked",
        title: "Serbest bırakma reddedildi",
        reference: inspection.inspectionNumber,
        reason: inspection.productReference ?? inspection.planCode ?? "Kalite incelemesi",
        statusLabel: getQualityReleaseStatusLabel(inspection.releaseStatus),
        href: `/kalite/kontroller/${inspection.id}`,
        tone: "danger",
      });
    }
  }

  for (const ncr of input.ncrs) {
    if (ncr.severity === "critical" && isOpenNcrStatus(ncr.status)) {
      alerts.push({
        id: `ncr-${ncr.id}`,
        kind: "critical-ncr",
        title: "Kritik NCR",
        reference: ncr.ncrNumber,
        reason: ncr.title,
        statusLabel: getQualityStatusLabel(ncr.severity),
        href: `/kalite/ncr/${ncr.id}`,
        tone: "danger",
      });
    }

    if (ncr.holdStatus === "active") {
      alerts.push({
        id: `hold-${ncr.id}`,
        kind: "quality-hold",
        title: "Aktif kalite blokesi",
        reference: ncr.ncrNumber,
        reason: ncr.productionOrderReference ?? ncr.productReference ?? "Kalite blokesi",
        statusLabel: "Kalite Blokesi: Aktif",
        href: `/kalite/ncr/${ncr.id}`,
        tone: "information",
      });
    }
  }

  for (const action of input.correctiveActions) {
    if (isCorrectiveActionOverdue(action)) {
      alerts.push({
        id: `ca-${action.id}`,
        kind: "overdue-action",
        title: "Geciken düzeltici aksiyon",
        reference: action.ncrNumber,
        reason: action.title,
        statusLabel: getQualityStatusLabel(action.status),
        href: `/kalite/ncr/${action.ncrId}`,
        tone: "information",
      });
    }
  }

  return alerts
    .sort((a, b) => {
      const priority = (kind: QualityCriticalAlert["kind"]) => {
        switch (kind) {
          case "critical-ncr":
          case "quality-hold":
            return 0;
          case "failed-inspection":
          case "release-blocked":
            return 1;
          default:
            return 2;
        }
      };

      return priority(a.kind) - priority(b.kind);
    })
    .slice(0, 8);
}
