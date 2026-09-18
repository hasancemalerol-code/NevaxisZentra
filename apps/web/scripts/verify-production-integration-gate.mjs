import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

function fail(message) {
  console.error(`Production integration gate verification failed: ${message}`);
  process.exit(1);
}

function readRequired(relativePath) {
  const absolutePath = resolve(projectRoot, relativePath);
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

function forbidText(source, forbidden, label) {
  if (source.includes(forbidden)) {
    fail(`${label} must not contain "${forbidden}".`);
  }
}

console.log("Running production integration gate verifier.");

// PRODUCTION-1L foundation
requireText(
  readRequired("packages/NevaxisZentra.Persistence/Production/ProductionOrder.cs"),
  "TenantId",
  "PRODUCTION-1L order",
);
requireText(
  readRequired("packages/NevaxisZentra.Persistence/Production/IProductionOrderReadRepository.cs"),
  "ListAsync",
  "PRODUCTION-1L read repository",
);
requireText(
  readRequired("packages/NevaxisZentra.Persistence/Production/IProductionOrderWriteRepository.cs"),
  "StartAsync",
  "PRODUCTION-1L write repository",
);

// PRODUCTION-2L quantities / scrap / operator
requireText(
  readRequired("packages/NevaxisZentra.Persistence/Production/IProductionExecutionEntryRepository.cs"),
  "RecordGoodAsync",
  "PRODUCTION-2L quantities",
);
requireText(
  readRequired("packages/NevaxisZentra.Persistence/Production/IProductionOperatorRepository.cs"),
  "AssignOperatorAsync",
  "PRODUCTION-2L operator",
);

// PRODUCTION-3L downtime / setup / cycles
requireText(
  readRequired("packages/NevaxisZentra.Persistence/Production/IProductionOperationsRepository.cs"),
  "StartDowntimeAsync",
  "PRODUCTION-3L downtime",
);
requireText(
  readRequired("packages/NevaxisZentra.Persistence/Production/ProductionCycleEntry.cs"),
  "CycleDurationMilliseconds",
  "PRODUCTION-3L cycles",
);

// PRODUCTION-4L hardening
requireText(
  readRequired("packages/NevaxisZentra.Persistence/Production/ProductionOrderWriteRepository.cs"),
  "ConcurrencyConflict",
  "PRODUCTION-4L concurrency",
);
requireText(
  readRequired("packages/NevaxisZentra.Persistence/Production/IProductionExceptionRepository.cs"),
  "RaiseExceptionAsync",
  "PRODUCTION-4L exceptions",
);

// PRODUCTION-5L shift / resource / operator / handover
requireText(
  readRequired("packages/NevaxisZentra.Persistence/Production/IProductionShiftRepository.cs"),
  "AssignShiftAsync",
  "PRODUCTION-5L shift",
);
requireText(
  readRequired("packages/NevaxisZentra.Persistence/Production/IProductionHandoverRepository.cs"),
  "CreateHandoverAsync",
  "PRODUCTION-5L handover",
);

// PRODUCTION-6L reporting
requireText(
  readRequired("apps/api/Production/ProductionReportingEndpointRouteBuilderExtensions.cs"),
  "MapProductionReportingEndpoints",
  "PRODUCTION-6L reporting API",
);
requireText(
  readRequired("apps/web/src/app/(app)/uretim/raporlama/page.tsx"),
  "readProductionReportingWorkspace",
  "PRODUCTION-6L reporting UI",
);

// Integration wiring
requireText(
  readRequired("apps/api/Program.cs"),
  "MapProductionExecutionReadEndpoints",
  "Program.cs",
);
requireText(
  readRequired("apps/api/Program.cs"),
  "MapProductionReportingEndpoints",
  "Program.cs",
);
requireText(
  readRequired("apps/web/src/app/(app)/uretim/page.tsx"),
  "/uretim/raporlama",
  "Production hub page",
);

forbidText(
  readRequired("apps/api/Production/ProductionExecutionEndpointRouteBuilderExtensions.cs"),
  "MapDelete",
  "Production execution endpoints",
);

console.log("Production integration gate verification passed.");
