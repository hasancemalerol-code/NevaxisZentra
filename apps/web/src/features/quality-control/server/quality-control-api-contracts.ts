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
