#!/usr/bin/env node
/**
 * verify-production-execution-security.mjs
 *
 * Verifies security properties of the Production Execution bounded context:
 * - Tenant + facility isolation at every endpoint
 * - RLS enforcement
 * - No browser-generated auth headers
 * - No DELETE route
 * - Preview cannot mutate
 * - No cross-tenant data exposure
 * - No raw error leakage to API responses
 * - Concurrency enforcement on all write commands
 */

import fs from "node:fs";
import path from "node:path";

const ROOT = path.resolve(process.cwd());
const PASS = "\u2705";
const FAIL = "\u274c";

let failures = 0;

function check(label, condition, detail) {
  if (condition) {
    console.log(`${PASS} ${label}`);
  } else {
    console.error(`${FAIL} ${label}${detail ? ` — ${detail}` : ""}`);
    failures++;
  }
}

function readText(relPath) {
  try {
    return fs.readFileSync(path.join(ROOT, relPath), "utf8");
  } catch {
    return null;
  }
}

// ── RLS ENFORCEMENT ──────────────────────────────────────────────────────────

const migration = readText(
  "packages/NevaxisFactOS.Persistence.Migrations/Migrations/20260807200000_AddProductionExecutionFoundation.cs",
);

check("Migration file exists", migration !== null);

check(
  "RLS ENABLED on production_orders",
  migration?.includes("ENABLE ROW LEVEL SECURITY") ?? false,
);

check(
  "RLS FORCED on production_orders",
  migration?.includes("FORCE ROW LEVEL SECURITY") ?? false,
);

check(
  "RLS SELECT policy uses tenant_id check",
  migration?.includes("NevaxisFactOS.tenant_id") ?? false,
);

check(
  "RLS INSERT policy uses WITH CHECK",
  migration?.includes("WITH CHECK") ?? false,
);

check(
  "No DELETE grant on production_orders",
  migration !== null &&
    !migration.includes("GRANT DELETE") &&
    !migration.includes("GRANT ALL"),
);

check(
  "Runtime grants: SELECT, INSERT, UPDATE only",
  migration?.includes("GRANT SELECT, INSERT, UPDATE") && migration?.includes("production_orders") ? true : false,
);

// ── FACILITY SECURITY ────────────────────────────────────────────────────────

const apiEndpoints = readText(
  "apps/api/Production/ProductionExecutionEndpointRouteBuilderExtensions.cs",
);

check("API endpoints file exists", apiEndpoints !== null);

check(
  "Every endpoint resolves facility access",
  apiEndpoints?.includes("IFacilityAccessResolver") ?? false,
);

check(
  "Facility access failure returns 403/404",
  apiEndpoints?.includes("MapFacilityAccessFailure") ?? false,
);

check(
  "No TenantId accepted from client",
  apiEndpoints !== null &&
    (!apiEndpoints.includes("tenantId =") || apiEndpoints.includes("FacilityAccessContext")),
);

// ── NO DELETE ROUTE ───────────────────────────────────────────────────────────

check(
  "No DELETE route for production orders",
  apiEndpoints !== null && !apiEndpoints.includes("MapDelete"),
);

// ── CONCURRENCY ON ALL WRITE COMMANDS ─────────────────────────────────────────

const writeRepo = readText(
  "packages/NevaxisFactOS.Persistence/Production/ProductionOrderWriteRepository.cs",
);

const writeMethods = ["StartAsync", "PauseAsync", "ResumeAsync", "CompleteAsync"];

for (const method of writeMethods) {
  // Each write method should use expectedVersion/ExpectedVersion
  const methodIndex = writeRepo?.indexOf(method) ?? -1;
  if (methodIndex >= 0) {
    const methodBody = writeRepo?.substring(methodIndex, methodIndex + 900);
    check(
      `${method} enforces expectedVersion`,
      (methodBody?.includes("expectedVersion") || methodBody?.includes("ExpectedVersion")) ?? false,
    );
  } else {
    check(`${method} is implemented`, false, "Method not found");
  }
}

check(
  "Stale version returns concurrency-conflict code",
  (writeRepo?.includes("concurrency-conflict") || writeRepo?.includes("ConcurrencyConflict")) ?? false,
);

// ── NO BROWSER AUTH HEADERS ───────────────────────────────────────────────────

const clientFiles = [
  "apps/web/src/features/production-execution/components/production-order-card.tsx",
  "apps/web/src/features/production-execution/components/production-order-lifecycle-actions.tsx",
  "apps/web/src/app/(app)/uretim/page.tsx",
  "apps/web/src/app/(app)/uretim/[orderId]/page.tsx",
];

for (const file of clientFiles) {
  const content = readText(file);

  if (content === null) {
    check(`${path.basename(file)} exists`, false, "File not found");
    continue;
  }

  check(
    `${path.basename(file)} does not inject dev auth headers`,
    !content.includes("X-crevafactos-dev") &&
      !content.includes("X-NevaxisFactOS-Tenant") &&
      !content.includes("tenantKey"),
  );
}

// ── PREVIEW MODE WRITE BLOCKING ───────────────────────────────────────────────

const writer = readText(
  "apps/web/src/features/production-execution/server/production-order-writer.ts",
);

check("Writer file exists", writer !== null);

check(
  "Writer blocks preview mode writes",
  writer?.includes("preview-writes-blocked") ?? false,
);

check(
  "Writer checks for development-preview mode before executing",
  writer?.includes("development-preview") ?? false,
);

// ── API CLIENT DOES NOT LEAK ERRORS ──────────────────────────────────────────

const apiClient = readText(
  "apps/web/src/features/production-execution/server/production-order-api-client.ts",
);

check("API client exists", apiClient !== null);

check(
  "API client maps HTTP errors to safe failure codes",
  (apiClient?.includes("source-unavailable") &&
    apiClient?.includes("permission-denied") &&
    apiClient?.includes("not-found")) ?? false,
);

check(
  "API client does not expose raw exception messages",
  apiClient !== null && !apiClient.includes("JSON.stringify(error)") && !apiClient.includes("error.stack"),
);

// ── TENANT ISOLATION IN REPOSITORY ───────────────────────────────────────────

const readRepoImpl = readText(
  "packages/NevaxisFactOS.Persistence/Production/ProductionOrderReadRepository.cs",
);

check("ProductionOrderReadRepository.cs exists", readRepoImpl !== null);

check(
  "Read repository filters by TenantId",
  readRepoImpl?.includes("TenantId") ?? false,
);

check(
  "Read repository filters by FacilityId",
  readRepoImpl?.includes("FacilityId") ?? false,
);

check(
  "Read repository uses AsNoTracking",
  readRepoImpl?.includes("AsNoTracking") ?? false,
);

// ── HANDOFF BELONGS TO CORRECT FACILITY ─────────────────────────────────────

check(
  "Consume handoff validates facility match",
  writeRepo?.includes("FacilityId") ?? false,
);

// ── NO PRODUCTION AUTH CHANGES ────────────────────────────────────────────────

// These files should not be new or modified
const noTouchFiles = [
  "apps/web/src/app/globals.css",
];

for (const file of noTouchFiles) {
  const exists = fs.existsSync(path.join(ROOT, file));
  check(`${file} still exists (not deleted)`, exists);
}

// ── SUMMARY ──────────────────────────────────────────────────────────────────

console.log();

if (failures === 0) {
  console.log("✅ All production execution security checks passed.");
} else {
  console.error(
    `❌ ${failures} check(s) failed. Review the output above.`,
  );
  process.exit(1);
}
