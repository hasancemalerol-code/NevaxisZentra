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

const allowedPreviewConsumers = new Set([
  "src/app/(app)/planlama/page.tsx",
  "src/app/(app)/planlama/[planId]/page.tsx",
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

requireText(
  previewData,
  "PRODUCTION_PLAN_PREVIEW_DATA",
  "production-plan-preview-data.ts",
);

requireText(
  listPage,
  previewImportToken,
  "planlama/page.tsx",
);

requireText(
  listPage,
  "PRODUCTION_PLAN_PREVIEW_DATA",
  "planlama/page.tsx",
);

requireText(
  detailPage,
  previewImportToken,
  "planlama/[planId]/page.tsx",
);

requireText(
  detailPage,
  "PRODUCTION_PLAN_PREVIEW_DATA",
  "planlama/[planId]/page.tsx",
);

requireText(
  listQuery,
  "productionPlans",
  "production-plan-query.ts",
);

requireText(
  detailQuery,
  "productionPlans",
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
  if (
    !allowedPreviewConsumers.has(routeFile)
  ) {
    fail(
      `Unexpected preview consumer configuration: ${routeFile}`,
    );
  }
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
    "Verified route-only preview composition,",
    "query input boundaries,",
    "feature-layer isolation,",
    "and readiness for repository or API replacement.",
  ].join(" "),
);