import type { QualityCorrectiveActionDetail } from "../domain/quality-control";
import { getQualityStatusLabel } from "../domain/quality-control";
import { isCorrectiveActionOverdue } from "./quality-dashboard-labels";

export function getQualityHoldLabel(holdStatus: string, holdActive: boolean): string {
  if (holdActive) {
    return "Aktif";
  }

  switch (holdStatus) {
    case "released":
      return "Serbest Bırakıldı";
    case "none":
      return "Yok";
    default:
      return getQualityStatusLabel(holdStatus);
  }
}

export function getCorrectiveActionStatusTone(
  action: Pick<QualityCorrectiveActionDetail, "status" | "dueAtUtc">,
): "neutral" | "information" | "success" | "danger" | "warning" {
  if (isCorrectiveActionOverdue(action)) {
    return "danger";
  }

  switch (action.status) {
    case "completed":
      return "success";
    case "in-progress":
      return "information";
    case "open":
      return "warning";
    default:
      return "neutral";
  }
}

export function getCorrectiveActionStatusLabel(
  action: Pick<QualityCorrectiveActionDetail, "status" | "dueAtUtc">,
): string {
  if (isCorrectiveActionOverdue(action)) {
    return "Gecikmiş";
  }

  switch (action.status) {
    case "open":
      return "Açık";
    case "in-progress":
      return "Devam Ediyor";
    case "completed":
      return "Tamamlandı";
    default:
      return getQualityStatusLabel(action.status);
  }
}

export function formatQualityDateTime(isoTimestamp?: string): string {
  if (!isoTimestamp) {
    return "—";
  }

  return new Date(isoTimestamp).toLocaleString("tr-TR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}
