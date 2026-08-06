import {
  existsSync,
  readFileSync,
  readdirSync,
} from "node:fs";
import {
  dirname,
  extname,
  relative,
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

const featureRoot =
  resolve(
    webRoot,
    "src/features/production-planning",
  );

const previewImportToken =
  "production-plan-preview-data";

const previewRepositoryImportToken =
  "production-plan-preview-repository";

const productionPlanReaderImportToken =
  "production-plan-reader";

const allowedPreviewDataConsumers = new Set([
  "src/features/production-planning/server/production-plan-repository-factory.ts",
]);

const allowedPreviewRepositoryConsumers =
  new Set([
    "src/features/production-planning/server/production-plan-repository-factory.ts",
  ]);

function fail(message) {
  console.error(
    `Production planning preview isolation verification failed: ${message}`,
  );

  process.exit(1);
}

function toPosixPath(filePath) {
  return filePath.replaceAll("\\", "/");
}

function resolveWebPath(relativePath) {
  return resolve(
    webRoot,
    relativePath,
  );
}

function readRequiredFile(relativePath) {
  const filePath =
    resolveWebPath(relativePath);

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

function collectSourceFiles(directoryPath) {
  const collectedFiles = [];

  for (
    const directoryEntry of
    readdirSync(
      directoryPath,
      {
        withFileTypes: true,
      },
    )
  ) {
    const entryPath =
      resolve(
        directoryPath,
        directoryEntry.name,
      );

    if (directoryEntry.isDirectory()) {
      collectedFiles.push(
        ...collectSourceFiles(entryPath),
      );

      continue;
    }

    if (
      [".ts", ".tsx", ".js", ".mjs"].includes(
        extname(directoryEntry.name),
      )
    ) {
      collectedFiles.push(entryPath);
    }
  }

  return collectedFiles;
}

const previewData =
  readRequiredFile(
    "src/features/production-planning/data/production-plan-preview-data.ts",
  );

const previewRepository =
  readRequiredFile(
    "src/features/production-planning/data/production-plan-preview-repository.ts",
  );

const repositoryContract =
  readRequiredFile(
    "src/features/production-planning/application/production-plan-repository.ts",
  );

const listPage =
  readRequiredFile(
    "src/app/(app)/planlama/page.tsx",
  );

const detailPage =
  readRequiredFile(
    "src/app/(app)/planlama/[planId]/page.tsx",
  );

const listQuery =
  readRequiredFile(
    "src/features/production-planning/application/production-plan-query.ts",
  );

const detailQuery =
  readRequiredFile(
    "src/features/production-planning/application/production-plan-detail-query.ts",
  );

const scopeResolver =
  readRequiredFile(
    "src/features/production-planning/server/production-plan-scope.ts",
  );

const repositoryFactory =
  readRequiredFile(
    "src/features/production-planning/server/production-plan-repository-factory.ts",
  );

const productionPlanReader =
  readRequiredFile(
    "src/features/production-planning/server/production-plan-reader.ts",
  );

requireText(
  previewData,
  "PRODUCTION_PLAN_PREVIEW_DATA",
  "production-plan-preview-data.ts",
);

requireText(
  repositoryContract,
  "export interface ProductionPlanRepository",
  "production-plan-repository.ts",
);

requireText(
  repositoryContract,
  "list(",
  "production-plan-repository.ts",
);

requireText(
  repositoryContract,
  "findById(",
  "production-plan-repository.ts",
);

forbidText(
  repositoryContract,
  "async create(",
  "production-plan-repository.ts",
);

forbidText(
  repositoryContract,
  "async update(",
  "production-plan-repository.ts",
);

forbidText(
  repositoryContract,
  "async delete(",
  "production-plan-repository.ts",
);

forbidText(
  repositoryContract,
  "writeProductionPlan",
  "production-plan-repository.ts",
);

requireText(
  previewRepository,
  "createProductionPlanPreviewRepository",
  "production-plan-preview-repository.ts",
);

requireText(
  previewRepository,
  "ProductionPlanRepository",
  "production-plan-preview-repository.ts",
);

requireText(
  previewRepository,
  "async list(",
  "production-plan-preview-repository.ts",
);

requireText(
  previewRepository,
  "async findById(",
  "production-plan-preview-repository.ts",
);

requireText(
  previewRepository,
  "invalid-scope",
  "production-plan-preview-repository.ts",
);

requireText(
  previewRepository,
  "Multiple plans use id",
  "production-plan-preview-repository.ts",
);

forbidText(
  previewRepository,
  previewImportToken,
  "production-plan-preview-repository.ts",
);

forbidText(
  previewRepository,
  "async create(",
  "production-plan-preview-repository.ts",
);

forbidText(
  previewRepository,
  "async update(",
  "production-plan-preview-repository.ts",
);

forbidText(
  previewRepository,
  "async delete(",
  "production-plan-preview-repository.ts",
);

forbidText(
  previewRepository,
  "writeProductionPlan",
  "production-plan-preview-repository.ts",
);

requireText(
  scopeResolver,
  "resolveProductionPlanningServerScope",
  "production-plan-scope.ts",
);

requireText(
  scopeResolver,
  "tenantId",
  "production-plan-scope.ts",
);

requireText(
  scopeResolver,
  "facilityId",
  "production-plan-scope.ts",
);

requireText(
  scopeResolver,
  "preview-fallback",
  "production-plan-scope.ts",
);

requireText(
  repositoryFactory,
  "createProductionPlanRepository",
  "production-plan-repository-factory.ts",
);

requireText(
  repositoryFactory,
  "ProductionPlanRepository",
  "production-plan-repository-factory.ts",
);

requireText(
  repositoryFactory,
  previewImportToken,
  "production-plan-repository-factory.ts",
);

requireText(
  repositoryFactory,
  "PRODUCTION_PLAN_PREVIEW_DATA",
  "production-plan-repository-factory.ts",
);

requireText(
  repositoryFactory,
  previewRepositoryImportToken,
  "production-plan-repository-factory.ts",
);

requireText(
  productionPlanReader,
  "readProductionPlanningWorkspace",
  "production-plan-reader.ts",
);

requireText(
  productionPlanReader,
  "readProductionPlanningDetail",
  "production-plan-reader.ts",
);

requireText(
  productionPlanReader,
  "resolveProductionPlanningServerScope",
  "production-plan-reader.ts",
);

requireText(
  productionPlanReader,
  "createProductionPlanRepository",
  "production-plan-reader.ts",
);

forbidText(
  productionPlanReader,
  previewImportToken,
  "production-plan-reader.ts",
);

forbidText(
  productionPlanReader,
  previewRepositoryImportToken,
  "production-plan-reader.ts",
);

forbidText(
  scopeResolver,
  previewImportToken,
  "production-plan-scope.ts",
);

requireText(
  listPage,
  productionPlanReaderImportToken,
  "planlama/page.tsx",
);

requireText(
  listPage,
  "readProductionPlanningWorkspace",
  "planlama/page.tsx",
);

requireText(
  listPage,
  'workspaceResult.status === "failure"',
  "planlama/page.tsx",
);

requireText(
  listPage,
  "productionPlans={productionPlans}",
  "planlama/page.tsx",
);

forbidText(
  listPage,
  previewImportToken,
  "planlama/page.tsx",
);

forbidText(
  listPage,
  previewRepositoryImportToken,
  "planlama/page.tsx",
);

forbidText(
  listPage,
  "PRODUCTION_PLAN_PREVIEW_DATA",
  "planlama/page.tsx",
);

forbidText(
  listPage,
  "createProductionPlanPreviewRepository",
  "planlama/page.tsx",
);

requireText(
  detailPage,
  productionPlanReaderImportToken,
  "planlama/[planId]/page.tsx",
);

requireText(
  detailPage,
  "readProductionPlanningDetail",
  "planlama/[planId]/page.tsx",
);

requireText(
  detailPage,
  'result.status === "failure"',
  "planlama/[planId]/page.tsx",
);

forbidText(
  detailPage,
  previewImportToken,
  "planlama/[planId]/page.tsx",
);

forbidText(
  detailPage,
  previewRepositoryImportToken,
  "planlama/[planId]/page.tsx",
);

forbidText(
  detailPage,
  "PRODUCTION_PLAN_PREVIEW_DATA",
  "planlama/[planId]/page.tsx",
);

forbidText(
  detailPage,
  "createProductionPlanPreviewRepository",
  "planlama/[planId]/page.tsx",
);

requireText(
  listQuery,
  "productionPlans",
  "production-plan-query.ts",
);

requireText(
  listQuery,
  "readProductionPlans",
  "production-plan-query.ts",
);

requireText(
  listQuery,
  "ProductionPlanRepository",
  "production-plan-query.ts",
);

requireText(
  detailQuery,
  "productionPlans",
  "production-plan-detail-query.ts",
);

requireText(
  detailQuery,
  "readProductionPlanDetail",
  "production-plan-detail-query.ts",
);

requireText(
  detailQuery,
  "ProductionPlanRepository",
  "production-plan-detail-query.ts",
);

forbidText(
  listQuery,
  previewImportToken,
  "production-plan-query.ts",
);

forbidText(
  detailQuery,
  previewImportToken,
  "production-plan-detail-query.ts",
);

forbidText(
  listQuery,
  previewRepositoryImportToken,
  "production-plan-query.ts",
);

forbidText(
  detailQuery,
  previewRepositoryImportToken,
  "production-plan-detail-query.ts",
);

forbidText(
  scopeResolver,
  previewRepositoryImportToken,
  "production-plan-scope.ts",
);

forbidText(
  productionPlanReader,
  previewImportToken,
  "production-plan-reader.ts",
);

const featureSourceFiles =
  collectSourceFiles(featureRoot);

for (
  const sourceFilePath of
  featureSourceFiles
) {
  const relativePath =
    toPosixPath(
      relative(
        webRoot,
        sourceFilePath,
      ),
    );

  if (
    relativePath ===
    "src/features/production-planning/data/production-plan-preview-data.ts"
  ) {
    continue;
  }

  if (
    allowedPreviewDataConsumers.has(
      relativePath,
    )
  ) {
    continue;
  }

  const source =
    readFileSync(
      sourceFilePath,
      "utf8",
    ).replaceAll("\r\n", "\n");

  if (
    source.includes(previewImportToken)
  ) {
    fail(
      `${relativePath} must not depend directly on preview data.`,
    );
  }
}

const appRouteFiles = [
  "src/app/(app)/planlama/page.tsx",
  "src/app/(app)/planlama/[planId]/page.tsx",
];

for (
  const routeFile of
  appRouteFiles
) {
  const routeSource =
    readRequiredFile(routeFile);

  forbidText(
    routeSource,
    previewImportToken,
    routeFile,
  );

  forbidText(
    routeSource,
    previewRepositoryImportToken,
    routeFile,
  );

  forbidText(
    routeSource,
    "PRODUCTION_PLAN_PREVIEW_DATA",
    routeFile,
  );

  forbidText(
    routeSource,
    "createProductionPlanPreviewRepository",
    routeFile,
  );

  requireText(
    routeSource,
    productionPlanReaderImportToken,
    routeFile,
  );
}

if (
  !allowedPreviewDataConsumers.has(
    "src/features/production-planning/server/production-plan-repository-factory.ts",
  )
) {
  fail(
    "Preview data factory consumer configuration is missing.",
  );
}

const forbiddenPreviewInfrastructureTerms = [
  "ProductionPlanRepository",
  "productionPlanRepository",
  "databaseClient",
  "prisma.",
  "supabase.",
  "drizzle.",
  "writeProductionPlan",
  "saveProductionPlan",
  "persistProductionPlan",
];

for (
  const forbiddenTerm of
  forbiddenPreviewInfrastructureTerms
) {
  forbidText(
    previewData,
    forbiddenTerm,
    "production-plan-preview-data.ts",
  );
}

console.log(
  "Production planning preview isolation verification passed.",
);

console.log(
  [
    "Verified server composition boundary,",
    "route-only reader usage,",
    "factory-only preview data consumption,",
    "read-only preview repository,",
    "query input boundaries,",
    "and feature-layer isolation.",
  ].join(" "),
);
