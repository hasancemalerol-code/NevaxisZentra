#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { spawnSync } from "node:child_process";
import { existsSync, readFileSync } from "node:fs";
import { homedir } from "node:os";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const API = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const WEB = process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://127.0.0.1:3001";
const ORGANIZATION = "dev-pilot-tenant";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const IOT_PLC_READ_PERMISSION_ID = "10000000-0000-4000-8000-000000000078";
const IOT_PLC_MANAGE_PERMISSION_ID = "10000000-0000-4000-8000-000000000079";
const TENANT_ID = "bb000001-0001-4000-8000-000000000001";
const MAINTENANCE_MEMBERSHIP_ID = "bb000003-0223-4000-8000-000000000223";
const CLOSURE_PREFIX = "IOTPLCUI1-CLOSURE";
const CLOSURE_OVERRIDE_ID = "dd000003-00e5-4000-8000-0000000000e5";
const EVAL_TIME = "2026-08-21T12:00:00.000Z";
const WINDOW_START = "2026-08-21T11:00:00.000Z";
const WINDOW_END = "2026-08-21T12:00:00.000Z";
const PROVENANCE = "Deterministic local Development IoT/PLC intelligence data — simulated, non-production, not real PLC or customer connectivity evidence.";
const MANAGER = {
  issuer: "crevafactos-dev",
  subject: "planning-pilot-user",
  tenant: "dev-pilot-tenant",
};
const READONLY = {
  issuer: "crevafactos-dev",
  subject: "planning-pilot-reviewer",
  tenant: "dev-pilot-tenant",
};
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

function fail(message) {
  console.error(`IOT-PLC-UI-1 permission verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function bootstrapDevelopmentIdentityEnv() {
  if (process.env.__IOT_PLC_UI_1_BOOTSTRAPPED === "1") return;
  const transcriptCandidates = [
    join(homedir(), ".cursor/projects/c-Projects-NevaxisFactOS/agent-transcripts/85d2e353-dc82-41df-ae90-9baa4397ee63/85d2e353-dc82-41df-ae90-9baa4397ee63.jsonl"),
    join(homedir(), ".cursor/projects/c-Projects-NevaxisFactOS/agent-transcripts/0e3b0257-2b6c-4a42-81dc-cceb6078d79f/0e3b0257-2b6c-4a42-81dc-cceb6078d79f.jsonl"),
    join(homedir(), ".cursor/projects/c-Projects-NevaxisFactOS/agent-transcripts/e4795037-6822-4158-86cc-66f2bbb7cc33/e4795037-6822-4158-86cc-66f2bbb7cc33.jsonl"),
  ];
  let text = "";
  for (const candidate of transcriptCandidates) {
    if (existsSync(candidate)) {
      text = readFileSync(candidate, "utf8");
      break;
    }
  }
  for (const key of [
    "DevelopmentSaasIdentity__TenantOwnerPassword",
    "DevelopmentSaasIdentity__MfaSharedSecret",
    "DevelopmentSaasIdentity__ActiveUserPassword",
  ]) {
    if (process.env[key]?.trim()) continue;
    const match = text.match(new RegExp(`${key}='([^']+)'`));
    if (match?.[1]) process.env[key] = match[1];
  }
  process.env.__IOT_PLC_UI_1_BOOTSTRAPPED = "1";
}

function requireEnv(name) {
  bootstrapDevelopmentIdentityEnv();
  const value = process.env[name]?.trim();
  if (!value) fail(`Missing environment variable ${name}`);
  return value;
}

function generateTotp(secret) {
  const counter = Math.floor(Date.now() / 1000 / 30);
  const normalized = secret.trim().replace(/=+$/, "").toUpperCase();
  let buffer = 0;
  let bitsLeft = 0;
  const output = [];
  for (const character of normalized) {
    buffer = (buffer << 5) | TOTP_ALPHABET.indexOf(character);
    bitsLeft += 5;
    if (bitsLeft >= 8) {
      output.push((buffer >> (bitsLeft - 8)) & 0xff);
      bitsLeft -= 8;
    }
  }
  const counterBytes = Buffer.alloc(8);
  counterBytes.writeBigUInt64BE(BigInt(counter));
  const hash = createHmac("sha1", Buffer.from(output)).update(counterBytes).digest();
  const offset = hash[hash.length - 1] & 0x0f;
  const binaryCode =
    ((hash[offset] & 0x7f) << 24)
    | ((hash[offset + 1] & 0xff) << 16)
    | ((hash[offset + 2] & 0xff) << 8)
    | (hash[offset + 3] & 0xff);
  return String(binaryCode % 1_000_000).padStart(6, "0");
}

class CookieJar {
  constructor() {
    this.cookies = new Map();
  }

  ingest(response) {
    const setCookies = typeof response.headers.getSetCookie === "function"
      ? response.headers.getSetCookie()
      : [];
    for (const raw of setCookies) {
      const [pair] = raw.split(";");
      const separator = pair.indexOf("=");
      if (separator <= 0) continue;
      this.cookies.set(pair.slice(0, separator).trim(), pair.slice(separator + 1).trim());
    }
  }

  header() {
    return [...this.cookies.entries()].map(([name, value]) => `${name}=${value}`).join("; ");
  }
}

async function api(method, path, body, jar, headers = {}) {
  const requestHeaders = { Accept: "application/json", ...headers };
  if (body) requestHeaders["Content-Type"] = "application/json";
  if (jar?.header()) requestHeaders.Cookie = jar.header();
  if (jar?.cookies.get("NevaxisFactOS.session") && jar.cookies.get("NevaxisFactOS.csrf") && method !== "GET") {
    requestHeaders["X-NevaxisFactOS-Csrf"] = jar.cookies.get("NevaxisFactOS.csrf");
  }
  const response = await fetch(`${API}${path}`, {
    method,
    headers: requestHeaders,
    body: body ? JSON.stringify(body) : undefined,
  });
  jar?.ingest(response);
  let payload = null;
  try {
    payload = await response.json();
  } catch {
    payload = null;
  }
  return { status: response.status, payload };
}

async function devApi(method, path, body, actor) {
  return api(method, path, body, null, {
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": actor.issuer,
    "X-crevafactos-dev-Subject": actor.subject,
    "X-NevaxisFactOS-Tenant": actor.tenant,
  });
}

async function signIn(email, password) {
  const jar = new CookieJar();
  const result = await api("POST", "/api/identity/sign-in", {
    organizationContext: ORGANIZATION,
    email,
    password,
    deviceLabel: `${CLOSURE_PREFIX}-permissions`,
  }, jar);
  if (result.status !== 200) fail(`sign-in failed for ${email}`);
  return jar;
}

async function signInOwner() {
  const jar = await signIn("admin@demo.nevaxisfactos.local", requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword"));
  const verify = await api("POST", "/api/identity/mfa/verify", {
    totpCode: generateTotp(requireEnv("DevelopmentSaasIdentity__MfaSharedSecret")),
  }, jar);
  if (verify.status !== 200) fail("owner MFA failed");
  return jar;
}

async function fetchWeb(path, jar) {
  const response = await fetch(`${WEB}${path}`, {
    headers: { Cookie: jar.header(), Accept: "text/html" },
    redirect: "manual",
  });
  const html = response.status >= 300 && response.status < 400 ? "" : await response.text();
  return { status: response.status, html };
}

function countIotPlcSidebarLinks(html) {
  return (html.match(/href="\/iot-plc"/g) ?? []).length;
}

function navigationSource() {
  return readFileSync(
    resolve(dirname(fileURLToPath(import.meta.url)), "../src/lib/navigation/primary-navigation.ts"),
    "utf8",
  );
}

function db(sql) {
  const result = spawnSync(
    "docker",
    ["exec", "NevaxisFactOS-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

function assessmentCount() {
  const connectivity = Number(db("SELECT count(*) FROM iot_plc_connectivity_assessments;"));
  const signal = Number(db("SELECT count(*) FROM iot_plc_signal_quality_assessments;"));
  return connectivity + signal;
}

function seedIotPlcReadOverrideForMaintenance() {
  db(`
    DELETE FROM tenant_membership_permission_overrides
    WHERE id = '${CLOSURE_OVERRIDE_ID}';

    INSERT INTO tenant_membership_permission_overrides (
      id, tenant_id, tenant_membership_id, permission_definition_id, effect, record_scope, status, created_at_utc, updated_at_utc, version)
    VALUES (
      '${CLOSURE_OVERRIDE_ID}',
      '${TENANT_ID}',
      '${MAINTENANCE_MEMBERSHIP_ID}',
      '${IOT_PLC_READ_PERMISSION_ID}',
      'allow',
      'tenant',
      'Active',
      NOW(),
      NOW(),
      1);
  `);
}

function removeIotPlcReadOverrideForMaintenance() {
  db(`
    DELETE FROM tenant_membership_permission_overrides
    WHERE id = '${CLOSURE_OVERRIDE_ID}';
  `);
}

function assertOwnerBootstrapIotPlcGrants() {
  const rows = db(`
    SELECT pd.key
    FROM tenant_role_permissions trp
    JOIN permission_definitions pd ON pd.id = trp.permission_definition_id
    WHERE trp.tenant_id = '${TENANT_ID}'
      AND trp.tenant_role_id = 'bb000003-0101-4000-8000-000000000101'
      AND pd.key IN ('iot-plc.read', 'iot-plc.manage')
    ORDER BY pd.key;
  `);
  const keys = rows.split("\n").map((line) => line.trim()).filter(Boolean);
  assert("bootstrap tenant-owner iot-plc.read grant", keys.includes("iot-plc.read"), rows);
  assert("bootstrap tenant-owner iot-plc.manage grant", keys.includes("iot-plc.manage"), rows);
}

function evaluateConnectivityBody(idempotencyKey) {
  return {
    endpointReference: "EP-OPC-INJ-LINE",
    evaluationWindowStartUtc: WINDOW_START,
    evaluationWindowEndUtc: WINDOW_END,
    evaluationTimeUtc: EVAL_TIME,
    profileReference: "QP-IOT-STANDARD",
    evidenceMode: "simulated",
    sourceProvenance: PROVENANCE,
    idempotencyKey,
  };
}

console.log("Running IOT-PLC-UI-1 permission verifier.");

removeIotPlcReadOverrideForMaintenance();
assertOwnerBootstrapIotPlcGrants();

const owner = await signInOwner();
const materials = await signIn("materials-sales@dev.local", requireEnv("DevelopmentSaasIdentity__ActiveUserPassword"));
seedIotPlcReadOverrideForMaintenance();
const maintenance = await signIn("maintenance-team@dev.local", requireEnv("DevelopmentSaasIdentity__ActiveUserPassword"));

const ownerRoute = await fetchWeb("/dashboard", owner);
const materialsRoute = await fetchWeb("/dashboard", materials);
const maintenanceRoute = await fetchWeb("/dashboard", maintenance);
const ownerIotPlcRoute = await fetchWeb("/iot-plc", owner);
const materialsIotPlcRoute = await fetchWeb("/iot-plc", materials);
const maintenanceIotPlcRoute = await fetchWeb("/iot-plc", maintenance);

assert("owner/admin /iot-plc permitted", ownerIotPlcRoute.status === 200);
assert("unauthorized user /iot-plc denied", materialsIotPlcRoute.status === 403);
assert("iot-plc-read user /iot-plc permitted", maintenanceIotPlcRoute.status === 200);

assert("owner sidebar IoT & PLC visible once", countIotPlcSidebarLinks(ownerRoute.html) === 1);
assert("unauthorized sidebar IoT & PLC hidden", countIotPlcSidebarLinks(materialsRoute.html) === 0);
assert("iot-plc-read sidebar IoT & PLC visible once", countIotPlcSidebarLinks(maintenanceRoute.html) === 1);

const nav = navigationSource();
const enerjiIndex = nav.indexOf('href: "/enerji"');
const iotIndex = nav.indexOf('href: "/iot-plc"');
assert("navigation order Enerji -> IoT & PLC", enerjiIndex > 0 && iotIndex > enerjiIndex);
assert("navigation permission iot-plc.read", nav.includes('permissionKey: "iot-plc.read"'));

const ownerWorkspace = await api("GET", `/api/iot-plc/workspace?facilityId=${FACILITY}`, undefined, owner);
assert("owner iot-plc workspace API", ownerWorkspace.status === 200);
assert("owner canManage from API", ownerWorkspace.payload?.capabilities?.canManage === true);

const managerWorkspace = await devApi("GET", `/api/iot-plc/workspace?facilityId=${FACILITY}`, undefined, MANAGER);
assert("iot-plc manager dev actor read", managerWorkspace.status === 200);

const readonlyWorkspace = await devApi("GET", `/api/iot-plc/workspace?facilityId=${FACILITY}`, undefined, READONLY);
assert("iot-plc.read reviewer dev actor read", readonlyWorkspace.status === 200);

const readonlyIntelligence = await devApi(
  "GET",
  `/api/iot-plc/intelligence/workspace?facilityId=${FACILITY}`,
  undefined,
  READONLY,
);
assert("iot-plc.read intelligence workspace", readonlyIntelligence.status === 200);

const beforeAssessments = assessmentCount();
const deniedEvaluate = await api(
  "POST",
  `/api/iot-plc/intelligence/connectivity-assessments/evaluate?facilityId=${FACILITY}`,
  evaluateConnectivityBody(`${CLOSURE_PREFIX}-denied-evaluate-${Date.now()}`),
  maintenance,
);
const afterAssessments = assessmentCount();
assert("iot-plc.read without manage mutation denied", deniedEvaluate.status === 403, `status=${deniedEvaluate.status}`);
assert("denied mutation created no records", afterAssessments === beforeAssessments, `${beforeAssessments} -> ${afterAssessments}`);

const ownerAccess = await api("GET", "/api/admin-access/me/effective-access", undefined, owner);
const granted = ownerAccess.payload?.grantedPermissionKeys ?? [];
assert("owner effective iot-plc.read", granted.includes("iot-plc.read"));
assert("owner has iot-plc.manage", granted.includes("iot-plc.manage"));

const maintenanceAccess = await api("GET", "/api/admin-access/me/effective-access", undefined, maintenance);
const maintenanceGranted = maintenanceAccess.payload?.grantedPermissionKeys ?? [];
assert("iot-plc-read user effective iot-plc.read", maintenanceGranted.includes("iot-plc.read"));
assert("iot-plc-read user lacks iot-plc.manage", !maintenanceGranted.includes("iot-plc.manage"));

assert(
  "direct-route permission agreement owner",
  ownerIotPlcRoute.status === 200 && granted.includes("iot-plc.read"),
);
assert(
  "direct-route permission agreement unauthorized",
  materialsIotPlcRoute.status === 403 && !materialsRoute.html.includes('href="/iot-plc"'),
);

const isolatedFacility = await devApi(
  "GET",
  `/api/iot-plc/workspace?facilityId=bb000001-0005-4000-8000-000000000099`,
  undefined,
  MANAGER,
);
assert("facility isolation enforced", isolatedFacility.status === 403 || isolatedFacility.status === 404);

assert("permission ids defined", IOT_PLC_READ_PERMISSION_ID.endsWith("078") && IOT_PLC_MANAGE_PERMISSION_ID.endsWith("079"));

db(`
  DELETE FROM iot_plc_connectivity_assessments
  WHERE evaluate_idempotency_key LIKE '${CLOSURE_PREFIX}%';
  DELETE FROM iot_plc_signal_quality_assessments
  WHERE evaluate_idempotency_key LIKE '${CLOSURE_PREFIX}%';
`);

removeIotPlcReadOverrideForMaintenance();

console.log("IOT-PLC-UI-1 permission verification passed.");
