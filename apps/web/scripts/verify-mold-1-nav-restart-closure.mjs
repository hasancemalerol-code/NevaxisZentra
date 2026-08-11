#!/usr/bin/env node
/**
 * MOLD-1 navigation restart closure proof.
 * Verifies active DB (5433/crevafactos_local), mold permissions/migration,
 * and live browser sidebar order after restart cycles.
 */
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";
import { chromium } from "playwright";

const ROOT = process.env.MOLD_CLOSURE_ROOT ?? resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const WEB_ROOT = resolve(ROOT, "apps/web");
const ENV_LOCAL = resolve(WEB_ROOT, ".env.local");
const MANAGER_SUBJECT = "production-pilot-operator";
const API_BASE = process.env.CREVAFACTOS_API_BASE_URL ?? "http://localhost:5000";
const WEB_BASE = process.env.MOLD_BROWSER_BASE_URL ?? "http://localhost:3001";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const TENANT = "dev-pilot-tenant";

const ACTORS = {
  manager: "production-pilot-operator",
  readonly: "planning-pilot-reviewer",
  unauthorized: "quality-negative-test-user",
};

const evidence = {
  activeDatabaseHost: "127.0.0.1",
  activeDatabasePort: 5433,
  activeDatabaseName: "crevafactos_local",
  apiPersistenceConfigVerified: false,
  migrationPresent: false,
  moldReadPermissionPresent: false,
  moldManagePermissionPresent: false,
  moldReadRoleGrants: 0,
  moldManageRoleGrants: 0,
  cycles: {},
};

function fail(message) {
  console.error(`MOLD-1 nav-restart-closure failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function db(sql) {
  const result = spawnSync(
    "docker",
    ["exec", "crevafactos-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

function ps(headers, path, method = "GET", body) {
  const args = [
    "-NoProfile",
    "-Command",
    `$h=@{'X-CrevaFactOS-Dev-Issuer'='crevafactos-dev';'X-CrevaFactOS-Dev-Subject'='${headers.subject}';'X-CrevaFactOS-Tenant'='${headers.tenant}';'Content-Type'='application/json'}; try { (Invoke-WebRequest -Uri '${API_BASE}${path}' -Method ${method} -Headers $h ${body ? `-Body '${JSON.stringify(body)}'` : ""} -UseBasicParsing).StatusCode } catch { $_.Exception.Response.StatusCode.value__ }`,
  ];
  const result = spawnSync("powershell", args, { encoding: "utf8" });
  return Number(result.stdout.trim());
}

async function waitForReady(timeoutMs = 60000) {
  const started = Date.now();
  while (Date.now() - started < timeoutMs) {
    try {
      const response = await fetch(`${API_BASE}/health/ready`);
      if (response.status === 200) return true;
    } catch {
      // retry
    }
    await new Promise((resolve) => setTimeout(resolve, 500));
  }
  return false;
}

function getListeningPid(port) {
  const result = spawnSync("powershell", [
    "-NoProfile",
    "-Command",
    `(Get-NetTCPConnection -LocalPort ${port} -State Listen -ErrorAction SilentlyContinue | Select-Object -First 1).OwningProcess`,
  ], { encoding: "utf8" });
  const pid = Number(result.stdout.trim());
  return Number.isFinite(pid) && pid > 0 ? pid : null;
}

function stopPid(pid, label) {
  if (!pid) return;
  spawnSync("powershell", ["-NoProfile", "-Command", `Stop-Process -Id ${pid} -Force -ErrorAction SilentlyContinue`], { stdio: "inherit" });
  console.log(`Stopped ${label} PID ${pid}`);
}

function startApi() {
  return spawnSync("powershell", [
    "-NoProfile",
    "-Command",
    `Start-Process -WindowStyle Hidden -WorkingDirectory '${ROOT}' -FilePath 'dotnet' -ArgumentList 'run','--project','apps/api/CrevaFactOS.Api.csproj','--no-build'`,
  ], { encoding: "utf8" });
}

function buildApi() {
  const result = spawnSync("dotnet", ["build", "apps/api/CrevaFactOS.Api.csproj", "--no-restore"], {
    cwd: ROOT,
    encoding: "utf8",
  });
  return result.status === 0;
}

function startWeb() {
  startManagerWeb();
}

async function waitForWeb(timeoutMs = 90000) {
  const started = Date.now();
  while (Date.now() - started < timeoutMs) {
    try {
      const response = await fetch(`${WEB_BASE}/dashboard`);
      if (response.status === 200) return true;
    } catch {
      // retry
    }
    await new Promise((resolve) => setTimeout(resolve, 1000));
  }
  return false;
}

function startRoleWeb(subject, port = 3001) {
  stopPid(getListeningPid(port), `web:${port}`);
  const subjectArg = subject ? `set CREVAFACTOS_DEV_AUTH_SUBJECT=${subject}&& ` : "";
  spawnSync("powershell", [
    "-NoProfile",
    "-Command",
    `Start-Process -WindowStyle Hidden -WorkingDirectory '${WEB_ROOT}' -FilePath 'cmd' -ArgumentList '/c','${subjectArg}npx next dev -p ${port}'`,
  ], { encoding: "utf8" });
}

function startManagerWeb() {
  startRoleWeb(null, 3001);
}

async function waitForWebPort(port, timeoutMs = 90000) {
  const started = Date.now();
  while (Date.now() - started < timeoutMs) {
    try {
      const response = await fetch(`http://localhost:${port}/dashboard`);
      if (response.status === 200) return true;
    } catch {
      // retry
    }
    await new Promise((resolve) => setTimeout(resolve, 1000));
  }
  return false;
}

function envLocalSubjectUnchanged() {
  const content = readFileSync(ENV_LOCAL, "utf8");
  return /CREVAFACTOS_DEV_AUTH_SUBJECT=production-pilot-operator/.test(content);
}

async function browserSidebarProof(role, baseUrl = WEB_BASE) {
  const browser = await chromium.launch({ headless: true });
  try {
    const page = await (await browser.newContext({ viewport: { width: 1600, height: 900 } })).newPage();
    const consoleErrors = [];
    page.on("console", (msg) => {
      if (msg.type() === "error") consoleErrors.push(msg.text());
    });

    await page.goto(`${baseUrl}/dashboard`, { waitUntil: "networkidle" });
    const nav = page.locator("nav[aria-label='Birincil navigasyon']");
    const factoryLabels = [];
    let inFactory = false;
    for (const group of await nav.locator("section.navigation-group").all()) {
      const groupLabel = await group.locator(".navigation-group-label").textContent();
      if (groupLabel?.includes("Fabrika operasyonları")) inFactory = true;
      else if (inFactory && groupLabel && !groupLabel.includes("Fabrika")) break;
      if (inFactory) factoryLabels.push(...await group.locator(".navigation-label").allTextContents());
    }

    const kaliplarVisible = await nav.getByRole("link", { name: "Kalıplar" }).isVisible().catch(() => false);
    const placementCorrect =
      factoryLabels.indexOf("Kalıplar") === factoryLabels.indexOf("Makineler") + 1 &&
      factoryLabels.indexOf("Bakım") === factoryLabels.indexOf("Kalıplar") + 1;

    let directRouteStatus = null;
    if (role === "unauthorized") {
      await page.goto(`${baseUrl}/kaliplar`, { waitUntil: "networkidle" });
      directRouteStatus = await page.getByText("Bu modüle erişim yetkiniz yok").isVisible();
    } else if (kaliplarVisible) {
      await nav.getByRole("link", { name: "Kalıplar" }).click();
      await page.waitForURL("**/kaliplar");
    }

    return {
      role,
      kaliplarVisible,
      placementCorrect,
      factoryLabels,
      directRouteUnauthorized: directRouteStatus,
      serializationErrors: consoleErrors.filter((t) => t.includes("Only plain objects") || t.includes("Client Components")),
      hydrationWarnings: consoleErrors.filter((t) => /hydration/i.test(t)),
    };
  } finally {
    await browser.close();
  }
}

async function proveRolesForCycle(cycleName) {
  assert(`${cycleName}: shared .env.local unchanged`, envLocalSubjectUnchanged());

  const cycle = {};
  assert(`${cycleName}: manager web ready on 3001`, await waitForWeb());
  cycle.manager = await browserSidebarProof("manager", WEB_BASE);
  assert(`${cycleName}: manager Kalıplar visible`, cycle.manager.kaliplarVisible === true);
  assert(`${cycleName}: manager placement Makineler → Kalıplar → Bakım`, cycle.manager.placementCorrect === true);

  startRoleWeb(ACTORS.readonly, 3001);
  assert(`${cycleName}: readonly process ready`, await waitForWeb());
  cycle.readonly = await browserSidebarProof("readonly", WEB_BASE);
  assert(`${cycleName}: readonly Kalıplar visible`, cycle.readonly.kaliplarVisible === true);
  stopPid(getListeningPid(3001), "web:3001");

  startRoleWeb(ACTORS.unauthorized, 3001);
  assert(`${cycleName}: unauthorized process ready`, await waitForWeb());
  cycle.unauthorized = await browserSidebarProof("unauthorized", WEB_BASE);
  assert(`${cycleName}: unauthorized Kalıplar hidden`, cycle.unauthorized.kaliplarVisible === false);
  assert(`${cycleName}: unauthorized direct route blocked`, cycle.unauthorized.directRouteUnauthorized === true);
  stopPid(getListeningPid(3001), "web:3001");

  startManagerWeb();
  assert(`${cycleName}: manager web restored on 3001`, await waitForWeb());
  assert(`${cycleName}: shared .env.local still unchanged`, envLocalSubjectUnchanged());

  evidence.cycles[cycleName] = cycle;
  return cycle;
}

function verifyActiveDatabaseAndPermissions() {
  const apiSettings = readFileSync(resolve(ROOT, "apps/api/appsettings.Development.json"), "utf8");
  evidence.apiPersistenceConfigVerified =
    apiSettings.includes('"Host": "127.0.0.1"') &&
    apiSettings.includes('"Port": 5433') &&
    apiSettings.includes('"Database": "crevafactos_local"');
  assert("API persistence targets 127.0.0.1:5433/crevafactos_local", evidence.apiPersistenceConfigVerified);

  evidence.migrationPresent = db(
    "SELECT COUNT(*) FROM \"__EFMigrationsHistory\" WHERE \"MigrationId\" = '20260811200000_AddMoldRegistryLifecycleFoundation'",
  ) === "1";
  assert("Mold migration present in active DB", evidence.migrationPresent);

  const permissions = db("SELECT key FROM permission_definitions WHERE key IN ('mold.read','mold.manage') ORDER BY key");
  evidence.moldReadPermissionPresent = permissions.includes("mold.read");
  evidence.moldManagePermissionPresent = permissions.includes("mold.manage");
  assert("mold.read permission definition present", evidence.moldReadPermissionPresent);
  assert("mold.manage permission definition present", evidence.moldManagePermissionPresent);

  evidence.moldReadRoleGrants = Number(db(
    "SELECT COUNT(*) FROM tenant_role_permissions trp JOIN permission_definitions pd ON pd.id = trp.permission_definition_id WHERE pd.key = 'mold.read'",
  ));
  evidence.moldManageRoleGrants = Number(db(
    "SELECT COUNT(*) FROM tenant_role_permissions trp JOIN permission_definitions pd ON pd.id = trp.permission_definition_id WHERE pd.key = 'mold.manage'",
  ));
  assert("mold.read role grants present", evidence.moldReadRoleGrants > 0, `count=${evidence.moldReadRoleGrants}`);
  assert("mold.manage role grants present", evidence.moldManageRoleGrants > 0, `count=${evidence.moldManageRoleGrants}`);

  assert("manager workspace-summary authorized", ps({ subject: ACTORS.manager, tenant: TENANT }, `/api/molds/workspace-summary?facilityId=${FACILITY}`) === 200);
  assert("readonly workspace-summary authorized", ps({ subject: ACTORS.readonly, tenant: TENANT }, `/api/molds/workspace-summary?facilityId=${FACILITY}`) === 200);
  assert("unauthorized workspace-summary denied", ps({ subject: ACTORS.unauthorized, tenant: TENANT }, `/api/molds/workspace-summary?facilityId=${FACILITY}`) === 403);
  assert("shared .env.local uses manager actor", envLocalSubjectUnchanged());
}

console.log("Running MOLD-1 nav-restart-closure proof.");

verifyActiveDatabaseAndPermissions();

assert("API ready (standard start baseline)", await waitForReady());
await proveRolesForCycle("standard_api_start");

const apiPid = getListeningPid(5000);
stopPid(apiPid, "api");
assert("API stopped for restart cycle", getListeningPid(5000) === null);
startApi();
assert("API ready after restart", await waitForReady());
await proveRolesForCycle("api_restart");

const webPid = getListeningPid(3001);
stopPid(webPid, "web");
startWeb();
assert("Web ready after web-only restart", await waitForWeb());
assert("API still ready after web restart", await waitForReady());
await proveRolesForCycle("web_restart");

stopPid(getListeningPid(5000), "api");
assert("narrow API build", buildApi());
startApi();
assert("API ready after narrow build restart", await waitForReady());
await proveRolesForCycle("narrow_build_api_restart");

console.log("MOLD-1-NAV-RESTART-CLOSURE evidence:");
console.log(JSON.stringify(evidence, null, 2));
console.log("MOLD-1 nav-restart-closure proof passed.");
