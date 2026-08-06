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

const serverRoot =
  resolve(featureRoot, "server");

const requiredServerFiles = [
  "src/features/production-planning/server/production-plan-api-client.ts",
  "src/features/production-planning/server/production-plan-api-contracts.ts",
  "src/features/production-planning/server/production-plan-api-mapper.ts",
  "src/features/production-planning/server/production-plan-api-repository.ts",
  "src/features/production-planning/server/production-plan-api-errors.ts",
  "src/features/production-planning/server/production-plan-runtime-config.ts",
  "src/features/production-planning/server/production-plan-request-context.ts",
  "src/features/production-planning/server/production-plan-reader.ts",
  "src/features/production-planning/server/production-plan-repository-factory.ts",
];

const routeFiles = [
  "src/app/(app)/planlama/page.tsx",
  "src/app/(app)/planlama/[planId]/page.tsx",
];

function fail(message) {
  console.error(
    `Production planning API bridge verification failed: ${message}`,
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

for (const requiredFile of requiredServerFiles) {
  readRequiredFile(requiredFile);
}

const apiClient =
  readRequiredFile(
    "src/features/production-planning/server/production-plan-api-client.ts",
  );

const apiMapper =
  readRequiredFile(
    "src/features/production-planning/server/production-plan-api-mapper.ts",
  );

const apiRepository =
  readRequiredFile(
    "src/features/production-planning/server/production-plan-api-repository.ts",
  );

const reader =
  readRequiredFile(
    "src/features/production-planning/server/production-plan-reader.ts",
  );

const factory =
  readRequiredFile(
    "src/features/production-planning/server/production-plan-repository-factory.ts",
  );

requireText(
  apiClient,
  "production-plan-server-boundary",
  "production-plan-api-client.ts",
);

requireText(
  apiClient,
  "/api/production-planning/plans",
  "production-plan-api-client.ts",
);

requireText(
  apiClient,
  "facilityId",
  "production-plan-api-client.ts",
);

requireText(
  apiClient,
  "URLSearchParams",
  "production-plan-api-client.ts",
);

requireText(
  apiClient,
  'cache: "no-store"',
  "production-plan-api-client.ts",
);

requireText(
  apiClient,
  "normalizeProductionPlanListQueryValue",
  "production-plan-api-client.ts",
);

requireText(
  apiClient,
  "normalizeProductionPlanListLimit",
  "production-plan-api-client.ts",
);

requireText(
  apiMapper,
  "mapProductionPlanSummaryApiDtoToDomain",
  "production-plan-api-mapper.ts",
);

requireText(
  apiMapper,
  "mapProductionPlanDetailApiDtoToDomain",
  "production-plan-api-mapper.ts",
);

forbidText(
  apiMapper,
  "as ProductionPlan",
  "production-plan-api-mapper.ts",
);

requireText(
  apiRepository,
  "createProductionPlanApiRepository",
  "production-plan-api-repository.ts",
);

requireText(
  factory,
  "development-preview",
  "production-plan-repository-factory.ts",
);

requireText(
  factory,
  "createProductionPlanApiRepository",
  "production-plan-repository-factory.ts",
);

requireText(
  reader,
  "parseProductionPlanRuntimeConfig",
  "production-plan-reader.ts",
);

forbidText(
  reader,
  "production-plan-api-client",
  "production-plan-reader.ts",
);

for (const routeFile of routeFiles) {
  const routeSource =
    readRequiredFile(routeFile);

  requireText(
    routeSource,
    "production-plan-reader",
    routeFile,
  );

  forbidText(
    routeSource,
    "production-plan-api-client",
    routeFile,
  );

  forbidText(
    routeSource,
    "production-plan-preview-data",
    routeFile,
  );

  forbidText(
    routeSource,
    "production-plan-runtime-config",
    routeFile,
  );
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
      [".ts", ".tsx"].includes(
        extname(directoryEntry.name),
      )
    ) {
      collectedFiles.push(entryPath);
    }
  }

  return collectedFiles;
}

const clientComponentFiles =
  collectSourceFiles(
    resolve(
      featureRoot,
      "components",
    ),
  );

for (
  const clientFilePath of
  clientComponentFiles
) {
  const relativePath =
    toPosixPath(
      relative(
        webRoot,
        clientFilePath,
      ),
    );

  const source =
    readFileSync(
      clientFilePath,
      "utf8",
    ).replaceAll("\r\n", "\n");

  if (
    source.includes(
      "production-plan-api-client",
    )
  ) {
    fail(
      `${relativePath} must not import the production planning API client.`,
    );
  }
}

console.log(
  "Production planning API bridge verification passed.",
);

console.log(
  [
    "Verified server-only API client paths,",
    "facility query usage,",
    "list query normalization,",
    "separate API mappers,",
    "reader-only route imports,",
    "and factory API wiring.",
  ].join(" "),
);
