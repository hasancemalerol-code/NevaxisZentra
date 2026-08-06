import { spawnSync } from "node:child_process";
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

const repoRoot =
  resolve(webRoot, "../..");

const featureRoot =
  resolve(
    webRoot,
    "src/features/production-planning",
  );

function fail(message) {
  console.error(
    `Production planning runtime mode verification failed: ${message}`,
  );

  process.exit(1);
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

function runRuntimeConfigCase(
  label,
  environment,
  expectedOutcome,
) {
  const script = `
    import { parseProductionPlanRuntimeConfig } from "./src/features/production-planning/server/production-plan-runtime-config.ts";

    const result = parseProductionPlanRuntimeConfig();
    const actual = result.status === "success"
      ? \`success:\${result.config.mode}\`
      : \`failure:\${result.code}\`;

    if (actual !== ${JSON.stringify(expectedOutcome)}) {
      console.error(\`Expected ${expectedOutcome}, received \${actual}\`);
      process.exit(1);
    }
  `;

  const result = spawnSync(
    process.execPath,
    [
      "--import",
      "tsx",
      "--input-type=module",
      "-e",
      script,
    ],
    {
      cwd: webRoot,
      encoding: "utf8",
      env: {
        ...process.env,
        ...environment,
      },
      stdio: "pipe",
    },
  );

  if (result.status !== 0) {
    console.error(result.stdout);
    console.error(result.stderr);
    fail(`${label} failed.`);
  }
}

const runtimeConfigSource =
  readRequiredFile(
    "src/features/production-planning/server/production-plan-runtime-config.ts",
  );

const factorySource =
  readRequiredFile(
    "src/features/production-planning/server/production-plan-repository-factory.ts",
  );

requireText(
  runtimeConfigSource,
  "development-preview",
  "production-plan-runtime-config.ts",
);

requireText(
  runtimeConfigSource,
  "development-api",
  "production-plan-runtime-config.ts",
);

requireText(
  runtimeConfigSource,
  "Preview and development API runtime modes are forbidden in production.",
  "production-plan-runtime-config.ts",
);

requireText(
  runtimeConfigSource,
  "must be explicitly configured outside production.",
  "production-plan-runtime-config.ts",
);

requireText(
  factorySource,
  "development-preview",
  "production-plan-repository-factory.ts",
);

requireText(
  factorySource,
  "createProductionPlanApiRepository",
  "production-plan-repository-factory.ts",
);

runRuntimeConfigCase(
  "production defaults to api mode",
  {
    NODE_ENV: "production",
    CREVAFACTOS_PRODUCTION_PLANNING_RUNTIME_MODE:
      "",
    CREVAFACTOS_API_BASE_URL:
      "https://api.example.test",
    CREVAFACTOS_FACILITY_ID:
      "11111111-1111-1111-1111-111111111111",
  },
  "success:api",
);

runRuntimeConfigCase(
  "production rejects development-preview",
  {
    NODE_ENV: "production",
    CREVAFACTOS_PRODUCTION_PLANNING_RUNTIME_MODE:
      "development-preview",
    CREVAFACTOS_DEV_PREVIEW_TENANT_ID:
      "tenant-crevafactos",
    CREVAFACTOS_DEV_PREVIEW_FACILITY_ID:
      "facility-istanbul",
  },
  "failure:configuration-error",
);

runRuntimeConfigCase(
  "production rejects development-api",
  {
    NODE_ENV: "production",
    CREVAFACTOS_PRODUCTION_PLANNING_RUNTIME_MODE:
      "development-api",
    CREVAFACTOS_API_BASE_URL:
      "https://api.example.test",
    CREVAFACTOS_DEV_FACILITY_ID:
      "11111111-1111-1111-1111-111111111111",
    CREVAFACTOS_DEV_AUTH_ISSUER: "dev-issuer",
    CREVAFACTOS_DEV_AUTH_SUBJECT: "dev-subject",
    CREVAFACTOS_DEV_TENANT_KEY: "tenant-key",
  },
  "failure:configuration-error",
);

runRuntimeConfigCase(
  "unknown runtime mode fails",
  {
    NODE_ENV: "development",
    CREVAFACTOS_PRODUCTION_PLANNING_RUNTIME_MODE:
      "preview",
  },
  "failure:configuration-error",
);

runRuntimeConfigCase(
  "api mode requires base url",
  {
    NODE_ENV: "production",
    CREVAFACTOS_PRODUCTION_PLANNING_RUNTIME_MODE:
      "api",
    CREVAFACTOS_API_BASE_URL: "",
    CREVAFACTOS_FACILITY_ID:
      "11111111-1111-1111-1111-111111111111",
  },
  "failure:configuration-error",
);

runRuntimeConfigCase(
  "development-preview requires explicit scope",
  {
    NODE_ENV: "development",
    CREVAFACTOS_PRODUCTION_PLANNING_RUNTIME_MODE:
      "development-preview",
    CREVAFACTOS_DEV_PREVIEW_TENANT_ID:
      "tenant-crevafactos",
    CREVAFACTOS_DEV_PREVIEW_FACILITY_ID:
      "facility-istanbul",
  },
  "success:development-preview",
);

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

const allowedPreviewConsumers = new Set([
  "src/features/production-planning/server/production-plan-repository-factory.ts",
]);

for (
  const sourceFilePath of
  collectSourceFiles(featureRoot)
) {
  const relativePath = relative(
    webRoot,
    sourceFilePath,
  ).replaceAll("\\", "/");

  if (
    relativePath.endsWith(
      "production-plan-preview-data.ts",
    )
  ) {
    continue;
  }

  if (
    allowedPreviewConsumers.has(
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
    source.includes(
      "production-plan-preview-data",
    )
  ) {
    fail(
      `${relativePath} must not import preview data outside the factory boundary.`,
    );
  }
}

console.log(
  "Production planning runtime mode verification passed.",
);

console.log(
  [
    "Verified explicit runtime modes,",
    "production guards,",
    "configuration failures without silent fallback,",
    "and factory-only preview consumption.",
  ].join(" "),
);
