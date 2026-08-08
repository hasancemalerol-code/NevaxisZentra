#!/usr/bin/env node
/**
 * verify-production-exception-routing.mjs
 *
 * PRODUCTION-5L: Verifies structural correctness of exception routing capability.
 * Checks: exception entity routing fields, API endpoints, permissions,
 * repository assign/unassign contracts, web server actions, audit taxonomy,
 * resolved-exception guard, web contracts.
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

function fileExists(relPath) {
  return fs.existsSync(path.join(ROOT, relPath));
}

// ── 1. EXCEPTION ENTITY ROUTING FIELDS ───────────────────────────────────────

const exceptionEntity = readText(
  "packages/CrevaFactOS.Persistence/Production/ProductionExecutionException.cs",
);

check(
  "ProductionExecutionException.cs exists",
  exceptionEntity !== null,
);

check(
  "Exception entity has AssignedToSubjectId",
  exceptionEntity?.includes("AssignedToSubjectId") ?? false,
);

check(
  "Exception entity has AssignedAtUtc",
  exceptionEntity?.includes("AssignedAtUtc") ?? false,
);

check(
  "Exception entity has AssignedBySubjectId",
  exceptionEntity?.includes("AssignedBySubjectId") ?? false,
);

// ── 2. ENTITY CONFIGURATION ───────────────────────────────────────────────────

const exceptionConfig = readText(
  "packages/CrevaFactOS.Persistence/Configurations/ProductionExecutionExceptionEntityConfiguration.cs",
);

check(
  "Exception EF config has assigned_to_subject_id column",
  (exceptionConfig?.includes("assigned_to_subject_id") ?? false) ||
    (exceptionConfig?.includes("AssignedToSubjectId") ?? false),
);

// ── 3. REPOSITORY CONTRACT ────────────────────────────────────────────────────

const exceptionRepo = readText(
  "packages/CrevaFactOS.Persistence/Production/IProductionExceptionRepository.cs",
);

check(
  "IProductionExceptionRepository has AssignExceptionAsync",
  exceptionRepo?.includes("AssignExceptionAsync") ?? false,
);

check(
  "IProductionExceptionRepository has UnassignExceptionAsync",
  exceptionRepo?.includes("UnassignExceptionAsync") ?? false,
);

check(
  "IProductionExceptionRepository has ResolveExceptionAsync",
  exceptionRepo?.includes("ResolveExceptionAsync") ?? false,
);

// ── 5. API ENDPOINTS ──────────────────────────────────────────────────────────

const endpoints = readText(
  "apps/api/Production/ProductionExecutionEndpointRouteBuilderExtensions.cs",
);

check(
  "API has exception assign endpoint",
  endpoints?.includes("/assign") ?? false,
);

check(
  "API has exception unassign endpoint",
  endpoints?.includes("/unassign") ?? false,
);

check(
  "API uses AssignedToSubjectId in contract",
  (endpoints?.includes("AssignedToSubjectId") ?? false) ||
    (endpoints?.includes("assignedToSubjectId") ?? false),
);

// ── 6. PERMISSIONS ────────────────────────────────────────────────────────────

const permKeys = readText(
  "packages/CrevaFactOS.Persistence/Authorization/ProductionExecutionPermissionKeys.cs",
);

check(
  "production.execution.exception.assign permission defined",
  permKeys?.includes('"production.execution.exception.assign"') ?? false,
);

// ── 7. RESOLVED EXCEPTION GUARD ───────────────────────────────────────────────

const writeRepo = readText(
  "packages/CrevaFactOS.Persistence/Production/ProductionOrderWriteRepository.cs",
);

check(
  "Write repository has resolved exception guard check",
  (writeRepo?.includes("resolved") ?? false) || (writeRepo?.includes("Resolved") ?? false),
);

// ── 8. WEB SERVER ACTIONS ────────────────────────────────────────────────────

const serverWriter = readText(
  "apps/web/src/features/production-execution/server/production-order-writer.ts",
);

check(
  "assignExceptionAction server action exists",
  serverWriter?.includes("assignExceptionAction") ?? false,
);

check(
  "unassignExceptionAction server action exists",
  serverWriter?.includes("unassignExceptionAction") ?? false,
);

// ── 9. WEB API CONTRACTS ─────────────────────────────────────────────────────

const exceptionApiContracts = readText(
  "apps/web/src/features/production-execution/server/production-execution-exceptions-api-contracts.ts",
);

check(
  "Exception API contracts have assignedToSubjectId",
  exceptionApiContracts?.includes("assignedToSubjectId") ?? false,
);

check(
  "Exception API contracts have assignedAtUtc",
  exceptionApiContracts?.includes("assignedAtUtc") ?? false,
);

// ── 10. AUDIT TAXONOMY ───────────────────────────────────────────────────────

const auditActions = readText(
  "packages/CrevaFactOS.Persistence/Audit/AuditActions.cs",
);

check(
  "AuditActions has ExceptionAssigned or equivalent",
  auditActions?.includes("ExceptionAssign") ?? false,
);

check(
  "AuditActions has ExceptionUnassigned or equivalent",
  auditActions?.includes("ExceptionUnassign") ?? false,
);

// ── RESULT ────────────────────────────────────────────────────────────────────

console.log("");
if (failures === 0) {
  console.log(`${PASS} verify-production-exception-routing: all checks passed.`);
  process.exit(0);
} else {
  console.error(`${FAIL} verify-production-exception-routing: ${failures} check(s) failed.`);
  process.exit(1);
}
