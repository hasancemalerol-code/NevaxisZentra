import "./saas-identity-server-boundary";

import { existsSync, readFileSync } from "node:fs";
import { join } from "node:path";

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

const CANONICAL_DEV_API_BASE_URL = "http://127.0.0.1:5000";

function normalizeEnvValue(value: string | undefined): string {
  return value?.trim() ?? "";
}

function readDevelopmentEnvLocalValue(key: string): string {
  if (process.env.NODE_ENV !== "development") {
    return "";
  }

  const candidates = [
    join(process.cwd(), ".env.local"),
    join(process.cwd(), "apps/web/.env.local"),
  ];

  for (const envPath of candidates) {
    if (!existsSync(envPath)) {
      continue;
    }

    try {
      const content = readFileSync(envPath, "utf8");
      for (const line of content.split(/\r?\n/)) {
        const trimmed = line.trim();
        if (!trimmed || trimmed.startsWith("#")) {
          continue;
        }

        const separator = trimmed.indexOf("=");
        if (separator <= 0) {
          continue;
        }

        if (trimmed.slice(0, separator).trim() !== key) {
          continue;
        }

        return trimmed
          .slice(separator + 1)
          .trim()
          .replace(/^['"]|['"]$/g, "");
      }
    } catch {
      continue;
    }
  }

  return "";
}

export function resolveSaasIdentityRuntimeConfig(): SaasIdentityRuntimeConfigResult {
  const localDevApiBaseUrl = readDevelopmentEnvLocalValue("NEVAXIS_FACTOS_API_BASE_URL");
  const apiBaseUrl =
    normalizeEnvValue(localDevApiBaseUrl)
    || normalizeEnvValue(process.env.NEVAXIS_FACTOS_API_BASE_URL)
    || (process.env.NODE_ENV === "development" ? CANONICAL_DEV_API_BASE_URL : "");

  if (!apiBaseUrl) {
    return {
      status: "failure",
      code: "configuration-error",
      message: "NEVAXIS_FACTOS_API_BASE_URL is required.",
      retryable: false,
    };
  }

  return {
    status: "success",
    config: { apiBaseUrl },
  };
}
