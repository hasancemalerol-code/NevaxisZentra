import type { ProductionPlan } from "../domain/production-plan";

export type ProductionPlanTimelineViewModel =
  Readonly<{
    plannedStartAt: string | null;
    plannedEndAt: string | null;
    estimatedDurationMinutes: number | null;
    shiftId: string | null;
    shiftName: string | null;
    hasStartTime: boolean;
    hasEndTime: boolean;
    hasCompleteTimeWindow: boolean;
    hasValidTimeWindow: boolean;
    durationLabel: string;
  }>;

function normalizeOptionalText(
  value: string | null | undefined,
): string | null {
  const normalizedValue = value?.trim();

  return normalizedValue || null;
}

function normalizeDuration(
  value: number | null | undefined,
): number | null {
  if (
    typeof value !== "number" ||
    !Number.isFinite(value) ||
    value <= 0
  ) {
    return null;
  }

  return Math.round(value);
}

function calculateDurationFromDates(
  plannedStartAt: string | null,
  plannedEndAt: string | null,
): number | null {
  if (!plannedStartAt || !plannedEndAt) {
    return null;
  }

  const startTime = new Date(
    plannedStartAt,
  ).getTime();

  const endTime = new Date(
    plannedEndAt,
  ).getTime();

  if (
    Number.isNaN(startTime) ||
    Number.isNaN(endTime) ||
    endTime <= startTime
  ) {
    return null;
  }

  return Math.round(
    (endTime - startTime) / 60_000,
  );
}

function formatDurationLabel(
  durationMinutes: number | null,
): string {
  if (!durationMinutes) {
    return "Süre hesaplanamadı";
  }

  const days = Math.floor(
    durationMinutes / 1_440,
  );

  const remainingAfterDays =
    durationMinutes % 1_440;

  const hours = Math.floor(
    remainingAfterDays / 60,
  );

  const minutes =
    remainingAfterDays % 60;

  const parts: string[] = [];

  if (days > 0) {
    parts.push(`${days} gün`);
  }

  if (hours > 0) {
    parts.push(`${hours} saat`);
  }

  if (minutes > 0 || parts.length === 0) {
    parts.push(`${minutes} dakika`);
  }

  return parts.join(" ");
}

export function createProductionPlanTimelineViewModel(
  productionPlan: ProductionPlan,
): ProductionPlanTimelineViewModel {
  const plannedStartAt =
    normalizeOptionalText(
      productionPlan.timeWindow.plannedStartAt,
    );

  const plannedEndAt =
    normalizeOptionalText(
      productionPlan.timeWindow.plannedEndAt,
    );

  const explicitDuration =
    normalizeDuration(
      productionPlan.timeWindow
        .estimatedDurationMinutes,
    );

  const calculatedDuration =
    calculateDurationFromDates(
      plannedStartAt,
      plannedEndAt,
    );

  const estimatedDurationMinutes =
    explicitDuration ??
    calculatedDuration;

  const hasStartTime =
    Boolean(plannedStartAt);

  const hasEndTime =
    Boolean(plannedEndAt);

  const hasCompleteTimeWindow =
    hasStartTime && hasEndTime;

  const hasValidTimeWindow =
    hasCompleteTimeWindow &&
    calculatedDuration !== null;

  return {
    plannedStartAt,
    plannedEndAt,
    estimatedDurationMinutes,
    shiftId: normalizeOptionalText(
      productionPlan.timeWindow.shiftId,
    ),
    shiftName: normalizeOptionalText(
      productionPlan.timeWindow.shiftName,
    ),
    hasStartTime,
    hasEndTime,
    hasCompleteTimeWindow,
    hasValidTimeWindow,
    durationLabel: formatDurationLabel(
      estimatedDurationMinutes,
    ),
  };
}