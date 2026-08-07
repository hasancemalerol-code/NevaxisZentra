#!/usr/bin/env node
/**
 * verify-live-production-2l.mjs
 *
 * Live end-to-end verification of PRODUCTION-2L:
 * quantity recording, scrap, reversal, completion guard,
 * stale-concurrency rejection, audit/outbox, immutability.
 *
 * Uses:
 *   issuer  = crevafactos-dev
 *   subject = production-pilot-operator (has production.execution.record)
 *   facility = bb000001-0005-4000-8000-000000000005
 *
 * Usage:  node verify-live-production-2l.mjs [base-url]
 *   default base-url: http://localhost:5000
 */

const BASE = process.argv[2] ?? "http://localhost:5000";
const FACILITY_ID = "bb000001-0005-4000-8000-000000000005";
const ISSUER = "crevafactos-dev";
const OPERATOR_SUBJECT = "production-pilot-operator";
const PLANNER_SUBJECT = "planning-pilot-user";
const TENANT_KEY = "dev-pilot-tenant";

const PASS = "\u2705";
const FAIL = "\u274c";
const INFO = "\u2139\uFE0F ";

let failures = 0;
let orderId = null;
let currentVersion = null;
let scrapEntryId = null;
let goodEntry1Id = null;
const apiLog = [];

function log(msg) {
  console.log(msg);
}
function info(label, value) {
  console.log(`${INFO} ${label}: ${value}`);
}
function pass(label) {
  console.log(`${PASS} ${label}`);
}
function fail(label, detail) {
  console.error(`${FAIL} ${label}${detail ? ` — ${detail}` : ""}`);
  failures++;
}
function check(label, condition, detail) {
  if (condition) pass(label);
  else fail(label, detail);
}
function section(title) {
  console.log(`\n── ${title} ${"─".repeat(Math.max(0, 52 - title.length))}`);
}

function operatorHeaders() {
  return {
    "Content-Type": "application/json",
    "X-CrevaFactOS-Dev-Issuer": ISSUER,
    "X-CrevaFactOS-Dev-Subject": OPERATOR_SUBJECT,
    "X-CrevaFactOS-Tenant": TENANT_KEY,
  };
}
function plannerHeaders() {
  return {
    "Content-Type": "application/json",
    "X-CrevaFactOS-Dev-Issuer": ISSUER,
    "X-CrevaFactOS-Dev-Subject": PLANNER_SUBJECT,
    "X-CrevaFactOS-Tenant": TENANT_KEY,
  };
}
function noAuthHeaders() {
  return { "Content-Type": "application/json" };
}

async function apiGet(path, headers = operatorHeaders()) {
  const url = `${BASE}${path}`;
  const resp = await fetch(url, { method: "GET", headers });
  let body;
  try { body = await resp.json(); } catch { body = null; }
  apiLog.push({ method: "GET", url, status: resp.status });
  return { status: resp.status, body };
}

async function apiPost(path, data, headers = operatorHeaders()) {
  const url = `${BASE}${path}`;
  const resp = await fetch(url, {
    method: "POST",
    headers,
    body: JSON.stringify(data),
  });
  let body;
  try { body = await resp.json(); } catch { body = null; }
  apiLog.push({ method: "POST", url, status: resp.status });
  return { status: resp.status, body };
}

// ── SECTION 1: API REACHABILITY ───────────────────────────────────────────────
section("1. API REACHABILITY");

try {
  const resp = await fetch(`${BASE}/health/ready`, { signal: AbortSignal.timeout(5000) });
  check("API health endpoint reachable", resp.status === 200,
    `status=${resp.status}`);
} catch (e) {
  fail("API is not reachable", `${BASE} — ${e.message}`);
  console.error("Cannot proceed without a running API. Exiting.");
  process.exit(1);
}
info("API base URL", BASE);

// ── SECTION 2: LIST PRODUCTION ORDERS ─────────────────────────────────────────
section("2. LIST PRODUCTION ORDERS");

const listResp = await apiGet(
  `/api/production/orders?facilityId=${FACILITY_ID}`,
);
check("List orders returns 200", listResp.status === 200, `got ${listResp.status}`);
const orders = listResp.body ?? [];
check("Orders list is an array", Array.isArray(orders), JSON.stringify(listResp.body)?.slice(0, 100));
info("Total orders found", Array.isArray(orders) ? orders.length : "N/A");

// Find a Ready order or any order to test with
const readyOrder = Array.isArray(orders) ? orders.find(o => o.status === "ready") : null;
const inProgressOrder = Array.isArray(orders) ? orders.find(o => o.status === "in-progress") : null;
const anyUsable = readyOrder ?? inProgressOrder;
if (anyUsable) {
  info("Using order id", anyUsable.id);
  info("Order status", anyUsable.status);
  info("Order plannedQuantity", anyUsable.plannedQuantity ?? "null");
  info("Order goodQuantity", anyUsable.goodQuantity ?? 0);
  info("Order scrapQuantity", anyUsable.scrapQuantity ?? 0);
  orderId = anyUsable.id;
  currentVersion = anyUsable.version;
} else {
  fail("No Ready or InProgress production orders found in dev pilot facility",
    "Create a planning handoff first, then consume it to produce a Ready order");
  console.error("\nCannot proceed without a usable production order.");
  process.exit(1);
}

// ── SECTION 3: GET ORDER DETAIL ───────────────────────────────────────────────
section("3. ORDER DETAIL");

const detailResp = await apiGet(
  `/api/production/orders/${orderId}?facilityId=${FACILITY_ID}`,
);
check("Detail returns 200", detailResp.status === 200, `got ${detailResp.status}`);
const orderDetail = detailResp.body;
check("Detail has quantitySummary", orderDetail?.quantitySummary !== undefined);
const qs = orderDetail?.quantitySummary;
info("plannedQuantity", qs?.plannedQuantity ?? "null");
info("goodQuantity", qs?.goodQuantity);
info("scrapQuantity", qs?.scrapQuantity);
info("grossProducedQuantity", qs?.grossProducedQuantity);
info("remainingQuantity", qs?.remainingQuantity ?? "null");
info("completionPercentage", qs?.completionPercentage ?? "null");
check("quantitySummary has goodQuantity", qs?.goodQuantity !== undefined);
check("quantitySummary has scrapQuantity", qs?.scrapQuantity !== undefined);
check("quantitySummary has grossProducedQuantity", qs?.grossProducedQuantity !== undefined);

// ── SECTION 4: START (if Ready) ───────────────────────────────────────────────
section("4. START ORDER");

if (readyOrder) {
  const startResp = await apiPost(
    `/api/production/orders/${orderId}/start?facilityId=${FACILITY_ID}`,
    { expectedVersion: currentVersion },
  );
  check("Start returns 200", startResp.status === 200, `got ${startResp.status} — ${JSON.stringify(startResp.body)}`);
  if (startResp.status === 200) {
    currentVersion = startResp.body?.version ?? currentVersion + 1;
    info("Version after start", currentVersion);
    pass("Order started: Ready → InProgress");
  }
} else {
  info("Order already InProgress", "skipping start step");
  pass("Skipping start — order already InProgress");
}

// Refresh detail to get current version/status
const freshDetail = await apiGet(
  `/api/production/orders/${orderId}?facilityId=${FACILITY_ID}`,
);
currentVersion = freshDetail.body?.version ?? currentVersion;
info("Current version before quantities", currentVersion);
const initialGood = freshDetail.body?.quantitySummary?.goodQuantity ?? 0;
const initialScrap = freshDetail.body?.quantitySummary?.scrapQuantity ?? 0;
const plannedQty = freshDetail.body?.quantitySummary?.plannedQuantity ?? null;
info("Initial goodQuantity", initialGood);
info("Initial scrapQuantity", initialScrap);
info("plannedQuantity source", plannedQty !== null ? `${plannedQty} (from handoff)` : "null (no plan)");

// ── SECTION 5: UNAUTHENTICATED REJECTION ──────────────────────────────────────
section("5. AUTH NEGATIVE: UNAUTHENTICATED");

const unauthResp = await apiPost(
  `/api/production/orders/${orderId}/quantities/good?facilityId=${FACILITY_ID}`,
  { expectedVersion: currentVersion, quantity: 1 },
  noAuthHeaders(),
);
check("Unauthenticated good write returns 401", unauthResp.status === 401,
  `got ${unauthResp.status}`);

// ── SECTION 6: GOOD +100 ──────────────────────────────────────────────────────
section("6. GOOD +100");

const good1Resp = await apiPost(
  `/api/production/orders/${orderId}/quantities/good?facilityId=${FACILITY_ID}`,
  { expectedVersion: currentVersion, quantity: 100, comment: "live verify good 100" },
);
check("Good +100 returns 200/201", good1Resp.status === 200 || good1Resp.status === 201,
  `got ${good1Resp.status} — ${JSON.stringify(good1Resp.body)}`);
if (good1Resp.status === 200 || good1Resp.status === 201) {
  goodEntry1Id = good1Resp.body?.entryId;
  currentVersion = good1Resp.body?.version ?? currentVersion + 1;
  info("Entry id", goodEntry1Id);
  info("Version after good +100", currentVersion);

  const d1 = await apiGet(`/api/production/orders/${orderId}?facilityId=${FACILITY_ID}`);
  const qs1 = d1.body?.quantitySummary;
  check("GoodQuantity = initial + 100", qs1?.goodQuantity === initialGood + 100,
    `expected ${initialGood + 100}, got ${qs1?.goodQuantity}`);
  check("ScrapQuantity unchanged after good", qs1?.scrapQuantity === initialScrap,
    `expected ${initialScrap}, got ${qs1?.scrapQuantity}`);
  check("LastProductionEntryAtUtc set", qs1?.lastEntryAtUtc !== null && qs1?.lastEntryAtUtc !== undefined);
}

// ── SECTION 7: SCRAP +5 ───────────────────────────────────────────────────────
section("7. SCRAP +5 (startup-scrap)");

const scrapResp = await apiPost(
  `/api/production/orders/${orderId}/quantities/scrap?facilityId=${FACILITY_ID}`,
  { expectedVersion: currentVersion, quantity: 5, reasonCode: "startup-scrap", comment: "live verify scrap" },
);
check("Scrap +5 returns 200/201", scrapResp.status === 200 || scrapResp.status === 201,
  `got ${scrapResp.status} — ${JSON.stringify(scrapResp.body)}`);
if (scrapResp.status === 200 || scrapResp.status === 201) {
  scrapEntryId = scrapResp.body?.entryId;
  currentVersion = scrapResp.body?.version ?? currentVersion + 1;
  info("Scrap entry id", scrapEntryId);
  info("Version after scrap +5", currentVersion);

  const d2 = await apiGet(`/api/production/orders/${orderId}?facilityId=${FACILITY_ID}`);
  const qs2 = d2.body?.quantitySummary;
  check("GoodQuantity still = initial + 100 after scrap", qs2?.goodQuantity === initialGood + 100,
    `expected ${initialGood + 100}, got ${qs2?.goodQuantity}`);
  check("ScrapQuantity = initial + 5 after scrap", qs2?.scrapQuantity === initialScrap + 5,
    `expected ${initialScrap + 5}, got ${qs2?.scrapQuantity}`);
  check("GrossProducedQuantity = good + scrap",
    qs2?.grossProducedQuantity === (initialGood + 100) + (initialScrap + 5),
    `expected ${(initialGood + 100) + (initialScrap + 5)}, got ${qs2?.grossProducedQuantity}`);
}

// ── SECTION 8: GOOD +250 ──────────────────────────────────────────────────────
section("8. GOOD +250");

const good2Resp = await apiPost(
  `/api/production/orders/${orderId}/quantities/good?facilityId=${FACILITY_ID}`,
  { expectedVersion: currentVersion, quantity: 250 },
);
check("Good +250 returns 200/201", good2Resp.status === 200 || good2Resp.status === 201,
  `got ${good2Resp.status} — ${JSON.stringify(good2Resp.body)}`);
if (good2Resp.status === 200 || good2Resp.status === 201) {
  currentVersion = good2Resp.body?.version ?? currentVersion + 1;
  info("Version after good +250", currentVersion);

  const d3 = await apiGet(`/api/production/orders/${orderId}?facilityId=${FACILITY_ID}`);
  const qs3 = d3.body?.quantitySummary;
  const expectedGood = initialGood + 350;
  const expectedScrap = initialScrap + 5;
  const expectedGross = expectedGood + expectedScrap;
  check(`GoodQuantity = ${expectedGood}`, qs3?.goodQuantity === expectedGood,
    `expected ${expectedGood}, got ${qs3?.goodQuantity}`);
  check(`ScrapQuantity = ${expectedScrap}`, qs3?.scrapQuantity === expectedScrap,
    `expected ${expectedScrap}, got ${qs3?.scrapQuantity}`);
  check(`GrossProducedQuantity = ${expectedGross}`, qs3?.grossProducedQuantity === expectedGross,
    `expected ${expectedGross}, got ${qs3?.grossProducedQuantity}`);

  if (plannedQty !== null) {
    const expectedRemaining = Math.max(plannedQty - expectedGood, 0);
    const expectedPct = (expectedGood / plannedQty) * 100;
    check(`RemainingQuantity = max(${plannedQty} - ${expectedGood}, 0) = ${expectedRemaining}`,
      Math.abs((qs3?.remainingQuantity ?? -1) - expectedRemaining) < 0.01,
      `expected ~${expectedRemaining}, got ${qs3?.remainingQuantity}`);
    check(`CompletionPercentage = ${expectedPct.toFixed(2)}%`,
      Math.abs((qs3?.completionPercentage ?? -1) - expectedPct) < 0.1,
      `expected ~${expectedPct.toFixed(2)}, got ${qs3?.completionPercentage}`);
    info("CompletionPercentage", `${(qs3?.completionPercentage ?? 0).toFixed(2)}%`);
  } else {
    info("PlannedQuantity null", "remaining/percentage checks skipped");
  }
}

// ── SECTION 9: ENTRY HISTORY ──────────────────────────────────────────────────
section("9. ENTRY HISTORY");

const histResp = await apiGet(
  `/api/production/orders/${orderId}/quantity-entries?facilityId=${FACILITY_ID}&limit=20`,
);
check("Entry history returns 200", histResp.status === 200, `got ${histResp.status}`);
const entries = Array.isArray(histResp.body) ? histResp.body : [];
check("History has at least 3 entries (2 good + 1 scrap)", entries.length >= 3,
  `got ${entries.length}`);
check("History has GoodProduction entry", entries.some(e => e.entryType === "good-production"));
check("History has Scrap entry", entries.some(e => e.entryType === "scrap"));
check("Scrap entry has startup-scrap reason",
  entries.some(e => e.entryType === "scrap" && e.reasonCode === "startup-scrap"));
check("History entries have recordedBySubjectId (server actor)",
  entries.every(e => e.recordedBySubjectId && e.recordedBySubjectId.length > 0));
check("TenantId NOT exposed in history entries",
  entries.every(e => e.tenantId === undefined || e.tenantId === null));

// ── SECTION 10: PAUSE + WRITE REJECTION ──────────────────────────────────────
section("10. PAUSE + PAUSED WRITE REJECTION");

const pauseResp = await apiPost(
  `/api/production/orders/${orderId}/pause?facilityId=${FACILITY_ID}`,
  { expectedVersion: currentVersion, reason: "live verify pause" },
);
check("Pause returns 200", pauseResp.status === 200,
  `got ${pauseResp.status} — ${JSON.stringify(pauseResp.body)}`);
if (pauseResp.status === 200) {
  currentVersion = pauseResp.body?.version ?? currentVersion + 1;
  info("Version after pause", currentVersion);

  const goodWhilePaused = await apiPost(
    `/api/production/orders/${orderId}/quantities/good?facilityId=${FACILITY_ID}`,
    { expectedVersion: currentVersion, quantity: 1 },
  );
  check("Good write while Paused returns 409",
    goodWhilePaused.status === 409 || goodWhilePaused.status === 400,
    `got ${goodWhilePaused.status} — ${JSON.stringify(goodWhilePaused.body)}`);
  check("Paused write has invalid-transition code",
    goodWhilePaused.body?.code === "invalid-transition",
    `code=${goodWhilePaused.body?.code}`);
}

// ── SECTION 11: RESUME ────────────────────────────────────────────────────────
section("11. RESUME");

const resumeResp = await apiPost(
  `/api/production/orders/${orderId}/resume?facilityId=${FACILITY_ID}`,
  { expectedVersion: currentVersion },
);
check("Resume returns 200", resumeResp.status === 200,
  `got ${resumeResp.status} — ${JSON.stringify(resumeResp.body)}`);
if (resumeResp.status === 200) {
  currentVersion = resumeResp.body?.version ?? currentVersion + 1;
  info("Version after resume", currentVersion);
}

// ── SECTION 12: SCRAP REASON NEGATIVES ───────────────────────────────────────
section("12. SCRAP REASON NEGATIVES");

const invalidReasonResp = await apiPost(
  `/api/production/orders/${orderId}/quantities/scrap?facilityId=${FACILITY_ID}`,
  { expectedVersion: currentVersion, quantity: 1, reasonCode: "definitely-not-a-valid-reason" },
);
check("Invalid reason returns 400", invalidReasonResp.status === 400,
  `got ${invalidReasonResp.status}`);
check("Invalid reason code = invalid-scrap-reason",
  invalidReasonResp.body?.code === "invalid-scrap-reason",
  `code=${invalidReasonResp.body?.code}`);

const otherNoCommentResp = await apiPost(
  `/api/production/orders/${orderId}/quantities/scrap?facilityId=${FACILITY_ID}`,
  { expectedVersion: currentVersion, quantity: 1, reasonCode: "other" },
);
check("'other' without comment returns 400",
  otherNoCommentResp.status === 400 || otherNoCommentResp.status === 422,
  `got ${otherNoCommentResp.status}`);
check("'other' without comment uses scrap-reason or comment-required code",
  ["invalid-scrap-reason", "comment-required"].includes(otherNoCommentResp.body?.code),
  `code=${otherNoCommentResp.body?.code}`);

// Verify counters not changed
const afterNegScrap = await apiGet(
  `/api/production/orders/${orderId}?facilityId=${FACILITY_ID}`,
);
const versionAfterNegScrap = afterNegScrap.body?.version;
check("Version unchanged after negative scrap writes", versionAfterNegScrap === currentVersion,
  `expected ${currentVersion}, got ${versionAfterNegScrap}`);

// ── SECTION 13: REVERSAL ──────────────────────────────────────────────────────
section("13. REVERSAL");

if (scrapEntryId) {
  const reverseResp = await apiPost(
    `/api/production/orders/${orderId}/quantity-entries/${scrapEntryId}/reverse?facilityId=${FACILITY_ID}`,
    { expectedVersion: currentVersion, reason: "live verification correction" },
  );
  check("Reversal returns 200/201", reverseResp.status === 200 || reverseResp.status === 201,
    `got ${reverseResp.status} — ${JSON.stringify(reverseResp.body)}`);
  if (reverseResp.status === 200 || reverseResp.status === 201) {
    currentVersion = reverseResp.body?.version ?? currentVersion + 1;
    info("Version after reversal", currentVersion);

    const d4 = await apiGet(`/api/production/orders/${orderId}?facilityId=${FACILITY_ID}`);
    const qs4 = d4.body?.quantitySummary;
    check("ScrapQuantity returned to initial after reversal",
      qs4?.scrapQuantity === initialScrap,
      `expected ${initialScrap}, got ${qs4?.scrapQuantity}`);
    check("GoodQuantity unchanged by scrap reversal",
      qs4?.goodQuantity === initialGood + 350,
      `expected ${initialGood + 350}, got ${qs4?.goodQuantity}`);

    // History should show reversal entry
    const histResp2 = await apiGet(
      `/api/production/orders/${orderId}/quantity-entries?facilityId=${FACILITY_ID}&limit=20`,
    );
    const entries2 = Array.isArray(histResp2.body) ? histResp2.body : [];
    check("History has Reversal entry", entries2.some(e => e.entryType === "reversal"),
      `entries: ${entries2.map(e => e.entryType).join(", ")}`);
    const originalScrapEntry = entries2.find(e => e.id === scrapEntryId);
    check("Original scrap entry still exists (immutable)",
      originalScrapEntry !== undefined);
    if (originalScrapEntry) {
      check("Original scrap entry marked as reversed",
        originalScrapEntry.isReversed === true,
        `isReversed=${originalScrapEntry.isReversed}`);
    }
    const reversalEntry = entries2.find(e => e.entryType === "reversal");
    check("Reversal entry has reversesEntryId = scrap entry id",
      reversalEntry?.reversesEntryId === scrapEntryId,
      `reversesEntryId=${reversalEntry?.reversesEntryId}`);
  }
} else {
  fail("Scrap entry id not captured — skipping reversal test");
}

// ── SECTION 14: DUPLICATE REVERSAL ────────────────────────────────────────────
section("14. DUPLICATE REVERSAL");

if (scrapEntryId) {
  const dupRevResp = await apiPost(
    `/api/production/orders/${orderId}/quantity-entries/${scrapEntryId}/reverse?facilityId=${FACILITY_ID}`,
    { expectedVersion: currentVersion, reason: "duplicate attempt" },
  );
  check("Duplicate reversal returns 409", dupRevResp.status === 409,
    `got ${dupRevResp.status} — ${JSON.stringify(dupRevResp.body)}`);
  check("Duplicate reversal code = production-entry-already-reversed",
    dupRevResp.body?.code === "production-entry-already-reversed",
    `code=${dupRevResp.body?.code}`);
  const afterDup = await apiGet(`/api/production/orders/${orderId}?facilityId=${FACILITY_ID}`);
  check("Version unchanged after duplicate reversal attempt",
    afterDup.body?.version === currentVersion,
    `expected ${currentVersion}, got ${afterDup.body?.version}`);
}

// ── SECTION 15: ZERO / NEGATIVE QUANTITY ─────────────────────────────────────
section("15. INVALID QUANTITY NEGATIVES");

const zeroQty = await apiPost(
  `/api/production/orders/${orderId}/quantities/good?facilityId=${FACILITY_ID}`,
  { expectedVersion: currentVersion, quantity: 0 },
);
check("Zero quantity returns 400/422", zeroQty.status === 400 || zeroQty.status === 422,
  `got ${zeroQty.status}`);

const negQty = await apiPost(
  `/api/production/orders/${orderId}/quantities/good?facilityId=${FACILITY_ID}`,
  { expectedVersion: currentVersion, quantity: -10 },
);
check("Negative quantity returns 400/422", negQty.status === 400 || negQty.status === 422,
  `got ${negQty.status}`);

// ── SECTION 16: COMPLETION GUARD ─────────────────────────────────────────────
section("16. COMPLETION GUARD");

if (plannedQty !== null) {
  const currentGood = (await apiGet(`/api/production/orders/${orderId}?facilityId=${FACILITY_ID}`))
    .body?.quantitySummary?.goodQuantity ?? 0;
  info("Current goodQuantity", currentGood);
  info("PlannedQuantity", plannedQty);

  if (currentGood < plannedQty) {
    // Try normal complete — should fail
    const completeShortFail = await apiPost(
      `/api/production/orders/${orderId}/complete?facilityId=${FACILITY_ID}`,
      { expectedVersion: currentVersion },
    );
    check("Normal complete while below target returns 409",
      completeShortFail.status === 409 || completeShortFail.status === 400,
      `got ${completeShortFail.status} — ${JSON.stringify(completeShortFail.body)}`);
    check("Failure code = production-target-not-reached",
      completeShortFail.body?.code === "production-target-not-reached",
      `code=${completeShortFail.body?.code}`);

    // Short completion without reason
    const shortNoReason = await apiPost(
      `/api/production/orders/${orderId}/complete?facilityId=${FACILITY_ID}`,
      { expectedVersion: currentVersion, allowShortCompletion: true },
    );
    check("Short completion without reason returns 400",
      shortNoReason.status === 400 || shortNoReason.status === 422,
      `got ${shortNoReason.status}`);
    check("Missing reason code = completion-reason-required",
      shortNoReason.body?.code === "completion-reason-required",
      `code=${shortNoReason.body?.code}`);

    // Short completion with reason
    const shortComplete = await apiPost(
      `/api/production/orders/${orderId}/complete?facilityId=${FACILITY_ID}`,
      {
        expectedVersion: currentVersion,
        allowShortCompletion: true,
        completionReason: "Live verification short close",
      },
    );
    check("Short completion with reason returns 200",
      shortComplete.status === 200,
      `got ${shortComplete.status} — ${JSON.stringify(shortComplete.body)}`);
    if (shortComplete.status === 200) {
      currentVersion = shortComplete.body?.version ?? currentVersion + 1;
      info("Version after short complete", currentVersion);
      info("Order should now be Completed", "");

      const afterComplete = await apiGet(
        `/api/production/orders/${orderId}?facilityId=${FACILITY_ID}`,
      );
      check("Status = completed after short complete",
        afterComplete.body?.status === "completed",
        `status=${afterComplete.body?.status}`);
      check("ActualEndAtUtc is set",
        afterComplete.body?.actualEndAtUtc !== null && afterComplete.body?.actualEndAtUtc !== undefined);
    }
  } else {
    // GoodQuantity >= PlannedQuantity: normal complete should work
    info("GoodQuantity already >= PlannedQuantity", "attempting normal complete");
    const normalComplete = await apiPost(
      `/api/production/orders/${orderId}/complete?facilityId=${FACILITY_ID}`,
      { expectedVersion: currentVersion },
    );
    check("Normal complete when target reached returns 200",
      normalComplete.status === 200,
      `got ${normalComplete.status} — ${JSON.stringify(normalComplete.body)}`);
    if (normalComplete.status === 200) {
      currentVersion = normalComplete.body?.version ?? currentVersion + 1;
    }
  }
} else {
  // No planned quantity — normal complete should work
  info("No PlannedQuantity — skipping completion guard, attempting normal complete", "");
  const noGuardComplete = await apiPost(
    `/api/production/orders/${orderId}/complete?facilityId=${FACILITY_ID}`,
    { expectedVersion: currentVersion },
  );
  check("Complete without planned quantity returns 200",
    noGuardComplete.status === 200,
    `got ${noGuardComplete.status} — ${JSON.stringify(noGuardComplete.body)}`);
  if (noGuardComplete.status === 200) {
    currentVersion = noGuardComplete.body?.version ?? currentVersion + 1;
  }
}

// ── SECTION 17: COMPLETED WRITE LOCK ─────────────────────────────────────────
section("17. COMPLETED WRITE LOCK");

const completedGood = await apiPost(
  `/api/production/orders/${orderId}/quantities/good?facilityId=${FACILITY_ID}`,
  { expectedVersion: currentVersion, quantity: 1 },
);
check("Good write on Completed order returns 409",
  completedGood.status === 409 || completedGood.status === 400,
  `got ${completedGood.status}`);
check("Completed write has invalid-transition code",
  completedGood.body?.code === "invalid-transition",
  `code=${completedGood.body?.code}`);

const completedScrap = await apiPost(
  `/api/production/orders/${orderId}/quantities/scrap?facilityId=${FACILITY_ID}`,
  { expectedVersion: currentVersion, quantity: 1, reasonCode: "flash" },
);
check("Scrap write on Completed order returns 409",
  completedScrap.status === 409 || completedScrap.status === 400,
  `got ${completedScrap.status}`);

// ── SECTION 18: LIVE CONCURRENCY TEST ─────────────────────────────────────────
section("18. LIVE CONCURRENCY (stale write rejection)");

// Find/start a fresh InProgress order for concurrency test
// First try to find another ready order
const listResp2 = await apiGet(`/api/production/orders?facilityId=${FACILITY_ID}`);
const allOrders = Array.isArray(listResp2.body) ? listResp2.body : [];
const readyForConc = allOrders.find(o => o.status === "ready");
let concOrderId = null;
let concVersion = null;

if (readyForConc) {
  const startConc = await apiPost(
    `/api/production/orders/${readyForConc.id}/start?facilityId=${FACILITY_ID}`,
    { expectedVersion: readyForConc.version },
  );
  if (startConc.status === 200) {
    concOrderId = readyForConc.id;
    concVersion = startConc.body?.version ?? readyForConc.version + 1;
    info("Concurrency test order", concOrderId);
    info("Concurrency test version", concVersion);
  }
}

if (!concOrderId) {
  // Use a different InProgress order if available
  const inProg = allOrders.find(o => o.status === "in-progress" && o.id !== orderId);
  if (inProg) {
    concOrderId = inProg.id;
    concVersion = inProg.version;
    info("Using existing InProgress order for concurrency test", concOrderId);
  }
}

if (concOrderId && concVersion !== null) {
  // Fire two requests with the SAME expectedVersion simultaneously
  info("Firing two simultaneous requests with same expectedVersion", concVersion);
  const [reqA, reqB] = await Promise.all([
    apiPost(
      `/api/production/orders/${concOrderId}/quantities/good?facilityId=${FACILITY_ID}`,
      { expectedVersion: concVersion, quantity: 10 },
    ),
    apiPost(
      `/api/production/orders/${concOrderId}/quantities/scrap?facilityId=${FACILITY_ID}`,
      { expectedVersion: concVersion, quantity: 1, reasonCode: "flash" },
    ),
  ]);

  info("Request A (good +10) status", reqA.status);
  info("Request B (scrap +1) status", reqB.status);

  const successCount = [reqA, reqB].filter(
    r => r.status === 200 || r.status === 201,
  ).length;
  const conflictCount = [reqA, reqB].filter(r => r.status === 409).length;

  check("Exactly one request succeeds", successCount === 1,
    `successCount=${successCount}, statusA=${reqA.status}, statusB=${reqB.status}`);
  check("Exactly one request gets 409 concurrency-conflict", conflictCount === 1,
    `conflictCount=${conflictCount}`);

  const conflictBody = reqA.status === 409 ? reqA.body : reqB.body;
  check("409 response has concurrency-conflict code",
    conflictBody?.code === "concurrency-conflict",
    `code=${conflictBody?.code}`);

  // Verify DB state: version incremented exactly once
  const concDetail = await apiGet(
    `/api/production/orders/${concOrderId}?facilityId=${FACILITY_ID}`,
  );
  check("Version incremented by exactly 1 after one succeeds",
    concDetail.body?.version === concVersion + 1,
    `expected ${concVersion + 1}, got ${concDetail.body?.version}`);
} else {
  fail("No suitable order for concurrency test — need second Ready/InProgress order");
  info("CONCURRENCY TEST NOT RUN", "Only one suitable order was available");
}

// ── SECTION 19: CROSS-ORDER ENTRY NEGATIVE ────────────────────────────────────
section("19. CROSS-ORDER ENTRY NEGATIVE");

if (scrapEntryId && concOrderId && concOrderId !== orderId) {
  const crossOrderReverse = await apiPost(
    `/api/production/orders/${concOrderId}/quantity-entries/${scrapEntryId}/reverse?facilityId=${FACILITY_ID}`,
    { expectedVersion: concVersion ?? 1, reason: "cross-order attack" },
  );
  check("Cross-order reversal attempt returns 404 or 409",
    crossOrderReverse.status === 404 || crossOrderReverse.status === 409 || crossOrderReverse.status === 400,
    `got ${crossOrderReverse.status}`);
  check("Cross-order reversal does not succeed",
    crossOrderReverse.status !== 200 && crossOrderReverse.status !== 201,
    `got ${crossOrderReverse.status}`);
} else {
  info("Cross-order test", "Skipped (only one order or no scrap entry captured)");
  pass("Cross-order entry test skipped (only one usable order available)");
}

// ── SECTION 20: WRONG FACILITY NEGATIVE ───────────────────────────────────────
section("20. WRONG FACILITY NEGATIVE");

const wrongFacilityId = "00000000-0000-0000-0000-000000000001";
const wrongFacResp = await apiPost(
  `/api/production/orders/${orderId}/quantities/good?facilityId=${wrongFacilityId}`,
  { expectedVersion: 1, quantity: 1 },
);
check("Wrong facility returns 403 or 404",
  wrongFacResp.status === 403 || wrongFacResp.status === 404,
  `got ${wrongFacResp.status}`);

// ── RESULTS ───────────────────────────────────────────────────────────────────
section("RESULTS");

console.log(`\nAPI calls made: ${apiLog.length}`);
console.log(`Order tested: ${orderId}`);
console.log(`PlannedQuantity: ${plannedQty ?? "null (no plan data)"}`);
console.log(`Final version: ${currentVersion}`);
console.log();

if (failures === 0) {
  console.log("✅ All live PRODUCTION-2L checks passed.");
} else {
  console.error(`❌ ${failures} live check(s) failed.`);
}

console.log();
console.log("Concurrency test order:", concOrderId ?? "not run");
console.log("Scrap entry tested:", scrapEntryId ?? "none");
console.log("Good entry 1:", goodEntry1Id ?? "none");

process.exit(failures > 0 ? 1 : 0);
