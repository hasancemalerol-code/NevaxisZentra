import {
  existsSync,
  readFileSync,
} from "node:fs";
import { fileURLToPath } from "node:url";

const webRootUrl = new URL("../", import.meta.url);

function resolveWebFile(relativePath) {
  return fileURLToPath(new URL(relativePath, webRootUrl));
}

function fail(message) {
  console.error(`Export premium UI verification failed: ${message}`);
  process.exit(1);
}

function readRequiredFile(relativePath) {
  const filePath = resolveWebFile(relativePath);
  if (!existsSync(filePath)) {
    fail(`Required file is missing: ${relativePath}`);
  }
  return readFileSync(filePath, "utf8").replaceAll("\r\n", "\n");
}

function requireText(source, expected, label) {
  if (!source.includes(expected)) {
    fail(`${label} must contain "${expected}".`);
  }
}

function forbidText(source, forbidden, label) {
  if (source.includes(forbidden)) {
    fail(`${label} must not contain "${forbidden}".`);
  }
}

const listPage = readRequiredFile("src/app/(app)/ihracat/page.tsx");
const detailPage = readRequiredFile(
  "src/app/(app)/ihracat/[exportCaseId]/page.tsx",
);
const caseCard = readRequiredFile(
  "src/features/export/components/export-case-card.tsx",
);
const exportCss = readRequiredFile("src/app/export.css");
const layout = readRequiredFile("src/app/layout.tsx");
const navigation = readRequiredFile(
  "src/lib/navigation/primary-navigation.ts",
);
const orderDetail = readRequiredFile(
  "src/app/(app)/siparisler/[orderDraftId]/page.tsx",
);

requireText(listPage, "readExportCases", "ihracat/page.tsx");
requireText(listPage, "İhracat", "ihracat/page.tsx");
requireText(listPage, "ExportCaseCard", "ihracat/page.tsx");
requireText(
  listPage,
  "İhracat siparişleri, belge hazırlığı ve sevkiyat öncesi uygunluk takibi",
  "ihracat/page.tsx",
);

requireText(detailPage, "readExportCaseById", "ihracat/[exportCaseId]/page.tsx");
requireText(detailPage, "ExportDetailWorkspace", "ihracat/[exportCaseId]/page.tsx");
requireText(detailPage, 'href="/ihracat"', "ihracat/[exportCaseId]/page.tsx");
requireText(detailPage, "/siparisler/", "ihracat/[exportCaseId]/page.tsx");

const detailWorkspace = readRequiredFile(
  "src/features/export/components/export-detail-workspace.tsx",
);
requireText(detailWorkspace, "ExportReadinessPanel", "export-detail-workspace");
requireText(detailWorkspace, "ExportDocumentsPanel", "export-detail-workspace");
requireText(detailWorkspace, "ExportActivityTimeline", "export-detail-workspace");

requireText(caseCard, "/ihracat/${exportCase.id}", "export-case-card.tsx");

requireText(exportCss, ".export-page", "export.css");
requireText(exportCss, ".export-case-card", "export.css");
requireText(layout, 'import "./export.css"', "layout.tsx");

requireText(navigation, 'label: "İhracat"', "primary-navigation.ts");
requireText(navigation, 'href: "/ihracat"', "primary-navigation.ts");
requireText(navigation, "Ship", "primary-navigation.ts");

requireText(orderDetail, "OrderExportSummaryPanel", "siparisler detail");
requireText(orderDetail, "readExportCaseByOrder", "siparisler detail");
requireText(orderDetail, "isExport", "siparisler detail");

forbidText(listPage, "AI EXPORT", "ihracat/page.tsx");
forbidText(detailPage, "coming soon", "ihracat/[exportCaseId]/page.tsx");
forbidText(detailPage, "Coming soon", "ihracat/[exportCaseId]/page.tsx");

console.log("Export premium UI verification passed.");
