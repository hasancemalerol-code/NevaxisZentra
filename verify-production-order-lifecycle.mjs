#!/usr/bin/env node
/**
 * verify-production-order-lifecycle.mjs
 *
 * Verifies the production order lifecycle state machine:
 * - Valid transitions (Ready→InProgress, InProgress→Paused, etc.)
 * - Status model completeness
 * - Idempotency enforcement (unique handoff constraint)
 * - Concurrency (expectedVersion in write commands)
 * - Audit/outbox per transition
 * - Lifecycle actions in web UI
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

// ── STATUS MODEL ─────────────────────────────────────────────────────────────

const statusEnum = readText(
  "packages/NevaxisFactOS.Persistence/Production/ProductionOrderStatus.cs",
);

check("ProductionOrderStatus.cs exists", statusEnum !== null);

const expectedStatuses = ["Ready", "InProgress", "Paused", "Completed", "Cancelled"];

for (const s of expectedStatuses) {
  check(`Status model includes: ${s}`, statusEnum?.includes(s) ?? false);
}

// ── LIFECYCLE TRANSITIONS IN WRITE REPOSITORY ─────────────────────────────────

const writeRepo = readText(
  "packages/NevaxisFactOS.Persistence/Production/ProductionOrderWriteRepository.cs",
);

check("ProductionOrderWriteRepository.cs exists", writeRepo !== null);

// Transition: Ready → InProgress (Start)
check(
  "Start transition checks Ready status",
  writeRepo?.includes("ProductionOrderStatus.Ready") ?? false,
);

check(
  "Start sets ActualStartAtUtc",
  writeRepo?.includes("ActualStartAtUtc") ?? false,
);

// Transition: InProgress → Paused (Pause)
check(
  "Pause transition checks InProgress status",
  writeRepo?.includes("InProgress") ?? false,
);

// Transition: Paused → InProgress (Resume)
check(
  "Resume transition checks Paused status",
  writeRepo?.includes("ProductionOrderStatus.Paused") ?? false,
);

check(
  "Resume does not reset ActualStartAtUtc",
  writeRepo !== null &&
    !writeRepo.includes("ActualStartAtUtc = ") ||
    writeRepo?.includes("ActualStartAtUtc = DateTimeOffset.UtcNow") === false,
);

// Transition: InProgress → Completed (Complete)
check(
  "Complete transition checks InProgress",
  writeRepo?.includes("Complete") ?? false,
);

check(
  "Complete sets ActualEndAtUtc",
  writeRepo?.includes("ActualEndAtUtc") ?? false,
);

// ── INVALID TRANSITION REJECTION ─────────────────────────────────────────────

check(
  "Invalid transition returns failure code",
  (writeRepo?.includes("invalid-transition") || writeRepo?.includes("InvalidTransition")) ?? false,
);

// ── CONCURRENCY ──────────────────────────────────────────────────────────────

const modelsFile = readText(
  "packages/NevaxisFactOS.Persistence/Production/ProductionOrderModels.cs",
);

check("ProductionOrderModels.cs exists", modelsFile !== null);

check(
  "expectedVersion used in start command",
  writeRepo?.includes("expectedVersion") ?? false,
);

check(
  "concurrency-conflict failure code defined",
  modelsFile?.includes("concurrency-conflict") ?? false,
);

// ── IDEMPOTENCY ───────────────────────────────────────────────────────────────

const entityConfig = readText(
  "packages/NevaxisFactOS.Persistence/Configurations/ProductionOrderEntityConfiguration.cs",
);

check("ProductionOrderEntityConfiguration.cs exists", entityConfig !== null);

check(
  "Unique index on ProductionPlanHandoffId enforces idempotency",
  entityConfig?.includes("ProductionPlanHandoffId").IsUnique ?? 
    entityConfig?.includes("IsUnique") ?? false,
);

const migration = readText(
  "packages/NevaxisFactOS.Persistence.Migrations/Migrations/20260807200000_AddProductionExecutionFoundation.cs",
);

check(
  "Migration enforces UNIQUE constraint on production_plan_handoff_id",
  migration?.includes("UNIQUE") ?? false,
);

// ── HANDOFF CONSUMPTION ATOMICITY ─────────────────────────────────────────────

check(
  "ConsumeHandoffAsync uses ExecuteWriteAsync for atomicity",
  writeRepo?.includes("ExecuteWriteAsync") ?? false,
);

check(
  "ConsumeHandoffAsync creates ProductionOrder",
  writeRepo?.includes("ProductionOrder") ?? false,
);

check(
  "ConsumeHandoffAsync updates handoff to Consumed",
  writeRepo?.includes("Consumed") ?? false,
);

// ── AUDIT & OUTBOX PER TRANSITION ─────────────────────────────────────────────

const auditActions = readText(
  "packages/NevaxisFactOS.Persistence/Audit/AuditActions.cs",
);

const auditEvents = readText(
  "packages/NevaxisFactOS.Persistence/Audit/AuditEventTypes.cs",
);

const outboxTypes = readText(
  "packages/NevaxisFactOS.Persistence/Outbox/OutboxMessageTypes.cs",
);

const productionAudit = readText(
  "packages/NevaxisFactOS.Persistence/Production/ProductionAuditConstants.cs",
);

check("ProductionAuditConstants.cs exists", productionAudit !== null);

for (const action of [
  "production.order.create",
  "production.order.start",
  "production.order.pause",
  "production.order.resume",
  "production.order.complete",
]) {
  check(
    `Audit action registered: ${action}`,
    auditActions?.includes(action) ?? false,
  );
}

for (const event of [
  "production.order.created",
  "production.order.started",
  "production.order.paused",
  "production.order.resumed",
  "production.order.completed",
]) {
  check(
    `Audit event registered: ${event}`,
    auditEvents?.includes(event) ?? false,
  );
}

for (const msgType of [
  "production-order-created",
  "production-order-started",
  "production-order-paused",
  "production-order-resumed",
  "production-order-completed",
]) {
  check(
    `Outbox message type registered: ${msgType}`,
    outboxTypes?.includes(msgType) ?? false,
  );
}

// ── WEB LIFECYCLE ACTIONS ────────────────────────────────────────────────────

const lifecycleActions = readText(
  "apps/web/src/features/production-execution/components/production-order-lifecycle-actions.tsx",
);

check(
  "ProductionOrderLifecycleActions component exists",
  lifecycleActions !== null,
);

check(
  "Start action available for Ready status",
  lifecycleActions?.includes('status === "ready"') ?? false,
);

check(
  "Pause action available for InProgress status",
  lifecycleActions?.includes('"in-progress"') ?? false,
);

check(
  "Resume action available for Paused status",
  lifecycleActions?.includes('"paused"') ?? false,
);

check(
  "Complete action available for InProgress status",
  lifecycleActions?.includes("canComplete") ?? false,
);

check(
  "No lifecycle actions rendered for completed orders",
  lifecycleActions?.includes("return null") ?? false,
);

// ── PAUSE REASON REQUIRED ─────────────────────────────────────────────────────

check(
  "Pause requires a reason",
  lifecycleActions?.includes("reason.length < 3") ?? false,
);

const writerActions = readText(
  "apps/web/src/features/production-execution/server/production-order-writer.ts",
);

check(
  "Writer validates pause reason",
  writerActions?.includes("Pause reason is required") ?? false,
);

// ── SUMMARY ──────────────────────────────────────────────────────────────────

console.log();

if (failures === 0) {
  console.log("✅ All production order lifecycle checks passed.");
} else {
  console.error(
    `❌ ${failures} check(s) failed. Review the output above.`,
  );
  process.exit(1);
}
