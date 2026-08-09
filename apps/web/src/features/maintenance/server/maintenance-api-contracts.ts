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
  if (typeof value !== "number" || Number.isNaN(value)) {
    return undefined;
  }
  return value;
}

function readBoolean(value: unknown, field: string): boolean {
  if (typeof value !== "boolean") {
    throw new Error(`Invalid boolean for ${field}.`);
  }
  return value;
}

function parseActivity(value: unknown) {
  if (typeof value !== "object" || value === null) {
    throw new Error("Invalid maintenance activity.");
  }
  const record = value as Record<string, unknown>;
  return {
    action: readString(record.action, "activity.action"),
    eventType: readString(record.eventType, "activity.eventType"),
    occurredAtUtc: readString(record.occurredAtUtc, "activity.occurredAtUtc"),
    actorSubjectId: readOptionalString(record.actorSubjectId),
    summary: readString(record.summary, "activity.summary"),
  };
}

export function parseMaintenanceWorkspaceSummaryApiResponse(payload: unknown) {
  if (typeof payload !== "object" || payload === null) {
    throw new Error("Invalid maintenance workspace summary payload.");
  }
  const record = payload as Record<string, unknown>;
  return {
    openRequestCount: readNumber(record.openRequestCount, "openRequestCount"),
    openWorkOrderCount: readNumber(record.openWorkOrderCount, "openWorkOrderCount"),
    criticalWorkOrderCount: readNumber(record.criticalWorkOrderCount, "criticalWorkOrderCount"),
    inProgressCount: readNumber(record.inProgressCount, "inProgressCount"),
    waitingCount: readNumber(record.waitingCount, "waitingCount"),
    completedTodayCount: readNumber(record.completedTodayCount, "completedTodayCount"),
    overduePreventiveCount: readNumber(record.overduePreventiveCount, "overduePreventiveCount"),
  };
}

export function parseMaintenanceRequestListApiResponse(payload: unknown) {
  if (!Array.isArray(payload)) {
    throw new Error("Invalid maintenance request list payload.");
  }
  return payload.map((item) => {
    if (typeof item !== "object" || item === null) {
      throw new Error("Invalid maintenance request summary.");
    }
    const record = item as Record<string, unknown>;
    return {
      id: readString(record.id, "request.id"),
      requestNumber: readString(record.requestNumber, "request.requestNumber"),
      assetType: readString(record.assetType, "request.assetType"),
      assetId: readOptionalString(record.assetId),
      assetReference: readString(record.assetReference, "request.assetReference"),
      assetName: readOptionalString(record.assetName),
      title: readString(record.title, "request.title"),
      type: readString(record.type, "request.type"),
      priority: readString(record.priority, "request.priority"),
      status: readString(record.status, "request.status"),
      reportedBySubjectId: readString(record.reportedBySubjectId, "request.reportedBySubjectId"),
      reportedAtUtc: readString(record.reportedAtUtc, "request.reportedAtUtc"),
      version: readNumber(record.version, "request.version"),
      updatedAtUtc: readString(record.updatedAtUtc, "request.updatedAtUtc"),
    };
  });
}

export function parseMaintenanceRequestDetailApiResponse(payload: unknown) {
  if (typeof payload !== "object" || payload === null) {
    throw new Error("Invalid maintenance request detail payload.");
  }
  const record = payload as Record<string, unknown>;
  const activity = Array.isArray(record.activity)
    ? record.activity.map(parseActivity)
    : [];
  return {
    id: readString(record.id, "request.id"),
    requestNumber: readString(record.requestNumber, "request.requestNumber"),
    assetType: readString(record.assetType, "request.assetType"),
    assetId: readOptionalString(record.assetId),
    assetReference: readString(record.assetReference, "request.assetReference"),
    assetName: readOptionalString(record.assetName),
    productionOrderId: readOptionalString(record.productionOrderId),
    productionOrderReference: readOptionalString(record.productionOrderReference),
    productionOrderStatus: readOptionalString(record.productionOrderStatus),
    type: readString(record.type, "request.type"),
    priority: readString(record.priority, "request.priority"),
    status: readString(record.status, "request.status"),
    title: readString(record.title, "request.title"),
    description: readString(record.description, "request.description"),
    source: readString(record.source, "request.source"),
    reportedBySubjectId: readString(record.reportedBySubjectId, "request.reportedBySubjectId"),
    reportedAtUtc: readString(record.reportedAtUtc, "request.reportedAtUtc"),
    version: readNumber(record.version, "request.version"),
    createdAtUtc: readString(record.createdAtUtc, "request.createdAtUtc"),
    updatedAtUtc: readString(record.updatedAtUtc, "request.updatedAtUtc"),
    activity,
  };
}

export function parseMaintenanceWorkOrderListApiResponse(payload: unknown) {
  if (!Array.isArray(payload)) {
    throw new Error("Invalid maintenance work order list payload.");
  }
  return payload.map((item) => {
    if (typeof item !== "object" || item === null) {
      throw new Error("Invalid maintenance work order summary.");
    }
    const record = item as Record<string, unknown>;
    return {
      id: readString(record.id, "workOrder.id"),
      workOrderNumber: readString(record.workOrderNumber, "workOrder.workOrderNumber"),
      maintenanceRequestId: readOptionalString(record.maintenanceRequestId),
      maintenanceRequestNumber: readOptionalString(record.maintenanceRequestNumber),
      assetType: readString(record.assetType, "workOrder.assetType"),
      assetId: readOptionalString(record.assetId),
      assetReference: readString(record.assetReference, "workOrder.assetReference"),
      assetName: readOptionalString(record.assetName),
      type: readString(record.type, "workOrder.type"),
      priority: readString(record.priority, "workOrder.priority"),
      status: readString(record.status, "workOrder.status"),
      assignedTechnicianSubjectId: readOptionalString(record.assignedTechnicianSubjectId),
      plannedStartUtc: readOptionalString(record.plannedStartUtc),
      plannedEndUtc: readOptionalString(record.plannedEndUtc),
      startedAtUtc: readOptionalString(record.startedAtUtc),
      version: readNumber(record.version, "workOrder.version"),
      updatedAtUtc: readString(record.updatedAtUtc, "workOrder.updatedAtUtc"),
    };
  });
}

export function parseMaintenanceWorkOrderDetailApiResponse(payload: unknown) {
  if (typeof payload !== "object" || payload === null) {
    throw new Error("Invalid maintenance work order detail payload.");
  }
  const record = payload as Record<string, unknown>;
  const activity = Array.isArray(record.activity)
    ? record.activity.map(parseActivity)
    : [];
  return {
    id: readString(record.id, "workOrder.id"),
    workOrderNumber: readString(record.workOrderNumber, "workOrder.workOrderNumber"),
    maintenanceRequestId: readOptionalString(record.maintenanceRequestId),
    maintenanceRequestNumber: readOptionalString(record.maintenanceRequestNumber),
    assetType: readString(record.assetType, "workOrder.assetType"),
    assetId: readOptionalString(record.assetId),
    assetReference: readString(record.assetReference, "workOrder.assetReference"),
    assetName: readOptionalString(record.assetName),
    productionOrderId: readOptionalString(record.productionOrderId),
    productionOrderReference: readOptionalString(record.productionOrderReference),
    productionOrderStatus: readOptionalString(record.productionOrderStatus),
    preventivePlanId: readOptionalString(record.preventivePlanId),
    preventivePlanCode: readOptionalString(record.preventivePlanCode),
    preventiveDueId: readOptionalString(record.preventiveDueId),
    externalServiceCost: readOptionalNumber(record.externalServiceCost),
    otherCost: readOptionalNumber(record.otherCost),
    type: readString(record.type, "workOrder.type"),
    priority: readString(record.priority, "workOrder.priority"),
    status: readString(record.status, "workOrder.status"),
    title: readString(record.title, "workOrder.title"),
    description: readString(record.description, "workOrder.description"),
    assignedTechnicianSubjectId: readOptionalString(record.assignedTechnicianSubjectId),
    plannedStartUtc: readOptionalString(record.plannedStartUtc),
    plannedEndUtc: readOptionalString(record.plannedEndUtc),
    startedAtUtc: readOptionalString(record.startedAtUtc),
    completedAtUtc: readOptionalString(record.completedAtUtc),
    completionNote: readOptionalString(record.completionNote),
    resolutionCode: readOptionalString(record.resolutionCode),
    createdBySubjectId: readString(record.createdBySubjectId, "workOrder.createdBySubjectId"),
    version: readNumber(record.version, "workOrder.version"),
    createdAtUtc: readString(record.createdAtUtc, "workOrder.createdAtUtc"),
    updatedAtUtc: readString(record.updatedAtUtc, "workOrder.updatedAtUtc"),
    activity,
  };
}

export function parsePreventiveMaintenancePlanListApiResponse(payload: unknown) {
  if (!Array.isArray(payload)) {
    throw new Error("Invalid preventive maintenance plan list payload.");
  }
  return payload.map((item) => {
    if (typeof item !== "object" || item === null) {
      throw new Error("Invalid preventive maintenance plan summary.");
    }
    const record = item as Record<string, unknown>;
    return {
      id: readString(record.id, "plan.id"),
      code: readString(record.code, "plan.code"),
      name: readString(record.name, "plan.name"),
      assetType: readString(record.assetType, "plan.assetType"),
      assetId: readOptionalString(record.assetId),
      assetReference: readString(record.assetReference, "plan.assetReference"),
      intervalType: readString(record.intervalType, "plan.intervalType"),
      intervalValue: readNumber(record.intervalValue, "plan.intervalValue"),
      nextDueAtUtc: readOptionalString(record.nextDueAtUtc),
      status: readString(record.status, "plan.status"),
      version: readNumber(record.version, "plan.version"),
      updatedAtUtc: readString(record.updatedAtUtc, "plan.updatedAtUtc"),
    };
  });
}

export function parseMaintenanceWriteApiResponse(payload: unknown) {
  if (typeof payload !== "object" || payload === null) {
    throw new Error("Invalid maintenance write response.");
  }
  const record = payload as Record<string, unknown>;
  return {
    id: readString(record.id, "write.id"),
    version: readNumber(record.version, "write.version"),
  };
}

function parseWorkNote(value: unknown) {
  if (typeof value !== "object" || value === null) {
    throw new Error("Invalid maintenance work note.");
  }
  const record = value as Record<string, unknown>;
  return {
    id: readString(record.id, "note.id"),
    workOrderId: readString(record.workOrderId, "note.workOrderId"),
    noteType: readString(record.noteType, "note.noteType"),
    text: readString(record.text, "note.text"),
    isImportant: readBoolean(record.isImportant, "note.isImportant"),
    createdAtUtc: readString(record.createdAtUtc, "note.createdAtUtc"),
    createdBySubjectId: readString(record.createdBySubjectId, "note.createdBySubjectId"),
  };
}

export function parseMaintenanceWorkNoteListApiResponse(payload: unknown) {
  if (!Array.isArray(payload)) {
    throw new Error("Invalid maintenance work note list payload.");
  }
  return payload.map(parseWorkNote);
}

function parseTimelineEntry(value: unknown) {
  if (typeof value !== "object" || value === null) {
    throw new Error("Invalid maintenance timeline entry.");
  }
  const record = value as Record<string, unknown>;
  return {
    entryType: readString(record.entryType, "timeline.entryType"),
    title: readString(record.title, "timeline.title"),
    detail: readOptionalString(record.detail),
    occurredAtUtc: readString(record.occurredAtUtc, "timeline.occurredAtUtc"),
    actorSubjectId: readOptionalString(record.actorSubjectId),
  };
}

export function parseMaintenanceTimelineApiResponse(payload: unknown) {
  if (!Array.isArray(payload)) {
    throw new Error("Invalid maintenance timeline payload.");
  }
  return payload.map(parseTimelineEntry);
}

function parsePartUsage(value: unknown) {
  if (typeof value !== "object" || value === null) {
    throw new Error("Invalid maintenance part usage.");
  }
  const record = value as Record<string, unknown>;
  return {
    id: readString(record.id, "part.id"),
    workOrderId: readString(record.workOrderId, "part.workOrderId"),
    partReference: readString(record.partReference, "part.partReference"),
    partName: readOptionalString(record.partName),
    quantity: readNumber(record.quantity, "part.quantity"),
    unit: readString(record.unit, "part.unit"),
    sourceSystem: readOptionalString(record.sourceSystem),
    sourceReference: readOptionalString(record.sourceReference),
    estimatedUnitCost: readOptionalNumber(record.estimatedUnitCost),
    actualUnitCost: readOptionalNumber(record.actualUnitCost),
    estimatedLineCost: readOptionalNumber(record.estimatedLineCost),
    actualLineCost: readOptionalNumber(record.actualLineCost),
    recordedAtUtc: readString(record.recordedAtUtc, "part.recordedAtUtc"),
    recordedBySubjectId: readString(record.recordedBySubjectId, "part.recordedBySubjectId"),
  };
}

export function parseMaintenancePartUsageListApiResponse(payload: unknown) {
  if (!Array.isArray(payload)) {
    throw new Error("Invalid maintenance part usage list payload.");
  }
  return payload.map(parsePartUsage);
}

export function parseMaintenanceCostSummaryApiResponse(payload: unknown) {
  if (typeof payload !== "object" || payload === null) {
    throw new Error("Invalid maintenance cost summary payload.");
  }
  const record = payload as Record<string, unknown>;
  return {
    workOrderId: readString(record.workOrderId, "cost.workOrderId"),
    estimatedPartsCost: readOptionalNumber(record.estimatedPartsCost),
    actualPartsCost: readOptionalNumber(record.actualPartsCost),
    externalServiceCost: readOptionalNumber(record.externalServiceCost),
    otherCost: readOptionalNumber(record.otherCost),
    totalMaintenanceCost: readOptionalNumber(record.totalMaintenanceCost),
  };
}

function parsePreventiveDueSummary(value: unknown) {
  if (typeof value !== "object" || value === null) {
    throw new Error("Invalid preventive maintenance due summary.");
  }
  const record = value as Record<string, unknown>;
  return {
    id: readString(record.id, "due.id"),
    preventivePlanId: readString(record.preventivePlanId, "due.preventivePlanId"),
    preventivePlanCode: readOptionalString(record.preventivePlanCode),
    preventivePlanName: readOptionalString(record.preventivePlanName),
    assetType: readString(record.assetType, "due.assetType"),
    assetReference: readString(record.assetReference, "due.assetReference"),
    dueAtUtc: readString(record.dueAtUtc, "due.dueAtUtc"),
    status: readString(record.status, "due.status"),
    generatedAtUtc: readString(record.generatedAtUtc, "due.generatedAtUtc"),
    workOrderId: readOptionalString(record.workOrderId),
    workOrderNumber: readOptionalString(record.workOrderNumber),
    completedAtUtc: readOptionalString(record.completedAtUtc),
    version: readNumber(record.version, "due.version"),
  };
}

export function parsePreventiveMaintenanceDueListApiResponse(payload: unknown) {
  if (!Array.isArray(payload)) {
    throw new Error("Invalid preventive maintenance due list payload.");
  }
  return payload.map(parsePreventiveDueSummary);
}

export function parsePreventiveMaintenanceDueDetailApiResponse(payload: unknown) {
  if (typeof payload !== "object" || payload === null) {
    throw new Error("Invalid preventive maintenance due detail payload.");
  }
  const record = payload as Record<string, unknown>;
  return {
    id: readString(record.id, "due.id"),
    preventivePlanId: readString(record.preventivePlanId, "due.preventivePlanId"),
    preventivePlanCode: readString(record.preventivePlanCode, "due.preventivePlanCode"),
    preventivePlanName: readString(record.preventivePlanName, "due.preventivePlanName"),
    assetType: readString(record.assetType, "due.assetType"),
    assetReference: readString(record.assetReference, "due.assetReference"),
    dueAtUtc: readString(record.dueAtUtc, "due.dueAtUtc"),
    status: readString(record.status, "due.status"),
    generatedAtUtc: readString(record.generatedAtUtc, "due.generatedAtUtc"),
    workOrderId: readOptionalString(record.workOrderId),
    workOrderNumber: readOptionalString(record.workOrderNumber),
    completedAtUtc: readOptionalString(record.completedAtUtc),
    version: readNumber(record.version, "due.version"),
  };
}

export function parseMaintenanceAssetHistoryApiResponse(payload: unknown) {
  if (typeof payload !== "object" || payload === null) {
    throw new Error("Invalid maintenance asset history payload.");
  }
  const record = payload as Record<string, unknown>;
  return {
    assetReference: readString(record.assetReference, "history.assetReference"),
    requests: Array.isArray(record.requests)
      ? parseMaintenanceRequestListApiResponse(record.requests)
      : [],
    workOrders: Array.isArray(record.workOrders)
      ? parseMaintenanceWorkOrderListApiResponse(record.workOrders)
      : [],
    preventivePlans: Array.isArray(record.preventivePlans)
      ? parsePreventiveMaintenancePlanListApiResponse(record.preventivePlans)
      : [],
    preventiveDues: Array.isArray(record.preventiveDues)
      ? parsePreventiveMaintenanceDueListApiResponse(record.preventiveDues)
      : [],
    workNotes: Array.isArray(record.workNotes)
      ? parseMaintenanceWorkNoteListApiResponse(record.workNotes)
      : [],
    partUsages: Array.isArray(record.partUsages)
      ? parseMaintenancePartUsageListApiResponse(record.partUsages)
      : [],
    costSummaries: Array.isArray(record.costSummaries)
      ? record.costSummaries.map((item) => parseMaintenanceCostSummaryApiResponse(item))
      : [],
  };
}
