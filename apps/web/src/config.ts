const DEFAULT_APPLICATION_NAME = "FactOS";

export type WebApplicationConfig = Readonly<{
  applicationName: string;
}>;

export function getWebApplicationConfig(
  environment: NodeJS.ProcessEnv = process.env,
): WebApplicationConfig {
  const applicationName =
    environment.NEVAXIS_FACTOS_WEB_APPLICATION_NAME ??
    environment.CREVAFACTOS_WEB_APPLICATION_NAME ??
    DEFAULT_APPLICATION_NAME;

  if (applicationName.trim().length === 0) {
    throw new Error(
      "NEVAXIS_FACTOS_WEB_APPLICATION_NAME (or legacy CREVAFACTOS_WEB_APPLICATION_NAME) cannot be empty.",
    );
  }

  return { applicationName };
}
