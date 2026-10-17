export type PlanningLogisticsEvaluationView = Readonly<{
  productionPlanId: string;
  exportCaseNumber?: string;
  shipmentReference?: string;
  pickupDeadlineUtc?: string;
  departureDeadlineUtc?: string;
  latestProductionCompletionUtc: string;
  plannedProductionCompletionUtc: string;
  slackMinutes: number;
  riskLevel: string;
  blocking: boolean;
  blockingReason?: string;
  exportHandoffReady?: boolean;
  exportHandoffBlocked?: boolean;
  exportReadinessStatus?: string;
  evaluatedAtUtc: string;
  sourceVersion: number;
  linkageStatus: string;
}>;

export function deriveExportReadiness(
  evaluation: Pick<
    PlanningLogisticsEvaluationView,
    "linkageStatus" | "blockingReason"
  >,
): Pick<
  PlanningLogisticsEvaluationView,
  "exportHandoffReady" | "exportHandoffBlocked"
> {
  const normalizedReason = evaluation.blockingReason?.toLowerCase() ?? "";
  const exportHandoffBlocked =
    normalizedReason.includes("export-readiness-blocked")
    || normalizedReason.includes("packing-not-ready")
    || normalizedReason.includes("shipment-not-ready");

  const exportHandoffReady =
    evaluation.linkageStatus === "linked" && !exportHandoffBlocked;

  return {
    exportHandoffReady,
    exportHandoffBlocked,
  };
}

export function formatExportReadinessLabel(
  exportHandoffReady?: boolean,
  exportHandoffBlocked?: boolean,
): string {
  if (exportHandoffBlocked) {
    return "İhracat hazırlığı engelli";
  }

  if (exportHandoffReady) {
    return "İhracat hazırlığı tamam";
  }

  return "İhracat hazırlığı dikkat gerektiriyor";
}

export type PlanningLogisticsCardSummary = Readonly<{
  planId: string;
  riskLevel: string;
  slackMinutes: number;
  blocking: boolean;
  indicatorLabel: string;
}>;

export function createPlanningLogisticsCardSummary(
  evaluation: PlanningLogisticsEvaluationView,
): PlanningLogisticsCardSummary {
  return {
    planId: evaluation.productionPlanId,
    riskLevel: evaluation.riskLevel,
    slackMinutes: evaluation.slackMinutes,
    blocking: evaluation.blocking,
    indicatorLabel: formatPlanningLogisticsIndicatorLabel(evaluation),
  };
}

export function formatPlanningLogisticsIndicatorLabel(
  evaluation: PlanningLogisticsEvaluationView,
): string {
  if (evaluation.blocking && evaluation.slackMinutes < 0) {
    return "Sevkiyata yetişmiyor";
  }

  if (evaluation.riskLevel === "critical" || evaluation.riskLevel === "high") {
    return `Lojistik riski: ${formatPlanningLogisticsRiskLabel(evaluation.riskLevel)}`;
  }

  if (
    evaluation.pickupDeadlineUtc &&
    evaluation.slackMinutes >= 0 &&
    evaluation.slackMinutes <= 360
  ) {
    const pickupHours = Math.max(
      1,
      Math.round(
        (new Date(evaluation.pickupDeadlineUtc).getTime() - Date.now()) /
          (60 * 60 * 1000),
      ),
    );
    return `Çıkışa ${pickupHours} saat`;
  }

  if (evaluation.riskLevel === "medium") {
    return "Termin riski";
  }

  return "Lojistik uyumlu";
}

export function formatPlanningLogisticsRiskLabel(
  riskLevel: string,
): string {
  switch (riskLevel) {
    case "critical":
      return "Kritik";
    case "high":
      return "Yüksek";
    case "medium":
      return "Orta";
    case "low":
      return "Düşük";
    default:
      return "Yok";
  }
}

export function formatPlanningLogisticsSlackLabel(
  slackMinutes: number,
): string {
  if (slackMinutes < 0) {
    const hours = Math.abs(Math.round(slackMinutes / 60));
    return `${hours} saat aşım`;
  }

  if (slackMinutes === 0) {
    return "Tampon yok";
  }

  const hours = Math.round(slackMinutes / 60);
  return `${hours} saat tampon`;
}
