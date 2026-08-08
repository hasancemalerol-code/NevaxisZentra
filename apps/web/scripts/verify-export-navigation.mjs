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
  console.error(`Export navigation verification failed: ${message}`);
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

const listPage = readRequiredFile("src/app/(app)/ihracat/page.tsx");
const detailPage = readRequiredFile(
  "src/app/(app)/ihracat/[exportCaseId]/page.tsx",
);
const caseCard = readRequiredFile(
  "src/features/export/components/export-case-card.tsx",
);
const navigation = readRequiredFile(
  "src/lib/navigation/primary-navigation.ts",
);

requireText(listPage, "readExportCases", "ihracat/page.tsx");
requireText(caseCard, "/ihracat/${exportCase.id}", "export-case-card.tsx");
requireText(detailPage, 'href="/ihracat"', "detail back link");
requireText(detailPage, "/siparisler/", "detail source order link");
requireText(navigation, 'label: "İhracat"', "primary-navigation");
requireText(navigation, 'href: "/ihracat"', "primary-navigation");

const productionIndex = navigation.indexOf('label: "Üretim"');
const exportIndex = navigation.indexOf('label: "İhracat"');
const shiftsIndex = navigation.indexOf('label: "Vardiyalar"');

if (!(productionIndex < exportIndex && exportIndex < shiftsIndex)) {
  fail("İhracat must appear immediately after Üretim and before Vardiyalar.");
}

console.log("Export navigation verification passed.");
