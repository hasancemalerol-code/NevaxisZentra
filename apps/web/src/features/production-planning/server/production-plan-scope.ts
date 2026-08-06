import type { ProductionPlanRepositoryScope } from "../application/production-plan-repository";

export type ProductionPlanningScopeSource =
  "preview-fallback";

export type ProductionPlanningServerScope =
  Readonly<
    ProductionPlanRepositoryScope & {
      source: ProductionPlanningScopeSource;
    }
  >;

export type ProductionPlanningScopeResolutionFailure =
  Readonly<{
    status: "failure";
    code: "invalid-scope";
    message: string;
    retryable: false;
  }>;

export type ProductionPlanningScopeResolutionResult =
  | Readonly<{
      status: "success";
      scope: ProductionPlanningServerScope;
    }>
  | ProductionPlanningScopeResolutionFailure;

const PREVIEW_FALLBACK_TENANT_ID =
  "tenant-crevafactos";

const PREVIEW_FALLBACK_FACILITY_ID =
  "facility-istanbul";

function normalizeScopeValue(
  value: string | undefined,
): string {
  return value?.trim() ?? "";
}

function createInvalidScopeFailure(
  message: string,
): ProductionPlanningScopeResolutionFailure {
  return {
    status: "failure",
    code: "invalid-scope",
    message,
    retryable: false,
  };
}

function createPreviewFallbackScope(): ProductionPlanningServerScope {
  const tenantId = normalizeScopeValue(
    PREVIEW_FALLBACK_TENANT_ID,
  );
  const facilityId = normalizeScopeValue(
    PREVIEW_FALLBACK_FACILITY_ID,
  );

  if (!tenantId || !facilityId) {
    throw new Error(
      "Preview fallback scope constants are invalid.",
    );
  }

  return {
    tenantId,
    facilityId,
    source: "preview-fallback",
  };
}

export function resolveProductionPlanningServerScope(): ProductionPlanningScopeResolutionResult {
  const scope = createPreviewFallbackScope();

  if (
    !normalizeScopeValue(scope.tenantId) ||
    !normalizeScopeValue(scope.facilityId)
  ) {
    return createInvalidScopeFailure(
      "tenantId and facilityId are required.",
    );
  }

  return {
    status: "success",
    scope,
  };
}
