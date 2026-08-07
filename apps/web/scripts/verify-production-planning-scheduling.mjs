import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");

function fail(message) {
  console.error(`Production planning scheduling verification failed: ${message}`);
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

console.log("Running production planning scheduling verifier.");

const contracts = readRequiredFile(
  "src/features/production-planning/server/production-plan-scheduling-api-contracts.ts",
);
const client = readRequiredFile(
  "src/features/production-planning/server/production-plan-scheduling-api-client.ts",
);
const writer = readRequiredFile(
  "src/features/production-planning/server/production-plan-scheduling-writer.ts",
);
const actions = readRequiredFile(
  "src/features/production-planning/server/production-plan-scheduling-actions.ts",
);
const component = readRequiredFile(
  "src/features/production-planning/components/production-plan-scheduling-actions.tsx",
);
const detailPage = readRequiredFile(
  "src/app/(app)/planlama/[planId]/page.tsx",
);

// 1. Server boundary enforced
requireText(contracts, "production-plan-server-boundary", "scheduling-api-contracts.ts");
requireText(client, "production-plan-server-boundary", "scheduling-api-client.ts");
requireText(writer, "production-plan-server-boundary", "scheduling-writer.ts");

// 2. Actions file is a server action
requireText(actions, '"use server"', "scheduling-actions.ts");

// 3. Component is a client component
requireText(component, '"use client"', "scheduling-actions.tsx");

// 4. Scheduling endpoint defined correctly
requireText(client, "/schedule", "scheduling-api-client.ts");

// 5. Preview write blocked
requireText(writer, "development-preview", "scheduling-writer.ts");
requireText(writer, "preview-write-blocked", "scheduling-writer.ts");

// 6. Schedule action requires Approved status only
requireText(component, '"approved"', "scheduling-actions.tsx");

// 7. Start/end datetime fields in form
requireText(component, "plannedStart", "scheduling-actions.tsx");
requireText(component, "plannedEnd", "scheduling-actions.tsx");

// 8. Schedule success response includes scheduling fields
requireText(contracts, "plannedStartAtUtc", "scheduling-api-contracts.ts");
requireText(contracts, "plannedEndAtUtc", "scheduling-api-contracts.ts");
requireText(contracts, "scheduledAtUtc", "scheduling-api-contracts.ts");

// 9. Concurrency conflict handled
requireText(component, "concurrency-conflict", "scheduling-actions.tsx");

// 10. Blocking risk handled
requireText(component, "schedule-blocked-by-risk", "scheduling-actions.tsx");

// 11. Router refresh on success (no silent state override)
requireText(component, "router.refresh", "scheduling-actions.tsx");

// 12. Detail page includes scheduling component and action
requireText(detailPage, "ProductionPlanSchedulingActions", "page.tsx");
requireText(detailPage, "scheduleAction", "page.tsx");

// 13. Component does not make direct backend fetch
forbidText(component, "fetch(", "scheduling-actions.tsx (client component)");

// 14. No direct browser auth headers in component
forbidText(component, "Authorization", "scheduling-actions.tsx");

console.log("Production planning scheduling verifier passed.");
