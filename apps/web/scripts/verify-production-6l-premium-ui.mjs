import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");

function fail(message) {
  console.error(`Production 6L premium UI verification failed: ${message}`);
  process.exit(1);
}

function readRequired(relativePath) {
  const absolutePath = resolve(webRoot, relativePath);
  if (!existsSync(absolutePath)) {
    fail(`Required file is missing: ${relativePath}`);
  }
  return readFileSync(absolutePath, "utf8").replaceAll("\r\n", "\n");
}

function requireText(source, expected, label) {
  if (!source.includes(expected)) {
    fail(`${label} must contain "${expected}".`);
  }
}

console.log("Running production 6L premium UI verifier.");

const page = readRequired("src/app/(app)/uretim/raporlama/page.tsx");
const uretimPage = readRequired("src/app/(app)/uretim/page.tsx");

requireText(page, "production-execution-page", "raporlama page");
requireText(page, "production-execution-kpis", "raporlama page");
requireText(page, "Aktif İş Emirleri", "raporlama page");
requireText(page, "Termin / Sevkiyat Riski", "raporlama page");
requireText(page, "Emir performansı", "raporlama page");
requireText(page, "/uretim/", "raporlama page");
requireText(uretimPage, "/uretim/raporlama", "uretim page");

console.log("Production 6L premium UI verification passed.");
