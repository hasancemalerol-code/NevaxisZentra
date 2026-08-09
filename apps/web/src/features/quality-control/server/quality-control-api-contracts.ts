function readString(value: unknown, field: string): string {
  if (typeof value !== "string") {
    throw new Error(`Invalid string for ${field}.`);
  }
  return value;
}

function readOptionalString(value: unknown): string | undefined {
  return typeof value === "string" ? value : undefined;
}

function readNumber(value: unknown, field: string): number {
  if (typeof value !== "number" || Number.isNaN(value)) {
    throw new Error(`Invalid number for ${field}.`);
  }
  return value;
}

function readOptionalNumber(value: unknown): number | undefined {
  return typeof value === "number" && !Number.isNaN(value) ? value : undefined;
}

function readBoolean(value: unknown, field: string): boolean {
  if (typeof value !== "boolean") {
    throw new Error(`Invalid boolean for ${field}.`);
  }
  return value;
}

function parseDefinition(value: unknown) {
  if (typeof value !== "object" || value === null) {
    throw new Error("Invalid inspection definition.");
  }
  const record = value as Record<string, unknown>;
  return {
    id: readString(record.id, "definition.id"),
    code: readString(record.code, "definition.code"),
    name: readString(record.name, "definition.name"),
    description: readOptionalString(record.description),
    inspectionType: readString(record.inspectionType, "definition.inspectionType"),
    sequence: readNumber(record.sequence, "definition.sequence"),
    unit: readOptionalString(record.unit),
    nominalValue: readOptionalNumber(record.nominalValue),
    lowerLimit: readOptionalNumber(record.lowerLimit),
    upperLimit: readOptionalNumber(record.upperLimit),
    required: readBoolean(record.required, "definition.required"),
    critical: readBoolean(record.critical, "definition.critical"),
    instruction: readOptionalString(record.instruction),
    version: readNumber(record.version, "definition.version"),
  };
}

export function parseQualityWorkspaceSummaryApiResponse(payload: unknown) {
  if (typeof payload !== "object" || payload === null) {
    throw new Error("Invalid workspace summary payload.");
  }
  const record = payload as Record<string, unknown>;
  return {
    openInspectionCount: readNumber(record.openInspectionCount, "openInspectionCount"),
    pendingReviewCount: readNumber(record.pendingReviewCount, "pendingReviewCount"),
    failedInspectionCount: readNumber(record.failedInspectionCount, "failedInspectionCount"),
    releasedInspectionCount: readNumber(record.releasedInspectionCount, "releasedInspectionCount"),
    openNcrCount: readNumber(record.openNcrCount, "openNcrCount"),
    activeHoldCount: readNumber(record.activeHoldCount, "activeHoldCount"),
    actionPendingCount: readNumber(record.actionPendingCount, "actionPendingCount"),
  };
}

export function parseQualityControlPlanListApiResponse(payload: unknown) {
  if (!Array.isArray(payload)) {
    throw new Error("Invalid control plan list payload.");
  }
  return payload.map((item) => {
    if (typeof item !== "object" || item === null) {
      throw new Error("Invalid control plan summary.");
    }
    const record = item as Record<string, unknown>;
    return {
      id: readString(record.id, "plan.id"),
      code: readString(record.code, "plan.code"),
      name: readString(record.name, "plan.name"),
      status: readString(record.status, "plan.status"),
      revision: readString(record.revision, "plan.revision"),
      productReference: readOptionalString(record.productReference),
      operationReference: readOptionalString(record.operationReference),
      definitionCount: readNumber(record.definitionCount, "plan.definitionCount"),
      version: readNumber(record.version, "plan.version"),
      updatedAtUtc: readString(record.updatedAtUtc, "plan.updatedAtUtc"),
    };
  });
}

export function parseQualityControlPlanDetailApiResponse(payload: unknown) {
  if (typeof payload !== "object" || payload === null) {
    throw new Error("Invalid control plan detail payload.");
  }
  const record = payload as Record<string, unknown>;
  const definitions = Array.isArray(record.inspectionDefinitions)
    ? record.inspectionDefinitions.map(parseDefinition)
    : [];
  return {
    id: readString(record.id, "plan.id"),
    code: readString(record.code, "plan.code"),
    name: readString(record.name, "plan.name"),
    description: readOptionalString(record.description),
    status: readString(record.status, "plan.status"),
    revision: readString(record.revision, "plan.revision"),
    productReference: readOptionalString(record.productReference),
    productGroup: readOptionalString(record.productGroup),
    operationReference: readOptionalString(record.operationReference),
    version: readNumber(record.version, "plan.version"),
    inspectionDefinitions: definitions,
  };
}

export function parseQualityInspectionListApiResponse(payload: unknown) {
  if (!Array.isArray(payload)) {
    throw new Error("Invalid inspection list payload.");
  }
  return payload.map((item) => {
    if (typeof item !== "object" || item === null) {
      throw new Error("Invalid inspection summary.");
    }
    const record = item as Record<string, unknown>;
    return {
      id: readString(record.id, "inspection.id"),
      inspectionNumber: readString(record.inspectionNumber, "inspection.inspectionNumber"),
      status: readString(record.status, "inspection.status"),
      overallResult: readString(record.overallResult, "inspection.overallResult"),
      releaseStatus: readString(record.releaseStatus, "inspection.releaseStatus"),
      productionOrderId: readString(record.productionOrderId, "inspection.productionOrderId"),
      productionOrderReference: readOptionalString(record.productionOrderReference),
      productReference: readOptionalString(record.productReference),
      qualityControlPlanId: readString(record.qualityControlPlanId, "inspection.qualityControlPlanId"),
      planCode: readOptionalString(record.planCode),
      version: readNumber(record.version, "inspection.version"),
      updatedAtUtc: readString(record.updatedAtUtc, "inspection.updatedAtUtc"),
    };
  });
}

export function parseQualityInspectionDetailApiResponse(payload: unknown) {
  if (typeof payload !== "object" || payload === null) {
    throw new Error("Invalid inspection detail payload.");
  }
  const record = payload as Record<string, unknown>;
  const productionRaw = record.productionContext;
  if (typeof productionRaw !== "object" || productionRaw === null) {
    throw new Error("Invalid production context.");
  }
  const production = productionRaw as Record<string, unknown>;
  const results = Array.isArray(record.results)
    ? record.results.map((item) => {
        if (typeof item !== "object" || item === null) {
          throw new Error("Invalid inspection result.");
        }
        const row = item as Record<string, unknown>;
        return {
          id: readString(row.id, "result.id"),
          inspectionDefinitionId: readString(row.inspectionDefinitionId, "result.inspectionDefinitionId"),
          definitionCode: readString(row.definitionCode, "result.definitionCode"),
          definitionName: readString(row.definitionName, "result.definitionName"),
          inspectionType: readString(row.inspectionType, "result.inspectionType"),
          unit: readOptionalString(row.unit),
          nominalValue: readOptionalNumber(row.nominalValue),
          lowerLimit: readOptionalNumber(row.lowerLimit),
          upperLimit: readOptionalNumber(row.upperLimit),
          required: readBoolean(row.required, "result.required"),
          critical: readBoolean(row.critical, "result.critical"),
          measuredNumericValue: readOptionalNumber(row.measuredNumericValue),
          measuredTextValue: readOptionalString(row.measuredTextValue),
          booleanResult: typeof row.booleanResult === "boolean" ? row.booleanResult : undefined,
          result: readString(row.result, "result.result"),
          comment: readOptionalString(row.comment),
        };
      })
    : [];

  const linkedNonconformances = Array.isArray(record.linkedNonconformances)
    ? record.linkedNonconformances.map((item) => {
        if (typeof item !== "object" || item === null) {
          throw new Error("Invalid linked nonconformance.");
        }
        const row = item as Record<string, unknown>;
        return {
          id: readString(row.id, "linkedNcr.id"),
          ncrNumber: readString(row.ncrNumber, "linkedNcr.ncrNumber"),
          status: readString(row.status, "linkedNcr.status"),
        };
      })
    : [];

  return {
    id: readString(record.id, "inspection.id"),
    inspectionNumber: readString(record.inspectionNumber, "inspection.inspectionNumber"),
    status: readString(record.status, "inspection.status"),
    overallResult: readString(record.overallResult, "inspection.overallResult"),
    releaseStatus: readString(record.releaseStatus, "inspection.releaseStatus"),
    productionOrderId: readString(record.productionOrderId, "inspection.productionOrderId"),
    productionContext: {
      productionOrderId: readString(production.productionOrderId, "production.productionOrderId"),
      referenceCode: readString(production.referenceCode, "production.referenceCode"),
      productReference: readOptionalString(production.productReference),
      status: readString(production.status, "production.status"),
      plannedQuantity: readOptionalNumber(production.plannedQuantity),
      goodQuantity: readNumber(production.goodQuantity, "production.goodQuantity"),
      scrapQuantity: readNumber(production.scrapQuantity, "production.scrapQuantity"),
      available: readBoolean(production.available, "production.available"),
    },
    qualityControlPlanId: readString(record.qualityControlPlanId, "inspection.qualityControlPlanId"),
    planCode: readString(record.planCode, "inspection.planCode"),
    planName: readString(record.planName, "inspection.planName"),
    inspectorSubjectId: readOptionalString(record.inspectorSubjectId),
    notes: readOptionalString(record.notes),
    releaseNote: readOptionalString(record.releaseNote),
    version: readNumber(record.version, "inspection.version"),
    results,
    linkedNonconformances,
  };
}

function parseProductionContext(value: unknown) {
  if (typeof value !== "object" || value === null) {
    throw new Error("Invalid production context.");
  }
  const production = value as Record<string, unknown>;
  return {
    productionOrderId: readString(production.productionOrderId, "production.productionOrderId"),
    referenceCode: readString(production.referenceCode, "production.referenceCode"),
    productReference: readOptionalString(production.productReference),
    status: readString(production.status, "production.status"),
    plannedQuantity: readOptionalNumber(production.plannedQuantity),
    goodQuantity: readNumber(production.goodQuantity, "production.goodQuantity"),
    scrapQuantity: readNumber(production.scrapQuantity, "production.scrapQuantity"),
    available: readBoolean(production.available, "production.available"),
  };
}

function parseHold(value: unknown) {
  if (typeof value !== "object" || value === null) {
    throw new Error("Invalid hold payload.");
  }
  const record = value as Record<string, unknown>;
  return {
    id: readString(record.id, "hold.id"),
    scope: readString(record.scope, "hold.scope"),
    status: readString(record.status, "hold.status"),
    reason: readString(record.reason, "hold.reason"),
    placedAtUtc: readString(record.placedAtUtc, "hold.placedAtUtc"),
    placedBySubjectId: readString(record.placedBySubjectId, "hold.placedBySubjectId"),
    releasedAtUtc: readOptionalString(record.releasedAtUtc),
    releasedBySubjectId: readOptionalString(record.releasedBySubjectId),
    releaseReason: readOptionalString(record.releaseReason),
    version: readNumber(record.version, "hold.version"),
  };
}

function parseCorrectiveAction(value: unknown) {
  if (typeof value !== "object" || value === null) {
    throw new Error("Invalid corrective action payload.");
  }
  const record = value as Record<string, unknown>;
  return {
    id: readString(record.id, "action.id"),
    title: readString(record.title, "action.title"),
    description: readString(record.description, "action.description"),
    status: readString(record.status, "action.status"),
    ownerSubjectId: readOptionalString(record.ownerSubjectId),
    dueAtUtc: readOptionalString(record.dueAtUtc),
    completedAtUtc: readOptionalString(record.completedAtUtc),
    completionNote: readOptionalString(record.completionNote),
    version: readNumber(record.version, "action.version"),
    updatedAtUtc: readString(record.updatedAtUtc, "action.updatedAtUtc"),
  };
}

export function parseQualityNcrListApiResponse(payload: unknown) {
  if (!Array.isArray(payload)) {
    throw new Error("Invalid NCR list payload.");
  }
  return payload.map((item) => {
    if (typeof item !== "object" || item === null) {
      throw new Error("Invalid NCR summary.");
    }
    const record = item as Record<string, unknown>;
    return {
      id: readString(record.id, "ncr.id"),
      ncrNumber: readString(record.ncrNumber, "ncr.ncrNumber"),
      title: readString(record.title, "ncr.title"),
      severity: readString(record.severity, "ncr.severity"),
      status: readString(record.status, "ncr.status"),
      holdStatus: readString(record.holdStatus, "ncr.holdStatus"),
      dispositionStatus: readString(record.dispositionStatus, "ncr.dispositionStatus"),
      dispositionType: readOptionalString(record.dispositionType),
      productionOrderId: readOptionalString(record.productionOrderId),
      productionOrderReference: readOptionalString(record.productionOrderReference),
      productReference: readOptionalString(record.productReference),
      ownerSubjectId: readOptionalString(record.ownerSubjectId),
      detectedAtUtc: readString(record.detectedAtUtc, "ncr.detectedAtUtc"),
      createdAtUtc: readString(record.createdAtUtc, "ncr.createdAtUtc"),
      version: readNumber(record.version, "ncr.version"),
      updatedAtUtc: readString(record.updatedAtUtc, "ncr.updatedAtUtc"),
    };
  });
}

export function parseQualityNcrDetailApiResponse(payload: unknown) {
  if (typeof payload !== "object" || payload === null) {
    throw new Error("Invalid NCR detail payload.");
  }
  const record = payload as Record<string, unknown>;
  const productionContext =
    record.productionContext === null || record.productionContext === undefined
      ? undefined
      : parseProductionContext(record.productionContext);
  const activeHold =
    record.activeHold === null || record.activeHold === undefined
      ? undefined
      : parseHold(record.activeHold);
  const correctiveActions = Array.isArray(record.correctiveActions)
    ? record.correctiveActions.map(parseCorrectiveAction)
    : [];

  return {
    id: readString(record.id, "ncr.id"),
    ncrNumber: readString(record.ncrNumber, "ncr.ncrNumber"),
    title: readString(record.title, "ncr.title"),
    description: readString(record.description, "ncr.description"),
    category: readOptionalString(record.category),
    severity: readString(record.severity, "ncr.severity"),
    status: readString(record.status, "ncr.status"),
    holdStatus: readString(record.holdStatus, "ncr.holdStatus"),
    dispositionStatus: readString(record.dispositionStatus, "ncr.dispositionStatus"),
    dispositionType: readOptionalString(record.dispositionType),
    dispositionNote: readOptionalString(record.dispositionNote),
    dispositionDecidedBySubjectId: readOptionalString(record.dispositionDecidedBySubjectId),
    dispositionDecidedAtUtc: readOptionalString(record.dispositionDecidedAtUtc),
    productionOrderId: readOptionalString(record.productionOrderId),
    productionContext,
    qualityInspectionId: readOptionalString(record.qualityInspectionId),
    qualityInspectionNumber: readOptionalString(record.qualityInspectionNumber),
    qualityInspectionResultId: readOptionalString(record.qualityInspectionResultId),
    productReference: readOptionalString(record.productReference),
    operationReference: readOptionalString(record.operationReference),
    detectedBySubjectId: readString(record.detectedBySubjectId, "ncr.detectedBySubjectId"),
    ownerSubjectId: readOptionalString(record.ownerSubjectId),
    detectedAtUtc: readString(record.detectedAtUtc, "ncr.detectedAtUtc"),
    closedAtUtc: readOptionalString(record.closedAtUtc),
    closedBySubjectId: readOptionalString(record.closedBySubjectId),
    version: readNumber(record.version, "ncr.version"),
    createdAtUtc: readString(record.createdAtUtc, "ncr.createdAtUtc"),
    updatedAtUtc: readString(record.updatedAtUtc, "ncr.updatedAtUtc"),
    activeHold,
    correctiveActions,
  };
}

export function parseProductionQualitySignalApiResponse(payload: unknown) {
  if (typeof payload !== "object" || payload === null) {
    throw new Error("Invalid production quality signal payload.");
  }
  const record = payload as Record<string, unknown>;
  return {
    available: readBoolean(record.available, "signal.available"),
    qualityHoldActive: readBoolean(record.qualityHoldActive, "signal.qualityHoldActive"),
    blocking: readBoolean(record.blocking, "signal.blocking"),
    openNcrCount: readNumber(record.openNcrCount, "signal.openNcrCount"),
    criticalNcrCount: readNumber(record.criticalNcrCount, "signal.criticalNcrCount"),
    releaseStatus: readOptionalString(record.releaseStatus),
    reason: readOptionalString(record.reason),
  };
}

export function parseLinkedNonconformanceApiResponse(payload: unknown) {
  if (typeof payload !== "object" || payload === null) {
    throw new Error("Invalid linked nonconformance payload.");
  }
  const record = payload as Record<string, unknown>;
  return {
    id: readString(record.id, "linkedNcr.id"),
    ncrNumber: readString(record.ncrNumber, "linkedNcr.ncrNumber"),
    status: readString(record.status, "linkedNcr.status"),
  };
}

export function parseQualityWriteApiResponse(payload: unknown) {
  if (typeof payload !== "object" || payload === null) {
    throw new Error("Invalid write response.");
  }
  const record = payload as Record<string, unknown>;
  return {
    id: readString(record.id, "write.id"),
    version: readNumber(record.version, "write.version"),
  };
}

function parseInspectionReportMeasurement(value: unknown) {
  if (typeof value !== "object" || value === null) {
    throw new Error("Invalid inspection measurement.");
  }
  const record = value as Record<string, unknown>;
  return {
    definitionCode: readString(record.definitionCode, "measurement.definitionCode"),
    definitionName: readString(record.definitionName, "measurement.definitionName"),
    unit: readOptionalString(record.unit),
    nominalValue: readOptionalNumber(record.nominalValue),
    lowerLimit: readOptionalNumber(record.lowerLimit),
    upperLimit: readOptionalNumber(record.upperLimit),
    required: readBoolean(record.required, "measurement.required"),
    critical: readBoolean(record.critical, "measurement.critical"),
    measuredNumericValue: readOptionalNumber(record.measuredNumericValue),
    measuredTextValue: readOptionalString(record.measuredTextValue),
    booleanResult: typeof record.booleanResult === "boolean" ? record.booleanResult : undefined,
    result: readString(record.result, "measurement.result"),
  };
}

export function parseQualityReportingOverviewApiResponse(payload: unknown) {
  if (typeof payload !== "object" || payload === null) {
    throw new Error("Invalid reporting overview payload.");
  }
  const record = payload as Record<string, unknown>;
  return {
    totalInspectionCount: readNumber(record.totalInspectionCount, "totalInspectionCount"),
    openInspectionCount: readNumber(record.openInspectionCount, "openInspectionCount"),
    completedInspectionCount: readNumber(record.completedInspectionCount, "completedInspectionCount"),
    passedInspectionCount: readNumber(record.passedInspectionCount, "passedInspectionCount"),
    failedInspectionCount: readNumber(record.failedInspectionCount, "failedInspectionCount"),
    releasedInspectionCount: readNumber(record.releasedInspectionCount, "releasedInspectionCount"),
    rejectedInspectionCount: readNumber(record.rejectedInspectionCount, "rejectedInspectionCount"),
    openNcrCount: readNumber(record.openNcrCount, "openNcrCount"),
    criticalNcrCount: readNumber(record.criticalNcrCount, "criticalNcrCount"),
    activeHoldCount: readNumber(record.activeHoldCount, "activeHoldCount"),
    blockingProductionOrderCount: readNumber(
      record.blockingProductionOrderCount,
      "blockingProductionOrderCount",
    ),
    openCorrectiveActionCount: readNumber(record.openCorrectiveActionCount, "openCorrectiveActionCount"),
    overdueCorrectiveActionCount: readNumber(
      record.overdueCorrectiveActionCount,
      "overdueCorrectiveActionCount",
    ),
    firstPassYieldPercent: readOptionalNumber(record.firstPassYieldPercent),
    passRatePercent: readOptionalNumber(record.passRatePercent),
    releaseRatePercent: readOptionalNumber(record.releaseRatePercent),
  };
}

export function parseQualityTrendListApiResponse(payload: unknown) {
  if (!Array.isArray(payload)) {
    throw new Error("Invalid trend list payload.");
  }
  return payload.map((item) => {
    if (typeof item !== "object" || item === null) {
      throw new Error("Invalid trend period.");
    }
    const record = item as Record<string, unknown>;
    return {
      periodKey: readString(record.periodKey, "trend.periodKey"),
      periodStartUtc: readString(record.periodStartUtc, "trend.periodStartUtc"),
      inspectionCount: readNumber(record.inspectionCount, "trend.inspectionCount"),
      passCount: readNumber(record.passCount, "trend.passCount"),
      failCount: readNumber(record.failCount, "trend.failCount"),
      ncrCount: readNumber(record.ncrCount, "trend.ncrCount"),
      criticalNcrCount: readNumber(record.criticalNcrCount, "trend.criticalNcrCount"),
      holdCount: readNumber(record.holdCount, "trend.holdCount"),
    };
  });
}

export function parseQualityFailureBreakdownApiResponse(payload: unknown) {
  if (!Array.isArray(payload)) {
    throw new Error("Invalid failure breakdown payload.");
  }
  return payload.map((item) => {
    if (typeof item !== "object" || item === null) {
      throw new Error("Invalid failure breakdown item.");
    }
    const record = item as Record<string, unknown>;
    return {
      dimension: readString(record.dimension, "failure.dimension"),
      key: readString(record.key, "failure.key"),
      label: readOptionalString(record.label),
      count: readNumber(record.count, "failure.count"),
    };
  });
}

export function parseQualityNcrAnalyticsApiResponse(payload: unknown) {
  if (typeof payload !== "object" || payload === null) {
    throw new Error("Invalid NCR analytics payload.");
  }
  const record = payload as Record<string, unknown>;
  return {
    openCount: readNumber(record.openCount, "ncrAnalytics.openCount"),
    resolvedCount: readNumber(record.resolvedCount, "ncrAnalytics.resolvedCount"),
    closedCount: readNumber(record.closedCount, "ncrAnalytics.closedCount"),
    criticalCount: readNumber(record.criticalCount, "ncrAnalytics.criticalCount"),
    majorCount: readNumber(record.majorCount, "ncrAnalytics.majorCount"),
    minorCount: readNumber(record.minorCount, "ncrAnalytics.minorCount"),
    averageAgeHours: readOptionalNumber(record.averageAgeHours),
    averageResolutionHours: readOptionalNumber(record.averageResolutionHours),
    overdueCorrectiveActionCount: readNumber(
      record.overdueCorrectiveActionCount,
      "ncrAnalytics.overdueCorrectiveActionCount",
    ),
  };
}

export function parseQualityHoldAnalyticsApiResponse(payload: unknown) {
  if (typeof payload !== "object" || payload === null) {
    throw new Error("Invalid hold analytics payload.");
  }
  const record = payload as Record<string, unknown>;
  return {
    activeCount: readNumber(record.activeCount, "holdAnalytics.activeCount"),
    releasedCount: readNumber(record.releasedCount, "holdAnalytics.releasedCount"),
    affectedProductionOrderCount: readNumber(
      record.affectedProductionOrderCount,
      "holdAnalytics.affectedProductionOrderCount",
    ),
    averageHoldDurationHours: readOptionalNumber(record.averageHoldDurationHours),
  };
}

export function parseQualityCorrectiveActionAnalyticsApiResponse(payload: unknown) {
  if (typeof payload !== "object" || payload === null) {
    throw new Error("Invalid corrective action analytics payload.");
  }
  const record = payload as Record<string, unknown>;
  return {
    openCount: readNumber(record.openCount, "caAnalytics.openCount"),
    inProgressCount: readNumber(record.inProgressCount, "caAnalytics.inProgressCount"),
    completedCount: readNumber(record.completedCount, "caAnalytics.completedCount"),
    overdueCount: readNumber(record.overdueCount, "caAnalytics.overdueCount"),
    averageCompletionLeadTimeHours: readOptionalNumber(record.averageCompletionLeadTimeHours),
  };
}

export function parseQualityInspectionReportListApiResponse(payload: unknown) {
  if (!Array.isArray(payload)) {
    throw new Error("Invalid inspection report list payload.");
  }
  return payload.map(parseQualityInspectionInternalSummaryApiResponse);
}

export function parseQualityInspectionInternalSummaryApiResponse(payload: unknown) {
  if (typeof payload !== "object" || payload === null) {
    throw new Error("Invalid inspection summary payload.");
  }
  const record = payload as Record<string, unknown>;
  const measurements = Array.isArray(record.measurements)
    ? record.measurements.map(parseInspectionReportMeasurement)
    : [];
  return {
    inspectionId: readString(record.inspectionId, "summary.inspectionId"),
    inspectionNumber: readString(record.inspectionNumber, "summary.inspectionNumber"),
    facilityReference: readString(record.facilityReference, "summary.facilityReference"),
    productionOrderId: readString(record.productionOrderId, "summary.productionOrderId"),
    productionOrderReference: readOptionalString(record.productionOrderReference),
    productReference: readOptionalString(record.productReference),
    controlPlanId: readString(record.controlPlanId, "summary.controlPlanId"),
    controlPlanCode: readString(record.controlPlanCode, "summary.controlPlanCode"),
    controlPlanName: readString(record.controlPlanName, "summary.controlPlanName"),
    controlPlanRevision: readString(record.controlPlanRevision, "summary.controlPlanRevision"),
    inspectionDateUtc: readOptionalString(record.inspectionDateUtc),
    inspectorSubjectId: readOptionalString(record.inspectorSubjectId),
    overallResult: readString(record.overallResult, "summary.overallResult"),
    releaseStatus: readString(record.releaseStatus, "summary.releaseStatus"),
    measurements,
    hasBlockingHold: readBoolean(record.hasBlockingHold, "summary.hasBlockingHold"),
    openNcrCount: readNumber(record.openNcrCount, "summary.openNcrCount"),
    blockingSummary: readOptionalString(record.blockingSummary),
  };
}

export function parseQualityReleaseIntelligenceApiResponse(payload: unknown) {
  if (typeof payload !== "object" || payload === null) {
    throw new Error("Invalid release intelligence payload.");
  }
  const record = payload as Record<string, unknown>;
  const reasonCodes = Array.isArray(record.reasonCodes)
    ? record.reasonCodes.map((item, index) => readString(item, `reasonCodes[${index}]`))
    : [];
  return {
    available: readBoolean(record.available, "releaseIntelligence.available"),
    inspectionStatus: readOptionalString(record.inspectionStatus),
    overallResult: readOptionalString(record.overallResult),
    releaseStatus: readOptionalString(record.releaseStatus),
    blocking: readBoolean(record.blocking, "releaseIntelligence.blocking"),
    activeHoldCount: readNumber(record.activeHoldCount, "releaseIntelligence.activeHoldCount"),
    openNcrCount: readNumber(record.openNcrCount, "releaseIntelligence.openNcrCount"),
    criticalNcrCount: readNumber(record.criticalNcrCount, "releaseIntelligence.criticalNcrCount"),
    incompleteCorrectiveActionCount: readNumber(
      record.incompleteCorrectiveActionCount,
      "releaseIntelligence.incompleteCorrectiveActionCount",
    ),
    readyForRelease: readBoolean(record.readyForRelease, "releaseIntelligence.readyForRelease"),
    reasonCodes,
    reasonSummary: readOptionalString(record.reasonSummary),
  };
}

export function parseQualityExportReadinessApiResponse(payload: unknown) {
  if (typeof payload !== "object" || payload === null) {
    throw new Error("Invalid export readiness payload.");
  }
  const record = payload as Record<string, unknown>;
  return {
    qualityReleaseStatus: readOptionalString(record.qualityReleaseStatus),
    qualityBlocking: readBoolean(record.qualityBlocking, "exportReadiness.qualityBlocking"),
    qualityReady: readBoolean(record.qualityReady, "exportReadiness.qualityReady"),
    reason: readOptionalString(record.reason),
  };
}

function parseResultTraceabilityItem(value: unknown) {
  if (typeof value !== "object" || value === null) {
    throw new Error("Invalid result traceability item.");
  }
  const record = value as Record<string, unknown>;
  return {
    id: readString(record.id, "traceResult.id"),
    definitionCode: readString(record.definitionCode, "traceResult.definitionCode"),
    definitionName: readString(record.definitionName, "traceResult.definitionName"),
    result: readString(record.result, "traceResult.result"),
    recordedAtUtc: readString(record.recordedAtUtc, "traceResult.recordedAtUtc"),
    recordedBySubjectId: readString(record.recordedBySubjectId, "traceResult.recordedBySubjectId"),
  };
}

function parseInspectionTraceabilityItem(value: unknown) {
  if (typeof value !== "object" || value === null) {
    throw new Error("Invalid inspection traceability item.");
  }
  const record = value as Record<string, unknown>;
  const results = Array.isArray(record.results)
    ? record.results.map(parseResultTraceabilityItem)
    : [];
  return {
    id: readString(record.id, "traceInspection.id"),
    inspectionNumber: readString(record.inspectionNumber, "traceInspection.inspectionNumber"),
    status: readString(record.status, "traceInspection.status"),
    overallResult: readString(record.overallResult, "traceInspection.overallResult"),
    releaseStatus: readString(record.releaseStatus, "traceInspection.releaseStatus"),
    completedAtUtc: readOptionalString(record.completedAtUtc),
    inspectorSubjectId: readOptionalString(record.inspectorSubjectId),
    results,
  };
}

function parseNcrTraceabilityItem(value: unknown) {
  if (typeof value !== "object" || value === null) {
    throw new Error("Invalid NCR traceability item.");
  }
  const record = value as Record<string, unknown>;
  return {
    id: readString(record.id, "traceNcr.id"),
    ncrNumber: readString(record.ncrNumber, "traceNcr.ncrNumber"),
    severity: readString(record.severity, "traceNcr.severity"),
    status: readString(record.status, "traceNcr.status"),
    dispositionType: readOptionalString(record.dispositionType),
    holdStatus: readString(record.holdStatus, "traceNcr.holdStatus"),
    detectedAtUtc: readString(record.detectedAtUtc, "traceNcr.detectedAtUtc"),
    ownerSubjectId: readOptionalString(record.ownerSubjectId),
  };
}

function parseHoldTraceabilityItem(value: unknown) {
  if (typeof value !== "object" || value === null) {
    throw new Error("Invalid hold traceability item.");
  }
  const record = value as Record<string, unknown>;
  return {
    id: readString(record.id, "traceHold.id"),
    ncrId: readString(record.ncrId, "traceHold.ncrId"),
    scope: readString(record.scope, "traceHold.scope"),
    status: readString(record.status, "traceHold.status"),
    reason: readString(record.reason, "traceHold.reason"),
    placedAtUtc: readString(record.placedAtUtc, "traceHold.placedAtUtc"),
    placedBySubjectId: readString(record.placedBySubjectId, "traceHold.placedBySubjectId"),
    releasedAtUtc: readOptionalString(record.releasedAtUtc),
    releasedBySubjectId: readOptionalString(record.releasedBySubjectId),
  };
}

function parseCorrectiveActionTraceabilityItem(value: unknown) {
  if (typeof value !== "object" || value === null) {
    throw new Error("Invalid corrective action traceability item.");
  }
  const record = value as Record<string, unknown>;
  return {
    id: readString(record.id, "traceAction.id"),
    ncrId: readString(record.ncrId, "traceAction.ncrId"),
    title: readString(record.title, "traceAction.title"),
    status: readString(record.status, "traceAction.status"),
    dueAtUtc: readOptionalString(record.dueAtUtc),
    completedAtUtc: readOptionalString(record.completedAtUtc),
    ownerSubjectId: readOptionalString(record.ownerSubjectId),
  };
}

export function parseProductionOrderTraceabilityApiResponse(payload: unknown) {
  if (typeof payload !== "object" || payload === null) {
    throw new Error("Invalid production order traceability payload.");
  }
  const record = payload as Record<string, unknown>;
  const inspections = Array.isArray(record.inspections)
    ? record.inspections.map(parseInspectionTraceabilityItem)
    : [];
  const nonconformances = Array.isArray(record.nonconformances)
    ? record.nonconformances.map(parseNcrTraceabilityItem)
    : [];
  const holds = Array.isArray(record.holds) ? record.holds.map(parseHoldTraceabilityItem) : [];
  const correctiveActions = Array.isArray(record.correctiveActions)
    ? record.correctiveActions.map(parseCorrectiveActionTraceabilityItem)
    : [];
  return {
    productionOrderId: readString(record.productionOrderId, "traceability.productionOrderId"),
    productionOrderReference: readOptionalString(record.productionOrderReference),
    productReference: readOptionalString(record.productReference),
    inspections,
    nonconformances,
    holds,
    correctiveActions,
  };
}
