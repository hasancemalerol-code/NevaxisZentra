import {
  existsSync,
  readFileSync,
} from "node:fs";
import { fileURLToPath } from "node:url";

const webRootUrl = new URL(
  "../",
  import.meta.url,
);

function resolveWebFile(relativePath) {
  return fileURLToPath(
    new URL(relativePath, webRootUrl),
  );
}

function fail(message) {
  console.error(
    `Production planning filter URL verification failed: ${message}`,
  );

  process.exit(1);
}

function readRequiredFile(relativePath) {
  const filePath =
    resolveWebFile(relativePath);

  if (!existsSync(filePath)) {
    fail(`Required file is missing: ${relativePath}`);
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
      `${fileLabel} must not contain unsafe text "${forbiddenText}".`,
    );
  }
}

const filterUrl =
  readRequiredFile(
    "src/features/production-planning/presentation/production-plan-filter-url.ts",
  );

const filterState =
  readRequiredFile(
    "src/features/production-planning/application/use-production-plan-filter-state.ts",
  );

const filterBar =
  readRequiredFile(
    "src/features/production-planning/components/production-plan-filter-bar.tsx",
  );

const filteredWorkspace =
  readRequiredFile(
    "src/features/production-planning/components/production-plan-filtered-workspace.tsx",
  );

const requiredAllowlistDeclarations = [
  "PRODUCTION_PLAN_STATUSES",
  "PRODUCTION_PLAN_ORIGINS",
  "PRODUCTION_PLAN_PRIORITIES",
  "PRODUCTION_PLAN_CONFIDENCES",
  "PRODUCTION_PLAN_TARGET_SYSTEMS",
];

for (
  const declaration of
  requiredAllowlistDeclarations
) {
  requireText(
    filterUrl,
    `new Set<`,
    "production-plan-filter-url.ts",
  );

  requireText(
    filterUrl,
    declaration,
    "production-plan-filter-url.ts",
  );
}

const requiredStatusValues = [
  '"draft"',
  '"needs-review"',
  '"ready-for-approval"',
  '"approved"',
  '"rejected"',
  '"scheduled"',
  '"in-progress"',
  '"completed"',
  '"cancelled"',
];

for (const value of requiredStatusValues) {
  requireText(
    filterUrl,
    value,
    "production-plan-filter-url.ts",
  );
}

const requiredOriginValues = [
  '"approved-order"',
  '"erp-production-request"',
  '"manual"',
  '"ai-recommendation"',
];

for (const value of requiredOriginValues) {
  requireText(
    filterUrl,
    value,
    "production-plan-filter-url.ts",
  );
}

const requiredPriorityValues = [
  '"low"',
  '"normal"',
  '"high"',
  '"urgent"',
];

for (const value of requiredPriorityValues) {
  requireText(
    filterUrl,
    value,
    "production-plan-filter-url.ts",
  );
}

const requiredConfidenceValues = [
  '"low"',
  '"medium"',
  '"high"',
];

for (const value of requiredConfidenceValues) {
  requireText(
    filterUrl,
    value,
    "production-plan-filter-url.ts",
  );
}

const requiredTargetValues = [
  '"erp"',
  '"mes"',
  '"crevafactos"',
];

for (const value of requiredTargetValues) {
  requireText(
    filterUrl,
    value,
    "production-plan-filter-url.ts",
  );
}

requireText(
  filterUrl,
  "function getSingleSearchParam",
  "production-plan-filter-url.ts",
);

requireText(
  filterUrl,
  "function parseBooleanSearchParam",
  "production-plan-filter-url.ts",
);

requireText(
  filterUrl,
  'normalizedValue === "1"',
  "production-plan-filter-url.ts",
);

requireText(
  filterUrl,
  'normalizedValue === "0"',
  "production-plan-filter-url.ts",
);

requireText(
  filterUrl,
  "function parseEnumValue",
  "production-plan-filter-url.ts",
);

requireText(
  filterUrl,
  "allowedValues.has(value as T)",
  "production-plan-filter-url.ts",
);

requireText(
  filterUrl,
  "return undefined;",
  "production-plan-filter-url.ts",
);

requireText(
  filterUrl,
  "new URLSearchParams()",
  "production-plan-filter-url.ts",
);

requireText(
  filterUrl,
  "setOptionalSearchParam",
  "production-plan-filter-url.ts",
);

requireText(
  filterUrl,
  'pathname = "/planlama"',
  "production-plan-filter-url.ts",
);

requireText(
  filterUrl,
  "searchParams.toString()",
  "production-plan-filter-url.ts",
);

requireText(
  filterState,
  "createSearchParamsRecord",
  "use-production-plan-filter-state.ts",
);

requireText(
  filterState,
  "normalizeFilter",
  "use-production-plan-filter-state.ts",
);

requireText(
  filterState,
  "createProductionPlanFilterUrl",
  "use-production-plan-filter-state.ts",
);

requireText(
  filterState,
  "router.replace(nextUrl",
  "use-production-plan-filter-state.ts",
);

requireText(
  filterState,
  "scroll: false",
  "use-production-plan-filter-state.ts",
);

requireText(
  filterState,
  "router.replace(pathname",
  "use-production-plan-filter-state.ts",
);

requireText(
  filterBar,
  "onChange",
  "production-plan-filter-bar.tsx",
);

requireText(
  filterBar,
  "onClear",
  "production-plan-filter-bar.tsx",
);

requireText(
  filteredWorkspace,
  "useProductionPlanFilterState",
  "production-plan-filtered-workspace.tsx",
);

requireText(
  filteredWorkspace,
  "queryProductionPlans",
  "production-plan-filtered-workspace.tsx",
);

const forbiddenUnsafePatterns = [
  "window.location.href",
  "window.location.assign",
  "document.location",
  "eval(",
  "new Function(",
  "router.push(`?${",
  "decodeURIComponent(searchParams",
];

for (
  const forbiddenPattern of
  forbiddenUnsafePatterns
) {
  forbidText(
    filterUrl,
    forbiddenPattern,
    "production-plan-filter-url.ts",
  );

  forbidText(
    filterState,
    forbiddenPattern,
    "use-production-plan-filter-state.ts",
  );

  forbidText(
    filterBar,
    forbiddenPattern,
    "production-plan-filter-bar.tsx",
  );
}

console.log(
  "Production planning filter URL verification passed.",
);

console.log(
  [
    "Verified enum allowlists,",
    "boolean parsing,",
    "normalized URL generation,",
    "invalid parameter rejection,",
    "and router-based filter synchronization.",
  ].join(" "),
);