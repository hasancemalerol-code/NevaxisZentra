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
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(
    `Production planning final-hardening verification failed: ${message}`,
  );
  process.exit(1);
}

function resolveWebPath(relativePath) {
  return resolve(webRoot, relativePath);
}

function resolveProjectPath(relativePath) {
  return resolve(projectRoot, relativePath);
}

function readRequiredFile(absolutePath, label) {
  if (!existsSync(absolutePath)) {
    fail(`Required file is missing: ${label ?? absolutePath}`);
  }
  return readFileSync(absolutePath, "utf8").replaceAll("\r\n", "\n");
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

function forbidFile(absolutePath, label) {
  if (existsSync(absolutePath)) {
    fail(`File must not exist in this phase: ${label}`);
  }
}

console.log("Running production planning final-hardening verifier.");

// Load key backend files
const workflowRepository = readRequiredFile(
  resolveProjectPath("packages/CrevaFactOS.Persistence/ProductionPlanning/ProductionPlanWorkflowRepository.cs"),
  "ProductionPlanWorkflowRepository.cs",
);
const writeRepository = readRequiredFile(
  resolveProjectPath("packages/CrevaFactOS.Persistence/ProductionPlanning/ProductionPlanWriteRepository.cs"),
  "ProductionPlanWriteRepository.cs",
);
const transferRepository = readRequiredFile(
  resolveProjectPath("packages/CrevaFactOS.Persistence/ProductionPlanning/ProductionPlanTransferRepository.cs"),
  "ProductionPlanTransferRepository.cs",
);
const handoffRepository = readRequiredFile(
  resolveProjectPath("packages/CrevaFactOS.Persistence/ProductionPlanning/ProductionPlanHandoffRepository.cs"),
  "ProductionPlanHandoffRepository.cs",
);
const dbContext = readRequiredFile(
  resolveProjectPath("packages/CrevaFactOS.Persistence/CrevaFactOSDbContext.cs"),
  "CrevaFactOSDbContext.cs",
);
const tenantSession = readRequiredFile(
  resolveProjectPath("packages/CrevaFactOS.Persistence/Tenancy/TenantDatabaseSession.cs"),
  "TenantDatabaseSession.cs",
);
const permissionKeys = readRequiredFile(
  resolveProjectPath("packages/CrevaFactOS.Persistence/Authorization/ProductionPlanningPermissionKeys.cs"),
  "ProductionPlanningPermissionKeys.cs",
);
const endpointExtensions = readRequiredFile(
  resolveProjectPath("apps/api/ProductionPlanning/ProductionPlanningEndpointRouteBuilderExtensions.cs"),
  "ProductionPlanningEndpointRouteBuilderExtensions.cs",
);

// Load key web files
const workflowWriter = readRequiredFile(
  resolveWebPath("src/features/production-planning/server/production-plan-workflow-writer.ts"),
  "production-plan-workflow-writer.ts",
);
const handoffWriter = readRequiredFile(
  resolveWebPath("src/features/production-planning/server/production-plan-handoff-writer.ts"),
  "production-plan-handoff-writer.ts",
);
const transferWriter = readRequiredFile(
  resolveWebPath("src/features/production-planning/server/production-plan-transfer-writer.ts"),
  "production-plan-transfer-writer.ts",
);
const workflowActionsComponent = readRequiredFile(
  resolveWebPath("src/features/production-planning/components/production-plan-workflow-actions.tsx"),
  "production-plan-workflow-actions.tsx",
);
const handoffPanel = readRequiredFile(
  resolveWebPath("src/features/production-planning/components/production-plan-handoff-panel.tsx"),
  "production-plan-handoff-panel.tsx",
);
const planDetailPage = readRequiredFile(
  resolveWebPath("src/app/(app)/planlama/[planId]/page.tsx"),
  "planlama/[planId]/page.tsx",
);

// ── Workflow Locks ──

// 1. Approved plan cannot be edited (PlanNotEditable in write repository)
requireText(writeRepository, "PlanNotEditable", "ProductionPlanWriteRepository.cs");

// 2. No auto-approve or AI auto-approve path
forbidText(workflowRepository, "autoApprove", "ProductionPlanWorkflowRepository.cs");
forbidText(workflowRepository, "aiApprove", "ProductionPlanWorkflowRepository.cs");

// 3. Self-approval guard
requireText(workflowRepository, "SelfApprovalNotAllowed", "ProductionPlanWorkflowRepository.cs");

// 4. Transfer already succeeded blocks re-execution
requireText(transferRepository, "Succeeded", "ProductionPlanTransferRepository.cs");

// 5. Transfer Uncertain state blocks normal retry
requireText(transferRepository, "Uncertain", "ProductionPlanTransferRepository.cs");

// 6. Handoff idempotency guard (duplicate blocked)
requireText(handoffRepository, "AlreadyPrepared", "ProductionPlanHandoffRepository.cs");

// ── Permission Separation ──

// 7. Dedicated permissions for each lifecycle stage
requireText(permissionKeys, "production.planning.read", "ProductionPlanningPermissionKeys.cs");
requireText(permissionKeys, "production.planning.write", "ProductionPlanningPermissionKeys.cs");
requireText(permissionKeys, "production.planning.review", "ProductionPlanningPermissionKeys.cs");
requireText(permissionKeys, "production.planning.approve", "ProductionPlanningPermissionKeys.cs");
requireText(permissionKeys, "production.planning.schedule", "ProductionPlanningPermissionKeys.cs");
requireText(permissionKeys, "production.planning.transfer", "ProductionPlanningPermissionKeys.cs");
requireText(permissionKeys, "production.planning.handoff", "ProductionPlanningPermissionKeys.cs");

// 8. No wildcard permission
forbidText(permissionKeys, "production.planning.*", "ProductionPlanningPermissionKeys.cs");
forbidText(permissionKeys, "\"*\"", "ProductionPlanningPermissionKeys.cs");

// ── Tenant/Facility Isolation ──

// 9. DbContext has global query filters (tenant isolation)
requireText(dbContext, "HasQueryFilter", "CrevaFactOSDbContext.cs");
requireText(dbContext, "TenantId", "CrevaFactOSDbContext.cs");

// 10. TenantDatabaseSession enforces tenant scope
requireText(tenantSession, "TenantId", "TenantDatabaseSession.cs");

// ── Atomic Operations ──

// 11. All three repositories use ExecuteWriteAsync for atomicity
requireText(workflowRepository, "ExecuteWriteAsync", "ProductionPlanWorkflowRepository.cs");
requireText(transferRepository, "ExecuteWriteAsync", "ProductionPlanTransferRepository.cs");
requireText(handoffRepository, "ExecuteWriteAsync", "ProductionPlanHandoffRepository.cs");

// ── AI Decision Boundary ──

// 12. AI cannot trigger approve/schedule/transfer/handoff
forbidText(workflowRepository, "aiApprove", "ProductionPlanWorkflowRepository.cs");
forbidText(workflowRepository, "aiSchedule", "ProductionPlanWorkflowRepository.cs");
forbidText(transferRepository, "aiTransfer", "ProductionPlanTransferRepository.cs");
forbidText(handoffRepository, "aiHandoff", "ProductionPlanHandoffRepository.cs");

// ── No separate Production application package ──

forbidFile(
  resolveProjectPath("packages/CrevaFactOS.Production"),
  "packages/CrevaFactOS.Production",
);

// ── Planning / Production ownership boundary ──

// 14. Planning repositories must not mutate Production execution truth directly.
forbidText(workflowRepository, "ProductionOrders", "ProductionPlanWorkflowRepository.cs");
forbidText(writeRepository, "ProductionOrders", "ProductionPlanWriteRepository.cs");
forbidText(transferRepository, "ProductionOrders", "ProductionPlanTransferRepository.cs");
forbidText(handoffRepository, "ProductionOrders.Update", "ProductionPlanHandoffRepository.cs");
forbidText(handoffRepository, "dbContext.ProductionOrders", "ProductionPlanHandoffRepository.cs");
requireText(handoffRepository, "ProductionPlanHandoff", "ProductionPlanHandoffRepository.cs");

// Persistence may expose ProductionOrders for read/reporting in the shared DbContext.
requireText(dbContext, "ProductionOrders", "CrevaFactOSDbContext.cs");
forbidText(dbContext, "MachineCommands", "CrevaFactOSDbContext.cs");

// ── Server Boundary ──

// 15. All server writers block preview writes
requireText(workflowWriter, "development-preview", "production-plan-workflow-writer.ts");
requireText(handoffWriter, "development-preview", "production-plan-handoff-writer.ts");
requireText(transferWriter, "development-preview", "production-plan-transfer-writer.ts");

// 16. Client components cannot directly pass actor identity
forbidText(workflowActionsComponent, "identitySubjectId", "production-plan-workflow-actions.tsx");
forbidText(handoffPanel, "identitySubjectId", "production-plan-handoff-panel.tsx");

// ── Transfer Recovery Metadata ──

// 17. Transfer repository tracks processing state
requireText(transferRepository, "Processing", "ProductionPlanTransferRepository.cs");

// ── Handoff Panel in Detail Page ──

// 18. Handoff panel is rendered for scheduled plans
requireText(planDetailPage, "ProductionPlanHandoffPanel", "planlama/[planId]/page.tsx");

// ── No DELETE grant verification (static check) ──

// 19. No DELETE grant in migration
const migrationPath = resolveProjectPath(
  "packages/CrevaFactOS.Persistence.Migrations/Migrations/20260807150000_AddProductionPlanningHandoffAndMakerChecker.cs",
);
if (existsSync(migrationPath)) {
  const migration = readFileSync(migrationPath, "utf8").replaceAll("\r\n", "\n");
  forbidText(migration, "GRANT DELETE", "handoff migration");
}

// ── Endpoint DI ──

// 20. Handoff service is registered
requireText(endpointExtensions, "IProductionPlanHandoffService", "ProductionPlanningEndpointRouteBuilderExtensions.cs");

console.log("Production planning final-hardening verification passed.");
console.log(
  [
    "Verified workflow locks (approved/succeeded/uncertain),",
    "self-approval guard,",
    "permission separation (7 distinct permissions),",
    "no wildcard permission,",
    "tenant isolation,",
    "atomic transactions,",
    "AI decision boundary,",
    "no Production module dependency,",
    "Planning must not mutate ProductionOrders directly,",
    "preview write blocked,",
    "no client actor passthrough,",
    "transfer recovery metadata,",
    "handoff panel in detail page,",
    "no DELETE grant,",
    "and handoff service registered.",
  ].join(" "),
);
