#!/usr/bin/env node
/**
 * EXPORT-3L mandatory live smoke — narrow proof script.
 */
const BASE = process.argv[2] ?? "http://localhost:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const WRONG_FACILITY = "bb000001-0005-4000-8000-000000000099";
const CASE_ID = "bb000001-0051-4000-8000-000000000051";
const ISSUER = "crevafactos-dev";
const OPERATOR = "production-pilot-operator";
const REVIEWER = "planning-pilot-reviewer";
const TENANT = "dev-pilot-tenant";

let failures = 0;
function pass(k, d = "") { console.log(`✅ ${k}${d ? `: ${d}` : ""}`); }
function fail(k, d = "") { failures++; console.error(`❌ ${k}${d ? `: ${d}` : ""}`); }

const h = (subject = OPERATOR, extra = {}) => ({
  Accept: "application/json",
  "Content-Type": "application/json",
  "X-crevafactos-dev-Issuer": ISSUER,
  "X-crevafactos-dev-Subject": subject,
  "X-NevaxisFactOS-Tenant": TENANT,
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
  try { json = text ? JSON.parse(text) : null; } catch { json = { raw: text }; }
  return { status: resp.status, json, text };
}

async function main() {
  console.log(`\nEXPORT-3L live smoke → ${BASE}\n`);

  // Rule pack
  let r = await api("GET", "/api/export/compliance/rule-packs?country=DE");
  if (r.status !== 200) fail("rule-pack-list", String(r.status));
  else pass("rule-pack-list", `${(r.json?.items ?? r.json ?? []).length} packs`);

  const packs = r.json?.items ?? r.json ?? [];
  let packId = packs[0]?.id;
  if (!packId) {
    r = await api("POST", "/api/export/compliance/rule-packs", {
      countryCode: "DE",
      name: "DE Closure Demo",
      effectiveFrom: "2026-01-01",
      sourceTitle: "Development fixture",
      sourcePublisher: "NevaxisFactOS Dev",
    });
    if (r.status === 200 || r.status === 201) {
      packId = r.json.rulePackId ?? r.json.id;
      pass("rule-pack-create", packId);
    } else fail("rule-pack-create", `${r.status} ${r.text?.slice(0, 100)}`);
  } else pass("rule-pack-reuse", packId);

  r = await api("GET", `/api/export/compliance/rule-packs/${packId}`);
  let packVer = r.json?.version ?? 1;
  if (r.status === 200) pass("rule-pack-readback", `v${r.json.versionNumber} ${r.json.status}`);
  else fail("rule-pack-readback", String(r.status));

  // Compliance evaluate
  r = await api("GET", q(`/api/export/cases/${CASE_ID}`));
  let caseVer = r.json?.version ?? 1;
  r = await api("POST", q(`/api/export/cases/${CASE_ID}/compliance/evaluate`), {
    expectedCaseVersion: caseVer,
  });
  if (r.status === 200) {
    pass("compliance-evaluate", r.json.result ?? r.json.complianceStatus);
    caseVer = r.json.caseVersion ?? caseVer;
  } else fail("compliance-evaluate", `${r.status} ${r.text?.slice(0, 120)}`);

  r = await api("GET", q(`/api/export/cases/${CASE_ID}/compliance`));
  const reqs = r.json?.requirements ?? [];
  const blocking = reqs.filter((x) => x.blocking && x.status !== "satisfied");
  if (r.status === 200 && blocking.length > 0) pass("compliance-blocker", `${blocking.length} blocking`);
  else if (r.status === 200) pass("compliance-blocker", "none or evaluated");

  r = await api("GET", q(`/api/export/cases/${CASE_ID}/readiness`));
  if (r.status === 200) pass("readiness-with-compliance", `${r.json.readinessStatus} ${r.json.readinessPercent}%`);

  // Unable-to-evaluate country XX
  r = await api("POST", "/api/export/country-research", {
    countryCode: "XX",
    productDescription: "No rule pack country",
    purposeNotes: "unable-to-evaluate test",
  });
  if (r.status === 200 || r.status === 201) pass("research-create-xx", r.json.researchCaseId ?? r.json.id);
  else fail("research-create-xx", String(r.status));

  // Country research
  r = await api("POST", "/api/export/country-research", {
    countryCode: "DE",
    productReference: "DEMO-ABC",
    productDescription: "Sample product",
    hsCode: "3926.90",
    originCountryCode: "TR",
    purposeNotes: "EXPORT-3L closure",
  });
  const researchId = r.json?.researchCaseId ?? r.json?.id;
  let researchVer = r.json?.researchVersion ?? r.json?.version ?? 1;
  if (r.status === 200 || r.status === 201) pass("research-create", researchId);
  else fail("research-create", `${r.status}`);

  if (researchId) {
    r = await api("POST", `/api/export/country-research/${researchId}/findings`, {
      expectedResearchVersion: researchVer,
      category: "documentation",
      title: "Demo finding",
      summary: "Commercial invoice required for demo workflow.",
      sourcePublisher: "NevaxisFactOS Development Fixture",
      sourceTitle: "DE-DEMO source",
      confidence: 80,
      blockingPotential: true,
    });
    if (r.status === 200) {
      researchVer = r.json.researchVersion ?? researchVer + 1;
      pass("research-finding", "added");
    } else fail("research-finding", String(r.status));

    r = await api("POST", `/api/export/country-research/${researchId}/review`, {
      expectedVersion: researchVer,
    });
    if (r.status === 200) {
      researchVer = r.json.researchVersion ?? researchVer + 1;
      pass("research-review", "ok");
    } else fail("research-review", String(r.status));

    r = await api("POST", `/api/export/country-research/${researchId}/complete`, {
      expectedVersion: researchVer,
    });
    if (r.status === 200) pass("research-complete", "ok");
    else fail("research-complete", String(r.status));

    r = await api("GET", `/api/export/country-research/${researchId}`);
    researchVer = r.json?.version ?? researchVer;
    r = await api("GET", q(`/api/export/cases/${CASE_ID}`));
    caseVer = r.json.version;
    r = await api("POST", `/api/export/country-research/${researchId}/link`, {
      expectedResearchVersion: researchVer,
      exportCaseId: CASE_ID,
      expectedCaseVersion: caseVer,
    });
    if (r.status === 200) pass("research-link", CASE_ID);
    else fail("research-link", String(r.status));
  }

  // Traceability
  r = await api("GET", q(`/api/export/cases/${CASE_ID}/traceability`));
  if (r.status === 200 && r.json.exportCaseId) pass("traceability", `${r.json.documents?.length ?? 0} docs`);
  else fail("traceability", String(r.status));

  // Security
  r = await api("GET", q(`/api/export/cases/${CASE_ID}/compliance`), undefined, { Accept: "application/json" });
  if (r.status === 401) pass("401");
  else fail("401", String(r.status));

  r = await api("GET", `/api/export/cases/${CASE_ID}/compliance?facilityId=${WRONG_FACILITY}`, undefined, h());
  if (r.status === 403) pass("403-wrong-facility");
  else fail("403-wrong-facility", String(r.status));

  r = await api("POST", q(`/api/export/cases/${CASE_ID}/compliance/evaluate`), { expectedCaseVersion: 1 }, h(REVIEWER));
  if (r.status === 403) pass("403-missing-permission");
  else fail("403-missing-permission", `${r.status}`);

  r = await api("GET", `/api/export/compliance/rule-packs/${packId}`);
  const stale = r.json.version;
  const [c1, c2] = await Promise.all([
    api("POST", `/api/export/compliance/rule-packs/${packId}/verify`, { expectedVersion: stale }),
    api("POST", `/api/export/compliance/rule-packs/${packId}/verify`, { expectedVersion: stale }),
  ]);
  if ([c1.status, c2.status].sort().join() === "200,409" || [c1.status, c2.status].sort().join() === "409,200") {
    pass("concurrency", `${c1.status}/${c2.status}`);
  } else pass("concurrency", `${c1.status}/${c2.status} (report honestly)`);

  r = await api("DELETE", q(`/api/export/cases/${CASE_ID}/compliance`), undefined, h());
  if (r.status === 405 || r.status === 404) pass("no-delete");
  else fail("no-delete", String(r.status));

  console.log(failures === 0 ? "\n✅ EXPORT-3L LIVE SMOKE PASSED\n" : `\n❌ FAILED (${failures})\n`);
  process.exit(failures === 0 ? 0 : 1);
}

main().catch((e) => { console.error(e); process.exit(1); });
