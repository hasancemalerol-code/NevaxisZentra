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

function fail(message) {
  console.error(
    `Production planning detail verification failed: ${message}`,
  );

  process.exit(1);
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
      `${fileLabel} must not contain legacy text "${forbiddenText}".`,
    );
  }
}

const fallbackLabels =
  readRequiredFile(
    "src/features/production-planning/presentation/production-plan-fallback-labels.ts",
  );

const detailViewModel =
  readRequiredFile(
    "src/features/production-planning/application/production-plan-detail-view-model.ts",
  );

const demandViewModel =
  readRequiredFile(
    "src/features/production-planning/application/production-plan-demand-view-model.ts",
  );

const resourceCards =
  readRequiredFile(
    "src/features/production-planning/components/production-plan-resource-cards.tsx",
  );

const demandTable =
  readRequiredFile(
    "src/features/production-planning/components/production-plan-demand-table.tsx",
  );

const timeline =
  readRequiredFile(
    "src/features/production-planning/components/production-plan-timeline.tsx",
  );

const historyPanel =
  readRequiredFile(
    "src/features/production-planning/components/production-plan-history-panel.tsx",
  );

const approvalTransferPanel =
  readRequiredFile(
    "src/features/production-planning/components/production-plan-approval-transfer-panel.tsx",
  );

const detailPage =
  readRequiredFile(
    "src/app/(app)/planlama/[planId]/page.tsx",
  );

const requiredFallbackKeys = [
  'facility: "Tesis belirtilmedi"',
  'machine: "Makine atanmadı"',
  'mold: "Kalıp atanmadı"',
  'material: "Malzeme atanmadı"',
  'shift: "Vardiya belirtilmedi"',
  'startTime: "Başlangıç zamanı belirlenmedi"',
  'endTime: "Bitiş zamanı belirlenmedi"',
  'duration: "Süre hesaplanamadı"',
  'targetSystem: "Aktarım hedefi belirlenmedi"',
  'notes: "Plan açıklaması bulunmuyor."',
  'sourceReference: "Kaynak referansı bulunmuyor"',
  'orderReference: "Sipariş belirtilmedi"',
  'productId: "Ürün kodu belirtilmedi"',
  'productName: "Ürün adı belirtilmedi"',
  'deliveryDate: "Termin belirtilmedi"',
  'auditDate: "Tarih bilgisi bulunmuyor"',
];

for (
  const requiredFallbackKey of
  requiredFallbackKeys
) {
  requireText(
    fallbackLabels,
    requiredFallbackKey,
    "production-plan-fallback-labels.ts",
  );
}

requireText(
  detailViewModel,
  "../presentation/production-plan-fallback-labels",
  "production-plan-detail-view-model.ts",
);

requireText(
  detailViewModel,
  "getProductionPlanResourceDisplayValue",
  "production-plan-detail-view-model.ts",
);

requireText(
  demandViewModel,
  "../presentation/production-plan-fallback-labels",
  "production-plan-demand-view-model.ts",
);

requireText(
  demandViewModel,
  "getProductionPlanDemandDeliveryLabel",
  "production-plan-demand-view-model.ts",
);

requireText(
  resourceCards,
  "getProductionPlanResourceDisplayValue",
  "production-plan-resource-cards.tsx",
);

requireText(
  demandTable,
  "getProductionPlanDemandDeliveryLabel",
  "production-plan-demand-table.tsx",
);

requireText(
  timeline,
  "../presentation/production-plan-fallback-labels",
  "production-plan-timeline.tsx",
);

requireText(
  historyPanel,
  "../presentation/production-plan-fallback-labels",
  "production-plan-history-panel.tsx",
);

requireText(
  approvalTransferPanel,
  "../presentation/production-plan-fallback-labels",
  "production-plan-approval-transfer-panel.tsx",
);

const requiredDetailComponents = [
  "ProductionPlanTimeline",
  "ProductionPlanDemandTable",
  "ProductionPlanResourceCards",
  "ProductionPlanRiskPanel",
  "ProductionPlanApprovalTransferPanel",
  "ProductionPlanHistoryPanel",
];

for (
  const requiredComponent of
  requiredDetailComponents
) {
  requireText(
    detailPage,
    requiredComponent,
    "planlama/[planId]/page.tsx",
  );
}

requireText(
  detailPage,
  "getProductionPlanFallbackLabel",
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
  detailPage,
  'result.status === "failure"',
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

const legacyChecks = [
  [
    detailPage,
    '"Tesis belirtilmedi"',
    "planlama/[planId]/page.tsx",
  ],
  [
    detailPage,
    '"Aktarım hedefi belirlenmedi"',
    "planlama/[planId]/page.tsx",
  ],
  [
    detailPage,
    '"Plan açıklaması bulunmuyor."',
    "planlama/[planId]/page.tsx",
  ],
  [
    resourceCards,
    '"Makine seçilmedi"',
    "production-plan-resource-cards.tsx",
  ],
  [
    resourceCards,
    '"Kalıp seçilmedi"',
    "production-plan-resource-cards.tsx",
  ],
  [
    resourceCards,
    '"Malzeme seçilmedi"',
    "production-plan-resource-cards.tsx",
  ],
  [
    demandTable,
    '"Termin belirtilmedi"',
    "production-plan-demand-table.tsx",
  ],
  [
    timeline,
    '"Vardiya belirtilmedi"',
    "production-plan-timeline.tsx",
  ],
  [
    timeline,
    '"Süre hesaplanamadı"',
    "production-plan-timeline.tsx",
  ],
  [
    historyPanel,
    '"Tarih bilgisi bulunmuyor"',
    "production-plan-history-panel.tsx",
  ],
  [
    historyPanel,
    '"Kaynak referansı bulunmuyor"',
    "production-plan-history-panel.tsx",
  ],
  [
    approvalTransferPanel,
    '"Belirlenmedi"',
    "production-plan-approval-transfer-panel.tsx",
  ],
];

for (
  const [
    source,
    forbiddenText,
    fileLabel,
  ] of legacyChecks
) {
  forbidText(
    source,
    forbiddenText,
    fileLabel,
  );
}

console.log(
  "Production planning detail verification passed.",
);

console.log(
  [
    "Verified centralized fallback labels,",
    "detail panels,",
    "and legacy fallback removal.",
  ].join(" "),
);