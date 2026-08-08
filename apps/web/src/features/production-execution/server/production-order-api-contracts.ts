import "./production-order-server-boundary";

import type {
  ProductionOrder,
  ProductionOrderStatus,
  ProductionOrderSummary,
  ProductionQuantitySummary,
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

function isOptionalNumber(v: unknown): v is number | undefined {
  return v === undefined || v === null || (typeof v === "number" && Number.isFinite(v));
}

function parseQuantitySummary(raw: unknown): ParseResult<ProductionQuantitySummary> {
  if (typeof raw !== "object" || raw === null) {
    return fail("quantitySummary must be an object.");
  }
  const o = raw as Record<string, unknown>;
  if (!isOptionalNumber(o.plannedQuantity)) return fail("quantitySummary.plannedQuantity must be a number.");
  if (!isNumber(o.goodQuantity)) return fail("quantitySummary.goodQuantity is required.");
  if (!isNumber(o.scrapQuantity)) return fail("quantitySummary.scrapQuantity is required.");
  if (!isNumber(o.grossProducedQuantity)) return fail("quantitySummary.grossProducedQuantity is required.");
  return ok<ProductionQuantitySummary>({
    plannedQuantity: isOptionalNumber(o.plannedQuantity) ? (o.plannedQuantity as number | undefined) ?? undefined : undefined,
    plannedQuantityUnit: isOptionalString(o.plannedQuantityUnit) ? o.plannedQuantityUnit ?? undefined : undefined,
    goodQuantity: o.goodQuantity as number,
    scrapQuantity: o.scrapQuantity as number,
    grossProducedQuantity: o.grossProducedQuantity as number,
    remainingQuantity: isOptionalNumber(o.remainingQuantity) ? (o.remainingQuantity as number | undefined) ?? undefined : undefined,
    completionPercentage: isOptionalNumber(o.completionPercentage) ? (o.completionPercentage as number | undefined) ?? undefined : undefined,
    lastEntryAtUtc: isOptionalString(o.lastEntryAtUtc) ? o.lastEntryAtUtc ?? undefined : undefined,
  });
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
    plannedQuantity: isOptionalNumber(o.plannedQuantity) ? (o.plannedQuantity as number | undefined) ?? undefined : undefined,
    goodQuantity: isNumber(o.goodQuantity) ? o.goodQuantity : 0,
    scrapQuantity: isNumber(o.scrapQuantity) ? o.scrapQuantity : 0,
    totalDowntimeSeconds: isOptionalNumber(o.totalDowntimeSeconds) ? (o.totalDowntimeSeconds as number | undefined) ?? undefined : undefined,
    manualCycleCount: isOptionalNumber(o.manualCycleCount) ? (o.manualCycleCount as number | undefined) ?? undefined : undefined,
    version: o.version,
    updatedAtUtc: o.updatedAtUtc,
    currentShiftCode: isOptionalString(o.currentShiftCode) ? o.currentShiftCode ?? undefined : undefined,
    primaryOperatorSubjectId: isOptionalString(o.primaryOperatorSubjectId) ? o.primaryOperatorSubjectId ?? undefined : undefined,
    activeOperatorCount: isOptionalNumber(o.activeOperatorCount) ? (o.activeOperatorCount as number | undefined) ?? undefined : undefined,
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

  const quantitySummaryResult = parseQuantitySummary(o.quantitySummary);
  if (quantitySummaryResult.status === "failure") {
    return fail(quantitySummaryResult.message);
  }

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
    quantitySummary: quantitySummaryResult.value,
    totalDowntimeSeconds: isOptionalNumber(o.totalDowntimeSeconds) ? (o.totalDowntimeSeconds as number | undefined) ?? undefined : undefined,
    totalSetupSeconds: isOptionalNumber(o.totalSetupSeconds) ? (o.totalSetupSeconds as number | undefined) ?? undefined : undefined,
    manualCycleCount: isOptionalNumber(o.manualCycleCount) ? (o.manualCycleCount as number | undefined) ?? undefined : undefined,
    totalManualCycleDurationMilliseconds: isOptionalNumber(o.totalManualCycleDurationMilliseconds) ? (o.totalManualCycleDurationMilliseconds as number | undefined) ?? undefined : undefined,
    lastCycleAtUtc: isOptionalString(o.lastCycleAtUtc) ? o.lastCycleAtUtc ?? undefined : undefined,
    createdBySubjectId: o.createdBySubjectId,
    lastModifiedBySubjectId: o.lastModifiedBySubjectId,
    version: o.version,
    createdAtUtc: o.createdAtUtc,
    updatedAtUtc: o.updatedAtUtc,
    // 5L: shift/operator summary
    currentShiftId: isOptionalString(o.currentShiftId) ? o.currentShiftId ?? undefined : undefined,
    currentShiftCode: isOptionalString(o.currentShiftCode) ? o.currentShiftCode ?? undefined : undefined,
    currentShiftStartAtUtc: isOptionalString(o.currentShiftStartAtUtc) ? o.currentShiftStartAtUtc ?? undefined : undefined,
    currentShiftEndAtUtc: isOptionalString(o.currentShiftEndAtUtc) ? o.currentShiftEndAtUtc ?? undefined : undefined,
    primaryOperatorSubjectId: isOptionalString(o.primaryOperatorSubjectId) ? o.primaryOperatorSubjectId ?? undefined : undefined,
    activeOperatorCount: isOptionalNumber(o.activeOperatorCount) ? (o.activeOperatorCount as number | undefined) ?? undefined : undefined,
    lastAssignmentAtUtc: isOptionalString(o.lastAssignmentAtUtc) ? o.lastAssignmentAtUtc ?? undefined : undefined,
  });
}
