#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { setTimeout as delay } from "node:timers/promises";
import {
  applyDevelopmentSaasIdentitySecrets,
  requireDevelopmentSaasIdentitySecret,
  resolveRepositoryRoot,
} from "../../../scripts/shared/load-development-saas-identity-secrets.mjs";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const WEB = (process.env.CREVAFACTOS_WEB_BASE_URL ?? "http://127.0.0.1:3001").replace(/\/$/, "");
const API = (process.env.CREVAFACTOS_API_BASE_URL ?? "http://127.0.0.1:5000").replace(/\/$/, "");
const CDP_PORT = Number(process.env.CREVAFACTOS_DESKTOP_CDP_PORT ?? "9233");
const SCREENSHOT_DIR = process.env.CREVAFACTOS_DESKTOP_ALL_MODULES_SCREENSHOT_DIR
  ?? join(ROOT, ".cursor-screenshots/desktop-all-modules-validation-1");
const RESULTS_JSON = join(SCREENSHOT_DIR, "validation-results.json");
const MANIFEST_PATH = join(dirname(fileURLToPath(import.meta.url)), "desktop-all-modules-validation-manifest-1.json");
const ORGANIZATION = "dev-pilot-tenant";
const EMAIL = "tenant-owner@dev.local";
const REPRESENTATIVE_ONLY = process.env.CREVAFACTOS_DESKTOP_ALL_MODULES_REPRESENTATIVE_ONLY === "1";
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

const ALLOWLISTED_NETWORK_PATTERNS = [
  /\/api\/identity\/me\b/,
  /\/health\/ready\b/,
  /favicon\.ico\b/,
];

function fail(message) {
  console.error(`DESKTOP-ALL-MODULES validation failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`PASS ${label}${detail ? ` - ${detail}` : ""}`);
}

function generateTotp(secret, counterOffset = 0) {
  const counter = Math.floor(Date.now() / 1000 / 30) + counterOffset;
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
  const hashOffset = hash[hash.length - 1] & 0x0f;
  const binaryCode =
    ((hash[hashOffset] & 0x7f) << 24) |
    ((hash[hashOffset + 1] & 0xff) << 16) |
    ((hash[hashOffset + 2] & 0xff) << 8) |
    (hash[hashOffset + 3] & 0xff);
  return String(binaryCode % 1_000_000).padStart(6, "0");
}

function classifyResult(result) {
  if (result.onSignIn) return "AUTH_LOOP";
  if (result.is404) return "ROUTE_MISSING";
  if (result.subscriptionDenied) return "ACCESS_BLOCKED";
  if (result.permissionDenied) return "ACCESS_BLOCKED";
  if (result.runtimeError) return "RUNTIME_BROKEN";
  if (result.connectionOverlay) return "RUNTIME_BROKEN";
  if (result.blankPage) return "EMPTY_OR_WEAK";
  if (result.weakSurface) return "EMPTY_OR_WEAK";
  if (!result.cssLoaded) return "VISUAL_REVIEW_REQUIRED";
  return "READY";
}

function routeSlug(route) {
  return route.replace(/^\//, "").replace(/[/?=&]/g, "-").replace(/-+/g, "-") || "root";
}

async function waitForCdp(timeoutMs = 240000) {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    for (const endpoint of ["/json/list", "/json"]) {
      try {
        const targets = await fetch(`http://127.0.0.1:${CDP_PORT}${endpoint}`).then((r) => r.json());
        const page = targets.find(
          (t) => t.type === "page" && (t.url?.startsWith(WEB) || t.url?.startsWith("http://localhost:3001")),
        );
        if (page?.webSocketDebuggerUrl) return page;
      } catch {
        // not ready
      }
    }
    await delay(1000);
  }
  fail("WebView2 CDP target not available");
}

async function createCdpSession() {
  const target = await waitForCdp();
  const ws = new WebSocket(target.webSocketDebuggerUrl);
  await new Promise((resolveOpen, rejectOpen) => {
    ws.addEventListener("open", resolveOpen, { once: true });
    ws.addEventListener("error", () => rejectOpen(new Error("CDP socket failed")), { once: true });
  });

  let nextId = 1;
  const pending = new Map();
  const consoleErrors = [];
  const runtimeExceptions = [];
  const failedNetwork = [];

  ws.addEventListener("message", (event) => {
    const message = JSON.parse(String(event.data));
    if (message.method === "Runtime.consoleAPICalled") {
      const entry = message.params?.args?.map((arg) => arg.value ?? arg.description ?? "").join(" ");
      if (entry && message.params?.type === "error") consoleErrors.push(entry.slice(0, 400));
    }
    if (message.method === "Runtime.exceptionThrown") {
      runtimeExceptions.push(String(message.params?.exceptionDetails?.text ?? "runtime-exception").slice(0, 400));
    }
    if (message.method === "Network.responseReceived") {
      const status = message.params?.response?.status ?? 0;
      const url = String(message.params?.response?.url ?? "");
      if (status >= 400 && !ALLOWLISTED_NETWORK_PATTERNS.some((pattern) => pattern.test(url))) {
        failedNetwork.push({ url: url.slice(0, 240), status });
      }
    }
    if (message.method === "Network.loadingFailed") {
      const url = String(message.params?.request?.url ?? message.params?.documentURL ?? "");
      if (url && !ALLOWLISTED_NETWORK_PATTERNS.some((pattern) => pattern.test(url))) {
        failedNetwork.push({ url: url.slice(0, 240), status: "loading-failed" });
      }
    }
    if (message.id && pending.has(message.id)) {
      const { resolveResult, rejectResult } = pending.get(message.id);
      pending.delete(message.id);
      message.error ? rejectResult(new Error(message.error.message)) : resolveResult(message.result);
    }
  });

  const send = (method, params = {}, timeoutMs = 60000) =>
    new Promise((resolveResult, rejectResult) => {
      const id = nextId++;
      const timer = setTimeout(() => {
        pending.delete(id);
        rejectResult(new Error(`CDP timeout: ${method}`));
      }, timeoutMs);
      pending.set(id, {
        resolveResult: (value) => {
          clearTimeout(timer);
          resolveResult(value);
        },
        rejectResult: (error) => {
          clearTimeout(timer);
          rejectResult(error);
        },
      });
      ws.send(JSON.stringify({ id, method, params }));
    });

  const evaluate = async (expression) =>
    (await send("Runtime.evaluate", { returnByValue: true, awaitPromise: true, expression }))?.result?.value;

  await send("Runtime.enable");
  await send("Log.enable");
  await send("Page.enable");
  await send("Network.enable");

  return { ws, send, evaluate, consoleErrors, runtimeExceptions, failedNetwork };
}

async function capturePng(send, fileName) {
  mkdirSync(SCREENSHOT_DIR, { recursive: true });
  try {
    const result = await send("Page.captureScreenshot", { format: "png" });
    const path = join(SCREENSHOT_DIR, fileName);
    writeFileSync(path, Buffer.from(result.data, "base64"));
    console.log(`PASS screenshot ${fileName}`);
    return path;
  } catch (error) {
    console.log(`WARN screenshot skipped ${fileName}: ${error instanceof Error ? error.message : error}`);
    return null;
  }
}

async function waitForHref(evaluate, predicate, timeoutMs = 60000) {
  const deadline = Date.now() + timeoutMs;
  while (Date.now() < deadline) {
    const href = await evaluate("location.href");
    if (predicate(String(href ?? ""))) return href;
    await delay(400);
  }
  return await evaluate("location.href");
}

async function navigateTo(send, evaluate, path) {
  await send("Page.navigate", { url: `${WEB}${path.startsWith("/") ? path : `/${path}`}` });
  await delay(2200);
  await waitForHref(
    evaluate,
    (href) => href.includes(path.split("?")[0]) || href === `${WEB}${path}`,
    90000,
  );
  await delay(1200);
}

async function pageSnapshot(evaluate) {
  return await evaluate(`({
    href: location.href,
    pathname: location.pathname,
    title: document.title,
    onSignIn: location.pathname.startsWith("/giris"),
    localhostVisible: /localhost|127\\.0\\.0\\.1/i.test(document.body?.innerText || ""),
    is404: Boolean(document.querySelector('[data-testid="not-found-page"]')) || /^404\\s*[-—]/i.test(document.querySelector("h1")?.textContent || ""),
    subscriptionDenied: /abonelik|erişim kısıtlandı|modül erişimi/i.test(document.body?.innerText || "") && location.pathname.includes("abonelik"),
    permissionDenied: /erişim reddedildi|yetkiniz yok|izin yok/i.test(document.body?.innerText || ""),
    runtimeError: Boolean(document.querySelector("#__next-build-error, nextjs-portal[data-nextjs-dialog]")) || /Application error|Unhandled Runtime Error|Bir hata oluştu|This page couldn.?t load/i.test(document.body?.innerText || ""),
    connectionOverlay: /Bağlantı kurulamadı|connection failed|sunucuya bağlan/i.test(document.body?.innerText || ""),
    blankPage: (document.body?.innerText || "").trim().length < 40,
    weakSurface: /Bu modül için henüz|yakında|placeholder|TODO/i.test(document.body?.innerText || ""),
    cssLoaded: Boolean(getComputedStyle(document.body).fontFamily) && getComputedStyle(document.body).fontFamily !== "Times",
    hasShell: Boolean(document.querySelector("nav, [data-testid='app-shell'], .app-shell")),
    headerText: document.querySelector("h1, [data-testid='page-header-title']")?.textContent?.trim() || "",
    bodyPreview: (document.body?.innerText || "").slice(0, 240)
  })`);
}

async function ensureAuthenticated(send, evaluate, ownerPassword, mfaSecret) {
  await send("Page.navigate", { url: `${WEB}/giris` });
  await delay(2500);

  let authState = null;
  for (let attempt = 0; attempt < 50; attempt += 1) {
    authState = await evaluate(`({
      href: location.href,
      signIn: !!document.querySelector("#sign-in-email"),
      mfa: !!document.querySelector("#mfa-totp") || location.pathname.includes("/mfa-dogrula"),
      dashboard: location.pathname.includes("/dashboard")
    })`);
    if (authState?.signIn || authState?.mfa || authState?.dashboard) break;
    await delay(500);
  }

  if (authState?.dashboard && !authState?.mfa && !authState?.signIn) {
    console.log("PASS existing authenticated session detected");
    return { performedLogin: false };
  }

  if (authState?.signIn) {
    await evaluate(`(() => {
      const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, "value").set;
      const fill = (id, value) => {
        const node = document.querySelector(id);
        if (!node) throw new Error("missing " + id);
        setter.call(node, value);
        node.dispatchEvent(new Event("input", { bubbles: true }));
        node.dispatchEvent(new Event("change", { bubbles: true }));
      };
      fill("#sign-in-organization", ${JSON.stringify(ORGANIZATION)});
      fill("#sign-in-email", ${JSON.stringify(EMAIL)});
      fill("#sign-in-password", ${JSON.stringify(ownerPassword)});
      document.querySelector("form.auth-form button")?.click();
    })()`);

    let afterLogin = null;
    for (let attempt = 0; attempt < 60; attempt += 1) {
      await delay(500);
      afterLogin = await evaluate(`({
        href: location.href,
        onSignIn: location.pathname.startsWith("/giris"),
        mfa: location.pathname.includes("/mfa-dogrula") || !!document.querySelector("#mfa-totp"),
        dashboard: location.pathname.includes("/dashboard"),
        invalidText: (document.body?.innerText || "").includes("E-posta, kuruluş kodu veya parola hatalı"),
        errorText: (document.body?.innerText || "").includes("İşlem tamamlanamadı")
      })`);
      if (afterLogin?.mfa || afterLogin?.dashboard || afterLogin?.invalidText || afterLogin?.errorText) break;
    }

    if (afterLogin?.invalidText || afterLogin?.errorText) {
      fail(`sign-in rejected in WebView2: ${JSON.stringify(afterLogin)}`);
    }
    if (afterLogin?.onSignIn) {
      fail("sign-in did not leave /giris");
    }
    authState = afterLogin;
  }

  if (authState?.mfa || String(authState?.href ?? "").includes("/mfa-dogrula")) {
    let mfaOk = false;
    for (const offset of [-1, 0, 1]) {
      const result = await evaluate(`(async () => {
        const csrf = decodeURIComponent((document.cookie.match(/crevafactos\\.csrf=([^;]+)/) || [])[1] || "");
        const res = await fetch("/api/identity/mfa/verify", {
          method: "POST",
          credentials: "include",
          headers: {
            Accept: "application/json",
            "Content-Type": "application/json",
            "X-CrevaFactOS-Csrf": csrf,
          },
          body: JSON.stringify({ totpCode: ${JSON.stringify(generateTotp(mfaSecret, offset))} }),
        });
        return { status: res.status };
      })()`);
      if (Number(result?.status) === 200) {
        mfaOk = true;
        break;
      }
    }
    assert("MFA verification succeeded", mfaOk);
    await delay(2500);
  }

  await send("Page.navigate", { url: `${WEB}/dashboard` });
  await delay(4000);

  return { performedLogin: true };
}

async function main() {
  applyDevelopmentSaasIdentitySecrets(ROOT);
  const ownerPassword = requireDevelopmentSaasIdentitySecret("DevelopmentSaasIdentity__TenantOwnerPassword", ROOT);
  const mfaSecret = requireDevelopmentSaasIdentitySecret("DevelopmentSaasIdentity__MfaSharedSecret", ROOT);
  const manifest = JSON.parse(readFileSync(MANIFEST_PATH, "utf8"));
  assert(`manifest route count ${manifest.canonicalRouteCount}`, manifest.routes.length === manifest.canonicalRouteCount);

  const health = await fetch(`${API}/health/ready`).catch(() => null);
  assert("API health/ready 200", health?.ok === true, `${API}/health/ready`);

  mkdirSync(SCREENSHOT_DIR, { recursive: true });
  const { ws, send, evaluate, consoleErrors, runtimeExceptions, failedNetwork } = await createCdpSession();

  const routeResults = [];
  const presentationFindings = { P0: [], P1: [], P2: [] };
  const dataCleanupCandidates = [];
  const capturedScreens = new Set();

  try {
    await ensureAuthenticated(send, evaluate, ownerPassword, mfaSecret);

    await send("Page.navigate", { url: `${WEB}/dashboard` });
    await delay(3500);

    const navSnap = await evaluate(`({
      links: [...document.querySelectorAll(".sidebar-navigation a[href], nav.sidebar-navigation a[href]")].map((a) => ({
        href: a.getAttribute("href"),
        text: (a.textContent || "").trim()
      })),
      count: document.querySelectorAll(".sidebar-navigation a[href], nav.sidebar-navigation a[href]").length
    })`);
    assert("sidebar navigation visible", (navSnap?.count ?? 0) >= 20, `count=${navSnap?.count ?? 0}`);

    const expectedVisible = manifest.routes.filter((r) => r.expectedVisible);
    const actualHrefs = new Set((navSnap?.links ?? []).map((l) => l.href));
    const navigationAudit = expectedVisible.map((item) => ({
      module: item.module,
      route: item.route,
      expectedVisible: true,
      actualVisible: actualHrefs.has(item.route),
      match: actualHrefs.has(item.route),
    }));
    const missingNav = navigationAudit.filter((item) => !item.match);
    if (missingNav.length > 0) {
      presentationFindings.P0.push(`Missing sidebar navigation for: ${missingNav.map((m) => m.route).join(", ")}`);
    }

    for (const item of manifest.routes) {
      if (REPRESENTATIVE_ONLY && !Object.values(manifest.representativeScreenshots ?? {}).includes(item.route)) {
        continue;
      }
      const beforeConsole = consoleErrors.length;
      const beforeNetwork = failedNetwork.length;
      const beforeExceptions = runtimeExceptions.length;

      await navigateTo(send, evaluate, item.route);
      const snap = await pageSnapshot(evaluate);
      const classification = classifyResult(snap ?? {});
      let pass =
        classification === "READY" ||
        classification === "VISUAL_REVIEW_REQUIRED" ||
        (classification === "EMPTY_OR_WEAK" && snap?.hasShell && !snap?.runtimeError && !snap?.is404);

      const routeConsoleErrors = consoleErrors.slice(beforeConsole);
      const routeNetworkFailures = failedNetwork.slice(beforeNetwork);
      const routeExceptions = runtimeExceptions.slice(beforeExceptions);
      const documentFailed = routeNetworkFailures.some(
        (entry) =>
          entry.url.includes(item.route.split("?")[0]) &&
          (entry.status === 500 || entry.status === "loading-failed"),
      );
      if (routeConsoleErrors.length > 0 || routeExceptions.length > 0 || documentFailed) pass = false;

      if (snap?.localhostVisible) {
        pass = false;
        presentationFindings.P0.push(`${item.route}: localhost/127.0.0.1 visible in UI`);
      }
      if (/verifier|fixture|negative-test|sales-demo-readiness/i.test(snap?.bodyPreview ?? "")) {
        dataCleanupCandidates.push({ route: item.route, reason: "verifier/fixture text visible in UI" });
      }

      const result = {
        index: routeResults.length + 1,
        route: item.route,
        module: item.module,
        navigationGroup: item.navigationGroup,
        httpResult: snap?.onSignIn ? "redirect-login" : "200",
        documentTitle: snap?.title ?? "",
        renderSuccess: Boolean(snap?.hasShell && !snap?.runtimeError && !snap?.blankPage),
        appShellPresent: Boolean(snap?.hasShell),
        expectedHeading: snap?.headerText ?? "",
        consoleErrors: routeConsoleErrors,
        runtimeExceptions: routeExceptions,
        failedNetwork: routeNetworkFailures,
        authStatus: snap?.onSignIn ? "unauthenticated" : "authenticated",
        visualState: classification,
        verdict: pass ? "PASS" : "FAIL",
        classification,
      };
      routeResults.push(result);
      console.log(`${result.verdict} ${item.module} (${item.route}) -> ${classification}`);

      if (!pass) {
        await capturePng(send, `failure-${routeSlug(item.route)}.png`);
      }

      const repShot = Object.entries(manifest.representativeScreenshots ?? {}).find(([, route]) => route === item.route);
      if (repShot && !capturedScreens.has(repShot[0])) {
        await capturePng(send, repShot[0]);
        capturedScreens.add(repShot[0]);
      }
    }

    // Representative screenshots for routes not hit in loop order
    for (const [fileName, route] of Object.entries(manifest.representativeScreenshots ?? {})) {
      if (capturedScreens.has(fileName)) continue;
      await navigateTo(send, evaluate, route);
      await capturePng(send, fileName);
      capturedScreens.add(fileName);
    }

    // Deep validation flows
    const deepFlows = [];
    for (const flow of manifest.salesCriticalFlows ?? []) {
      await navigateTo(send, evaluate, flow.route);
      const snap = await pageSnapshot(evaluate);
      const ok =
        !snap?.onSignIn &&
        !snap?.runtimeError &&
        !snap?.permissionDenied &&
        !snap?.subscriptionDenied &&
        !snap?.is404 &&
        (snap?.bodyPreview ?? "").trim().length > 60;
      deepFlows.push({ ...flow, pass: ok, href: snap?.href ?? "" });
      console.log(`${ok ? "PASS" : "FAIL"} deep-flow ${flow.name} (${flow.route})`);
    }

    // Playbook detail deep validation
    await navigateTo(send, evaluate, "/ai-gorevleri/playbooklar?gorunum=calisan");
    const playbookDetail = await evaluate(`({
      hasWorkerFlows: (document.body?.innerText || "").includes("Çalışan Akışlar"),
      hasDetail: (document.body?.innerText || "").includes("Detayı gör")
    })`);
    const playbookDetailPass = Boolean(playbookDetail?.hasWorkerFlows || playbookDetail?.hasDetail);
    assert("playbook run detail surface visible", playbookDetailPass, JSON.stringify(playbookDetail));
    if (!capturedScreens.has("03-playbook-run-detail.png")) {
      await capturePng(send, "03-playbook-run-detail.png");
    }

    const passCount = routeResults.filter((r) => r.verdict === "PASS").length;
    const failCount = routeResults.length - passCount;
    const expectedPass = REPRESENTATIVE_ONLY
      ? Object.keys(manifest.representativeScreenshots ?? {}).length
      : manifest.canonicalRouteCount;
    const userFacingConsoleErrors = consoleErrors.length + runtimeExceptions.length;

    const output = {
      generatedAt: new Date().toISOString(),
      environment: "desktop-webview2",
      web: WEB,
      api: API,
      canonicalRouteCount: manifest.canonicalRouteCount,
      representativeOnly: REPRESENTATIVE_ONLY,
      passCount,
      failCount,
      expectedPassCount: expectedPass,
      userFacingConsoleErrors,
      navigationAudit: {
        expectedVisible: navigationAudit,
        actualVisibleCount: navSnap?.count ?? 0,
        match: missingNav.length === 0,
      },
      routeResults,
      deepFlows,
      presentationFindings,
      dataCleanupCandidates,
      screenshotsDir: SCREENSHOT_DIR,
      representativeScreenshots: [...capturedScreens],
    };

    writeFileSync(RESULTS_JSON, `${JSON.stringify(output, null, 2)}\n`, "utf8");
    console.log(`\nMatrix: ${passCount}/${manifest.canonicalRouteCount} PASS, ${failCount} FAIL`);
    console.log(`Results: ${RESULTS_JSON}`);

    if (failCount > 0 || (!REPRESENTATIVE_ONLY && missingNav.length > 0) || deepFlows.some((f) => !f.pass)) {
      const failures = routeResults
        .filter((r) => r.verdict === "FAIL")
        .map((r) => `${r.module} (${r.route}): ${r.classification}`)
        .join("\n");
      fail(`route failures:\n${failures}`);
    }
    if (userFacingConsoleErrors > 0) {
      fail(`user-facing console/runtime errors: ${userFacingConsoleErrors}`);
    }

    console.log("DESKTOP-ALL-MODULES-VALIDATION: PASS");
  } finally {
    ws.close();
  }
}

await main();
