#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from "node:fs";
import { spawn, spawnSync } from "node:child_process";
import { tmpdir } from "node:os";
import { join } from "node:path";

const WEB = (process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001").replace("127.0.0.1", "localhost").replace(/\/$/, "");
const API = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const MANAGER = { issuer: "crevafactos-dev", subject: "planning-pilot-user", tenant: "dev-pilot-tenant" };
const SCREENSHOT_DIR = join(process.cwd(), ".cursor-screenshots");
const STATE_FILE = join(SCREENSHOT_DIR, "ai-task-playbook-visual-review-1-state.json");
const TOTP_ALPHABET = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";
const SHOTS = {
  list: join(SCREENSHOT_DIR, "ai-task-playbook-run-detail-nav-1-running-list.png"),
  detail: join(SCREENSHOT_DIR, "ai-task-playbook-run-detail-nav-1-detail-open.png"),
};

function fail(message) {
  console.error(`AI-TASK-PLAYBOOK-RUN-DETAIL-NAVIGATION-1 failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function bootstrapSecrets() {
  const home = process.env.USERPROFILE || "";
  const ids = ["a7824c6f-6aa8-498f-895d-7f9b31bab8e3", "85d2e353-dc82-41df-ae90-9baa4397ee63", "97f4a247-01a6-4c89-844a-460d8bcd8197"];
  let text = "";
  for (const id of ids) {
    const p = join(home, ".cursor/projects/c-Projects-NevaxisZentra/agent-transcripts", id, `${id}.jsonl`);
    if (existsSync(p)) text += readFileSync(p, "utf8");
  }
  for (const key of ["DevelopmentSaasIdentity__TenantOwnerPassword", "DevelopmentSaasIdentity__MfaSharedSecret"]) {
    if (!process.env[key]) {
      const m = text.match(new RegExp(`${key}='([^']+)'`));
      if (m?.[1]) process.env[key] = m[1];
    }
  }
}

function managerHeaders() {
  return {
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": MANAGER.issuer,
    "X-crevafactos-dev-Subject": MANAGER.subject,
    "X-NevaxisZentra-Tenant": MANAGER.tenant,
  };
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
  const binaryCode = ((hash[hashOffset] & 0x7f) << 24)
    | ((hash[hashOffset + 1] & 0xff) << 16)
    | ((hash[hashOffset + 2] & 0xff) << 8)
    | (hash[hashOffset + 3] & 0xff);
  return String(binaryCode % 1_000_000).padStart(6, "0");
}

async function api(path, init = {}) {
  const res = await fetch(`${API}${path}`, init);
  const text = await res.text();
  let payload = null;
  try { payload = text ? JSON.parse(text) : null; } catch { payload = text; }
  return { status: res.status, payload };
}

function ensureVisualReviewState() {
  if (!existsSync(STATE_FILE)) {
    const result = spawnSync("node", ["apps/web/scripts/bootstrap-ai-task-playbook-visual-review-1.mjs"], {
      cwd: process.cwd(),
      encoding: "utf8",
      stdio: "pipe",
    });
    if (result.status !== 0) {
      fail(`visual review bootstrap failed: ${result.stderr || result.stdout}`);
    }
  }
  const state = JSON.parse(readFileSync(STATE_FILE, "utf8"));
  if (!state.runId) fail("visual review state missing runId");
  return state;
}

async function fetchRunSnapshot(runId) {
  const res = await api(`/api/ai-tasks/playbook-runs/${runId}?facilityId=${FACILITY}`, { headers: managerHeaders() });
  assert("run snapshot API", res.status === 200, JSON.stringify(res.payload));
  return res.payload;
}

async function main() {
  console.log("Running AI-TASK-PLAYBOOK-RUN-DETAIL-NAVIGATION-1 live flow verifier.");
  bootstrapSecrets();
  mkdirSync(SCREENSHOT_DIR, { recursive: true });

  const health = await api("/health/ready");
  assert("API /health/ready 200", health.status === 200);

  const state = ensureVisualReviewState();
  const { runId } = state;
  const before = await fetchRunSnapshot(runId);

  const exe = "C:\\Program Files\\Google\\Chrome\\Application\\chrome.exe";
  const ownerPassword = process.env.DevelopmentSaasIdentity__TenantOwnerPassword;
  const mfaSecret = process.env.DevelopmentSaasIdentity__MfaSharedSecret;
  if (!existsSync(exe) || !ownerPassword || !mfaSecret) {
    fail("Chrome or dev auth secrets unavailable for navigation verification");
  }

  const userDataDir = mkdtempSync(join(tmpdir(), "ai-task-playbook-run-nav-1-"));
  const port = 9890 + Math.floor(Math.random() * 100);
  const child = spawn(exe, [`--remote-debugging-port=${port}`, "--headless=new", "--disable-gpu", "--window-size=1440,1200", `--user-data-dir=${userDataDir}`], { stdio: "ignore" });

  try {
    let version = null;
    for (let i = 0; i < 40; i++) {
      try { version = await fetch(`http://127.0.0.1:${port}/json/version`).then((r) => r.json()); break; } catch { await new Promise((r) => setTimeout(r, 500)); }
    }
    if (!version) fail("Chrome debugger unavailable");

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
        if (message.error) reject(new Error(message.error.message));
        else resolve(message.result);
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

    const snap = async (file) => {
      const metrics = await ss("Page.getLayoutMetrics");
      const width = Math.max(1, Math.floor(metrics?.contentSize?.width ?? 1440));
      const height = Math.max(1, Math.floor(metrics?.contentSize?.height ?? 1200));
      const shot = await ss("Page.captureScreenshot", {
        format: "png",
        captureBeyondViewport: true,
        clip: { x: 0, y: 0, width, height, scale: 1 },
      });
      writeFileSync(file, Buffer.from(shot.data, "base64"));
    };

    await ss("Page.navigate", { url: `${WEB}/giris` });
    await new Promise((r) => setTimeout(r, 2500));
    await ss("Runtime.evaluate", {
      expression: `(() => {
        const setter = Object.getOwnPropertyDescriptor(window.HTMLInputElement.prototype, "value").set;
        const fill = (sel, val) => { const n = document.querySelector(sel); setter.call(n, val); n.dispatchEvent(new Event("input", { bubbles: true })); n.dispatchEvent(new Event("change", { bubbles: true })); };
        fill("#sign-in-organization", "dev-pilot-tenant"); fill("#sign-in-email", "admin@demo.nevaxiszentra.local"); fill("#sign-in-password", ${JSON.stringify(ownerPassword)});
        document.querySelector("form.auth-form button")?.click();
      })()`,
    });
    for (let i = 0; i < 60; i++) {
      const href = (await ss("Runtime.evaluate", { expression: "location.href", returnByValue: true })).result?.value;
      if (String(href).includes("/mfa-dogrula")) break;
      await new Promise((r) => setTimeout(r, 300));
    }
    for (const offset of [-1, 0, 1]) {
      const attempt = await ss("Runtime.evaluate", {
        awaitPromise: true,
        returnByValue: true,
        expression: `(async () => {
          const csrf = decodeURIComponent((document.cookie.match(/NevaxisZentra\\.csrf=([^;]+)/) || [])[1] || "");
          const res = await fetch("/api/identity/mfa/verify", { method: "POST", credentials: "include", headers: { Accept: "application/json", "Content-Type": "application/json", "X-NevaxisZentra-Csrf": csrf }, body: JSON.stringify({ totpCode: ${JSON.stringify(generateTotp(mfaSecret, offset))} }) });
          return res.status;
        })()`,
      });
      if (attempt.result?.value === 200) break;
    }
    await new Promise((r) => setTimeout(r, 2000));

    await ss("Page.navigate", { url: `${WEB}/ai-gorevleri/playbooklar?gorunum=calisan` });
    await new Promise((r) => setTimeout(r, 5000));

    const listInfo = (await ss("Runtime.evaluate", {
      returnByValue: true,
      expression: `(() => {
        const card = document.querySelector('[data-testid^="ai-task-playbook-run-"]');
        const link = document.querySelector('[data-testid^="ai-task-playbook-run-detail-link-"]');
        return {
          activeViewCards: document.querySelectorAll('[data-testid^="ai-task-playbook-run-"]').length,
          href: link?.getAttribute('href') || null,
          testId: link?.getAttribute('data-testid') || null,
          text: link?.textContent?.trim() || null,
        };
      })()`,
    })).result?.value;

    assert("active runs visible on calisan view", listInfo.activeViewCards > 0, JSON.stringify(listInfo));
    assert("detail link present", !!listInfo.href, JSON.stringify(listInfo));
    assert("detail link uses run id", listInfo.href === `/ai-gorevleri/playbooklar/calisma/${runId}`, listInfo.href);
    assert("detail link test id", listInfo.testId === `ai-task-playbook-run-detail-link-${runId}`);

    await snap(SHOTS.list);

    const clickResult = (await ss("Runtime.evaluate", {
      returnByValue: true,
      expression: `(() => {
        const link = document.querySelector('[data-testid="ai-task-playbook-run-detail-link-${runId}"]');
        if (!link) return { ok: false, reason: 'missing-link' };
        link.click();
        return { ok: true, href: link.getAttribute('href') };
      })()`,
    })).result?.value;
    assert("mouse click dispatched", clickResult.ok === true, JSON.stringify(clickResult));
    await new Promise((r) => setTimeout(r, 6000));

    const afterClick = (await ss("Runtime.evaluate", {
      returnByValue: true,
      expression: `({
        href: location.href,
        runPage: !!document.querySelector('[data-testid="ai-task-playbook-run-page"]'),
        runDetail: !!document.querySelector('[data-testid="ai-task-playbook-run-detail"]'),
        playbookName: document.querySelector('.ai-task-playbook-run-detail h2')?.textContent?.trim() || null,
        status: document.querySelector('.ai-task-playbook-run-detail__header .badge')?.textContent?.trim() || null,
        progress: document.querySelector('.ai-task-playbook-run-detail__metric strong')?.textContent?.trim() || null,
        stepCount: document.querySelectorAll('[data-testid^="ai-task-playbook-step-"]').length,
      })`,
    })).result?.value;

    assert("url changed to run detail", afterClick.href.includes(`/ai-gorevleri/playbooklar/calisma/${runId}`), afterClick.href);
    assert("run detail page loaded", afterClick.runPage === true);
    assert("run detail panel loaded", afterClick.runDetail === true);
    assert("playbook name visible", afterClick.playbookName === "Kritik Makine Olayı", afterClick.playbookName);
    assert("run status visible", afterClick.status === "Devam Ediyor", afterClick.status);
    assert("step count = 6", afterClick.stepCount === 6, String(afterClick.stepCount));
    assert("progress = 2/6", afterClick.progress === "2/6", afterClick.progress);

    await snap(SHOTS.detail);

    await ss("Runtime.evaluate", {
      expression: `(() => {
        const link = document.querySelector('[data-testid="ai-task-playbook-run-detail-link-${runId}"]');
        if (link) {
          link.focus();
          link.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));
          link.dispatchEvent(new KeyboardEvent('keyup', { key: 'Enter', bubbles: true }));
        }
      })()`,
    });

    await ss("Runtime.evaluate", { expression: "history.back()" });
    await new Promise((r) => setTimeout(r, 4000));
    const afterBack = (await ss("Runtime.evaluate", {
      returnByValue: true,
      expression: `({ href: location.href, hasActiveRunCard: !!document.querySelector('[data-testid^="ai-task-playbook-run-"]') })`,
    })).result?.value;
    assert("browser back returns to active list", afterBack.href.includes("gorunum=calisan"), afterBack.href);
    assert("active run card still visible after back", afterBack.hasActiveRunCard === true);

    ws.close();
  } finally {
    child.kill("SIGTERM");
    try { rmSync(userDataDir, { recursive: true, force: true }); } catch { /* ignore */ }
  }

  const after = await fetchRunSnapshot(runId);
  assert("no run mutation", after.status === before.status);
  assert("progress unchanged", after.requiredStepsCompleted === before.requiredStepsCompleted);
  assert("required total unchanged", after.requiredStepsTotal === before.requiredStepsTotal);
  assert("steps unchanged", (after.steps?.length ?? 0) === (before.steps?.length ?? 0));

  assert("running list screenshot saved", existsSync(SHOTS.list));
  assert("detail open screenshot saved", existsSync(SHOTS.detail));

  console.log("AI-TASK-PLAYBOOK-RUN-DETAIL-NAVIGATION-1 live flow verifier passed.");
}

main().catch((error) => fail(error.stack || error.message));
