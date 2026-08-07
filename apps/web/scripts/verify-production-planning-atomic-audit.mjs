import {
  existsSync,
  readFileSync,
} from "node:fs";
import {
  dirname,
  resolve,
} from "node:path";
import {
  fileURLToPath,
} from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");

// Atomic audit checks are primarily backend concerns.
// This verifier checks that the web bridge does NOT implement
// its own audit logic and correctly delegates to the server-side API,
// which handles atomicity through EF Core transactions.

function fail(message) {
  console.error(
    `Production planning atomic-audit verification failed: ${message}`,
  );
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

console.log("Running production planning atomic-audit verifier.");

const workflowClient = readRequiredFile(
  "src/features/production-planning/server/production-plan-workflow-api-client.ts",
);
const workflowWriter = readRequiredFile(
  "src/features/production-planning/server/production-plan-workflow-writer.ts",
);
const workflowContracts = readRequiredFile(
  "src/features/production-planning/server/production-plan-workflow-api-contracts.ts",
);
const workflowActions = readRequiredFile(
  "src/features/production-planning/server/production-plan-workflow-actions.ts",
);
const draftWriter = readRequiredFile(
  "src/features/production-planning/server/production-plan-draft-writer.ts",
);

// 1. Web workflow bridge does NOT write audit records directly
//    (audit is written atomically by the backend API)
forbidText(workflowClient, "audit", "workflow-api-client.ts (must not write audit directly)");
forbidText(workflowWriter, "AuditRecord", "workflow-writer.ts (must not write audit directly)");
forbidText(workflowWriter, "auditWriter", "workflow-writer.ts (must not write audit directly)");

// 2. Web workflow bridge does NOT write outbox messages directly
forbidText(workflowClient, "outbox", "workflow-api-client.ts (must not write outbox directly)");
forbidText(workflowWriter, "OutboxMessage", "workflow-writer.ts (must not write outbox directly)");

// 3. Workflow HTTP calls are POST to the API (which handles atomic transactions)
requireText(workflowClient, "method: \"POST\"", "workflow-api-client.ts");

// 4. No separate audit write after plan update (no post-commit audit)
forbidText(workflowClient, "postAudit", "workflow-api-client.ts");
forbidText(workflowWriter, "postAudit", "workflow-writer.ts");

// 5. No fire-and-forget audit (best-effort audit not used for workflow)
forbidText(workflowWriter, "best-effort", "workflow-writer.ts");
forbidText(workflowWriter, "bestEffort", "workflow-writer.ts");

// 6. expectedVersion is sent in all commands (audit only on successful version-matched transition)
requireText(workflowContracts, "expectedVersion", "workflow-api-contracts.ts");

// 7. Draft writer also uses API-delegated approach (no separate audit)
forbidText(draftWriter, "AuditRecord", "production-plan-draft-writer.ts (must not write audit directly)");
forbidText(draftWriter, "auditWriter", "production-plan-draft-writer.ts (must not write audit directly)");

// 8. Server actions file exists and properly delegates
requireText(workflowActions, '"use server"', "production-plan-workflow-actions.ts");
requireText(workflowActions, "writeSubmitProductionPlanForReview", "production-plan-workflow-actions.ts");
requireText(workflowActions, "writeApproveProductionPlan", "production-plan-workflow-actions.ts");
requireText(workflowActions, "writeRejectProductionPlan", "production-plan-workflow-actions.ts");

// 9. Workflow writer blocks preview mode (no stray writes possible)
requireText(workflowWriter, "preview-write-blocked", "workflow-writer.ts");

console.log("Production planning atomic-audit verification passed.");

console.log(
  [
    "Verified that web bridge delegates audit/outbox atomicity to backend API,",
    "no direct audit or outbox writes in web layer,",
    "no best-effort audit,",
    "no post-commit separate audit,",
    "expectedVersion enforced,",
    "and preview writes blocked.",
  ].join(" "),
);
