import type {
  ShiftAssignmentSummary,
  OperatorAssignmentSummary,
  OperatorAssignmentRole,
  ShiftHandoverSummary,
  ShiftExecutionSummary,
  OperatorExecutionSummary,
} from "../domain/production-execution-resources";

type ParseResult<T> =
  | { status: "success"; value: T }
  | { status: "failure"; message: string };

function str(v: unknown): string | undefined {
  return typeof v === "string" ? v : undefined;
}

function num(v: unknown): number | undefined {
  return typeof v === "number" ? v : undefined;
}

function bool(v: unknown): boolean | undefined {
  return typeof v === "boolean" ? v : undefined;
}

const VALID_ROLES = new Set<string>(["primary-operator", "operator", "supervisor"]);

// ── Shift Assignment ────────────────────────────────────────────────────────────

function parseShiftAssignment(item: unknown): ParseResult<ShiftAssignmentSummary> {
  if (typeof item !== "object" || item === null) {
    return { status: "failure", message: "Expected shift assignment object." };
  }
  const r = item as Record<string, unknown>;
  const id = str(r["id"]);
  const productionShiftId = str(r["productionShiftId"]);
  const shiftCode = str(r["shiftCode"]);
  const shiftDate = str(r["shiftDate"]);
  const shiftStartAtUtc = str(r["shiftStartAtUtc"]);
  const shiftEndAtUtc = str(r["shiftEndAtUtc"]);
  const assignedAtUtc = str(r["assignedAtUtc"]);
  const assignedBySubjectId = str(r["assignedBySubjectId"]);
  const isActive = bool(r["isActive"]);

  if (!id || !productionShiftId || !shiftCode || !shiftDate || !shiftStartAtUtc || !shiftEndAtUtc || !assignedAtUtc || !assignedBySubjectId || isActive == null) {
    return { status: "failure", message: "Missing required shift assignment fields." };
  }

  return {
    status: "success",
    value: {
      id, productionShiftId, shiftCode, shiftDate,
      shiftStartAtUtc, shiftEndAtUtc, assignedAtUtc, assignedBySubjectId,
      releasedAtUtc: str(r["releasedAtUtc"]),
      releasedBySubjectId: str(r["releasedBySubjectId"]),
      releaseReason: str(r["releaseReason"]),
      isActive,
    },
  };
}

export function parseShiftAssignmentListApiResponse(
  body: unknown,
): ParseResult<ShiftAssignmentSummary[]> {
  if (!Array.isArray(body)) {
    return { status: "failure", message: "Expected array for shift assignments." };
  }
  const results: ShiftAssignmentSummary[] = [];
  for (const item of body) {
    const parsed = parseShiftAssignment(item);
    if (parsed.status === "failure") return parsed;
    results.push(parsed.value);
  }
  return { status: "success", value: results };
}

// ── Operator Assignment ─────────────────────────────────────────────────────────

function parseOperatorAssignment(item: unknown): ParseResult<OperatorAssignmentSummary> {
  if (typeof item !== "object" || item === null) {
    return { status: "failure", message: "Expected operator assignment object." };
  }
  const r = item as Record<string, unknown>;
  const id = str(r["id"]);
  const subjectId = str(r["subjectId"]);
  const assignmentRole = str(r["assignmentRole"]);
  const assignedAtUtc = str(r["assignedAtUtc"]);
  const assignedBySubjectId = str(r["assignedBySubjectId"]);
  const isActive = bool(r["isActive"]);

  if (!id || !subjectId || !assignmentRole || !assignedAtUtc || !assignedBySubjectId || isActive == null) {
    return { status: "failure", message: "Missing required operator assignment fields." };
  }

  if (!VALID_ROLES.has(assignmentRole)) {
    return { status: "failure", message: `Unknown operator role: ${assignmentRole}` };
  }

  return {
    status: "success",
    value: {
      id, subjectId,
      assignmentRole: assignmentRole as OperatorAssignmentRole,
      assignedAtUtc, assignedBySubjectId,
      releasedAtUtc: str(r["releasedAtUtc"]),
      releasedBySubjectId: str(r["releasedBySubjectId"]),
      releaseReason: str(r["releaseReason"]),
      isActive,
    },
  };
}

export function parseOperatorAssignmentListApiResponse(
  body: unknown,
): ParseResult<OperatorAssignmentSummary[]> {
  if (!Array.isArray(body)) {
    return { status: "failure", message: "Expected array for operator assignments." };
  }
  const results: OperatorAssignmentSummary[] = [];
  for (const item of body) {
    const parsed = parseOperatorAssignment(item);
    if (parsed.status === "failure") return parsed;
    results.push(parsed.value);
  }
  return { status: "success", value: results };
}

// ── Shift Handover ──────────────────────────────────────────────────────────────

function parseHandover(item: unknown): ParseResult<ShiftHandoverSummary> {
  if (typeof item !== "object" || item === null) {
    return { status: "failure", message: "Expected handover object." };
  }
  const r = item as Record<string, unknown>;
  const id = str(r["id"]);
  const fromShiftAssignmentId = str(r["fromShiftAssignmentId"]);
  const note = str(r["note"]);
  const createdAtUtc = str(r["createdAtUtc"]);
  const createdBySubjectId = str(r["createdBySubjectId"]);

  if (!id || !fromShiftAssignmentId || !note || !createdAtUtc || !createdBySubjectId) {
    return { status: "failure", message: "Missing required handover fields." };
  }

  return {
    status: "success",
    value: {
      id, fromShiftAssignmentId,
      toShiftAssignmentId: str(r["toShiftAssignmentId"]),
      note,
      openIssuesSummary: str(r["openIssuesSummary"]),
      createdAtUtc, createdBySubjectId,
    },
  };
}

export function parseHandoverListApiResponse(
  body: unknown,
): ParseResult<ShiftHandoverSummary[]> {
  if (!Array.isArray(body)) {
    return { status: "failure", message: "Expected array for handovers." };
  }
  const results: ShiftHandoverSummary[] = [];
  for (const item of body) {
    const parsed = parseHandover(item);
    if (parsed.status === "failure") return parsed;
    results.push(parsed.value);
  }
  return { status: "success", value: results };
}

// ── Shift Execution Summary ─────────────────────────────────────────────────────

export function parseShiftExecutionSummaryApiResponse(
  body: unknown,
): ParseResult<ShiftExecutionSummary> {
  if (typeof body !== "object" || body === null) {
    return { status: "failure", message: "Expected shift execution summary object." };
  }
  const r = body as Record<string, unknown>;
  const shiftId = str(r["shiftId"]);
  const shiftCode = str(r["shiftCode"]);
  const shiftStartAtUtc = str(r["shiftStartAtUtc"]);
  const shiftEndAtUtc = str(r["shiftEndAtUtc"]);
  const activeOrderCount = num(r["activeOrderCount"]);
  const completedOrderCount = num(r["completedOrderCount"]);
  const goodQuantity = num(r["goodQuantity"]);
  const scrapQuantity = num(r["scrapQuantity"]);
  const downtimeSeconds = num(r["downtimeSeconds"]);
  const setupSeconds = num(r["setupSeconds"]);
  const manualCycleCount = num(r["manualCycleCount"]);
  const openExceptionCount = num(r["openExceptionCount"]);
  const criticalExceptionCount = num(r["criticalExceptionCount"]);

  if (!shiftId || !shiftCode || !shiftStartAtUtc || !shiftEndAtUtc
    || activeOrderCount == null || completedOrderCount == null
    || goodQuantity == null || scrapQuantity == null || downtimeSeconds == null
    || setupSeconds == null || manualCycleCount == null
    || openExceptionCount == null || criticalExceptionCount == null) {
    return { status: "failure", message: "Missing required shift execution summary fields." };
  }

  return {
    status: "success",
    value: {
      shiftId, shiftCode, shiftStartAtUtc, shiftEndAtUtc,
      activeOrderCount, completedOrderCount,
      goodQuantity, scrapQuantity, downtimeSeconds, setupSeconds,
      manualCycleCount, openExceptionCount, criticalExceptionCount,
    },
  };
}

// ── Operator Execution Summary ──────────────────────────────────────────────────

export function parseOperatorExecutionSummaryApiResponse(
  body: unknown,
): ParseResult<OperatorExecutionSummary> {
  if (typeof body !== "object" || body === null) {
    return { status: "failure", message: "Expected operator execution summary object." };
  }
  const r = body as Record<string, unknown>;
  const subjectId = str(r["subjectId"]);
  const activeOrderCount = num(r["activeOrderCount"]);
  const completedOrderCount = num(r["completedOrderCount"]);
  const goodQuantity = num(r["goodQuantity"]);
  const scrapQuantity = num(r["scrapQuantity"]);
  const assignedExceptionCount = num(r["assignedExceptionCount"]);

  if (!subjectId || activeOrderCount == null || completedOrderCount == null
    || goodQuantity == null || scrapQuantity == null || assignedExceptionCount == null) {
    return { status: "failure", message: "Missing required operator execution summary fields." };
  }

  return {
    status: "success",
    value: {
      subjectId, activeOrderCount, completedOrderCount,
      goodQuantity, scrapQuantity, assignedExceptionCount,
      lastActivityAtUtc: str(r["lastActivityAtUtc"]),
    },
  };
}
