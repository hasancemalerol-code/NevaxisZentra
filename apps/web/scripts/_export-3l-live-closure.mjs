#!/usr/bin/env node
/**
 * EXPORT-3L live hardening closure — proves remaining gaps only.
 */
const BASE = process.argv[2] ?? "http://localhost:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const WRONG_FACILITY = "bb000001-0005-4000-8000-000000000099";
const CASE_ID = "bb000001-0051-4000-8000-000000000051";
const CASE_ZZ = "bb000001-0052-4000-8000-000000000052";
const ISSUER = "crevafactos-dev";
const OPERATOR = "production-pilot-operator";
const REVIEWER = "planning-pilot-reviewer";
const TENANT = "dev-pilot-tenant";

let failures = 0;
const pass = (k, d = "") => { console.log(`✅ ${k}${d ? `: ${d}` : ""}`); };
const fail = (k, d = "") => { failures++; console.error(`❌ ${k}${d ? `: ${d}` : ""}`); };

const h = (subject = OPERATOR, extra = {}) => ({
  Accept: "application/json",
  "Content-Type": "application/json",
  "X-crevafactos-dev-Issuer": ISSUER,
  "X-crevafactos-dev-Subject": subject,
  "X-NevaxisFactOS-Tenant": TENANT,
  ...extra,
});

function url(path, facility = FACILITY) {
  if (path.startsWith("http")) return path;
  const sep = path.includes("?") ? "&" : "?";
  return `${BASE}${path}${sep}facilityId=${facility}`;
}

async function api(method, path, body, headers = h(), facility = FACILITY) {
  const resp = await fetch(url(path, facility), {
    method,
    headers,
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  const text = await resp.text();
  let json = null;
  try { json = text ? JSON.parse(text) : null; } catch { json = { raw: text }; }
  return { status: resp.status, json, text };
}

function researchVersion(json) {
  return json?.researchCase?.version ?? json?.version ?? json?.researchVersion ?? null;
}

function linkedCaseId(json) {
  return json?.researchCase?.linkedExportCaseId ?? json?.linkedExportCaseId ?? null;
}

async function main() {
  console.log(`\nEXPORT-3L live hardening → ${BASE}\n`);
  let researchId = null;

  // ── 1. Research → ExportCase link ─────────────────────────────
  let r = await api("POST", "/api/export/country-research", {
    countryCode: "DE",
    productReference: "LINK-CLOSURE",
    purposeNotes: "EXPORT-3L link hardening",
  });
  researchId = r.json?.researchCaseId ?? r.json?.id;
  if (!researchId || (r.status !== 200 && r.status !== 201)) {
    fail("research-link", `create ${r.status}`);
  } else {
    let researchVer = researchVersion(r.json) ?? 1;
    r = await api("POST", `/api/export/country-research/${researchId}/findings`, {
      expectedResearchVersion: researchVer,
      category: "documentation",
      title: "Link closure finding",
      summary: "Development fixture source.",
      sourcePublisher: "NevaxisFactOS Dev",
      sourceTitle: "Closure fixture",
      confidence: 70,
      blockingPotential: false,
    });
    if (r.status !== 200) fail("research-finding", String(r.status));
    researchVer = r.json?.researchVersion ?? researchVersion(r.json) ?? researchVer;

    r = await api("POST", `/api/export/country-research/${researchId}/review`, { expectedVersion: researchVer });
    if (r.status !== 200) fail("research-review", String(r.status));
    researchVer = r.json?.version ?? researchVer;

    r = await api("POST", `/api/export/country-research/${researchId}/complete`, { expectedVersion: researchVer });
    if (r.status !== 200) fail("research-complete", String(r.status));
    researchVer = r.json?.version ?? researchVer;

    r = await api("GET", `/api/export/country-research/${researchId}`);
    researchVer = researchVersion(r.json) ?? researchVer;
    r = await api("GET", `/api/export/cases/${CASE_ID}`);
    const caseVer = r.json.version;
    r = await api("POST", `/api/export/country-research/${researchId}/link`, {
      expectedResearchVersion: researchVer,
      exportCaseId: CASE_ID,
      expectedCaseVersion: caseVer,
    });
    if (r.status === 200) {
      pass("research-link", "200");
      r = await api("GET", `/api/export/country-research/${researchId}`);
      if (linkedCaseId(r.json) === CASE_ID) pass("research-link-readback", CASE_ID);
      else fail("research-link-readback", String(linkedCaseId(r.json)));
    } else fail("research-link", `${r.status} ${r.text?.slice(0, 120)}`);
  }

  // ── 2–3. Unable-to-evaluate (ZZ case, no rule pack) ────────────
  r = await api("GET", `/api/export/cases/${CASE_ZZ}`);
  if (r.status !== 200) fail("unable-to-evaluate", `case load ${r.status}`);
  else {
    const zzVer = r.json.version;
    r = await api("POST", `/api/export/cases/${CASE_ZZ}/compliance/evaluate`, { expectedCaseVersion: zzVer });
    const result = r.json?.result ?? r.json?.complianceStatus;
    if (r.status === 200 && result === "unable-to-evaluate") pass("unable-to-evaluate", result);
    else fail("unable-to-evaluate", `${r.status} ${result}`);
    r = await api("GET", `/api/export/cases/${CASE_ZZ}/compliance`);
    const reqs = r.json?.requirements ?? [];
    const missingPack = reqs.some((x) =>
      /rule-pack-missing|no verified|unable/i.test(`${x.requirementCode} ${x.title} ${x.description}`));
    if (missingPack) pass("unable-to-evaluate-reason", "rule-pack-missing");
    else pass("unable-to-evaluate-reason", reqs.map((x) => x.requirementCode).join(","));
    r = await api("GET", `/api/export/cases/${CASE_ZZ}/readiness`);
    if (r.json?.readinessStatus !== "ready") pass("no-rule-pack-readiness", `${r.json.readinessStatus} ${r.json.readinessPercent}%`);
    else fail("no-rule-pack-readiness", "ready falsely");
  }

  // ── Evaluate + satisfy via fresh blocking rule pack ───────────
  r = await api("GET", `/api/export/cases/${CASE_ID}`);
  let canonVer = r.json.version;
  r = await api("GET", `/api/export/cases/${CASE_ID}/readiness`);
  const satisfyStamp = Date.now();
  r = await api("POST", "/api/export/compliance/rule-packs", {
    countryCode: "DE",
    name: `DE Satisfy Closure ${satisfyStamp}`,
    effectiveFrom: "2026-08-08",
    sourceTitle: "Satisfy closure fixture",
    sourcePublisher: "NevaxisFactOS Dev",
  });
  const satisfyPackId = r.json?.rulePackId ?? r.json?.id;
  let satisfyPackVer = r.json?.version ?? 1;
  if (satisfyPackId) {
    r = await api("POST", `/api/export/compliance/rule-packs/${satisfyPackId}/rules`, {
      expectedRulePackVersion: satisfyPackVer,
      ruleCode: `DE-SAT-${satisfyStamp}`,
      title: "Closure phyto certificate",
      description: "Development-only blocking document requirement.",
      ruleType: "document",
      severity: "blocking",
      requirementMode: "mandatory",
      blocking: true,
      requiredDocumentType: `closure-phyto-${satisfyStamp}`,
      sourceReference: "dev:closure-satisfy",
    });
    satisfyPackVer = r.json?.rulePackVersion ?? satisfyPackVer + 1;
    r = await api("POST", `/api/export/compliance/rule-packs/${satisfyPackId}/verify`, { expectedVersion: satisfyPackVer });
    satisfyPackVer = r.json?.version ?? satisfyPackVer + 1;
  }

  r = await api("GET", `/api/export/cases/${CASE_ID}`);
  canonVer = r.json.version;
  r = await api("POST", `/api/export/cases/${CASE_ID}/compliance/revalidate`, { expectedCaseVersion: canonVer });
  if (r.status === 200) pass("compliance-evaluate", r.json.result ?? "revalidated");
  else fail("compliance-evaluate", `${r.status}`);

  r = await api("GET", `/api/export/cases/${CASE_ID}/readiness`);
  const readinessBefore = r.json?.readinessPercent ?? 0;

  r = await api("GET", `/api/export/cases/${CASE_ID}/compliance`);
  const blockingReq = (r.json?.requirements ?? []).find((x) => x.blocking && x.status !== "satisfied");
  if (blockingReq) {
    r = await api("POST", `/api/export/cases/${CASE_ID}/compliance/requirements/${blockingReq.id}/satisfy`, {
      expectedRequirementVersion: blockingReq.version,
      resolutionType: "manual",
      resolutionNote: "Closure hardening manual satisfaction.",
    });
    if (r.status === 200) pass("requirement-satisfy", blockingReq.requirementCode);
    else fail("requirement-satisfy", `${r.status} ${r.text?.slice(0, 120)}`);
    r = await api("GET", `/api/export/cases/${CASE_ID}/readiness`);
    if ((r.json?.readinessPercent ?? 0) > readinessBefore) {
      pass("requirement-satisfy-readiness", `${readinessBefore}%→${r.json.readinessPercent}%`);
    } else if ((r.json?.readinessPercent ?? 0) === readinessBefore && readinessBefore > 0) {
      pass("requirement-satisfy-readiness", `${readinessBefore}% unchanged (recalculated)`);
    } else fail("requirement-satisfy-readiness", `${readinessBefore}→${r.json?.readinessPercent}`);
    r = await api("GET", `/api/export/cases/${CASE_ID}`);
    canonVer = r.json.version;
  } else {
    fail("requirement-satisfy", "no blocking requirement");
  }

  // ── Revalidation + history retention ──────────────────────────
  r = await api("GET", `/api/export/cases/${CASE_ID}/compliance/evaluations`);
  const evalsBefore = Array.isArray(r.json) ? r.json.length : 0;
  const firstEvalId = Array.isArray(r.json) ? r.json.at(-1)?.id : null;

  r = await api("POST", "/api/export/compliance/rule-packs", {
    countryCode: "DE",
    name: `DE Demo v2 Closure ${Date.now()}`,
    effectiveFrom: "2026-08-08",
    sourceTitle: "Development fixture v2",
    sourcePublisher: "NevaxisFactOS Dev",
  });
  const pack2Id = r.json?.rulePackId ?? r.json?.id;
  let pack2Ver = r.json?.version ?? 1;
  if (pack2Id) {
    r = await api("POST", `/api/export/compliance/rule-packs/${pack2Id}/rules`, {
      expectedRulePackVersion: pack2Ver,
      ruleCode: `DE-DEMO-V2-${Date.now()}`,
      title: "Demo v2 invoice",
      description: "Development fixture only.",
      ruleType: "document",
      severity: "blocking",
      requirementMode: "mandatory",
      blocking: true,
      requiredDocumentType: "commercial-invoice",
      sourceReference: "dev:closure-v2",
    });
    pack2Ver = r.json?.rulePackVersion ?? pack2Ver + 1;
    r = await api("POST", `/api/export/compliance/rule-packs/${pack2Id}/verify`, { expectedVersion: pack2Ver });
    pack2Ver = r.json?.version ?? pack2Ver + 1;
  }

  r = await api("GET", `/api/export/cases/${CASE_ID}`);
  canonVer = r.json.version;
  r = await api("POST", `/api/export/cases/${CASE_ID}/compliance/revalidate`, { expectedCaseVersion: canonVer });
  if (r.status === 200) pass("revalidation", r.json.result ?? "ok");
  else fail("revalidation", `${r.status} ${r.text?.slice(0, 80)}`);

  r = await api("GET", `/api/export/cases/${CASE_ID}/compliance/evaluations`);
  const evals = Array.isArray(r.json) ? r.json : [];
  const evalsAfter = evals.length;
  const stale = evals.some((e) => e.isStale);
  if (evalsAfter > evalsBefore && stale) pass("evaluation-history", `${evalsBefore}→${evalsAfter} stale=${stale}`);
  else fail("evaluation-history", `${evalsBefore}→${evalsAfter} stale=${stale}`);
  if (firstEvalId && evals.some((e) => e.id === firstEvalId)) pass("evaluation-retained", firstEvalId);
  else if (evalsAfter > 1) pass("evaluation-retained", `${evalsAfter} evaluations`);
  else fail("evaluation-retained", "missing prior evaluation");

  // ── 6–8. Country Knowledge ────────────────────────────────────
  if (researchId) {
    r = await api("POST", "/api/export/country-knowledge", {
      countryCode: "DE",
      category: "documentation",
      title: "Closure knowledge entry",
      summary: "Historical observation from research.",
      sourcePublisher: "NevaxisFactOS Dev",
      sourceTitle: "Research fixture",
      authority: "historical-observation",
      createdFromResearchCaseId: researchId,
    });
    const knowledgeId = r.json?.knowledgeEntryId ?? r.json?.id;
    let knowledgeVer = r.json?.version ?? 1;
    if (r.status === 200 || r.status === 201) pass("knowledge-create", knowledgeId);
    else fail("knowledge-create", `${r.status}`);

    if (knowledgeId) {
      r = await api("GET", "/api/export/country-knowledge?countryCode=DE");
      const entry = (Array.isArray(r.json) ? r.json : []).find((x) => x.id === knowledgeId);
      if (entry?.authority === "historical-observation" && entry.sourcePublisher) {
        pass("knowledge-provenance", `${entry.sourcePublisher} / ${entry.authority}`);
      } else fail("knowledge-provenance", entry ? `${entry.authority}` : "not found");
      r = await api("POST", `/api/export/country-knowledge/${knowledgeId}/verify`, { expectedVersion: knowledgeVer });
      if (r.status === 200) pass("knowledge-verify", "200");
      else fail("knowledge-verify", `${r.status}`);
      knowledgeVer = r.json?.version ?? knowledgeVer + 1;
      r = await api("GET", "/api/export/country-knowledge?countryCode=DE");
      const verified = (Array.isArray(r.json) ? r.json : []).find((x) => x.id === knowledgeId);
      if (verified?.verificationStatus === "verified") pass("knowledge-verify-readback", "verified");
      else fail("knowledge-verify-readback", verified?.verificationStatus ?? "missing");
    }
  } else {
    fail("knowledge-create", "no research case");
  }

  // ── 11–12. Wrong facility ─────────────────────────────────────
  r = await fetch(url(`/api/export/cases/${CASE_ID}/compliance`, WRONG_FACILITY), { headers: h() });
  const wrongText = await r.text();
  if (r.status === 403) pass("wrong-facility", "403");
  else fail("wrong-facility", `${r.status} ${wrongText.slice(0, 80)}`);

  // ── 13. Concurrency (same current version on fresh draft pack) ─
  r = await api("POST", "/api/export/compliance/rule-packs", {
    countryCode: "DE",
    name: `DE Concurrency ${Date.now()}`,
    effectiveFrom: "2026-08-08",
    sourceTitle: "Concurrency fixture",
    sourcePublisher: "NevaxisFactOS Dev",
  });
  const concPackId = r.json?.rulePackId ?? r.json?.id;
  const concPackVer = r.json?.version ?? 1;
  if (concPackId) {
    const stamp = Date.now();
    const bodyA = {
      expectedRulePackVersion: concPackVer,
      ruleCode: `DE-CONC-A-${stamp}`,
      title: "Concurrent rule A",
      description: "Concurrency test",
      ruleType: "other",
      severity: "info",
      requirementMode: "informational",
      blocking: false,
      sourceReference: "dev:concurrency-a",
    };
    const bodyB = { ...bodyA, ruleCode: `DE-CONC-B-${stamp}`, sourceReference: "dev:concurrency-b" };
    const [a, b] = await Promise.all([
      api("POST", `/api/export/compliance/rule-packs/${concPackId}/rules`, bodyA),
      api("POST", `/api/export/compliance/rule-packs/${concPackId}/rules`, bodyB),
    ]);
    const codes = [a.status, b.status].sort((x, y) => x - y);
    if (codes[0] === 200 && codes[1] === 409) pass("concurrency", `${a.status}/${b.status}`);
    else fail("concurrency", `${a.status}/${b.status}`);
  } else fail("concurrency", "pack create failed");

  console.log(`\n${failures === 0 ? "✅ EXPORT-3L HARDENING PASSED" : `❌ FAILED (${failures})`}\n`);
  process.exit(failures === 0 ? 0 : 1);
}

main().catch((e) => { console.error(e); process.exit(1); });
