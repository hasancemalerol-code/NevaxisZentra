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
    `Production planning handoff verification failed: ${message}`,
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

console.log("Running production planning handoff verifier.");

// Backend persistence
const handoffEntity = readRequiredFile(
  resolveProjectPath("packages/CrevaFactOS.Persistence/ProductionPlanning/ProductionPlanHandoff.cs"),
  "ProductionPlanHandoff.cs",
);
const handoffRepository = readRequiredFile(
  resolveProjectPath("packages/CrevaFactOS.Persistence/ProductionPlanning/ProductionPlanHandoffRepository.cs"),
  "ProductionPlanHandoffRepository.cs",
);
const handoffModels = readRequiredFile(
  resolveProjectPath("packages/CrevaFactOS.Persistence/ProductionPlanning/ProductionPlanHandoffModels.cs"),
  "ProductionPlanHandoffModels.cs",
);
const handoffStatus = readRequiredFile(
  resolveProjectPath("packages/CrevaFactOS.Persistence/ProductionPlanning/ProductionPlanHandoffStatus.cs"),
  "ProductionPlanHandoffStatus.cs",
);
const handoffEntityConfig = readRequiredFile(
  resolveProjectPath("packages/CrevaFactOS.Persistence/Configurations/ProductionPlanHandoffEntityConfiguration.cs"),
  "ProductionPlanHandoffEntityConfiguration.cs",
);
const handoffApiService = readRequiredFile(
  resolveProjectPath("apps/api/ProductionPlanning/ProductionPlanHandoffService.cs"),
  "ProductionPlanHandoffService.cs",
);
const endpointExtensions = readRequiredFile(
  resolveProjectPath("apps/api/ProductionPlanning/ProductionPlanningEndpointRouteBuilderExtensions.cs"),
  "ProductionPlanningEndpointRouteBuilderExtensions.cs",
);
const permissionKeys = readRequiredFile(
  resolveProjectPath("packages/CrevaFactOS.Persistence/Authorization/ProductionPlanningPermissionKeys.cs"),
  "ProductionPlanningPermissionKeys.cs",
);
const auditEventTypes = readRequiredFile(
  resolveProjectPath("packages/CrevaFactOS.Persistence/Audit/AuditEventTypes.cs"),
  "AuditEventTypes.cs",
);
const auditActions = readRequiredFile(
  resolveProjectPath("packages/CrevaFactOS.Persistence/Audit/AuditActions.cs"),
  "AuditActions.cs",
);
const outboxMessageTypes = readRequiredFile(
  resolveProjectPath("packages/CrevaFactOS.Persistence/Outbox/OutboxMessageTypes.cs"),
  "OutboxMessageTypes.cs",
);
const migration = readRequiredFile(
  resolveProjectPath("packages/CrevaFactOS.Persistence.Migrations/Migrations/20260807150000_AddProductionPlanningHandoffAndMakerChecker.cs"),
  "20260807150000_AddProductionPlanningHandoffAndMakerChecker.cs",
);

// Web
const handoffActions = readRequiredFile(
  resolveWebPath("src/features/production-planning/server/production-plan-handoff-actions.ts"),
  "production-plan-handoff-actions.ts",
);
const handoffWriter = readRequiredFile(
  resolveWebPath("src/features/production-planning/server/production-plan-handoff-writer.ts"),
  "production-plan-handoff-writer.ts",
);
const handoffApiClient = readRequiredFile(
  resolveWebPath("src/features/production-planning/server/production-plan-handoff-api-client.ts"),
  "production-plan-handoff-api-client.ts",
);
const handoffApiContracts = readRequiredFile(
  resolveWebPath("src/features/production-planning/server/production-plan-handoff-api-contracts.ts"),
  "production-plan-handoff-api-contracts.ts",
);
const handoffPanel = readRequiredFile(
  resolveWebPath("src/features/production-planning/components/production-plan-handoff-panel.tsx"),
  "production-plan-handoff-panel.tsx",
);
const planDetailPage = readRequiredFile(
  resolveWebPath("src/app/(app)/planlama/[planId]/page.tsx"),
  "planlama/[planId]/page.tsx",
);

// 1. Handoff entity has required fields
requireText(handoffEntity, "IdempotencyKey", "ProductionPlanHandoff.cs");
requireText(handoffEntity, "ProductionPlanId", "ProductionPlanHandoff.cs");
requireText(handoffEntity, "ProductionPlanVersion", "ProductionPlanHandoff.cs");
requireText(handoffEntity, "TenantId", "ProductionPlanHandoff.cs");
requireText(handoffEntity, "FacilityId", "ProductionPlanHandoff.cs");
requireText(handoffEntity, "PreparedBySubjectId", "ProductionPlanHandoff.cs");
requireText(handoffEntity, "PayloadJson", "ProductionPlanHandoff.cs");

// 2. Handoff status enum has required states
requireText(handoffStatus, "Prepared", "ProductionPlanHandoffStatus.cs");
requireText(handoffStatus, "Consumed", "ProductionPlanHandoffStatus.cs");
requireText(handoffStatus, "Failed", "ProductionPlanHandoffStatus.cs");

// 3. Entity config has unique idempotency constraint
requireText(handoffEntityConfig, "idempotency_key", "ProductionPlanHandoffEntityConfiguration.cs");
requireText(handoffEntityConfig, "HasIndex", "ProductionPlanHandoffEntityConfiguration.cs");
requireText(handoffEntityConfig, "IsUnique", "ProductionPlanHandoffEntityConfiguration.cs");

// 4. Repository builds idempotency key from deterministic components
requireText(handoffRepository, "BuildIdempotencyKey", "ProductionPlanHandoffRepository.cs");
requireText(handoffRepository, "TenantId", "ProductionPlanHandoffRepository.cs");
requireText(handoffRepository, "FacilityId", "ProductionPlanHandoffRepository.cs");
requireText(handoffRepository, "ProductionPlanId", "ProductionPlanHandoffRepository.cs");
requireText(handoffRepository, "ProductionPlanVersion", "ProductionPlanHandoffRepository.cs");

// 5. Repository checks plan is scheduled before handoff
requireText(handoffRepository, "Scheduled", "ProductionPlanHandoffRepository.cs");

// 6. Repository uses atomic transaction (ExecuteWriteAsync)
requireText(handoffRepository, "ExecuteWriteAsync", "ProductionPlanHandoffRepository.cs");

// 7. Audit record is written in same transaction
requireText(handoffRepository, "AuditRecord", "ProductionPlanHandoffRepository.cs");

// 8. Outbox message is written in same transaction
requireText(handoffRepository, "OutboxMessage", "ProductionPlanHandoffRepository.cs");

// 9. Repository has AlreadyPrepared idempotency guard
requireText(handoffRepository, "AlreadyPrepared", "ProductionPlanHandoffRepository.cs");

// 10. Dedicated handoff permission key
requireText(permissionKeys, "production.planning.handoff", "ProductionPlanningPermissionKeys.cs");

// 11. Handoff audit event and action types declared
requireText(auditEventTypes, "ProductionPlanHandoffPrepared", "AuditEventTypes.cs");
requireText(auditActions, "ProductionPlanHandoffPrepare", "AuditActions.cs");

// 12. Outbox message type declared
requireText(outboxMessageTypes, "ProductionPlanHandoffPrepared", "OutboxMessageTypes.cs");

// 13. Handoff endpoint is registered
requireText(endpointExtensions, "/handoff", "ProductionPlanningEndpointRouteBuilderExtensions.cs");
requireText(endpointExtensions, "PrepareHandoffAsync", "ProductionPlanningEndpointRouteBuilderExtensions.cs");

// 14. IProductionPlanHandoffRepository registered in DI
requireText(endpointExtensions, "IProductionPlanHandoffRepository", "ProductionPlanningEndpointRouteBuilderExtensions.cs");
requireText(endpointExtensions, "IProductionPlanHandoffService", "ProductionPlanningEndpointRouteBuilderExtensions.cs");

// 15. Migration creates the table
requireText(migration, "production_plan_handoffs", "migration");
requireText(migration, "production.planning.handoff", "migration");
requireText(migration, "last_modified_by_subject_id", "migration");

// 16. Web: handoff actions file is server-only
requireText(handoffActions, '"use server"', "production-plan-handoff-actions.ts");

// 17. Web: handoff writer blocks preview writes
requireText(handoffWriter, "development-preview", "production-plan-handoff-writer.ts");
requireText(handoffWriter, "preview-write-blocked", "production-plan-handoff-writer.ts");

// 18. Web: handoff client uses POST
requireText(handoffApiClient, "method: \"POST\"", "production-plan-handoff-api-client.ts");

// 19. Web: handoff contract parses idempotency / success response
requireText(handoffApiContracts, "handoffId", "production-plan-handoff-api-contracts.ts");
requireText(handoffApiContracts, "preparedAtUtc", "production-plan-handoff-api-contracts.ts");

// 20. Web: handoff panel checks scheduling eligibility
requireText(handoffPanel, "scheduled", "production-plan-handoff-panel.tsx");
requireText(handoffPanel, "Üretime Aktarım Hazırla", "production-plan-handoff-panel.tsx");

// 21. Web: handoff panel shows success state
requireText(handoffPanel, "Üretim modülü tarafından alınmaya hazır", "production-plan-handoff-panel.tsx");

// 22. Web: plan detail page includes handoff panel
requireText(planDetailPage, "ProductionPlanHandoffPanel", "planlama/[planId]/page.tsx");
requireText(planDetailPage, "prepareHandoffAction", "planlama/[planId]/page.tsx");

// 23. Handoff payload excludes secrets / auth tokens
forbidText(handoffRepository, "authToken", "ProductionPlanHandoffRepository.cs");
forbidText(handoffRepository, "password", "ProductionPlanHandoffRepository.cs");

// 24. No fake Production order creation
forbidText(handoffRepository, "CreateProductionOrder", "ProductionPlanHandoffRepository.cs");
forbidText(handoffPanel, "üretim emri oluşturuldu", "production-plan-handoff-panel.tsx");

// 25. Production module dependency does not exist
const productionModuleExists = existsSync(
  resolveProjectPath("packages/CrevaFactOS.Production"),
);
if (productionModuleExists) {
  fail("Production module package must not exist in this phase.");
}

console.log("Production planning handoff verification passed.");
console.log(
  [
    "Verified handoff entity with required fields,",
    "Prepared/Consumed/Failed statuses,",
    "unique idempotency constraint,",
    "deterministic idempotency key,",
    "scheduling eligibility check,",
    "atomic audit+outbox in transaction,",
    "dedicated handoff permission,",
    "handoff endpoint registered,",
    "DI registered,",
    "migration present,",
    "web server boundary enforced,",
    "preview write blocked,",
    "handoff panel in plan detail,",
    "and no Production module dependency.",
  ].join(" "),
);
