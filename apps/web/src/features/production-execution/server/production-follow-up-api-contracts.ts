import type {
  ProductionFollowUpAuditEvent,
  ProductionFollowUpDetail,
  ProductionFollowUpRow,
  ProductionFollowUpStatus,
  ProductionFollowUpTaskPurpose,
  ProductionFollowUpWorkspacePayload,
} from "../domain/production-follow-up";

export type ParseResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | Readonly<{ status: "failure"; message: string }>;

const FOLLOW_UP_STATUSES = new Set<ProductionFollowUpStatus>([
  "requires-review",
  "acknowledged",
  "task-created",
  "resolved",
  "dismissed",
]);

const TASK_PURPOSES = new Set<ProductionFollowUpTaskPurpose>([
  "human-review-draft",
  "data-review-draft",
]);

function parseString(value: unknown, field: string): ParseResult<string> {
  if (typeof value !== "string" || value.trim().length === 0) {
    return { status: "failure", message: `${field} is required.` };
  }
  return { status: "success", value };
}

function parseOptionalString(value: unknown): string | undefined {
  return typeof value === "string" && value.length > 0 ? value : undefined;
}

function parseBoolean(value: unknown, fallback = false): boolean {
  return typeof value === "boolean" ? value : fallback;
}

function parseNumber(value: unknown, field: string): ParseResult<number> {
  if (typeof value !== "number" || Number.isNaN(value)) {
    return { status: "failure", message: `${field} is required.` };
  }
  return { status: "success", value };
}

function parseFollowUpStatus(value: unknown): ParseResult<ProductionFollowUpStatus> {
  if (typeof value !== "string" || !FOLLOW_UP_STATUSES.has(value as ProductionFollowUpStatus)) {
    return { status: "failure", message: "followUpStatus is invalid." };
  }
  return { status: "success", value: value as ProductionFollowUpStatus };
}

function parseTaskPurpose(value: unknown): ProductionFollowUpTaskPurpose | undefined {
  if (typeof value !== "string" || !TASK_PURPOSES.has(value as ProductionFollowUpTaskPurpose)) {
    return undefined;
  }
  return value as ProductionFollowUpTaskPurpose;
}

function parseAuditEvent(value: unknown): ParseResult<ProductionFollowUpAuditEvent> {
  if (!value || typeof value !== "object") {
    return { status: "failure", message: "auditEvent is invalid." };
  }
  const row = value as Record<string, unknown>;
  const eventType = parseString(row.eventType, "eventType");
  if (eventType.status === "failure") return eventType;
  const payloadJson = parseString(row.payloadJson, "payloadJson");
  if (payloadJson.status === "failure") return payloadJson;
  const actor = parseString(row.actor, "actor");
  if (actor.status === "failure") return actor;
  const occurredAtUtc = parseString(row.occurredAtUtc, "occurredAtUtc");
  if (occurredAtUtc.status === "failure") return occurredAtUtc;
  return {
    status: "success",
    value: {
      eventType: eventType.value,
      payloadJson: payloadJson.value,
      actor: actor.value,
      occurredAtUtc: occurredAtUtc.value,
    },
  };
}

function parseRow(value: unknown): ParseResult<ProductionFollowUpRow> {
  if (!value || typeof value !== "object") {
    return { status: "failure", message: "exception row is invalid." };
  }
  const row = value as Record<string, unknown>;
  const id = parseString(row.id, "id");
  if (id.status === "failure") return id;
  const productionOrderId = parseString(row.productionOrderId, "productionOrderId");
  if (productionOrderId.status === "failure") return productionOrderId;
  const followUpStatus = parseFollowUpStatus(row.followUpStatus);
  if (followUpStatus.status === "failure") return followUpStatus;
  const version = parseNumber(row.version, "version");
  if (version.status === "failure") return version;
  const raisedAtUtc = parseString(row.raisedAtUtc, "raisedAtUtc");
  if (raisedAtUtc.status === "failure") return raisedAtUtc;
  const exceptionType = parseString(row.exceptionType, "exceptionType");
  if (exceptionType.status === "failure") return exceptionType;
  const severity = parseString(row.severity, "severity");
  if (severity.status === "failure") return severity;
  const exceptionStatus = parseString(row.exceptionStatus, "exceptionStatus");
  if (exceptionStatus.status === "failure") return exceptionStatus;

  return {
    status: "success",
    value: {
      id: id.value,
      productionOrderId: productionOrderId.value,
      productionOrderReference: parseOptionalString(row.productionOrderReference) ?? "",
      exceptionType: exceptionType.value,
      severity: severity.value,
      exceptionStatus: exceptionStatus.value,
      message: typeof row.message === "string" ? row.message : "",
      raisedAtUtc: raisedAtUtc.value,
      followUpStatus: followUpStatus.value,
      linkedAiTaskId: parseOptionalString(row.linkedAiTaskId),
      version: version.value,
      canCreateTaskDraft: parseBoolean(row.canCreateTaskDraft),
      taskDraftPurpose: parseTaskPurpose(row.taskDraftPurpose),
      hasInsufficientEvidence: parseBoolean(row.hasInsufficientEvidence),
      isBlockedOrDelayed: parseBoolean(row.isBlockedOrDelayed),
    },
  };
}

export function parseProductionFollowUpWorkspacePayload(payload: unknown): ParseResult<ProductionFollowUpWorkspacePayload> {
  if (!payload || typeof payload !== "object") {
    return { status: "failure", message: "workspace payload is invalid." };
  }
  const data = payload as Record<string, unknown>;
  const automationDisclaimer =
    typeof data.automationDisclaimer === "string" ? data.automationDisclaimer : "";
  const kpisRaw = data.kpis;
  if (!kpisRaw || typeof kpisRaw !== "object") {
    return { status: "failure", message: "kpis is required." };
  }
  const kpis = kpisRaw as Record<string, unknown>;
  const capabilitiesRaw = data.capabilities;
  if (!capabilitiesRaw || typeof capabilitiesRaw !== "object") {
    return { status: "failure", message: "capabilities is required." };
  }
  const capabilities = capabilitiesRaw as Record<string, unknown>;
  if (!Array.isArray(data.exceptions)) {
    return { status: "failure", message: "exceptions is required." };
  }
  const exceptions: ProductionFollowUpRow[] = [];
  for (const item of data.exceptions) {
    const parsed = parseRow(item);
    if (parsed.status === "failure") return parsed;
    exceptions.push(parsed.value);
  }

  return {
    status: "success",
    value: {
      automationDisclaimer,
      kpis: {
        requiresReviewCount: typeof kpis.requiresReviewCount === "number" ? kpis.requiresReviewCount : 0,
        acknowledgedCount: typeof kpis.acknowledgedCount === "number" ? kpis.acknowledgedCount : 0,
        taskLinkedCount: typeof kpis.taskLinkedCount === "number" ? kpis.taskLinkedCount : 0,
        blockedOrDelayedCount: typeof kpis.blockedOrDelayedCount === "number" ? kpis.blockedOrDelayedCount : 0,
        dataReviewCount: typeof kpis.dataReviewCount === "number" ? kpis.dataReviewCount : 0,
      },
      capabilities: {
        canRead: parseBoolean(capabilities.canRead),
        canManage: parseBoolean(capabilities.canManage),
        canCreateTask: parseBoolean(capabilities.canCreateTask),
      },
      exceptions,
    },
  };
}

export function parseProductionFollowUpDetail(payload: unknown): ParseResult<ProductionFollowUpDetail> {
  if (!payload || typeof payload !== "object") {
    return { status: "failure", message: "detail payload is invalid." };
  }
  const row = payload as Record<string, unknown>;
  const id = parseString(row.id, "id");
  if (id.status === "failure") return id;
  const productionOrderId = parseString(row.productionOrderId, "productionOrderId");
  if (productionOrderId.status === "failure") return productionOrderId;
  const followUpStatus = parseFollowUpStatus(row.followUpStatus);
  if (followUpStatus.status === "failure") return followUpStatus;
  const version = parseNumber(row.version, "version");
  if (version.status === "failure") return version;
  const raisedAtUtc = parseString(row.raisedAtUtc, "raisedAtUtc");
  if (raisedAtUtc.status === "failure") return raisedAtUtc;
  const raisedBySubjectId = parseString(row.raisedBySubjectId, "raisedBySubjectId");
  if (raisedBySubjectId.status === "failure") return raisedBySubjectId;
  const exceptionType = parseString(row.exceptionType, "exceptionType");
  if (exceptionType.status === "failure") return exceptionType;
  const severity = parseString(row.severity, "severity");
  if (severity.status === "failure") return severity;
  const exceptionStatus = parseString(row.exceptionStatus, "exceptionStatus");
  if (exceptionStatus.status === "failure") return exceptionStatus;
  const productionOrderReference =
    typeof row.productionOrderReference === "string" ? row.productionOrderReference : "";
  const productionOrderTitle = parseString(row.productionOrderTitle, "productionOrderTitle");
  if (productionOrderTitle.status === "failure") return productionOrderTitle;
  const productionOrderStatus = parseString(row.productionOrderStatus, "productionOrderStatus");
  if (productionOrderStatus.status === "failure") return productionOrderStatus;
  const evidenceSummary = typeof row.evidenceSummary === "string" ? row.evidenceSummary : "";

  const auditEvents: ProductionFollowUpAuditEvent[] = [];
  if (Array.isArray(row.auditEvents)) {
    for (const item of row.auditEvents) {
      const parsed = parseAuditEvent(item);
      if (parsed.status === "failure") return parsed;
      auditEvents.push(parsed.value);
    }
  }

  return {
    status: "success",
    value: {
      id: id.value,
      productionOrderId: productionOrderId.value,
      productionOrderReference,
      productionOrderTitle: productionOrderTitle.value,
      productionOrderStatus: productionOrderStatus.value,
      machineReference: parseOptionalString(row.machineReference),
      moldReference: parseOptionalString(row.moldReference),
      productReference: parseOptionalString(row.productReference),
      exceptionType: exceptionType.value,
      severity: severity.value,
      exceptionStatus: exceptionStatus.value,
      message: typeof row.message === "string" ? row.message : "",
      raisedAtUtc: raisedAtUtc.value,
      raisedBySubjectId: raisedBySubjectId.value,
      followUpStatus: followUpStatus.value,
      linkedAiTaskId: parseOptionalString(row.linkedAiTaskId),
      version: version.value,
      canCreateTaskDraft: parseBoolean(row.canCreateTaskDraft),
      taskDraftPurpose: parseTaskPurpose(row.taskDraftPurpose),
      hasInsufficientEvidence: parseBoolean(row.hasInsufficientEvidence),
      evidenceSummary,
      auditEvents,
    },
  };
}

export function parseProductionFollowUpTaskDraftResponse(payload: unknown): ParseResult<Readonly<{
  taskId: string;
  exceptionId: string;
  idempotentReplay: boolean;
  exception: ProductionFollowUpDetail;
}>> {
  if (!payload || typeof payload !== "object") {
    return { status: "failure", message: "task draft response is invalid." };
  }
  const data = payload as Record<string, unknown>;
  const taskId = parseString(data.taskId, "taskId");
  if (taskId.status === "failure") return taskId;
  const exceptionId = parseString(data.exceptionId, "exceptionId");
  if (exceptionId.status === "failure") return exceptionId;
  const exception = parseProductionFollowUpDetail(data.exception);
  if (exception.status === "failure") return exception;
  return {
    status: "success",
    value: {
      taskId: taskId.value,
      exceptionId: exceptionId.value,
      idempotentReplay: parseBoolean(data.idempotentReplay),
      exception: exception.value,
    },
  };
}
