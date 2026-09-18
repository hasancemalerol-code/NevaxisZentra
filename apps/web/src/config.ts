import { readZentraEnv } from "@/lib/config/zentra-env";

const DEFAULT_APPLICATION_NAME = "Zentra";

export type WebApplicationConfig = Readonly<{
  applicationName: string;
}>;

export function getWebApplicationConfig(
  environment: NodeJS.ProcessEnv = process.env,
): WebApplicationConfig {
  const applicationName =
    readZentraEnv("WEB_APPLICATION_NAME", environment) ??
    DEFAULT_APPLICATION_NAME;

  if (applicationName.trim().length === 0) {
    throw new Error(
      "NEVAXIS_ZENTRA_WEB_APPLICATION_NAME (or legacy NEVAXIS_FACTOS_/CREVAFACTOS_ equivalent) cannot be empty.",
    );
  }

  return { applicationName };
}
