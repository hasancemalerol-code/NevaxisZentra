import assert from "node:assert/strict";
import { readFileSync, readdirSync, statSync } from "node:fs";
import Module from "node:module";
import path from "node:path";
import test from "node:test";

// Opening a Maintenance page used to POST /api/maintenance/condition/evaluate and /condition/risk/evaluate on every render,
// so a page view could create assessments (and make every AI draft stale). A page render must only read. Recomputing the
// assessments is the explicit, permission-gated evaluateMaintenanceConditionAction.
type Env = Record<string, string | undefined>;
const env = process.env as Env;
const realFetch = globalThis.fetch;
const moduleLoader = Module as unknown as { _load: (request: string, parent: unknown, isMain: boolean) => unknown };
const originalLoad = moduleLoader._load;

const calls: string[] = [];
let grants: string[] = [];

const ASSET = {
  assetReference: "ASSET-CNC-001",
  assetName: "CNC",
  machineCode: "CNC-001",
  machineName: "CNC",
  machineId: "bb000001-0095-4000-8000-000000000095",
  conditionStatus: "unknown",
  activeAnomalyCount: 0,
  dataFreshness: "stale",
  dataSufficiency: "insufficient",
};

function install() {
  env.NODE_ENV = "production";
  env.NEVAXIS_FACTOS_API_BASE_URL = "http://127.0.0.1:5000";
  env.NEVAXIS_FACTOS_FACILITY_ID = "bb000001-0005-4000-8000-000000000005";
  moduleLoader._load = function patchedLoad(request, parent, isMain) {
    if (request === "next/headers") {
      return {
        headers: async () => new Headers({ cookie: "NevaxisZentra.session=SESSION-VALUE-FOR-TEST" }),
        cookies: async () => ({
          getAll: () => [{ name: "NevaxisZentra.session", value: "SESSION-VALUE-FOR-TEST" }],
          get: () => undefined,
        }),
      };
    }
    if (request === "next/cache") {
      return { revalidatePath: () => undefined };
    }
    if (request === "next/navigation") {
      return { redirect: () => undefined, forbidden: () => undefined, notFound: () => undefined };
    }
    return originalLoad.call(this, request, parent, isMain);
  };
  globalThis.fetch = (async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = new URL(String(input));
    const method = (init?.method ?? "GET").toUpperCase();
    calls.push(`${method} ${url.pathname}`);
    if (url.pathname === "/api/admin-access/me/effective-access") {
      return new Response(JSON.stringify({ grantedPermissionKeys: grants }), { status: 200 });
    }
    if (url.pathname === "/api/maintenance/condition/overview") {
      return new Response(
        JSON.stringify({
          overview: { monitoredAssetCount: 1, normalCount: 0, watchCount: 0, degradedCount: 0, criticalCount: 0, insufficientDataCount: 1 },
          assets: [ASSET],
        }),
        { status: 200 },
      );
    }
    if (method === "POST" && /\/condition(\/risk)?\/evaluate$/.test(url.pathname)) {
      return new Response(JSON.stringify({ evaluated: true }), { status: 200 });
    }
    return new Response("{}", { status: 404 });
  }) as typeof fetch;
}

const mutations = () => calls.filter((entry) => !entry.startsWith("GET "));

test.beforeEach(() => {
  calls.length = 0;
  grants = ["maintenance.read", "maintenance.manage"];
});
test.before(install);
test.after(() => {
  moduleLoader._load = originalLoad;
  globalThis.fetch = realFetch;
});

test("the condition page loader only reads: repeated loads issue no POST/PUT/PATCH/DELETE", async () => {
  const { readMaintenanceConditionWorkspace } = await import("../src/features/maintenance/server/maintenance-condition-reader");

  for (let i = 0; i < 3; i += 1) {
    const result = await readMaintenanceConditionWorkspace("ASSET-CNC-001");
    assert.equal(result.status, "success");
  }

  assert.deepEqual(mutations(), [], "a page render must not mutate");
  assert.equal(calls.filter((entry) => entry === "GET /api/maintenance/condition/overview").length, 3);
  assert.ok(!calls.some((entry) => entry.includes("/evaluate")), "no evaluation endpoint may be touched by a render");
});

test("a viewer without the manage permission loads the same read-only page and still issues no mutation", async () => {
  grants = ["maintenance.read"];
  const { readMaintenanceConditionWorkspace } = await import("../src/features/maintenance/server/maintenance-condition-reader");

  const result = await readMaintenanceConditionWorkspace("ASSET-CNC-001");

  assert.equal(result.status, "success");
  assert.equal(result.status === "success" ? result.canManageMaintenance : true, false);
  assert.deepEqual(mutations(), []);
});

test("a manager is offered the manage controls from the real permission, not from a development subject", async () => {
  const { readMaintenanceConditionWorkspace } = await import("../src/features/maintenance/server/maintenance-condition-reader");

  const result = await readMaintenanceConditionWorkspace("ASSET-CNC-001");

  assert.equal(result.status === "success" ? result.canManageMaintenance : false, true);
  assert.deepEqual(mutations(), []);
});

test("the predictive-risk page loader only reads", async () => {
  const { readMaintenancePredictiveRiskWorkspace } = await import("../src/features/maintenance/server/maintenance-predictive-risk-reader");

  await readMaintenancePredictiveRiskWorkspace();
  await readMaintenancePredictiveRiskWorkspace();

  assert.deepEqual(mutations(), []);
  assert.ok(!calls.some((entry) => entry.includes("/evaluate")));
});

test("the explicit action evaluates exactly once: one condition evaluation, then one risk evaluation", async () => {
  const { evaluateMaintenanceConditionAction } = await import("../src/features/maintenance/server/maintenance-evaluation-actions");

  const result = await evaluateMaintenanceConditionAction();

  assert.equal(result.status, "success");
  assert.deepEqual(mutations(), ["POST /api/maintenance/condition/evaluate", "POST /api/maintenance/condition/risk/evaluate"]);
});

test("the explicit action refuses a caller without maintenance.manage and calls no evaluation endpoint", async () => {
  grants = ["maintenance.read"];
  const { evaluateMaintenanceConditionAction } = await import("../src/features/maintenance/server/maintenance-evaluation-actions");

  const result = await evaluateMaintenanceConditionAction();

  assert.equal(result.status, "failure");
  assert.deepEqual(mutations(), []);
});

test("evaluation endpoints are reachable only from the explicit action module", () => {
  const root = path.join(__dirname, "..", "src");
  const offenders: string[] = [];
  const walk = (directory: string) => {
    for (const name of readdirSync(directory)) {
      const full = path.join(directory, name);
      if (statSync(full).isDirectory()) {
        walk(full);
        continue;
      }
      if (!/\.(ts|tsx)$/.test(name)) continue;
      const text = readFileSync(full, "utf8");
      const callsEvaluate = /\bevaluateMaintenance(Condition|PredictiveRisk)\(/.test(text) && !/export function evaluateMaintenance/.test(text);
      if (callsEvaluate && !full.endsWith("maintenance-evaluation-actions.ts")) {
        offenders.push(path.relative(root, full));
      }
    }
  };
  walk(root);

  assert.deepEqual(offenders, [], "only the explicit action may call the evaluation endpoints");
});
