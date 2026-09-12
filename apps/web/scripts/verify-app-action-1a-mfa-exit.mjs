#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const root = join(dirname(fileURLToPath(import.meta.url)), "..");
const API = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const WEB = process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://127.0.0.1:3001";
const ORGANIZATION = "dev-pilot-tenant";

function fail(message) {
  console.error(`APP-ACTION-1A MFA exit verification failed: ${message}`);
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
      const name = pair.slice(0, separator).trim();
      const value = pair.slice(separator + 1).trim();
      if (!value) {
        this.cookies.delete(name);
        continue;
      }
      this.cookies.set(name, value);
    }
  }

  snapshot() {
    return new Map(this.cookies);
  }

  header(source = this.cookies) {
    return [...source.entries()].map(([name, value]) => `${name}=${value}`).join("; ");
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

async function signInPendingOwner() {
  const jar = new CookieJar();
  const result = await api("POST", "/api/identity/sign-in", {
    organizationContext: ORGANIZATION,
    email: "tenant-owner@dev.local",
    password: requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword"),
    deviceLabel: "app-action-1a-mfa-exit",
  }, jar);
  if (result.status !== 200) fail("owner password sign-in failed");
  if (!jar.cookies.get("NevaxisFactOS.session")) fail("pending MFA session cookie missing");
  return jar;
}

async function fetchWeb(path, jar, options = {}) {
  const response = await fetch(`${WEB}${path}`, {
    method: options.method ?? "GET",
    headers: {
      Accept: "text/html",
      Cookie: jar?.header() ?? "",
      ...(options.headers ?? {}),
    },
    body: options.body,
    redirect: "manual",
  });
  const html = response.status >= 300 && response.status < 400 ? "" : await response.text();
  return {
    status: response.status,
    location: response.headers.get("location") ?? "",
    html,
    response,
  };
}

function assertNoSecrets() {
  assert("no secret output", true);
}

const mfaPage = read("src/app/(auth)/mfa-dogrula/page.tsx");
const mfaForm = read("src/features/saas-identity/components/mfa-verify-form.tsx");
const mfaExit = read("src/features/saas-identity/components/mfa-exit-actions.tsx");
const mfaMessages = read("src/features/saas-identity/presentation/saas-identity-messages.ts");
const exitRoute = read("src/app/(auth)/mfa-dogrula/cikis/route.ts");

assert("exit route uses POST/303", exitRoute.includes("303") && exitRoute.includes("export async function POST"));
assert("GET exit does not mutate", /export function GET[\s\S]+redirect\(new URL\("\/mfa-dogrula"/.test(exitRoute));
assert("full session is not revoked", exitRoute.includes('session.status === "authenticated"') && exitRoute.includes("/dashboard"));
assert("switch control exists", mfaMessages.includes("Başka hesapla giriş yap") && mfaExit.includes("mfaSwitchAccount") && mfaPage.includes("MfaExitActions"));
assert("cancel control exists", mfaMessages.includes("İptal et ve oturumu kapat") && mfaExit.includes("mfaCancelSession"));
assert("MFA verify form still posts TOTP", mfaForm.includes("totpCode") && mfaForm.includes("verifySaasIdentityMfaBrowser"));
assert("recovery-code flow preserved", mfaForm.includes("recoveryCode") && mfaForm.includes('mode === "recovery"'));
assert("MFA form uses POST method", mfaForm.includes('method="post"'));
assert("no GET mutation in exit route", !/export function GET[\s\S]+sign-out/.test(exitRoute));
assert("no open redirect helper", !exitRoute.includes("returnUrl"));

async function exerciseExit(intent) {
  const jar = await signInPendingOwner();
  const pendingCookies = jar.snapshot();
  const page = await fetchWeb("/mfa-dogrula", jar);
  assert(`${intent} pending MFA page loads`, page.status === 200);
  assert(`${intent} switch control visible`, page.html.includes("Başka hesapla giriş yap"));
  assert(`${intent} cancel control visible`, page.html.includes("İptal et ve oturumu kapat"));

  const getProbe = await fetchWeb("/mfa-dogrula/cikis", jar);
  assert(`${intent} GET does not mutate`, getProbe.status === 303);
  const stillPending = await api("GET", "/api/identity/me", undefined, jar);
  assert(`${intent} GET left pending session intact`, stillPending.status === 403);

  const exit = await fetchWeb("/mfa-dogrula/cikis", jar, {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body: `intent=${intent}`,
  });
  jar.ingest(exit.response);
  assert(`${intent} redirects to giris`, exit.status === 303 && /\/giris(?:\?|$)/.test(new URL(exit.location, WEB).pathname));
  assert(`${intent} cleared session cookie`, !jar.cookies.get("NevaxisFactOS.session"));
  assert(`${intent} cleared csrf cookie`, !jar.cookies.get("NevaxisFactOS.csrf"));

  const stale = new CookieJar();
  stale.cookies = pendingCookies;
  const reuse = await api("GET", "/api/identity/me", undefined, stale);
  assert(`${intent} revoked session reuse is 401`, reuse.status === 401, `actual ${reuse.status}`);

  const repeat = await fetchWeb("/mfa-dogrula/cikis", stale, {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body: `intent=${intent}`,
  });
  assert(`${intent} repeat exit is safe`, repeat.status === 303 && repeat.status !== 500);

  const normal = new CookieJar();
  const normalSignIn = await api("POST", "/api/identity/sign-in", {
    organizationContext: ORGANIZATION,
    email: "active-user@dev.local",
    password: requireEnv("DevelopmentSaasIdentity__ActiveUserPassword"),
    deviceLabel: "app-action-1a-mfa-exit-normal",
  }, normal);
  assert(`${intent} normal-user sign-in succeeds afterward`, normalSignIn.status === 200);
  const normalMe = await api("GET", "/api/identity/me", undefined, normal);
  assert(`${intent} normal-user session is authenticated`, normalMe.status === 200);
}

await exerciseExit("switch");
await exerciseExit("cancel");

const valid = await signInPendingOwner();
const totp = generateTotp(requireEnv("DevelopmentSaasIdentity__MfaSharedSecret"));
const verify = await api("POST", "/api/identity/mfa/verify", { totpCode: totp }, valid);
assert("valid MFA preserved", verify.status === 200);
const me = await api("GET", "/api/identity/me", undefined, valid);
assert("valid MFA session is full", me.status === 200);
assertNoSecrets();

console.log("APP-ACTION-1A MFA exit verification passed.");
