#!/usr/bin/env node
/**
 * QUALITY-2L mandatory live smoke — narrow proof script.
 * Restores baseline after run (release holds, close NCRs).
 */
const BASE = process.argv[2] ?? "http://localhost:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const WRONG_FACILITY = "bb000001-0005-4000-8000-000000000099";
const PILOT_PLAN_ID = "bb000001-0060-4000-8000-000000000060";
const PILOT_DEF_ID = "bb000001-0061-4000-8000-000000000061";
const ISSUER = "crevafactos-dev";
const OPERATOR = "production-pilot-operator";
const REVIEWER = "planning-pilot-reviewer";
const PLANNER = "planning-pilot-user";
const TENANT = "dev-pilot-tenant";

let failures = 0;
const createdNcrIds = [];

function pass(k, d = "") {
  console.log(`✅ ${k}${d ? `: ${d}` : ""}`);
}
function fail(k, d = "") {
  failures++;
  console.error(`❌ ${k}${d ? `: ${d}` : ""}`);
}

const h = (subject = OPERATOR, extra = {}) => ({
  Accept: "application/json",
  "Content-Type": "application/json",
  "X-crevafactos-dev-Issuer": ISSUER,
  "X-crevafactos-dev-Subject": subject,
  "X-NevaxisZentra-Tenant": TENANT,
  ...extra,
});
const q = (path, facility = FACILITY) =>
  `${BASE}${path}${path.includes("?") ? "&" : "?"}facilityId=${facility}`;

async function api(method, path, body, headers = h()) {
  const resp = await fetch(path.startsWith("http") ? path : q(path), {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  const text = await resp.text();
  let json = null;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    json = { raw: text };
  }
  return { status: resp.status, json, text };
}

async function restoreBaseline() {
  console.log("\nRestoring baseline...");
  for (const ncrId of createdNcrIds) {
    let r = await api("GET", `/api/quality/ncrs/${ncrId}`);
    if (r.status !== 200) continue;
    let version = r.json.version ?? 1;

    if (r.json.holdStatus === "active" || r.json.activeHold?.status === "active") {
      const release = await api("POST", `/api/quality/ncrs/${ncrId}/hold/release`, {
        expectedVersion: version,
        releaseReason: "QUALITY-2L smoke baseline restore",
      }, h(REVIEWER));
      if (release.status === 200) {
        version = release.json.version ?? version + 1;
      }
    }

    r = await api("GET", `/api/quality/ncrs/${ncrId}`);
    if (r.status !== 200) continue;
    version = r.json.version ?? version;

    for (const action of r.json.correctiveActions ?? []) {
      if (action.status === "open") {
        const started = await api(
          "POST",
          `/api/quality/corrective-actions/${action.id}/start`,
          { expectedVersion: action.version, note: "restore" },
        );
        if (started.status === 200) {
          await api(
            "POST",
            `/api/quality/corrective-actions/${action.id}/complete`,
            { expectedVersion: started.json.version ?? action.version + 1, note: "restore" },
          );
        }
      } else if (action.status === "in-progress") {
        await api(
          "POST",
          `/api/quality/corrective-actions/${action.id}/complete`,
          { expectedVersion: action.version, note: "restore" },
        );
      }
    }

    r = await api("GET", `/api/quality/ncrs/${ncrId}`);
    if (r.status !== 200) continue;
    version = r.json.version ?? version;

    if (r.json.dispositionStatus !== "decided" && (r.json.severity === "major" || r.json.severity === "critical")) {
      const disposition = await api(
        "POST",
        `/api/quality/ncrs/${ncrId}/disposition`,
        { expectedVersion: version, dispositionType: "rework", note: "restore" },
        h(REVIEWER),
      );
      if (disposition.status === 200) {
        version = disposition.json.version ?? version + 1;
      }
    }

    r = await api("GET", `/api/quality/ncrs/${ncrId}`);
    if (r.status !== 200) continue;
    version = r.json.version ?? version;

    if (r.json.status !== "resolved" && r.json.status !== "closed") {
      const resolved = await api(
        "POST",
        `/api/quality/ncrs/${ncrId}/resolve`,
        { expectedVersion: version, note: "restore" },
        h(REVIEWER),
      );
      if (resolved.status === 200) {
        version = resolved.json.version ?? version + 1;
      }
    }

    if (r.json.status !== "closed") {
      await api(
        "POST",
        `/api/quality/ncrs/${ncrId}/close`,
        { expectedVersion: version, note: "restore" },
        h(REVIEWER),
      );
    }
  }
  pass("baseline-restore", `${createdNcrIds.length} NCR(s) processed`);
}

async function main() {
  console.log(`\nQUALITY-2L live smoke → ${BASE}\n`);

  let r = await api("GET", "/api/production/orders?limit=5");
  const orderId = r.json?.[0]?.id;
  const orderStatusBefore = r.json?.[0]?.status;
  if (r.status === 200 && orderId) pass("production-order-list", orderId);
  else fail("production-order-list", String(r.status));

  r = await api("POST", "/api/quality/inspections", {
    productionOrderId: orderId,
    qualityControlPlanId: PILOT_PLAN_ID,
    notes: "QUALITY-2L smoke",
  });
  const inspectionId = r.json?.id;
  let inspectionVersion = r.json?.version ?? 1;
  if ((r.status === 200 || r.status === 201) && inspectionId) pass("inspection-create", inspectionId);
  else fail("inspection-create", `${r.status} ${r.text?.slice(0, 120)}`);

  if (!inspectionId || !orderId) {
    console.error("\nAborting — missing inspection or order id.");
    process.exit(1);
  }

  r = await api("POST", `/api/quality/inspections/${inspectionId}/start`, {
    expectedVersion: inspectionVersion,
  });
  if (r.status === 200) {
    inspectionVersion = r.json.version ?? inspectionVersion + 1;
    pass("inspection-start", `v${inspectionVersion}`);
  } else fail("inspection-start", String(r.status));

  r = await api("POST", `/api/quality/inspections/${inspectionId}/results`, {
    expectedVersion: inspectionVersion,
    inspectionDefinitionId: PILOT_DEF_ID,
    measuredNumericValue: 56.0,
  });
  let resultId = null;
  if (r.status === 200) {
    inspectionVersion = r.json.version ?? inspectionVersion + 1;
    pass("fail-result", "recorded");
  } else fail("fail-result", String(r.status));

  r = await api("GET", `/api/quality/inspections/${inspectionId}`);
  resultId = r.json?.results?.[0]?.id ?? resultId;
  if (r.status === 200 && r.json?.overallResult === "fail") pass("overall-fail", r.json.overallResult);
  else fail("overall-fail", `${r.status} ${r.json?.overallResult}`);

  r = await api("POST", "/api/quality/ncrs", {
    productionOrderId: orderId,
    qualityInspectionId: inspectionId,
    qualityInspectionResultId: resultId,
    title: "QUALITY-2L smoke NCR",
    description: "Failed inspection linkage smoke",
    severity: "major",
    category: "inspection-failure",
  });
  const ncrId = r.json?.id;
  let ncrVersion = r.json?.version ?? 1;
  if ((r.status === 200 || r.status === 201) && ncrId) {
    createdNcrIds.push(ncrId);
    pass("ncr-create", ncrId);
  } else fail("ncr-create", `${r.status} ${r.text?.slice(0, 160)}`);

  if (!ncrId) {
    await restoreBaseline();
    process.exit(1);
  }

  r = await api("GET", `/api/quality/ncrs/${ncrId}`);
  if (r.status === 200 && r.json?.qualityInspectionId === inspectionId) {
    pass("ncr-inspection-link", r.json.qualityInspectionNumber ?? "linked");
  } else fail("ncr-inspection-link", `${r.status}`);

  r = await api("POST", "/api/quality/ncrs", {
    productionOrderId: orderId,
    qualityInspectionId: inspectionId,
    qualityInspectionResultId: resultId,
    title: "duplicate attempt",
    description: "should fail",
    severity: "major",
  });
  if (r.status === 409) pass("duplicate-ncr-409", "ok");
  else fail("duplicate-ncr-409", String(r.status));

  r = await api("POST", `/api/quality/ncrs/${ncrId}/hold`, {
    expectedVersion: ncrVersion,
    scope: "production-order",
    reason: "QUALITY-2L smoke hold",
  });
  if (r.status === 200) {
    ncrVersion = r.json.version ?? ncrVersion + 1;
    pass("hold-place", `v${ncrVersion}`);
  } else fail("hold-place", String(r.status));

  r = await api("GET", `/api/quality/production-orders/${orderId}/signal`);
  if (r.status === 200 && r.json?.blocking === true && r.json?.qualityHoldActive === true) {
    pass("blocking-signal", `${r.json.blocking} hold=${r.json.qualityHoldActive}`);
  } else fail("blocking-signal", `${r.status} ${JSON.stringify(r.json)}`);

  r = await api(
    "POST",
    `/api/quality/ncrs/${ncrId}/hold/release`,
    { expectedVersion: ncrVersion, releaseReason: "unauthorized attempt" },
    h(PLANNER),
  );
  if (r.status === 403) pass("hold-release-unauthorized-403", "planner denied");
  else fail("hold-release-unauthorized-403", String(r.status));

  r = await api(
    "POST",
    `/api/quality/ncrs/${ncrId}/hold/release`,
    { expectedVersion: ncrVersion, releaseReason: "authorized release" },
    h(REVIEWER),
  );
  if (r.status === 200) {
    ncrVersion = r.json.version ?? ncrVersion + 1;
    pass("hold-release-authorized", `v${ncrVersion}`);
  } else fail("hold-release-authorized", String(r.status));

  r = await api(
    "POST",
    `/api/quality/ncrs/${ncrId}/disposition`,
    { expectedVersion: ncrVersion, dispositionType: "rework", note: "smoke disposition" },
    h(REVIEWER),
  );
  if (r.status === 200) {
    ncrVersion = r.json.version ?? ncrVersion + 1;
    pass("disposition", `v${ncrVersion}`);
  } else fail("disposition", String(r.status));

  r = await api("POST", `/api/quality/ncrs/${ncrId}/corrective-actions`, {
    title: "Smoke corrective action",
    description: "Complete during smoke",
  });
  const actionId = r.json?.id;
  let actionVersion = r.json?.version ?? 1;
  if (r.status === 200 || r.status === 201) pass("ca-create", actionId);
  else fail("ca-create", String(r.status));

  if (actionId) {
    r = await api("POST", `/api/quality/corrective-actions/${actionId}/start`, {
      expectedVersion: actionVersion,
      note: "start smoke",
    });
    if (r.status === 200) {
      actionVersion = r.json.version ?? actionVersion + 1;
      pass("ca-start", `v${actionVersion}`);
    } else fail("ca-start", String(r.status));

    r = await api("POST", `/api/quality/corrective-actions/${actionId}/complete`, {
      expectedVersion: actionVersion,
      note: "complete smoke",
    });
    if (r.status === 200) pass("ca-complete", "ok");
    else fail("ca-complete", String(r.status));
  }

  r = await api("GET", `/api/quality/ncrs/${ncrId}`);
  ncrVersion = r.json?.version ?? ncrVersion;

  r = await api(
    "POST",
    `/api/quality/ncrs/${ncrId}/resolve`,
    { expectedVersion: ncrVersion, note: "smoke resolve" },
    h(REVIEWER),
  );
  if (r.status === 200) {
    ncrVersion = r.json.version ?? ncrVersion + 1;
    pass("ncr-resolve", `v${ncrVersion}`);
  } else fail("ncr-resolve", String(r.status));

  r = await api(
    "POST",
    `/api/quality/ncrs/${ncrId}/close`,
    { expectedVersion: ncrVersion, note: "smoke close" },
    h(REVIEWER),
  );
  if (r.status === 200) {
    ncrVersion = r.json.version ?? ncrVersion + 1;
    pass("ncr-close", `v${ncrVersion}`);
  } else fail("ncr-close", String(r.status));

  r = await api("POST", "/api/quality/ncrs", {
    productionOrderId: orderId,
    title: "blocked close smoke",
    description: "hold guard",
    severity: "critical",
  });
  const guardNcrId = r.json?.id;
  let guardVersion = r.json?.version ?? 1;
  if ((r.status === 200 || r.status === 201) && guardNcrId) {
    createdNcrIds.push(guardNcrId);
    pass("guard-ncr-create", guardNcrId);
  } else fail("guard-ncr-create", String(r.status));

  if (guardNcrId) {
    await api("POST", `/api/quality/ncrs/${guardNcrId}/hold`, {
      expectedVersion: guardVersion,
      scope: "production-order",
      reason: "blocked close test",
    });
    guardVersion += 1;

    r = await api(
      "POST",
      `/api/quality/ncrs/${guardNcrId}/close`,
      { expectedVersion: guardVersion, note: "should fail" },
      h(REVIEWER),
    );
    if (r.status === 409 || r.status === 400) pass("blocked-close-guard", String(r.status));
    else fail("blocked-close-guard", String(r.status));
  }

  r = await api(
    "POST",
    `/api/quality/ncrs/${ncrId}/close`,
    { expectedVersion: ncrVersion, note: "stale" },
    h(REVIEWER),
  );
  if (r.status === 409) pass("stale-version-409", "ok");
  else fail("stale-version-409", String(r.status));

  r = await fetch(q("/api/quality/ncrs"), {
    method: "GET",
    headers: { Accept: "application/json", "X-crevafactos-dev-Subject": "missing-auth-subject" },
  });
  if (r.status === 401) pass("401-unauthenticated", "ok");
  else fail("401-unauthenticated", String(r.status));

  r = await api("POST", `/api/quality/ncrs/${ncrId}/hold`, {
    expectedVersion: 1,
    scope: "production-order",
    reason: "missing permission",
  }, h(PLANNER));
  if (r.status === 403) pass("missing-permission-403", "planner denied hold");
  else fail("missing-permission-403", String(r.status));

  r = await api("GET", q("/api/quality/ncrs", WRONG_FACILITY));
  if (r.status === 403) pass("wrong-facility-403", "ok");
  else fail("wrong-facility-403", String(r.status));

  r = await api("GET", `/api/production/orders?limit=5`);
  const orderStatusAfter = r.json?.[0]?.status;
  if (r.status === 200 && orderStatusBefore === orderStatusAfter) {
    pass("production-status-unchanged", orderStatusAfter ?? "same");
  } else fail("production-status-unchanged", `${orderStatusBefore} -> ${orderStatusAfter}`);

  r = await api("GET", `/api/quality/ncrs/${ncrId}`);
  if (r.status === 200 && r.json?.status === "closed") pass("readback", r.json.status);
  else fail("readback", `${r.status} ${r.json?.status}`);

  await restoreBaseline();

  console.log(
    failures === 0
      ? "\nQUALITY-2L live smoke PASSED\n"
      : `\nQUALITY-2L live smoke FAILED (${failures})\n`,
  );
  process.exit(failures === 0 ? 0 : 1);
}

main().catch(async (error) => {
  console.error(error);
  await restoreBaseline().catch(() => {});
  process.exit(1);
});
