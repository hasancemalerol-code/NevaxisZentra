import type {
  ProductionPlanConfidence,
  ProductionPlanOrigin,
  ProductionPlanPriority,
  ProductionPlanStatus,
  ProductionPlanTargetSystem,
} from "../domain/production-plan";
import type { ProductionPlanFilter } from "../application/production-plan-query";

export type ProductionPlanSearchParams =
  Readonly<
    Record<
      string,
      string | string[] | undefined
    >
  >;

const PRODUCTION_PLAN_STATUSES =
  new Set<ProductionPlanStatus>([
    "draft",
    "needs-review",
    "ready-for-approval",
    "approved",
    "rejected",
    "scheduled",
    "in-progress",
    "completed",
    "cancelled",
  ]);

const PRODUCTION_PLAN_ORIGINS =
  new Set<ProductionPlanOrigin>([
    "approved-order",
    "erp-production-request",
    "manual",
    "ai-recommendation",
  ]);

const PRODUCTION_PLAN_PRIORITIES =
  new Set<ProductionPlanPriority>([
    "low",
    "normal",
    "high",
    "urgent",
  ]);

const PRODUCTION_PLAN_CONFIDENCES =
  new Set<ProductionPlanConfidence>([
    "low",
    "medium",
    "high",
  ]);

const PRODUCTION_PLAN_TARGET_SYSTEMS =
  new Set<ProductionPlanTargetSystem>([
    "erp",
    "mes",
    "crevafactos",
  ]);

function getSingleSearchParam(
  value: string | string[] | undefined,
): string | undefined {
  if (Array.isArray(value)) {
    return value[0]?.trim() || undefined;
  }

  return value?.trim() || undefined;
}

function parseBooleanSearchParam(
  value: string | string[] | undefined,
): boolean | undefined {
  const normalizedValue =
    getSingleSearchParam(value);

  if (normalizedValue === "1") {
    return true;
  }

  if (normalizedValue === "0") {
    return false;
  }

  return undefined;
}

function parseEnumValue<T extends string>(
  value: string | undefined,
  allowedValues: ReadonlySet<T>,
): T | undefined {
  if (
    value &&
    allowedValues.has(value as T)
  ) {
    return value as T;
  }

  return undefined;
}

export function parseProductionPlanFilter(
  searchParams: ProductionPlanSearchParams,
): ProductionPlanFilter {
  const search = getSingleSearchParam(
    searchParams.q,
  );

  const facilityId = getSingleSearchParam(
    searchParams.facility,
  );

  return {
    search,
    status: parseEnumValue(
      getSingleSearchParam(
        searchParams.status,
      ),
      PRODUCTION_PLAN_STATUSES,
    ),
    origin: parseEnumValue(
      getSingleSearchParam(
        searchParams.origin,
      ),
      PRODUCTION_PLAN_ORIGINS,
    ),
    priority: parseEnumValue(
      getSingleSearchParam(
        searchParams.priority,
      ),
      PRODUCTION_PLAN_PRIORITIES,
    ),
    confidence: parseEnumValue(
      getSingleSearchParam(
        searchParams.confidence,
      ),
      PRODUCTION_PLAN_CONFIDENCES,
    ),
    targetSystem: parseEnumValue(
      getSingleSearchParam(
        searchParams.target,
      ),
      PRODUCTION_PLAN_TARGET_SYSTEMS,
    ),
    facilityId,
    blockingOnly:
      parseBooleanSearchParam(
        searchParams.blocking,
      ),
    approvalPendingOnly:
      parseBooleanSearchParam(
        searchParams.approvalPending,
      ),
    transferableOnly:
      parseBooleanSearchParam(
        searchParams.transferable,
      ),
  };
}

function setOptionalSearchParam(
  searchParams: URLSearchParams,
  key: string,
  value: string | boolean | undefined,
): void {
  if (
    value === undefined ||
    value === false ||
    value === ""
  ) {
    searchParams.delete(key);
    return;
  }

  searchParams.set(
    key,
    value === true ? "1" : value,
  );
}

export function createProductionPlanFilterUrl(
  filter: ProductionPlanFilter,
  pathname = "/planlama",
): string {
  const searchParams =
    new URLSearchParams();

  setOptionalSearchParam(
    searchParams,
    "q",
    filter.search?.trim(),
  );

  setOptionalSearchParam(
    searchParams,
    "status",
    filter.status,
  );

  setOptionalSearchParam(
    searchParams,
    "origin",
    filter.origin,
  );

  setOptionalSearchParam(
    searchParams,
    "priority",
    filter.priority,
  );

  setOptionalSearchParam(
    searchParams,
    "confidence",
    filter.confidence,
  );

  setOptionalSearchParam(
    searchParams,
    "target",
    filter.targetSystem,
  );

  setOptionalSearchParam(
    searchParams,
    "facility",
    filter.facilityId?.trim(),
  );

  setOptionalSearchParam(
    searchParams,
    "blocking",
    filter.blockingOnly,
  );

  setOptionalSearchParam(
    searchParams,
    "approvalPending",
    filter.approvalPendingOnly,
  );

  setOptionalSearchParam(
    searchParams,
    "transferable",
    filter.transferableOnly,
  );

  const queryString =
    searchParams.toString();

  return queryString
    ? `${pathname}?${queryString}`
    : pathname;
}

export function hasActiveProductionPlanFilter(
  filter: ProductionPlanFilter,
): boolean {
  return Boolean(
    filter.search ||
      filter.status ||
      filter.origin ||
      filter.priority ||
      filter.confidence ||
      filter.targetSystem ||
      filter.facilityId ||
      filter.blockingOnly ||
      filter.approvalPendingOnly ||
      filter.transferableOnly,
  );
}