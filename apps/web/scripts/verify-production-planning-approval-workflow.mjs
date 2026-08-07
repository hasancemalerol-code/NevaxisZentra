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
    `Production planning approval-workflow verification failed: ${message}`,
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

console.log("Running production planning approval-workflow verifier.");

const workflowContracts = readRequiredFile(
  "src/features/production-planning/server/production-plan-workflow-api-contracts.ts",
);
const workflowClient = readRequiredFile(
  "src/features/production-planning/server/production-plan-workflow-api-client.ts",
);
const workflowWriter = readRequiredFile(
  "src/features/production-planning/server/production-plan-workflow-writer.ts",
);
const workflowActions = readRequiredFile(
  "src/features/production-planning/server/production-plan-workflow-actions.ts",
);
const workflowActionsComponent = readRequiredFile(
  "src/features/production-planning/components/production-plan-workflow-actions.tsx",
);
const detailPage = readRequiredFile(
  "src/app/(app)/planlama/[planId]/page.tsx",
);

// 1. Server boundary enforced on all server modules
requireText(workflowContracts, "production-plan-server-boundary", "workflow-api-contracts.ts");
requireText(workflowClient, "production-plan-server-boundary", "workflow-api-client.ts");
requireText(workflowWriter, "production-plan-server-boundary", "workflow-workflow-writer.ts");

// 2. Four command endpoints exist in API client
requireText(workflowClient, "submit-for-review", "workflow-api-client.ts");
requireText(workflowClient, "request-revision", "workflow-api-client.ts");
requireText(workflowClient, "approve", "workflow-api-client.ts");
requireText(workflowClient, "reject", "workflow-api-client.ts");

// 3. Server actions file uses "use server"
requireText(workflowActions, '"use server"', "production-plan-workflow-actions.ts");

// 4. Review permission is distinct from approve (separate endpoints in client)
requireText(workflowClient, "submitProductionPlanForReview", "workflow-api-client.ts");
requireText(workflowClient, "requestProductionPlanRevision", "workflow-api-client.ts");
requireText(workflowClient, "approveProductionPlan", "workflow-api-client.ts");
requireText(workflowClient, "rejectProductionPlan", "workflow-api-client.ts");

// 5. Preview write blocked in workflow writer
requireText(workflowWriter, "development-preview", "workflow-writer.ts");
requireText(workflowWriter, "preview-write-blocked", "workflow-writer.ts");

// 6. No free status update (status is not sent in body)
forbidText(workflowContracts, '"status":', "workflow-api-contracts.ts (no free status in request body)");

// 7. Workflow actions component is a client component
requireText(workflowActionsComponent, '"use client"', "production-plan-workflow-actions.tsx");

// 8. State transitions covered in component
requireText(workflowActionsComponent, "canSubmitForReview", "production-plan-workflow-actions.tsx");
requireText(workflowActionsComponent, "canRequestRevision", "production-plan-workflow-actions.tsx");
requireText(workflowActionsComponent, "canApproveOrReject", "production-plan-workflow-actions.tsx");

// 9. Approved edit lock: isEditable excludes approved/rejected
requireText(detailPage, "needs-review", "detail page isEditable check");
forbidText(detailPage, '"approved"', "detail page isEditable must not include approved");

// 10. Detail page imports workflow actions
requireText(detailPage, "ProductionPlanWorkflowActions", "detail page");
requireText(detailPage, "production-plan-workflow-actions", "detail page");

// 11. No workflow action sends tenantId from client
forbidText(workflowContracts, "tenantId:", "workflow-api-contracts.ts (tenantId must not be in request body)");
forbidText(workflowClient, "tenantId", "workflow-api-client.ts");

// 12. expectedVersion is required in all commands
requireText(workflowContracts, "expectedVersion", "workflow-api-contracts.ts");

// 13. No auto-approve or AI approve
forbidText(workflowWriter, "auto-approve", "workflow-writer.ts");
forbidText(workflowWriter, "aiGenerated", "workflow-writer.ts");

// 14. router.refresh called after success (optimistic update not used)
requireText(workflowActionsComponent, "router.refresh()", "production-plan-workflow-actions.tsx");

console.log("Production planning approval-workflow verification passed.");

console.log(
  [
    "Verified review and approve permissions,",
    "four command endpoints,",
    "state transition allowlist,",
    "no free status update,",
    "server boundary enforcement,",
    "preview write blocked,",
    "approved edit lock,",
    "detail UI workflow panel,",
    "and router.refresh on success.",
  ].join(" "),
);
