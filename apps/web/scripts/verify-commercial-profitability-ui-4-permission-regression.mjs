#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const API = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const WEB = (process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001").replace("127.0.0.1", "localhost").replace(/\/$/, "");
const ROUTE = "/ticari-zeka/musteri-karliligi";
const PRODUCT_ROUTE = "/ticari-zeka/urun-karliligi";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const UNAUTH_FACILITY = "bb000001-0005-4000-8000-000000000099";
const CUSTOMER_RUN = "bb000001-03b6-4000-8000-0000000003b6";
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
  console.error(`COMMERCIAL-PROFITABILITY-UI-4 permission regression verifier failed: ${message}`);
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
    "X-crevafactos-dev-Issuer": actor.issuer,
    "X-crevafactos-dev-Subject": actor.subject,
    "X-NevaxisZentra-Tenant": actor.tenant,
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

console.log("Running COMMERCIAL-PROFITABILITY-UI-4 permission regression verifier.");

const pageSource = read("apps/web/src/app/(app)/ticari-zeka/musteri-karliligi/page.tsx");
const productPage = read("apps/web/src/app/(app)/ticari-zeka/urun-karliligi/page.tsx");
const queries = read("apps/web/src/features/commercial-intelligence/server/commercial-customer-profitability-queries.ts");

assert("page requires commercial-intelligence.read", pageSource.includes('forbidUnlessGranted("commercial-intelligence.read")'));
assert("product workspace preserved", productPage.includes("CommercialProfitabilityWorkspaceView"));
assert(
  "customer workspace requests customer-contribution-position",
  queries.includes("PROFITABILITY_CUSTOMER_CONTRIBUTION_TYPE") || queries.includes("customer-contribution-position"),
);

const ready = await fetch(`${API}/health/ready`);
assert("API ready", ready.status === 200, String(ready.status));

const noAuth = await fetch(`${API}/api/commercial-intelligence/profitability/comparisons?facilityId=${FACILITY}&comparisonType=customer-contribution-position`);
assert("missing auth 401", noAuth.status === 401, String(noAuth.status));

const readonlyList = await api(
  "GET",
  `/api/commercial-intelligence/profitability/comparisons?facilityId=${FACILITY}&comparisonType=customer-contribution-position`,
  undefined,
  actorHeaders(READONLY),
);
assert("readonly customer list allowed", readonlyList.status === 200, String(readonlyList.status));

const authorizedList = await api(
  "GET",
  `/api/commercial-intelligence/profitability/comparisons?facilityId=${FACILITY}&comparisonType=customer-contribution-position`,
);
assert("authorized customer list 200", authorizedList.status === 200, authorizedList.raw?.slice(0, 200));
assert("customer items present", Array.isArray(authorizedList.payload?.items) && authorizedList.payload.items.length > 0);
assert(
  "all items customer-contribution-position",
  authorizedList.payload.items.every((item) => item.comparisonType === "customer-contribution-position"),
);

const wrongFacility = await api(
  "GET",
  `/api/commercial-intelligence/profitability/comparisons?facilityId=${UNAUTH_FACILITY}&comparisonType=customer-contribution-position`,
);
assert("wrong facility 403", wrongFacility.status === 403, String(wrongFacility.status));

const crossTenant = await api(
  "GET",
  `/api/commercial-intelligence/profitability/comparisons?facilityId=${FACILITY}&comparisonType=customer-contribution-position`,
  undefined,
  actorHeaders({ ...MANAGER, tenant: "unknown-tenant-key" }),
);
assert("cross tenant denied", crossTenant.status === 401 || crossTenant.status === 403, String(crossTenant.status));

const customerDetail = await api(
  "GET",
  `/api/commercial-intelligence/profitability/comparisons/${CUSTOMER_RUN}?facilityId=${FACILITY}`,
);
assert("customer detail 200", customerDetail.status === 200, customerDetail.raw?.slice(0, 200));
assert(
  "customer outcome preserved",
  customerDetail.payload?.comparisonOutcome === "within-comparison-band",
  customerDetail.payload?.comparisonOutcome,
);
assert(
  "customer type preserved",
  customerDetail.payload?.comparisonType === "customer-contribution-position",
  customerDetail.payload?.comparisonType,
);

const unauthRoute = await fetchWeb(ROUTE);
assert(
  "missing authentication redirect",
  unauthRoute.status === 302 || unauthRoute.status === 307 || unauthRoute.location?.includes("/giris"),
  String(unauthRoute.status),
);

console.log("⚠ Live authenticated customer HTML route check skipped: MFA secret not required for static/API verification in PROFITABILITY-4");
assert("static customer workspace wired", pageSource.includes("CommercialCustomerProfitabilityWorkspaceView"));
assert("static product workspace wired", productPage.includes("CommercialProfitabilityWorkspaceView"));

console.log("COMMERCIAL-PROFITABILITY-UI-4 permission regression verification passed.");
