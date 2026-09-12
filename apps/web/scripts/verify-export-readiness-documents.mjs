import {
  existsSync,
  readFileSync,
} from "node:fs";
import { fileURLToPath } from "node:url";

const webRootUrl = new URL("../", import.meta.url);
const repoRootUrl = new URL("../../", webRootUrl);

function resolvePath(baseUrl, relativePath) {
  return fileURLToPath(new URL(relativePath, baseUrl));
}

function fail(message) {
  console.error(
    `Export readiness/documents verification failed: ${message}`,
  );
  process.exit(1);
}

function readRequired(baseUrl, relativePath) {
  const filePath = resolvePath(baseUrl, relativePath);
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

const engine = readRequired(
  repoRootUrl,
  "packages/NevaxisFactOS.Persistence/Export/ExportReadinessEngine.cs",
);
requireText(engine, "source-order-linked", "ExportReadinessEngine");
requireText(engine, "destination-country-known", "ExportReadinessEngine");
requireText(engine, "origin-country-known", "ExportReadinessEngine");
requireText(engine, "commercial-invoice", "ExportReadinessEngine");
requireText(engine, "packing-list", "ExportReadinessEngine");
forbidText(engine, "Germany requires", "ExportReadinessEngine");
forbidText(engine, "USA requires", "ExportReadinessEngine");

const writeRepo = readRequired(
  repoRootUrl,
  "packages/NevaxisFactOS.Persistence/Export/ExportCaseWriteRepository.cs",
);
requireText(writeRepo, "RegisterDocumentAsync", "ExportCaseWriteRepository");
requireText(writeRepo, "VerifyDocumentAsync", "ExportCaseWriteRepository");
requireText(writeRepo, "RejectDocumentAsync", "ExportCaseWriteRepository");
requireText(writeRepo, "ExportDocumentStatus.Verified", "ExportCaseWriteRepository");

const readinessPanel = readRequired(
  webRootUrl,
  "src/features/export/components/export-readiness-panel.tsx",
);
requireText(readinessPanel, "readinessPercent", "export-readiness-panel");
requireText(readinessPanel, "isBlocking", "export-readiness-panel");

const documentsPanel = readRequired(
  webRootUrl,
  "src/features/export/components/export-documents-panel.tsx",
);
requireText(documentsPanel, "verifyExportDocumentAction", "export-documents-panel");
requireText(documentsPanel, "rejectExportDocumentAction", "export-documents-panel");
requireText(documentsPanel, "registerExportDocumentAction", "export-documents-panel");

console.log("Export readiness/documents verification passed.");
