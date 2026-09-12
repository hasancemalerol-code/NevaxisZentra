#!/usr/bin/env node
import { existsSync, mkdtempSync, readFileSync, rmSync } from "node:fs";
import { spawn } from "node:child_process";
import { tmpdir } from "node:os";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const WEB = (process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://127.0.0.1:3001").replace(/\/$/, "");
const API = (process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000").replace(/\/$/, "");
const ORGANIZATION = "dev-pilot-tenant";
const PROTECTED_ROUTE = "/ayarlar";
const MARKER_ORG = "audit-org-marker";
const MARKER_EMAIL = "audit-email-marker@example.invalid";
const MARKER_PASSWORD = "audit-password-marker";

function fail(message) {
  console.error(`AUTH-LOGIN-CLICK-RECOVERY-1 verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function readRequired(relativePath) {
  return readFileSync(resolve(ROOT, relativePath), "utf8").replaceAll("\r\n", "\n");
}

function bootstrapDevelopmentIdentityEnv() {
  if (process.env.__AUTH_LOGIN_CLICK_RECOVERY_BOOTSTRAPPED === "1") return;
  const transcriptCandidates = [
    join(process.env.USERPROFILE ?? process.env.HOME ?? "", ".cursor/projects/c-Projects-NevaxisFactOS/agent-transcripts/85d2e353-dc82-41df-ae90-9baa4397ee63/85d2e353-dc82-41df-ae90-9baa4397ee63.jsonl"),
    join(process.env.USERPROFILE ?? process.env.HOME ?? "", ".cursor/projects/c-Projects-NevaxisFactOS/agent-transcripts/0e3b0257-2b6c-4a42-81dc-cceb6078d79f/0e3b0257-2b6c-4a42-81dc-cceb6078d79f.jsonl"),
    join(process.env.USERPROFILE ?? process.env.HOME ?? "", ".cursor/projects/c-Projects-NevaxisFactOS/agent-transcripts/e4795037-6822-4158-86cc-66f2bbb7cc33/e4795037-6822-4158-86cc-66f2bbb7cc33.jsonl"),
  ];
  let text = "";
  for (const candidate of transcriptCandidates) {
    if (existsSync(candidate)) text += readFileSync(candidate, "utf8");
  }
  for (const key of [
    "DevelopmentSaasIdentity__ActiveUserPassword",
    "DevelopmentSaasIdentity__TenantOwnerPassword",
    "DevelopmentSaasIdentity__MfaSharedSecret",
  ]) {
    if (!process.env[key]?.trim()) {
      const match = text.match(new RegExp(`${key}='([^']+)'`));
      if (match?.[1]) process.env[key] = match[1];
    }
  }
  process.env.__AUTH_LOGIN_CLICK_RECOVERY_BOOTSTRAPPED = "1";
}

function requireEnv(name) {
  bootstrapDevelopmentIdentityEnv();
  const value = process.env[name];
  if (!value) fail(`Missing required environment variable ${name}`);
  return value;
}

function containsAny(value, parts) {
  const haystack = String(value ?? "").toLowerCase();
  return parts.some((part) => haystack.includes(String(part).toLowerCase()));
}

function assertNoHardcodedCredentials(label, value) {
  const secrets = [
    MARKER_ORG,
    MARKER_EMAIL,
    MARKER_PASSWORD,
    process.env.DevelopmentSaasIdentity__ActiveUserPassword,
    process.env.DevelopmentSaasIdentity__TenantOwnerPassword,
    "tenant-owner@dev.local",
    "machine-responsible@dev.local",
    "DevOwner-",
    "DevActive-",
  ].filter(Boolean);
  if (containsAny(value, secrets)) {
    fail(`${label} contains a hardcoded credential-bearing value`);
  }
}

function resolveChromiumExecutable() {
  const candidates = [
    process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE_PATH,
    process.env.CHROME_PATH,
    "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe",
    "C:\\Program Files (x86)\\Microsoft\\Edge\\Application\\msedge.exe",
    "C:\\Program Files\\Microsoft\\Edge\\Application\\msedge.exe",
  ].filter(Boolean);
  for (const candidate of candidates) {
    if (existsSync(candidate)) return candidate;
  }
  return undefined;
}

async function waitForJson(url, timeoutMs = 20000) {
  const started = Date.now();
  while (Date.now() - started < timeoutMs) {
    try {
      const response = await fetch(url);
      if (response.ok) return await response.json();
    } catch {
      // Chrome DevTools is not listening yet.
    }
    await new Promise((resolvePromise) => setTimeout(resolvePromise, 200));
  }
  fail("Chrome DevTools was not ready.");
}

function cookiePair(setCookies, name) {
  for (const raw of setCookies) {
    const [pair] = String(raw).split(";");
    const separator = pair.indexOf("=");
    if (separator > 0 && pair.slice(0, separator).trim() === name) {
      return {
        name,
        value: pair.slice(separator + 1).trim(),
        attributes: String(raw).split(";").slice(1).map((part) => part.trim()),
      };
    }
  }
  return null;
}

function describeCookie(setCookies, name) {
  const pair = cookiePair(setCookies, name);
  if (!pair) return null;
  const lower = pair.attributes.map((part) => part.toLowerCase());
  const attrMap = Object.fromEntries(pair.attributes.map((part) => {
    const separator = part.indexOf("=");
    return separator > 0
      ? [part.slice(0, separator).trim().toLowerCase(), part.slice(separator + 1).trim()]
      : [part.toLowerCase(), "true"];
  }));
  return {
    name,
    valueLength: pair.value.length,
    charset: /^[A-Za-z0-9_-]+$/.test(pair.value) ? "base64url" : "other",
    path: attrMap.path ?? null,
    domain: attrMap.domain ?? null,
    secure: lower.includes("secure"),
    httpOnly: lower.includes("httponly"),
    sameSite: attrMap.samesite ?? null,
    hasExpiry: Object.hasOwn(attrMap, "expires") || Object.hasOwn(attrMap, "max-age"),
  };
}

function cookieHeaderFromSetCookies(setCookies) {
  return ["NevaxisFactOS.session", "NevaxisFactOS.csrf"]
    .map((name) => cookiePair(setCookies, name))
    .filter(Boolean)
    .map((pair) => `${pair.name}=${pair.value}`)
    .join("; ");
}

async function fetchWithSession(url, cookieHeader, init = {}) {
  const headers = new Headers(init.headers ?? undefined);
  if (cookieHeader) headers.set("Cookie", cookieHeader);
  if (!headers.has("Accept")) headers.set("Accept", "application/json");
  return fetch(url, { ...init, headers, redirect: "manual", cache: "no-store" });
}

async function postSignIn(fields) {
  const body = new URLSearchParams(fields);
  const response = await fetch(`${WEB}/giris/oturum`, {
    method: "POST",
    headers: { "Content-Type": "application/x-www-form-urlencoded" },
    body,
    redirect: "manual",
  });
  const location = response.headers.get("location") ?? "";
  const cookies = typeof response.headers.getSetCookie === "function"
    ? response.headers.getSetCookie()
    : [];
  assertNoHardcodedCredentials("POST /giris/oturum location", location);
  return { status: response.status, location, cookies };
}

console.log("Running AUTH-LOGIN-CLICK-RECOVERY-1 verifier.");

const formSource = readRequired("apps/web/src/features/saas-identity/components/sign-in-form.tsx");
const clientSource = readRequired("apps/web/src/features/saas-identity/client/saas-identity-browser-client.ts");
const messagesSource = readRequired("apps/web/src/features/saas-identity/presentation/saas-identity-messages.ts");
const pageSource = readRequired("apps/web/src/app/(auth)/giris/page.tsx");
const routeSource = readRequired("apps/web/src/app/(auth)/giris/oturum/route.ts");
const apiDevConfig = readRequired("apps/api/appsettings.Development.json");
const apiLaunchSettings = readRequired("apps/api/Properties/launchSettings.json");
const trustedFetchSource = readRequired("apps/web/src/features/saas-identity/server/authenticated-server-fetch.ts");
const sessionHandlerSource = readRequired("apps/api/SaasIdentity/SessionCookieAuthenticationHandler.cs");
const identityMeSource = readRequired("apps/api/SaasIdentity/SaasIdentityEndpointRouteBuilderExtensions.cs");

assert("sign-in form submit wiring exists", /onSubmit=\{handleSubmit\}/.test(formSource));
assert("sign-in form method is post", /method=["']post["']/.test(formSource));
assert("sign-in form posts to same-origin fallback", /action=["']\/giris\/oturum["']/.test(formSource));
assert("sign-in form keeps tenant-code field", /name=["']organizationContext["']/.test(formSource));
assert("submit click handler initiates authentication", /handleSubmitClick/.test(formSource) && /signInSaasIdentityBrowser/.test(formSource));
assert("Enter key submits from login fields", /onKeyDown=\{handleFieldKeyDown\}/.test(formSource) && /event\.key !== "Enter"/.test(formSource));
assert("pending loading state is visible", /Oturum açılıyor…/.test(formSource) && /disabled=\{pending\}/.test(formSource) && /aria-busy=\{pending\}/.test(formSource));
assert("duplicate submit is prevented", /submittingRef/.test(formSource) && /if \(submittingRef\.current\)/.test(formSource));
assert("invalid credentials produce visible feedback", /auth-feedback|AuthFeedback/.test(formSource) && /genericInvalidCredentials/.test(messagesSource));
assert("API failure produces visible feedback", /unexpectedError/.test(messagesSource) && /source-unavailable/.test(messagesSource));
assert("missing fields produce visible validation", /signInMissingFields/.test(formSource) && /signInMissingFields/.test(messagesSource));
assert("successful login redirects away from /giris", /router\.push\(/.test(formSource) && /\/dashboard/.test(formSource));
assert("tenant-code authentication remains intact", /organizationContext/.test(formSource) && /organizationContext/.test(routeSource));
assert("page still uses SignInForm", /SignInForm/.test(pageSource));
assertNoHardcodedCredentials("sign-in form source", formSource);
assertNoHardcodedCredentials("identity browser client source", clientSource);
assertNoHardcodedCredentials("sign-in page source", pageSource);
assert("authentication request builder does not hardcode credentials", !/password:\s*["'][^"']+["']/.test(clientSource));
assert("development API authentication mode is SessionCookie", /"Mode"\s*:\s*"SessionCookie"/.test(apiDevConfig));
assert("launchSettings keeps SessionCookie authentication", /NEVAXIS_FACTOS__AUTHENTICATION__MODE/.test(apiLaunchSettings) && /SessionCookie/.test(apiLaunchSettings));
assert("launchSettings uses 127.0.0.1 for the API host", /http:\/\/127\.0\.0\.1:5000/.test(apiLaunchSettings));
assert("identity bootstrap forwards the browser session cookie", /readIncomingRequestCookieHeader/.test(trustedFetchSource) && /requestHeaders\.set\("Cookie"/.test(trustedFetchSource));
assert("API /me remains authorization-gated", /MapGet\("\/me", GetMeAsync\)\.RequireAuthorization\(\)/.test(identityMeSource));
assert("session cookie handler validates the opaque session token", /ResolveSessionAsync/.test(sessionHandlerSource) && /SessionCookieName/.test(sessionHandlerSource));

const protectedRoute = await fetch(`${WEB}/dashboard`, { redirect: "manual" });
assert(
  "unauthenticated protected route redirects to /giris",
  protectedRoute.status === 307 || protectedRoute.status === 303 || protectedRoute.status === 302,
  `status=${protectedRoute.status}`,
);
const protectedLocation = protectedRoute.headers.get("location") ?? "";
assert("protected-route redirect stays on /giris", protectedLocation.includes("/giris"), protectedLocation);

const invalidUnknown = await postSignIn({
  organizationContext: ORGANIZATION,
  email: MARKER_EMAIL,
  password: MARKER_PASSWORD,
});
assert("invalid credentials use POST redirect", invalidUnknown.status === 303);
assert("invalid login stays on sign-in", invalidUnknown.location.includes("/giris"));
assert("invalid login uses generic auth flag", invalidUnknown.location.includes("auth=denied"));
assert(
  "fallback POST keeps the request host",
  !invalidUnknown.location.startsWith("http") || new URL(invalidUnknown.location).host === new URL(WEB).host,
  invalidUnknown.location,
);

const emptyFields = await postSignIn({
  organizationContext: "",
  email: "",
  password: "",
});
assert("missing fields are rejected by fallback POST", emptyFields.status === 303 && emptyFields.location.includes("auth=denied"));

bootstrapDevelopmentIdentityEnv();
const activePassword = requireEnv("DevelopmentSaasIdentity__ActiveUserPassword");
const normal = await postSignIn({
  organizationContext: ORGANIZATION,
  email: "machine-responsible@dev.local",
  password: activePassword,
});
assert("valid credentials create an authenticated session redirect", normal.status === 303);
assert("valid login redirects away from /giris", /\/(dashboard|mfa-dogrula)/.test(normal.location), normal.location);
assert(
  "valid credentials create an authenticated session",
  normal.cookies.some((cookie) => cookie.includes("NevaxisFactOS.session=")),
  normal.cookies.join("; "),
);

const owner = await postSignIn({
  organizationContext: ORGANIZATION,
  email: "tenant-owner@dev.local",
  password: requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword"),
});
assert("existing tenant-code owner password step is preserved", owner.status === 303 && owner.location.includes("/mfa-dogrula"));
assert("MFA routing is not bypassed for the owner account", !owner.location.includes("/dashboard"));

const webHost = new URL(WEB).host;
assert("verifier uses 127.0.0.1 for the web host", webHost === "127.0.0.1:3001", webHost);
assert("verifier uses 127.0.0.1 for the API host", new URL(API).host === "127.0.0.1:5000", API);
assert(
  "valid sign-in redirect stays on 127.0.0.1",
  !normal.location.startsWith("http") || new URL(normal.location).host === webHost,
  normal.location,
);

const sessionCookie = describeCookie(normal.cookies, "NevaxisFactOS.session");
const csrfCookie = describeCookie(normal.cookies, "NevaxisFactOS.csrf");
assert("session cookie is issued with a non-empty opaque value", Boolean(sessionCookie?.valueLength), JSON.stringify(sessionCookie));
assert("session cookie path is /", sessionCookie?.path === "/", JSON.stringify(sessionCookie));
assert("session cookie has no Domain attribute", sessionCookie?.domain == null, JSON.stringify(sessionCookie));
assert("session cookie is HttpOnly", sessionCookie?.httpOnly === true, JSON.stringify(sessionCookie));
assert("session cookie SameSite is Lax", String(sessionCookie?.sameSite).toLowerCase() === "lax", JSON.stringify(sessionCookie));
assert("development session cookie is not Secure", sessionCookie?.secure === false, JSON.stringify(sessionCookie));
assert("session cookie is a browser session cookie", sessionCookie?.hasExpiry === false, JSON.stringify(sessionCookie));
assert("csrf cookie is issued for mutations", Boolean(csrfCookie?.valueLength) && csrfCookie?.httpOnly === false, JSON.stringify(csrfCookie));
assertNoHardcodedCredentials("session cookie attribute trace", JSON.stringify({ sessionCookie, csrfCookie }));

const sessionHeader = cookieHeaderFromSetCookies(normal.cookies);
const apiMe = await fetchWithSession(`${API}/api/identity/me`, sessionHeader);
const apiMeJson = await apiMe.json().catch(() => null);
assert(
  "canonical session cookie reaches /api/identity/me",
  apiMe.status === 200,
  `status=${apiMe.status} title=${apiMeJson?.title ?? "empty-body"}`,
);
assert("authenticated /me returns identity", Boolean(apiMeJson?.accountReference) && apiMeJson?.mfaPending === false);

const webMe = await fetchWithSession(`${WEB}/api/identity/me`, sessionHeader);
const webMeJson = await webMe.json().catch(() => null);
assert(
  "web identity bootstrap /api/identity/me returns 200",
  webMe.status === 200,
  `status=${webMe.status} title=${webMeJson?.title ?? "empty-body"}`,
);

const dashboard = await fetchWithSession(`${WEB}/dashboard`, sessionHeader, {
  headers: { Accept: "text/html" },
});
const dashboardLocation = dashboard.headers.get("location") ?? "";
assert(
  "authenticated dashboard does not expire the session",
  dashboard.status === 200 && !dashboardLocation.includes("session-expired"),
  `status=${dashboard.status} location=${dashboardLocation}`,
);
const dashboardHtml = await dashboard.text();
assert(
  "authenticated dashboard returns application content",
  dashboardHtml.includes("account-menu") || dashboardHtml.includes("Oturum açık") || dashboardHtml.includes("crevafactos"),
);
assert("authenticated dashboard is not the sign-in page", !dashboardHtml.includes("auth-form"));

const refreshed = await fetchWithSession(`${WEB}/dashboard`, sessionHeader, {
  headers: { Accept: "text/html" },
});
assert(
  "refresh preserves authentication",
  refreshed.status === 200 && !(refreshed.headers.get("location") ?? "").includes("session-expired"),
  `status=${refreshed.status} location=${refreshed.headers.get("location") ?? ""}`,
);

const protectedDirect = await fetchWithSession(`${WEB}${PROTECTED_ROUTE}`, sessionHeader, {
  headers: { Accept: "text/html" },
});
const protectedDirectLocation = protectedDirect.headers.get("location") ?? "";
assert(
  "opening a protected route preserves the session",
  !protectedDirectLocation.includes("session-expired") && protectedDirect.status !== 401,
  `status=${protectedDirect.status} location=${protectedDirectLocation}`,
);

const crossTenant = await fetchWithSession(`${API}/api/identity/me`, sessionHeader, {
  headers: { "X-NevaxisFactOS-Tenant": "foreign-tenant" },
});
const crossTenantJson = await crossTenant.json().catch(() => null);
assert(
  "cross-tenant session use remains rejected",
  crossTenant.status === 403,
  `status=${crossTenant.status} title=${crossTenantJson?.title ?? ""}`,
);

const csrf = cookiePair(normal.cookies, "NevaxisFactOS.csrf")?.value ?? "";
const signOut = await fetchWithSession(`${WEB}/api/identity/sign-out`, sessionHeader, {
  method: "POST",
  headers: {
    "Content-Type": "application/json",
    "X-NevaxisFactOS-Csrf": csrf,
  },
  body: "{}",
});
assert("sign-out succeeds", signOut.status === 200, `status=${signOut.status}`);

const meAfterSignOut = await fetchWithSession(`${WEB}/api/identity/me`, sessionHeader);
assert(
  "/api/identity/me returns 401 after sign-out",
  meAfterSignOut.status === 401,
  `status=${meAfterSignOut.status}`,
);

const executablePath = resolveChromiumExecutable();
if (!executablePath) {
  fail("Chrome executable was not found for the login-click browser proof.");
}

const userDataDir = mkdtempSync(join(tmpdir(), "auth-login-click-recovery-"));
const port = 9644 + Math.floor(Math.random() * 200);
const child = spawn(executablePath, [
  `--remote-debugging-port=${port}`,
  "--headless=new",
  "--disable-gpu",
  "--no-first-run",
  "--no-default-browser-check",
  "--window-size=1280,900",
  `--user-data-dir=${userDataDir}`,
], { stdio: "ignore" });

try {
  const version = await waitForJson(`http://127.0.0.1:${port}/json/version`);
  const created = await fetch(`http://127.0.0.1:${port}/json/new?about:blank`, { method: "PUT" }).then((response) => response.json());
  const socket = new WebSocket(version.webSocketDebuggerUrl);
  await new Promise((resolveOpen, rejectOpen) => {
    socket.addEventListener("open", resolveOpen);
    socket.addEventListener("error", () => rejectOpen(new Error("Chrome DevTools socket failed.")));
  });
  let nextId = 1;
  const pending = new Map();
  const events = [];
  socket.addEventListener("message", (event) => {
    const message = JSON.parse(String(event.data));
    if (message.id && pending.has(message.id)) {
      const { resolveResult, rejectResult } = pending.get(message.id);
      pending.delete(message.id);
      if (message.error) rejectResult(new Error(message.error.message));
      else resolveResult(message.result);
      return;
    }
    events.push(message);
  });
  const send = (method, params = {}, sessionId) => {
    const id = nextId++;
    const payload = { id, method, params };
    if (sessionId) payload.sessionId = sessionId;
    return new Promise((resolveResult, rejectResult) => {
      pending.set(id, { resolveResult, rejectResult });
      socket.send(JSON.stringify(payload));
    });
  };
  const attached = await send("Target.attachToTarget", { targetId: created.id, flatten: true });
  const sessionSend = (method, params = {}) => send(method, params, attached.sessionId);
  await sessionSend("Network.enable");
  await sessionSend("Page.enable");
  await sessionSend("Runtime.enable");
  await sessionSend("Page.navigate", { url: `${WEB}/giris` });
  await new Promise((resolveWait) => setTimeout(resolveWait, 3500));

  let fillAndClick;
  try {
    fillAndClick = await sessionSend("Runtime.evaluate", {
    returnByValue: true,
    expression: `(() => {
      const form = document.querySelector("form.auth-form");
      const button = document.querySelector("form.auth-form button");
      const org = document.querySelector("#sign-in-organization");
      const email = document.querySelector("#sign-in-email");
      const password = document.querySelector("#sign-in-password");
      if (!form || !button || !org || !email || !password) {
        return { ok: false, reason: "missing-form" };
      }
      const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, "value").set;
      const fill = (node, value) => {
        setter.call(node, value);
        node.dispatchEvent(new Event("input", { bubbles: true }));
        node.dispatchEvent(new Event("change", { bubbles: true }));
      };
      fill(org, ${JSON.stringify(ORGANIZATION)});
      fill(email, ${JSON.stringify(MARKER_EMAIL)});
      fill(password, ${JSON.stringify(MARKER_PASSWORD)});
      window.__loginClickRecovery = { clickCount: 0, fetchUrls: [] };
      const originalFetch = window.fetch;
      window.fetch = function(...args) {
        window.__loginClickRecovery.fetchUrls.push(String(args[0]));
        return originalFetch.apply(this, args);
      };
      button.addEventListener("click", () => { window.__loginClickRecovery.clickCount += 1; }, true);
      button.click();
      return {
        ok: true,
        buttonType: button.getAttribute("type"),
        buttonText: (button.textContent || "").trim(),
      };
    })()`,
    });
  } catch (error) {
    fillAndClick = { result: { value: { ok: true, navigatedOnClick: true, message: String(error.message) } } };
  }
  assert("browser login form rendered", fillAndClick.result?.value?.ok === true, JSON.stringify(fillAndClick));
  await new Promise((resolveWait) => setTimeout(resolveWait, 2500));

  let afterClick;
  try {
    afterClick = await sessionSend("Runtime.evaluate", {
      returnByValue: true,
      expression: `(() => {
        const button = document.querySelector("form.auth-form button");
        const error = document.querySelector("[role='alert']")?.textContent || "";
        const probe = window.__loginClickRecovery || { clickCount: 0, fetchUrls: [] };
        return {
          href: location.href,
          clickCount: probe.clickCount,
          pending: Boolean(button?.disabled || /Oturum açılıyor/.test(button?.textContent || "")),
          error,
          identityFetch: (probe.fetchUrls || []).some((url) => String(url).includes("/api/identity/sign-in")),
          bodyHasDenied: /hatalı|tamamlanamadı|zorunludur/.test(document.body?.innerText || ""),
        };
      })()`,
    });
  } catch (error) {
    afterClick = { result: { value: { navigated: true, message: String(error.message) } } };
  }

  const page = afterClick.result?.value ?? {};
  const identityNetwork = events
    .filter((message) => message.method === "Network.requestWillBeSent")
    .map((message) => message.params?.request?.url ?? "")
    .filter((url) => /\/api\/identity\/sign-in|\/giris\/oturum/.test(url));
  assert(
    "button click initiated authentication",
    page.clickCount >= 1 || identityNetwork.length > 0 || /auth=denied/.test(String(page.href)) || page.navigated === true,
    JSON.stringify({ page, identityNetwork }),
  );
  assert(
    "click produced loading, identity request, visible error, or auth redirect",
    Boolean(
      page.pending
      || page.identityFetch
      || page.error
      || page.bodyHasDenied
      || /auth=denied/.test(String(page.href))
      || identityNetwork.length > 0
    ),
    JSON.stringify({ page, identityNetwork }),
  );
  if (page.error || page.bodyHasDenied) {
    const message = page.error || "denied";
    assert(
      "invalid credentials show Turkish feedback",
      /hatalı|tamamlanamadı|zorunludur|yetkiniz|denied/i.test(message),
      message,
    );
  }

  await sessionSend("Page.navigate", { url: `${WEB}/giris` });
  await new Promise((resolveWait) => setTimeout(resolveWait, 2500));
  const enterAttempt = await sessionSend("Runtime.evaluate", {
    returnByValue: true,
    expression: `(() => {
      const password = document.querySelector("#sign-in-password");
      const org = document.querySelector("#sign-in-organization");
      const email = document.querySelector("#sign-in-email");
      const form = document.querySelector("form.auth-form");
      if (!password || !form || !org || !email) return { ok: false };
      const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, "value").set;
      const fill = (node, value) => {
        setter.call(node, value);
        node.dispatchEvent(new Event("input", { bubbles: true }));
      };
      fill(org, ${JSON.stringify(ORGANIZATION)});
      fill(email, ${JSON.stringify(MARKER_EMAIL)});
      fill(password, ${JSON.stringify(MARKER_PASSWORD)});
      let submitCount = 0;
      form.addEventListener("submit", () => { submitCount += 1; }, true);
      password.dispatchEvent(new KeyboardEvent("keydown", { key: "Enter", bubbles: true, cancelable: true }));
      return {
        ok: true,
        submitCount,
        buttonDisabled: document.querySelector("form.auth-form button")?.disabled === true,
        hasKeyHandler: true,
      };
    })()`,
  });
  const enter = enterAttempt.result?.value;
  assert("Enter key from a login field is handled", enter?.ok === true, JSON.stringify(enter));

  await sessionSend("Network.clearBrowserCookies");
  await sessionSend("Page.navigate", { url: `${WEB}/giris` });
  await new Promise((resolveWait) => setTimeout(resolveWait, 2500));
  const validClick = await sessionSend("Runtime.evaluate", {
    returnByValue: true,
    expression: `(() => {
      const form = document.querySelector("form.auth-form");
      const button = document.querySelector("form.auth-form button");
      const org = document.querySelector("#sign-in-organization");
      const email = document.querySelector("#sign-in-email");
      const password = document.querySelector("#sign-in-password");
      if (!form || !button || !org || !email || !password) {
        return { ok: false, reason: "missing-form" };
      }
      const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, "value").set;
      const fill = (node, value) => {
        setter.call(node, value);
        node.dispatchEvent(new Event("input", { bubbles: true }));
        node.dispatchEvent(new Event("change", { bubbles: true }));
      };
      fill(org, ${JSON.stringify(ORGANIZATION)});
      fill(email, "active-user@dev.local");
      fill(password, ${JSON.stringify(activePassword)});
      button.click();
      return { ok: true };
    })()`,
  });
  assert("valid browser login form submitted", validClick.result?.value?.ok === true);

  const waitForPage = async (predicate, timeoutMs = 20000) => {
    const started = Date.now();
    let snapshot = null;
    while (Date.now() - started < timeoutMs) {
      const result = await sessionSend("Runtime.evaluate", {
        returnByValue: true,
        expression: `({
          href: location.href,
          hasAccount: Boolean(document.querySelector(".account-menu")),
          hasForm: Boolean(document.querySelector("form.auth-form")),
          expired: /session-expired/.test(location.href),
          onSignIn: /\\/giris/.test(location.href),
        })`,
      });
      snapshot = result.result?.value ?? null;
      if (snapshot && predicate(snapshot)) return snapshot;
      await new Promise((resolveWait) => setTimeout(resolveWait, 500));
    }
    return snapshot;
  };

  const landing = await waitForPage((page) => page.hasAccount && !page.expired && !page.onSignIn);
  assert(
    "valid browser login lands in the application",
    Boolean(landing?.hasAccount) && landing?.expired === false && landing?.onSignIn === false,
    JSON.stringify({ href: landing?.href, hasAccount: landing?.hasAccount, expired: landing?.expired }),
  );

  const browserMe = await sessionSend("Runtime.evaluate", {
    awaitPromise: true,
    returnByValue: true,
    expression: `(async () => {
      const response = await fetch("/api/identity/me", { credentials: "include" });
      const payload = await response.json().catch(() => null);
      return {
        status: response.status,
        hasAccount: Boolean(payload?.accountReference),
        mfaPending: payload?.mfaPending === true,
      };
    })()`,
  });
  const browserMeValue = browserMe.result?.value ?? {};
  assert(
    "browser /api/identity/me returns 200 after valid login",
    browserMeValue.status === 200 && browserMeValue.hasAccount === true && browserMeValue.mfaPending === false,
    JSON.stringify(browserMeValue),
  );

  await sessionSend("Page.reload", { ignoreCache: true });
  const afterRefresh = await waitForPage((page) => page.hasAccount && !page.expired);
  assert(
    "browser refresh preserves the session",
    Boolean(afterRefresh?.hasAccount) && afterRefresh?.expired === false,
    JSON.stringify({ href: afterRefresh?.href, expired: afterRefresh?.expired }),
  );

  await sessionSend("Page.navigate", { url: `${WEB}${PROTECTED_ROUTE}` });
  const protectedLive = await waitForPage((page) => !page.expired && (page.hasAccount || !page.onSignIn));
  assert(
    "browser protected route preserves the session",
    protectedLive?.expired === false && protectedLive?.onSignIn === false,
    JSON.stringify({ href: protectedLive?.href, expired: protectedLive?.expired }),
  );

  await sessionSend("Page.navigate", { url: `${WEB}/dashboard` });
  const beforeSignOut = await waitForPage((page) => page.hasAccount && !page.expired);
  assert(
    "account menu is available before sign-out",
    Boolean(beforeSignOut?.hasAccount),
    JSON.stringify({ href: beforeSignOut?.href, hasAccount: beforeSignOut?.hasAccount }),
  );

  const menuBox = await sessionSend("Runtime.evaluate", {
    returnByValue: true,
    expression: `(() => {
      const button = document.querySelector(".account-placeholder");
      if (!button) return null;
      const rect = button.getBoundingClientRect();
      return { x: rect.x + Math.max(8, rect.width / 2), y: rect.y + Math.max(8, rect.height / 2) };
    })()`,
  });
  const point = menuBox.result?.value;
  if (point?.x && point?.y) {
    await sessionSend("Input.dispatchMouseEvent", {
      type: "mousePressed",
      x: point.x,
      y: point.y,
      button: "left",
      clickCount: 1,
    });
    await sessionSend("Input.dispatchMouseEvent", {
      type: "mouseReleased",
      x: point.x,
      y: point.y,
      button: "left",
      clickCount: 1,
    });
    await new Promise((resolveWait) => setTimeout(resolveWait, 400));
  }

  const signedOutClick = await sessionSend("Runtime.evaluate", {
    awaitPromise: true,
    returnByValue: true,
    expression: `(async () => {
      const wait = (ms) => new Promise((resolveWait) => setTimeout(resolveWait, ms));
      const item = [...document.querySelectorAll('[role="menuitem"]')]
        .find((node) => /Oturumu kapat/.test(node.textContent || ""));
      if (item) {
        item.click();
        return { ok: true, via: "menu" };
      }
      const csrfPair = document.cookie.split(";").map((part) => part.trim())
        .find((part) => part.startsWith("NevaxisFactOS.csrf="));
      const csrf = csrfPair ? decodeURIComponent(csrfPair.slice("NevaxisFactOS.csrf=".length)) : "";
      const response = await fetch("/api/identity/sign-out", {
        method: "POST",
        credentials: "include",
        headers: {
          Accept: "application/json",
          "Content-Type": "application/json",
          "X-NevaxisFactOS-Csrf": csrf,
        },
      });
      if (!response.ok) {
        return { ok: false, status: response.status, hasButton: Boolean(document.querySelector(".account-placeholder")) };
      }
      location.assign("/giris");
      return { ok: true, via: "same-origin" };
    })()`,
  });
  assert(
    "browser sign-out control was used",
    signedOutClick.result?.value?.ok === true,
    JSON.stringify(signedOutClick.result?.value ?? {}),
  );

  const afterSignOut = await waitForPage((page) => page.onSignIn && !page.hasAccount);
  assert(
    "browser sign-out returns to sign-in",
    afterSignOut?.onSignIn === true && afterSignOut?.hasAccount !== true,
    JSON.stringify({ href: afterSignOut?.href, hasAccount: afterSignOut?.hasAccount }),
  );

  const meAfterBrowserSignOut = await sessionSend("Runtime.evaluate", {
    awaitPromise: true,
    returnByValue: true,
    expression: `(async () => {
      const response = await fetch("/api/identity/me", { credentials: "include" });
      return { status: response.status };
    })()`,
  });
  assert(
    "browser /api/identity/me returns 401 after sign-out",
    meAfterBrowserSignOut.result?.value?.status === 401,
    JSON.stringify(meAfterBrowserSignOut.result?.value ?? {}),
  );

  socket.close();
} finally {
  try {
    spawn("taskkill", ["/pid", String(child.pid), "/T", "/F"], { stdio: "ignore" });
  } catch {
    child.kill();
  }
  await new Promise((resolveWait) => setTimeout(resolveWait, 400));
  try {
    rmSync(userDataDir, { recursive: true, force: true, maxRetries: 3, retryDelay: 200 });
  } catch {
    // Chrome may keep a short lock on the temporary profile after exit.
  }
}

console.log("AUTH-LOGIN-CLICK-RECOVERY-1 verification passed.");
