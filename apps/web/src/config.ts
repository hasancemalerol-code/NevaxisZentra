const DEFAULT_APPLICATION_NAME = "CrevaFactOS Web";

export type WebApplicationConfig = Readonly<{
  applicationName: string;
}>;

export function getWebApplicationConfig(
  environment: NodeJS.ProcessEnv = process.env,
): WebApplicationConfig {
  const applicationName =
    environment.CREVAFACTOS_WEB_APPLICATION_NAME ?? DEFAULT_APPLICATION_NAME;

  if (applicationName.trim().length === 0) {
    throw new Error("CREVAFACTOS_WEB_APPLICATION_NAME cannot be empty.");
  }

  return { applicationName };
}
