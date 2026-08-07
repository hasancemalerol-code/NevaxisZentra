import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");

function fail(message) {
  console.error(`Production planning transfer-boundary verification failed: ${message}`);
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

console.log("Running production planning transfer-boundary verifier.");

const contracts = readRequiredFile(
  "src/features/production-planning/server/production-plan-transfer-api-contracts.ts",
);
const client = readRequiredFile(
  "src/features/production-planning/server/production-plan-transfer-api-client.ts",
);
const writer = readRequiredFile(
  "src/features/production-planning/server/production-plan-transfer-writer.ts",
);
const actions = readRequiredFile(
  "src/features/production-planning/server/production-plan-transfer-actions.ts",
);
const component = readRequiredFile(
  "src/features/production-planning/components/production-plan-transfer-panel.tsx",
);
const detailPage = readRequiredFile(
  "src/app/(app)/planlama/[planId]/page.tsx",
);

// 1. Server boundary enforced on server modules
requireText(contracts, "production-plan-server-boundary", "transfer-api-contracts.ts");
requireText(client, "production-plan-server-boundary", "transfer-api-client.ts");
requireText(writer, "production-plan-server-boundary", "transfer-writer.ts");

// 2. Actions file is a server action
requireText(actions, '"use server"', "transfer-actions.ts");

// 3. Component is a client component
requireText(component, '"use client"', "transfer-panel.tsx");

// 4. Request and execute are separate endpoints
requireText(client, "transfer-requests", "transfer-api-client.ts");
requireText(client, "/execute", "transfer-api-client.ts");

// 5. Request and execute are separate exported functions
requireText(actions, "createTransferRequestAction", "transfer-actions.ts");
requireText(actions, "executeTransferAction", "transfer-actions.ts");

// 6. Target system is typed allowlist — no vendor-specific values
forbidText(contracts, "SAP", "transfer-api-contracts.ts");
forbidText(contracts, "Logo", "transfer-api-contracts.ts");
forbidText(contracts, "Netsis", "transfer-api-contracts.ts");
requireText(component, '"erp"', "transfer-panel.tsx");
requireText(component, '"mes"', "transfer-panel.tsx");

// 7. Idempotency fields in response
requireText(contracts, "transferRequestId", "transfer-api-contracts.ts");

// 8. Uncertain state handled in UI
requireText(component, "reconciliationRequired", "transfer-panel.tsx");
requireText(component, "Manuel Mutabakat", "transfer-panel.tsx");

// 9. Retryable failure handled distinctly from non-retryable
requireText(component, "retryable", "transfer-panel.tsx");
requireText(component, "Tekrar Dene", "transfer-panel.tsx");

// 10. No automatic retry — router.refresh only, no auto re-call
requireText(component, "router.refresh", "transfer-panel.tsx");
forbidText(component, "setTimeout", "transfer-panel.tsx");
forbidText(component, "setInterval", "transfer-panel.tsx");

// 11. Transfer panel only visible when plan is scheduled
requireText(component, '"scheduled"', "transfer-panel.tsx");

// 12. Preview write blocked
requireText(writer, "development-preview", "transfer-writer.ts");
requireText(writer, "preview-write-blocked", "transfer-writer.ts");

// 13. Detail page includes transfer panel and actions
requireText(detailPage, "ProductionPlanTransferPanel", "page.tsx");
requireText(detailPage, "createTransferRequestAction", "page.tsx");
requireText(detailPage, "executeTransferAction", "page.tsx");

// 14. Component does not make direct backend fetch
forbidText(component, "fetch(", "transfer-panel.tsx (client component)");

// 15. No raw backend error messages shown to user
forbidText(component, "rawError", "transfer-panel.tsx");

// 16. Transfer succeeded state blocks new requests
requireText(component, "isTransferSucceeded", "transfer-panel.tsx");

console.log("Production planning transfer-boundary verifier passed.");
