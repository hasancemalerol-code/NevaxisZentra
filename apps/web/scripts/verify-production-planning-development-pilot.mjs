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

const currentFilePath =
  fileURLToPath(import.meta.url);

const scriptsDirectory =
  dirname(currentFilePath);

const webRoot =
  resolve(scriptsDirectory, "..");

const repositoryRoot =
  resolve(webRoot, "../..");

function fail(message) {
  console.error(
    `Production planning development pilot verification failed: ${message}`,
  );

  process.exit(1);
}

function readRequiredFile(relativePath) {
  const filePath =
    resolve(webRoot, relativePath);

  if (!existsSync(filePath)) {
    fail(
      `Required file is missing: ${relativePath}`,
    );
  }

  return readFileSync(
    filePath,
    "utf8",
  ).replaceAll("\r\n", "\n");
}

function readRepositoryFile(relativePath) {
  const filePath =
    resolve(
      repositoryRoot,
      relativePath,
    );

  if (!existsSync(filePath)) {
    fail(
      `Required repository file is missing: ${relativePath}`,
    );
  }

  return readFileSync(
    filePath,
    "utf8",
  ).replaceAll("\r\n", "\n");
}

function requireText(
  source,
  expectedText,
  fileLabel,
) {
  if (!source.includes(expectedText)) {
    fail(
      `${fileLabel} must contain "${expectedText}".`,
    );
  }
}

function forbidText(
  source,
  forbiddenText,
  fileLabel,
) {
  if (source.includes(forbiddenText)) {
    fail(
      `${fileLabel} must not contain "${forbiddenText}".`,
    );
  }
}

const bootstrapService =
  readRepositoryFile(
    "packages/NevaxisZentra.Persistence/Development/DevelopmentPilotBootstrapService.cs",
  );

const bootstrapExtensions =
  readRepositoryFile(
    "packages/NevaxisZentra.Persistence/Development/DevelopmentPilotServiceCollectionExtensions.cs",
  );

const hostedBootstrap =
  readRepositoryFile(
    "apps/api/Development/DevelopmentPilotHostedBootstrapService.cs",
  );

const planSeed =
  readRepositoryFile(
    "packages/NevaxisZentra.Persistence/Development/DevelopmentPilotProductionPlanSeed.cs",
  );

const programCs =
  readRepositoryFile("apps/api/Program.cs");

const apiContracts =
  readRequiredFile(
    "src/features/production-planning/server/production-plan-api-contracts.ts",
  );

const apiMapper =
  readRequiredFile(
    "src/features/production-planning/server/production-plan-api-mapper.ts",
  );

const cardComponent =
  readRequiredFile(
    "src/features/production-planning/components/production-plan-card.tsx",
  );

const repositoryFactory =
  readRequiredFile(
    "src/features/production-planning/server/production-plan-repository-factory.ts",
  );

requireText(
  bootstrapExtensions,
  "ValidateDevelopmentPilotProductionGuard",
  "DevelopmentPilotServiceCollectionExtensions.cs",
);

requireText(
  bootstrapExtensions,
  "environment.IsDevelopment()",
  "DevelopmentPilotServiceCollectionExtensions.cs",
);

requireText(
  bootstrapExtensions,
  "Development pilot bootstrap cannot be enabled outside the Development environment",
  "DevelopmentPilotServiceCollectionExtensions.cs",
);

requireText(
  hostedBootstrap,
  "!environment.IsDevelopment() || !options.Value.Enabled",
  "DevelopmentPilotHostedBootstrapService.cs",
);

requireText(
  programCs,
  "ValidateDevelopmentPilotProductionGuard",
  "Program.cs",
);

requireText(
  bootstrapService,
  "ProductionPlanningPermissionKeys.Read",
  "DevelopmentPilotBootstrapService.cs",
);

forbidText(
  bootstrapService,
  "production.planning.write",
  "DevelopmentPilotBootstrapService.cs",
);

forbidText(
  bootstrapService,
  "production.planning.manage",
  "DevelopmentPilotBootstrapService.cs",
);

requireText(
  bootstrapService,
  "EnsureFacilityMembershipAsync",
  "DevelopmentPilotBootstrapService.cs",
);

requireText(
  planSeed,
  "DevelopmentPilotProductionPlanSeed",
  "DevelopmentPilotProductionPlanSeed.cs",
);

requireText(
  apiContracts,
  "demandCount: number",
  "production-plan-api-contracts.ts",
);

requireText(
  apiContracts,
  "blockingRiskCount: number",
  "production-plan-api-contracts.ts",
);

requireText(
  apiContracts,
  "hasBlockingRisk: boolean",
  "production-plan-api-contracts.ts",
);

requireText(
  apiMapper,
  "listSummary:",
  "production-plan-api-mapper.ts",
);

requireText(
  cardComponent,
  "listSummary",
  "production-plan-card.tsx",
);

requireText(
  readRequiredFile(
    "src/features/production-planning/server/production-plan-repository-factory.ts",
  ),
  "PRODUCTION_PLAN_PREVIEW_DATA",
  "production-plan-repository-factory.ts",
);

forbidText(
  cardComponent,
  "PRODUCTION_PLAN_PREVIEW_DATA",
  "production-plan-card.tsx",
);

console.log(
  "Production planning development pilot verification passed.",
);
