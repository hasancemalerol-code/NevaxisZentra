#!/usr/bin/env node
/**
 * verify-production-operator-assignment.mjs
 *
 * PRODUCTION-5L: Verifies structural correctness of operator assignment capability.
 * Checks: entity, repository contract, API endpoints, EF configuration,
 * order summary fields, permissions, web panel, server actions.
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

// ── 1. ENTITY FILES ───────────────────────────────────────────────────────────

check(
  "ProductionOperatorAssignment.cs exists",
  fileExists("packages/NevaxisFactOS.Persistence/Production/ProductionOperatorAssignment.cs"),
);

check(
  "IProductionOperatorRepository.cs exists",
  fileExists("packages/NevaxisFactOS.Persistence/Production/IProductionOperatorRepository.cs"),
);

check(
  "ProductionOperatorRepository.cs exists",
  fileExists("packages/NevaxisFactOS.Persistence/Production/ProductionOperatorRepository.cs"),
);

// ── 2. ENTITY FIELDS ──────────────────────────────────────────────────────────

const operatorEntity = readText(
  "packages/NevaxisFactOS.Persistence/Production/ProductionOperatorAssignment.cs",
);

check(
  "ProductionOperatorAssignment has ProductionOrderId",
  operatorEntity?.includes("ProductionOrderId") ?? false,
);

check(
  "ProductionOperatorAssignment has SubjectId",
  operatorEntity?.includes("SubjectId") ?? false,
);

check(
  "ProductionOperatorAssignment has AssignmentRole",
  operatorEntity?.includes("AssignmentRole") ?? false,
);

check(
  "ProductionOperatorAssignment has ReleasedAtUtc",
  operatorEntity?.includes("ReleasedAtUtc") ?? false,
);

check(
  "ProductionOperatorAssignment has ReleasedBySubjectId",
  operatorEntity?.includes("ReleasedBySubjectId") ?? false,
);

check(
  "ProductionOperatorAssignment has ReleaseReason",
  operatorEntity?.includes("ReleaseReason") ?? false,
);

check(
  "ProductionOperatorAssignment has AssignedBySubjectId",
  operatorEntity?.includes("AssignedBySubjectId") ?? false,
);

// ── 3. EF CONFIGURATION ───────────────────────────────────────────────────────

check(
  "ProductionOperatorAssignmentEntityConfiguration.cs exists",
  fileExists(
    "packages/NevaxisFactOS.Persistence/Configurations/ProductionOperatorAssignmentEntityConfiguration.cs",
  ),
);

const opConfig = readText(
  "packages/NevaxisFactOS.Persistence/Configurations/ProductionOperatorAssignmentEntityConfiguration.cs",
);

check(
  "Operator config has assignment release index",
  (opConfig?.includes("ix_production_operator_assignments_order_released") ?? false) ||
    (opConfig?.includes("ReleasedAtUtc") ?? false),
);

// ── 4. PRODUCTION ORDER SUMMARY FIELDS ───────────────────────────────────────

const productionOrder = readText(
  "packages/NevaxisFactOS.Persistence/Production/ProductionOrder.cs",
);

check(
  "ProductionOrder has PrimaryOperatorSubjectId",
  productionOrder?.includes("PrimaryOperatorSubjectId") ?? false,
);

check(
  "ProductionOrder has ActiveOperatorCount",
  productionOrder?.includes("ActiveOperatorCount") ?? false,
);

// ── 5. OPERATOR REPOSITORY ───────────────────────────────────────────────────

const operatorRepo = readText(
  "packages/NevaxisFactOS.Persistence/Production/IProductionOperatorRepository.cs",
);

check(
  "IProductionOperatorRepository has AssignOperatorAsync",
  operatorRepo?.includes("AssignOperatorAsync") ?? false,
);

check(
  "IProductionOperatorRepository has ReleaseOperatorAsync",
  operatorRepo?.includes("ReleaseOperatorAsync") ?? false,
);

check(
  "IProductionOperatorRepository has ListOperatorAssignmentsAsync",
  operatorRepo?.includes("ListOperatorAssignmentsAsync") ?? false,
);

// ── 6. API ENDPOINTS ──────────────────────────────────────────────────────────

const endpoints = readText(
  "apps/api/Production/ProductionExecutionEndpointRouteBuilderExtensions.cs",
);

check(
  "API has operators endpoint",
  endpoints?.includes("operators") ?? false,
);

check(
  "API accepts assignmentRole",
  (endpoints?.includes("assignmentRole") ?? false) ||
    (endpoints?.includes("AssignmentRole") ?? false),
);

// ── 7. PERMISSIONS ────────────────────────────────────────────────────────────

const permKeys = readText(
  "packages/NevaxisFactOS.Persistence/Authorization/ProductionExecutionPermissionKeys.cs",
);

check(
  "production.execution.assign permission defined (covers operator assignment)",
  permKeys?.includes('"production.execution.assign"') ?? false,
);

// ── 8. WEB LAYER ─────────────────────────────────────────────────────────────

check(
  "production-operator-panel.tsx exists",
  fileExists(
    "apps/web/src/features/production-execution/components/production-operator-panel.tsx",
  ),
);

const serverWriter = readText(
  "apps/web/src/features/production-execution/server/production-order-writer.ts",
);

check(
  "assignOperatorAction server action exists",
  serverWriter?.includes("assignOperatorAction") ?? false,
);

check(
  "releaseOperatorAction server action exists",
  serverWriter?.includes("releaseOperatorAction") ?? false,
);

// ── 9. API CONTRACTS ──────────────────────────────────────────────────────────

const apiContracts = readText(
  "apps/web/src/features/production-execution/server/production-order-api-contracts.ts",
);

check(
  "Web API contracts have primaryOperatorSubjectId",
  apiContracts?.includes("primaryOperatorSubjectId") ?? false,
);

check(
  "Web API contracts have activeOperatorCount",
  apiContracts?.includes("activeOperatorCount") ?? false,
);

// ── RESULT ────────────────────────────────────────────────────────────────────

console.log("");
if (failures === 0) {
  console.log(`${PASS} verify-production-operator-assignment: all checks passed.`);
  process.exit(0);
} else {
  console.error(`${FAIL} verify-production-operator-assignment: ${failures} check(s) failed.`);
  process.exit(1);
}
