import { formatDate, formatDateTime } from "@/lib/presentation/format";
import type {
  ProductionPlan,
  ProductionPlanConfidence,
} from "../domain/production-plan";

const TURKISH_LOCALE = "tr-TR";

const TIME_FORMATTER = new Intl.DateTimeFormat(
  TURKISH_LOCALE,
  {
    hour: "2-digit",
    minute: "2-digit",
  },
);

const NUMBER_FORMATTER = new Intl.NumberFormat(
  TURKISH_LOCALE,
  {
    maximumFractionDigits: 2,
  },
);

const INTEGER_FORMATTER = new Intl.NumberFormat(
  TURKISH_LOCALE,
  {
    maximumFractionDigits: 0,
  },
);

const CONFIDENCE_PERCENTAGES = {
  low: 45,
  medium: 70,
  high: 92,
} satisfies Record<
  ProductionPlanConfidence,
  number
>;

function parseDate(
  value: string | undefined,
): Date | null {
  if (!value) {
    return null;
  }

  const date = new Date(value);

  return Number.isNaN(date.getTime())
    ? null
    : date;
}

export function formatProductionPlanDate(
  value: string | undefined,
): string {
  const date = parseDate(value);

  return date
    ? formatDate(date)
    : "Tarih belirlenmedi";
}

export function formatProductionPlanDateTime(
  value: string | undefined,
): string {
  const date = parseDate(value);

  return date
    ? formatDateTime(date)
    : "Zaman belirlenmedi";
}

export function formatProductionPlanTime(
  value: string | undefined,
): string {
  const date = parseDate(value);

  return date
    ? TIME_FORMATTER.format(date)
    : "Saat belirlenmedi";
}

export function formatProductionPlanQuantity(
  quantity: number,
  unit?: string,
): string {
  const formattedQuantity =
    NUMBER_FORMATTER.format(quantity);

  return unit?.trim()
    ? `${formattedQuantity} ${unit.trim()}`
    : formattedQuantity;
}

export function formatProductionPlanDuration(
  durationMinutes: number | undefined,
): string {
  if (
    durationMinutes === undefined ||
    durationMinutes < 0 ||
    !Number.isFinite(durationMinutes)
  ) {
    return "Süre belirlenmedi";
  }

  const roundedMinutes = Math.round(
    durationMinutes,
  );

  const days = Math.floor(
    roundedMinutes / 1_440,
  );

  const remainingAfterDays =
    roundedMinutes % 1_440;

  const hours = Math.floor(
    remainingAfterDays / 60,
  );

  const minutes =
    remainingAfterDays % 60;

  const parts: string[] = [];

  if (days > 0) {
    parts.push(
      `${INTEGER_FORMATTER.format(days)} gün`,
    );
  }

  if (hours > 0) {
    parts.push(
      `${INTEGER_FORMATTER.format(hours)} saat`,
    );
  }

  if (
    minutes > 0 ||
    parts.length === 0
  ) {
    parts.push(
      `${INTEGER_FORMATTER.format(minutes)} dakika`,
    );
  }

  return parts.join(" ");
}

export function formatProductionPlanConfidence(
  confidence: ProductionPlanConfidence,
): string {
  return `%${CONFIDENCE_PERCENTAGES[confidence]}`;
}

export function formatProductionPlanTimeWindow(
  timeWindow: ProductionPlan["timeWindow"],
): string {
  const startDate = parseDate(
    timeWindow.plannedStartAt,
  );

  const endDate = parseDate(
    timeWindow.plannedEndAt,
  );

  if (!startDate || !endDate) {
    return "Plan zaman aralığı belirlenmedi";
  }

  const sameDay =
    startDate.getFullYear() ===
      endDate.getFullYear() &&
    startDate.getMonth() ===
      endDate.getMonth() &&
    startDate.getDate() ===
      endDate.getDate();

  if (sameDay) {
    return [
      formatDate(startDate),
      `${TIME_FORMATTER.format(startDate)}–${TIME_FORMATTER.format(endDate)}`,
    ].join(" ");
  }

  return [
    formatDateTime(
      startDate,
    ),
    "–",
    formatDateTime(
      endDate,
    ),
  ].join(" ");
}

export function formatProductionPlanRiskCount(
  productionPlan: ProductionPlan,
): string {
  const listSummary =
    productionPlan.listSummary;

  const blockingCount =
    productionPlan.risks.length > 0
      ? productionPlan.risks.filter(
          (risk) => risk.blocking,
        ).length
      : (listSummary?.blockingRiskCount ??
        0);

  const criticalCount =
    productionPlan.risks.length > 0
      ? productionPlan.risks.filter(
          (risk) =>
            risk.severity === "critical",
        ).length
      : (listSummary?.criticalRiskCount ??
        0);

  const totalRiskCount =
    productionPlan.risks.length > 0
      ? productionPlan.risks.length
      : (listSummary?.riskCount ?? 0);

  if (blockingCount > 0) {
    return `${blockingCount} engelleyici risk`;
  }

  if (criticalCount > 0) {
    return `${criticalCount} kritik risk`;
  }

  if (totalRiskCount === 0) {
    return "Risk bulunmuyor";
  }

  return `${totalRiskCount} risk`;
}