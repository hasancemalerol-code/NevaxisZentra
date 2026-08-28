#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const API = process.env.CREVAFACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const WEB = (process.env.CREVAFACTOS_WEB_BASE_URL ?? "http://localhost:3001").replace("127.0.0.1", "localhost").replace(/\/$/, "");
const ROUTE = "/ticari-zeka/malzeme-ve-maliyet-karliligi";
const PRODUCT_ROUTE = "/ticari-zeka/urun-karliligi";
const CUSTOMER_ROUTE = "/ticari-zeka/musteri-karliligi";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const UNAUTH_FACILITY = "bb000001-0005-4000-8000-000000000099";
const MATERIAL_RUN = "bb000001-03b7-4000-8000-0000000003b7";
const COST_RUN = "bb000001-03b4-4000-8000-0000000003b4";
const INVALID = "00000000-0000-4000-8000-000000000099";
const MANAGER = {
  issuer: "crevafactos-dev",
  subject: "planning-pilot-user",
  tenant: "dev-pilot-tenant",
};
const READONLY = {
  issuer: "crevafactos-dev",
  subject: "planning-pilot-reviewer",
  tenant: "dev-pilot-tenant",
};

function fail(message) {
  console.error(`COMMERCIAL-PROFITABILITY-UI-5 permission regression verifier failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function read(rel) {
  return readFileSync(resolve(ROOT, rel), "utf8").replaceAll("\r\n", "\n");
}

function actorHeaders(actor) {
  return {
    Accept: "application/json",
    "Content-Type": "application/json",
    "X-CrevaFactOS-Dev-Issuer": actor.issuer,
    "X-CrevaFactOS-Dev-Subject": actor.subject,
    "X-CrevaFactOS-Tenant": actor.tenant,
  };
}

class CookieJar {
  constructor() { this.cookies = new Map(); }
  header() { return [...this.cookies.entries()].map(([name, value]) => `${name}=${value}`).join("; "); }
}

async function api(method, path, body, headers = actorHeaders(MANAGER)) {
  const requestHeaders = { ...headers };
  if (body) requestHeaders["Content-Type"] = "application/json";
  const response = await fetch(`${API}${path}`, { method, headers: requestHeaders, body: body ? JSON.stringify(body) : undefined });
  const raw = await response.text();
  let payload = null;
  try { payload = raw ? JSON.parse(raw) : null; } catch { payload = { raw: raw.slice(0, 300) }; }
  return { status: response.status, payload, raw };
}

async function fetchWeb(path, jar = new CookieJar()) {
  const response = await fetch(`${WEB}${path}`, { headers: { Cookie: jar.header(), Accept: "text/html" }, redirect: "manual" });
  const html = response.status >= 300 && response.status < 400 ? "" : await response.text();
  return { status: response.status, html, location: response.headers.get("location") };
}

console.log("Running COMMERCIAL-PROFITABILITY-UI-5 permission regression verifier.");

const pageSource = read("apps/web/src/app/(app)/ticari-zeka/malzeme-ve-maliyet-karliligi/page.tsx");
const productPage = read("apps/web/src/app/(app)/ticari-zeka/urun-karliligi/page.tsx");
const customerPage = read("apps/web/src/app/(app)/ticari-zeka/musteri-karliligi/page.tsx");
const queries = read("apps/web/src/features/commercial-intelligence/server/commercial-material-cost-profitability-queries.ts");

assert("page requires commercial-intelligence.read", pageSource.includes('forbidUnlessGranted("commercial-intelligence.read")'));
assert("product workspace preserved", productPage.includes("CommercialProfitabilityWorkspaceView"));
assert("customer workspace preserved", customerPage.includes("CommercialCustomerProfitabilityWorkspaceView"));
assert("material type lock", queries.includes("PROFITABILITY_MATERIAL_CONTRIBUTION_TYPE"));
assert("cost type lock", queries.includes("PROFITABILITY_COST_COMPONENT_MIX_TYPE"));

const ready = await fetch(`${API}/health/ready`);
assert("API ready", ready.status === 200, String(ready.status));

const noAuthMaterial = await fetch(`${API}/api/commercial-intelligence/profitability/comparisons?facilityId=${FACILITY}&comparisonType=material-contribution-position`);
assert("missing auth material list 401", noAuthMaterial.status === 401, String(noAuthMaterial.status));

const noAuthCost = await fetch(`${API}/api/commercial-intelligence/profitability/comparisons?facilityId=${FACILITY}&comparisonType=cost-component-mix-change`);
assert("missing auth cost list 401", noAuthCost.status === 401, String(noAuthCost.status));

const readonlyMaterial = await api(
  "GET",
  `/api/commercial-intelligence/profitability/comparisons?facilityId=${FACILITY}&comparisonType=material-contribution-position`,
  undefined,
  actorHeaders(READONLY),
);
assert("readonly material list allowed", readonlyMaterial.status === 200, String(readonlyMaterial.status));

const readonlyCost = await api(
  "GET",
  `/api/commercial-intelligence/profitability/comparisons?facilityId=${FACILITY}&comparisonType=cost-component-mix-change`,
  undefined,
  actorHeaders(READONLY),
);
assert("readonly cost list allowed", readonlyCost.status === 200, String(readonlyCost.status));

const materialList = await api(
  "GET",
  `/api/commercial-intelligence/profitability/comparisons?facilityId=${FACILITY}&comparisonType=material-contribution-position`,
);
assert("authorized material list 200", materialList.status === 200, materialList.raw?.slice(0, 200));
assert(
  "all items material-contribution-position",
  materialList.payload?.items?.every((item) => item.comparisonType === "material-contribution-position") ?? false,
);

const costList = await api(
  "GET",
  `/api/commercial-intelligence/profitability/comparisons?facilityId=${FACILITY}&comparisonType=cost-component-mix-change`,
);
assert("authorized cost list 200", costList.status === 200, costList.raw?.slice(0, 200));
assert(
  "all items cost-component-mix-change",
  costList.payload?.items?.every((item) => item.comparisonType === "cost-component-mix-change") ?? false,
);

const wrongFacility = await api(
  "GET",
  `/api/commercial-intelligence/profitability/comparisons?facilityId=${UNAUTH_FACILITY}&comparisonType=material-contribution-position`,
);
assert("wrong facility 403", wrongFacility.status === 403, String(wrongFacility.status));

const crossTenant = await api(
  "GET",
  `/api/commercial-intelligence/profitability/comparisons?facilityId=${FACILITY}&comparisonType=material-contribution-position`,
  undefined,
  actorHeaders({ ...MANAGER, tenant: "unknown-tenant-key" }),
);
assert("cross tenant denied", crossTenant.status === 401 || crossTenant.status === 403, String(crossTenant.status));

const materialDetail = await api(
  "GET",
  `/api/commercial-intelligence/profitability/comparisons/${MATERIAL_RUN}?facilityId=${FACILITY}`,
);
assert("material detail 200", materialDetail.status === 200, materialDetail.raw?.slice(0, 200));
assert(
  "material type preserved",
  materialDetail.payload?.comparisonType === "material-contribution-position",
  materialDetail.payload?.comparisonType,
);

const costDetail = await api(
  "GET",
  `/api/commercial-intelligence/profitability/comparisons/${COST_RUN}?facilityId=${FACILITY}`,
);
assert("cost detail 200", costDetail.status === 200, costDetail.raw?.slice(0, 200));
assert(
  "cost type preserved",
  costDetail.payload?.comparisonType === "cost-component-mix-change",
  costDetail.payload?.comparisonType,
);

const invalidDetail = await api(
  "GET",
  `/api/commercial-intelligence/profitability/comparisons/${INVALID}?facilityId=${FACILITY}`,
);
assert("invalid comparison denied", invalidDetail.status === 404 || invalidDetail.status === 403, String(invalidDetail.status));

const unauthRoute = await fetchWeb(ROUTE);
assert(
  "missing authentication redirect",
  unauthRoute.status === 302 || unauthRoute.status === 307 || unauthRoute.location?.includes("/giris"),
  String(unauthRoute.status),
);

assert("static material-cost workspace wired", pageSource.includes("CommercialMaterialCostProfitabilityWorkspaceView"));

console.log("COMMERCIAL-PROFITABILITY-UI-5 permission regression verification passed.");
