import type {
  ProductionExceptionType,
  ProductionExceptionSeverity,
  ProductionExceptionStatus,
  ProductionExecutionExceptionSummary,
  ProductionExecutionHealthSummary,
} from "../domain/production-execution-exceptions";

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

const VALID_EXCEPTION_TYPES = new Set([
  "stale-downtime",
  "stale-setup",
  "quantity-anomaly",
  "cycle-anomaly",
  "schedule-overrun",
  "handoff-inconsistency",
  "operator-attention",
  "other",
]);

const VALID_SEVERITIES = new Set(["info", "warning", "critical"]);
const VALID_STATUSES = new Set(["open", "acknowledged", "resolved"]);

function parseExceptionSummary(
  item: unknown,
): ParseResult<ProductionExecutionExceptionSummary> {
  if (typeof item !== "object" || item === null) {
    return { status: "failure", message: "Expected exception object." };
  }

  const r = item as Record<string, unknown>;
  const id = str(r["id"]);
  const exceptionType = str(r["exceptionType"]);
  const severity = str(r["severity"]);
  const status = str(r["status"]);
  const message = str(r["message"]);
  const raisedAtUtc = str(r["raisedAtUtc"]);
  const raisedBySubjectId = str(r["raisedBySubjectId"]);
  const version = num(r["version"]);

  if (!id || !exceptionType || !severity || !status || !message || !raisedAtUtc || !raisedBySubjectId || version == null) {
    return { status: "failure", message: "Missing required exception fields." };
  }

  if (!VALID_EXCEPTION_TYPES.has(exceptionType)) {
    return { status: "failure", message: `Unknown exception type: ${exceptionType}` };
  }

  if (!VALID_SEVERITIES.has(severity)) {
    return { status: "failure", message: `Unknown severity: ${severity}` };
  }

  if (!VALID_STATUSES.has(status)) {
    return { status: "failure", message: `Unknown status: ${status}` };
  }

  return {
    status: "success",
    value: {
      id,
      exceptionType: exceptionType as ProductionExceptionType,
      severity: severity as ProductionExceptionSeverity,
      status: status as ProductionExceptionStatus,
      message,
      raisedAtUtc,
      raisedBySubjectId,
      resolvedAtUtc: str(r["resolvedAtUtc"]),
      resolvedBySubjectId: str(r["resolvedBySubjectId"]),
      resolutionNote: str(r["resolutionNote"]),
      version,
    },
  };
}

export function parseExceptionListApiResponse(
  body: unknown,
): ParseResult<ProductionExecutionExceptionSummary[]> {
  if (!Array.isArray(body)) {
    return { status: "failure", message: "Expected an array for exceptions list." };
  }

  const results: ProductionExecutionExceptionSummary[] = [];
  for (const item of body) {
    const parsed = parseExceptionSummary(item);
    if (parsed.status === "failure") return parsed;
    results.push(parsed.value);
  }

  return { status: "success", value: results };
}

export function parseHealthSummaryApiResponse(
  body: unknown,
): ParseResult<ProductionExecutionHealthSummary> {
  if (typeof body !== "object" || body === null) {
    return { status: "failure", message: "Expected health summary object." };
  }

  const r = body as Record<string, unknown>;
  const status = str(r["status"]);
  const openExceptionCount = num(r["openExceptionCount"]);
  const criticalExceptionCount = num(r["criticalExceptionCount"]);
  const hasActiveDowntime = bool(r["hasActiveDowntime"]);
  const downtimeIsStale = bool(r["downtimeIsStale"]);
  const hasActiveSetup = bool(r["hasActiveSetup"]);
  const setupIsStale = bool(r["setupIsStale"]);
  const scheduleOverrun = bool(r["scheduleOverrun"]);
  const requiresAttention = bool(r["requiresAttention"]);

  if (
    !status ||
    openExceptionCount == null ||
    criticalExceptionCount == null ||
    hasActiveDowntime == null ||
    downtimeIsStale == null ||
    hasActiveSetup == null ||
    setupIsStale == null ||
    scheduleOverrun == null ||
    requiresAttention == null
  ) {
    return { status: "failure", message: "Missing required health summary fields." };
  }

  const validStatuses = new Set(["normal", "attention", "critical"]);
  if (!validStatuses.has(status)) {
    return { status: "failure", message: `Unknown health status: ${status}` };
  }

  return {
    status: "success",
    value: {
      status: status as ProductionExecutionHealthSummary["status"],
      openExceptionCount,
      criticalExceptionCount,
      hasActiveDowntime,
      downtimeIsStale,
      hasActiveSetup,
      setupIsStale,
      quantityProgress: num(r["quantityProgress"]),
      scheduleOverrun,
      lastOperationalActivityAtUtc: str(r["lastOperationalActivityAtUtc"]),
      requiresAttention,
    },
  };
}
