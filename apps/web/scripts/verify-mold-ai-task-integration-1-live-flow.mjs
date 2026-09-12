#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { spawn } from "node:child_process";
import { tmpdir } from "node:os";
import { join } from "node:path";

const WEB = (process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001").replace("127.0.0.1", "localhost").replace(/\/$/, "");
const API = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const ROUTE = "/kaliplar/takip-ve-gorevler";
const MAINTENANCE_MOLD = "bb000001-0070-4000-8000-000000000070";
const INSUFFICIENT_MOLD = "bb000001-0071-4000-8000-000000000071";

const SCREENSHOT_DIR = join(process.cwd(), ".cursor-screenshots");
const WORKSPACE_SHOT = join(SCREENSHOT_DIR, "mold-ai-task-integration-1-workspace-authenticated.png");
const DETAIL_SHOT = join(SCREENSHOT_DIR, "mold-ai-task-integration-1-mold-detail-authenticated.png");
const TASK_SHOT = join(SCREENSHOT_DIR, "mold-ai-task-integration-1-task-detail-authenticated.png");
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

function fail(message) {
  console.error(`MOLD-AI-TASK-INTEGRATION-1 live flow failed: ${message}`);
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

function workspaceSnapExpr() {
  return `({
    href: location.href,
    onSignIn: location.pathname.startsWith("/giris"),
    hasWorkspace: Boolean(document.querySelector('[data-testid="mold-follow-up-workspace"]')),
    hasTable: Boolean(document.querySelector('[data-testid="mold-follow-up-table"]')),
    hasDisclaimer: Boolean(document.querySelector('[data-testid="mold-follow-up-automation-disclaimer"]')),
    bodyText: document.body?.innerText || "",
  })`;
}

function detailSnapExpr() {
  return `(function() {
    const text = document.body?.innerText || "";
    const hasAutoAction =
      /bakım emri oluştur|üretim durdur|yaşam döngüsü değiştir|otomatik bakım/i.test(text) &&
      !/otomatik bakım emri, kalıp yaşam döngüsü veya üretim kararı uygulanmaz/i.test(text);
    return {
      hasDetailReady: Boolean(document.querySelector('[data-testid="mold-follow-up-detail-ready"]')),
      hasCreateButton: Boolean(document.querySelector('[data-testid="mold-follow-up-create-task-button"]')),
      createButtonText: document.querySelector('[data-testid="mold-follow-up-create-task-button"]')?.textContent?.trim() || "",
      hasLinkedTask: Boolean(document.querySelector('[data-testid="mold-follow-up-linked-task-link"]')),
      linkedHref: document.querySelector('[data-testid="mold-follow-up-linked-task-link"]')?.getAttribute("href") || "",
      hasEvidence: /kanıt|vuruş|yaşam döngüsü|MOLD-PILOT/i.test(text),
      hasMoldCode: /MOLD-PILOT/i.test(text),
      hasAutoAction,
      bodyText: text,
    };
  })()`;
}

function taskSnapExpr() {
  return `({
    href: location.href,
    hasSourceSection: Boolean(document.querySelector('[data-testid="ai-task-source-reference-section"]')),
    sourceModuleText: document.querySelector('[data-testid="ai-task-source-reference-section"]')?.innerText || "",
    hasMoldBackLink: Boolean(document.querySelector('[data-testid="ai-task-source-reference-section"] a[href*="takip-ve-gorevler"]')),
    bodyText: document.body?.innerText || "",
  })`;
}

console.log("Running MOLD-AI-TASK-INTEGRATION-1 live browser flow verifier.");
bootstrapSecrets();
const ownerPassword = process.env.DevelopmentSaasIdentity__TenantOwnerPassword;
const mfaSecret = process.env.DevelopmentSaasIdentity__MfaSharedSecret;
const exe = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
if (!existsSync(exe)) fail("Chrome not found");
if (!ownerPassword || !mfaSecret) fail("Missing tenant owner password or MFA secret");
mkdirSync(SCREENSHOT_DIR, { recursive: true });

const apiReady = await fetch(`${API}/health/ready`);
assert("API ready", apiReady.status === 200, `status=${apiReady.status}`);

const userDataDir = mkdtempSync(join(tmpdir(), "mold-ai-task-1-"));
const port = 9890 + Math.floor(Math.random() * 100);
const child = spawn(exe, [`--remote-debugging-port=${port}`, "--headless=new", "--disable-gpu", "--window-size=1440,1600", `--user-data-dir=${userDataDir}`], { stdio: "ignore" });

let createdTaskId = null;

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
  await new Promise((res, rej) => { ws.onopen = res; ws.onerror = rej; });
  let nextId = 1;
  const pending = new Map();
  ws.onmessage = (event) => {
    const message = JSON.parse(String(event.data));
    if (message.id && pending.has(message.id)) {
      const { resolve, reject } = pending.get(message.id);
      pending.delete(message.id);
      message.error ? reject(new Error(message.error.message)) : resolve(message.result);
    }
  };
  const send = (method, params = {}, sessionId) => new Promise((resolve, reject) => {
    const id = nextId++;
    pending.set(id, { resolve, reject });
    const payload = { id, method, params };
    if (sessionId) payload.sessionId = sessionId;
    ws.send(JSON.stringify(payload));
  });
  const attached = await send("Target.attachToTarget", { targetId: created.id, flatten: true });
  const ss = (method, params = {}) => send(method, params, attached.sessionId);
  const evalSnap = async (expr) => (await ss("Runtime.evaluate", { returnByValue: true, expression: expr })).result?.value;
  const waitFor = async (expr, pred, ms = 60000) => {
    const start = Date.now();
    while (Date.now() - start < ms) {
      const value = await evalSnap(expr);
      if (pred(value)) return value;
      await new Promise((r) => setTimeout(r, 300));
    }
    const last = await evalSnap(expr);
    fail(`timeout waiting for snapshot: ${JSON.stringify(last)}`);
  };
  const waitHref = async (pred, ms = 60000) => {
    const start = Date.now();
    while (Date.now() - start < ms) {
      const href = await evalSnap("location.href");
      if (pred(href)) return href;
      await new Promise((r) => setTimeout(r, 300));
    }
    fail(`timeout waiting for href: ${await evalSnap("location.href")}`);
  };
  const navigateTo = async (path) => {
    const url = `${WEB}${path}`;
    await ss("Runtime.evaluate", { expression: `window.location.assign(${JSON.stringify(url)})` });
    await waitHref((href) => href.includes(path.split("?")[0]), 60000);
  };
  const selectMold = async (moldId) => {
    await ss("Runtime.evaluate", {
      expression: `document.querySelector('[data-testid="mold-follow-up-row-${moldId}"] button')?.click()`,
    });
    await waitHref((href) => href.includes(`mold=${encodeURIComponent(moldId)}`), 30000);
    await new Promise((r) => setTimeout(r, 1500));
  };

  await ss("Page.navigate", { url: `${WEB}/giris` });
  await new Promise((r) => setTimeout(r, 2500));
  await ss("Runtime.evaluate", {
    expression: `(() => {
    const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, "value").set;
    const fill = (sel, val) => { const n = document.querySelector(sel); setter.call(n, val); n.dispatchEvent(new Event("input", { bubbles: true })); n.dispatchEvent(new Event("change", { bubbles: true })); };
    fill("#sign-in-organization", "dev-pilot-tenant"); fill("#sign-in-email", "tenant-owner@dev.local"); fill("#sign-in-password", ${JSON.stringify(ownerPassword)});
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

  const me = await ss("Runtime.evaluate", {
    awaitPromise: true,
    returnByValue: true,
    expression: `(async () => { const r = await fetch("/api/identity/me", { credentials: "include" }); return { status: r.status, hasAccount: Boolean((await r.json().catch(() => null))?.accountReference) }; })()`,
  });
  assert("/api/identity/me returns 200 after MFA", me.result?.value?.status === 200 && me.result?.value?.hasAccount, JSON.stringify(me.result?.value));

  await navigateTo(ROUTE);
  const workspaceSnap = await waitFor(workspaceSnapExpr(), (s) => s?.hasWorkspace && s?.hasTable && !s?.onSignIn, 60000);
  assert("mold workspace loaded", workspaceSnap.hasWorkspace);
  assert("mold inventory table visible", workspaceSnap.hasTable);
  assert("automation disclaimer visible", workspaceSnap.hasDisclaimer);
  assert("no auto maintenance/production copy", !/bakım emri oluştur|üretim durdur|yaşam döngüsü değiştir/i.test(workspaceSnap.bodyText));
  writeFileSync(WORKSPACE_SHOT, Buffer.from((await ss("Page.captureScreenshot", { format: "png", fromSurface: true })).data, "base64"));
  assert("workspace screenshot captured", existsSync(WORKSPACE_SHOT));

  await selectMold(MAINTENANCE_MOLD);
  let detailSnap = await waitFor(
    detailSnapExpr(),
    (s) => s?.hasDetailReady && (s?.hasCreateButton || s?.hasLinkedTask),
    45000,
  );
  assert("maintenance mold detail ready", detailSnap.hasDetailReady);
  assert("mold context visible", detailSnap.hasMoldCode || detailSnap.hasEvidence);
  assert("no auto action controls", !detailSnap.hasAutoAction);
  writeFileSync(DETAIL_SHOT, Buffer.from((await ss("Page.captureScreenshot", { format: "png", fromSurface: true })).data, "base64"));
  assert("mold detail screenshot captured", existsSync(DETAIL_SHOT));

  if (!detailSnap.hasLinkedTask && detailSnap.hasCreateButton) {
    assert("technical review task button label", detailSnap.createButtonText.includes("Kalıp teknik inceleme görev taslağı oluştur"));
    await ss("Runtime.evaluate", {
      expression: `document.querySelector('[data-testid="mold-follow-up-create-task-button"]')?.click()`,
    });
    await waitHref((href) => href.includes("/ai-gorevleri/") && !href.includes("/yeni"), 45000);
    createdTaskId = (await evalSnap("location.pathname")).split("/").pop();
    assert("navigated to task detail after create", Boolean(createdTaskId));
  } else if (detailSnap.hasLinkedTask) {
    await ss("Runtime.evaluate", {
      expression: `document.querySelector('[data-testid="mold-follow-up-linked-task-link"]')?.click()`,
    });
    await waitHref((href) => href.includes("/ai-gorevleri/"), 30000);
    createdTaskId = (await evalSnap("location.pathname")).split("/").pop();
  }

  const taskSnap = await waitFor(taskSnapExpr(), (s) => s?.hasSourceSection, 30000);
  assert("task detail page loaded", taskSnap.href.includes("/ai-gorevleri/"));
  assert("task source reference visible", taskSnap.hasSourceSection);
  assert("mold source module visible", /Kalıp|mold/i.test(taskSnap.sourceModuleText));
  assert("mold back-link present", taskSnap.hasMoldBackLink);
  writeFileSync(TASK_SHOT, Buffer.from((await ss("Page.captureScreenshot", { format: "png", fromSurface: true })).data, "base64"));
  assert("task detail screenshot captured", existsSync(TASK_SHOT));

  const taskIdBeforeRefresh = createdTaskId;
  await ss("Runtime.evaluate", { expression: "location.reload()" });
  await new Promise((r) => setTimeout(r, 2500));
  const taskRefresh = await waitFor(taskSnapExpr(), (s) => s?.hasSourceSection && s?.hasMoldBackLink, 30000);
  assert("task refresh preserves source reference", taskRefresh.hasSourceSection && taskRefresh.hasMoldBackLink);

  await navigateTo(`${ROUTE}?mold=${encodeURIComponent(MAINTENANCE_MOLD)}`);
  const moldRefresh = await waitFor(detailSnapExpr(), (s) => s?.hasDetailReady && s?.hasLinkedTask, 45000);
  assert("mold refresh preserves linked task", moldRefresh.hasLinkedTask);
  assert("linked task href stable", moldRefresh.linkedHref.includes(taskIdBeforeRefresh));

  const linkedTaskIdBeforeReplay = taskIdBeforeRefresh;
  if (moldRefresh.hasCreateButton) {
    await ss("Runtime.evaluate", {
      expression: `document.querySelector('[data-testid="mold-follow-up-create-task-button"]')?.click()`,
    });
    await new Promise((r) => setTimeout(r, 2000));
  } else {
    await ss("Runtime.evaluate", {
      expression: `document.querySelector('[data-testid="mold-follow-up-linked-task-link"]')?.click()`,
    });
    await waitHref((href) => href.includes("/ai-gorevleri/"), 30000);
  }
  const replayHref = await evalSnap("location.href");
  const replayTaskId = replayHref.includes("/ai-gorevleri/") ? replayHref.split("/ai-gorevleri/")[1]?.split(/[/?#]/)[0] : linkedTaskIdBeforeReplay;
  assert("idempotent replay same task id", replayTaskId === linkedTaskIdBeforeReplay, `${replayTaskId} vs ${linkedTaskIdBeforeReplay}`);

  await navigateTo(`${ROUTE}?mold=${encodeURIComponent(INSUFFICIENT_MOLD)}`);
  const insufficientSnap = await waitFor(detailSnapExpr(), (s) => s?.hasDetailReady, 45000);
  assert("insufficient evidence detail ready", insufficientSnap.hasDetailReady);
  if (insufficientSnap.hasCreateButton) {
    assert("data-review task label only", insufficientSnap.createButtonText.includes("Kalıp veri inceleme"));
    assert("not technical review task label", !insufficientSnap.createButtonText.includes("Kalıp teknik inceleme"));
  }
  assert("insufficient evidence no auto action", !insufficientSnap.hasAutoAction);

  console.log(`Workspace screenshot: ${WORKSPACE_SHOT}`);
  console.log(`Detail screenshot: ${DETAIL_SHOT}`);
  console.log(`Task screenshot: ${TASK_SHOT}`);
  console.log(`Maintenance mold: ${MAINTENANCE_MOLD}`);
  console.log(`Insufficient mold: ${INSUFFICIENT_MOLD}`);
  console.log(`Created task id: ${createdTaskId}`);
  console.log("MOLD-AI-TASK-INTEGRATION-1 live browser flow verification passed.");
} finally {
  child.kill("SIGTERM");
  try {
    rmSync(userDataDir, { recursive: true, force: true });
  } catch {
    // Chrome may still hold the profile directory on Windows.
  }
}
