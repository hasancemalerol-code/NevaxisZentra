import type {
  MoldFollowUpAuditEvent,
  MoldFollowUpDetail,
  MoldFollowUpLifecycleEvent,
  MoldFollowUpRow,
  MoldFollowUpStatus,
  MoldFollowUpTaskPurpose,
  MoldFollowUpWorkspacePayload,
} from "../domain/mold-follow-up";

export type ParseResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | Readonly<{ status: "failure"; message: string }>;

const FOLLOW_UP_STATUSES = new Set<MoldFollowUpStatus>([
  "requires-review",
  "acknowledged",
  "task-created",
  "resolved",
  "dismissed",
]);

const TASK_PURPOSES = new Set<MoldFollowUpTaskPurpose>([
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

function parseNumber(value: unknown): number | undefined {
  return typeof value === "number" && !Number.isNaN(value) ? value : undefined;
}

function parseFollowUpStatus(value: unknown): ParseResult<MoldFollowUpStatus> {
  if (typeof value !== "string" || !FOLLOW_UP_STATUSES.has(value as MoldFollowUpStatus)) {
    return { status: "failure", message: "followUpStatus is invalid." };
  }
  return { status: "success", value: value as MoldFollowUpStatus };
}

function parseTaskPurpose(value: unknown): MoldFollowUpTaskPurpose | undefined {
  if (typeof value !== "string" || !TASK_PURPOSES.has(value as MoldFollowUpTaskPurpose)) {
    return undefined;
  }
  return value as MoldFollowUpTaskPurpose;
}

function parseLifecycleEvent(value: unknown): ParseResult<MoldFollowUpLifecycleEvent> {
  if (!value || typeof value !== "object") {
    return { status: "failure", message: "lifecycleEvent is invalid." };
  }
  const row = value as Record<string, unknown>;
  const id = parseString(row.id, "id");
  if (id.status === "failure") return id;
  const previousState = parseString(row.previousState, "previousState");
  if (previousState.status === "failure") return previousState;
  const newState = parseString(row.newState, "newState");
  if (newState.status === "failure") return newState;
  const reason = parseString(row.reason, "reason");
  if (reason.status === "failure") return reason;
  const actorDisplayName = parseString(row.actorDisplayName, "actorDisplayName");
  if (actorDisplayName.status === "failure") return actorDisplayName;
  const occurredAtUtc = parseString(row.occurredAtUtc, "occurredAtUtc");
  if (occurredAtUtc.status === "failure") return occurredAtUtc;
  return {
    status: "success",
    value: {
      id: id.value,
      previousState: previousState.value,
      newState: newState.value,
      reason: reason.value,
      actorDisplayName: actorDisplayName.value,
      occurredAtUtc: occurredAtUtc.value,
      sourceReference: parseOptionalString(row.sourceReference),
    },
  };
}

function parseAuditEvent(value: unknown): ParseResult<MoldFollowUpAuditEvent> {
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

function parseRow(value: unknown): ParseResult<MoldFollowUpRow> {
  if (!value || typeof value !== "object") {
    return { status: "failure", message: "mold row is invalid." };
  }
  const row = value as Record<string, unknown>;
  const id = parseString(row.id, "id");
  if (id.status === "failure") return id;
  const code = typeof row.code === "string" ? row.code : "";
  const name = parseString(row.name, "name");
  if (name.status === "failure") return name;
  const lifecycleState = parseString(row.lifecycleState, "lifecycleState");
  if (lifecycleState.status === "failure") return lifecycleState;
  const followUpStatus = parseFollowUpStatus(row.followUpStatus);
  if (followUpStatus.status === "failure") return followUpStatus;
  const updatedAtUtc = parseString(row.updatedAtUtc, "updatedAtUtc");
  if (updatedAtUtc.status === "failure") return updatedAtUtc;
  return {
    status: "success",
    value: {
      id: id.value,
      code,
      name: name.value,
      lifecycleState: lifecycleState.value,
      followUpStatus: followUpStatus.value,
      currentShotCount: parseNumber(row.currentShotCount),
      nominalShotLife: parseNumber(row.nominalShotLife),
      counterRecordedAtUtc: parseOptionalString(row.counterRecordedAtUtc),
      primaryProductReference: parseOptionalString(row.primaryProductReference),
      updatedAtUtc: updatedAtUtc.value,
      linkedAiTaskId: parseOptionalString(row.linkedAiTaskId),
      canCreateTaskDraft: parseBoolean(row.canCreateTaskDraft),
      taskDraftPurpose: parseTaskPurpose(row.taskDraftPurpose),
      hasInsufficientEvidence: parseBoolean(row.hasInsufficientEvidence),
    },
  };
}

export function parseMoldFollowUpWorkspacePayload(value: unknown): ParseResult<MoldFollowUpWorkspacePayload> {
  if (!value || typeof value !== "object") {
    return { status: "failure", message: "workspace payload is invalid." };
  }
  const payload = value as Record<string, unknown>;
  const automationDisclaimer =
    typeof payload.automationDisclaimer === "string" ? payload.automationDisclaimer : "";
  const kpisRaw = payload.kpis;
  const capabilitiesRaw = payload.capabilities;
  const moldsRaw = payload.molds;
  if (!kpisRaw || typeof kpisRaw !== "object" || !capabilitiesRaw || typeof capabilitiesRaw !== "object") {
    return { status: "failure", message: "workspace kpis/capabilities missing." };
  }
  if (!Array.isArray(moldsRaw)) {
    return { status: "failure", message: "workspace molds missing." };
  }
  const kpis = kpisRaw as Record<string, unknown>;
  const capabilities = capabilitiesRaw as Record<string, unknown>;
  const molds: MoldFollowUpRow[] = [];
  for (const row of moldsRaw) {
    const parsed = parseRow(row);
    if (parsed.status === "failure") return parsed;
    molds.push(parsed.value);
  }
  return {
    status: "success",
    value: {
      automationDisclaimer,
      kpis: {
        requiresReviewCount: parseNumber(kpis.requiresReviewCount) ?? 0,
        acknowledgedCount: parseNumber(kpis.acknowledgedCount) ?? 0,
        taskLinkedCount: parseNumber(kpis.taskLinkedCount) ?? 0,
        maintenanceStateCount: parseNumber(kpis.maintenanceStateCount) ?? 0,
        dataReviewCount: parseNumber(kpis.dataReviewCount) ?? 0,
      },
      capabilities: {
        canRead: parseBoolean(capabilities.canRead),
        canManage: parseBoolean(capabilities.canManage),
        canCreateTask: parseBoolean(capabilities.canCreateTask),
      },
      molds,
    },
  };
}

export function parseMoldFollowUpDetail(value: unknown): ParseResult<MoldFollowUpDetail> {
  if (!value || typeof value !== "object") {
    return { status: "failure", message: "mold detail is invalid." };
  }
  const row = value as Record<string, unknown>;
  const id = parseString(row.id, "id");
  if (id.status === "failure") return id;
  const code = typeof row.code === "string" ? row.code : "";
  const name = parseString(row.name, "name");
  if (name.status === "failure") return name;
  const lifecycleState = parseString(row.lifecycleState, "lifecycleState");
  if (lifecycleState.status === "failure") return lifecycleState;
  const ownershipType = parseString(row.ownershipType, "ownershipType");
  if (ownershipType.status === "failure") return ownershipType;
  const locationType = parseString(row.locationType, "locationType");
  if (locationType.status === "failure") return locationType;
  const followUpStatus = parseFollowUpStatus(row.followUpStatus);
  if (followUpStatus.status === "failure") return followUpStatus;
  const updatedAtUtc = parseString(row.updatedAtUtc, "updatedAtUtc");
  if (updatedAtUtc.status === "failure") return updatedAtUtc;
  const version = parseNumber(row.version);
  if (version === undefined) {
    return { status: "failure", message: "version is required." };
  }
  const evidenceSummary = typeof row.evidenceSummary === "string" ? row.evidenceSummary : "";
  const lifecycleEvents: MoldFollowUpLifecycleEvent[] = [];
  if (Array.isArray(row.lifecycleEvents)) {
    for (const event of row.lifecycleEvents) {
      const parsed = parseLifecycleEvent(event);
      if (parsed.status === "failure") return parsed;
      lifecycleEvents.push(parsed.value);
    }
  }
  const auditEvents: MoldFollowUpAuditEvent[] = [];
  if (Array.isArray(row.auditEvents)) {
    for (const event of row.auditEvents) {
      const parsed = parseAuditEvent(event);
      if (parsed.status === "failure") return parsed;
      auditEvents.push(parsed.value);
    }
  }
  return {
    status: "success",
    value: {
      id: id.value,
      code,
      name: name.value,
      description: parseOptionalString(row.description),
      lifecycleState: lifecycleState.value,
      ownershipType: ownershipType.value,
      locationType: locationType.value,
      locationLabel: parseOptionalString(row.locationLabel),
      cavityCount: parseNumber(row.cavityCount),
      nominalShotLife: parseNumber(row.nominalShotLife),
      currentShotCount: parseNumber(row.currentShotCount),
      counterRecordedAtUtc: parseOptionalString(row.counterRecordedAtUtc),
      counterSource: parseOptionalString(row.counterSource),
      primaryProductReference: parseOptionalString(row.primaryProductReference),
      materialFamilyReference: parseOptionalString(row.materialFamilyReference),
      manufacturer: parseOptionalString(row.manufacturer),
      updatedAtUtc: updatedAtUtc.value,
      version,
      followUpStatus: followUpStatus.value,
      linkedAiTaskId: parseOptionalString(row.linkedAiTaskId),
      canCreateTaskDraft: parseBoolean(row.canCreateTaskDraft),
      taskDraftPurpose: parseTaskPurpose(row.taskDraftPurpose),
      hasInsufficientEvidence: parseBoolean(row.hasInsufficientEvidence),
      evidenceSummary,
      lifecycleEvents,
      auditEvents,
    },
  };
}

export function parseMoldFollowUpTaskDraftResponse(value: unknown): ParseResult<Readonly<{
  taskId: string;
  moldId: string;
  idempotentReplay: boolean;
  mold: MoldFollowUpDetail;
}>> {
  if (!value || typeof value !== "object") {
    return { status: "failure", message: "task draft response is invalid." };
  }
  const row = value as Record<string, unknown>;
  const taskId = parseString(row.taskId, "taskId");
  if (taskId.status === "failure") return taskId;
  const moldId = parseString(row.moldId, "moldId");
  if (moldId.status === "failure") return moldId;
  const mold = parseMoldFollowUpDetail(row.mold);
  if (mold.status === "failure") return mold;
  return {
    status: "success",
    value: {
      taskId: taskId.value,
      moldId: moldId.value,
      idempotentReplay: parseBoolean(row.idempotentReplay),
      mold: mold.value,
    },
  };
}
