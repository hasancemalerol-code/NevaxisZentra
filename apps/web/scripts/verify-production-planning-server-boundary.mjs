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

const routeFiles = [
  "src/app/(app)/planlama/page.tsx",
  "src/app/(app)/planlama/[planId]/page.tsx",
];

const globalsCssPath =
  "src/app/globals.css";

const navigationPath =
  "src/lib/navigation/primary-navigation.ts";

function fail(message) {
  console.error(
    `Production planning server boundary verification failed: ${message}`,
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

const reader =
  readRequiredFile(
    "src/features/production-planning/server/production-plan-reader.ts",
  );

const requestContext =
  readRequiredFile(
    "src/features/production-planning/server/production-plan-request-context.ts",
  );

const runtimeConfig =
  readRequiredFile(
    "src/features/production-planning/server/production-plan-runtime-config.ts",
  );

const scope =
  readRequiredFile(
    "src/features/production-planning/server/production-plan-scope.ts",
  );

requireText(
  reader,
  "readProductionPlanningWorkspace",
  "production-plan-reader.ts",
);

requireText(
  reader,
  "readProductionPlanningDetail",
  "production-plan-reader.ts",
);

forbidText(
  runtimeConfig,
  "NEXT_PUBLIC_",
  "production-plan-runtime-config.ts",
);

forbidText(
  requestContext,
  "NEXT_PUBLIC_",
  "production-plan-request-context.ts",
);

forbidText(
  scope,
  "searchParams",
  "production-plan-scope.ts",
);

forbidText(
  scope,
  "tenantIdFromQuery",
  "production-plan-scope.ts",
);

requireText(
  requestContext,
  "serializeProductionPlanRequestContext",
  "production-plan-request-context.ts",
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
    "production-plan-request-context",
    routeFile,
  );

  forbidText(
    routeSource,
    "production-plan-runtime-config",
    routeFile,
  );
}

function collectClientComponentFiles(
  directoryPath,
) {
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
        ...collectClientComponentFiles(
          entryPath,
        ),
      );

      continue;
    }

    if (
      extname(directoryEntry.name) ===
      ".tsx"
    ) {
      collectedFiles.push(entryPath);
    }
  }

  return collectedFiles;
}

const clientFiles =
  collectClientComponentFiles(
    resolve(
      featureRoot,
      "components",
    ),
  );

for (const clientFile of clientFiles) {
  const source =
    readFileSync(
      clientFile,
      "utf8",
    ).replaceAll("\r\n", "\n");

  if (source.includes('"use client"')) {
    const valueImportPattern =
      /^import\s+(?!type\s)[^'"]*['"].*\/server\//m;

    if (valueImportPattern.test(source)) {
      fail(
        `${relative(webRoot, clientFile)} must not import production planning server modules.`,
      );
    }
  }
}

readRequiredFile(globalsCssPath);
readRequiredFile(navigationPath);

console.log(
  "Production planning server boundary verification passed.",
);

console.log(
  [
    "Verified reader-only routes,",
    "non-public runtime config,",
    "absence of tenant query resolution,",
    "client/server import boundaries,",
    "and unchanged globals/navigation files.",
  ].join(" "),
);
