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
    `Production planning navigation verification failed: ${message}`,
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
      `${fileLabel} must not contain "${forbiddenText}".`,
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

const planCard =
  readRequiredFile(
    "src/features/production-planning/components/production-plan-card.tsx",
  );

const filteredWorkspace =
  readRequiredFile(
    "src/features/production-planning/components/production-plan-filtered-workspace.tsx",
  );

const detailQuery =
  readRequiredFile(
    "src/features/production-planning/application/production-plan-detail-query.ts",
  );

const previewData =
  readRequiredFile(
    "src/features/production-planning/data/production-plan-preview-data.ts",
  );

requireText(
  listPage,
  "PRODUCTION_PLAN_PREVIEW_DATA",
  "planlama/page.tsx",
);

requireText(
  listPage,
  "ProductionPlanFilteredWorkspace",
  "planlama/page.tsx",
);

requireText(
  filteredWorkspace,
  "ProductionPlanWorkspace",
  "production-plan-filtered-workspace.tsx",
);

requireText(
  planCard,
  "`/planlama/${productionPlan.id}`",
  "production-plan-card.tsx",
);

requireText(
  detailPage,
  'href="/planlama"',
  "planlama/[planId]/page.tsx",
);

requireText(
  detailPage,
  "queryProductionPlanDetail",
  "planlama/[planId]/page.tsx",
);

requireText(
  detailPage,
  "PRODUCTION_PLAN_PREVIEW_DATA",
  "planlama/[planId]/page.tsx",
);

requireText(
  detailQuery,
  "findProductionPlanById",
  "production-plan-detail-query.ts",
);

requireText(
  detailQuery,
  "Multiple plans use id",
  "production-plan-detail-query.ts",
);

requireText(
  previewData,
  "PRODUCTION_PLAN_PREVIEW_DATA",
  "production-plan-preview-data.ts",
);

const forbiddenRoutePatterns = [
  'href="/production-planning"',
  'href="/uretim-planlama"',
  'href="/plan"',
  '`/production-planning/${productionPlan.id}`',
  '`/uretim-planlama/${productionPlan.id}`',
];

for (
  const forbiddenRoutePattern of
  forbiddenRoutePatterns
) {
  forbidText(
    planCard,
    forbiddenRoutePattern,
    "production-plan-card.tsx",
  );

  forbidText(
    detailPage,
    forbiddenRoutePattern,
    "planlama/[planId]/page.tsx",
  );
}

console.log(
  "Production planning navigation verification passed.",
);

console.log(
  [
    "Verified list-to-detail links,",
    "detail back navigation,",
    "shared preview data,",
    "and canonical /planlama routes.",
  ].join(" "),
);