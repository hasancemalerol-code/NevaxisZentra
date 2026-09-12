#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const WEB = process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://127.0.0.1:3001";
const ORGANIZATION = "dev-pilot-tenant";
const MARKER_ORG = "audit-org-marker";
const MARKER_EMAIL = "audit-email-marker@example.invalid";
const MARKER_PASSWORD = "audit-password-marker";

function fail(message) {
  console.error(`APP-ACTION sign-in disclosure verification failed: ${message}`);
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

function containsAny(value, parts) {
  const haystack = String(value ?? "").toLowerCase();
  return parts.some((part) => haystack.includes(String(part).toLowerCase()));
}

function assertNoSecrets(label, value) {
  const secrets = [
    MARKER_ORG,
    MARKER_EMAIL,
    MARKER_PASSWORD,
    process.env.DevelopmentSaasIdentity__ActiveUserPassword,
    process.env.DevelopmentSaasIdentity__TenantOwnerPassword,
    "admin@demo.nevaxisfactos.local",
    "machine-responsible@dev.local",
  ].filter(Boolean);
  if (containsAny(value, secrets)) {
    fail(`${label} leaked a credential-bearing value`);
  }
}

const formSource = readFileSync(
  resolve(ROOT, "apps/web/src/features/saas-identity/components/sign-in-form.tsx"),
  "utf8",
);
assert("sign-in form method is post", /method=["']post["']/.test(formSource));
assert("sign-in form does not use GET", !/method=["']get["']/i.test(formSource));
assert("sign-in form posts to same-origin fallback", /action=["']\/giris\/oturum["']/.test(formSource));

const getFallback = await fetch(`${WEB}/giris/oturum`, { method: "GET", redirect: "manual" });
assert("GET /giris/oturum does not accept credentials", getFallback.status === 303);
assertNoSecrets("GET /giris/oturum location", getFallback.headers.get("location"));

const nativeGet = await fetch(
  `${WEB}/giris?organizationContext=${encodeURIComponent(MARKER_ORG)}&email=${encodeURIComponent(MARKER_EMAIL)}&password=${encodeURIComponent(MARKER_PASSWORD)}`,
  { method: "GET", redirect: "manual" },
);
assert("sensitive query is stripped from /giris", nativeGet.status === 307 || nativeGet.status === 303);
const strippedLocation = nativeGet.headers.get("location") ?? "";
assert("stripped redirect keeps /giris", strippedLocation.includes("/giris"));
assertNoSecrets("stripped /giris location", strippedLocation);

async function postSignIn(fields) {
  const body = new URLSearchParams(fields);
  const response = await fetch(`${WEB}/giris/oturum`, {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body,
    redirect: "manual",
  });
  const location = response.headers.get("location") ?? "";
  assertNoSecrets("POST /giris/oturum location", location);
  assertNoSecrets("POST /giris/oturum url", response.url);
  return { status: response.status, location };
}

const invalidUnknown = await postSignIn({
  organizationContext: ORGANIZATION,
  email: MARKER_EMAIL,
  password: MARKER_PASSWORD,
});
const invalidKnown = await postSignIn({
  organizationContext: ORGANIZATION,
  email: "machine-responsible@dev.local",
  password: MARKER_PASSWORD,
});
assert("invalid unknown account uses POST redirect", invalidUnknown.status === 303);
assert("invalid known account uses POST redirect", invalidKnown.status === 303);
assert(
  "invalid login does not enumerate accounts",
  invalidUnknown.location === invalidKnown.location,
);
assert("invalid login stays on sign-in", invalidUnknown.location.includes("/giris"));
assert("invalid login uses generic auth flag", invalidUnknown.location.includes("auth=denied"));

const activePassword = requireEnv("DevelopmentSaasIdentity__ActiveUserPassword");
const normal = await postSignIn({
  organizationContext: ORGANIZATION,
  email: "machine-responsible@dev.local",
  password: activePassword,
});
assert("normal-user no-JS POST succeeds", normal.status === 303);
assert("normal-user lands on an app path", /\/(dashboard|mfa-dogrula)/.test(normal.location));

const owner = await postSignIn({
  organizationContext: ORGANIZATION,
  email: "admin@demo.nevaxisfactos.local",
  password: requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword"),
});
assert("admin password step reaches MFA", owner.status === 303 && owner.location.includes("/mfa-dogrula"));

console.log("APP-ACTION sign-in disclosure verification passed.");
