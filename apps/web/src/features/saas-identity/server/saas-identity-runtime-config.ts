import "./saas-identity-server-boundary";

export type SaasIdentityRuntimeConfig = Readonly<{
  apiBaseUrl: string;
}>;

export type SaasIdentityRuntimeConfigResult =
  | Readonly<{ status: "success"; config: SaasIdentityRuntimeConfig }>
  | Readonly<{
      status: "failure";
      code: "configuration-error";
      message: string;
      retryable: false;
    }>;

function normalizeEnvValue(value: string | undefined): string {
  return value?.trim() ?? "";
}

export function resolveSaasIdentityRuntimeConfig(): SaasIdentityRuntimeConfigResult {
  const apiBaseUrl = normalizeEnvValue(
    process.env.CREVAFACTOS_API_BASE_URL,
  );

  if (!apiBaseUrl) {
    return {
      status: "failure",
      code: "configuration-error",
      message: "CREVAFACTOS_API_BASE_URL is required.",
      retryable: false,
    };
  }

  return {
    status: "success",
    config: { apiBaseUrl },
  };
}
