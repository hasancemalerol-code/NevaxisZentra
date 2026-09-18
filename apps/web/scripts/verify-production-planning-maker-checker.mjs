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
    `Production planning maker-checker verification failed: ${message}`,
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

console.log("Running production planning maker-checker verifier.");

// Backend: ProductionPlan entity
const productionPlan = readRequiredFile(
  resolveProjectPath("packages/NevaxisZentra.Persistence/ProductionPlanning/ProductionPlan.cs"),
  "ProductionPlan.cs",
);
const workflowRepository = readRequiredFile(
  resolveProjectPath("packages/NevaxisZentra.Persistence/ProductionPlanning/ProductionPlanWorkflowRepository.cs"),
  "ProductionPlanWorkflowRepository.cs",
);
const writeRepository = readRequiredFile(
  resolveProjectPath("packages/NevaxisZentra.Persistence/ProductionPlanning/ProductionPlanWriteRepository.cs"),
  "ProductionPlanWriteRepository.cs",
);
const workflowModels = readRequiredFile(
  resolveProjectPath("packages/NevaxisZentra.Persistence/ProductionPlanning/ProductionPlanWorkflowModels.cs"),
  "ProductionPlanWorkflowModels.cs",
);
const apiResultMapper = readRequiredFile(
  resolveProjectPath("apps/api/ProductionPlanning/ProductionPlanApiResultMapper.cs"),
  "ProductionPlanApiResultMapper.cs",
);
const developmentPilotIds = readRequiredFile(
  resolveProjectPath("packages/NevaxisZentra.Persistence/Development/DevelopmentPilotIds.cs"),
  "DevelopmentPilotIds.cs",
);
const bootstrapService = readRequiredFile(
  resolveProjectPath("packages/NevaxisZentra.Persistence/Development/DevelopmentPilotBootstrapService.cs"),
  "DevelopmentPilotBootstrapService.cs",
);

// Web: workflow actions component
const workflowActionsComponent = readRequiredFile(
  resolveWebPath("src/features/production-planning/components/production-plan-workflow-actions.tsx"),
  "production-plan-workflow-actions.tsx",
);

// 1. LastModifiedBySubjectId exists on ProductionPlan entity
requireText(productionPlan, "LastModifiedBySubjectId", "ProductionPlan.cs");

// 2. LastModifiedBySubjectId is nullable Guid
requireText(productionPlan, "Guid?", "ProductionPlan.cs");

// 3. Self-approval guard exists in ApproveAsync
requireText(workflowRepository, "LastModifiedBySubjectId", "ProductionPlanWorkflowRepository.cs");
requireText(workflowRepository, "SelfApprovalNotAllowed", "ProductionPlanWorkflowRepository.cs");

// 4. Self-approval check uses IdentitySubjectId from context (not from client)
requireText(workflowRepository, "context.IdentitySubjectId", "ProductionPlanWorkflowRepository.cs");

// 5. Self-approval check happens inside the transaction / repository method
requireText(workflowRepository, "ApproveAsync", "ProductionPlanWorkflowRepository.cs");

// 6. Write repository sets LastModifiedBySubjectId on create
requireText(writeRepository, "LastModifiedBySubjectId", "ProductionPlanWriteRepository.cs");
requireText(writeRepository, "context.IdentitySubjectId", "ProductionPlanWriteRepository.cs");

// 7. Failure code is declared
requireText(workflowModels, "SelfApprovalNotAllowed", "ProductionPlanWorkflowModels.cs");

// 8. API maps SelfApprovalNotAllowed to a Conflict response
requireText(apiResultMapper, "SelfApprovalNotAllowed", "ProductionPlanApiResultMapper.cs");
requireText(apiResultMapper, "self-approval-not-allowed", "ProductionPlanApiResultMapper.cs");

// 9. No development identity bypass for self-approval
forbidText(workflowRepository, "IsDevelopment", "ProductionPlanWorkflowRepository.cs");
forbidText(workflowRepository, "skipMakerChecker", "ProductionPlanWorkflowRepository.cs");

// 10. Actor is not set from client body
forbidText(workflowRepository, "body.SubjectId", "ProductionPlanWorkflowRepository.cs");
forbidText(workflowRepository, "request.SubjectId", "ProductionPlanWorkflowRepository.cs");

// 11. Second development identity (reviewer) exists
requireText(developmentPilotIds, "ReviewerIdentitySubjectId", "DevelopmentPilotIds.cs");
requireText(developmentPilotIds, "ReviewerTenantMembershipId", "DevelopmentPilotIds.cs");
requireText(developmentPilotIds, "ReviewerTenantRoleId", "DevelopmentPilotIds.cs");

// 12. Bootstrap service sets up the reviewer identity
requireText(bootstrapService, "Reviewer", "DevelopmentPilotBootstrapService.cs");

// 13. Web UI shows maker-checker error message in Turkish
requireText(workflowActionsComponent, "self-approval-not-allowed", "production-plan-workflow-actions.tsx");
requireText(workflowActionsComponent, "Bu planı son düzenleyen kullanıcı", "production-plan-workflow-actions.tsx");

// 14. Actor identity not passed from client component (backend is authoritative)
forbidText(workflowActionsComponent, "actorId", "production-plan-workflow-actions.tsx");
forbidText(workflowActionsComponent, "identitySubjectId", "production-plan-workflow-actions.tsx");
forbidText(workflowActionsComponent, "lastModifiedBySubjectId", "production-plan-workflow-actions.tsx");

console.log("Production planning maker-checker verification passed.");
console.log(
  [
    "Verified LastModifiedBySubjectId on ProductionPlan,",
    "self-approval guard in ApproveAsync,",
    "SelfApprovalNotAllowed failure code,",
    "API maps to 409 Conflict,",
    "no development bypass,",
    "second reviewer identity in bootstrap,",
    "and web UI shows Turkish maker-checker message.",
  ].join(" "),
);
