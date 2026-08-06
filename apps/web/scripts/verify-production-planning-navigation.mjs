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
  "readProductionPlanningWorkspace",
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
  "readProductionPlanningDetail",
  "planlama/[planId]/page.tsx",
);

requireText(
  detailPage,
  "production-plan-reader",
  "planlama/[planId]/page.tsx",
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
  detailQuery,
  "findProductionPlanById",
  "production-plan-detail-query.ts",
);

requireText(
  detailQuery,
  "readProductionPlanDetail",
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

const previewRepository =
  readRequiredFile(
    "src/features/production-planning/data/production-plan-preview-repository.ts",
  );

requireText(
  previewRepository,
  "createProductionPlanPreviewRepository",
  "production-plan-preview-repository.ts",
);

requireText(
  previewRepository,
  "Multiple plans use id",
  "production-plan-preview-repository.ts",
);

const repositoryFactory =
  readRequiredFile(
    "src/features/production-planning/server/production-plan-repository-factory.ts",
  );

requireText(
  repositoryFactory,
  "PRODUCTION_PLAN_PREVIEW_DATA",
  "production-plan-repository-factory.ts",
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
    "server reader boundary,",
    "and canonical /planlama routes.",
  ].join(" "),
);