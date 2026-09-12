#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { existsSync, writeFileSync, mkdirSync } from "node:fs";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { setTimeout as delay } from "node:timers/promises";
import {
  applyDevelopmentSaasIdentitySecrets,
  requireDevelopmentSaasIdentitySecret,
  resolveRepositoryRoot,
} from "../../../scripts/shared/load-development-saas-identity-secrets.mjs";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const WEB = (process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://127.0.0.1:3001").replace(/\/$/, "");
const API = (process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000").replace(/\/$/, "");
const CDP_PORT = Number(process.env.NEVAXIS_FACTOS_DESKTOP_CDP_PORT ?? "9233");
const ORGANIZATION = "dev-pilot-tenant";
const EMAIL = "tenant-owner@dev.local";
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
const SCREENSHOT_DIR = process.env.NEVAXIS_FACTOS_DESKTOP_SCREENSHOT_DIR
  ?? join(ROOT, ".cursor-screenshots/desktop-sales-demo-1");

async function captureWebViewPng(send, fileName) {
  mkdirSync(SCREENSHOT_DIR, { recursive: true });
  try {
    const result = await send("Page.captureScreenshot", { format: "png" });
    const path = join(SCREENSHOT_DIR, fileName);
    writeFileSync(path, Buffer.from(result.data, "base64"));
    console.log(`PASS screenshot captured - ${fileName}`);
    return path;
  } catch (error) {
    console.log(`WARN screenshot capture skipped for ${fileName}: ${error instanceof Error ? error.message : error}`);
    return null;
  }
}

function fail(message) {
  console.error(`DESKTOP-AUTH-LIVE verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`PASS ${label}${detail ? ` - ${detail}` : ""}`);
}

function requireEnv(name) {
  return requireDevelopmentSaasIdentitySecret(name, ROOT);
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

async function waitForCdp(timeoutMs = 180000) {
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
  const networkEvents = [];
  ws.addEventListener("message", (event) => {
    const message = JSON.parse(String(event.data));
    if (message.method === "Network.responseReceived") {
      networkEvents.push(message.params);
    }
    if (message.id && pending.has(message.id)) {
      const { resolveResult, rejectResult } = pending.get(message.id);
      pending.delete(message.id);
      message.error ? rejectResult(new Error(message.error.message)) : resolveResult(message.result);
    }
  });
  const send = (method, params = {}, timeoutMs = 45000) =>
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
  await send("Network.enable");
  await send("Page.enable");
  await send("Runtime.enable");
  return { ws, send, networkEvents };
}

async function evaluate(send, expression) {
  const result = await send("Runtime.evaluate", { expression, returnByValue: true, awaitPromise: true });
  if (result.exceptionDetails) {
    throw new Error(result.exceptionDetails.text || "Runtime.evaluate failed");
  }
  return result.result?.value;
}

async function probeBrowserLogin(password) {
  const response = await fetch(`${WEB}/api/identity/sign-in`, {
    method: "POST",
    headers: { Accept: "application/json", "Content-Type": "application/json" },
    body: JSON.stringify({
      organizationContext: ORGANIZATION,
      email: EMAIL,
      password,
      deviceLabel: "browser-probe",
    }),
  });
  const body = await response.text();
  return { status: response.status, contentType: response.headers.get("content-type") ?? "", body };
}

async function main() {
  applyDevelopmentSaasIdentitySecrets(ROOT);
  const ownerPassword = requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword");
  const mfaSecret = requireEnv("DevelopmentSaasIdentity__MfaSharedSecret");

  const browserProbe = await probeBrowserLogin(ownerPassword);
  assert(
    "browser identity proxy responds with JSON",
    browserProbe.contentType.includes("json"),
    `status=${browserProbe.status}`,
  );
  assert(
    "browser login reaches MFA or authenticated outcome",
    browserProbe.status === 200,
    `status=${browserProbe.status}`,
  );

  const { send, networkEvents, ws } = await createCdpSession();
  try {
    await send("Page.navigate", { url: `${WEB}/giris` });
    let pageState = null;
    for (let attempt = 0; attempt < 60; attempt += 1) {
      await delay(1000);
      pageState = await evaluate(
        send,
        `({
          href: location.href,
          signIn: !!document.querySelector("#sign-in-email"),
          mfa: !!document.querySelector("#mfa-totp"),
          dashboard: location.pathname.includes("/dashboard"),
          connectionFailure: (document.body?.innerText || "").includes("bağlantısı kurulamadı"),
          title: document.title
        })`,
      );
      if (pageState?.connectionFailure) {
        await evaluate(
          send,
          `(() => {
            const button = [...document.querySelectorAll("button")].find((node) =>
              (node.textContent || "").includes("Yeniden dene"),
            );
            button?.click();
          })()`,
        );
        await delay(4000);
        continue;
      }
      if (pageState?.signIn || pageState?.mfa || pageState?.dashboard) break;
    }

    if (pageState?.dashboard) {
      await evaluate(
        send,
        `fetch("/api/identity/sign-out", { method: "POST", credentials: "include", headers: { Accept: "application/json", "Content-Type": "application/json" }, body: "{}" }).catch(() => null)`,
      );
      await send("Page.navigate", { url: `${WEB}/giris` });
      await delay(4000);
      pageState = await evaluate(
        send,
        `({
          href: location.href,
          signIn: !!document.querySelector("#sign-in-email"),
          mfa: !!document.querySelector("#mfa-totp"),
          dashboard: location.pathname.includes("/dashboard"),
          title: document.title
        })`,
      );
    }

    assert(
      "webview auth entry visible",
      pageState?.signIn === true || pageState?.mfa === true,
      JSON.stringify(pageState),
    );

    if (!pageState?.signIn && !pageState?.mfa && String(pageState?.href ?? "").includes("/giris")) {
      for (let attempt = 0; attempt < 30; attempt += 1) {
        await delay(1000);
        pageState = await evaluate(
          send,
          `({
            href: location.href,
            signIn: !!document.querySelector("#sign-in-email"),
            mfa: !!document.querySelector("#mfa-totp"),
            dashboard: location.pathname.includes("/dashboard"),
            title: document.title
          })`,
        );
        if (pageState?.signIn || pageState?.mfa || pageState?.dashboard) break;
      }
      assert(
        "webview auth entry visible after hydration",
        pageState?.signIn === true || pageState?.mfa === true,
        JSON.stringify(pageState),
      );
    }

    let performedLogin = false;
    if (pageState?.signIn) {
      performedLogin = true;
      await captureWebViewPng(send, "19-desktop-login.png");
      await evaluate(
        send,
        `(() => {
          window.__desktopAuthProbe = { signInStatus: null, signInUrl: null, signInBody: null };
          const originalFetch = window.fetch;
          window.fetch = async (...args) => {
            const url = String(args[0]);
            const response = await originalFetch(...args);
            if (url.includes("/api/identity/sign-in")) {
              window.__desktopAuthProbe.signInUrl = url;
              window.__desktopAuthProbe.signInStatus = response.status;
              try { window.__desktopAuthProbe.signInBody = await response.clone().text(); } catch {}
            }
            return response;
          };
        })()`,
      );

      const loginAttempt = await evaluate(
        send,
        `(() => {
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
          document.querySelector("form.auth-form button").click();
          return { clicked: true };
        })()`,
      );
      assert("webview login click submitted", loginAttempt?.clicked === true);

      let state = null;
      for (let attempt = 0; attempt < 40; attempt += 1) {
        await delay(500);
        state = await evaluate(send, `({
          href: location.href,
          probe: window.__desktopAuthProbe || null,
          errorText: (document.body?.innerText || "").includes("İşlem tamamlanamadı"),
          invalidText: (document.body?.innerText || "").includes("E-posta, kuruluş kodu veya parola hatalı"),
          mfa: location.pathname.includes("/mfa-dogrula"),
          dashboard: location.pathname.includes("/dashboard")
        })`);
        if (state?.mfa || state?.dashboard || state?.errorText || state?.invalidText) break;
      }

      assert(
        "webview login request reached /api/identity/sign-in",
        String(state?.probe?.signInUrl ?? "").includes("/api/identity/sign-in"),
        JSON.stringify(state?.probe),
      );
      assert(
        "webview login HTTP status recorded",
        Number(state?.probe?.signInStatus) === 200,
        JSON.stringify(state?.probe),
      );
      assert("webview login did not show unexpected error", state?.errorText !== true, JSON.stringify(state));
      assert("webview login did not show invalid-credentials error", state?.invalidText !== true, JSON.stringify(state));
      pageState = state;
    }

    if (pageState?.mfa || pageState?.href?.includes("/mfa-dogrula")) {
      const totp = generateTotp(mfaSecret, 0);
      const mfaResult = await evaluate(
        send,
        `(() => {
          const csrf = decodeURIComponent((document.cookie.match(/NevaxisFactOS\\.csrf=([^;]+)/) || [])[1] || "");
          return fetch("/api/identity/mfa/verify", {
            method: "POST",
            credentials: "include",
            headers: {
              Accept: "application/json",
              "Content-Type": "application/json",
              "X-NevaxisFactOS-Csrf": csrf,
            },
            body: JSON.stringify({ totpCode: ${JSON.stringify(totp)} }),
          }).then(async (response) => ({
            status: response.status,
            body: await response.text(),
          }));
        })()`,
      );
      assert("webview MFA verify HTTP 200", Number(mfaResult?.status) === 200, JSON.stringify(mfaResult));
      await send("Page.navigate", { url: `${WEB}/dashboard` });
      await delay(4000);
      for (let attempt = 0; attempt < 40; attempt += 1) {
        await delay(500);
        pageState = await evaluate(send, `({ href: location.href, dashboard: location.pathname.includes("/dashboard"), mfa: location.pathname.includes("/mfa-dogrula"), onSignIn: location.pathname.includes("/giris") })`);
        if (pageState?.dashboard) break;
      }
    }

    assert("webview authenticated dashboard loaded", pageState?.dashboard === true, JSON.stringify(pageState));
    await captureWebViewPng(send, "20-desktop-authenticated-dashboard.png");

    const identityResponse = networkEvents.find((event) =>
      String(event?.response?.url ?? "").includes("/api/identity/sign-in"),
    );
    if (performedLogin) {
      assert(
        "cdp captured identity sign-in response",
        Boolean(identityResponse),
        `status=${identityResponse?.response?.status ?? "missing"}`,
      );
    } else {
      console.log(`PASS cdp identity sign-in capture skipped (existing MFA session)`);
    }

    await send("Page.navigate", { url: `${WEB}/ai-gorevleri/playbooklar?gorunum=calisan` });
    await delay(5000);
    const playbook = await evaluate(send, `({ href: location.href, onSignIn: location.pathname.includes("/giris"), detail: (document.body?.innerText || "").includes("Detayı gör") || (document.body?.innerText || "").includes("Çalışan Akışlar") })`);
    assert("webview authenticated playbook navigation", playbook?.onSignIn !== true, JSON.stringify(playbook));
    await captureWebViewPng(send, "21-desktop-authenticated-playbook-detail.png");

    console.log("DESKTOP-AUTH-LIVE: PASS");
  } finally {
    ws.close();
  }
}

await main();
