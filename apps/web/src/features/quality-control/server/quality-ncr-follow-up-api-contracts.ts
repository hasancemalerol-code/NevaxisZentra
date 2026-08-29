import type {
  QualityNcrFollowUpAuditEvent,
  QualityNcrFollowUpFinding,
  QualityNcrFollowUpRow,
  QualityNcrFollowUpStatus,
  QualityNcrFollowUpTaskPurpose,
  QualityNcrFollowUpWorkspacePayload,
} from "../domain/quality-ncr-follow-up";

export type ParseResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | Readonly<{ status: "failure"; message: string }>;

const FOLLOW_UP_STATUSES = new Set<QualityNcrFollowUpStatus>([
  "requires-review",
  "acknowledged",
  "task-created",
  "resolved",
  "dismissed",
]);

const TASK_PURPOSES = new Set<QualityNcrFollowUpTaskPurpose>([
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

function parseFollowUpStatus(value: unknown): ParseResult<QualityNcrFollowUpStatus> {
  if (typeof value !== "string" || !FOLLOW_UP_STATUSES.has(value as QualityNcrFollowUpStatus)) {
    return { status: "failure", message: "followUpStatus is invalid." };
  }
  return { status: "success", value: value as QualityNcrFollowUpStatus };
}

function parseTaskPurpose(value: unknown): QualityNcrFollowUpTaskPurpose | undefined {
  if (typeof value !== "string" || !TASK_PURPOSES.has(value as QualityNcrFollowUpTaskPurpose)) {
    return undefined;
  }
  return value as QualityNcrFollowUpTaskPurpose;
}

function parseAuditEvent(value: unknown): ParseResult<QualityNcrFollowUpAuditEvent> {
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

function parseFollowUpRow(value: unknown): ParseResult<QualityNcrFollowUpRow> {
  if (!value || typeof value !== "object") {
    return { status: "failure", message: "finding row is invalid." };
  }
  const row = value as Record<string, unknown>;
  const id = parseString(row.id, "id");
  if (id.status === "failure") return id;
  const ncrNumber = parseString(row.ncrNumber, "ncrNumber");
  if (ncrNumber.status === "failure") return ncrNumber;
  const title = parseString(row.title, "title");
  if (title.status === "failure") return title;
  const severity = parseString(row.severity, "severity");
  if (severity.status === "failure") return severity;
  const status = parseString(row.status, "status");
  if (status.status === "failure") return status;
  const followUpStatus = parseFollowUpStatus(row.followUpStatus);
  if (followUpStatus.status === "failure") return followUpStatus;
  const detectedAtUtc = parseString(row.detectedAtUtc, "detectedAtUtc");
  if (detectedAtUtc.status === "failure") return detectedAtUtc;

  return {
    status: "success",
    value: {
      id: id.value,
      ncrNumber: ncrNumber.value,
      title: title.value,
      severity: severity.value,
      status: status.value,
      followUpStatus: followUpStatus.value,
      holdStatus: parseOptionalString(row.holdStatus),
      dispositionStatus: parseOptionalString(row.dispositionStatus),
      productReference: parseOptionalString(row.productReference),
      productionOrderId: parseOptionalString(row.productionOrderId),
      productionOrderReference: parseOptionalString(row.productionOrderReference),
      detectedAtUtc: detectedAtUtc.value,
      linkedAiTaskId: parseOptionalString(row.linkedAiTaskId),
      canCreateTaskDraft: parseBoolean(row.canCreateTaskDraft),
      taskDraftPurpose: parseTaskPurpose(row.taskDraftPurpose),
      hasInsufficientEvidence: parseBoolean(row.hasInsufficientEvidence),
    },
  };
}

export function parseQualityNcrFollowUpFinding(value: unknown): ParseResult<QualityNcrFollowUpFinding> {
  if (!value || typeof value !== "object") {
    return { status: "failure", message: "finding is invalid." };
  }
  const row = value as Record<string, unknown>;
  const id = parseString(row.id, "id");
  if (id.status === "failure") return id;
  const ncrNumber = parseString(row.ncrNumber, "ncrNumber");
  if (ncrNumber.status === "failure") return ncrNumber;
  const title = parseString(row.title, "title");
  if (title.status === "failure") return title;
  const description = parseString(row.description, "description");
  if (description.status === "failure") return description;
  const severity = parseString(row.severity, "severity");
  if (severity.status === "failure") return severity;
  const status = parseString(row.status, "status");
  if (status.status === "failure") return status;
  const detectedAtUtc = parseString(row.detectedAtUtc, "detectedAtUtc");
  if (detectedAtUtc.status === "failure") return detectedAtUtc;
  const version = parseNumber(row.version, "version");
  if (version.status === "failure") return version;
  const followUpStatus = parseFollowUpStatus(row.followUpStatus);
  if (followUpStatus.status === "failure") return followUpStatus;
  const evidenceSummary = typeof row.evidenceSummary === "string" ? row.evidenceSummary : "";

  const auditEvents: QualityNcrFollowUpAuditEvent[] = [];
  if (Array.isArray(row.auditEvents)) {
    for (const event of row.auditEvents) {
      const parsed = parseAuditEvent(event);
      if (parsed.status === "failure") return parsed;
      auditEvents.push(parsed.value);
    }
  }

  let productionContext: QualityNcrFollowUpFinding["productionContext"];
  if (row.productionContext && typeof row.productionContext === "object") {
    const ctx = row.productionContext as Record<string, unknown>;
    const productionOrderId = parseString(ctx.productionOrderId, "productionOrderId");
    if (productionOrderId.status === "success") {
      productionContext = {
        productionOrderId: productionOrderId.value,
        referenceCode: parseOptionalString(ctx.referenceCode),
        productReference: parseOptionalString(ctx.productReference),
        status: parseOptionalString(ctx.status),
      };
    }
  }

  return {
    status: "success",
    value: {
      id: id.value,
      ncrNumber: ncrNumber.value,
      title: title.value,
      description: description.value,
      category: parseOptionalString(row.category),
      severity: severity.value,
      status: status.value,
      holdStatus: parseOptionalString(row.holdStatus),
      dispositionStatus: parseOptionalString(row.dispositionStatus),
      dispositionType: parseOptionalString(row.dispositionType),
      dispositionNote: parseOptionalString(row.dispositionNote),
      productionOrderId: parseOptionalString(row.productionOrderId),
      productionContext,
      qualityInspectionId: parseOptionalString(row.qualityInspectionId),
      qualityInspectionNumber: parseOptionalString(row.qualityInspectionNumber),
      qualityInspectionResultId: parseOptionalString(row.qualityInspectionResultId),
      productReference: parseOptionalString(row.productReference),
      operationReference: parseOptionalString(row.operationReference),
      detectedAtUtc: detectedAtUtc.value,
      version: version.value,
      followUpStatus: followUpStatus.value,
      linkedAiTaskId: parseOptionalString(row.linkedAiTaskId),
      canCreateTaskDraft: parseBoolean(row.canCreateTaskDraft),
      taskDraftPurpose: parseTaskPurpose(row.taskDraftPurpose),
      hasInsufficientEvidence: parseBoolean(row.hasInsufficientEvidence),
      evidenceSummary,
      auditEvents,
    },
  };
}

export function parseQualityNcrFollowUpWorkspacePayload(
  value: unknown,
): ParseResult<QualityNcrFollowUpWorkspacePayload> {
  if (!value || typeof value !== "object") {
    return { status: "failure", message: "workspace payload is invalid." };
  }
  const payload = value as Record<string, unknown>;
  const automationDisclaimer = parseString(payload.automationDisclaimer, "automationDisclaimer");
  if (automationDisclaimer.status === "failure") return automationDisclaimer;

  if (!payload.kpis || typeof payload.kpis !== "object") {
    return { status: "failure", message: "kpis is required." };
  }
  const kpis = payload.kpis as Record<string, unknown>;
  const requiresReviewCount = parseNumber(kpis.requiresReviewCount, "requiresReviewCount");
  if (requiresReviewCount.status === "failure") return requiresReviewCount;
  const acknowledgedCount = parseNumber(kpis.acknowledgedCount, "acknowledgedCount");
  if (acknowledgedCount.status === "failure") return acknowledgedCount;
  const taskLinkedCount = parseNumber(kpis.taskLinkedCount, "taskLinkedCount");
  if (taskLinkedCount.status === "failure") return taskLinkedCount;
  const highPriorityCount = parseNumber(kpis.highPriorityCount, "highPriorityCount");
  if (highPriorityCount.status === "failure") return highPriorityCount;
  const dataReviewCount = parseNumber(kpis.dataReviewCount, "dataReviewCount");
  if (dataReviewCount.status === "failure") return dataReviewCount;

  if (!payload.capabilities || typeof payload.capabilities !== "object") {
    return { status: "failure", message: "capabilities is required." };
  }
  const capabilities = payload.capabilities as Record<string, unknown>;

  const findings: QualityNcrFollowUpRow[] = [];
  if (Array.isArray(payload.findings)) {
    for (const finding of payload.findings) {
      const parsed = parseFollowUpRow(finding);
      if (parsed.status === "failure") return parsed;
      findings.push(parsed.value);
    }
  }

  return {
    status: "success",
    value: {
      automationDisclaimer: automationDisclaimer.value,
      kpis: {
        requiresReviewCount: requiresReviewCount.value,
        acknowledgedCount: acknowledgedCount.value,
        taskLinkedCount: taskLinkedCount.value,
        highPriorityCount: highPriorityCount.value,
        dataReviewCount: dataReviewCount.value,
      },
      capabilities: {
        canRead: parseBoolean(capabilities.canRead),
        canManage: parseBoolean(capabilities.canManage),
        canCreateTask: parseBoolean(capabilities.canCreateTask),
      },
      findings,
    },
  };
}

export function parseQualityNcrFollowUpTaskDraftResponse(value: unknown): ParseResult<Readonly<{
  taskId: string;
  ncrId: string;
  idempotentReplay: boolean;
  finding: QualityNcrFollowUpFinding;
}>> {
  if (!value || typeof value !== "object") {
    return { status: "failure", message: "task draft response is invalid." };
  }
  const payload = value as Record<string, unknown>;
  const taskId = parseString(payload.taskId, "taskId");
  if (taskId.status === "failure") return taskId;
  const ncrId = parseString(payload.ncrId, "ncrId");
  if (ncrId.status === "failure") return ncrId;
  const finding = parseQualityNcrFollowUpFinding(payload.finding);
  if (finding.status === "failure") return finding;

  return {
    status: "success",
    value: {
      taskId: taskId.value,
      ncrId: ncrId.value,
      idempotentReplay: parseBoolean(payload.idempotentReplay),
      finding: finding.value,
    },
  };
}
