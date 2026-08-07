import {
  existsSync,
  readFileSync,
  statSync,
} from "node:fs";
import { spawnSync } from "node:child_process";
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

const webRoot = resolve(
  scriptsDirectory,
  "..",
);

function fail(message) {
  console.error(
    `Production planning release verification failed: ${message}`,
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

const requiredVerifierFiles = [
  "scripts/verify-production-planning-detail.mjs",
  "scripts/verify-production-planning-navigation.mjs",
  "scripts/verify-production-planning-states.mjs",
  "scripts/verify-production-planning-filter-url.mjs",
  "scripts/verify-production-planning-preview-isolation.mjs",
  "scripts/verify-production-planning-integration.mjs",
  "scripts/verify-production-planning-api-bridge.mjs",
  "scripts/verify-production-planning-server-boundary.mjs",
  "scripts/verify-production-planning-runtime-mode.mjs",
  "scripts/verify-production-planning-development-pilot.mjs",
  "scripts/verify-production-planning-draft-write.mjs",
  "scripts/verify-production-planning-concurrency.mjs",
  "scripts/verify-production-planning-write-security.mjs",
  "scripts/verify-production-planning-scheduling.mjs",
  "scripts/verify-production-planning-transfer-boundary.mjs",
  "scripts/verify-production-planning-transfer-security.mjs",
];

for (
  const verifierFile of
  requiredVerifierFiles
) {
  if (
    !existsSync(
      resolveWebPath(verifierFile),
    )
  ) {
    fail(
      `Required verifier is missing: ${verifierFile}`,
    );
  }
}

const listPage =
  readRequiredFile(
    "src/app/(app)/planlama/page.tsx",
  );

const detailPage =
  readRequiredFile(
    "src/app/(app)/planlama/[planId]/page.tsx",
  );

const globalsCssPath =
  resolveWebPath(
    "src/app/globals.css",
  );

if (!existsSync(globalsCssPath)) {
  fail(
    "Global stylesheet is missing.",
  );
}

const globalsCssStats =
  statSync(globalsCssPath);

if (globalsCssStats.size === 0) {
  fail(
    "Global stylesheet is empty.",
  );
}

const globalsCss =
  readFileSync(
    globalsCssPath,
    "utf8",
  ).replaceAll("\r\n", "\n");

requireText(
  listPage,
  "ProductionPlanFilteredWorkspace",
  "planlama/page.tsx",
);

requireText(
  listPage,
  "readProductionPlanningWorkspace",
  "planlama/page.tsx",
);

requireText(
  listPage,
  "production-plan-reader",
  "planlama/page.tsx",
);

forbidText(
  listPage,
  "PRODUCTION_PLAN_PREVIEW_DATA",
  "planlama/page.tsx",
);

requireText(
  detailPage,
  "readProductionPlanningDetail",
  "planlama/[planId]/page.tsx",
);

requireText(
  detailPage,
  "production-plan-reader",
  "planlama/[planId]/page.tsx",
);

forbidText(
  detailPage,
  "PRODUCTION_PLAN_PREVIEW_DATA",
  "planlama/[planId]/page.tsx",
);

requireText(
  detailPage,
  "ProductionPlanTimeline",
  "planlama/[planId]/page.tsx",
);

requireText(
  detailPage,
  "ProductionPlanDemandTable",
  "planlama/[planId]/page.tsx",
);

requireText(
  detailPage,
  "ProductionPlanResourceCards",
  "planlama/[planId]/page.tsx",
);

requireText(
  detailPage,
  "ProductionPlanRiskPanel",
  "planlama/[planId]/page.tsx",
);

requireText(
  detailPage,
  "ProductionPlanApprovalTransferPanel",
  "planlama/[planId]/page.tsx",
);

requireText(
  detailPage,
  "ProductionPlanHistoryPanel",
  "planlama/[planId]/page.tsx",
);

requireText(
  detailPage,
  "ProductionPlanSchedulingActions",
  "planlama/[planId]/page.tsx",
);

requireText(
  detailPage,
  "ProductionPlanTransferPanel",
  "planlama/[planId]/page.tsx",
);

const requiredCssMarkers = [
  "PRODUCTION PLANNING PREMIUM START",
  "PRODUCTION PLAN FILTER PREMIUM START",
  "PRODUCTION PLAN DETAIL PREMIUM START",
  "PRODUCTION PLAN RESOURCES PREMIUM START",
  "PRODUCTION PLAN RISK PANEL PREMIUM START",
  "PRODUCTION PLAN APPROVAL TRANSFER PREMIUM START",
  "PRODUCTION PLAN DEMAND TABLE PREMIUM START",
  "PRODUCTION PLAN TIMELINE PREMIUM START",
  "PRODUCTION PLAN HISTORY PANEL PREMIUM START",
  "PRODUCTION PLAN DETAIL RESPONSIVE START",
];

for (
  const cssMarker of
  requiredCssMarkers
) {
  requireText(
    globalsCss,
    cssMarker,
    "globals.css",
  );
}

console.log(
  "Running production planning preview isolation verifier.",
);

const previewIsolationVerifierPath =
  resolveWebPath(
    "scripts/verify-production-planning-preview-isolation.mjs",
  );

const previewIsolationResult = spawnSync(
  process.execPath,
  [previewIsolationVerifierPath],
  {
    cwd: resolve(
      webRoot,
      "../..",
    ),
    encoding: "utf8",
    stdio: "pipe",
  },
);

if (previewIsolationResult.stdout) {
  process.stdout.write(
    previewIsolationResult.stdout,
  );
}

if (previewIsolationResult.stderr) {
  process.stderr.write(
    previewIsolationResult.stderr,
  );
}

if (previewIsolationResult.error) {
  fail(
    `Preview isolation verifier could not start: ${previewIsolationResult.error.message}`,
  );
}

if (previewIsolationResult.status !== 0) {
  fail(
    `Preview isolation verifier exited with status ${previewIsolationResult.status ?? "unknown"}.`,
  );
}

console.log("");
console.log(
  "Running production planning integration verifier.",
);

const integrationVerifierPath =
  resolveWebPath(
    "scripts/verify-production-planning-integration.mjs",
  );

const integrationResult = spawnSync(
  process.execPath,
  [integrationVerifierPath],
  {
    cwd: resolve(
      webRoot,
      "../..",
    ),
    encoding: "utf8",
    stdio: "pipe",
  },
);

if (integrationResult.stdout) {
  process.stdout.write(
    integrationResult.stdout,
  );
}

if (integrationResult.stderr) {
  process.stderr.write(
    integrationResult.stderr,
  );
}

if (integrationResult.error) {
  fail(
    `Integration verifier could not start: ${integrationResult.error.message}`,
  );
}

if (integrationResult.status !== 0) {
  fail(
    `Integration verifier exited with status ${integrationResult.status ?? "unknown"}.`,
  );
}

const additionalVerifiers = [
  "scripts/verify-production-planning-api-bridge.mjs",
  "scripts/verify-production-planning-server-boundary.mjs",
  "scripts/verify-production-planning-runtime-mode.mjs",
  "scripts/verify-production-planning-development-pilot.mjs",
  "scripts/verify-production-planning-draft-write.mjs",
  "scripts/verify-production-planning-concurrency.mjs",
  "scripts/verify-production-planning-write-security.mjs",
  "scripts/verify-production-planning-scheduling.mjs",
  "scripts/verify-production-planning-transfer-boundary.mjs",
  "scripts/verify-production-planning-transfer-security.mjs",
];

for (const verifier of additionalVerifiers) {
  console.log("");
  console.log(
    `Running ${verifier}.`,
  );

  const verifierPath =
    resolveWebPath(verifier);

  const verifierResult = spawnSync(
    process.execPath,
    [verifierPath],
    {
      cwd: resolve(
        webRoot,
        "../..",
      ),
      encoding: "utf8",
      stdio: "pipe",
    },
  );

  if (verifierResult.stdout) {
    process.stdout.write(
      verifierResult.stdout,
    );
  }

  if (verifierResult.stderr) {
    process.stderr.write(
      verifierResult.stderr,
    );
  }

  if (verifierResult.status !== 0) {
    fail(
      `${verifier} exited with status ${verifierResult.status ?? "unknown"}.`,
    );
  }
}

const repositoryContract =
  readRequiredFile(
    "src/features/production-planning/application/production-plan-repository.ts",
  );

const previewRepository =
  readRequiredFile(
    "src/features/production-planning/data/production-plan-preview-repository.ts",
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
  repositoryContract,
  "export interface ProductionPlanRepository",
  "production-plan-repository.ts",
);

requireText(
  previewRepository,
  "createProductionPlanPreviewRepository",
  "production-plan-preview-repository.ts",
);

requireText(
  scopeResolver,
  "resolveProductionPlanningServerScope",
  "production-plan-scope.ts",
);

requireText(
  repositoryFactory,
  "createProductionPlanRepository",
  "production-plan-repository-factory.ts",
);

requireText(
  repositoryFactory,
  "PRODUCTION_PLAN_PREVIEW_DATA",
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

if (previewRepository.trim().length === 0) {
  fail(
    "Preview repository file is empty.",
  );
}

if (repositoryFactory.trim().length === 0) {
  fail(
    "Repository factory file is empty.",
  );
}

if (productionPlanReader.trim().length === 0) {
  fail(
    "Production plan reader file is empty.",
  );
}

console.log("");
console.log(
  "Production planning release verification passed.",
);

console.log(
  [
    "Verified canonical routes,",
    "server composition boundary,",
    "repository-backed read flow,",
    "detail composition,",
    "integration gates,",
    "non-empty global CSS,",
    "preview-data isolation, and all planning premium style blocks.",
  ].join(" "),
);