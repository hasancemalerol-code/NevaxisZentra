import "./production-plan-server-boundary";

import type { ProductionPlanRepositoryScope } from "../application/production-plan-repository";
import type { ProductionPlanRuntimeConfig } from "./production-plan-runtime-config";

export type ProductionPlanningScopeSource =
  | "development-preview-config"
  | "development-api-config"
  | "server-config";

export type ProductionPlanningServerScope =
  Readonly<
    ProductionPlanRepositoryScope & {
      source: ProductionPlanningScopeSource;
    }
  >;

export type ProductionPlanningScopeResolutionFailureCode =
  | "invalid-scope"
  | "configuration-error";

export type ProductionPlanningScopeResolutionFailure =
  Readonly<{
    status: "failure";
    code: ProductionPlanningScopeResolutionFailureCode;
    message: string;
    retryable: false;
  }>;

export type ProductionPlanningScopeResolutionResult =
  | Readonly<{
      status: "success";
      scope: ProductionPlanningServerScope;
    }>
  | ProductionPlanningScopeResolutionFailure;

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

function createConfigurationFailure(
  message: string,
): ProductionPlanningScopeResolutionFailure {
  return {
    status: "failure",
    code: "configuration-error",
    message,
    retryable: false,
  };
}

function resolveDevelopmentPreviewScope(
  config: ProductionPlanRuntimeConfig,
): ProductionPlanningScopeResolutionResult {
  const tenantId = normalizeScopeValue(
    config.previewTenantId,
  );
  const facilityId = normalizeScopeValue(
    config.previewFacilityId,
  );

  if (!tenantId || !facilityId) {
    return createConfigurationFailure(
      "Development preview tenant and facility scope are required.",
    );
  }

  return {
    status: "success",
    scope: {
      tenantId,
      facilityId,
      source: "development-preview-config",
    },
  };
}

function resolveDevelopmentApiScope(
  config: ProductionPlanRuntimeConfig,
): ProductionPlanningScopeResolutionResult {
  const tenantKey = normalizeScopeValue(
    config.developmentAuth?.tenantKey,
  );
  const facilityId = normalizeScopeValue(
    config.facilityId,
  );

  if (!tenantKey || !facilityId) {
    return createConfigurationFailure(
      "Development API tenant and facility scope are required.",
    );
  }

  return {
    status: "success",
    scope: {
      tenantId: tenantKey,
      facilityId,
      source: "development-api-config",
    },
  };
}

function resolveProductionApiScope(
  config: ProductionPlanRuntimeConfig,
): ProductionPlanningScopeResolutionResult {
  const facilityId = normalizeScopeValue(
    config.facilityId,
  );

  if (!facilityId) {
    return createInvalidScopeFailure(
      "A server-side facility identifier is required.",
    );
  }

  return {
    status: "success",
    scope: {
      tenantId: "",
      facilityId,
      source: "server-config",
    },
  };
}

export function resolveProductionPlanningServerScope(
  config: ProductionPlanRuntimeConfig,
): ProductionPlanningScopeResolutionResult {
  switch (config.mode) {
    case "development-preview":
      return resolveDevelopmentPreviewScope(
        config,
      );

    case "development-api":
      return resolveDevelopmentApiScope(
        config,
      );

    case "api":
      return resolveProductionApiScope(
        config,
      );

    default:
      return createConfigurationFailure(
        "Production planning runtime mode is invalid.",
      );
  }
}
