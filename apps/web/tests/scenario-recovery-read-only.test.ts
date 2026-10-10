import assert from "node:assert/strict";
import { readFileSync, readdirSync, statSync } from "node:fs";
import Module from "node:module";
import path from "node:path";
import test from "node:test";

// Viewing /senaryo-kurtarma is side-effect-free: only an explicit, permission-gated action creates a scenario run.
type Env = Record<string, string | undefined>;
const env = process.env as Env;
const realFetch = globalThis.fetch;
const moduleLoader = Module as unknown as { _load: (request: string, parent: unknown, isMain: boolean) => unknown };
const originalLoad = moduleLoader._load;

const LINE_ID = "11111111-2222-4333-8444-555555555555";
const RUN_ID = "99999999-aaaa-4bbb-8ccc-dddddddddddd";

const calls: string[] = [];
let grants: string[] = [];

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
    if (request === "next/navigation") {
      return {
        redirect: (target: string) => {
          throw Object.assign(new Error("NEXT_REDIRECT"), { digest: `NEXT_REDIRECT;replace;${target};307;` });
        },
      };
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
    if (url.pathname === "/api/scenario-recovery/history") {
      return new Response("[]", { status: 200 });
    }
    if (method === "POST" && url.pathname === "/api/scenario-recovery/runs") {
      return new Response(JSON.stringify({ runId: RUN_ID }), { status: 200 });
    }
    if (method === "POST" && url.pathname.endsWith("/generate-options")) {
      return new Response(JSON.stringify({ runId: RUN_ID }), { status: 200 });
    }
    return new Response("{}", { status: 404 });
  }) as typeof fetch;
}

const mutations = () => calls.filter((entry) => !entry.startsWith("GET "));
const scenarioMutations = () => mutations().filter((entry) => entry.includes("/api/scenario-recovery/"));

test.beforeEach(() => {
  calls.length = 0;
  grants = ["scenario-recovery.read", "scenario-recovery.simulate"];
});

test.before(install);
test.after(() => {
  moduleLoader._load = originalLoad;
  globalThis.fetch = realFetch;
});

test("loading a line without a run is read-only: repeated page loads change nothing and offer an explicit action", async () => {
  const { readScenarioRecoveryPage } = await import("../src/features/scenario-recovery/server/scenario-recovery-reader");

  for (let i = 0; i < 3; i += 1) {
    const result = await readScenarioRecoveryPage({ lineId: LINE_ID, entryPoint: "DIRECT" });
    assert.equal(result.status, "ready");
    assert.equal(result.status === "ready" ? result.lineId : "", LINE_ID);
    assert.equal(result.status === "ready" ? result.canSimulate : false, true);
  }

  assert.deepEqual(mutations(), [], "page loads must issue no POST/PUT/PATCH/DELETE");
  assert.equal(calls.filter((entry) => entry === "GET /api/scenario-recovery/history").length, 3);
});

test("a viewer without the simulate permission still sees a read-only page and cannot be offered creation", async () => {
  grants = ["scenario-recovery.read"];
  const { readScenarioRecoveryPage } = await import("../src/features/scenario-recovery/server/scenario-recovery-reader");

  const result = await readScenarioRecoveryPage({ lineId: LINE_ID });

  assert.equal(result.status, "ready");
  assert.equal(result.status === "ready" ? result.canSimulate : true, false);
  assert.deepEqual(mutations(), []);
});

test("an open run is read with GET only", async () => {
  const { readScenarioRecoveryPage } = await import("../src/features/scenario-recovery/server/scenario-recovery-reader");

  await readScenarioRecoveryPage({ runId: RUN_ID });

  assert.deepEqual(mutations(), []);
});

test("the explicit action creates exactly one scenario flow (one create, one generate) and opens the run", async () => {
  const { createScenarioRecoveryRunAction } = await import("../src/features/scenario-recovery/server/scenario-recovery-actions");
  const form = new FormData();
  form.set("lineId", LINE_ID);
  form.set("entryPoint", "DIRECT");

  let redirectedTo = "";
  try {
    await createScenarioRecoveryRunAction({ status: "idle" }, form);
    assert.fail("the action must redirect to the created run");
  } catch (error) {
    redirectedTo = String((error as { digest?: string }).digest ?? error);
  }

  assert.match(redirectedTo, new RegExp(`runId=${RUN_ID}`));
  assert.deepEqual(scenarioMutations(), [
    "POST /api/scenario-recovery/runs",
    `POST /api/scenario-recovery/runs/${RUN_ID}/generate-options`,
  ]);
});

test("the action refuses a caller without the simulate permission and calls no scenario API", async () => {
  grants = ["scenario-recovery.read"];
  const { createScenarioRecoveryRunAction } = await import("../src/features/scenario-recovery/server/scenario-recovery-actions");
  const form = new FormData();
  form.set("lineId", LINE_ID);

  const result = await createScenarioRecoveryRunAction({ status: "idle" }, form);

  assert.equal(result.status, "failure");
  assert.deepEqual(scenarioMutations(), []);
});

test("the action refuses a caller without any scenario permission", async () => {
  grants = [];
  const { createScenarioRecoveryRunAction } = await import("../src/features/scenario-recovery/server/scenario-recovery-actions");
  const form = new FormData();
  form.set("lineId", LINE_ID);

  const result = await createScenarioRecoveryRunAction({ status: "idle" }, form);

  assert.equal(result.status, "failure");
  assert.deepEqual(scenarioMutations(), []);
});

test("the action rejects a missing or malformed line id before any API call", async () => {
  const { createScenarioRecoveryRunAction } = await import("../src/features/scenario-recovery/server/scenario-recovery-actions");
  for (const lineId of [undefined, "", "not-a-guid", "../../etc"]) {
    const form = new FormData();
    if (lineId !== undefined) form.set("lineId", lineId);
    const result = await createScenarioRecoveryRunAction({ status: "idle" }, form);
    assert.equal(result.status, "failure", String(lineId));
  }
  assert.deepEqual(scenarioMutations(), []);
});

function sourceFiles(directory: string): string[] {
  return readdirSync(directory).flatMap((entry) => {
    const full = path.join(directory, entry);
    return statSync(full).isDirectory() ? sourceFiles(full) : /\.(ts|tsx)$/.test(entry) ? [full] : [];
  });
}

test("only the explicit action module may call the scenario create and generate clients", () => {
  const srcRoot = path.resolve(__dirname, "..", "src");
  const offenders = sourceFiles(srcRoot)
    .filter((file) => !file.endsWith("scenario-recovery-api-client.ts"))
    .filter((file) => !file.endsWith("scenario-recovery-actions.ts"))
    .filter((file) => /\bcreateScenarioRecoveryRun\b|\bgenerateScenarioRecoveryOptions\b/.test(readFileSync(file, "utf8")))
    .map((file) => path.relative(srcRoot, file));
  assert.deepEqual(offenders, [], "page loaders must not create scenario runs or options");

  const reader = readFileSync(path.join(srcRoot, "features/scenario-recovery/server/scenario-recovery-reader.ts"), "utf8");
  assert.doesNotMatch(reader, /method:\s*"POST"/);
});

test("the fresh-tenant ready state is explained and never submits by itself", () => {
  const panel = readFileSync(
    path.resolve(__dirname, "..", "src", "features", "scenario-recovery", "components", "scenario-recovery-create-panel.tsx"),
    "utf8",
  );
  assert.match(panel, /Sayfa görüntülenirken senaryo oluşturulmaz/);
  assert.match(panel, /Bu hat için kayıtlı senaryo yok/);
  assert.match(panel, /Senaryo oluştur/);
  assert.doesNotMatch(panel, /useEffect|requestSubmit|\.submit\(|autoSubmit/, "creation must wait for an intentional click");
});
