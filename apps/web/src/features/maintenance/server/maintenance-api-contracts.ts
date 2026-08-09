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
