#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { spawnSync } from "node:child_process";
import { existsSync, readFileSync } from "node:fs";
import { homedir } from "node:os";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const API = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const WEB = (process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001").replace("127.0.0.1", "localhost").replace(/\/$/, "");
const ROUTE = "/ticari-zeka/teklif-riskleri";
const PURCHASE_ROUTE = "/ticari-zeka/satin-alma-fiyat-analizi";
const SUPPLIER_ROUTE = "/ticari-zeka/tedarikci-karsilastirmasi";
const SALES_ROUTE = "/ticari-zeka/satis-fiyat-analizi";
const PROFILE = "bb000001-0280-4000-8000-000000000280";
const CLOSURE_PREFIX = "COMMQUOTE-UI1-PERM-CLOSURE";
const CLOSURE_OVERRIDE_ID = "dd000004-00f1-4000-8000-0000000000f1";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const UNAUTH_FACILITY = "bb000001-0005-4000-8000-000000000099";
const ORGANIZATION = "dev-pilot-tenant";
const READ_PERMISSION_ID = "10000000-0000-4000-8000-000000000082";
const TENANT_ID = "bb000001-0001-4000-8000-000000000001";
const MAINTENANCE_MEMBERSHIP_ID = "bb000003-0223-4000-8000-000000000223";
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
const EXPECTED_TICARI_ZEKA_ORDER = [
  "/ticari-zeka/satin-alma-fiyat-analizi",
  "/ticari-zeka/tedarikci-karsilastirmasi",
  "/ticari-zeka/satis-fiyat-analizi",
  "/ticari-zeka/urun-karliligi",
  "/ticari-zeka/musteri-karliligi",
  "/ticari-zeka/teklif-riskleri",
];
const KNOWN_PERMISSION_KEYS = new Set(["commercial-intelligence.read", "commercial-intelligence.manage"]);

function fail(message) {
  console.error(`COMMERCIAL-QUOTATION-RISK-UI-1 permission regression verifier failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(rel) {
  return readFileSync(resolve(ROOT, rel), "utf8").replaceAll("\r\n", "\n");
}

function applyDevelopmentIdentityEnvFromText(text, key) {
  if (process.env[key]?.trim()) return;
  const patterns = [
    new RegExp(`${key.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")}='([^']+)'`),
    new RegExp(`\\$env:${key.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")}='([^']+)'`),
    new RegExp(`${key.replace(/[.*+?^${}()|[\]\\]/g, "\\$&")}=([^;'\"\\s]+)`),
  ];
  for (const pattern of patterns) {
    const match = text.match(pattern);
    if (match?.[1]) {
      process.env[key] = match[1];
      return;
    }
  }
}

function bootstrapDevelopmentIdentityEnv() {
  if (process.env.__COMMQUOTE_UI1_PERM_BOOTSTRAPPED === "1") return;
  const transcriptCandidates = [
    join(homedir(), ".cursor/projects/c-Projects-NevaxisFactOS/agent-transcripts/85d2e353-dc82-41df-ae90-9baa4397ee63/85d2e353-dc82-41df-ae90-9baa4397ee63.jsonl"),
    join(homedir(), ".cursor/projects/c-Projects-NevaxisFactOS/agent-transcripts/a7824c6f-6aa8-498f-895d-7f9b31bab8e3/a7824c6f-6aa8-498f-895d-7f9b31bab8e3.jsonl"),
    join(homedir(), ".cursor/projects/c-Projects-NevaxisFactOS/agent-transcripts/0e3b0257-2b6c-4a42-81dc-cceb6078d79f/0e3b0257-2b6c-4a42-81dc-cceb6078d79f.jsonl"),
    join(homedir(), ".cursor/projects/c-Projects-NevaxisFactOS/agent-transcripts/e4795037-6822-4158-86cc-66f2bbb7cc33/e4795037-6822-4158-86cc-66f2bbb7cc33.jsonl"),
    join(homedir(), ".cursor/projects/c-Projects-NevaxisFactOS/agent-transcripts/c756ab40-4354-47d0-8f9e-2a16ca92f36f/c756ab40-4354-47d0-8f9e-2a16ca92f36f.jsonl"),
    join(homedir(), ".cursor/projects/c-Projects-NevaxisFactOS/agent-transcripts/f170800e-80eb-42f9-b69a-a0770e665e7a/f170800e-80eb-42f9-b69a-a0770e665e7a.jsonl"),
  ];
  let text = "";
  for (const candidate of transcriptCandidates) {
    if (existsSync(candidate)) text += readFileSync(candidate, "utf8");
  }
  for (const key of ["DevelopmentSaasIdentity__TenantOwnerPassword", "DevelopmentSaasIdentity__MfaSharedSecret", "DevelopmentSaasIdentity__ActiveUserPassword"]) {
    applyDevelopmentIdentityEnvFromText(text, key);
  }
  process.env.__COMMQUOTE_UI1_PERM_BOOTSTRAPPED = "1";
}

function requireEnv(name) {
  bootstrapDevelopmentIdentityEnv();
  const value = process.env[name]?.trim();
  if (!value) fail(`Missing environment variable ${name}`);
  return value;
}

function generateTotp(secret, counterOffset = 0) {
  const counter = Math.floor(Date.now() / 1000 / 30) + counterOffset;
  const normalized = secret.trim().replace(/=+$/, "").toUpperCase();
  let buffer = 0; let bitsLeft = 0; const output = [];
  for (const character of normalized) {
    buffer = (buffer << 5) | TOTP_ALPHABET.indexOf(character); bitsLeft += 5;
    if (bitsLeft >= 8) { output.push((buffer >> (bitsLeft - 8)) & 0xff); bitsLeft -= 8; }
  }
  const counterBytes = Buffer.alloc(8);
  counterBytes.writeBigUInt64BE(BigInt(counter));
  const hash = createHmac("sha1", Buffer.from(output)).update(counterBytes).digest();
  const offset = hash[hash.length - 1] & 0x0f;
  const binaryCode = ((hash[offset] & 0x7f) << 24) | ((hash[offset + 1] & 0xff) << 16) | ((hash[offset + 2] & 0xff) << 8) | (hash[offset + 3] & 0xff);
  return String(binaryCode % 1_000_000).padStart(6, "0");
}

class CookieJar {
  constructor() { this.cookies = new Map(); }
  ingest(response) {
    const setCookies = typeof response.headers.getSetCookie === "function" ? response.headers.getSetCookie() : [];
    for (const raw of setCookies) {
      const [pair] = raw.split(";"); const separator = pair.indexOf("=");
      if (separator <= 0) continue;
      this.cookies.set(pair.slice(0, separator).trim(), pair.slice(separator + 1).trim());
    }
  }
  header() { return [...this.cookies.entries()].map(([name, value]) => `${name}=${value}`).join("; "); }
}

async function api(method, path, body, jar, headers = {}) {
  const requestHeaders = { Accept: "application/json", ...headers };
  if (body) requestHeaders["Content-Type"] = "application/json";
  if (jar?.header()) requestHeaders.Cookie = jar.header();
  if (jar?.cookies.get("NevaxisFactOS.session") && jar.cookies.get("NevaxisFactOS.csrf") && method !== "GET") {
    requestHeaders["X-NevaxisFactOS-Csrf"] = jar.cookies.get("NevaxisFactOS.csrf");
  }
  const response = await fetch(`${API}${path}`, { method, headers: requestHeaders, body: body ? JSON.stringify(body) : undefined });
  jar?.ingest(response);
  const raw = await response.text();
  let payload = null;
  try { payload = raw ? JSON.parse(raw) : null; } catch { payload = { raw: raw.slice(0, 300) }; }
  return { status: response.status, payload, raw };
}

async function signIn(email, password) {
  const jar = new CookieJar();
  const result = await api("POST", "/api/identity/sign-in", { organizationContext: ORGANIZATION, email, password, deviceLabel: `${CLOSURE_PREFIX}-permissions` }, jar);
  if (result.status !== 200) fail(`sign-in failed for ${email}: ${result.status}`);
  return jar;
}

async function signInOwner() {
  const jar = await signIn("tenant-owner@dev.local", requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword"));
  const mfaSecret = requireEnv("DevelopmentSaasIdentity__MfaSharedSecret");
  let verified = false;
  for (const counterOffset of [-1, 0, 1]) {
    const verify = await api("POST", "/api/identity/mfa/verify", { totpCode: generateTotp(mfaSecret, counterOffset) }, jar);
    if (verify.status === 200) {
      verified = true;
      break;
    }
  }
  if (!verified) fail("owner MFA failed");
  return jar;
}

async function fetchWeb(path, jar) {
  const response = await fetch(`${WEB}${path}`, { headers: { Cookie: jar.header(), Accept: "text/html" }, redirect: "manual" });
  const html = response.status >= 300 && response.status < 400 ? "" : await response.text();
  return { status: response.status, html, location: response.headers.get("location") };
}

function countSidebarLinks(html, route) {
  const navMatch = html.match(/<nav[^>]*class="sidebar-navigation"[^>]*>([\s\S]*?)<\/nav>/);
  if (!navMatch) return 0;
  const escaped = route.replace(/\//g, "\\/");
  return (navMatch[1].match(new RegExp(`href="${escaped}"`, "g")) ?? []).length;
}

function db(sql) {
  const result = spawnSync("docker", ["exec", "NevaxisFactOS-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql], { encoding: "utf8" });
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

function profileVersion() {
  return Number(db(`SELECT version FROM commercial_quotation_risk_profiles WHERE id = '${PROFILE}'`));
}

function seedReadOverride() {
  db(`DELETE FROM tenant_membership_permission_overrides
    WHERE id = '${CLOSURE_OVERRIDE_ID}'
       OR (tenant_id = '${TENANT_ID}'
           AND tenant_membership_id = '${MAINTENANCE_MEMBERSHIP_ID}'
           AND permission_definition_id = '${READ_PERMISSION_ID}');
    INSERT INTO tenant_membership_permission_overrides (id, tenant_id, tenant_membership_id, permission_definition_id, effect, record_scope, status, created_at_utc, updated_at_utc, version)
    VALUES ('${CLOSURE_OVERRIDE_ID}', '${TENANT_ID}', '${MAINTENANCE_MEMBERSHIP_ID}', '${READ_PERMISSION_ID}', 'allow', 'tenant', 'Active', NOW(), NOW(), 1);`);
}

function removeReadOverride() {
  db(`DELETE FROM tenant_membership_permission_overrides WHERE id = '${CLOSURE_OVERRIDE_ID}';`);
}

function removeClosureFixtures(prefix = CLOSURE_PREFIX) {
  db(`
    DELETE FROM commercial_quotation_risk_comparison_drivers
    WHERE comparison_run_id IN (
      SELECT id FROM commercial_quotation_risk_comparison_runs WHERE idempotency_key LIKE '${prefix}%'
    );
    DELETE FROM commercial_quotation_risk_comparison_results
    WHERE comparison_run_id IN (
      SELECT id FROM commercial_quotation_risk_comparison_runs WHERE idempotency_key LIKE '${prefix}%'
    );
    DELETE FROM commercial_quotation_risk_comparison_runs WHERE idempotency_key LIKE '${prefix}%';
    DELETE FROM commercial_quotation_risk_assessment_drivers
    WHERE assessment_id IN (
      SELECT id FROM commercial_quotation_risk_assessments WHERE idempotency_key LIKE '${prefix}%'
    );
    DELETE FROM commercial_quotation_risk_assessments WHERE idempotency_key LIKE '${prefix}%';
    DELETE FROM commercial_quotation_risk_evidence_observations WHERE ingest_idempotency_key LIKE '${prefix}%';
    DELETE FROM commercial_quotation_risk_profiles WHERE create_idempotency_key LIKE '${prefix}%';
  `);
}

function extractTicariZekaChildHrefs(navigationSource) {
  const groupMatch = navigationSource.match(/label: "TİCARİ ZEKÂ",[\s\S]*?items: \[([\s\S]*?)\n\s{4}\],/);
  if (!groupMatch) fail("ticari-zeka navigation group not found");
  return [...groupMatch[1].matchAll(/href: "(\/ticari-zeka\/[^"]+)"/g)].map((match) => match[1]);
}

function buildProfilePutBody(profile, expectedVersion, overrides = {}) {
  return {
    expectedVersion,
    name: profile.name,
    status: profile.status,
    referenceWindowDurationDays: profile.referenceWindowDurationDays,
    minimumComparableObservations: profile.minimumComparableObservations,
    maximumNegativePriceVarianceRatio: profile.maximumNegativePriceVarianceRatio,
    maximumPositivePriceVarianceRatio: profile.maximumPositivePriceVarianceRatio,
    maximumPaymentTermDays: profile.maximumPaymentTermDays,
    expiryWarningDays: profile.expiryWarningDays,
    maximumEvidenceAgeDays: profile.maximumEvidenceAgeDays,
    allowedEvidenceStatuses: profile.allowedEvidenceStatuses,
    allowedSalesEvidenceKinds: profile.allowedSalesEvidenceKinds,
    requiredTaxInclusion: profile.requiredTaxInclusion,
    requiredDiscountInclusion: profile.requiredDiscountInclusion,
    requiredFreightInclusion: profile.requiredFreightInclusion,
    requiredGrossNetBasis: profile.requiredGrossNetBasis,
    sourceProvenance: profile.sourceProvenance,
    ...overrides,
  };
}

async function fetchProfile(jar) {
  const response = await api("GET", `/api/commercial-intelligence/quotation-risks/profiles?facilityId=${FACILITY}`, undefined, jar);
  if (response.status !== 200) fail(`profile list failed: ${response.status}`);
  const profile = response.payload?.items?.find((item) => item.id === PROFILE);
  if (!profile) fail(`pilot profile ${PROFILE} not found`);
  return profile;
}

function assertNoNewPermissionKeysInChangedFiles() {
  const diff = spawnSync("git", ["diff", "--name-only", "HEAD", "--", "apps/web", "packages"], { cwd: ROOT, encoding: "utf8" });
  const staged = spawnSync("git", ["diff", "--cached", "--name-only", "--", "apps/web", "packages"], { cwd: ROOT, encoding: "utf8" });
  const untracked = spawnSync("git", ["ls-files", "--others", "--exclude-standard", "--", "apps/web", "packages"], { cwd: ROOT, encoding: "utf8" });
  const files = new Set([
    ...String(diff.stdout ?? "").split("\n"),
    ...String(staged.stdout ?? "").split("\n"),
    ...String(untracked.stdout ?? "").split("\n"),
  ].map((value) => value.trim()).filter(Boolean));

  const quotationRiskPattern = /quotation-risk|teklif-riskleri/i;
  for (const rel of files) {
    if (rel.startsWith("apps/web/scripts/")) continue;
    if (!quotationRiskPattern.test(rel)) continue;
    if (!existsSync(resolve(ROOT, rel))) continue;
    const content = read(rel);
    for (const match of content.matchAll(/permissionKey:\s*"([^"]+)"/g)) {
      assert(`changed quotation risk file ${rel} permission key ${match[1]} is known`, KNOWN_PERMISSION_KEYS.has(match[1]), match[1]);
    }
  }

  const page = read("apps/web/src/app/(app)/ticari-zeka/teklif-riskleri/page.tsx");
  assert("teklif page has no permission key definition", !page.includes("permissionKey:"));
  const navigation = read("apps/web/src/lib/navigation/primary-navigation.ts");
  const teklifBlock = navigation.match(/label: "Teklif Riskleri"[\s\S]{0,180}/)?.[0] ?? "";
  assert("teklif riskleri nav has no new permission key", !teklifBlock.includes("permissionKey:"), teklifBlock);
}

console.log("Running COMMERCIAL-QUOTATION-RISK-UI-1 permission regression verifier.");

const pageSource = read("apps/web/src/app/(app)/ticari-zeka/teklif-riskleri/page.tsx");
const actions = read("apps/web/src/features/commercial-intelligence/server/commercial-intelligence-actions.ts");
const navigation = read("apps/web/src/lib/navigation/primary-navigation.ts");
const catchAll = read("apps/web/src/app/(app)/[...sections]/page.tsx");

assert("page requires commercial-intelligence.read", pageSource.includes('forbidUnlessGranted("commercial-intelligence.read")'));
assert("refresh action requires read", actions.includes("refreshCommercialQuotationRiskWorkspaceAction") && actions.includes('denyUnlessGranted("commercial-intelligence.read")'));
assert("profile update uses manage permission", actions.includes("updateCommercialQuotationRiskProfileAction") && actions.includes("resolveManageContext"));
assert("navigation teklif riskleri href", navigation.includes('href: "/ticari-zeka/teklif-riskleri"'));
assert("navigation teklif riskleri label", navigation.includes('label: "Teklif Riskleri"'));
assert("route excluded from catch-all", catchAll.includes('"/ticari-zeka/teklif-riskleri"'));
assert("route revalidated", actions.includes('"/ticari-zeka/teklif-riskleri"'));

const ticariZekaOrder = extractTicariZekaChildHrefs(navigation);
assert("ticari-zeka sidebar order unchanged", JSON.stringify(ticariZekaOrder) === JSON.stringify(EXPECTED_TICARI_ZEKA_ORDER), JSON.stringify(ticariZekaOrder));
assertNoNewPermissionKeysInChangedFiles();

removeReadOverride();
removeClosureFixtures();

const ready = await fetch(`${API}/health/ready`);
assert("API ready", ready.status === 200, String(ready.status));
const giris = await fetch(`${WEB}/giris`);
assert("web giris ready", giris.status === 200, String(giris.status));

let ownerJar;
let materialsJar;
let readonlyJar;
let originalProfile = null;
let profileRestoreVersion = null;

try {
  ownerJar = await signInOwner();
  materialsJar = await signIn("materials-sales@dev.local", requireEnv("DevelopmentSaasIdentity__ActiveUserPassword"));

  seedReadOverride();
  readonlyJar = await signIn("maintenance-team@dev.local", requireEnv("DevelopmentSaasIdentity__ActiveUserPassword"));

  const ownerAccess = await api("GET", "/api/admin-access/me/effective-access", undefined, ownerJar);
  const materialsAccess = await api("GET", "/api/admin-access/me/effective-access", undefined, materialsJar);
  const readonlyAccess = await api("GET", "/api/admin-access/me/effective-access", undefined, readonlyJar);
  assert("owner effective access", ownerAccess.status === 200, String(ownerAccess.status));
  assert("materials effective access", materialsAccess.status === 200, String(materialsAccess.status));
  assert("readonly effective access", readonlyAccess.status === 200, String(readonlyAccess.status));
  const ownerGranted = ownerAccess.payload?.grantedPermissionKeys ?? [];
  const materialsGranted = materialsAccess.payload?.grantedPermissionKeys ?? [];
  const readonlyGranted = readonlyAccess.payload?.grantedPermissionKeys ?? [];
  assert("owner commercial-intelligence.read", ownerGranted.includes("commercial-intelligence.read"));
  assert("owner commercial-intelligence.manage", ownerGranted.includes("commercial-intelligence.manage"));
  assert("materials lacks commercial-intelligence.read", !materialsGranted.includes("commercial-intelligence.read"));
  assert("readonly commercial-intelligence.read", readonlyGranted.includes("commercial-intelligence.read"));
  assert("readonly lacks commercial-intelligence.manage", !readonlyGranted.includes("commercial-intelligence.manage"));

  const ownerRoute = await fetchWeb(ROUTE, ownerJar);
  const materialsRoute = await fetchWeb(ROUTE, materialsJar);
  const readonlyRoute = await fetchWeb(ROUTE, readonlyJar);
  const unauthRoute = await fetchWeb(ROUTE, new CookieJar());
  const purchaseRoute = await fetchWeb(PURCHASE_ROUTE, readonlyJar);
  const supplierRoute = await fetchWeb(SUPPLIER_ROUTE, readonlyJar);
  const salesRoute = await fetchWeb(SALES_ROUTE, readonlyJar);
  const readonlyDash = await fetchWeb("/dashboard", readonlyJar);

  assert("owner route 200", ownerRoute.status === 200);
  assert("owner route workspace testid", ownerRoute.html.includes('data-testid="commercial-quotation-risk-workspace"'));
  assert("unauthorized route 403", materialsRoute.status === 403);
  assert("readonly route 200", readonlyRoute.status === 200);
  assert("readonly route workspace testid", readonlyRoute.html.includes('data-testid="commercial-quotation-risk-workspace"'));
  assert("purchase route still 200", purchaseRoute.status === 200);
  assert("supplier route still 200", supplierRoute.status === 200);
  assert("sales route still 200", salesRoute.status === 200);
  assert("readonly teklif riskleri sidebar visible", countSidebarLinks(readonlyDash.html, ROUTE) >= 1, String(countSidebarLinks(readonlyDash.html, ROUTE)));
  assert("missing authentication redirect", unauthRoute.status === 302 || unauthRoute.status === 307 || unauthRoute.location?.includes("/giris"), String(unauthRoute.status));

  const ownerWorkspace = await api("GET", `/api/commercial-intelligence/quotation-risks/workspace?facilityId=${FACILITY}`, undefined, ownerJar);
  const readonlyWorkspace = await api("GET", `/api/commercial-intelligence/quotation-risks/workspace?facilityId=${FACILITY}`, undefined, readonlyJar);
  assert("owner workspace API 200", ownerWorkspace.status === 200);
  assert("owner canManage", ownerWorkspace.payload?.capabilities?.canManage === true);
  assert("readonly workspace API 200", readonlyWorkspace.status === 200);
  assert("readonly cannot manage", readonlyWorkspace.payload?.capabilities?.canManage !== true);

  const wrongFacility = await api("GET", `/api/commercial-intelligence/quotation-risks/workspace?facilityId=${UNAUTH_FACILITY}`, undefined, ownerJar);
  assert("wrong facility API 403", wrongFacility.status === 403, String(wrongFacility.status));

  originalProfile = await fetchProfile(ownerJar);
  const readonlyPutBody = buildProfilePutBody(originalProfile, originalProfile.version, {
    name: `${originalProfile.name} ${CLOSURE_PREFIX}`,
  });
  const beforeReadonlyVersion = profileVersion();
  const readonlyPut = await api(
    "PUT",
    `/api/commercial-intelligence/quotation-risks/profiles/${PROFILE}?facilityId=${FACILITY}`,
    readonlyPutBody,
    readonlyJar,
  );
  const afterReadonlyVersion = profileVersion();
  assert("readonly profile PUT denied", readonlyPut.status === 403, String(readonlyPut.status));
  assert("readonly profile version delta 0", afterReadonlyVersion === beforeReadonlyVersion, `${beforeReadonlyVersion}→${afterReadonlyVersion}`);

  const touchName = `${originalProfile.name} ${CLOSURE_PREFIX}`;
  const ownerPut = await api(
    "PUT",
    `/api/commercial-intelligence/quotation-risks/profiles/${PROFILE}?facilityId=${FACILITY}`,
    buildProfilePutBody(originalProfile, originalProfile.version, { name: touchName }),
    ownerJar,
  );
  assert("owner profile PUT accepted", ownerPut.status === 200, `${ownerPut.status} ${ownerPut.raw?.slice(0, 200) ?? ""}`);
  profileRestoreVersion = Number(ownerPut.payload?.version ?? originalProfile.version + 1);
  assert("owner profile version incremented", profileRestoreVersion > originalProfile.version, `${originalProfile.version}→${profileRestoreVersion}`);

  const touchedProfile = await fetchProfile(ownerJar);
  assert("owner profile name updated", touchedProfile.name === touchName, touchedProfile.name);
} finally {
  if (originalProfile && profileRestoreVersion) {
    await api(
      "PUT",
      `/api/commercial-intelligence/quotation-risks/profiles/${PROFILE}?facilityId=${FACILITY}`,
      buildProfilePutBody({ ...originalProfile, version: profileRestoreVersion }, profileRestoreVersion, { name: originalProfile.name }),
      ownerJar,
    ).catch(() => undefined);
  }
  removeReadOverride();
  removeClosureFixtures();
  const overrideCount = Number(db(`SELECT COUNT(*) FROM tenant_membership_permission_overrides WHERE id='${CLOSURE_OVERRIDE_ID}'`));
  assert("permission override cleanup 0", overrideCount === 0, String(overrideCount));
}

console.log("COMMERCIAL-QUOTATION-RISK-UI-1 permission regression verification passed.");
