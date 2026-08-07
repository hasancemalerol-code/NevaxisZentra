import "./production-order-server-boundary";

import type {
  ProductionDowntimeSummary,
  ProductionSetupSessionSummary,
  ProductionCycleEntrySummary,
  ProductionRuntimeSummary,
} from "../domain/production-execution-operations";

export type ParseResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | Readonly<{ status: "failure"; message: string }>;

function fail<T>(message: string): ParseResult<T> {
  return { status: "failure", message };
}

function ok<T>(value: T): ParseResult<T> {
  return { status: "success", value };
}

function isString(v: unknown): v is string {
  return typeof v === "string";
}

function isOptionalString(v: unknown): v is string | undefined {
  return v === undefined || v === null || typeof v === "string";
}

function isNumber(v: unknown): v is number {
  return typeof v === "number" && Number.isFinite(v);
}

function isOptionalNumber(v: unknown): v is number | undefined {
  return v === undefined || v === null || (typeof v === "number" && Number.isFinite(v));
}

export function parseDowntimeSummary(raw: unknown): ParseResult<ProductionDowntimeSummary> {
  if (typeof raw !== "object" || raw === null) return fail("Expected an object.");
  const o = raw as Record<string, unknown>;
  if (!isString(o.id)) return fail("id is required.");
  if (!isString(o.reasonCode)) return fail("reasonCode is required.");
  if (!isString(o.category)) return fail("category is required.");
  if (!isString(o.startedAtUtc)) return fail("startedAtUtc is required.");
  return ok<ProductionDowntimeSummary>({
    id: o.id,
    reasonCode: o.reasonCode,
    category: o.category,
    startedAtUtc: o.startedAtUtc,
    endedAtUtc: isOptionalString(o.endedAtUtc) ? o.endedAtUtc ?? undefined : undefined,
    durationSeconds: isOptionalNumber(o.durationSeconds) ? (o.durationSeconds as number | undefined) ?? undefined : undefined,
    comment: isOptionalString(o.comment) ? o.comment ?? undefined : undefined,
  });
}

export function parseDowntimeListApiResponse(raw: unknown): ParseResult<ProductionDowntimeSummary[]> {
  if (!Array.isArray(raw)) return fail("Expected an array.");
  const items: ProductionDowntimeSummary[] = [];
  for (let i = 0; i < raw.length; i++) {
    const parsed = parseDowntimeSummary(raw[i]);
    if (parsed.status === "failure") return fail(`Item ${i}: ${parsed.message}`);
    items.push(parsed.value);
  }
  return ok(items);
}

export function parseSetupSessionSummary(raw: unknown): ParseResult<ProductionSetupSessionSummary> {
  if (typeof raw !== "object" || raw === null) return fail("Expected an object.");
  const o = raw as Record<string, unknown>;
  if (!isString(o.id)) return fail("id is required.");
  if (!isString(o.setupType)) return fail("setupType is required.");
  if (!isString(o.startedAtUtc)) return fail("startedAtUtc is required.");
  return ok<ProductionSetupSessionSummary>({
    id: o.id,
    setupType: o.setupType,
    startedAtUtc: o.startedAtUtc,
    completedAtUtc: isOptionalString(o.completedAtUtc) ? o.completedAtUtc ?? undefined : undefined,
    durationSeconds: isOptionalNumber(o.durationSeconds) ? (o.durationSeconds as number | undefined) ?? undefined : undefined,
    comment: isOptionalString(o.comment) ? o.comment ?? undefined : undefined,
  });
}

export function parseSetupSessionListApiResponse(raw: unknown): ParseResult<ProductionSetupSessionSummary[]> {
  if (!Array.isArray(raw)) return fail("Expected an array.");
  const items: ProductionSetupSessionSummary[] = [];
  for (let i = 0; i < raw.length; i++) {
    const parsed = parseSetupSessionSummary(raw[i]);
    if (parsed.status === "failure") return fail(`Item ${i}: ${parsed.message}`);
    items.push(parsed.value);
  }
  return ok(items);
}

export function parseCycleEntrySummary(raw: unknown): ParseResult<ProductionCycleEntrySummary> {
  if (typeof raw !== "object" || raw === null) return fail("Expected an object.");
  const o = raw as Record<string, unknown>;
  if (!isString(o.id)) return fail("id is required.");
  if (!isNumber(o.cycleNumber)) return fail("cycleNumber is required.");
  if (!isNumber(o.cycleDurationMilliseconds)) return fail("cycleDurationMilliseconds is required.");
  if (!isString(o.recordedAtUtc)) return fail("recordedAtUtc is required.");
  if (!isString(o.source)) return fail("source is required.");
  return ok<ProductionCycleEntrySummary>({
    id: o.id,
    cycleNumber: o.cycleNumber,
    cycleDurationMilliseconds: o.cycleDurationMilliseconds,
    quantityProduced: isOptionalNumber(o.quantityProduced) ? (o.quantityProduced as number | undefined) ?? undefined : undefined,
    recordedAtUtc: o.recordedAtUtc,
    source: o.source,
    comment: isOptionalString(o.comment) ? o.comment ?? undefined : undefined,
  });
}

export function parseCycleEntryListApiResponse(raw: unknown): ParseResult<ProductionCycleEntrySummary[]> {
  if (!Array.isArray(raw)) return fail("Expected an array.");
  const items: ProductionCycleEntrySummary[] = [];
  for (let i = 0; i < raw.length; i++) {
    const parsed = parseCycleEntrySummary(raw[i]);
    if (parsed.status === "failure") return fail(`Item ${i}: ${parsed.message}`);
    items.push(parsed.value);
  }
  return ok(items);
}

export function parseRuntimeSummaryApiResponse(raw: unknown): ParseResult<ProductionRuntimeSummary> {
  if (typeof raw !== "object" || raw === null) return fail("Expected an object.");
  const o = raw as Record<string, unknown>;
  if (!isNumber(o.currentElapsedSeconds)) return fail("currentElapsedSeconds is required.");
  if (!isNumber(o.totalDowntimeSeconds)) return fail("totalDowntimeSeconds is required.");
  if (!isNumber(o.totalSetupSeconds)) return fail("totalSetupSeconds is required.");
  if (!isNumber(o.netRunningSeconds)) return fail("netRunningSeconds is required.");
  if (!isNumber(o.manualCycleCount)) return fail("manualCycleCount is required.");

  let activeDowntime: ProductionDowntimeSummary | undefined;
  if (o.activeDowntime !== null && o.activeDowntime !== undefined) {
    const parsed = parseDowntimeSummary(o.activeDowntime);
    if (parsed.status === "failure") return fail(`activeDowntime: ${parsed.message}`);
    activeDowntime = parsed.value;
  }

  let activeSetup: ProductionSetupSessionSummary | undefined;
  if (o.activeSetup !== null && o.activeSetup !== undefined) {
    const parsed = parseSetupSessionSummary(o.activeSetup);
    if (parsed.status === "failure") return fail(`activeSetup: ${parsed.message}`);
    activeSetup = parsed.value;
  }

  return ok<ProductionRuntimeSummary>({
    actualStartAtUtc: isOptionalString(o.actualStartAtUtc) ? o.actualStartAtUtc ?? undefined : undefined,
    currentElapsedSeconds: o.currentElapsedSeconds,
    totalDowntimeSeconds: o.totalDowntimeSeconds,
    totalSetupSeconds: o.totalSetupSeconds,
    netRunningSeconds: o.netRunningSeconds,
    manualCycleCount: o.manualCycleCount,
    averageManualCycleMilliseconds: isOptionalNumber(o.averageManualCycleMilliseconds) ? (o.averageManualCycleMilliseconds as number | undefined) ?? undefined : undefined,
    lastCycleAtUtc: isOptionalString(o.lastCycleAtUtc) ? o.lastCycleAtUtc ?? undefined : undefined,
    activeDowntime,
    activeSetup,
  });
}
