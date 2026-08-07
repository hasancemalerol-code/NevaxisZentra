import "./production-order-server-boundary";

import type {
  ProductionExecutionEntry,
  ProductionExecutionEntryType,
} from "../domain/production-execution-entry";

const ENTRY_TYPES = new Set<ProductionExecutionEntryType>([
  "good-production",
  "scrap",
  "reversal",
]);

function isValidEntryType(value: unknown): value is ProductionExecutionEntryType {
  return typeof value === "string" && ENTRY_TYPES.has(value as ProductionExecutionEntryType);
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

function isBoolean(v: unknown): v is boolean {
  return typeof v === "boolean";
}

export type ParseResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | Readonly<{ status: "failure"; message: string }>;

function fail<T>(message: string): ParseResult<T> {
  return { status: "failure", message };
}

function ok<T>(value: T): ParseResult<T> {
  return { status: "success", value };
}

export function parseProductionExecutionEntryApiResponse(
  raw: unknown,
): ParseResult<ProductionExecutionEntry> {
  if (typeof raw !== "object" || raw === null) {
    return fail("Expected an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isString(o.id)) return fail("id is required.");
  if (!isValidEntryType(o.entryType)) return fail(`entryType is invalid: '${String(o.entryType)}'.`);
  if (!isNumber(o.quantity)) return fail("quantity is required.");
  if (!isString(o.unit)) return fail("unit is required.");
  if (!isString(o.recordedAtUtc)) return fail("recordedAtUtc is required.");
  if (!isString(o.recordedBySubjectId)) return fail("recordedBySubjectId is required.");
  if (!isBoolean(o.isReversed)) return fail("isReversed is required.");

  return ok<ProductionExecutionEntry>({
    id: o.id,
    entryType: o.entryType,
    quantity: o.quantity,
    unit: o.unit,
    reasonCode: isOptionalString(o.reasonCode) ? o.reasonCode ?? undefined : undefined,
    comment: isOptionalString(o.comment) ? o.comment ?? undefined : undefined,
    recordedAtUtc: o.recordedAtUtc,
    recordedBySubjectId: o.recordedBySubjectId,
    isReversed: o.isReversed,
    reversesEntryId: isOptionalString(o.reversesEntryId) ? o.reversesEntryId ?? undefined : undefined,
  });
}

export function parseProductionExecutionEntryListApiResponse(
  raw: unknown,
): ParseResult<ProductionExecutionEntry[]> {
  if (!Array.isArray(raw)) {
    return fail("Expected an array.");
  }

  const items: ProductionExecutionEntry[] = [];

  for (let i = 0; i < raw.length; i++) {
    const parsed = parseProductionExecutionEntryApiResponse(raw[i]);

    if (parsed.status === "failure") {
      return fail(`Item ${i}: ${parsed.message}`);
    }

    items.push(parsed.value);
  }

  return ok(items);
}
