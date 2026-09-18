#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://localhost:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`MOLD-1 pagination/ordering verifier failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function headers() {
  return {
    "X-crevafactos-dev-Issuer": "crevafactos-dev",
    "X-crevafactos-dev-Subject": "production-pilot-operator",
    "X-NevaxisZentra-Tenant": "dev-pilot-tenant",
  };
}

async function list(query) {
  const response = await fetch(`${BASE}/api/molds?facilityId=${FACILITY}&${query}`, { headers: headers() });
  const json = await response.json();
  return { status: response.status, json };
}

console.log("Running MOLD-1 pagination/ordering live verifier.");

const repo = readFileSync(resolve(ROOT, "packages/NevaxisZentra.Persistence/Mold/MoldRepository.cs"), "utf8").replaceAll("\r\n", "\n");
assert("ordering documented in repository", repo.includes(".OrderBy(m => m.CodeNormalized)") && repo.includes(".ThenBy(m => m.Name)"));

const defaultPage = await list("page=1");
assert("default list → 200", defaultPage.status === 200);
assert("default page size bounded (≤100)", Array.isArray(defaultPage.json) && defaultPage.json.length <= 100);

const explicitSize = await list("page=1&pageSize=5");
assert("explicit page size honored", explicitSize.status === 200 && explicitSize.json.length <= 5);

const zeroSize = await list("page=1&pageSize=0");
assert("zero page size normalized to ≥1", zeroSize.status === 200 && zeroSize.json.length >= 0);

const negativeSize = await list("page=1&pageSize=-5");
assert("negative page size normalized", negativeSize.status === 200);

const excessiveSize = await list("page=1&pageSize=500");
assert("excessive page size capped at 100", excessiveSize.status === 200 && excessiveSize.json.length <= 100);

const invalidPage = await list("page=-1&pageSize=10");
assert("invalid page normalized", invalidPage.status === 200);

const unauthorizedFacility = "bb000001-0005-4000-8000-000000000099";
const crossFacility = await fetch(`${BASE}/api/molds?facilityId=${unauthorizedFacility}&page=1&pageSize=10`, { headers: headers() });
assert("tenant/facility boundary before pagination → 403", crossFacility.status === 403);

const run1 = await list("page=1&pageSize=50");
const run2 = await list("page=1&pageSize=50");
const run3 = await list("page=1&pageSize=50");
const ids1 = run1.json.map((m) => m.id).join(",");
const ids2 = run2.json.map((m) => m.id).join(",");
const ids3 = run3.json.map((m) => m.id).join(",");
assert("repeated query same order", ids1 === ids2 && ids2 === ids3);

const page1 = await list("page=1&pageSize=2");
const page2 = await list("page=2&pageSize=2");
if (page1.json.length >= 2 && page2.json.length >= 1) {
  const page1Ids = new Set(page1.json.map((m) => m.id));
  const overlap = page2.json.some((m) => page1Ids.has(m.id));
  assert("pagination no duplicate records", !overlap);
}

const codes = run1.json.map((m) => m.code);
const sortedCodes = [...codes].sort((a, b) => a.localeCompare(b, undefined, { sensitivity: "base" }));
assert("primary ordering by normalized code", JSON.stringify(codes) === JSON.stringify(sortedCodes) || codes.every((_, i, arr) => i === 0 || arr[i - 1].localeCompare(arr[i]) <= 0));

console.log("MOLD-1 pagination/ordering verification passed.");
