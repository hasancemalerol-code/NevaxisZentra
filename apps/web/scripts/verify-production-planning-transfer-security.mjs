import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");

function fail(message) {
  console.error(`Production planning transfer-security verification failed: ${message}`);
  process.exit(1);
}

function resolveWebPath(relativePath) {
  return resolve(webRoot, relativePath);
}

function readRequiredFile(relativePath) {
  const filePath = resolveWebPath(relativePath);
  if (!existsSync(filePath)) {
    fail(`Required file is missing: ${relativePath}`);
  }
  return readFileSync(filePath, "utf8").replaceAll("\r\n", "\n");
}

function requireText(source, expectedText, fileLabel) {
  if (!source.includes(expectedText)) {
    fail(`${fileLabel} must contain "${expectedText}".`);
  }
}

function forbidText(source, forbiddenText, fileLabel) {
  if (source.includes(forbiddenText)) {
    fail(`${fileLabel} must not contain "${forbiddenText}".`);
  }
}

console.log("Running production planning transfer-security verifier.");

const writer = readRequiredFile(
  "src/features/production-planning/server/production-plan-transfer-writer.ts",
);
const client = readRequiredFile(
  "src/features/production-planning/server/production-plan-transfer-api-client.ts",
);
const schedulingWriter = readRequiredFile(
  "src/features/production-planning/server/production-plan-scheduling-writer.ts",
);
const schedulingClient = readRequiredFile(
  "src/features/production-planning/server/production-plan-scheduling-api-client.ts",
);
const component = readRequiredFile(
  "src/features/production-planning/components/production-plan-transfer-panel.tsx",
);
const schedulingComponent = readRequiredFile(
  "src/features/production-planning/components/production-plan-scheduling-actions.tsx",
);

// 1. Server modules import server boundary
requireText(writer, "production-plan-server-boundary", "transfer-writer.ts");
requireText(client, "production-plan-server-boundary", "transfer-api-client.ts");
requireText(schedulingWriter, "production-plan-server-boundary", "scheduling-writer.ts");
requireText(schedulingClient, "production-plan-server-boundary", "scheduling-api-client.ts");

// 2. Client components do not import server boundary
forbidText(component, "production-plan-server-boundary", "transfer-panel.tsx (client)");
forbidText(schedulingComponent, "production-plan-server-boundary", "scheduling-actions.tsx (client)");

// 3. No auth headers leak to client components
forbidText(component, "Authorization", "transfer-panel.tsx");
forbidText(schedulingComponent, "Authorization", "scheduling-actions.tsx");

// 4. No direct fetch calls in client components
forbidText(component, "fetch(", "transfer-panel.tsx");
forbidText(schedulingComponent, "fetch(", "scheduling-actions.tsx");

// 5. Preview write blocked in scheduling
requireText(schedulingWriter, "preview-write-blocked", "scheduling-writer.ts");
requireText(schedulingWriter, "development-preview", "scheduling-writer.ts");

// 6. Preview write blocked in transfer
requireText(writer, "preview-write-blocked", "transfer-writer.ts");
requireText(writer, "development-preview", "transfer-writer.ts");

// 7. Server request context used (tenant/facility isolation)
requireText(writer, "resolveProductionPlanRequestContext", "transfer-writer.ts");
requireText(schedulingWriter, "resolveProductionPlanRequestContext", "scheduling-writer.ts");

// 8. Facility scope resolved server-side
requireText(writer, "resolveProductionPlanningServerScope", "transfer-writer.ts");
requireText(schedulingWriter, "resolveProductionPlanningServerScope", "scheduling-writer.ts");

// 9. No raw backend credentials or secrets in client components
forbidText(component, "apiBaseUrl", "transfer-panel.tsx");
forbidText(schedulingComponent, "apiBaseUrl", "scheduling-actions.tsx");

// 10. facilityId is query param in server-built URLs only
requireText(client, "facilityId", "transfer-api-client.ts");
requireText(schedulingClient, "facilityId", "scheduling-api-client.ts");
forbidText(component, "facilityId", "transfer-panel.tsx");
forbidText(schedulingComponent, "facilityId", "scheduling-actions.tsx");

console.log("Production planning transfer-security verifier passed.");
