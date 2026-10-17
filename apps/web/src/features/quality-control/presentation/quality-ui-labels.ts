import { formatDateTime } from "@/lib/presentation/format";
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

  return formatDateTime(isoTimestamp);
}

const CATEGORY_LABELS: Readonly<Record<string, string>> = {
  surface: "Yüzey",
  dimensional: "Ölçü",
  dimension: "Ölçü",
  appearance: "Görünüm",
  cosmetic: "Görünüm",
  material: "Malzeme",
  process: "Proses",
  assembly: "Montaj",
  packaging: "Ambalaj",
  labeling: "Etiketleme",
  labelling: "Etiketleme",
  functional: "Fonksiyonel",
  electrical: "Elektrik",
  mechanical: "Mekanik",
  contamination: "Kontaminasyon",
  supplier: "Tedarikçi",
  documentation: "Dokümantasyon",
  safety: "Güvenlik",
  workmanship: "İşçilik",
  tooling: "Takım",
  mold: "Kalıp",
  "inspection-failure": "Kontrol uygunsuzluğu",
  inspection_failure: "Kontrol uygunsuzluğu",
  other: "Diğer",
};

/** Turkish label for a free-text NCR category or defect dimension value; unknown values are humanized, never shown raw. */
export function getQualityCategoryLabel(value: string | null | undefined): string {
  const key = (value ?? "").trim().toLowerCase();
  if (!key) {
    return "—";
  }

  return CATEGORY_LABELS[key] ?? getQualityStatusLabel(key);
}
