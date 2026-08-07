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

function fail(message) {
  console.error(
    `Production planning human-approval verification failed: ${message}`,
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

console.log("Running production planning human-approval verifier.");

const workflowWriter = readRequiredFile(
  "src/features/production-planning/server/production-plan-workflow-writer.ts",
);
const workflowActions = readRequiredFile(
  "src/features/production-planning/server/production-plan-workflow-actions.ts",
);
const workflowActionsComponent = readRequiredFile(
  "src/features/production-planning/components/production-plan-workflow-actions.tsx",
);
const workflowClient = readRequiredFile(
  "src/features/production-planning/server/production-plan-workflow-api-client.ts",
);
const requestContext = readRequiredFile(
  "src/features/production-planning/server/production-plan-request-context.ts",
);
const workflowContracts = readRequiredFile(
  "src/features/production-planning/server/production-plan-workflow-api-contracts.ts",
);

// 1. Approve is a separate function from submit/review (no combined auto-approve path)
requireText(workflowWriter, "writeApproveProductionPlan", "workflow-writer.ts");
requireText(workflowWriter, "writeSubmitProductionPlanForReview", "workflow-writer.ts");

// 2. No auto-approve — submit and approve are separate operations
forbidText(workflowWriter, "autoApprove", "workflow-writer.ts");
forbidText(workflowWriter, "auto-approve", "workflow-writer.ts");

// 3. AI-generated flag is not in workflow request bodies
forbidText(workflowContracts, "aiGenerated", "workflow-api-contracts.ts");
forbidText(workflowWriter, "aiGenerated", "workflow-writer.ts");

// 4. Worker/system approval shortcut does not exist in web layer
forbidText(workflowWriter, "systemApprove", "workflow-writer.ts");
forbidText(workflowWriter, "workerApprove", "workflow-writer.ts");
forbidText(workflowWriter, "backgroundApprove", "workflow-writer.ts");

// 5. Approval actor comes from server request context (not client body)
requireText(workflowWriter, "resolveProductionPlanRequestContext", "workflow-writer.ts");
requireText(requestContext, "X-CrevaFactOS-Dev-Subject", "production-plan-request-context.ts (dev actor)");

// 6. Preview mode blocks approval writes
requireText(workflowWriter, "development-preview", "workflow-writer.ts");
requireText(workflowWriter, "preview-write-blocked", "workflow-writer.ts");

// 7. Actor identity cannot be passed from the client component
forbidText(workflowActionsComponent, "actorId", "production-plan-workflow-actions.tsx");
forbidText(workflowActionsComponent, "identitySubjectId", "production-plan-workflow-actions.tsx");
forbidText(workflowActionsComponent, "tenantId", "production-plan-workflow-actions.tsx");

// 8. Timestamp cannot be passed from the client component
forbidText(workflowActionsComponent, "approvedAt", "production-plan-workflow-actions.tsx");
forbidText(workflowActionsComponent, "occurredAt", "production-plan-workflow-actions.tsx");

// 9. Server actions wrapper is correctly marked as server-only
requireText(workflowActions, '"use server"', "production-plan-workflow-actions.ts");

// 10. HTTP client uses POST (not GET or PUT — no approve via GET)
requireText(workflowClient, "method: \"POST\"", "workflow-api-client.ts");
forbidText(workflowClient, "method: \"GET\"", "workflow-api-client.ts (approve/reject must not be GET)");

// 11. No approval via GET request (approve endpoint must be POST)
forbidText(workflowClient, "method: \"GET\"", "workflow-api-client.ts (approve/reject must not use GET)");

// 12. Reason/note fields are strings — no complex decision objects passed
requireText(workflowContracts, "note?: string", "workflow-api-contracts.ts (approval note is optional string)");
requireText(workflowContracts, "reason: string", "workflow-api-contracts.ts (rejection reason is required string)");

console.log("Production planning human-approval verification passed.");

console.log(
  [
    "Verified approve permission is separate from review,",
    "no AI auto-approve,",
    "no worker/system approval shortcut,",
    "approval actor from server request context,",
    "preview approval blocked,",
    "actor/timestamp not from client component,",
    "and server actions marked use-server.",
  ].join(" "),
);
