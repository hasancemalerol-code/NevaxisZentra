#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const root = join(dirname(fileURLToPath(import.meta.url)), "..");
const API = process.env.CREVAFACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const WEB = process.env.CREVAFACTOS_WEB_BASE_URL ?? "http://127.0.0.1:3001";
const ORGANIZATION = "dev-pilot-tenant";

function fail(message) {
  console.error(`APP-ACTION-1B shell verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function requireEnv(name) {
  const value = process.env[name];
  if (!value) fail(`Missing required environment variable ${name}`);
  return value;
}

function read(relativePath) {
  return readFileSync(join(root, relativePath), "utf8");
}

function generateTotp(secret) {
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

  clone() {
    const jar = new CookieJar();
    for (const [name, value] of this.cookies.entries()) {
      jar.cookies.set(name, value);
    }
    return jar;
  }
}

async function api(method, path, body, jar) {
  const headers = { Accept: "application/json" };
  if (body) headers["Content-Type"] = "application/json";
  if (jar?.header()) headers.Cookie = jar.header();
  if (jar?.cookies.get("crevafactos.session") && jar.cookies.get("crevafactos.csrf") && method !== "GET") {
    headers["X-CrevaFactOS-Csrf"] = jar.cookies.get("crevafactos.csrf");
  }
  const response = await fetch(`${API}${path}`, {
    method,
    headers,
    body: body ? JSON.stringify(body) : undefined,
  });
  jar?.ingest(response);
  return { status: response.status };
}

async function fetchWeb(path, jar, method = "GET") {
  const response = await fetch(`${WEB}${path}`, {
    method,
    headers: { Cookie: jar.header(), Accept: "text/html" },
    redirect: "manual",
  });
  const html = response.status >= 300 && response.status < 400 ? "" : await response.text();
  return { status: response.status, html };
}

async function signInOwner() {
  const jar = new CookieJar();
  const signInResult = await api("POST", "/api/identity/sign-in", {
    organizationContext: ORGANIZATION,
    email: "tenant-owner@dev.local",
    password: requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword"),
    deviceLabel: "app-action-1b-shell",
  }, jar);
  if (signInResult.status !== 200) fail("owner sign-in failed");
  const verify = await api("POST", "/api/identity/mfa/verify", {
    totpCode: generateTotp(requireEnv("DevelopmentSaasIdentity__MfaSharedSecret")),
  }, jar);
  if (verify.status !== 200) fail("owner MFA failed");
  return jar;
}

const topBar = read("src/components/app-shell/top-bar.tsx");
const workspacePanel = read("src/components/app-shell/workspace-context-panel.tsx");
const searchPalette = read("src/components/app-shell/global-search-palette.tsx");
const notifications = read("src/components/app-shell/notifications-control.tsx");
const accountMenu = read("src/features/saas-identity/components/account-menu.tsx");

assert("workspace panel states switching unavailable", workspacePanel.includes("değiştirme henüz kullanıma hazır değil"));
assert("search placeholder is honest", topBar.includes("Modül veya sayfa ara"));
assert("search palette filters by permission-fed items", searchPalette.includes("Yalnızca erişebildiğiniz"));
assert("notifications link to bildirim merkezi", notifications.includes('/teams/bildirimler'));
assert("notifications no longer disabled stub", !notifications.includes("Bildirim merkezi henüz kullanıma hazır değil."));
assert("account menu has settings and sign-out", accountMenu.includes("Ayarlar") && accountMenu.includes("signOutSaasIdentityBrowser"));

const owner = await signInOwner();
const home = await fetchWeb("/", owner);
assert("authenticated shell home is 200", home.status === 200);
assert("workspace selector is present", home.html.includes("workspace-selector"));
assert("search control is present", home.html.includes("Modül veya sayfa ara"));
assert("notifications link is present", home.html.includes("/teams/bildirimler"));
assert("account menu trigger is present", home.html.includes("account-placeholder"));
assert("no future-work shell aria labels remain", !/işlev ileriki pakette eklenecek/.test(home.html));

const signOut = await api("POST", "/api/identity/sign-out", undefined, owner);
assert("sign-out succeeds", signOut.status === 200 || signOut.status === 204);
assert("session cookie cleared", !owner.cookies.has("crevafactos.session") || owner.cookies.get("crevafactos.session") === "");

const revokedJar = owner.clone();
const revoked = await api("GET", "/api/admin-access/me/effective-access", undefined, revokedJar);
assert("revoked session reuse returns 401", revoked.status === 401);

const repeatSignOut = await api("POST", "/api/identity/sign-out", undefined, owner);
assert("repeat sign-out is safe", repeatSignOut.status === 200 || repeatSignOut.status === 204 || repeatSignOut.status === 401);

const mfaExit = read("src/app/(auth)/mfa-dogrula/cikis/route.ts");
assert("pending-MFA exit route preserved", mfaExit.includes("POST") && mfaExit.includes("/giris"));

console.log("APP-ACTION-1B shell verification passed.");
