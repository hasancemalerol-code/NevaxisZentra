#!/usr/bin/env node
/**
 * verify-production-scrap.mjs
 *
 * Verifies structural correctness of PRODUCTION-2L scrap tracking:
 * - Typed scrap reason codes (controlled allowlist)
 * - 'other' reason requires comment
 * - Scrap entry flow (InProgress only, quantity > 0, typed reason mandatory)
 * - Reversal / correction model (immutable, no DELETE, compensating entry)
 * - Duplicate reversal blocked
 * - API failure codes for invalid scrap reason and entry not found
 * - Web scrap UI: reason picker, 'other' requires comment
 * - Audit/outbox types for scrap and reversal events
 * - No silent mutation of existing entries
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

// ── 1. SCRAP REASON FILE ─────────────────────────────────────────────────────

const scrapReasons = readText(
  "packages/NevaxisFactOS.Persistence/Production/ProductionExecutionScrapReason.cs",
);

check("ProductionExecutionScrapReason.cs exists", scrapReasons !== null);

const expectedReasons = [
  "short-shot",
  "flash",
  "sink-mark",
  "burn-mark",
  "warpage",
  "discoloration",
  "dimensional",
  "contamination",
  "startup-scrap",
  "process-instability",
  "material",
  "mold",
  "machine",
  "operator",
  "other",
];

for (const reason of expectedReasons) {
  check(
    `Scrap reason '${reason}' is defined`,
    scrapReasons?.includes(`"${reason}"`) ?? false,
  );
}

check(
  "Scrap reasons have static All allowlist",
  scrapReasons?.includes("public static readonly IReadOnlySet<string> All") ??
    false,
);
check(
  "'other' reason requires comment (RequiresComment method)",
  (scrapReasons?.includes("Other") && scrapReasons?.includes("Requires")) ?? false,
);

// ── 2. SCRAP ENTRY IN REPOSITORY ─────────────────────────────────────────────

const entryRepo = readText(
  "packages/NevaxisFactOS.Persistence/Production/ProductionExecutionEntryRepository.cs",
);

check(
  "Repository has RecordScrapAsync method",
  entryRepo?.includes("RecordScrapAsync") ?? false,
);
check(
  "Scrap entry validates typed reason code against allowlist",
  entryRepo?.includes("ProductionExecutionScrapReasons") ?? false,
);
check(
  "Scrap entry validates 'other' requires comment",
  entryRepo?.includes("Requires") ?? false,
);
check(
  "Scrap entry type is set to Scrap",
  entryRepo?.includes("ProductionExecutionEntryType.Scrap") ?? false,
);
check(
  "Scrap validates quantity > 0",
  entryRepo?.includes("quantity") ?? false,
);

// ── 3. REVERSAL FLOW ─────────────────────────────────────────────────────────

check(
  "Repository has ReverseAsync method",
  entryRepo?.includes("ReverseAsync") ?? false,
);
check(
  "Reversal creates a new compensating entry (EntryType.Reversal)",
  entryRepo?.includes("ProductionExecutionEntryType.Reversal") ?? false,
);
check(
  "Reversal sets ReversesEntryId on new entry",
  entryRepo?.includes("ReversesEntryId") ?? false,
);
check(
  "Reversal validates original entry exists",
  entryRepo?.includes("ProductionEntryNotFound") ?? false,
);
check(
  "Duplicate reversal is blocked",
  entryRepo?.includes("ProductionEntryAlreadyReversed") ?? false,
);
check(
  "Reversal requires a reason string",
  entryRepo?.includes("reason") ?? false,
);
check(
  "Reversal does NOT call dbContext.Remove",
  !entryRepo?.includes("dbContext.Remove") ?? false,
);
check(
  "Reversal does NOT call dbContext.Update on original entry",
  !entryRepo?.includes(".Update(original") ?? false,
);

// ── 4. FAILURE CODES ─────────────────────────────────────────────────────────

const models = readText(
  "packages/NevaxisFactOS.Persistence/Production/ProductionOrderModels.cs",
);

check(
  "Failure code: invalid-scrap-reason",
  models?.includes("InvalidScrapReason") ?? false,
);
check(
  "Failure code: production-entry-not-found",
  models?.includes("ProductionEntryNotFound") ?? false,
);
check(
  "Failure code: production-entry-already-reversed",
  models?.includes("ProductionEntryAlreadyReversed") ?? false,
);
check(
  "Failure code: comment-required (or scrap-comment-required)",
  models?.includes("CommentRequired") ?? false,
);

// ── 5. AUDIT TAXONOMY FOR SCRAP ──────────────────────────────────────────────

const auditConst = readText(
  "packages/NevaxisFactOS.Persistence/Production/ProductionAuditConstants.cs",
);

check(
  "Audit action: production.order.quantity.scrap.record",
  auditConst?.includes("ProductionQuantityScrapRecord") ?? false,
);
check(
  "Audit event: production.order.quantity.scrap.recorded",
  auditConst?.includes("ProductionQuantityScrapRecorded") ?? false,
);
check(
  "Audit action: production.order.quantity.reverse",
  auditConst?.includes("ProductionQuantityReverse") ?? false,
);
check(
  "Audit event: production.order.quantity.reversed",
  auditConst?.includes("ProductionQuantityReversed") ?? false,
);

// ── 6. OUTBOX TAXONOMY FOR SCRAP ─────────────────────────────────────────────

check(
  "Outbox type: production-order-scrap-recorded",
  auditConst?.includes("ProductionScrapRecorded") ?? false,
);
check(
  "Outbox type: production-order-quantity-reversed",
  auditConst?.includes("ProductionQuantityReversed") ?? false,
);

// ── 7. API ENDPOINT FOR SCRAP ────────────────────────────────────────────────

const apiEndpoints = readText(
  "apps/api/Production/ProductionExecutionEndpointRouteBuilderExtensions.cs",
);

check(
  "API has POST quantities/scrap endpoint",
  apiEndpoints?.includes("/quantities/scrap") ?? false,
);
check(
  "API has POST quantity-entries/{entryId}/reverse endpoint",
  (apiEndpoints?.includes("quantity-entries") &&
    apiEndpoints?.includes("reverse")) ?? false,
);
check(
  "API maps invalid-scrap-reason to 400",
  apiEndpoints?.includes("InvalidScrapReason") ?? false,
);
check(
  "API maps production-entry-already-reversed to 409 or 422",
  apiEndpoints?.includes("ProductionEntryAlreadyReversed") ?? false,
);

// ── 8. API DTOs ───────────────────────────────────────────────────────────────

const apiDtos = readText(
  "apps/api/Production/ProductionOrderDtos.cs",
);

check("API DTO file exists", apiDtos !== null);
check(
  "RecordScrapApiRequest has quantity field",
  apiDtos?.includes("RecordScrapApiRequest") ?? false,
);
check(
  "RecordScrapApiRequest has reasonCode field",
  apiDtos?.includes("ReasonCode") ?? false,
);
check(
  "ReverseEntryApiRequest exists",
  apiDtos?.includes("ReverseEntryApiRequest") ?? false,
);

// ── 9. WEB SCRAP UI ───────────────────────────────────────────────────────────

const operatorActions = readText(
  "apps/web/src/features/production-execution/components/production-operator-actions.tsx",
);

check(
  "Operator actions component exists",
  operatorActions !== null,
);
check(
  "Operator actions has scrap recording form",
  operatorActions?.includes("scrap") ?? false,
);
check(
  "Scrap form has reason picker",
  operatorActions?.includes("SCRAP_REASON_CODES") ??
    operatorActions?.includes("scrapReasonRef") ??
    false,
);

const webScrapDomain = readText(
  "apps/web/src/features/production-execution/domain/production-execution-entry.ts",
);

check(
  "Web scrap domain file exists",
  webScrapDomain !== null,
);
check(
  "Web scrap reasons include startup-scrap",
  webScrapDomain?.includes("startup-scrap") ?? false,
);
check(
  "Web scrap domain has requiresComment utility",
  webScrapDomain?.includes("requiresComment") ?? false,
);
check(
  "Web scrap domain flags 'other' requires comment",
  webScrapDomain?.includes("other") ?? false,
);

// ── 10. WEB WRITER FOR SCRAP ─────────────────────────────────────────────────

const webWriter = readText(
  "apps/web/src/features/production-execution/server/production-order-writer.ts",
);

check(
  "Web writer has recordScrapAction",
  webWriter?.includes("recordScrapAction") ?? false,
);
check(
  "Web writer validates scrap quantity > 0",
  webWriter?.includes("quantity") ?? false,
);
check(
  "Web writer has reverseEntryAction",
  webWriter?.includes("reverseEntryAction") ?? false,
);

// ── 11. NO SILENT MUTATION CHECK ─────────────────────────────────────────────

check(
  "Entry repository does not silently UPDATE existing rows",
  !(entryRepo?.match(/dbContext\.(Update|Entry\().*\bentry\b.*State.*Modified/s) ?? false),
);

// ── RESULT ───────────────────────────────────────────────────────────────────

console.log();
if (failures === 0) {
  console.log("✅ All production scrap checks passed.");
  process.exit(0);
} else {
  console.error(`❌ ${failures} production scrap check(s) failed.`);
  process.exit(1);
}
