import "./production-order-server-boundary";

import type {
  ProductionOrder,
  ProductionOrderStatus,
  ProductionOrderSummary,
} from "../domain/production-order";

const PRODUCTION_ORDER_STATUSES = new Set<ProductionOrderStatus>([
  "ready",
  "in-progress",
  "paused",
  "completed",
  "cancelled",
]);

function isValidStatus(value: unknown): value is ProductionOrderStatus {
  return typeof value === "string" && PRODUCTION_ORDER_STATUSES.has(value as ProductionOrderStatus);
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

export type ParseResult<T> =
  | Readonly<{ status: "success"; value: T }>
  | Readonly<{ status: "failure"; message: string }>;

function fail<T>(message: string): ParseResult<T> {
  return { status: "failure", message };
}

function ok<T>(value: T): ParseResult<T> {
  return { status: "success", value };
}

export function parseProductionOrderSummaryApiResponse(
  raw: unknown,
): ParseResult<ProductionOrderSummary> {
  if (typeof raw !== "object" || raw === null) {
    return fail("Expected an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isString(o.id)) return fail("id is required.");
  if (!isString(o.facilityId)) return fail("facilityId is required.");
  if (!isString(o.sourceProductionPlanId)) return fail("sourceProductionPlanId is required.");
  if (!isString(o.referenceCode)) return fail("referenceCode is required.");
  if (!isString(o.title)) return fail("title is required.");
  if (!isValidStatus(o.status)) return fail(`status has an invalid value: '${String(o.status)}'.`);
  if (!isString(o.plannedStartAtUtc)) return fail("plannedStartAtUtc is required.");
  if (!isString(o.plannedEndAtUtc)) return fail("plannedEndAtUtc is required.");
  if (!isNumber(o.version)) return fail("version is required.");
  if (!isString(o.updatedAtUtc)) return fail("updatedAtUtc is required.");

  return ok<ProductionOrderSummary>({
    id: o.id,
    facilityId: o.facilityId,
    sourceProductionPlanId: o.sourceProductionPlanId,
    referenceCode: o.referenceCode,
    title: o.title,
    status: o.status,
    plannedStartAtUtc: o.plannedStartAtUtc,
    plannedEndAtUtc: o.plannedEndAtUtc,
    actualStartAtUtc: isOptionalString(o.actualStartAtUtc) ? o.actualStartAtUtc ?? undefined : undefined,
    machineReference: isOptionalString(o.machineReference) ? o.machineReference ?? undefined : undefined,
    moldReference: isOptionalString(o.moldReference) ? o.moldReference ?? undefined : undefined,
    shiftName: isOptionalString(o.shiftName) ? o.shiftName ?? undefined : undefined,
    version: o.version,
    updatedAtUtc: o.updatedAtUtc,
  });
}

export function parseProductionOrderListApiResponse(
  raw: unknown,
): ParseResult<ProductionOrderSummary[]> {
  if (!Array.isArray(raw)) {
    return fail("Expected an array.");
  }

  const items: ProductionOrderSummary[] = [];

  for (let i = 0; i < raw.length; i++) {
    const parsed = parseProductionOrderSummaryApiResponse(raw[i]);

    if (parsed.status === "failure") {
      return fail(`Item ${i}: ${parsed.message}`);
    }

    items.push(parsed.value);
  }

  return ok(items);
}

export function parseProductionOrderDetailApiResponse(
  raw: unknown,
): ParseResult<ProductionOrder> {
  if (typeof raw !== "object" || raw === null) {
    return fail("Expected an object.");
  }

  const o = raw as Record<string, unknown>;

  if (!isString(o.id)) return fail("id is required.");
  if (!isString(o.facilityId)) return fail("facilityId is required.");
  if (!isString(o.productionPlanHandoffId)) return fail("productionPlanHandoffId is required.");
  if (!isString(o.sourceProductionPlanId)) return fail("sourceProductionPlanId is required.");
  if (!isNumber(o.sourceProductionPlanVersion)) return fail("sourceProductionPlanVersion is required.");
  if (!isString(o.referenceCode)) return fail("referenceCode is required.");
  if (!isString(o.title)) return fail("title is required.");
  if (!isValidStatus(o.status)) return fail(`status has an invalid value: '${String(o.status)}'.`);
  if (!isString(o.plannedStartAtUtc)) return fail("plannedStartAtUtc is required.");
  if (!isString(o.plannedEndAtUtc)) return fail("plannedEndAtUtc is required.");
  if (!isString(o.createdBySubjectId)) return fail("createdBySubjectId is required.");
  if (!isString(o.lastModifiedBySubjectId)) return fail("lastModifiedBySubjectId is required.");
  if (!isNumber(o.version)) return fail("version is required.");
  if (!isString(o.createdAtUtc)) return fail("createdAtUtc is required.");
  if (!isString(o.updatedAtUtc)) return fail("updatedAtUtc is required.");

  return ok<ProductionOrder>({
    id: o.id,
    facilityId: o.facilityId,
    productionPlanHandoffId: o.productionPlanHandoffId,
    sourceProductionPlanId: o.sourceProductionPlanId,
    sourceProductionPlanVersion: o.sourceProductionPlanVersion,
    referenceCode: o.referenceCode,
    title: o.title,
    status: o.status,
    plannedStartAtUtc: o.plannedStartAtUtc,
    plannedEndAtUtc: o.plannedEndAtUtc,
    actualStartAtUtc: isOptionalString(o.actualStartAtUtc) ? o.actualStartAtUtc ?? undefined : undefined,
    actualEndAtUtc: isOptionalString(o.actualEndAtUtc) ? o.actualEndAtUtc ?? undefined : undefined,
    shiftName: isOptionalString(o.shiftName) ? o.shiftName ?? undefined : undefined,
    machineReference: isOptionalString(o.machineReference) ? o.machineReference ?? undefined : undefined,
    moldReference: isOptionalString(o.moldReference) ? o.moldReference ?? undefined : undefined,
    productReference: isOptionalString(o.productReference) ? o.productReference ?? undefined : undefined,
    createdBySubjectId: o.createdBySubjectId,
    lastModifiedBySubjectId: o.lastModifiedBySubjectId,
    version: o.version,
    createdAtUtc: o.createdAtUtc,
    updatedAtUtc: o.updatedAtUtc,
  });
}
