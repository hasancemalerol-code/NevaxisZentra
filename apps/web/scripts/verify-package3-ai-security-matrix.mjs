#!/usr/bin/env node
// RC3 Package 3 — HTTP-level AI security matrix against a LOCAL stack and a LOCAL ephemeral database.
//
// Opt-in operational check (never part of CI). It signs in as a local, test-only owner whose credentials live only in
// the ignored secret file named by P3_ACCOUNT_FILE (organizationCode, ownerEmail, password, totpSecret). Nothing
// secret is printed. It writes fixtures directly into the ephemeral database (a second tenant, entitlement
// definitions, permission overrides) and removes them again.
//
//   P3_PHASE=1  API running with RulesBasedAi disabled (the Production default)
//   P3_PHASE=2  API running with RulesBasedAi__Enabled=true (explicit customer configuration)
import { spawnSync } from "node:child_process";
import { createHmac, randomUUID } from "node:crypto";
import { readFileSync } from "node:fs";

const API = process.env.P3_API ?? "http://127.0.0.1:5000";
const PG_CONTAINER = process.env.P3_PG_CONTAINER ?? "nevaxiszentra-release-audit-postgres";
const PG_DB = process.env.P3_PG_DB ?? "zentra_p3_e2e";
const PG_USER = process.env.P3_PG_USER ?? "NevaxisZentra";
const PHASE = process.env.P3_PHASE ?? "1";
const account = JSON.parse(readFileSync(process.env.P3_ACCOUNT_FILE ?? "", "utf8"));

if (PG_DB === "zentra_release_audit" || PG_DB.startsWith("crevafactos")) {
  console.error("Refusing to run against a non-ephemeral database.");
  process.exit(2);
}

let failures = 0;
let passes = 0;
const check = (name, ok, detail = "") => {
  if (ok) passes++;
  else failures++;
  console.log(`${ok ? "PASS" : "FAIL"}  ${name}${detail ? `  [${detail}]` : ""}`);
};

function psql(sql) {
  const r = spawnSync("docker", ["exec", PG_CONTAINER, "psql", "-U", PG_USER, "-d", PG_DB, "-t", "-A", "-v", "ON_ERROR_STOP=1", "-c", sql], { encoding: "utf8" });
  if (r.status !== 0) throw new Error(`psql failed: ${(r.stderr || r.stdout).slice(0, 300)}`);
  return r.stdout.trim();
}

// ---- TOTP + cookie client ------------------------------------------------------------------------------------------
const B32 = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
const b32 = (s) => {
  let bits = "";
  for (const ch of s.replace(/=+$/, "").toUpperCase()) bits += B32.indexOf(ch).toString(2).padStart(5, "0");
  return Buffer.from(Array.from({ length: Math.floor(bits.length / 8) }, (_, i) => parseInt(bits.slice(i * 8, i * 8 + 8), 2)));
};
const totp = (secret, step = 0) => {
  const counter = Buffer.alloc(8);
  counter.writeBigUInt64BE(BigInt(Math.floor(Date.now() / 30000) + step));
  const h = createHmac("sha1", b32(secret)).update(counter).digest();
  const o = h[h.length - 1] & 15;
  return String((((h[o] & 127) << 24) | (h[o + 1] << 16) | (h[o + 2] << 8) | h[o + 3]) % 1_000_000).padStart(6, "0");
};

class Client {
  cookies = new Map();
  async request(method, path, body) {
    const headers = {};
    if (this.cookies.size) headers.cookie = [...this.cookies].map(([k, v]) => `${k}=${v}`).join("; ");
    const csrf = this.cookies.get("NevaxisZentra.csrf");
    if (csrf) headers["X-NevaxisZentra-Csrf"] = csrf;
    if (body !== undefined) headers["content-type"] = "application/json";
    const res = await fetch(API + path, { method, headers, body: body === undefined ? undefined : JSON.stringify(body), redirect: "manual" });
    for (const line of res.headers.getSetCookie?.() ?? []) {
      const [pair, ...attrs] = line.split(";");
      const i = pair.indexOf("=");
      const name = pair.slice(0, i).trim();
      const value = pair.slice(i + 1).trim();
      if (!value || attrs.some((a) => /^\s*max-age=0/i.test(a))) this.cookies.delete(name);
      else this.cookies.set(name, value);
    }
    const text = await res.text();
    let json = null;
    try {
      json = JSON.parse(text);
    } catch {
      /* not json */
    }
    return { status: res.status, json, text };
  }
}

async function login() {
  const c = new Client();
  const s = await c.request("POST", "/api/identity/sign-in", { organizationContext: account.organizationCode, email: account.ownerEmail, password: account.password });
  if (s.status !== 200) throw new Error(`sign-in ${s.status}`);
  if (s.json?.requiresMfa) {
    let v = await c.request("POST", "/api/identity/mfa/verify", { totpCode: totp(account.totpSecret) });
    if (v.status !== 200) v = await c.request("POST", "/api/identity/mfa/verify", { totpCode: totp(account.totpSecret, 1) });
    if (v.status !== 200) throw new Error(`mfa ${v.status}`);
  }
  return c;
}

// ---- fixtures ------------------------------------------------------------------------------------------------------
const TENANT_B = "b0000000-0000-4000-8000-0000000000b1";
const FACILITY_B = "b0000000-0000-4000-8000-0000000000b2";
const tenantA = psql(`select id from tenants where tenant_key='${account.organizationCode}'`);
const FAC = psql(`select id from facilities where tenant_id='${tenantA}' limit 1`);
const MEMBERSHIP = psql(`select id from tenant_memberships where tenant_id='${tenantA}' limit 1`);

psql(`insert into tenants(id,tenant_key,display_name,status,created_at_utc) values('${TENANT_B}','p3-foreign-tenant','P3 Foreign Tenant','Active',now()) on conflict do nothing`);
psql(`insert into facilities(id,tenant_id,code,name,status,time_zone_id,created_at_utc,updated_at_utc,version) values('${FACILITY_B}','${TENANT_B}','FOREIGN','Foreign Facility','Active','UTC',now(),now(),1) on conflict do nothing`);

const owner = await login();
const anon = new Client();
const q = (path) => `${path}${path.includes("?") ? "&" : "?"}facilityId=${FAC}`;

const taskBody = (title) => ({
  title, summary: "P3 matrix task", category: "quality", priority: "high", riskLevel: "high",
  detectedIssue: "P3 matrix", reasoning: "P3 matrix",
  recommendation: { title: "Review", description: "Review the finding", actionType: "review", requiresApproval: false, reversible: true, targetSystem: null },
  confidence: { score: 0.8, explanation: "matrix", modelVersion: null },
  impact: { financialAmount: null, currency: null, productionQuantity: null, productionUnit: null, estimatedDelayMinutes: 30, estimatedDowntimeMinutes: null, estimatedSavingsAmount: null, summary: "matrix" },
  sources: [{ sourceType: "manual", sourceId: null, sourceLabel: "matrix", observedAt: null, valueSummary: null }],
  siteId: null, productionLineId: null, machineId: null, moldId: null, productionOrderId: null, salesOrderId: null, materialId: null, supplierId: null, customerId: null, dueAtUtc: null,
});

const created = await owner.request("POST", q("/api/ai-tasks"), taskBody("P3 matrix task A"));
check("setup: owner can create a task in its own facility", created.status === 201 || created.status === 200, `status ${created.status}`);
const TA = created.json?.id ?? created.json?.entityId ?? psql(`select id from ai_tasks where tenant_id='${tenantA}' order by created_at_utc desc limit 1`);
const TB = psql(`insert into ai_tasks select (jsonb_populate_record(null::ai_tasks, to_jsonb(t) || jsonb_build_object('id', gen_random_uuid(), 'tenant_id', '${TENANT_B}'::uuid, 'facility_id', '${FACILITY_B}'::uuid))).* from ai_tasks t where t.id='${TA}' returning id`).split("\n")[0];
check("setup: a task owned by a foreign tenant exists", Boolean(TB), TB);

const countTasks = () => Number(psql(`select count(*) from ai_tasks where tenant_id='${tenantA}'`));
const gatewayBody = (caseId) => ({
  tenantId: tenantA, facilityId: FAC, workerId: null, digitalWorkCaseId: caseId, requestClassification: "factory-copilot-synthesis",
  taskType: "matrix", context: { contextSnapshotId: null, canonicalStateSummary: "x", digitalWorkCaseId: caseId, constraintSummary: null, simulationSummary: null, dataQualityState: "TRUSTED" },
  actionRiskLevel: 1, requiresStructuredOutput: true,
});

const surface = [
  ["POST", "/api/factory-copilot/query", { queryText: "Which machine had the most downtime?", context: { entryPoint: "FABRIKA" } }],
  ["POST", "/api/factory-copilot/suggested-questions", { context: { entryPoint: "FABRIKA" } }],
  ["POST", "/api/model-gateway/invoke", gatewayBody(null)],
  ["GET", "/api/model-gateway/audit?take=5"],
  ["GET", "/api/intelligence-runtime/control-center"],
  ["GET", "/api/intelligence-runtime/openai/validate"],
  ["GET", "/api/ai-tasks"],
  ["POST", `/api/ai-tasks/${TA}/ai/analyze`, {}],
  ["GET", `/api/ai-tasks/${TA}/ai`],
  ["POST", `/api/export/cases/${randomUUID()}/ai/analyze`, {}],
  ["GET", "/api/ai/usage/current"],
];
const call = (client, [m, p, b], facility = true) => client.request(m, facility ? q(p) : p, b);

console.log(`\n== PHASE ${PHASE} ==`);

// 1. authentication
for (const e of surface) {
  const r = await call(anon, e);
  check(`unauthenticated ${e[0]} ${e[1].split("?")[0].replace(TA, "{task}")} is refused`, r.status === 401, `status ${r.status}`);
}

// 2. facility scope
for (const e of surface) {
  const missing = await call(owner, e, false);
  check(`no facility ${e[1].split("?")[0].replace(TA, "{task}")} is refused (no 2xx/5xx)`, missing.status >= 400 && missing.status < 500, `status ${missing.status}`);
}
for (const [label, fac] of [["a foreign tenant's facility", FACILITY_B], ["an unknown facility", randomUUID()]]) {
  for (const e of surface.slice(0, 8)) {
    const sep = e[1].includes("?") ? "&" : "?";
    const r = await owner.request(e[0], `${e[1]}${sep}facilityId=${fac}`, e[2]);
    check(`${label}: ${e[0]} ${e[1].split("?")[0].replace(TA, "{task}")} fails closed`, r.status === 403, `status ${r.status}`);
  }
}

// 3. cross-tenant ids
const foreignTask = await owner.request("GET", q(`/api/ai-tasks/${TB}`));
check("foreign task id under own facility is not found", foreignTask.status === 404, `status ${foreignTask.status}`);
const foreignTaskForeignFacility = await owner.request("GET", `/api/ai-tasks/${TB}?facilityId=${FACILITY_B}`);
check("foreign task id under the foreign facility is forbidden", foreignTaskForeignFacility.status === 403, `status ${foreignTaskForeignFacility.status}`);
const analysesBefore = Number(psql(`select count(*) from ai_task_ai_analyses where task_id='${TB}'`));
const foreignAnalyze = await owner.request("POST", q(`/api/ai-tasks/${TB}/ai/analyze`), {});
check("analysing a foreign task never succeeds", foreignAnalyze.status >= 400 && foreignAnalyze.status < 500, `status ${foreignAnalyze.status}`);
check("no analysis row was written for the foreign task", Number(psql(`select count(*) from ai_task_ai_analyses where task_id='${TB}'`)) === analysesBefore);
const foreignRec = await owner.request("POST", q(`/api/ai-tasks/${TB}/ai/recommendations/${randomUUID()}/accept`), { expectedVersion: 1 });
check("accepting a foreign task's recommendation is not found", foreignRec.status === 404, `status ${foreignRec.status}`);

// gateway: a foreign or unknown work case reference fails closed and writes no audit row
const strayCase = randomUUID();
const gw = await owner.request("POST", q("/api/model-gateway/invoke"), gatewayBody(strayCase));
check("model gateway rejects a work-case reference that is not in scope", gw.status >= 400 && gw.status < 500, `status ${gw.status} ${gw.json?.code ?? ""}`);
check("model gateway wrote no audit row for the stray reference", Number(psql(`select count(*) from model_gateway_audit_records where digital_work_case_id='${strayCase}'`)) === 0);
const clientTenant = await owner.request("POST", q("/api/model-gateway/invoke"), { ...gatewayBody(null), tenantId: TENANT_B, facilityId: FACILITY_B });
check("a client-supplied tenant/facility in the body is ignored", clientTenant.status !== 500, `status ${clientTenant.status}`);
check("model gateway wrote nothing under the foreign tenant", Number(psql(`select count(*) from model_gateway_audit_records where tenant_id='${TENANT_B}'`)) === 0);

// 4. module entitlement (definition without a grant = not entitled)
const defs = { "module.ai-task-center": randomUUID(), "module.production": randomUUID(), "module.export": randomUUID() };
const defEntries = Object.entries(defs);
const tasksBeforeEntitlement = countTasks();
try {
  for (const [code, id] of defEntries) {
    psql(`insert into saas_commercial_entitlement_definitions(id,code,code_normalized,display_name_tr,value_kind,created_at_utc) values('${id}','${code}','${code.toLowerCase()}','P3 test','ModuleAccess',now()) on conflict do nothing`);
  }
  const normalized = psql(`select code_normalized from saas_commercial_entitlement_definitions where id='${defs["module.ai-task-center"]}'`);
  check("setup: entitlement definition recorded", normalized.length > 0, normalized);
  for (const [m, p, b] of [
    ["GET", "/api/ai-tasks"], ["POST", "/api/ai-tasks", taskBody("P3 must not exist")], [`POST`, `/api/ai-tasks/${TA}/ai/analyze`, {}],
    ["POST", "/api/factory-copilot/query", surface[0][2]], ["POST", "/api/model-gateway/invoke", gatewayBody(null)], ["GET", "/api/intelligence-runtime/control-center"],
    ["POST", `/api/export/cases/${randomUUID()}/ai/analyze`, {}],
  ]) {
    const r = await owner.request(m, q(p), b);
    check(`module not entitled: ${m} ${p.replace(TA, "{task}").split("?")[0]} is refused by the API`, r.status === 403 && /failureCode|module|entitle/i.test(r.text), `status ${r.status}`);
  }
  check("no task was created while the module was not entitled", countTasks() === tasksBeforeEntitlement);
} finally {
  for (const [, id] of defEntries) psql(`delete from saas_commercial_entitlement_definitions where id='${id}'`);
}
const restored = await owner.request("GET", q("/api/ai-tasks"));
check("after the entitlement definitions are removed the module is available again", restored.status === 200, `status ${restored.status}`);

// 5. permission denied
const overridePermission = async (key, fn) => {
  const defId = psql(`select id from permission_definitions where key='${key}'`);
  const ovId = randomUUID();
  psql(`insert into tenant_membership_permission_overrides(id,tenant_id,tenant_membership_id,permission_definition_id,effect,status,created_at_utc,updated_at_utc,version) values('${ovId}','${tenantA}','${MEMBERSHIP}','${defId}','deny','Active',now(),now(),1)`);
  psql(`update tenant_memberships set authorization_version = authorization_version + 1 where id='${MEMBERSHIP}'`);
  try {
    return await fn();
  } finally {
    psql(`delete from tenant_membership_permission_overrides where id='${ovId}'`);
    psql(`update tenant_memberships set authorization_version = authorization_version + 1 where id='${MEMBERSHIP}'`);
  }
};
const tasksBeforePermission = countTasks();
await overridePermission("ai-task.create", async () => {
  const r = await owner.request("POST", q("/api/ai-tasks"), taskBody("P3 must not exist either"));
  check("permission denied: ai-task.create refuses the mutation", r.status === 403, `status ${r.status}`);
});
check("no task was created while the permission was denied", countTasks() === tasksBeforePermission);
await overridePermission("factory-copilot.query", async () => {
  const r = await owner.request("POST", q("/api/factory-copilot/query"), surface[0][2]);
  check("permission denied: factory-copilot.query is refused", r.status === 403, `status ${r.status}`);
});
await overridePermission("ai-task.ai.analyze", async () => {
  const r = await owner.request("POST", q(`/api/ai-tasks/${TA}/ai/analyze`), {});
  check("permission denied: ai-task.ai.analyze is refused", r.status === 403, `status ${r.status}`);
});
await overridePermission("model-gateway.route", async () => {
  const r = await owner.request("POST", q("/api/model-gateway/invoke"), gatewayBody(null));
  check("permission denied: model-gateway.route is refused", r.status === 403, `status ${r.status}`);
});

// 6. phase specific
const analyze = await owner.request("POST", q(`/api/ai-tasks/${TA}/ai/analyze`), { analysisType: "triage", idempotencyKey: `p3-${randomUUID()}` });
if (PHASE === "1") {
  check("rules-based assistant is unavailable by default (no analysis produced)", analyze.status >= 400 || analyze.json?.analysis == null, `status ${analyze.status} ${analyze.json?.title ?? analyze.json?.failureCode ?? ""}`);
  check("no analysis row was written while the assistant is unavailable", Number(psql(`select count(*) from ai_task_ai_analyses where task_id='${TA}'`)) === 0);
  const ws = await owner.request("GET", q(`/api/ai-tasks/${TA}/ai`));
  check("the workspace reports the provider as unavailable", ws.status === 200 && ws.json?.providerAvailable === false, `status ${ws.status} available=${ws.json?.providerAvailable}`);
} else {
  check("rules-based assistant answers when explicitly enabled", analyze.status === 200 && analyze.json?.analysis != null, `status ${analyze.status}`);
  const provider = analyze.json?.analysis?.providerName;
  check("the answer is labelled by its real provider id (rules based, not a model)", provider === "deterministic-development", String(provider));
  const recs = analyze.json?.analysis?.recommendations ?? [];
  console.log(`INFO  recommendation actions: ${recs.map((r) => r.actionType).join(", ") || "none"}`);
}

// 6b. DB-backed accept-recommendation security (phase 2 only: the rules assistant must be enabled to produce one)
if (PHASE === "2") {
  const QUALITY_CHANNEL = "bb000001-03d2-4000-8000-0000000003d2";
  const FOREIGN_CHANNEL = "b0000000-0000-4000-8000-0000000000c3";
  psql(`insert into teams_channels(id,tenant_id,facility_id,name,channel_type,created_at_utc,created_by,updated_at_utc,updated_by) values('${QUALITY_CHANNEL}','${tenantA}','${FAC}','P3 Kalite','department',now(),'p3',now(),'p3') on conflict do nothing`);
  psql(`insert into teams_channels(id,tenant_id,facility_id,name,channel_type,created_at_utc,created_by,updated_at_utc,updated_by) values('${FOREIGN_CHANNEL}','${TENANT_B}','${FACILITY_B}','P3 Foreign','department',now(),'p3',now(),'p3') on conflict do nothing`);

  const mk = async (title) => {
    const t = await owner.request("POST", q("/api/ai-tasks"), taskBody(title));
    const id = t.json?.id ?? t.json?.entityId;
    const a = await owner.request("POST", q(`/api/ai-tasks/${id}/ai/analyze`), { analysisType: "triage", idempotencyKey: `p3-${randomUUID()}` });
    const rec = (a.json?.analysis?.recommendations ?? []).find((r) => r.actionType === "assign-team");
    return { id, rec };
  };
  const taskState = (id) => psql(`select version||'|'||coalesce(assigned_team_channel_id::text,'-')||'|'||priority||'|'||escalation_level from ai_tasks where id='${id}'`);
  const recStatus = (id) => psql(`select status from ai_task_ai_recommendations where id='${id}'`);
  const auditCount = (id, type) => Number(psql(`select count(*) from ai_task_audit_events where task_id='${id}' and event_type='${type}'`));
  const accept = (task, recId, body, facility = FAC) =>
    owner.request("POST", `/api/ai-tasks/${task.id}/ai/recommendations/${recId}/accept?facilityId=${facility}`, body);

  const dev = await mk("P3 accept task (quality)");
  check("the recommended team channel is a real channel of this tenant and facility", dev.rec?.payloadJson?.includes(QUALITY_CHANNEL) === true, dev.rec?.payloadJson);
  const version0 = Number(taskState(dev.id).split("|")[0]);
  const auditBefore = auditCount(dev.id, "ai-recommendation-accepted");

  // privilege: feedback permission alone must not assign
  await overridePermission("ai-task.assign", async () => {
    const r = await accept(dev, dev.rec.id, { expectedVersion: version0, reason: "p3" });
    check("accept without ai-task.assign is forbidden", r.status === 403, `status ${r.status}`);
  });
  check("denied accept: task not assigned", taskState(dev.id) === `${version0}|-|high|0` || taskState(dev.id).split("|")[1] === "-", taskState(dev.id));
  check("denied accept: recommendation still pending", recStatus(dev.rec.id) === "pending", recStatus(dev.rec.id));
  check("denied accept: no accept audit event", auditCount(dev.id, "ai-recommendation-accepted") === auditBefore);

  // an edited payload may not retarget the action to another team, including another tenant's
  for (const [label, target] of [["a different team of this facility", randomUUID()], ["another tenant's team channel", FOREIGN_CHANNEL]]) {
    const r = await accept(dev, dev.rec.id, { expectedVersion: version0, reason: "p3", editedPayloadJson: JSON.stringify({ teamChannelId: target }) });
    check(`edited payload retargeting to ${label} is refused`, r.status === 400, `status ${r.status}`);
  }
  check("refused retargets changed nothing", taskState(dev.id).split("|")[1] === "-" && recStatus(dev.rec.id) === "pending");

  // authorised accept: exactly one mutation and one audit event
  const ok = await accept(dev, dev.rec.id, { expectedVersion: version0, reason: "p3 approve" });
  check("authorised accept succeeds", ok.status === 200, `status ${ok.status} ${ok.text.slice(0, 160)}`);
  const afterOk = taskState(dev.id).split("|");
  check("exactly one mutation: the recommended team is assigned", afterOk[1] === QUALITY_CHANNEL, afterOk.join("|"));
  check("exactly one version increment", Number(afterOk[0]) === version0 + 1, `v${version0} -> v${afterOk[0]}`);
  check("recommendation recorded as accepted", recStatus(dev.rec.id) === "accepted", recStatus(dev.rec.id));
  check("one accept audit event identifies the actor", auditCount(dev.id, "ai-recommendation-accepted") === auditBefore + 1);
  check("the audit event carries the acting membership, not the request body", psql(`select count(*) from ai_task_audit_events where task_id='${dev.id}' and event_type='ai-recommendation-accepted' and actor_id='${MEMBERSHIP}'`) === "1");

  // replay: the same accept again must not apply anything a second time
  const replay = await accept(dev, dev.rec.id, { expectedVersion: Number(afterOk[0]), reason: "p3 replay" });
  check("replaying the accept is refused", replay.status >= 400 && replay.status < 500, `status ${replay.status}`);
  check("replay did not mutate again", taskState(dev.id) === afterOk.join("|"));
  check("replay wrote no second audit event", auditCount(dev.id, "ai-recommendation-accepted") === auditBefore + 1);

  // a stale task version fails closed (no mutation)
  const stale = await mk("P3 accept task (stale)");
  const staleRes = await accept(stale, stale.rec.id, { expectedVersion: 9999, reason: "p3" });
  check("a stale expected version is a conflict, not a mutation", staleRes.status === 409, `status ${staleRes.status}`);
  check("stale accept changed nothing", taskState(stale.id).split("|")[1] === "-" && recStatus(stale.rec.id) === "pending");

  // cross-tenant: a foreign tenant's task and recommendation
  const foreignTask = psql(`insert into ai_tasks select (jsonb_populate_record(null::ai_tasks, to_jsonb(t) || jsonb_build_object('id', gen_random_uuid(), 'tenant_id', '${TENANT_B}'::uuid, 'facility_id', '${FACILITY_B}'::uuid))).* from ai_tasks t where t.id='${stale.id}' returning id`).split("\n")[0];
  const foreignRecId = randomUUID();
  psql(`set session_replication_role = replica; insert into ai_task_ai_recommendations select (jsonb_populate_record(null::ai_task_ai_recommendations, to_jsonb(r) || jsonb_build_object('id','${foreignRecId}'::uuid,'tenant_id','${TENANT_B}'::uuid,'facility_id','${FACILITY_B}'::uuid,'task_id','${foreignTask}'::uuid,'status','pending'))).* from ai_task_ai_recommendations r where r.id='${stale.rec.id}'`);
  const foreignVersion = psql(`select version from ai_tasks where id='${foreignTask}'`);
  const crossA = await accept({ id: foreignTask }, foreignRecId, { expectedVersion: Number(foreignVersion) });
  check("accepting another tenant's recommendation under own facility is not found", crossA.status === 404, `status ${crossA.status}`);
  const crossB = await accept({ id: foreignTask }, foreignRecId, { expectedVersion: Number(foreignVersion) }, FACILITY_B);
  check("accepting another tenant's recommendation under its facility is forbidden", crossB.status === 403, `status ${crossB.status}`);
  check("foreign tenant's task and recommendation untouched", psql(`select version from ai_tasks where id='${foreignTask}'`) === foreignVersion && recStatus(foreignRecId) === "pending");
  // own recommendation id used with the foreign task id
  const mixed = await accept({ id: foreignTask }, stale.rec.id, { expectedVersion: Number(foreignVersion) });
  check("own recommendation id against a foreign task id is not found", mixed.status === 404, `status ${mixed.status}`);

  // another mutating action: escalation needs ai-task.escalate
  const esc = await mk("P3 accept task (escalate)");
  const escRec = randomUUID();
  psql(`insert into ai_task_ai_recommendations select (jsonb_populate_record(null::ai_task_ai_recommendations, to_jsonb(r) || jsonb_build_object('id','${escRec}'::uuid,'action_type','escalate','payload_json','{}','status','pending'))).* from ai_task_ai_recommendations r where r.id='${esc.rec.id}'`);
  const escVersion = Number(taskState(esc.id).split("|")[0]);
  await overridePermission("ai-task.escalate", async () => {
    const r = await accept(esc, escRec, { expectedVersion: escVersion });
    check("accept of an escalation without ai-task.escalate is forbidden", r.status === 403, `status ${r.status}`);
  });
  check("denied escalation changed nothing", taskState(esc.id).split("|")[0] === String(escVersion) && recStatus(escRec) === "pending");
}

// 7. session lifecycle (last: it ends the session)
const garbage = new Client();
garbage.cookies.set("NevaxisZentra.session", "garbage-session-token");
check("a garbage session cookie is refused", (await garbage.request("GET", q("/api/ai-tasks"))).status === 401);
psql(`update saas_identity_sessions set idle_expires_at_utc = now() - interval '1 minute', absolute_expires_at_utc = now() - interval '1 minute' where account_id in (select id from saas_identity_accounts) and revoked_at_utc is null`);
const expired = await owner.request("GET", q("/api/ai-tasks"));
check("an expired session is refused (401) rather than served or crashing", expired.status === 401, `status ${expired.status}`);
const afterExpiry = await owner.request("POST", q("/api/factory-copilot/query"), surface[0][2]);
check("an expired session cannot reach the Copilot", afterExpiry.status === 401, `status ${afterExpiry.status}`);

console.log(`\nSUMMARY phase ${PHASE}: ${passes} passed, ${failures} failed`);
process.exit(failures === 0 ? 0 : 1);
