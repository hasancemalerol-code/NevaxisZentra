#!/usr/bin/env node
// RC3 Package 3 — Maintenance AI security matrix against a LOCAL stack and a LOCAL ephemeral database that was seeded by
// the Development Pilot bootstrap (the only supported creator of asset-to-machine mappings). Opt-in, never part of CI.
//
// Credentials come only from the ignored secret file named by P3_ACCOUNT_FILE (organizationCode, ownerEmail, password,
// totpSecret). Nothing secret is printed. The only fixtures written directly are a second tenant and a copy of one draft
// and one mapping under it, used solely as the TARGET of cross-tenant denials.
import { spawnSync } from "node:child_process";
import { createHmac, randomUUID } from "node:crypto";
import { readFileSync } from "node:fs";

const API = process.env.P3_API ?? "http://127.0.0.1:5000";
const PG_CONTAINER = process.env.P3_PG_CONTAINER ?? "nevaxiszentra-release-audit-postgres";
const PG_DB = process.env.P3_PG_DB ?? "zentra_p3_pilot";
const PG_USER = process.env.P3_PG_USER ?? "NevaxisZentra";
const FAC = process.env.P3_FACILITY ?? "bb000001-0005-4000-8000-000000000005";
const ASSET = process.env.P3_ASSET ?? "ASSET-CNC-001";
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
const psql = (sql) => {
  const r = spawnSync("docker", ["exec", PG_CONTAINER, "psql", "-U", PG_USER, "-d", PG_DB, "-t", "-A", "-v", "ON_ERROR_STOP=1", "-c", sql], { encoding: "utf8" });
  if (r.status !== 0) throw new Error(`psql failed: ${(r.stderr || r.stdout).slice(0, 300)}`);
  return r.stdout.trim();
};

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

const tenantA = psql(`select id from tenants where tenant_key='${account.organizationCode}'`);
// The signed-in owner's membership is resolved after sign-in (a pilot tenant has several memberships; the override must
// target the one in use).
let MEMBERSHIP = process.env.P3_MEMBERSHIP ?? "";
const TENANT_B = "b0000000-0000-4000-8000-0000000000b1";
const FACILITY_B = "b0000000-0000-4000-8000-0000000000b2";
const FOREIGN_ASSET = "ASSET-FOREIGN-001";
psql(`insert into tenants(id,tenant_key,display_name,status,created_at_utc) values('${TENANT_B}','p3-foreign-tenant','P3 Foreign Tenant','Active',now()) on conflict do nothing`);
psql(`insert into facilities(id,tenant_id,code,name,status,time_zone_id,created_at_utc,updated_at_utc,version) values('${FACILITY_B}','${TENANT_B}','FOREIGN','Foreign Facility','Active','UTC',now(),now(),1) on conflict do nothing`);
psql(`set session_replication_role = replica; insert into maintenance_asset_machine_mappings select (jsonb_populate_record(null::maintenance_asset_machine_mappings, to_jsonb(m) || jsonb_build_object('id', gen_random_uuid(), 'tenant_id', '${TENANT_B}'::uuid, 'facility_id', '${FACILITY_B}'::uuid, 'asset_reference', '${FOREIGN_ASSET}', 'asset_id', 'asset-foreign-001'))).* from maintenance_asset_machine_mappings m where m.asset_reference='${ASSET}' and m.tenant_id='${tenantA}' on conflict do nothing`);

const owner = await login();
const anon = new Client();
const q = (p, fac = FAC) => `${p}?facilityId=${fac}`;
const requestCount = () => Number(psql(`select count(*) from maintenance_requests where tenant_id='${tenantA}'`));
const draftCount = () => Number(psql(`select count(*) from maintenance_ai_action_drafts where tenant_id='${tenantA}'`));
const auditCount = () => Number(psql(`select count(*) from maintenance_ai_draft_audit_events where tenant_id='${tenantA}'`));

console.log("\n== MAINTENANCE AI MATRIX ==");
let executed = psql(`select id from maintenance_ai_action_drafts where tenant_id='${tenantA}' and status='executed' limit 1`);
if (!executed) {
  // No UI happy path has been run on this database yet: run the explicit generate -> approve path once.
  await owner.request("POST", `/api/maintenance/condition/evaluate?facilityId=${FAC}`, {});
  await owner.request("POST", `/api/maintenance/condition/risk/evaluate?facilityId=${FAC}`, {});
  const g = await owner.request("POST", `/api/maintenance/assets/${ASSET}/ai-drafts/generate?facilityId=${FAC}`, {});
  const reqBefore = Number(psql(`select count(*) from maintenance_requests where tenant_id='${tenantA}'`));
  const v = Number(psql(`select version from maintenance_ai_action_drafts where id='${g.json?.draftId}'`));
  const ap = await owner.request("POST", `/api/maintenance/ai-drafts/${g.json?.draftId}/approve?facilityId=${FAC}`, { expectedVersion: v });
  check("setup: explicit approval succeeds and creates exactly one maintenance request", ap.status === 200 && Number(psql(`select count(*) from maintenance_requests where tenant_id='${tenantA}'`)) === reqBefore + 1, `status ${ap.status}`);
  executed = psql(`select id from maintenance_ai_action_drafts where tenant_id='${tenantA}' and status='executed' limit 1`);
}
const executedVersion = executed ? Number(psql(`select version from maintenance_ai_action_drafts where id='${executed}'`)) : 0;
check("setup: an executed (approved) draft exists from the UI happy path", Boolean(executed), executed);

// authentication
for (const [m, p, b] of [
  ["POST", `/api/maintenance/assets/${ASSET}/ai-drafts/generate`, {}],
  ["GET", `/api/maintenance/assets/${ASSET}/ai-drafts/current`],
  ["POST", `/api/maintenance/ai-drafts/${executed}/approve`, { expectedVersion: executedVersion }],
  ["POST", `/api/maintenance/ai-drafts/${executed}/reject`, { expectedVersion: executedVersion, rejectionReason: "x" }],
]) {
  const r = await anon.request(m, q(p), b);
  check(`unauthenticated ${m} ${p.split("/").slice(-1)[0]} is refused`, r.status === 401, `status ${r.status}`);
}

// facility scope
const noFacility = await owner.request("POST", `/api/maintenance/assets/${ASSET}/ai-drafts/generate`, {});
check("generate without a facility is refused", noFacility.status >= 400 && noFacility.status < 500, `status ${noFacility.status}`);
for (const [label, fac] of [["a foreign tenant's facility", FACILITY_B], ["an unknown facility", randomUUID()]]) {
  const g = await owner.request("POST", q(`/api/maintenance/assets/${ASSET}/ai-drafts/generate`, fac), {});
  check(`${label}: generate fails closed`, g.status === 403, `status ${g.status}`);
}

// cross-tenant asset / draft references
const draftsBefore = draftCount();
const foreignDraftsBefore = Number(psql(`select count(*) from maintenance_ai_action_drafts where tenant_id='${TENANT_B}'`));
const foreignAssetGen = await owner.request("POST", q(`/api/maintenance/assets/${FOREIGN_ASSET}/ai-drafts/generate`), {});
check("a foreign tenant's asset reference is not found under own facility", foreignAssetGen.status === 404, `status ${foreignAssetGen.status}`);
const foreignAssetGenFac = await owner.request("POST", q(`/api/maintenance/assets/${FOREIGN_ASSET}/ai-drafts/generate`, FACILITY_B), {});
check("a foreign tenant's asset under its own facility is forbidden", foreignAssetGenFac.status === 403, `status ${foreignAssetGenFac.status}`);
check("no draft was created for a foreign asset", draftCount() === draftsBefore && Number(psql(`select count(*) from maintenance_ai_action_drafts where tenant_id='${TENANT_B}'`)) === foreignDraftsBefore);

const foreignDraft = randomUUID();
psql(`set session_replication_role = replica; insert into maintenance_ai_action_drafts select (jsonb_populate_record(null::maintenance_ai_action_drafts, to_jsonb(d) || jsonb_build_object('id','${foreignDraft}'::uuid,'tenant_id','${TENANT_B}'::uuid,'facility_id','${FACILITY_B}'::uuid,'asset_reference','${FOREIGN_ASSET}','status','draft'))).* from maintenance_ai_action_drafts d where d.id='${executed}'`);
const foreignVersion = Number(psql(`select version from maintenance_ai_action_drafts where id='${foreignDraft}'`));
const requestsBeforeForeign = requestCount();
for (const [label, fac, expected] of [["under own facility", FAC, 404], ["under the foreign facility", FACILITY_B, 403]]) {
  const a = await owner.request("POST", q(`/api/maintenance/ai-drafts/${foreignDraft}/approve`, fac), { expectedVersion: foreignVersion });
  check(`approving another tenant's draft ${label} fails closed`, a.status === expected, `status ${a.status}`);
  const r = await owner.request("POST", q(`/api/maintenance/ai-drafts/${foreignDraft}/reject`, fac), { expectedVersion: foreignVersion, rejectionReason: "x" });
  check(`rejecting another tenant's draft ${label} fails closed`, r.status === expected, `status ${r.status}`);
}
check("the foreign draft is untouched and no request was created", psql(`select status from maintenance_ai_action_drafts where id='${foreignDraft}'`) === "draft" && requestCount() === requestsBeforeForeign);
const unknownDraft = await owner.request("POST", q(`/api/maintenance/ai-drafts/${randomUUID()}/approve`), { expectedVersion: 1 });
check("approving an unknown draft id is not found", unknownDraft.status === 404, `status ${unknownDraft.status}`);

// replay: the same approval again must not create a second request
const requestsBeforeReplay = requestCount();
const auditBeforeReplay = auditCount();
const replay = await owner.request("POST", q(`/api/maintenance/ai-drafts/${executed}/approve`), { expectedVersion: executedVersion });
const originalRequestId = psql(`select created_maintenance_request_id from maintenance_ai_action_drafts where id='${executed}'`);
check(
  "replaying an approval is refused or an idempotent replay of the same request",
  (replay.status >= 400 && replay.status < 500) || (replay.status === 200 && replay.json?.maintenanceRequestId === originalRequestId),
  `status ${replay.status}`,
);
check("replay created no second maintenance request", requestCount() === requestsBeforeReplay);
const replayReject = await owner.request("POST", q(`/api/maintenance/ai-drafts/${executed}/reject`, FAC), { expectedVersion: executedVersion, rejectionReason: "late reject" });
check("an executed draft cannot be rejected afterwards", replayReject.status >= 400 && replayReject.status < 500, `status ${replayReject.status}`);
check("the executed draft stays executed", psql(`select status from maintenance_ai_action_drafts where id='${executed}'`) === "executed");
void auditBeforeReplay;

if (process.env.P3_CHECK_PERMISSIONS === "1") {
  MEMBERSHIP ||= psql(`select tenant_membership_id from saas_identity_sessions where tenant_id='${tenantA}' and revoked_at_utc is null and tenant_membership_id is not null order by created_at_utc desc limit 1`);
// permission denied: maintenance.manage
const defId = psql(`select id from permission_definitions where key='maintenance.manage'`);
const ovId = randomUUID();
psql(`insert into tenant_membership_permission_overrides(id,tenant_id,tenant_membership_id,permission_definition_id,effect,status,created_at_utc,updated_at_utc,version) values('${ovId}','${tenantA}','${MEMBERSHIP}','${defId}','deny','Active',now(),now(),1)`);
psql(`update tenant_memberships set authorization_version = authorization_version + 1 where id='${MEMBERSHIP}'`);
try {
  const draftsBeforePerm = draftCount();
  const reqBeforePerm = requestCount();
  const g = await owner.request("POST", q(`/api/maintenance/assets/${ASSET}/ai-drafts/generate`), {});
  check("permission denied: generate is forbidden", g.status === 403, `status ${g.status}`);
  const a = await owner.request("POST", q(`/api/maintenance/ai-drafts/${executed}/approve`), { expectedVersion: executedVersion });
  check("permission denied: approve is forbidden", a.status === 403, `status ${a.status}`);
  check("permission denied: nothing was created", draftCount() === draftsBeforePerm && requestCount() === reqBeforePerm);
} finally {
  psql(`delete from tenant_membership_permission_overrides where id='${ovId}'`);
  psql(`update tenant_memberships set authorization_version = authorization_version + 1 where id='${MEMBERSHIP}'`);
}

}

// audit identity
const lastAudit = psql(`select action||'|'||(actor_subject_id is not null)||'|'||(tenant_id='${tenantA}') from maintenance_ai_draft_audit_events where draft_id='${executed}' and action like '%approved%' limit 1`);
check("the approval audit row names the action, an actor subject and this tenant", lastAudit === "maintenance.ai.draft.approved|true|true", lastAudit);
check("the request-creation audit row exists for the executed draft", Number(psql(`select count(*) from maintenance_ai_draft_audit_events where draft_id='${executed}' and action='maintenance.ai.draft.request-created'`)) === 1);
check("the audit trail stores no raw prompt or provider body", Number(psql(`select count(*) from maintenance_ai_draft_audit_events where tenant_id='${tenantA}' and (details_json ilike '%password%' or details_json ilike '%secret%' or details_json ilike '%token%')`)) === 0);

// ---- page load is side-effect-free; evaluation is an explicit action --------------------------------------------------------
if (process.env.P3_WEB) {
  const WEB = process.env.P3_WEB;
  const PRESS = "ASSET-PRESS-001";
  const snapshot = () => ({
    assessments: Number(psql(`select count(*) from maintenance_predictive_risk_assessments where tenant_id='${tenantA}'`)),
    currentOf: Object.fromEntries([ASSET, PRESS].map((asset) => [asset, psql(`select id from maintenance_predictive_risk_assessments where tenant_id='${tenantA}' and asset_reference='${asset}' and is_current`)])),
    byAsset: Object.fromEntries([ASSET, PRESS].map((asset) => [asset, Number(psql(`select count(*) from maintenance_predictive_risk_assessments where tenant_id='${tenantA}' and asset_reference='${asset}'`))])),
    profiles: psql(`select coalesce(sum(version),0)||'|'||coalesce(max(last_evaluated_at_utc)::text,'-') from maintenance_condition_profiles where tenant_id='${tenantA}'`),
    anomalies: Number(psql(`select count(*) from maintenance_condition_anomalies where tenant_id='${tenantA}'`)),
    drafts: draftCount(),
    requests: requestCount(),
    draftAudit: auditCount(),
    audit: Number(psql(`select count(*) from audit_records where tenant_id='${tenantA}'`)),
  });
  const same = (a, b) => ["assessments", "profiles", "anomalies", "drafts", "requests", "draftAudit", "audit"].every((k) => a[k] === b[k]);
  const cookieHeader = () => [...owner.cookies].map(([k, v]) => `${k}=${v}`).join("; ");
  const loadPage = async (pathname) => {
    const res = await fetch(`${WEB}${pathname}`, { headers: { cookie: cookieHeader() }, redirect: "manual" });
    return { status: res.status, text: await res.text() };
  };
  const apiEvaluate = async () => {
    const a = await owner.request("POST", q("/api/maintenance/condition/evaluate"), {});
    const b = await owner.request("POST", q("/api/maintenance/condition/risk/evaluate"), {});
    return a.status === 200 && b.status === 200;
  };

  console.log("\n== PAGE LOAD PURITY ==");
  check("setup: an explicit evaluation succeeds", await apiEvaluate());
  const base = snapshot();

  let ok = true;
  const pages = [`/bakim/durum-zekasi?asset=${ASSET}`, `/bakim/durum-zekasi?asset=${PRESS}`, "/bakim/kestirimci-riskler", "/bakim"];
  for (let round = 0; round < 3; round++) {
    for (const p of pages) {
      const r = await loadPage(p);
      ok = ok && r.status === 200;
    }
  }
  check("repeated page loads render successfully", ok);
  const afterLoads = snapshot();
  check("repeated page loads created zero assessments", afterLoads.assessments === base.assessments, `${base.assessments} -> ${afterLoads.assessments}`);
  check("repeated page loads rewrote no condition profile (version and evaluation time unchanged)", afterLoads.profiles === base.profiles, `${base.profiles} -> ${afterLoads.profiles}`);
  check("repeated page loads created zero anomalies, drafts and maintenance requests", afterLoads.anomalies === base.anomalies && afterLoads.drafts === base.drafts && afterLoads.requests === base.requests);
  check("repeated page loads wrote zero audit rows", afterLoads.draftAudit === base.draftAudit && afterLoads.audit === base.audit);
  const apiReads = [
    ["GET", "/api/maintenance/condition/overview"],
    ["GET", `/api/maintenance/assets/${ASSET}/condition-intelligence`],
    ["GET", "/api/maintenance/condition/risk/overview"],
    ["GET", `/api/maintenance/assets/${ASSET}/predictive-risk`],
    ["GET", `/api/maintenance/assets/${ASSET}/ai-drafts/current`],
  ];
  for (let i = 0; i < 3; i++) for (const [m, p] of apiReads) await owner.request(m, q(p));
  check("repeated API reads are side-effect-free too", same(snapshot(), base));

  // the draft stays the same draft across page loads
  const gen = await owner.request("POST", q(`/api/maintenance/assets/${ASSET}/ai-drafts/generate`), {});
  check("an explicit generate produces a draft (or the existing identical draft)", gen.status === 200 && Boolean(gen.json?.draftId), `status ${gen.status}`);
  const draftId = gen.json?.draftId;
  const draftState = () => psql(`select id||'|'||status||'|'||version from maintenance_ai_action_drafts where id='${draftId}'`);
  const draftBefore = draftState();
  const afterGen = snapshot();
  for (let i = 0; i < 4; i++) for (const p of pages) await loadPage(p);
  check("the draft is the same draft, unchanged, across page loads", draftState() === draftBefore, draftBefore);
  check("page loads after the draft wrote nothing", same(snapshot(), afterGen));
  const regen = await owner.request("POST", q(`/api/maintenance/assets/${ASSET}/ai-drafts/generate`), {});
  check("a repeated generate returns the same draft instead of making another", regen.json?.draftId === draftId, String(regen.json?.draftId));

  // underlying data changes: the page must not silently recompute
  const machineId = psql(`select machine_id from maintenance_asset_machine_mappings where tenant_id='${tenantA}' and asset_reference='${ASSET}'`);
  const stamp = Date.now();
  for (let i = 0; i < 3; i++) {
    psql(`set session_replication_role = replica; insert into machine_alarm_observations(id,tenant_id,facility_id,machine_id,gateway_id,alarm_identity,external_alarm_code,alarm_text,severity,event_kind,source_timestamp_utc,received_at_utc,quality,idempotency_key,is_duplicate,is_out_of_order,is_unmatched_clear,created_at_utc) values(gen_random_uuid(),'${tenantA}','${FAC}','${machineId}',gen_random_uuid(),'p3-alarm-${stamp}-${i}','E${i}','p3 telemetry change','critical','machine.alarm.raised',now() - interval '${i + 1} minutes',now(),'good','p3-${stamp}-${i}',false,false,false,now())`);
  }
  const afterDataChange = snapshot();
  const pageAfterChange = await loadPage(`/bakim/durum-zekasi?asset=${ASSET}`);
  for (let i = 0; i < 3; i++) for (const p of pages) await loadPage(p);
  check("changed telemetry did not cause a write when pages were opened", same(snapshot(), afterDataChange));
  check("the page tells the user it does not recompute on open", pageAfterChange.text.includes("değerlendirmeyi çalıştırmaz"));
  check("the draft is still current after the data changed and pages were opened", draftState() === draftBefore);

  // the explicit action: exactly the intended single update
  const beforeEval = snapshot();
  check("the explicit evaluate action succeeds", await apiEvaluate());
  const afterEval = snapshot();
  check(
    "explicit evaluate replaced the current assessment of the asset whose data changed, exactly once",
    afterEval.currentOf[ASSET] !== beforeEval.currentOf[ASSET] && afterEval.byAsset[ASSET] - beforeEval.byAsset[ASSET] <= 1,
    `rows ${beforeEval.byAsset[ASSET]} -> ${afterEval.byAsset[ASSET]}`,
  );
  check(
    "explicit evaluate left the asset whose data did not change untouched (same current assessment, no new row)",
    afterEval.currentOf[PRESS] === beforeEval.currentOf[PRESS] && afterEval.byAsset[PRESS] === beforeEval.byAsset[PRESS],
  );
  check("explicit evaluate created no drafts and no maintenance requests", afterEval.drafts === beforeEval.drafts && afterEval.requests === beforeEval.requests);
  check("exactly one current assessment per asset after the evaluation", Number(psql(`select count(*) from maintenance_predictive_risk_assessments where tenant_id='${tenantA}' and asset_reference='${ASSET}' and is_current`)) === 1);
  check("evaluating again with unchanged data creates no further assessment", (await apiEvaluate()) && snapshot().assessments === afterEval.assessments);
  for (let i = 0; i < 2; i++) for (const p of pages) await loadPage(p);
  check("page loads after the evaluation are again write-free", snapshot().assessments === afterEval.assessments && snapshot().drafts === afterEval.drafts);
}

console.log(`\nSUMMARY maintenance AI: ${passes} passed, ${failures} failed`);
process.exit(failures === 0 ? 0 : 1);
