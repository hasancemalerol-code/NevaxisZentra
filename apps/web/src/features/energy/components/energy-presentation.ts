import type { LucideIcon } from "lucide-react";
import {
  Activity,
  Coins,
  Flame,
  Gauge,
  Minus,
  TrendingDown,
  TrendingUp,
  Zap,
} from "lucide-react";

import type { EnergyKpiCard, EnergyTrendPoint } from "../domain/energy-types";

export type EnergyKpiTone = "info" | "success" | "warning" | "danger" | "teal";

export type EnergyKpiPresentation = Readonly<{
  tone: EnergyKpiTone;
  icon: LucideIcon;
  statusLabel: string;
  sparklineValues: readonly number[] | null;
  displayValue: string;
  displayUnit: string | null;
  scopeLabel: string | null;
  accessibleSummary: string | null;
}>;

export type EnergyKpiEvidenceRail = Readonly<{
  progressPercent: number;
  markerPercent: number | null;
  railLabel: string;
  accessibleSummary: string | null;
}>;

const KPI_ICONS: Record<EnergyKpiCard["id"], LucideIcon> = {
  "total-consumption": Zap,
  "peak-power": Gauge,
  "energy-intensity": Activity,
  "target-variance": TrendingDown,
  "idle-consumption": Flame,
  "estimated-cost": Coins,
};

const DISPLAY_PRECISION = 0.05;

type LowerIsBetterOutcome = "below" | "at" | "above" | "no-target" | "incompatible";

type MetricComparison = Readonly<{
  outcome: LowerIsBetterOutcome;
  actual: number | null;
  target: number | null;
  statusLabel: string;
  tone: EnergyKpiTone;
  icon: LucideIcon;
}>;

function parseLeadingNumber(value: string): number | null {
  const match = value.match(/-?[\d.,]+/);
  if (!match) return null;
  return Number.parseFloat(match[0].replace(/\./g, "").replace(",", "."));
}

function parseTargetFromMeta(meta: string): number | null {
  const match = meta.match(/(?:Hedef|Onaylı hedef):\s*([\d.,]+)/i);
  if (!match) return null;
  return parseLeadingNumber(match[1]);
}

function extractTargetDisplay(meta: string): string | null {
  const match = meta.match(/(?:Hedef|Onaylı hedef):\s*(.+)$/i);
  return match ? match[1].trim() : null;
}

function compareLowerIsBetter(actual: number | null, target: number | null): LowerIsBetterOutcome {
  if (actual === null || Number.isNaN(actual) || !Number.isFinite(actual)) {
    return "incompatible";
  }
  if (target === null || Number.isNaN(target) || !Number.isFinite(target)) {
    return "no-target";
  }
  if (Math.abs(actual - target) <= DISPLAY_PRECISION) {
    return "at";
  }
  return actual < target ? "below" : "above";
}

function lowerIsBetterPresentation(
  outcome: LowerIsBetterOutcome,
  icon: LucideIcon,
): Pick<MetricComparison, "outcome" | "statusLabel" | "tone" | "icon"> {
  switch (outcome) {
    case "below":
      return {
        outcome,
        tone: "success",
        statusLabel: "Hedefin altında",
        icon: TrendingDown,
      };
    case "at":
      return {
        outcome,
        tone: "success",
        statusLabel: "Hedefle uyumlu",
        icon: Minus,
      };
    case "above":
      return {
        outcome,
        tone: "warning",
        statusLabel: "Hedefin üzerinde",
        icon: TrendingUp,
      };
    case "no-target":
      return {
        outcome,
        tone: "info",
        statusLabel: "Karşılaştırma için hedef yok",
        icon: Minus,
      };
    default:
      return {
        outcome: "incompatible",
        tone: "info",
        statusLabel: "Karşılaştırılamıyor",
        icon,
      };
  }
}

function resolveLowerIsBetterComparison(
  kpi: EnergyKpiCard,
  icon: LucideIcon,
): MetricComparison {
  const actual = parseLeadingNumber(kpi.value);
  const target = parseTargetFromMeta(kpi.meta);
  const outcome = compareLowerIsBetter(actual, target);
  const presentation = lowerIsBetterPresentation(outcome, icon);
  return {
    ...presentation,
    actual,
    target,
    icon: presentation.icon,
  };
}

function resolveTargetVarianceComparison(kpi: EnergyKpiCard): MetricComparison {
  const icon = KPI_ICONS["target-variance"];
  const target = parseTargetFromMeta(kpi.meta);
  const variance = parseLeadingNumber(kpi.value);

  if (kpi.value === "—" || variance === null || target === null) {
    return {
      ...lowerIsBetterPresentation("no-target", icon),
      actual: null,
      target,
      icon: Minus,
    };
  }

  const actual = target + variance;
  const outcome = compareLowerIsBetter(actual, target);
  const presentation = lowerIsBetterPresentation(outcome, icon);
  return {
    ...presentation,
    actual,
    target,
    icon: presentation.icon,
  };
}

function resolveConflictOrDataState(kpi: EnergyKpiCard): MetricComparison | null {
  if (kpi.evidenceLabel === "Kaynak çelişkisi") {
    return {
      outcome: "incompatible",
      actual: parseLeadingNumber(kpi.value),
      target: parseTargetFromMeta(kpi.meta),
      tone: "danger",
      statusLabel: "Kaynak uyuşmazlığı",
      icon: TrendingUp,
    };
  }
  if (kpi.state === "insufficient-data") {
    return {
      outcome: "incompatible",
      actual: null,
      target: parseTargetFromMeta(kpi.meta),
      tone: "info",
      statusLabel: "Veri yetersiz",
      icon: Minus,
    };
  }
  if (kpi.state === "unavailable") {
    return {
      outcome: "incompatible",
      actual: null,
      target: parseTargetFromMeta(kpi.meta),
      tone: "info",
      statusLabel: kpi.unavailableReason ?? "Karşılaştırılamıyor",
      icon: Minus,
    };
  }
  return null;
}

function resolveKpiScopeLabel(kpi: EnergyKpiCard): string | null {
  switch (kpi.id) {
    case "total-consumption":
      return kpi.meta;
    case "peak-power":
      return "Raporlama dönemi tepe gücü";
    case "energy-intensity":
      return "Değerlendirme penceresi yoğunluğu";
    case "idle-consumption":
      return "Değerlendirme penceresi boşta tüketimi";
    case "target-variance":
      return "Onaylı hedef sapması";
    case "estimated-cost":
      return "Raporlama dönemi maliyet kanıtı";
    default:
      return null;
  }
}

function buildAccessibleSummary(
  kpi: EnergyKpiCard,
  comparison: MetricComparison | null,
  statusLabel: string,
): string | null {
  if (!comparison || comparison.actual === null) {
    if (kpi.id === "estimated-cost") {
      return `${kpi.label}: ${kpi.value}. ${statusLabel}.`;
    }
    if (kpi.id === "total-consumption") {
      return `${kpi.label}: ${kpi.value}${kpi.unit ? ` ${kpi.unit}` : ""}. ${statusLabel}.`;
    }
    return null;
  }

  const unitSuffix = kpi.unit ? ` ${kpi.unit}` : "";
  const targetText = extractTargetDisplay(kpi.meta) ?? "hedef yok";
  return `Gerçekleşen ${kpi.value}${unitSuffix}; hedef ${targetText}; ${statusLabel.toLowerCase()}.`;
}

function buildEvidenceRailGeometry(
  kpi: EnergyKpiCard,
  comparison: MetricComparison,
  statusLabel: string,
): EnergyKpiEvidenceRail {
  if (
    comparison.outcome === "no-target"
    || comparison.outcome === "incompatible"
    || comparison.actual === null
    || comparison.target === null
  ) {
    return {
      progressPercent: 0,
      markerPercent: null,
      railLabel: statusLabel,
      accessibleSummary: buildAccessibleSummary(kpi, comparison, statusLabel),
    };
  }

  const scaleMax = Math.max(comparison.actual, comparison.target, 0.01) * 1.08;
  const markerPercent = Math.min((comparison.target / scaleMax) * 100, 100);
  const progressPercent = Math.min((comparison.actual / scaleMax) * 100, 100);

  return {
    progressPercent,
    markerPercent,
    railLabel: statusLabel,
    accessibleSummary: buildAccessibleSummary(kpi, comparison, statusLabel),
  };
}

export function createSparklinePath(values: readonly number[]): string {
  if (values.length < 2) return "";

  const width = 112;
  const height = 28;
  const min = Math.min(...values);
  const max = Math.max(...values);
  const range = Math.max(max - min, 1);

  return values
    .map((value, index) => {
      const x = (index / (values.length - 1)) * width;
      const y = height - ((value - min) / range) * height;
      return `${index === 0 ? "M" : "L"} ${x.toFixed(2)} ${y.toFixed(2)}`;
    })
    .join(" ");
}

export function resolveKpiEvidenceRail(kpi: EnergyKpiCard): EnergyKpiEvidenceRail | null {
  if (kpi.id === "total-consumption") {
    return null;
  }

  if (kpi.id === "estimated-cost") {
    return {
      progressPercent: 0,
      markerPercent: null,
      railLabel: kpi.state === "available" ? "Örnek tarife" : "Tarife kanıtı yok",
      accessibleSummary: `${kpi.label}: ${kpi.value}. Örnek tarife.`,
    };
  }

  const icon = KPI_ICONS[kpi.id];
  const blocked = resolveConflictOrDataState(kpi);
  if (blocked) {
    return {
      progressPercent: 0,
      markerPercent: null,
      railLabel: blocked.statusLabel,
      accessibleSummary: buildAccessibleSummary(kpi, blocked, blocked.statusLabel),
    };
  }

  const comparison =
    kpi.id === "target-variance"
      ? resolveTargetVarianceComparison(kpi)
      : resolveLowerIsBetterComparison(kpi, icon);

  return buildEvidenceRailGeometry(kpi, comparison, comparison.statusLabel);
}

export function resolveKpiPresentation(
  kpi: EnergyKpiCard,
  trend: readonly EnergyTrendPoint[],
): EnergyKpiPresentation {
  const icon = KPI_ICONS[kpi.id];
  let tone: EnergyKpiTone = "info";
  let statusLabel = "Bilgi";
  let statusIcon = icon;
  let sparklineValues: readonly number[] | null = null;
  let scopeLabel = resolveKpiScopeLabel(kpi);
  let accessibleSummary: string | null = null;

  const blocked = resolveConflictOrDataState(kpi);
  if (blocked) {
    tone = blocked.tone;
    statusLabel = blocked.statusLabel;
    statusIcon = blocked.icon;
    accessibleSummary = buildAccessibleSummary(kpi, blocked, statusLabel);
    return {
      tone,
      icon: statusIcon,
      statusLabel,
      sparklineValues,
      displayValue: kpi.value,
      displayUnit: kpi.unit,
      scopeLabel,
      accessibleSummary,
    };
  }

  switch (kpi.id) {
    case "total-consumption":
      tone = "info";
      statusLabel = "Raporlama dönemi toplamı";
      sparklineValues = trend
        .map((point) => point.consumedKwh)
        .filter((value): value is number => value !== null && !Number.isNaN(value));
      if (sparklineValues.length < 2) sparklineValues = null;
      accessibleSummary = `${kpi.label}: ${kpi.value}${kpi.unit ? ` ${kpi.unit}` : ""}. ${statusLabel}.`;
      break;
    case "peak-power":
    case "energy-intensity":
    case "idle-consumption": {
      const comparison = resolveLowerIsBetterComparison(kpi, icon);
      tone = comparison.tone;
      statusLabel = comparison.statusLabel;
      statusIcon = comparison.icon;
      accessibleSummary = buildAccessibleSummary(kpi, comparison, statusLabel);
      break;
    }
    case "target-variance": {
      const comparison = resolveTargetVarianceComparison(kpi);
      tone = comparison.tone;
      statusLabel = comparison.statusLabel;
      statusIcon = comparison.icon;
      accessibleSummary = buildAccessibleSummary(kpi, comparison, statusLabel);
      break;
    }
    case "estimated-cost":
      tone = "info";
      statusLabel = kpi.state === "available" ? "Örnek tarife" : "Tarife kanıtı yok";
      accessibleSummary = `${kpi.label}: ${kpi.value}. ${statusLabel}.`;
      scopeLabel = "Raporlama dönemi maliyet kanıtı";
      break;
    default:
      break;
  }

  return {
    tone,
    icon: statusIcon,
    statusLabel,
    sparklineValues,
    displayValue: kpi.value,
    displayUnit: kpi.unit,
    scopeLabel,
    accessibleSummary,
  };
}

export function allocationSliceColor(tone: "machine" | "auxiliary" | "residual"): string {
  switch (tone) {
    case "machine":
      return "#2563eb";
    case "auxiliary":
      return "#0d9488";
    default:
      return "#94a3b8";
  }
}
