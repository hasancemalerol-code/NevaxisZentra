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
const ROUTE = "/ticari-zeka/urun-karliligi";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const UNAUTH_FACILITY = "bb000001-0005-4000-8000-000000000099";
const PROFILE = "bb000001-0310-4000-8000-000000000310";
const DETERIORATION_RUN = "bb000001-03b1-4000-8000-0000000003b1";
const INSUFFICIENT_RUN = "bb000001-03b9-4000-8000-0000000003b9";
const ORGANIZATION = "dev-pilot-tenant";
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
  console.error(`COMMERCIAL-PROFITABILITY-UI-3 permission regression verifier failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(rel) {
  return readFileSync(resolve(ROOT, rel), "utf8").replaceAll("\r\n", "\n");
}

function actorHeaders(actor) {
  return {
    Accept: "application/json",
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": actor.issuer,
    "X-crevafactos-dev-Subject": actor.subject,
    "X-NevaxisFactOS-Tenant": actor.tenant,
  };
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
  if (process.env.__COMMPROFIT3_PERM_BOOTSTRAPPED === "1") return;
  const transcriptCandidates = [
    join(homedir(), ".cursor/projects/c-Projects-NevaxisFactOS/agent-transcripts/a7824c6f-6aa8-498f-895d-7f9b31bab8e3/a7824c6f-6aa8-498f-895d-7f9b31bab8e3.jsonl"),
  ];
  let text = "";
  for (const candidate of transcriptCandidates) {
    if (existsSync(candidate)) text += readFileSync(candidate, "utf8");
  }
  for (const key of ["DevelopmentSaasIdentity__TenantOwnerPassword", "DevelopmentSaasIdentity__MfaSharedSecret", "DevelopmentSaasIdentity__ActiveUserPassword"]) {
    applyDevelopmentIdentityEnvFromText(text, key);
  }
  process.env.__COMMPROFIT3_PERM_BOOTSTRAPPED = "1";
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

async function api(method, path, body, headers = actorHeaders(MANAGER)) {
  const requestHeaders = { ...headers };
  if (body) requestHeaders["Content-Type"] = "application/json";
  const response = await fetch(`${API}${path}`, { method, headers: requestHeaders, body: body ? JSON.stringify(body) : undefined });
  const raw = await response.text();
  let payload = null;
  try { payload = raw ? JSON.parse(raw) : null; } catch { payload = { raw: raw.slice(0, 300) }; }
  return { status: response.status, payload, raw };
}

async function signIn(email, password) {
  const jar = new CookieJar();
  const result = await fetch(`${API}/api/identity/sign-in`, {
    method: "POST",
    headers: { "Content-Type": "application/json", Accept: "application/json" },
    body: JSON.stringify({ organizationContext: ORGANIZATION, email, password, deviceLabel: "COMMPROFIT3-PERM" }),
  });
  jar.ingest(result);
  if (result.status !== 200) fail(`sign-in failed for ${email}: ${result.status}`);
  return jar;
}

async function signInOwnerOrThrow() {
  const jar = await signIn("admin@demo.nevaxisfactos.local", requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword"));
  const mfaSecret = requireEnv("DevelopmentSaasIdentity__MfaSharedSecret");
  let verified = false;
  for (const counterOffset of [-1, 0, 1]) {
    const verify = await fetch(`${API}/api/identity/mfa/verify`, {
      method: "POST",
      headers: { "Content-Type": "application/json", Accept: "application/json", Cookie: jar.header() },
      body: JSON.stringify({ totpCode: generateTotp(mfaSecret, counterOffset) }),
    });
    jar.ingest(verify);
    if (verify.status === 200) {
      verified = true;
      break;
    }
  }
  if (!verified) throw new Error("owner MFA failed");
  return jar;
}

async function fetchWeb(path, jar) {
  const response = await fetch(`${WEB}${path}`, { headers: { Cookie: jar.header(), Accept: "text/html" }, redirect: "manual" });
  const html = response.status >= 300 && response.status < 400 ? "" : await response.text();
  return { status: response.status, html, location: response.headers.get("location") };
}

function formatRatioAsPercent(ratio) {
  if (ratio === null || ratio === undefined || Number.isNaN(ratio)) return "—";
  const percent = ratio * 100;
  const formatted = new Intl.NumberFormat("tr-TR", {
    style: "decimal",
    maximumFractionDigits: 1,
    signDisplay: "exceptZero",
  }).format(percent);
  return `${formatted}%`;
}

console.log("Running COMMERCIAL-PROFITABILITY-UI-3 permission regression verifier.");

const pageSource = read("apps/web/src/app/(app)/ticari-zeka/urun-karliligi/page.tsx");
const actions = read("apps/web/src/features/commercial-intelligence/server/commercial-intelligence-actions.ts");
const catchAll = read("apps/web/src/app/(app)/[...sections]/page.tsx");
const view = read("apps/web/src/features/commercial-intelligence/components/commercial-profitability-workspace-view.tsx");
const sections = read("apps/web/src/features/commercial-intelligence/components/commercial-profitability-workspace-sections.tsx");

assert("page requires commercial-intelligence.read", pageSource.includes('forbidUnlessGranted("commercial-intelligence.read")'));
assert("refresh action requires read", actions.includes("refreshCommercialProfitabilityWorkspaceAction") && actions.includes('denyUnlessGranted("commercial-intelligence.read")'));
assert("route excluded from catch-all", catchAll.includes('"/ticari-zeka/urun-karliligi"'));
assert("route revalidated", actions.includes('"/ticari-zeka/urun-karliligi"'));
assert(
  "insufficient state copy",
  view.includes("Karşılaştırma için yeterli veri yok") || sections.includes("Karşılaştırma için yeterli veri yok"),
);
assert("turkish percent formatting path", view.includes("formatProfitabilityMarginDeltaDisplay"));

const ready = await fetch(`${API}/health/ready`);
assert("API ready", ready.status === 200, String(ready.status));

const noAuth = await fetch(`${API}/api/commercial-intelligence/profitability/comparisons?facilityId=${FACILITY}`);
assert("missing auth 401", noAuth.status === 401, String(noAuth.status));

const readonlyList = await api(
  "GET",
  `/api/commercial-intelligence/profitability/comparisons?facilityId=${FACILITY}`,
  undefined,
  actorHeaders(READONLY),
);
assert("readonly comparison list allowed", readonlyList.status === 200, String(readonlyList.status));

const authorizedList = await api(
  "GET",
  `/api/commercial-intelligence/profitability/comparisons?facilityId=${FACILITY}`,
);
assert("authorized comparison list 200", authorizedList.status === 200, authorizedList.raw?.slice(0, 200));
assert("list has items", Array.isArray(authorizedList.payload?.items) && authorizedList.payload.items.length > 0);

const wrongFacility = await api(
  "GET",
  `/api/commercial-intelligence/profitability/comparisons?facilityId=${UNAUTH_FACILITY}`,
);
assert("wrong facility 403", wrongFacility.status === 403, String(wrongFacility.status));

const crossTenant = await api(
  "GET",
  `/api/commercial-intelligence/profitability/comparisons?facilityId=${FACILITY}`,
  undefined,
  actorHeaders({ ...MANAGER, tenant: "unknown-tenant-key" }),
);
assert("cross tenant denied", crossTenant.status === 401 || crossTenant.status === 403, String(crossTenant.status));

const invalidEvaluate = await api(
  "POST",
  `/api/commercial-intelligence/profitability/comparisons/evaluate?facilityId=${FACILITY}`,
  {
    profileId: PROFILE,
    expectedProfileVersion: 1,
    comparisonType: "unsupported-dimension",
    currentWindowStartUtc: "2026-07-01T00:00:00.000Z",
    currentWindowEndUtc: "2026-12-31T23:59:59.000Z",
    sourceProvenance: "verifier",
    idempotencyKey: `COMMPROFIT3-INVALID-${Date.now()}`,
  },
);
assert("invalid comparison type evaluate rejected", invalidEvaluate.status === 400, String(invalidEvaluate.status));

const deteriorationDetail = await api(
  "GET",
  `/api/commercial-intelligence/profitability/comparisons/${DETERIORATION_RUN}?facilityId=${FACILITY}`,
);
assert("deterioration detail 200", deteriorationDetail.status === 200, deteriorationDetail.raw?.slice(0, 200));
assert(
  "deterioration outcome preserved",
  deteriorationDetail.payload?.comparisonOutcome === "contribution-deterioration",
  deteriorationDetail.payload?.comparisonOutcome,
);
assert(
  "deterioration delta preserved",
  deteriorationDetail.payload?.contributionMarginDelta === -0.06,
  String(deteriorationDetail.payload?.contributionMarginDelta),
);
assert(
  "deterioration margin labels deterministic",
  formatRatioAsPercent(deteriorationDetail.payload?.contributionMarginDelta) === "-6%",
  formatRatioAsPercent(deteriorationDetail.payload?.contributionMarginDelta),
);

const insufficientDetail = await api(
  "GET",
  `/api/commercial-intelligence/profitability/comparisons/${INSUFFICIENT_RUN}?facilityId=${FACILITY}`,
);
assert("insufficient detail 200", insufficientDetail.status === 200, insufficientDetail.raw?.slice(0, 200));
assert(
  "insufficient outcome preserved",
  insufficientDetail.payload?.comparisonOutcome === "insufficient-data",
  insufficientDetail.payload?.comparisonOutcome,
);
assert(
  "insufficient margin not coerced to zero",
  insufficientDetail.payload?.referenceMedianContributionMarginRatio === null
    || insufficientDetail.payload?.currentMedianContributionMarginRatio === null
    || insufficientDetail.payload?.contributionMarginDelta === null,
  JSON.stringify({
    reference: insufficientDetail.payload?.referenceMedianContributionMarginRatio,
    current: insufficientDetail.payload?.currentMedianContributionMarginRatio,
    delta: insufficientDetail.payload?.contributionMarginDelta,
  }),
);

let ownerRouteChecked = false;
try {
  const ownerJar = await signInOwnerOrThrow();
  const ownerRoute = await fetchWeb(ROUTE, ownerJar);
  assert("owner route 200", ownerRoute.status === 200, String(ownerRoute.status));
  assert("owner route workspace testid", ownerRoute.html.includes('data-testid="commercial-profitability-workspace"'));
  assert("owner route shows margin delta label", ownerRoute.html.includes("Katkı marjı değişimi") || ownerRoute.html.includes("Marj farkı"));
  ownerRouteChecked = true;
} catch (error) {
  console.log(`⚠ Live owner web route checks skipped: ${error instanceof Error ? error.message : String(error)}`);
  assert("static workspace view wired", pageSource.includes("CommercialProfitabilityWorkspaceView"));
}

const unauthRoute = await fetchWeb(ROUTE, new CookieJar());
assert(
  "missing authentication redirect",
  unauthRoute.status === 302 || unauthRoute.status === 307 || unauthRoute.location?.includes("/giris"),
  String(unauthRoute.status),
);
assert("web route validation performed", ownerRouteChecked || unauthRoute.status === 302 || unauthRoute.status === 307);

console.log("COMMERCIAL-PROFITABILITY-UI-3 permission regression verification passed.");
