import assert from "node:assert/strict";
import { readdirSync, readFileSync } from "node:fs";
import Module from "node:module";
import path from "node:path";
import test from "node:test";

// Packaged Production runs the web server in API mode: no development identity headers, and the only
// credential the browser holds is the session cookie. Server loaders must forward that cookie (the trusted
// server-side bridge) and must never fall back to a development shortcut.
const SESSION_COOKIE = "NevaxisZentra.session=SESSION-VALUE-FOR-TEST; NevaxisZentra.csrf=CSRF-VALUE-FOR-TEST";
const BRIDGE_REFUSAL = "Production API mode requires a trusted server-side authentication bridge.";
const API_BASE = "http://127.0.0.1:5000";
const FACILITY_ID = "bb000001-0005-4000-8000-000000000005";

type Env = Record<string, string | undefined>;
const env = process.env as Env;
const ENV_KEYS = [
  "NODE_ENV",
  "NEVAXIS_FACTOS_API_BASE_URL",
  "NEVAXIS_FACTOS_FACILITY_ID",
  "NEVAXIS_FACTOS_PRODUCTION_EXECUTION_RUNTIME_MODE",
  "NEVAXIS_FACTOS_DEV_AUTH_ISSUER",
  "NEVAXIS_FACTOS_DEV_AUTH_SUBJECT",
  "NEVAXIS_FACTOS_DEV_TENANT_KEY",
] as const;

let cookieHeader = SESSION_COOKIE;
const moduleLoader = Module as unknown as {
  _load: (request: string, parent: unknown, isMain: boolean) => unknown;
};
const originalLoad = moduleLoader._load;
const savedEnv = new Map<string, string | undefined>();

function installPackagedProductionRuntime() {
  for (const key of ENV_KEYS) {
    savedEnv.set(key, env[key]);
    delete env[key];
  }
  env.NODE_ENV = "production";
  env.NEVAXIS_FACTOS_API_BASE_URL = API_BASE;
  env.NEVAXIS_FACTOS_FACILITY_ID = FACILITY_ID;

  moduleLoader._load = function patchedLoad(request, parent, isMain) {
    if (request === "next/headers") {
      return {
        headers: async () => new Headers(cookieHeader ? { cookie: cookieHeader } : {}),
        cookies: async () => ({
          getAll: () =>
            cookieHeader
              .split(";")
              .map((part) => part.trim())
              .filter(Boolean)
              .map((part) => {
                const separator = part.indexOf("=");
                return { name: part.slice(0, separator), value: part.slice(separator + 1) };
              }),
          get: () => undefined,
        }),
      };
    }
    return originalLoad.call(this, request, parent, isMain);
  };
}

function restoreRuntime() {
  moduleLoader._load = originalLoad;
  for (const [key, value] of savedEnv) {
    if (value === undefined) delete env[key];
    else env[key] = value;
  }
}

test("the synchronous resolver has no session bridge in production API mode (why it must not be used by page loaders)", async () => {
  installPackagedProductionRuntime();
  try {
    const { parseProductionExecutionRuntimeConfig } = await import(
      "../src/features/production-execution/server/production-order-runtime-config"
    );
    const { resolveProductionOrderRequestContext } = await import(
      "../src/features/production-execution/server/production-order-request-context"
    );
    const config = parseProductionExecutionRuntimeConfig();
    assert.equal(config.status, "success");
    if (config.status !== "success") return;
    assert.equal(config.config.mode, "api");

    const sync = resolveProductionOrderRequestContext(config.config);
    assert.equal(sync.status, "failure");
    assert.equal(sync.status === "failure" ? sync.message : "", BRIDGE_REFUSAL);
  } finally {
    restoreRuntime();
  }
});

test("the incoming-request resolver bridges the session cookie, and refuses without one (no dev shortcut)", async () => {
  installPackagedProductionRuntime();
  try {
    const { parseProductionExecutionRuntimeConfig } = await import(
      "../src/features/production-execution/server/production-order-runtime-config"
    );
    const { resolveProductionOrderRequestContextForIncomingRequest } = await import(
      "../src/features/production-execution/server/production-order-request-context"
    );
    const config = parseProductionExecutionRuntimeConfig();
    assert.equal(config.status, "success");
    if (config.status !== "success") return;

    cookieHeader = SESSION_COOKIE;
    const bridged = await resolveProductionOrderRequestContextForIncomingRequest(config.config);
    assert.equal(bridged.status, "success");
    if (bridged.status !== "success") return;
    assert.equal(bridged.context.apiBaseUrl, API_BASE);
    assert.equal(bridged.context.facilityId, FACILITY_ID);
    assert.equal(bridged.context.requestHeaders.Cookie, SESSION_COOKIE);
    // Only the browser session cookie is forwarded: no development identity or server secret headers.
    assert.deepEqual(Object.keys(bridged.context.requestHeaders), ["Cookie"]);

    cookieHeader = "";
    const anonymous = await resolveProductionOrderRequestContextForIncomingRequest(config.config);
    assert.equal(anonymous.status, "failure");
    assert.equal(anonymous.status === "failure" ? anonymous.message : "", BRIDGE_REFUSAL);

    cookieHeader = "unrelated=1";
    const wrongCookie = await resolveProductionOrderRequestContextForIncomingRequest(config.config);
    assert.equal(wrongCookie.status, "failure");
  } finally {
    cookieHeader = SESSION_COOKIE;
    restoreRuntime();
  }
});

test("the executive command center loader forwards the session cookie to every downstream API call", async () => {
  installPackagedProductionRuntime();
  const realFetch = globalThis.fetch;
  const calls: { url: string; cookie: string | null; devHeaders: boolean }[] = [];
  globalThis.fetch = (async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = String(input);
    const headers = new Headers(init?.headers ?? undefined);
    calls.push({
      url,
      cookie: headers.get("cookie"),
      devHeaders: [...headers.keys()].some((key) => key.toLowerCase().includes("dev-")),
    });
    if (url.includes("/api/admin-access/me/effective-access")) {
      return new Response(
        JSON.stringify({ grantedPermissionKeys: ["factory-case.read", "ai-task.read"], roleReferences: ["r"] }),
        { status: 200 },
      );
    }
    if (url.includes("/api/ai-tasks")) {
      return new Response(
        JSON.stringify({ items: [], totalCount: 0, page: 1, pageSize: 100, ruleSetVersion: "t", provenanceDisclaimer: "", capabilities: {} }),
        { status: 200 },
      );
    }
    return new Response(JSON.stringify({}), { status: 200 });
  }) as typeof fetch;

  try {
    const { readExecutiveCommandCenterWorkspace } = await import(
      "../src/features/executive-command-center/server/executive-command-center-queries"
    );
    let message = "";
    try {
      const result = await readExecutiveCommandCenterWorkspace({} as never);
      message = result.status === "failure" ? result.message : "";
    } catch {
      // The stubbed API bodies are not shape-complete, so the workspace builder may throw after the calls
      // were made. This test asserts the auth bridge (calls reached the API with the cookie), not the view.
    }

    assert.notEqual(message, BRIDGE_REFUSAL, "the loader must not stop at the missing-bridge refusal");
    const downstream = calls.filter((call) => !call.url.includes("/effective-access"));
    assert.ok(downstream.length > 0, "the loader reached the API");
    assert.ok(
      downstream.some((call) => call.url.includes("/api/ai-tasks")),
      "the AI task source was requested",
    );
    for (const call of calls) {
      assert.equal(call.cookie, SESSION_COOKIE, `${call.url} carried the session cookie`);
      assert.equal(call.devHeaders, false, `${call.url} carried no development identity header`);
    }
  } finally {
    globalThis.fetch = realFetch;
    restoreRuntime();
  }
});

// The same defect class everywhere a shipped page loader resolves its context: these loaders back routes that
// ship in the V1 navigation and must use the bridge. (Remaining loaders are bounded by the baseline test below.)
const BRIDGED_LOADERS = [
  "executive-command-center/server/executive-command-center-queries.ts",
  "factory-decision-center/server/factory-decision-center-reader.ts",
  "factory-decision-center/server/factory-decision-center-actions.ts",
  "bugun/server/bugun-workspace-queries.ts",
  "dashboard-showcase/server/dashboard-showcase-queries.ts",
  "isler-hub/server/isler-hub-queries.ts",
  "works/server/works-queries.ts",
  "works/server/works-actions.ts",
] as const;

test("command center, decision center, home, dashboard, task hub and works loaders use the incoming-request bridge", () => {
  const featuresRoot = path.resolve(__dirname, "..", "src", "features");
  for (const relative of BRIDGED_LOADERS) {
    const source = readFileSync(path.join(featuresRoot, relative), "utf8");
    assert.match(source, /resolveProductionOrderRequestContextForIncomingRequest/, `${relative} uses the bridge`);
    assert.doesNotMatch(
      source,
      /[^A-Za-z]resolveProductionOrderRequestContext\(/,
      `${relative} must not call the synchronous (no-bridge) resolver`,
    );
  }
});

// No customer-visible server loader or action may use a synchronous (no-bridge) request-context resolver: in
// Production API mode they can only fail with the bridge refusal. The three resolver families below each have
// an incoming-request variant that forwards the browser session cookie. The count of offending call sites is zero
// and may not rise; the only allow-listed callers are the resolvers' own definition files and the documented
// development-context fallback of the dashboard market-rates context (it runs only after the session path).
const SYNC_RESOLVERS = [
  "resolveProductionOrderRequestContext",
  "resolveProductionPlanRequestContext",
  "resolveExportCaseRequestContext",
  "resolveAiTaskRequestContext",
] as const;

const SYNC_RESOLVER_ALLOWED_FILES = new Set([
  "features/production-execution/server/production-order-request-context.ts",
  "features/production-planning/server/production-plan-request-context.ts",
  "features/export/server/export-case-request-context.ts",
  "features/ai-task-center/server/ai-task-request-context.ts",
  // Session-first: resolveDashboardRequestContext only reaches this after the incoming session cookie check.
  "features/dashboard/server/market-rates-request-context.ts",
]);

function walk(directory: string, files: string[] = []): string[] {
  for (const entry of readdirSync(directory, { withFileTypes: true })) {
    const full = path.join(directory, entry.name);
    if (entry.isDirectory()) walk(full, files);
    else if (/\.(ts|tsx)$/.test(entry.name)) files.push(full);
  }
  return files;
}

test("no shipped server loader or action calls a synchronous (no-bridge) request-context resolver", () => {
  const sourceRoot = path.resolve(__dirname, "..", "src");
  const offenders: string[] = [];
  for (const file of walk(sourceRoot)) {
    const relative = path.relative(sourceRoot, file).split(path.sep).join("/");
    if (SYNC_RESOLVER_ALLOWED_FILES.has(relative)) continue;
    const source = readFileSync(file, "utf8");
    for (const resolver of SYNC_RESOLVERS) {
      // A call is `name(` not preceded by an identifier character; `...ForIncomingRequest(` is a different name.
      if (new RegExp(`(^|[^A-Za-z])${resolver}\\(`).test(source)) {
        offenders.push(`${relative}: ${resolver}(`);
      }
    }
  }
  assert.deepEqual(offenders, [], "use the *ForIncomingRequest resolver so the session cookie is bridged");
});

test("the synchronous AI-task context is only consumed by the session-first dashboard context", () => {
  const sourceRoot = path.resolve(__dirname, "..", "src");
  const consumers = walk(sourceRoot)
    .filter((file) => /(^|[^A-Za-z])resolveAiTaskRequestContext\(/.test(readFileSync(file, "utf8")))
    .map((file) => path.relative(sourceRoot, file).split(path.sep).join("/"))
    .sort();
  assert.deepEqual(consumers, [
    "features/ai-task-center/server/ai-task-request-context.ts",
    "features/dashboard/server/market-rates-request-context.ts",
  ]);
});

test("production API mode never emits development identity headers from the bridged resolvers", () => {
  const sourceRoot = path.resolve(__dirname, "..", "src");
  const devHeaderFiles = walk(sourceRoot)
    .filter((file) => readFileSync(file, "utf8").includes("X-crevafactos-dev-Issuer"))
    .map((file) => path.relative(sourceRoot, file).split(path.sep).join("/"))
    .sort();
  assert.deepEqual(devHeaderFiles, [
    "features/admin-access/server/admin-access-effective-access.ts",
    "features/export/server/export-case-request-context.ts",
    "features/production-execution/server/production-order-request-context.ts",
    "features/production-planning/server/production-plan-request-context.ts",
  ]);
});
