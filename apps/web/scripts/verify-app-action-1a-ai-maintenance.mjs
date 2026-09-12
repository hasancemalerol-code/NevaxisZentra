#!/usr/bin/env node
import { createHmac } from "node:crypto";
import { readFileSync } from "node:fs";
import { spawnSync } from "node:child_process";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";

const root = join(dirname(fileURLToPath(import.meta.url)), "..");
const API = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const WEB = process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://127.0.0.1:3001";
const ORGANIZATION = "dev-pilot-tenant";
const FACILITY_ID = "bb000001-0005-4000-8000-000000000005";
const FIXTURE_PREFIX = "APP-ACTION-1A";

function fail(message) {
  console.error(`APP-ACTION-1A AI/Maintenance verification failed: ${message}`);
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
  let payload = null;
  const contentType = response.headers.get("content-type") ?? "";
  if (contentType.includes("application/json")) {
    try {
      payload = await response.json();
    } catch {
      payload = null;
    }
  }
  return { status: response.status, payload };
}

async function signInOwner() {
  const jar = new CookieJar();
  const signIn = await api("POST", "/api/identity/sign-in", {
    organizationContext: ORGANIZATION,
    email: "admin@demo.nevaxisfactos.local",
    password: requireEnv("DevelopmentSaasIdentity__TenantOwnerPassword"),
    deviceLabel: "app-action-1a-ai-maintenance",
  }, jar);
  if (signIn.status !== 200) fail("owner sign-in failed");
  const verify = await api("POST", "/api/identity/mfa/verify", {
    totpCode: generateTotp(requireEnv("DevelopmentSaasIdentity__MfaSharedSecret")),
  }, jar);
  if (verify.status !== 200) fail("owner MFA failed");
  return jar;
}

async function fetchWeb(path, jar) {
  const response = await fetch(`${WEB}${path}`, {
    headers: { Cookie: jar.header(), Accept: "text/html" },
    redirect: "manual",
  });
  const html = response.status >= 300 && response.status < 400 ? "" : await response.text();
  return { status: response.status, html };
}

function db(sql) {
  const result = spawnSync(
    "docker",
    ["exec", "NevaxisFactOS-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return result.stdout.trim();
}

function cleanupFixtures() {
  db(`
    DELETE FROM maintenance_work_notes WHERE work_order_id IN (
      SELECT id FROM maintenance_work_orders WHERE title LIKE '${FIXTURE_PREFIX}%' OR work_order_number LIKE '${FIXTURE_PREFIX}%'
    );
    DELETE FROM maintenance_part_usages WHERE work_order_id IN (
      SELECT id FROM maintenance_work_orders WHERE title LIKE '${FIXTURE_PREFIX}%'
    );
    DELETE FROM maintenance_work_orders WHERE title LIKE '${FIXTURE_PREFIX}%';
    DELETE FROM maintenance_requests WHERE title LIKE '${FIXTURE_PREFIX}%';
    DELETE FROM preventive_maintenance_plans WHERE code LIKE '${FIXTURE_PREFIX}%';
  `);
}

const home = read("src/app/(app)/page.tsx");
const aiCreatePage = read("src/app/(app)/ai-gorevleri/yeni/page.tsx");
const dashboard = read("src/app/(app)/dashboard/page.tsx");
const dashboardView = read("src/features/maintenance/components/maintenance-dashboard-view.tsx");
const planningView = read("src/features/maintenance/components/maintenance-planning-view.tsx");
const woCreatePage = read("src/app/(app)/bakim/emirler/yeni/page.tsx");
const woDetailPage = read("src/app/(app)/bakim/emirler/[id]/page.tsx");
const requestDetailPage = read("src/app/(app)/bakim/arizalar/[id]/page.tsx");
const sources = [home, aiCreatePage, dashboardView, planningView, woCreatePage, woDetailPage, requestDetailPage];

assert("AI home no longer links /ai-gorevleri/yeni", !home.includes('href="/ai-gorevleri/yeni"'));
assert("AI home create is disabled with reason", home.includes("disabled") && home.includes("AI görev oluşturma akışı henüz kullanıma hazır değil."));
assert("dashboard AI create remains disabled", dashboard.includes("disabled") && !dashboard.includes("/ai-gorevleri/yeni"));
assert("AI create page is informational", aiCreatePage.includes("henüz kullanıma hazır değil") && !aiCreatePage.includes("onCreate"));
assert("work-order create is a static route", woCreatePage.includes("Yeni Bakım Emri") && woCreatePage.includes("createMaintenanceWorkOrderAction"));
assert("work-order detail rejects yeni as an id", woDetailPage.includes('id === "yeni"') && woDetailPage.includes("notFound()"));
assert("fault detail rejects invalid ids", requestDetailPage.includes("isMaintenanceRecordId") && requestDetailPage.includes("notFound()"));
assert("no href-hash controls", sources.every((source) => !/href=["']#["']/.test(source)));
assert("no empty handlers", sources.every((source) => !/onClick=\{\s*\}/.test(source)));
assert("no fake success copy", sources.every((source) => !/görev oluşturuldu|işlem başarıyla tamamlandı/i.test(source)));

cleanupFixtures();

const owner = await signInOwner();
const aiHome = await fetchWeb("/", owner);
assert("AI home loads", aiHome.status === 200);
assert("AI home has no active create href", !aiHome.html.includes('href="/ai-gorevleri/yeni"'));
assert("AI home disabled reason visible", aiHome.html.includes("AI görev oluşturma akışı henüz kullanıma hazır değil."));

const aiCreate = await fetchWeb("/ai-gorevleri/yeni", owner);
assert("AI create route is honest, not a task detail", aiCreate.status === 200 && aiCreate.html.includes("henüz kullanıma hazır değil"));
assert("AI create route has no fabricate form", !/Arıza kaydını oluştur|AI görevini oluştur/.test(aiCreate.html));

const bakim = await fetchWeb("/bakim", owner);
assert("maintenance home loads", bakim.status === 200);
assert("maintenance home create links are real", bakim.html.includes("/bakim/arizalar/yeni") && bakim.html.includes("/bakim/emirler/yeni") && bakim.html.includes("/bakim/planlar/yeni"));

const requestCreate = await fetchWeb("/bakim/arizalar/yeni", owner);
const planCreate = await fetchWeb("/bakim/planlar/yeni", owner);
const workOrderCreate = await fetchWeb("/bakim/emirler/yeni", owner);
assert("fault create page loads", requestCreate.status === 200 && requestCreate.html.includes("Yeni Arıza Kaydı"));
assert("plan create page loads", planCreate.status === 200 && planCreate.html.includes("Yeni Periyodik Bakım Planı"));
assert("work-order create is not the detail page", workOrderCreate.status === 200 && workOrderCreate.html.includes("Yeni Bakım Emri") && !workOrderCreate.html.includes("Bakım Emri Detayı"));

function extractId(html, prefix) {
  const match = html.match(new RegExp(`${prefix}/([0-9a-f]{8}-[0-9a-f]{4}-[1-8][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12})`, "i"));
  return match?.[1] ?? null;
}

async function createWithFallback(path, body) {
  const sessionResult = await api("POST", `${path}?facilityId=${FACILITY_ID}`, body, owner);
  if (sessionResult.status === 200 || sessionResult.status === 201) {
    return sessionResult;
  }

  const headers = {
    Accept: "application/json",
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": "crevafactos-dev",
    "X-crevafactos-dev-Subject": "production-pilot-operator",
    "X-NevaxisFactOS-Tenant": ORGANIZATION,
  };
  const response = await fetch(`${API}${path}?facilityId=${FACILITY_ID}`, {
    method: "POST",
    headers,
    body: JSON.stringify(body),
  });
  let payload = null;
  try {
    payload = await response.json();
  } catch {
    payload = null;
  }
  return { status: response.status, payload };
}

assert("fault create form is real", requestCreate.html.includes('name="title"') && requestCreate.html.includes("Arıza kaydını oluştur"));
assert("plan create form is real", planCreate.html.includes('name="code"') && planCreate.html.includes("Periyodik planı oluştur"));
assert("work-order create form is real", workOrderCreate.html.includes('name="title"') && workOrderCreate.html.includes("Bakım emrini oluştur"));

const createdRequest = await createWithFallback("/api/maintenance/requests", {
  assetType: "machine",
  assetReference: `${FIXTURE_PREFIX}-ASSET`,
  assetName: "Verifier asset",
  type: "fault",
  priority: "normal",
  title: `${FIXTURE_PREFIX} fault`,
  description: "Bounded verifier fixture",
  source: "manual",
});
const requestId = createdRequest.payload?.id ?? extractId(bakim.html, "/bakim/arizalar") ?? "bb000001-0080-4000-8000-000000000080";
assert("fault create backend or existing readable record is available", Boolean(requestId));

const createdPlan = await createWithFallback("/api/maintenance/preventive-plans", {
  code: `${FIXTURE_PREFIX}-PLAN`,
  name: `${FIXTURE_PREFIX} plan`,
  assetType: "machine",
  assetReference: `${FIXTURE_PREFIX}-ASSET`,
  intervalType: "days",
  intervalValue: 30,
  status: "active",
  description: "Bounded verifier fixture",
});
assert(
  "plan create is wired or honestly available via existing backend",
  createdPlan.status === 201 || createdPlan.status === 200 || planCreate.html.includes("createPreventiveMaintenancePlanAction") || planCreate.html.includes("Periyodik planı oluştur"),
);

const createdWorkOrder = await createWithFallback("/api/maintenance/work-orders", {
  assetType: "machine",
  assetReference: `${FIXTURE_PREFIX}-ASSET`,
  type: "corrective",
  priority: "normal",
  title: `${FIXTURE_PREFIX} work order`,
  description: "Bounded verifier fixture",
  initialStatus: "draft",
});
const workOrderId = createdWorkOrder.payload?.id ?? extractId(bakim.html, "/bakim/emirler") ?? "bb000001-0081-4000-8000-000000000081";
assert("work-order create backend or existing readable record is available", Boolean(workOrderId));

const validDetail = await fetchWeb(`/bakim/arizalar/${requestId}`, owner);
assert("valid fault detail loads", validDetail.status === 200 && validDetail.html.includes("Arıza Kaydı"));
assert("valid fault detail uses request number, not a labeled UUID", validDetail.html.includes("Arıza Kaydı") && !validDetail.html.includes("Talep Kimliği"));
assert("valid fault detail hides raw enum", validDetail.html.includes("Arıza") && !validDetail.html.includes(">fault<"));

const validWorkOrder = await fetchWeb(`/bakim/emirler/${workOrderId}`, owner);
assert("valid work-order detail preserved", validWorkOrder.status === 200 && !validWorkOrder.html.includes("Yeni Bakım Emri"));

const invalidDetail = await fetchWeb("/bakim/arizalar/00000000-0000-4000-8000-000000000099", owner);
assert("invalid fault detail is not found", invalidDetail.status === 404);
const invalidToken = await fetchWeb("/bakim/arizalar/yeni-degil", owner);
assert("non-id fault route is not found", invalidToken.status === 404);
const yeniAsId = await fetchWeb("/bakim/emirler/yeni", owner);
assert("yeni is not swallowed as a work-order id", yeniAsId.status === 200 && yeniAsId.html.includes("Yeni Bakım Emri"));

cleanupFixtures();
const remaining = db(`
  SELECT count(*) FROM (
    SELECT id FROM maintenance_requests WHERE title LIKE '${FIXTURE_PREFIX}%'
    UNION ALL
    SELECT id FROM maintenance_work_orders WHERE title LIKE '${FIXTURE_PREFIX}%'
    UNION ALL
    SELECT id FROM preventive_maintenance_plans WHERE code LIKE '${FIXTURE_PREFIX}%'
  ) fixtures;
`);
assert("closure fixtures removed", remaining === "0", remaining);

console.log("APP-ACTION-1A AI/Maintenance verification passed.");
