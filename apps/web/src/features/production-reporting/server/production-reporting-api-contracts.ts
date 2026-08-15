function readNumber(value: unknown, field: string): number {
  if (typeof value !== "number" || Number.isNaN(value)) {
    throw new Error(`Invalid number for ${field}.`);
  }
  return value;
}

function readOptionalNumber(value: unknown): number | undefined {
  return typeof value === "number" && !Number.isNaN(value) ? value : undefined;
}

function readString(value: unknown, field: string): string {
  if (typeof value !== "string") {
    throw new Error(`Invalid string for ${field}.`);
  }
  return value;
}

function readOptionalString(value: unknown): string | undefined {
  return typeof value === "string" ? value : undefined;
}

function readBoolean(value: unknown, field: string): boolean {
  if (typeof value !== "boolean") {
    throw new Error(`Invalid boolean for ${field}.`);
  }
  return value;
}

function parseLogistics(value: unknown) {
  if (typeof value !== "object" || value === null) {
    throw new Error("Invalid logisticsRisk payload.");
  }
  const record = value as Record<string, unknown>;
  return {
    riskLevel: readString(record.riskLevel, "logisticsRisk.riskLevel"),
    slackMinutes: readNumber(record.slackMinutes, "logisticsRisk.slackMinutes"),
    blocking: readBoolean(record.blocking, "logisticsRisk.blocking"),
    reason: readOptionalString(record.reason),
    latestProductionCompletionUtc: readOptionalString(record.latestProductionCompletionUtc),
    available: readBoolean(record.available, "logisticsRisk.available"),
  };
}

function parsePlanActual(value: unknown) {
  if (typeof value !== "object" || value === null) {
    throw new Error("Invalid planActual payload.");
  }
  const record = value as Record<string, unknown>;
  return {
    plannedStartAtUtc: readString(record.plannedStartAtUtc, "planActual.plannedStartAtUtc"),
    plannedEndAtUtc: readString(record.plannedEndAtUtc, "planActual.plannedEndAtUtc"),
    actualStartAtUtc: readOptionalString(record.actualStartAtUtc),
    actualEndAtUtc: readOptionalString(record.actualEndAtUtc),
    plannedQuantity: readOptionalNumber(record.plannedQuantity),
    goodQuantity: readNumber(record.goodQuantity, "planActual.goodQuantity"),
    scrapQuantity: readNumber(record.scrapQuantity, "planActual.scrapQuantity"),
    remainingQuantity: readOptionalNumber(record.remainingQuantity),
    completionPercent: readOptionalNumber(record.completionPercent),
    yieldPercent: readOptionalNumber(record.yieldPercent),
    scheduleVarianceMinutes: readOptionalNumber(record.scheduleVarianceMinutes),
    scheduleOverrun: readBoolean(record.scheduleOverrun, "planActual.scheduleOverrun"),
  };
}

export function parseProductionReportingOverviewApiResponse(payload: unknown) {
  if (typeof payload !== "object" || payload === null) {
    throw new Error("Invalid reporting overview payload.");
  }
  const record = payload as Record<string, unknown>;
  const riskSignals = Array.isArray(record.riskSignals)
    ? record.riskSignals.map((item) => {
        if (typeof item !== "object" || item === null) {
          throw new Error("Invalid risk signal.");
        }
        const signal = item as Record<string, unknown>;
        return {
          code: readString(signal.code, "riskSignals.code"),
          label: readString(signal.label, "riskSignals.label"),
          severity: readString(signal.severity, "riskSignals.severity"),
          relatedProductionOrderId: readOptionalString(signal.relatedProductionOrderId),
          relatedReferenceCode: readOptionalString(signal.relatedReferenceCode),
        };
      })
    : [];

  return {
    totalOrderCount: readNumber(record.totalOrderCount, "totalOrderCount"),
    activeOrderCount: readNumber(record.activeOrderCount, "activeOrderCount"),
    completedOrderCount: readNumber(record.completedOrderCount, "completedOrderCount"),
    pausedOrderCount: readNumber(record.pausedOrderCount, "pausedOrderCount"),
    readyOrderCount: readNumber(record.readyOrderCount, "readyOrderCount"),
    plannedQuantity: readNumber(record.plannedQuantity, "plannedQuantity"),
    goodQuantity: readNumber(record.goodQuantity, "goodQuantity"),
    scrapQuantity: readNumber(record.scrapQuantity, "scrapQuantity"),
    remainingQuantity: readNumber(record.remainingQuantity, "remainingQuantity"),
    completionRatePercent: readOptionalNumber(record.completionRatePercent),
    yieldPercent: readOptionalNumber(record.yieldPercent),
    activeDowntimeCount: readNumber(record.activeDowntimeCount, "activeDowntimeCount"),
    totalDowntimeMinutes: readNumber(record.totalDowntimeMinutes, "totalDowntimeMinutes"),
    totalSetupMinutes: readNumber(record.totalSetupMinutes, "totalSetupMinutes"),
    manualCycleCount: readNumber(record.manualCycleCount, "manualCycleCount"),
    openExceptionCount: readNumber(record.openExceptionCount, "openExceptionCount"),
    unresolvedExceptionCount: readNumber(record.unresolvedExceptionCount, "unresolvedExceptionCount"),
    shiftCoverageCount: readNumber(record.shiftCoverageCount, "shiftCoverageCount"),
    operatorCoverageCount: readNumber(record.operatorCoverageCount, "operatorCoverageCount"),
    riskSignals,
    logisticsRisk: parseLogistics(record.logisticsRisk),
  };
}

export function parseProductionOrderPerformanceListApiResponse(payload: unknown) {
  if (!Array.isArray(payload)) {
    throw new Error("Invalid order performance list payload.");
  }

  return payload.map((item) => {
    if (typeof item !== "object" || item === null) {
      throw new Error("Invalid order performance row.");
    }
    const record = item as Record<string, unknown>;
    return {
      productionOrderId: readString(record.productionOrderId, "productionOrderId"),
      referenceCode: readString(record.referenceCode, "referenceCode"),
      title: readString(record.title, "title"),
      status: readString(record.status, "status"),
      productReference: readOptionalString(record.productReference),
      machineReference: readOptionalString(record.machineReference),
      sourceProductionPlanId: readString(record.sourceProductionPlanId, "sourceProductionPlanId"),
      planActual: parsePlanActual(record.planActual),
      hasActiveException: readBoolean(record.hasActiveException, "hasActiveException"),
      hasActiveDowntime: readBoolean(record.hasActiveDowntime, "hasActiveDowntime"),
      operationalRisk: readOptionalString(record.operationalRisk),
      logisticsRisk:
        record.logisticsRisk === undefined || record.logisticsRisk === null
          ? undefined
          : parseLogistics(record.logisticsRisk),
    };
  });
}

export function parseProductionShiftReportingListApiResponse(payload: unknown) {
  if (!Array.isArray(payload)) {
    throw new Error("Invalid shift reporting list payload.");
  }

  return payload.map((item) => {
    if (typeof item !== "object" || item === null) {
      throw new Error("Invalid shift reporting row.");
    }
    const record = item as Record<string, unknown>;
    return {
      shiftId: readString(record.shiftId, "shiftId"),
      shiftCode: readString(record.shiftCode, "shiftCode"),
      shiftName: readString(record.shiftName, "shiftName"),
      activeWindowStartUtc: readOptionalString(record.activeWindowStartUtc),
      activeWindowEndUtc: readOptionalString(record.activeWindowEndUtc),
      productionOrderCount: readNumber(record.productionOrderCount, "productionOrderCount"),
      goodQuantity: readNumber(record.goodQuantity, "goodQuantity"),
      scrapQuantity: readNumber(record.scrapQuantity, "scrapQuantity"),
      downtimeMinutes: readNumber(record.downtimeMinutes, "downtimeMinutes"),
      setupMinutes: readNumber(record.setupMinutes, "setupMinutes"),
      openExceptionCount: readNumber(record.openExceptionCount, "openExceptionCount"),
      assignedResourceCount: readNumber(record.assignedResourceCount, "assignedResourceCount"),
      assignedOperatorCount: readNumber(record.assignedOperatorCount, "assignedOperatorCount"),
      handoverState: readString(record.handoverState, "handoverState"),
    };
  });
}

export function parseProductionDowntimeReportingApiResponse(payload: unknown) {
  if (typeof payload !== "object" || payload === null) {
    throw new Error("Invalid downtime reporting payload.");
  }
  const record = payload as Record<string, unknown>;
  const events = Array.isArray(record.events)
    ? record.events.map((item) => {
        if (typeof item !== "object" || item === null) {
          throw new Error("Invalid downtime event.");
        }
        const event = item as Record<string, unknown>;
        return {
          downtimeId: readString(event.downtimeId, "downtimeId"),
          productionOrderId: readString(event.productionOrderId, "productionOrderId"),
          orderReferenceCode: readString(event.orderReferenceCode, "orderReferenceCode"),
          reasonCode: readString(event.reasonCode, "reasonCode"),
          resourceReference: readOptionalString(event.resourceReference),
          durationMinutes: readOptionalNumber(event.durationMinutes),
          isActive: readBoolean(event.isActive, "isActive"),
        };
      })
    : [];

  return {
    totalDowntimeMinutes: readNumber(record.totalDowntimeMinutes, "totalDowntimeMinutes"),
    activeDowntimeCount: readNumber(record.activeDowntimeCount, "activeDowntimeCount"),
    eventCount: readNumber(record.eventCount, "eventCount"),
    events,
  };
}

export function parseProductionExceptionReportingApiResponse(payload: unknown) {
  if (typeof payload !== "object" || payload === null) {
    throw new Error("Invalid exception reporting payload.");
  }
  const record = payload as Record<string, unknown>;
  const items = Array.isArray(record.items)
    ? record.items.map((item) => {
        if (typeof item !== "object" || item === null) {
          throw new Error("Invalid exception item.");
        }
        const row = item as Record<string, unknown>;
        return {
          exceptionId: readString(row.exceptionId, "exceptionId"),
          productionOrderId: readString(row.productionOrderId, "productionOrderId"),
          orderReferenceCode: readString(row.orderReferenceCode, "orderReferenceCode"),
          exceptionType: readString(row.exceptionType, "exceptionType"),
          severity: readString(row.severity, "severity"),
          status: readString(row.status, "status"),
          ageMinutes: readNumber(row.ageMinutes, "ageMinutes"),
        };
      })
    : [];

  return {
    openCount: readNumber(record.openCount, "openCount"),
    unassignedCount: readNumber(record.unassignedCount, "unassignedCount"),
    resolvedCount: readNumber(record.resolvedCount, "resolvedCount"),
    items,
  };
}

export function parseProductionScrapAnalyticsApiResponse(payload: unknown) {
  if (typeof payload !== "object" || payload === null) {
    throw new Error("Invalid scrap analytics payload.");
  }
  const record = payload as Record<string, unknown>;
  return {
    scrapQuantity: readNumber(record.scrapQuantity, "scrapQuantity"),
    goodQuantity: readNumber(record.goodQuantity, "goodQuantity"),
    totalProcessedQuantity: readNumber(record.totalProcessedQuantity, "totalProcessedQuantity"),
    scrapRatePercent: readOptionalNumber(record.scrapRatePercent),
  };
}

export function parseProductionCycleAnalyticsApiResponse(payload: unknown) {
  if (typeof payload !== "object" || payload === null) {
    throw new Error("Invalid cycle analytics payload.");
  }
  const record = payload as Record<string, unknown>;
  return {
    cycleCount: readNumber(record.cycleCount, "cycleCount"),
    averageCycleDurationMilliseconds: readOptionalNumber(record.averageCycleDurationMilliseconds),
    recentCycleDurationMilliseconds: readOptionalNumber(record.recentCycleDurationMilliseconds),
  };
}
