import assert from "node:assert/strict";
import Module from "node:module";
import test from "node:test";

const realFetch = globalThis.fetch;

// A fresh tenant has no line to recover: the page must explain what it needs without changing anything.
// --- /senaryo-kurtarma on a fresh tenant: an explained, side-effect-free state --------------------------------------
type Env = Record<string, string | undefined>;
const env = process.env as Env;
const ENV_KEYS = ["NODE_ENV", "NEVAXIS_FACTOS_API_BASE_URL", "NEVAXIS_FACTOS_FACILITY_ID", "NEVAXIS_FACTOS_PRODUCTION_EXECUTION_RUNTIME_MODE", "NEVAXIS_FACTOS_DEV_AUTH_ISSUER", "NEVAXIS_FACTOS_DEV_AUTH_SUBJECT", "NEVAXIS_FACTOS_DEV_TENANT_KEY"] as const;
const moduleLoader = Module as unknown as { _load: (request: string, parent: unknown, isMain: boolean) => unknown };
const originalLoad = moduleLoader._load;

test("/senaryo-kurtarma without a line states what it needs and makes no API call that changes anything", async () => {
  const saved = new Map<string, string | undefined>();
  for (const key of ENV_KEYS) {
    saved.set(key, env[key]);
    delete env[key];
  }
  env.NODE_ENV = "production";
  env.NEVAXIS_FACTOS_API_BASE_URL = "http://127.0.0.1:5000";
  env.NEVAXIS_FACTOS_FACILITY_ID = "bb000001-0005-4000-8000-000000000005";
  const cookie = "NevaxisZentra.session=SESSION-VALUE-FOR-TEST";
  moduleLoader._load = function patchedLoad(request, parent, isMain) {
    if (request === "next/headers") {
      return {
        headers: async () => new Headers({ cookie }),
        cookies: async () => ({
          getAll: () => [{ name: "NevaxisZentra.session", value: "SESSION-VALUE-FOR-TEST" }],
          get: () => undefined,
        }),
      };
    }
    return originalLoad.call(this, request, parent, isMain);
  };
  const methods: string[] = [];
  globalThis.fetch = (async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = new URL(String(input));
    methods.push(`${(init?.method ?? "GET").toUpperCase()} ${url.pathname}`);
    if (url.pathname === "/api/admin-access/me/effective-access") {
      return new Response(JSON.stringify({ grantedPermissionKeys: ["scenario-recovery.read", "scenario-recovery.simulate"] }), { status: 200 });
    }
    return new Response("{}", { status: 404 });
  }) as typeof fetch;

  try {
    const { readScenarioRecoveryPage } = await import("../src/features/scenario-recovery/server/scenario-recovery-reader");
    const result = await readScenarioRecoveryPage({});
    assert.equal(result.status, "failure");
    assert.match(result.status === "failure" ? result.message : "", /hattı bağlamı gerektirir/);
    assert.deepEqual(
      methods.filter((entry) => !entry.startsWith("GET ")),
      [],
      "an empty fresh-tenant state must not create or change anything",
    );
  } finally {
    moduleLoader._load = originalLoad;
    globalThis.fetch = realFetch;
    for (const [key, value] of saved) {
      if (value === undefined) delete env[key];
      else env[key] = value;
    }
  }
});
