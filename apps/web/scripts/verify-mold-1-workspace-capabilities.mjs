#!/usr/bin/env node
import { spawnSync } from "node:child_process";
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.CREVAFACTOS_API_BASE_URL ?? "http://localhost:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";

const ACTORS = {
  manager: "production-pilot-operator",
  readonly: "planning-pilot-reviewer",
  unauthorized: "quality-negative-test-user",
};

function fail(message) {
  console.error(`MOLD-1 workspace capabilities verifier failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function dbCount(sql) {
  const result = spawnSync(
    "docker",
    ["exec", "crevafactos-local-postgres-1", "psql", "-U", "crevafactos", "-d", "crevafactos_local", "-t", "-A", "-c", sql],
    { encoding: "utf8" },
  );
  if (result.status !== 0) fail(result.stderr || result.stdout);
  return Number(result.stdout.trim());
}

async function getWorkspaceSummary(subject) {
  const response = await fetch(`${BASE}/api/molds/workspace-summary?facilityId=${FACILITY}`, {
    headers: {
      "X-CrevaFactOS-Dev-Issuer": "crevafactos-dev",
      "X-CrevaFactOS-Dev-Subject": subject,
      "X-CrevaFactOS-Tenant": "dev-pilot-tenant",
    },
    cache: "no-store",
  });
  const text = await response.text();
  let json = null;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    json = text;
  }
  return { status: response.status, json };
}

console.log("Running MOLD-1 workspace capabilities live verifier.");

const apiClient = readFileSync(resolve(ROOT, "apps/web/src/features/molds/server/mold-api-client.ts"), "utf8").replaceAll("\r\n", "\n");
const endpoints = readFileSync(resolve(ROOT, "apps/api/Mold/MoldEndpointRouteBuilderExtensions.cs"), "utf8").replaceAll("\r\n", "\n");

assert("manage probe uses GET workspace-summary", apiClient.includes('getMoldWorkspaceSummary(context)'));
assert("manage probe does not POST to create endpoint", !apiClient.includes('method: "POST"') || !apiClient.match(/probeMoldManagePermission[\s\S]*method: "POST"/));
forbidMutationProbe(apiClient);
assert("workspace-summary exposes capabilities", endpoints.includes("capabilities = new") && endpoints.includes("canManage"));

const moldCountBefore = dbCount("SELECT COUNT(*) FROM molds");

const manager = await getWorkspaceSummary(ACTORS.manager);
assert("manager workspace-summary → 200", manager.status === 200);
assert("manager canRead", manager.json?.capabilities?.canRead === true);
assert("manager canManage", manager.json?.capabilities?.canManage === true);

const readonly = await getWorkspaceSummary(ACTORS.readonly);
assert("readonly workspace-summary → 200", readonly.status === 200);
assert("readonly canRead", readonly.json?.capabilities?.canRead === true);
assert("readonly canManage denied", readonly.json?.capabilities?.canManage === false);

const unauthorized = await getWorkspaceSummary(ACTORS.unauthorized);
assert("unauthorized workspace-summary → 403", unauthorized.status === 403);

const moldCountAfter = dbCount("SELECT COUNT(*) FROM molds");
assert("permission probes did not create molds", moldCountAfter === moldCountBefore);

console.log("MOLD-1 workspace capabilities verification passed.");

function forbidMutationProbe(source) {
  const probeBlock = source.slice(source.indexOf("export async function probeMoldManagePermission"));
  if (probeBlock.includes('method: "POST"') || probeBlock.includes("mutateJson")) {
    fail("probeMoldManagePermission must not use mutation endpoints");
  }
}
