import "./production-order-server-boundary";

export type ProductionExecutionRuntimeMode =
  | "api"
  | "development-preview"
  | "development-api";

const ALLOWED_RUNTIME_MODES =
  new Set<ProductionExecutionRuntimeMode>([
    "api",
    "development-preview",
    "development-api",
  ]);

export type ProductionExecutionRuntimeConfig =
  Readonly<{
    mode: ProductionExecutionRuntimeMode;
    apiBaseUrl?: string;
    developmentAuth?: Readonly<{
      issuer: string;
      subject: string;
      tenantKey: string;
    }>;
    facilityId?: string;
  }>;

export type ProductionExecutionRuntimeConfigResult =
  | Readonly<{
      status: "success";
      config: ProductionExecutionRuntimeConfig;
    }>
  | Readonly<{
      status: "failure";
      code: "configuration-error";
      message: string;
      retryable: false;
    }>;

function normalizeEnvValue(
  value: string | undefined,
): string {
  return value?.trim() ?? "";
}

const CUSTOMER_SAFE_CONFIGURATION_ERROR_MESSAGE =
  "Bu modül şu anda yapılandırma nedeniyle kullanılamıyor. Lütfen sistem yöneticinizle iletişime geçin.";

function fail(
  technicalMessage: string,
): ProductionExecutionRuntimeConfigResult {
  // Log the technical detail (env var names, etc.) server-side only. The
  // customer-facing message must never leak internal configuration keys.
  console.error(
    "[production-execution-runtime-config] " + technicalMessage,
  );

  return {
    status: "failure",
    code: "configuration-error",
    message: CUSTOMER_SAFE_CONFIGURATION_ERROR_MESSAGE,
    retryable: false,
  };
}

export function parseProductionExecutionRuntimeConfig(): ProductionExecutionRuntimeConfigResult {
  const isProduction =
    normalizeEnvValue(process.env.NODE_ENV) === "production";

  const configuredMode = normalizeEnvValue(
    process.env.NEVAXIS_FACTOS_PRODUCTION_EXECUTION_RUNTIME_MODE,
  );

  const apiBaseUrl = normalizeEnvValue(
    process.env.NEVAXIS_FACTOS_API_BASE_URL,
  );

  if (!configuredMode) {
    if (isProduction) {
      const facilityId = normalizeEnvValue(
        process.env.NEVAXIS_FACTOS_FACILITY_ID,
      );

      if (!apiBaseUrl) {
        return fail("NEVAXIS_FACTOS_API_BASE_URL is required.");
      }

      if (!facilityId) {
        return fail("NEVAXIS_FACTOS_FACILITY_ID is required.");
      }

      return {
        status: "success",
        config: { mode: "api", apiBaseUrl, facilityId },
      };
    }

    return fail(
      "NEVAXIS_FACTOS_PRODUCTION_EXECUTION_RUNTIME_MODE must be configured outside production.",
    );
  }

  if (
    !ALLOWED_RUNTIME_MODES.has(
      configuredMode as ProductionExecutionRuntimeMode,
    )
  ) {
    return fail(
      "NEVAXIS_FACTOS_PRODUCTION_EXECUTION_RUNTIME_MODE is invalid.",
    );
  }

  const mode = configuredMode as ProductionExecutionRuntimeMode;

  if (isProduction && mode !== "api") {
    return fail(
      "Preview and development API modes are forbidden in production.",
    );
  }

  if (mode === "api" || mode === "development-api") {
    if (!apiBaseUrl) {
      return fail("NEVAXIS_FACTOS_API_BASE_URL is required for API modes.");
    }

    const facilityId =
      mode === "development-api"
        ? normalizeEnvValue(process.env.NEVAXIS_FACTOS_DEV_FACILITY_ID) ||
          normalizeEnvValue(process.env.NEVAXIS_FACTOS_FACILITY_ID)
        : normalizeEnvValue(process.env.NEVAXIS_FACTOS_FACILITY_ID);

    if (!facilityId) {
      return fail("A facility identifier is required for API modes.");
    }

    if (mode === "development-api") {
      const issuer = normalizeEnvValue(process.env.NEVAXIS_FACTOS_DEV_AUTH_ISSUER);
      const subject = normalizeEnvValue(
        process.env.NEVAXIS_FACTOS_DEV_AUTH_SUBJECT,
      );
      const tenantKey = normalizeEnvValue(
        process.env.NEVAXIS_FACTOS_DEV_TENANT_KEY,
      );

      if (!issuer || !subject || !tenantKey) {
        return fail(
          "Development API authentication headers are not configured.",
        );
      }

      return {
        status: "success",
        config: {
          mode,
          apiBaseUrl,
          facilityId,
          developmentAuth: { issuer, subject, tenantKey },
        },
      };
    }

    return {
      status: "success",
      config: { mode, apiBaseUrl, facilityId },
    };
  }

  return fail("Development preview mode is not yet configured for production execution.");
}
