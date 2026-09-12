#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const root = join(dirname(fileURLToPath(import.meta.url)), "..");
const API = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const WEB = process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://127.0.0.1:3001";
const ORGANIZATION = "dev-pilot-tenant";

const OWNER_HREFS = [
  "/",
  "/ai-gorevleri/ai-task-001",
  "/ai-gorevleri/ai-task-002",
  "/ai-gorevleri/ai-task-003",
  "/proses-oee",
  "/uretim",
  "/makineler",
  "/malzemeler",
  "/kaliplar",
  "/bakim",
  "/planlama",
  "/kalite",
  "/riskler/siparis-termin",
];

function fail(message) {
  console.error(`DASHBOARD live navigation verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function requireEnv(name) {
  const value = process.env[name];
  if (!value) fail(`Missing environment variable ${name}`);
  return value;
}

function read(relativePath) {
  return readFileSync(join(root, relativePath), "utf8");
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

async function api(method, path, body, jar) {
  const headers = { Accept: "application/json" };
  if (body) headers["Content-Type"] = "application/json";
  if (jar?.header()) headers.Cookie = jar.header();
  if (jar?.cookies.get("NevaxisFactOS.session") && jar.cookies.get("NevaxisFactOS.csrf") && method !== "GET") {
    headers["X-NevaxisFactOS-Csrf"] = jar.cookies.get("NevaxisFactOS.csrf");
  }
  const response = await fetch(`${API}${path}`, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined,
  });
  jar?.ingest(response);
  return { status: response.status };
}

async function signIn(email, password) {
  const jar = new CookieJar();
  const result = await api("POST", "/api/identity/sign-in", {
    organizationContext: ORGANIZATION,
    email,
    password,
    deviceLabel: "dashboard-live-navigation",
  }, jar);
  if (result.status !== 200) fail("sign-in failed");
  return jar;
}

async function signInOwner() {
  const secret = requireEnv("DevelopmentSaasIdentity__MfaSharedSecret");
  const jar = await signIn("admin@demo.nevaxisfactos.local", requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword"));
  const alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
  const counter = Math.floor(Date.now() / 1000 / 30);
  const normalized = secret.trim().replace(/=+$/, "").toUpperCase();
  let buffer = 0;
  let bitsLeft = 0;
  const output = [];
  for (const character of normalized) {
    buffer = (buffer << 5) | alphabet.indexOf(character);
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
  const verify = await api("POST", "/api/identity/mfa/verify", {
    totpCode: String(binaryCode % 1_000_000).padStart(6, "0"),
  }, jar);
  if (verify.status !== 200) fail("owner MFA failed");
  return jar;
}

async function fetchWeb(path, jar) {
  const response = await fetch(`${WEB}${path}`, {
    headers: jar?.header() ? { Cookie: jar.header() } : undefined,
    redirect: "manual",
  });
  return {
    status: response.status,
    html: await response.text(),
  };
}

function extractDashboardHrefs(html) {
  const hrefs = new Set();
  const pattern = /href="(\/[^"#?]*)"/g;
  for (const match of html.matchAll(pattern)) {
    if (
      match[1].startsWith("/ai-gorevleri/")
      || match[1] === "/"
      || match[1] === "/proses-oee"
      || match[1] === "/uretim"
      || match[1] === "/makineler"
      || match[1] === "/malzemeler"
      || match[1] === "/kaliplar"
      || match[1] === "/bakim"
      || match[1] === "/planlama"
      || match[1] === "/kalite"
      || match[1].startsWith("/riskler/")
    ) {
      hrefs.add(match[1]);
    }
  }
  return [...hrefs];
}

const dashboardMock = read("src/lib/mock-data/dashboard.ts");
const aiSummary = read("src/components/dashboard/ai-task-center-summary.tsx");
const navigation = read("src/components/dashboard/dashboard-navigation.ts");

assert("dashboard ai tasks use canonical catalog ids", /id: "ai-task-001"/.test(dashboardMock));
assert("no fabricated task-* dashboard ids remain", !/id: "task-/.test(dashboardMock));
assert("ai summary resolves canonical navigation", aiSummary.includes("resolveAiTaskNavTarget"));
assert("ai summary has list-scoped fallback label", aiSummary.includes("AI Görev Merkezi'nde incele"));
assert("no href-hash controls in dashboard navigation sources", !/href=["']#["']/.test(aiSummary + navigation));
assert("no active not-allowed cursor on authorized inspect button", !/inspectButton:[\s\S]{0,220}cursor: "not-allowed"/.test(aiSummary));

const owner = await signInOwner();
const dashboard = await fetchWeb("/dashboard", owner);
assert("owner dashboard loads", dashboard.status === 200);

const hrefs = extractDashboardHrefs(dashboard.html);
assert("owner dashboard exposes ai detail links", hrefs.some((href) => href.startsWith("/ai-gorevleri/ai-task-")));
assert("owner dashboard exposes AI Task Center footer", hrefs.includes("/"));
assert("owner dashboard exposes operational summary link", hrefs.includes("/proses-oee"));
assert("owner dashboard exposes production workspace", hrefs.includes("/uretim"));
assert("owner dashboard exposes maintenance workspace", hrefs.includes("/bakim"));

for (const href of OWNER_HREFS) {
  const result = await fetchWeb(href, owner);
  assert(`owner href ${href} is not 404`, result.status !== 404, `actual ${result.status}`);
  assert(`owner href ${href} is not 403`, result.status !== 403, `actual ${result.status}`);
}

const maintenance = await signIn("maintenance-team@dev.local", requireEnv("DevelopmentSaasIdentity__ActiveUserPassword"));
const maintenanceDashboard = await fetchWeb("/dashboard", maintenance);
assert("maintenance-only dashboard loads", maintenanceDashboard.status === 200);
assert(
  "maintenance-only does not expose machine workspace link",
  !/href="\/makineler"/.test(maintenanceDashboard.html),
);
assert(
  "maintenance-only exposes maintenance workspace",
  /href="\/bakim"/.test(maintenanceDashboard.html),
);

const machine = await signIn("machine-responsible@dev.local", requireEnv("DevelopmentSaasIdentity__ActiveUserPassword"));
const machineDashboard = await fetchWeb("/dashboard", machine);
assert("machine-responsible dashboard loads", machineDashboard.status === 200);
assert(
  "machine-responsible exposes machine workspace",
  /href="\/makineler"/.test(machineDashboard.html),
);
assert(
  "machine-responsible does not expose material workspace link",
  !/href="\/malzemeler"/.test(machineDashboard.html),
);

console.log("DASHBOARD live navigation verification passed.");
