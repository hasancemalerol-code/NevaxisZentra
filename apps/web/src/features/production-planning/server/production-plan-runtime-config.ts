import "./production-plan-server-boundary";

import { readZentraEnv } from "@/lib/config/zentra-env";

export type ProductionPlanRuntimeMode =
  | "api"
  | "development-preview"
  | "development-api";

const ALLOWED_RUNTIME_MODES =
  new Set<ProductionPlanRuntimeMode>([
    "api",
    "development-preview",
    "development-api",
  ]);

export type ProductionPlanRuntimeConfig =
  Readonly<{
    mode: ProductionPlanRuntimeMode;
    apiBaseUrl?: string;
    developmentAuth?: Readonly<{
      issuer: string;
      subject: string;
      tenantKey: string;
    }>;
    facilityId?: string;
    previewTenantId?: string;
    previewFacilityId?: string;
  }>;

export type ProductionPlanRuntimeConfigFailure =
  Readonly<{
    status: "failure";
    code: "configuration-error";
    message: string;
    retryable: false;
  }>;

export type ProductionPlanRuntimeConfigResult =
  | Readonly<{
      status: "success";
      config: ProductionPlanRuntimeConfig;
    }>
  | ProductionPlanRuntimeConfigFailure;

function normalizeEnvValue(
  value: string | undefined,
): string {
  return value?.trim() ?? "";
}

const CUSTOMER_SAFE_CONFIGURATION_ERROR_MESSAGE =
  "Bu modül şu anda yapılandırma nedeniyle kullanılamıyor. Lütfen sistem yöneticinizle iletişime geçin.";

function createConfigurationFailure(
  technicalMessage: string,
): ProductionPlanRuntimeConfigFailure {
  // Log the technical detail (env var names, etc.) server-side only. The
  // customer-facing message must never leak internal configuration keys.
  console.error(
    "[production-plan-runtime-config] " + technicalMessage,
  );

  return {
    status: "failure",
    code: "configuration-error",
    message: CUSTOMER_SAFE_CONFIGURATION_ERROR_MESSAGE,
    retryable: false,
  };
}

function parseRuntimeMode(
  rawMode: string,
): ProductionPlanRuntimeMode | null {
  const normalizedMode =
    normalizeEnvValue(rawMode);

  if (
    !normalizedMode ||
    !ALLOWED_RUNTIME_MODES.has(
      normalizedMode as ProductionPlanRuntimeMode,
    )
  ) {
    return null;
  }

  return normalizedMode as ProductionPlanRuntimeMode;
}

function isProductionEnvironment(): boolean {
  return (
    normalizeEnvValue(process.env.NODE_ENV) ===
    "production"
  );
}

function readApiBaseUrl(): string {
  return normalizeEnvValue(
    process.env.NEVAXIS_FACTOS_API_BASE_URL,
  );
}

function readFacilityId(): string {
  return normalizeEnvValue(
    process.env.NEVAXIS_FACTOS_FACILITY_ID,
  );
}

function readDevelopmentAuth():
  | ProductionPlanRuntimeConfig["developmentAuth"]
  | null {
  const issuer = normalizeEnvValue(
    process.env.NEVAXIS_FACTOS_DEV_AUTH_ISSUER,
  );
  const subject = normalizeEnvValue(
    process.env.NEVAXIS_FACTOS_DEV_AUTH_SUBJECT,
  );
  const tenantKey = normalizeEnvValue(
    process.env.NEVAXIS_FACTOS_DEV_TENANT_KEY,
  );

  if (!issuer || !subject || !tenantKey) {
    return null;
  }

  return {
    issuer,
    subject,
    tenantKey,
  };
}

function readPreviewScope(): Readonly<{
  previewTenantId: string;
  previewFacilityId: string;
}> | null {
  const previewTenantId = normalizeEnvValue(
    process.env
      .NEVAXIS_FACTOS_DEV_PREVIEW_TENANT_ID,
  );
  const previewFacilityId =
    normalizeEnvValue(
      process.env
        .NEVAXIS_FACTOS_DEV_PREVIEW_FACILITY_ID,
    );

  if (!previewTenantId || !previewFacilityId) {
    return null;
  }

  return {
    previewTenantId,
    previewFacilityId,
  };
}

export function parseProductionPlanRuntimeConfig(): ProductionPlanRuntimeConfigResult {
  const isProduction =
    isProductionEnvironment();
  const configuredMode = normalizeEnvValue(
    readZentraEnv("PRODUCTION_PLANNING_RUNTIME_MODE"),
  );

  if (!configuredMode) {
    if (isProduction) {
      const apiBaseUrl = readApiBaseUrl();
      const facilityId = readFacilityId();

      if (!apiBaseUrl) {
        return createConfigurationFailure(
          "NEVAXIS_FACTOS_API_BASE_URL is required for API runtime modes.",
        );
      }

      if (!facilityId) {
        return createConfigurationFailure(
          "A server-side facility identifier is required for API runtime modes.",
        );
      }

      return {
        status: "success",
        config: {
          mode: "api",
          apiBaseUrl,
          facilityId,
        },
      };
    }

    return createConfigurationFailure(
      "NEVAXIS_FACTOS_PRODUCTION_PLANNING_RUNTIME_MODE must be explicitly configured outside production.",
    );
  }

  const mode = parseRuntimeMode(
    configuredMode,
  );

  if (!mode) {
    return createConfigurationFailure(
      "NEVAXIS_FACTOS_PRODUCTION_PLANNING_RUNTIME_MODE is invalid.",
    );
  }

  if (
    isProduction &&
    mode !== "api"
  ) {
    return createConfigurationFailure(
      "Preview and development API runtime modes are forbidden in production.",
    );
  }

  if (
    mode === "api" ||
    mode === "development-api"
  ) {
    const apiBaseUrl = readApiBaseUrl();

    if (!apiBaseUrl) {
      return createConfigurationFailure(
        "NEVAXIS_FACTOS_API_BASE_URL is required for API runtime modes.",
      );
    }

    const facilityId =
      mode === "development-api"
        ? normalizeEnvValue(
            process.env
              .NEVAXIS_FACTOS_DEV_FACILITY_ID,
          ) || readFacilityId()
        : readFacilityId();

    if (!facilityId) {
      return createConfigurationFailure(
        "A server-side facility identifier is required for API runtime modes.",
      );
    }

    if (mode === "development-api") {
      const developmentAuth =
        readDevelopmentAuth();

      if (!developmentAuth) {
        return createConfigurationFailure(
          "Development API authentication headers are not configured.",
        );
      }

      return {
        status: "success",
        config: {
          mode,
          apiBaseUrl,
          facilityId,
          developmentAuth,
        },
      };
    }

    return {
      status: "success",
      config: {
        mode,
        apiBaseUrl,
        facilityId,
      },
    };
  }

  const previewScope = readPreviewScope();

  if (!previewScope) {
    return createConfigurationFailure(
      "Development preview scope is not configured.",
    );
  }

  return {
    status: "success",
    config: {
      mode,
      previewTenantId:
        previewScope.previewTenantId,
      previewFacilityId:
        previewScope.previewFacilityId,
    },
  };
}
