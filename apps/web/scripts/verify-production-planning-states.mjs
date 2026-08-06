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
    `Production planning state verification failed: ${message}`,
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

const workspace =
  readRequiredFile(
    "src/features/production-planning/components/production-plan-workspace.tsx",
  );

const filteredWorkspace =
  readRequiredFile(
    "src/features/production-planning/components/production-plan-filtered-workspace.tsx",
  );

const riskPanel =
  readRequiredFile(
    "src/features/production-planning/components/production-plan-risk-panel.tsx",
  );

const demandTable =
  readRequiredFile(
    "src/features/production-planning/components/production-plan-demand-table.tsx",
  );

const historyPanel =
  readRequiredFile(
    "src/features/production-planning/components/production-plan-history-panel.tsx",
  );

const detailPage =
  readRequiredFile(
    "src/app/(app)/planlama/[planId]/page.tsx",
  );

const detailQuery =
  readRequiredFile(
    "src/features/production-planning/application/production-plan-detail-query.ts",
  );

requireText(
  workspace,
  "production-plan-workspace--empty",
  "production-plan-workspace.tsx",
);

requireText(
  workspace,
  "productionPlans.length === 0",
  "production-plan-workspace.tsx",
);

requireText(
  workspace,
  "totalCount",
  "production-plan-workspace.tsx",
);

requireText(
  filteredWorkspace,
  "filteredProductionPlans",
  "production-plan-filtered-workspace.tsx",
);

requireText(
  filteredWorkspace,
  "totalCount={productionPlans.length}",
  "production-plan-filtered-workspace.tsx",
);

requireText(
  riskPanel,
  "!viewModel.hasRisks",
  "production-plan-risk-panel.tsx",
);

requireText(
  riskPanel,
  "Aktif plan riski bulunmuyor",
  "production-plan-risk-panel.tsx",
);

requireText(
  demandTable,
  "!viewModel.hasDemands",
  "production-plan-demand-table.tsx",
);

requireText(
  demandTable,
  "Üretim talebi bulunmuyor",
  "production-plan-demand-table.tsx",
);

requireText(
  historyPanel,
  "!viewModel.hasHistory",
  "production-plan-history-panel.tsx",
);

requireText(
  historyPanel,
  "Audit geçmişi bulunmuyor",
  "production-plan-history-panel.tsx",
);

requireText(
  detailPage,
  'import { notFound } from "next/navigation";',
  "planlama/[planId]/page.tsx",
);

requireText(
  detailPage,
  'result.status === "not-found"',
  "planlama/[planId]/page.tsx",
);

requireText(
  detailPage,
  "notFound();",
  "planlama/[planId]/page.tsx",
);

requireText(
  detailQuery,
  'status: "not-found"',
  "production-plan-detail-query.ts",
);

requireText(
  detailQuery,
  "productionPlan: null",
  "production-plan-detail-query.ts",
);

const forbiddenSuccessFallbacks = [
  "Plan başarıyla yüklendi",
  "Veriler başarıyla getirildi",
  "İşlem başarılı",
  "Örnek hata yok",
];

for (
  const forbiddenSuccessFallback of
  forbiddenSuccessFallbacks
) {
  forbidText(
    workspace,
    forbiddenSuccessFallback,
    "production-plan-workspace.tsx",
  );

  forbidText(
    detailPage,
    forbiddenSuccessFallback,
    "planlama/[planId]/page.tsx",
  );
}

console.log(
  "Production planning state verification passed.",
);

console.log(
  [
    "Verified empty list,",
    "empty panel,",
    "not-found route,",
    "and result-count state handling.",
  ].join(" "),
);