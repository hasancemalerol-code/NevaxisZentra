#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { mkdtempSync, readFileSync, writeFileSync } from "node:fs";
import { tmpdir } from "node:os";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.CREVAFACTOS_API_BASE ?? "http://localhost:5000";
const FACILITY = process.env.CREVAFACTOS_FACILITY_ID ?? "bb000001-0005-4000-8000-000000000005";
const ISSUER = "crevafactos-dev";
const OPERATOR = "production-pilot-operator";
const NEGATIVE = "quality-negative-test-user";
const TENANT = "dev-pilot-tenant";
const RUN_SUFFIX = Date.now().toString(36);

function fail(message) {
  console.error(`INTEGRATION-WAVE-B runtime verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function headers(subject = OPERATOR, tenant = TENANT) {
  return {
    Accept: "application/json",
    "X-CrevaFactOS-Dev-Issuer": ISSUER,
    "X-CrevaFactOS-Dev-Subject": subject,
    "X-CrevaFactOS-Tenant": tenant,
  };
}

async function apiJson(method, path, body, subject = OPERATOR, facilityId = FACILITY) {
  const url = `${BASE}${path}${path.includes("?") ? "&" : "?"}facilityId=${facilityId}`;
  const response = await fetch(url, {
    method,
    headers: { ...headers(subject), "Content-Type": "application/json" },
    body: body ? JSON.stringify(body) : undefined,
  });
  const text = await response.text();
  let json = null;
  try { json = text ? JSON.parse(text) : null; } catch { json = null; }
  return { status: response.status, json, text };
}

async function apiForm(path, fileName, content, contentType, query = {}, subject = OPERATOR) {
  const params = new URLSearchParams({ facilityId: FACILITY, ...query });
  const form = new FormData();
  form.append("file", new Blob([content], { type: contentType }), fileName);
  const response = await fetch(`${BASE}${path}?${params}`, {
    method: "POST",
    headers: headers(subject),
    body: form,
  });
  const text = await response.text();
  let json = null;
  try { json = text ? JSON.parse(text) : null; } catch { json = null; }
  return { status: response.status, json, text };
}

async function materialTotal(subject = OPERATOR) {
  const response = await apiJson("GET", "/api/materials/workspace-summary", null, subject);
  if (response.status !== 200) fail(`material workspace summary expected 200 got ${response.status}`);
  return response.json?.totalCount ?? 0;
}

async function materialExists(code, subject = OPERATOR) {
  const response = await apiJson("GET", `/api/materials?search=${encodeURIComponent(code)}&pageSize=100`, null, subject);
  if (response.status !== 200) return false;
  return Array.isArray(response.json) && response.json.some((row) => row.code === code);
}

function buildMappingJson() {
  return JSON.stringify({
    targetCanonicalEntityType: "material",
    sourceEntityType: "materials",
    sourceKeyField: "code",
    fieldMappings: [
      { sourceField: "code", targetField: "code", required: true, targetType: "string" },
      { sourceField: "name", targetField: "name", required: true, targetType: "string" },
    ],
    defaults: {
      materialcategory: "other",
      polymerfamily: "unknown",
      physicalform: "granule",
      unitofmeasure: "kg",
      sourcetype: "imported",
    },
  });
}

function generateXlsxFixture(path) {
  const snippet = `
using ClosedXML.Excel;
using var workbook = new XLWorkbook();
var sheet = workbook.AddWorksheet("Materials");
sheet.Cell(1, 1).Value = "code";
sheet.Cell(1, 2).Value = "name";
sheet.Cell(2, 1).Value = "INT-XLS-${RUN_SUFFIX}";
sheet.Cell(2, 2).Value = "Xlsx Material";
sheet.Cell(3, 1).Value = "INT-XLS-FORM-${RUN_SUFFIX}";
sheet.Cell(3, 2).FormulaA1 = "=1+1";
workbook.SaveAs(@"${path.replaceAll("\\", "\\\\")}");
`;
  const tempDir = mkdtempSync(join(tmpdir(), "integration-xlsx-"));
  const program = join(tempDir, "Program.cs");
  const project = join(tempDir, "GenXlsx.csproj");
  writeFileSync(project, `<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup><OutputType>Exe</OutputType><TargetFramework>net10.0</TargetFramework></PropertyGroup>
  <ItemGroup><PackageReference Include="ClosedXML" Version="0.104.2" /></ItemGroup>
</Project>`);
  writeFileSync(program, snippet);
  const result = spawnSync("dotnet", ["run", "--project", project], { encoding: "utf8" });
  if (result.status !== 0) fail(`xlsx fixture generation failed: ${result.stderr || result.stdout}`);
  return readFileSync(path);
}

console.log("Running INTEGRATION-WAVE-B runtime verifier.");
console.log(`BASE=${BASE} FACILITY=${FACILITY} RUN_SUFFIX=${RUN_SUFFIX}`);

try {
  let health = await fetch(`${BASE}/health/live`);
  assert("API reachable", health.ok, `status=${health.status}`);

  let response = await fetch(`${BASE}/api/integration/connectors?facilityId=${FACILITY}`);
  assert("unauthenticated connector list denied", response.status === 401, `status=${response.status}`);

  response = await apiJson("GET", "/api/integration/connectors", null, NEGATIVE);
  assert("negative user connector list denied", response.status === 403, `status=${response.status}`);

  const connectorCode = `INT-CSV-${RUN_SUFFIX}`;
  response = await apiJson("POST", "/api/integration/connectors", {
    facilityId: FACILITY,
    name: connectorCode,
    description: "Wave B runtime verifier connector",
    connectorKind: "file-import",
    sourceSystemType: "file",
    sourceSystemName: "wave-b-runtime",
    environment: "development",
    authenticationMode: "none",
    synchronizationMode: "manual",
    domainBindingKind: "standalone",
    configurationJson: "{}",
  });
  assert("connector create", response.status === 201, response.text);
  const connectorId = response.json?.id;
  assert("connector id present", !!connectorId);

  const csvCode = `INT-MAT-${RUN_SUFFIX}`;
  const csvContent = `code,name\n${csvCode},Runtime Material Alpha\n`;
  response = await apiForm(
    `/api/integration/connectors/${connectorId}/file-imports/preview`,
    "materials.csv",
    csvContent,
    "text/csv",
  );
  assert("csv preview", response.status === 200, response.text);
  assert("csv preview headers", Array.isArray(response.json?.detectedHeaders));
  assert("schema fingerprint present", !!response.json?.schemaFingerprint);

  const headers = response.json.detectedHeaders;
  response = await apiJson("POST", `/api/integration/connectors/${connectorId}/mappings`, {
    facilityId: FACILITY,
    sourceHeaders: headers,
    mappingJson: buildMappingJson(),
    targetCanonicalEntityType: "material",
  });
  assert("mapping draft create", response.status === 201, response.text);
  const mappingId = response.json?.id;
  assert("mapping id present", !!mappingId);

  response = await apiJson(
    "POST",
    `/api/integration/connectors/${connectorId}/mappings/${mappingId}/validate`,
    { facilityId: FACILITY, expectedVersion: 1, sourceHeaders: headers },
  );
  assert("mapping validate", response.status === 200, response.text);

  response = await apiJson(
    "POST",
    `/api/integration/connectors/${connectorId}/mappings/${mappingId}/activate`,
    { facilityId: FACILITY, expectedVersion: 2 },
  );
  assert("mapping activate", response.status === 200, response.text);

  const materialsBeforeDryRun = await materialTotal();
  response = await apiForm(
    `/api/integration/connectors/${connectorId}/file-imports/dry-run`,
    "materials.csv",
    csvContent,
    "text/csv",
    { mappingVersionId: mappingId },
  );
  assert("csv dry-run", response.status === 200, response.text);
  assert("dry-run new projections", response.json?.newProjections >= 1);
  const materialsAfterDryRun = await materialTotal();
  assert("dry-run zero business mutations", materialsAfterDryRun === materialsBeforeDryRun,
    `before=${materialsBeforeDryRun} after=${materialsAfterDryRun}`);

  const importKey = `import-${RUN_SUFFIX}`;
  response = await apiForm(
    `/api/integration/connectors/${connectorId}/file-imports/import`,
    "materials.csv",
    csvContent,
    "text/csv",
    { mappingVersionId: mappingId, importIdempotencyKey: importKey },
  );
  assert("csv import", response.status === 200, response.text);
  assert("sync run id", !!response.json?.syncRunId);
  assert("material projected", await materialExists(csvCode));

  response = await apiJson("GET", `/api/integration/connectors/${connectorId}/sync-runs`);
  assert("sync runs listed", response.status === 200 && response.json?.length >= 1);

  response = await apiJson("GET", `/api/integration/connectors/${connectorId}/quarantine`);
  assert("quarantine listed", response.status === 200);

  const materialsAfterImport = await materialTotal();
  assert("import increased material count", materialsAfterImport > materialsBeforeDryRun);

  response = await apiForm(
    `/api/integration/connectors/${connectorId}/file-imports/import`,
    "materials.csv",
    csvContent,
    "text/csv",
    { mappingVersionId: mappingId, importIdempotencyKey: `replay-${RUN_SUFFIX}` },
  );
  assert("csv replay import", response.status === 200, response.text);
  assert("replay duplicates detected", (response.json?.summary?.duplicateRows ?? 0) >= 1
    || (response.json?.summary?.unchanged ?? 0) >= 1);
  const materialsAfterReplay = await materialTotal();
  assert("replay zero duplicate projections", materialsAfterReplay === materialsAfterImport,
    `afterImport=${materialsAfterImport} afterReplay=${materialsAfterReplay}`);

  response = await apiForm(
    `/api/integration/connectors/${connectorId}/file-imports/import`,
    "materials.csv",
    csvContent,
    "text/csv",
    { mappingVersionId: mappingId, importIdempotencyKey: importKey },
  );
  assert("idempotency key conflict", response.status === 400 || response.status === 409, response.text);

  const xlsxPath = join(tmpdir(), `materials-${RUN_SUFFIX}.xlsx`);
  const xlsxBytes = generateXlsxFixture(xlsxPath);
  response = await apiForm(
    `/api/integration/connectors/${connectorId}/file-imports/preview`,
    "materials.xlsx",
    xlsxBytes,
    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    { sheetName: "Materials" },
  );
  assert("xlsx preview", response.status === 200, response.text);
  assert("xlsx file kind", response.json?.fileKind === "xlsx");

  response = await apiForm(
    `/api/integration/connectors/${connectorId}/file-imports/dry-run`,
    "materials.xlsx",
    xlsxBytes,
    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    { mappingVersionId: mappingId, sheetName: "Materials" },
  );
  assert("xlsx dry-run", response.status === 200, response.text);

  response = await apiForm(
    `/api/integration/connectors/${connectorId}/file-imports/import`,
    "materials.xlsx",
    xlsxBytes,
    "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
    { mappingVersionId: mappingId, importIdempotencyKey: `xlsx-${RUN_SUFFIX}`, sheetName: "Materials" },
  );
  assert("xlsx import", response.status === 200, response.text);
  assert("xlsx material projected", await materialExists(`INT-XLS-${RUN_SUFFIX}`));

  const badCsv = [
    `code,name`,
    `GOOD-${RUN_SUFFIX},Good Row`,
    `,Missing Code`,
    `DUP-${RUN_SUFFIX},First`,
    `DUP-${RUN_SUFFIX},Duplicate In File`,
  ].join("\n");
  response = await apiForm(
    `/api/integration/connectors/${connectorId}/file-imports/import`,
    "bad.csv",
    badCsv,
    "text/csv",
    { mappingVersionId: mappingId, importIdempotencyKey: `bad-${RUN_SUFFIX}` },
  );
  assert("bad-data import completes", response.status === 200, response.text);
  assert("bad-data quarantine candidates", (response.json?.summary?.quarantineCandidates ?? 0) >= 1
    || (response.json?.summary?.invalidRows ?? 0) >= 1);
  response = await apiJson("GET", `/api/integration/connectors/${connectorId}/quarantine`);
  assert("quarantine records present", response.status === 200 && response.json?.length >= 1);

  const driftCsv = `code,name,unexpected\nDRIFT-${RUN_SUFFIX},Drift Row,x\n`;
  response = await apiForm(
    `/api/integration/connectors/${connectorId}/file-imports/dry-run`,
    "drift.csv",
    driftCsv,
    "text/csv",
    { mappingVersionId: mappingId },
  );
  assert("schema drift blocked", response.status === 400, response.text);

  console.log("CSV_GOLDEN_FLOW=PASS");
  console.log("CSV_IDEMPOTENT_REPLAY=PASS");
  console.log("IDEMPOTENCY_KEY_PAYLOAD_CONFLICT=PASS");
  console.log("XLSX_GOLDEN_FLOW=PASS");
  console.log("BAD_DATA_GOLDEN_FLOW=PASS");
  console.log("SCHEMA_DRIFT_GOLDEN_FLOW=PASS");
  console.log("INTEGRATION-WAVE-B runtime verification passed.");
} catch (error) {
  fail(error instanceof Error ? error.message : String(error));
}
