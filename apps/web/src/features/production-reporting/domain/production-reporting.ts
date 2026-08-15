export type ProductionLogisticsRiskSummary = Readonly<{
  riskLevel: string;
  slackMinutes: number;
  blocking: boolean;
  reason?: string;
  latestProductionCompletionUtc?: string;
  available: boolean;
}>;

export type ProductionRiskSignal = Readonly<{
  code: string;
  label: string;
  severity: string;
  relatedProductionOrderId?: string;
  relatedReferenceCode?: string;
}>;

export type ProductionReportingOverview = Readonly<{
  totalOrderCount: number;
  activeOrderCount: number;
  completedOrderCount: number;
  pausedOrderCount: number;
  readyOrderCount: number;
  plannedQuantity: number;
  goodQuantity: number;
  scrapQuantity: number;
  remainingQuantity: number;
  completionRatePercent?: number;
  yieldPercent?: number;
  activeDowntimeCount: number;
  totalDowntimeMinutes: number;
  totalSetupMinutes: number;
  manualCycleCount: number;
  openExceptionCount: number;
  unresolvedExceptionCount: number;
  shiftCoverageCount: number;
  operatorCoverageCount: number;
  riskSignals: readonly ProductionRiskSignal[];
  logisticsRisk: ProductionLogisticsRiskSummary;
}>;

export type ProductionPlanActualComparison = Readonly<{
  plannedStartAtUtc: string;
  plannedEndAtUtc: string;
  actualStartAtUtc?: string;
  actualEndAtUtc?: string;
  plannedQuantity?: number;
  goodQuantity: number;
  scrapQuantity: number;
  remainingQuantity?: number;
  completionPercent?: number;
  yieldPercent?: number;
  scheduleVarianceMinutes?: number;
  scheduleOverrun: boolean;
}>;

export type ProductionOrderPerformanceRow = Readonly<{
  productionOrderId: string;
  referenceCode: string;
  title: string;
  status: string;
  productReference?: string;
  machineReference?: string;
  sourceProductionPlanId: string;
  planActual: ProductionPlanActualComparison;
  hasActiveException: boolean;
  hasActiveDowntime: boolean;
  operationalRisk?: string;
  logisticsRisk?: ProductionLogisticsRiskSummary;
}>;

export type ProductionShiftReportingRow = Readonly<{
  shiftId: string;
  shiftCode: string;
  shiftName: string;
  activeWindowStartUtc?: string;
  activeWindowEndUtc?: string;
  productionOrderCount: number;
  goodQuantity: number;
  scrapQuantity: number;
  downtimeMinutes: number;
  setupMinutes: number;
  openExceptionCount: number;
  assignedResourceCount: number;
  assignedOperatorCount: number;
  handoverState: string;
}>;

export type ProductionDowntimeReportingSummary = Readonly<{
  totalDowntimeMinutes: number;
  activeDowntimeCount: number;
  eventCount: number;
  events: readonly Readonly<{
    downtimeId: string;
    productionOrderId: string;
    orderReferenceCode: string;
    reasonCode: string;
    resourceReference?: string;
    durationMinutes?: number;
    isActive: boolean;
  }>[];
}>;

export type ProductionExceptionReportingSummary = Readonly<{
  openCount: number;
  unassignedCount: number;
  resolvedCount: number;
  items: readonly Readonly<{
    exceptionId: string;
    productionOrderId: string;
    orderReferenceCode: string;
    exceptionType: string;
    severity: string;
    status: string;
    ageMinutes: number;
  }>[];
}>;

export type ProductionScrapAnalytics = Readonly<{
  scrapQuantity: number;
  goodQuantity: number;
  totalProcessedQuantity: number;
  scrapRatePercent?: number;
}>;

export type ProductionCycleAnalytics = Readonly<{
  cycleCount: number;
  averageCycleDurationMilliseconds?: number;
  recentCycleDurationMilliseconds?: number;
}>;
