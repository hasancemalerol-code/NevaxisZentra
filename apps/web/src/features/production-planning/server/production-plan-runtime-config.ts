import "./production-plan-server-boundary";

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

function createConfigurationFailure(
  message: string,
): ProductionPlanRuntimeConfigFailure {
  return {
    status: "failure",
    code: "configuration-error",
    message,
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
    process.env.CREVAFACTOS_API_BASE_URL,
  );
}

function readFacilityId(): string {
  return normalizeEnvValue(
    process.env.CREVAFACTOS_FACILITY_ID,
  );
}

function readDevelopmentAuth():
  | ProductionPlanRuntimeConfig["developmentAuth"]
  | null {
  const issuer = normalizeEnvValue(
    process.env.CREVAFACTOS_DEV_AUTH_ISSUER,
  );
  const subject = normalizeEnvValue(
    process.env.CREVAFACTOS_DEV_AUTH_SUBJECT,
  );
  const tenantKey = normalizeEnvValue(
    process.env.CREVAFACTOS_DEV_TENANT_KEY,
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
      .CREVAFACTOS_DEV_PREVIEW_TENANT_ID,
  );
  const previewFacilityId =
    normalizeEnvValue(
      process.env
        .CREVAFACTOS_DEV_PREVIEW_FACILITY_ID,
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
    process.env
      .CREVAFACTOS_PRODUCTION_PLANNING_RUNTIME_MODE,
  );

  if (!configuredMode) {
    if (isProduction) {
      const apiBaseUrl = readApiBaseUrl();
      const facilityId = readFacilityId();

      if (!apiBaseUrl) {
        return createConfigurationFailure(
          "CREVAFACTOS_API_BASE_URL is required for API runtime modes.",
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
      "CREVAFACTOS_PRODUCTION_PLANNING_RUNTIME_MODE must be explicitly configured outside production.",
    );
  }

  const mode = parseRuntimeMode(
    configuredMode,
  );

  if (!mode) {
    return createConfigurationFailure(
      "CREVAFACTOS_PRODUCTION_PLANNING_RUNTIME_MODE is invalid.",
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
        "CREVAFACTOS_API_BASE_URL is required for API runtime modes.",
      );
    }

    const facilityId =
      mode === "development-api"
        ? normalizeEnvValue(
            process.env
              .CREVAFACTOS_DEV_FACILITY_ID,
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
