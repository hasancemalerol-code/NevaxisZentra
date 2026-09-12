#!/usr/bin/env node
import { createHmac } from "node:crypto";

const API = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const WEB = process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://127.0.0.1:3001";
const ORGANIZATION = "dev-pilot-tenant";

const OPERATIONAL_QUICK_ACTIONS = {
  "/uretim": "production",
  "/planlama": "planning",
  "/bakim": "maintenance",
  "/kalite": "quality",
  "/erp-entegrasyonlari/senkronizasyonlar": "erp",
};

function fail(message) {
  console.error(`DASHBOARD action permission verification failed: ${message}`);
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
    deviceLabel: "dashboard-action-permissions",
  }, jar);
  if (result.status !== 200) fail("sign-in failed for persona");
  return jar;
}

async function signInOwner() {
  const secret = requireEnv("DevelopmentSaasIdentity__MfaSharedSecret");
  const jar = await signIn("tenant-owner@dev.local", requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword"));
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
  const totp = String(binaryCode % 1_000_000).padStart(6, "0");
  const verify = await api("POST", "/api/identity/mfa/verify", { totpCode: totp }, jar);
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

function extractQuickActionHrefs(html) {
  const hrefs = new Set();
  const pattern = /<a\b[^>]*class="[^"]*locked-quick-action[^"]*"[^>]*>|<a\b[^>]*href="[^"]+"[^>]*class="[^"]*locked-quick-action[^"]*"[^>]*>/g;
  const matches = html.match(pattern) ?? [];
  for (const tag of matches) {
    const href = tag.match(/href="([^"]+)"/)?.[1];
    if (href) hrefs.add(href);
  }
  return hrefs;
}

function hasQuickAction(html, href) {
  return extractQuickActionHrefs(html).has(href);
}

function hasActiveFooterLink(html, href, label) {
  const escapedLabel = label.replace(/[.*+?^${}()|[\]\\]/g, "\\$&");
  return new RegExp(`<a[^>]*href="${href}"[^>]*>[\\s\\S]{0,120}${escapedLabel}`).test(html);
}

function assertNoUnauthorizedOperationalActions(label, html, allowedHrefs) {
  const found = [...extractQuickActionHrefs(html)].filter((href) => href in OPERATIONAL_QUICK_ACTIONS);
  const unexpected = found.filter((href) => !allowedHrefs.includes(href));
  assert(`${label} has no unauthorized operational quick actions`, unexpected.length === 0, unexpected.join(", "));
}

const password = requireEnv("DevelopmentSaasIdentity__ActiveUserPassword");
const ownerJar = await signInOwner();
const machineJar = await signIn("machine-responsible@dev.local", password);
const salesJar = await signIn("materials-sales@dev.local", password);
const maintenanceJar = await signIn("maintenance-team@dev.local", password);

const owner = await fetchWeb("/dashboard", ownerJar);
const machine = await fetchWeb("/dashboard", machineJar);
const sales = await fetchWeb("/dashboard", salesJar);
const maintenance = await fetchWeb("/dashboard", maintenanceJar);

assert("owner dashboard loads", owner.status === 200);
assert("machine-responsible dashboard loads", machine.status === 200);
assert("materials-only dashboard loads", sales.status === 200);
assert("maintenance-only dashboard loads", maintenance.status === 200);

assert("AI create is disabled for owner", /command-center__ai-task-button[\s\S]{0,80}disabled|disabled[\s\S]{0,80}command-center__ai-task-button/.test(owner.html));
assert("AI create reason is visible", owner.html.includes("AI görev oluşturma akışı henüz kullanıma hazır değil."));
assert("dashboard does not link missing AI create route", !owner.html.includes("/ai-gorevleri/yeni"));
assert("Teams is a real navigation control", /command-center__teams-button[\s\S]{0,80}href="\/teams"|href="\/teams"[\s\S]{0,80}command-center__teams-button/.test(owner.html));

const teams = await fetchWeb("/teams", ownerJar);
assert("Teams destination is HTTP 200", teams.status === 200);

assert("owner sees production quick action", hasQuickAction(owner.html, "/uretim"));
assert("owner sees planning quick action", hasQuickAction(owner.html, "/planlama"));
assert("owner sees maintenance quick action", hasQuickAction(owner.html, "/bakim"));
assert("owner sees quality quick action", hasQuickAction(owner.html, "/kalite"));
assert("owner sees ERP quick action", hasQuickAction(owner.html, "/erp-entegrasyonlari/senkronizasyonlar"));
assert("owner sees AI Task Center quick action", hasQuickAction(owner.html, "/"));

for (const href of ["/uretim", "/planlama", "/bakim", "/kalite", "/erp-entegrasyonlari/senkronizasyonlar", "/", "/teams"]) {
  const destination = await fetchWeb(href, ownerJar);
  assert(`owner active destination ${href} is HTTP 200`, destination.status === 200);
}

assertNoUnauthorizedOperationalActions("machine-responsible", machine.html, []);
assertNoUnauthorizedOperationalActions("materials-only", sales.html, []);
assertNoUnauthorizedOperationalActions("maintenance-only", maintenance.html, ["/bakim"]);

assert("machine-responsible does not see Bakım Talebi", !hasQuickAction(machine.html, "/bakim"));
assert("materials-only does not see Bakım Talebi", !hasQuickAction(sales.html, "/bakim"));
assert("maintenance-only sees Bakım Talebi", hasQuickAction(maintenance.html, "/bakim"));
assert("maintenance-only Bakım destination is HTTP 200", (await fetchWeb("/bakim", maintenanceJar)).status === 200);

assert("owner production footer is active", hasActiveFooterLink(owner.html, "/uretim", "Tüm üretim emirlerini görüntüle"));
assert("owner maintenance footer is active", hasActiveFooterLink(owner.html, "/bakim", "Bakım planını görüntüle"));
assert("owner planning footer is active", hasActiveFooterLink(owner.html, "/planlama", "Tahmini tamamlanma"));
assert(
  "restricted personas do not get an active production footer",
  !hasActiveFooterLink(machine.html, "/uretim", "Tüm üretim emirlerini görüntüle")
    && !hasActiveFooterLink(sales.html, "/uretim", "Tüm üretim emirlerini görüntüle"),
);
assert(
  "restricted personas do not get an active maintenance footer",
  !hasActiveFooterLink(machine.html, "/bakim", "Bakım planını görüntüle")
    && !hasActiveFooterLink(sales.html, "/bakim", "Bakım planını görüntüle"),
);
assert(
  "restricted personas do not see production footer markup",
  !machine.html.includes("Tüm üretim emirlerini görüntüle")
    && !sales.html.includes("Tüm üretim emirlerini görüntüle"),
);
assert(
  "invalid dashboard AI references are not linked",
  !owner.html.includes("/ai-gorevleri/task-material-review")
    && !owner.html.includes("/ai-gorevleri/task-plan-adjustment")
    && !owner.html.includes("/ai-gorevleri/task-maintenance-review")
    && !owner.html.includes("/ai-gorevleri/task-delivery-risk"),
);
assert("AI footer is a real Task Center link", /Tüm AI görevlerini görüntüle/.test(owner.html) && /<a[^>]*href="\/"/.test(owner.html));

const invalidTask = await fetchWeb("/ai-gorevleri/task-material-review", ownerJar);
assert("invalid AI reference is not advertised as an active dashboard control", !owner.html.includes("/ai-gorevleri/task-material-review"));
void invalidTask;

assert("no fake success copy on dashboard", !/görev oluşturuldu|işlem başarıyla/i.test(owner.html));

console.log("DASHBOARD action permission verification passed.");
