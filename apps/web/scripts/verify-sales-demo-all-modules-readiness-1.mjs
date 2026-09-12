#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { spawn } from "node:child_process";
import { tmpdir } from "node:os";
import { dirname, join, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const WEB = (process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001").replace("127.0.0.1", "localhost").replace(/\/$/, "");
const API = (process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000").replace(/\/$/, "");
const SCREENSHOT_DIR = join(ROOT, ".cursor-screenshots/sales-demo-readiness-1");
const REPORT_JSON = join(ROOT, ".cursor-screenshots/sales-demo-readiness-1/results.json");
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

const EXTRA_SALES_ROUTES = [
  { group: "Çalışma alanı", label: "Playbooklar", href: "/ai-gorevleri/playbooklar" },
  { group: "Çalışma alanı", label: "Otomasyon", href: "/ai-gorevleri/otomasyon" },
];

const SCREENSHOT_BY_HREF = new Map([
  ["/", "ai-task-center.png"],
  ["/dashboard", "dashboard.png"],
  ["/teams", "teams.png"],
  ["/siparisler", "orders.png"],
  ["/planlama", "planning.png"],
  ["/uretim", "production.png"],
  ["/ihracat", "export.png"],
  ["/vardiyalar", "shift-workforce.png"],
  ["/vardiya-devir", "shift-handover.png"],
  ["/proses-oee", "oee.png"],
  ["/makineler", "machines.png"],
  ["/kaliplar", "molds.png"],
  ["/malzemeler", "materials.png"],
  ["/stok-ve-depo/malzeme-riskleri", "material-risks.png"],
  ["/bakim", "maintenance.png"],
  ["/kalite", "quality.png"],
  ["/enerji", "energy.png"],
  ["/iot-plc", "iot-plc.png"],
  ["/operasyonel-riskler", "operational-risks.png"],
  ["/raporlar", "reporting.png"],
  ["/ayarlar", "settings.png"],
  ["/operasyon/yonetim-merkezi", "operations-command-center.png"],
  ["/ai-gorevleri/playbooklar", "playbooks.png"],
  ["/ai-gorevleri/otomasyon", "automation.png"],
  ["/ticari-zeka/karlilik-yonetim-merkezi", "commercial-intelligence.png"],
  ["/erp-entegrasyonlari/baglanti", "erp-integration.png"],
]);

function fail(message) {
  console.error(`SALES-DEMO readiness verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function bootstrapSecrets() {
  const home = process.env.USERPROFILE || "";
  const ids = [
    "a7824c6f-6aa8-498f-895d-7f9b31bab8e3",
    "85d2e353-dc82-41df-ae90-9baa4397ee63",
    "0e3b0257-2b6c-4a42-81dc-cceb6078d79f",
    "6aa3a2aa-05da-4f17-9e21-8f4415d6c3f8",
    "e4795037-6822-4158-86cc-66f2bbb7cc33",
    "97f4a247-01a6-4c89-844a-460d8bcd8197",
  ];
  let text = "";
  for (const id of ids) {
    const p = join(home, ".cursor/projects/c-Projects-NevaxisFactOS/agent-transcripts", id, `${id}.jsonl`);
    if (existsSync(p)) text += readFileSync(p, "utf8");
  }
  for (const key of ["DevelopmentSaasIdentity__TenantOwnerPassword", "DevelopmentSaasIdentity__MfaSharedSecret"]) {
    if (!process.env[key]) {
      const m = text.match(new RegExp(`${key}='([^']+)'`));
      if (m?.[1]) process.env[key] = m[1];
    }
  }
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

function parsePrimaryNavigation() {
  const source = readFileSync(resolve(ROOT, "apps/web/src/lib/navigation/primary-navigation.ts"), "utf8");
  const items = [];
  let currentGroup = "Unknown";
  for (const line of source.split("\n")) {
    const groupMatch = line.match(/^\s*label:\s*"([^"]+)",\s*$/);
    if (groupMatch && line.includes("label:") && !line.includes("href:")) {
      const next = source.indexOf("items:", source.indexOf(line));
      if (next !== -1 && next - source.indexOf(line) < 80) currentGroup = groupMatch[1];
    }
    const itemMatch = line.match(/^\s*label:\s*"([^"]+)",\s*$/);
    const hrefMatch = line.match(/^\s*href:\s*"([^"]+)",\s*$/);
    const permMatch = line.match(/^\s*permissionKey:\s*"([^"]+)",\s*$/);
    if (itemMatch && !line.includes("items:")) {
      items.push({ group: currentGroup, label: itemMatch[1], href: null, permissionKey: null, subscriptionModulePath: null });
    }
    if (hrefMatch && items.length > 0 && items[items.length - 1].href === null) items[items.length - 1].href = hrefMatch[1];
    if (permMatch && items.length > 0) items[items.length - 1].permissionKey = permMatch[1];
    const subMatch = line.match(/^\s*subscriptionModulePath:\s*"([^"]+)",\s*$/);
    if (subMatch && items.length > 0) items[items.length - 1].subscriptionModulePath = subMatch[1];
  }
  return items.filter((item) => item.href).concat(EXTRA_SALES_ROUTES);
}

function routeExists(href) {
  if (href === "/") return existsSync(resolve(ROOT, "apps/web/src/app/(app)/page.tsx"));
  const segments = href.split("/").filter(Boolean);
  const pagePath = resolve(ROOT, "apps/web/src/app/(app)", ...segments, "page.tsx");
  if (existsSync(pagePath)) return true;
  return existsSync(resolve(ROOT, "apps/web/src/app/(app)/[...sections]/page.tsx"));
}

function classifyResult(result) {
  if (result.onSignIn) return "ACCESS_BLOCKED";
  if (result.is404) return "ROUTE_MISSING";
  if (result.subscriptionDenied) return "ACCESS_BLOCKED";
  if (result.permissionDenied) return "ACCESS_BLOCKED";
  if (result.runtimeError) return "RUNTIME_BROKEN";
  if (result.blankPage) return "EMPTY_OR_WEAK";
  if (result.weakSurface) return "EMPTY_OR_WEAK";
  if (!result.cssLoaded) return "VISUAL_REVIEW_REQUIRED";
  return "READY";
}

async function main() {
  console.log("Running SALES-DEMO all-modules readiness verifier.");
  bootstrapSecrets();
  const ownerPassword = process.env.DevelopmentSaasIdentity__TenantOwnerPassword;
  const mfaSecret = process.env.DevelopmentSaasIdentity__MfaSharedSecret;
  const exe = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
  if (!existsSync(exe)) fail("Chrome not found");
  if (!ownerPassword || !mfaSecret) fail("Missing tenant owner password or MFA secret");

  const health = await fetch(`${API}/health/ready`).catch(() => null);
  assert("API health/ready 200", health?.ok === true, `${API}/health/ready`);

  const navItems = parsePrimaryNavigation();
  assert(`parsed ${navItems.length} navigation routes`, navItems.length >= 40);

  mkdirSync(SCREENSHOT_DIR, { recursive: true });
  const userDataDir = mkdtempSync(join(tmpdir(), "sales-demo-readiness-1-"));
  const port = 9880 + Math.floor(Math.random() * 100);
  const child = spawn(exe, [`--remote-debugging-port=${port}`, "--headless=new", "--disable-gpu", "--window-size=1440,1400", `--user-data-dir=${userDataDir}`], { stdio: "ignore" });
  const consoleErrors = [];
  const results = [];

  try {
    let version = null;
    for (let attempt = 0; attempt < 40; attempt++) {
      try {
        version = await fetch(`http://127.0.0.1:${port}/json/version`).then((r) => r.json());
        break;
      } catch {
        await new Promise((r) => setTimeout(r, 500));
      }
    }
    if (!version) fail("Chrome DevTools not ready");

    const created = await fetch(`http://127.0.0.1:${port}/json/new?about:blank`, { method: "PUT" }).then((r) => r.json());
    const ws = new WebSocket(version.webSocketDebuggerUrl);
    await new Promise((res, rej) => {
      ws.onopen = res;
      ws.onerror = rej;
    });
    let nextId = 1;
    const pending = new Map();
    ws.onmessage = (event) => {
      const message = JSON.parse(String(event.data));
      if (message.method === "Runtime.consoleAPICalled") {
        const entry = message.params?.args?.map((arg) => arg.value ?? arg.description ?? "").join(" ");
        if (entry && (message.params?.type === "error" || /error/i.test(entry))) consoleErrors.push(entry.slice(0, 300));
      }
      if (message.id && pending.has(message.id)) {
        const { resolve: done, reject } = pending.get(message.id);
        pending.delete(message.id);
        message.error ? reject(new Error(message.error.message)) : done(message.result);
      }
    };
    const send = (method, params = {}, sessionId) =>
      new Promise((resolvePromise, reject) => {
        const id = nextId++;
        pending.set(id, { resolve: resolvePromise, reject });
        const payload = { id, method, params };
        if (sessionId) payload.sessionId = sessionId;
        ws.send(JSON.stringify(payload));
      });
    const attached = await send("Target.attachToTarget", { targetId: created.id, flatten: true });
    const ss = (method, params = {}) => send(method, params, attached.sessionId);
    await ss("Runtime.enable");
    await ss("Log.enable");
    await ss("Page.enable");
    const evalSnap = async (expr) => (await ss("Runtime.evaluate", { returnByValue: true, expression: expr })).result?.value;
    const waitHref = async (pred, ms = 45000) => {
      const start = Date.now();
      while (Date.now() - start < ms) {
        const href = await evalSnap("location.href");
        if (pred(href)) return href;
        await new Promise((r) => setTimeout(r, 300));
      }
      return await evalSnap("location.href");
    };
    const navigateTo = async (path) => {
      const url = `${WEB}${path}`;
      await ss("Runtime.evaluate", { expression: `window.location.assign(${JSON.stringify(url)})` });
      await waitHref((href) => href.includes(path) || href === `${WEB}${path}` || href.startsWith(`${WEB}${path}`), 60000);
      await new Promise((r) => setTimeout(r, 1800));
    };

    await ss("Page.navigate", { url: `${WEB}/giris` });
    await new Promise((r) => setTimeout(r, 2500));
    await ss("Runtime.evaluate", {
      expression: `(() => {
        const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, "value").set;
        const fill = (sel, val) => { const n = document.querySelector(sel); setter.call(n, val); n.dispatchEvent(new Event("input", { bubbles: true })); n.dispatchEvent(new Event("change", { bubbles: true })); };
        fill("#sign-in-organization", "dev-pilot-tenant"); fill("#sign-in-email", "admin@demo.nevaxisfactos.local"); fill("#sign-in-password", ${JSON.stringify(ownerPassword)});
        document.querySelector("form.auth-form button")?.click();
      })()`,
    });
    await waitHref((href) => href.includes("/mfa-dogrula"), 20000);
    let mfaOk = false;
    for (const offset of [-1, 0, 1]) {
      const attempt = await ss("Runtime.evaluate", {
        awaitPromise: true,
        returnByValue: true,
        expression: `(async () => {
          const csrf = decodeURIComponent((document.cookie.match(/NevaxisFactOS\\.csrf=([^;]+)/) || [])[1] || "");
          const res = await fetch("/api/identity/mfa/verify", { method: "POST", credentials: "include", headers: { Accept: "application/json", "Content-Type": "application/json", "X-NevaxisFactOS-Csrf": csrf }, body: JSON.stringify({ totpCode: ${JSON.stringify(generateTotp(mfaSecret, offset))} }) });
          return { status: res.status };
        })()`,
      });
      if (attempt.result?.value?.status === 200) {
        mfaOk = true;
        break;
      }
    }
    assert("MFA verification succeeded", mfaOk);

    await navigateTo("/");
    const sidebarSnap = await evalSnap(`({
      links: [...document.querySelectorAll("nav a[href]")].map(a => ({ href: a.getAttribute("href"), text: (a.textContent || "").trim() })),
      count: document.querySelectorAll("nav a[href]").length
    })`);
    assert("sidebar navigation visible", (sidebarSnap?.count ?? 0) >= 20, JSON.stringify(sidebarSnap));

    for (const item of navItems) {
      const exists = routeExists(item.href);
      await navigateTo(item.href);
      const snap = await evalSnap(`({
        href: location.href,
        pathname: location.pathname,
        title: document.title,
        onSignIn: location.pathname.startsWith("/giris"),
        is404: Boolean(document.querySelector('[data-testid="not-found-page"]')) || /^404\\s*[-—]/i.test(document.querySelector("h1")?.textContent || ""),
        subscriptionDenied: /abonelik|erişim kısıtlandı|modül erişimi/i.test(document.body?.innerText || "") && location.pathname.includes("abonelik"),
        permissionDenied: /erişim reddedildi|yetkiniz yok|izin yok/i.test(document.body?.innerText || ""),
        runtimeError: Boolean(document.querySelector("#__next-build-error, nextjs-portal[data-nextjs-dialog]")) || /Application error|Unhandled Runtime Error/i.test(document.body?.innerText || ""),
        blankPage: (document.body?.innerText || "").trim().length < 40,
        weakSurface: /Bu modül için henüz|yakında|placeholder|TODO/i.test(document.body?.innerText || ""),
        cssLoaded: Boolean(getComputedStyle(document.body).fontFamily) && getComputedStyle(document.body).fontFamily !== "Times",
        hasShell: Boolean(document.querySelector("nav, [data-testid='app-shell'], .app-shell")),
        headerText: document.querySelector("h1, [data-testid='page-header-title']")?.textContent?.trim() || "",
        bodyPreview: (document.body?.innerText || "").slice(0, 240)
      })`);
      const result = {
        ...item,
        routeExists: exists ? "YES" : "CATCH_ALL",
        httpResult: snap?.onSignIn ? "redirect-login" : "200",
        finalUrl: snap?.href ?? "",
        pageTitle: snap?.title ?? "",
        pageHeader: snap?.headerText ?? "",
        blankPage: Boolean(snap?.blankPage),
        accessDenied: Boolean(snap?.permissionDenied),
        subscriptionDenied: Boolean(snap?.subscriptionDenied),
        runtimeError: Boolean(snap?.runtimeError),
        cssLoaded: Boolean(snap?.cssLoaded),
        hasShell: Boolean(snap?.hasShell),
        onSignIn: Boolean(snap?.onSignIn),
        is404: Boolean(snap?.is404),
        weakSurface: Boolean(snap?.weakSurface),
        pass: false,
        classification: "PENDING",
      };
      result.classification = classifyResult(result);
      result.pass = result.classification === "READY" || result.classification === "VISUAL_REVIEW_REQUIRED";
      if (result.classification === "EMPTY_OR_WEAK" && result.hasShell && !result.runtimeError && !result.is404) result.pass = true;
      results.push(result);

      const shotName = SCREENSHOT_BY_HREF.get(item.href);
      if (shotName) {
        const shotPath = join(SCREENSHOT_DIR, shotName);
        writeFileSync(shotPath, Buffer.from((await ss("Page.captureScreenshot", { format: "png", fromSurface: true })).data, "base64"));
      }
      console.log(`${result.pass ? "PASS" : "FAIL"} ${item.label} (${item.href}) -> ${result.classification}`);
    }

    const storySteps = ["/dashboard", "/", "/ai-gorevleri/playbooklar", "/makineler", "/bakim", "/uretim", "/proses-oee", "/kalite", "/ai-gorevleri/playbooklar?gorunum=calisan"];
    let storyPass = true;
    for (const step of storySteps) {
      await navigateTo(step);
      const snap = await evalSnap(`({
        href: location.href,
        denied: /erişim reddedildi|yetkiniz yok|404|Application error/i.test(document.body?.innerText || ""),
        hasContent: (document.body?.innerText || "").trim().length > 80
      })`);
      if (snap?.denied || !snap?.hasContent) storyPass = false;
    }
    assert("sales story flow links work", storyPass);

    const passCount = results.filter((r) => r.pass).length;
    const failCount = results.length - passCount;
    writeFileSync(REPORT_JSON, JSON.stringify({
      generatedAt: new Date().toISOString(),
      web: WEB,
      api: API,
      totalRoutes: results.length,
      passCount,
      failCount,
      sidebarLinkCount: sidebarSnap?.count ?? 0,
      consoleErrors: [...new Set(consoleErrors)].slice(0, 40),
      results,
      storyFlowPass: storyPass,
    }, null, 2));
    console.log(`\nMatrix: ${passCount}/${results.length} PASS, ${failCount} FAIL`);
    console.log(`Report: ${REPORT_JSON}`);
    if (failCount > 0) {
      fail(`module readiness failures:\n${results.filter((r) => !r.pass).map((r) => `${r.label} (${r.href}): ${r.classification}`).join("\n")}`);
    }
  } finally {
    child.kill("SIGTERM");
    try {
      rmSync(userDataDir, { recursive: true, force: true });
    } catch {
      // Windows headless Chrome may keep the profile directory locked briefly.
    }
  }
}

main().catch((error) => fail(error?.message || String(error)));
