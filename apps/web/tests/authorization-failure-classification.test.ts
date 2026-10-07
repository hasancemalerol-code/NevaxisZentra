import assert from "node:assert/strict";
import Module from "node:module";
import test from "node:test";

// An expected authorization failure (the caller lacks a permission) and a runtime failure (the access service,
// the session, or the server-side bridge is unavailable) are different customer-facing conditions. Reporting a
// runtime failure as "permission denied" sends customers to an administrator for a problem no role change fixes,
// and reporting a denial as a runtime failure hides a real authorization decision.
const SESSION_COOKIE = "NevaxisZentra.session=SESSION-VALUE-FOR-TEST; NevaxisZentra.csrf=CSRF-VALUE-FOR-TEST";
const API_BASE = "http://127.0.0.1:5000";

type Env = Record<string, string | undefined>;
const env = process.env as Env;
const moduleLoader = Module as unknown as {
  _load: (request: string, parent: unknown, isMain: boolean) => unknown;
};
const originalLoad = moduleLoader._load;
const originalFetch = globalThis.fetch;
const ENV_KEYS = [
  "NODE_ENV",
  "NEVAXIS_FACTOS_API_BASE_URL",
  "NEVAXIS_FACTOS_FACILITY_ID",
  "NEVAXIS_FACTOS_PRODUCTION_EXECUTION_RUNTIME_MODE",
  "NEVAXIS_FACTOS_DEV_AUTH_ISSUER",
  "NEVAXIS_FACTOS_DEV_AUTH_SUBJECT",
  "NEVAXIS_FACTOS_DEV_TENANT_KEY",
] as const;
const savedEnv = new Map<string, string | undefined>();

function installRuntime(options: Readonly<{ configured?: boolean }> = {}) {
  for (const key of ENV_KEYS) {
    savedEnv.set(key, env[key]);
    delete env[key];
  }
  env.NODE_ENV = "production";
  env.NEVAXIS_FACTOS_FACILITY_ID = "bb000001-0005-4000-8000-000000000005";
  if (options.configured === false) delete env.NEVAXIS_FACTOS_API_BASE_URL;
  else env.NEVAXIS_FACTOS_API_BASE_URL = API_BASE;
  moduleLoader._load = function patchedLoad(request, parent, isMain) {
    if (request === "next/headers") {
      return {
        headers: async () => new Headers({ cookie: SESSION_COOKIE }),
        cookies: async () => ({
          getAll: () =>
            SESSION_COOKIE.split(";")
              .map((part) => part.trim())
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

function restore() {
  moduleLoader._load = originalLoad;
  globalThis.fetch = originalFetch;
  for (const [key, value] of savedEnv) {
    if (value === undefined) delete env[key];
    else env[key] = value;
  }
  savedEnv.clear();
}

function stubEffectiveAccess(respond: () => Response | Promise<Response>) {
  globalThis.fetch = (async () => respond()) as typeof fetch;
}

async function loadPermissionModule() {
  return import("../src/features/admin-access/server/require-current-permission");
}

test("a granted permission is granted", async () => {
  installRuntime();
  try {
    stubEffectiveAccess(() => new Response(JSON.stringify({ grantedPermissionKeys: ["ai-task.read"] }), { status: 200 }));
    const { denyUnlessGranted } = await loadPermissionModule();
    assert.deepEqual(await denyUnlessGranted("ai-task.read"), { status: "granted" });
  } finally {
    restore();
  }
});

test("a missing permission on a successful access read is an authorization denial", async () => {
  installRuntime();
  try {
    stubEffectiveAccess(() => new Response(JSON.stringify({ grantedPermissionKeys: ["other.read"] }), { status: 200 }));
    const { denyUnlessGranted, denyUnlessAnyGranted, AUTHORIZATION_DENIED_MESSAGE } = await loadPermissionModule();
    const single = await denyUnlessGranted("ai-task.read");
    assert.equal(single.status, "failure");
    assert.equal(single.status === "failure" ? single.code : "", "authorization-denied");
    assert.equal(single.status === "failure" ? single.message : "", AUTHORIZATION_DENIED_MESSAGE);
    const any = await denyUnlessAnyGranted(["a.read", "b.read"]);
    assert.equal(any.status === "failure" ? any.code : "", "authorization-denied");
  } finally {
    restore();
  }
});

test("runtime failures of the access service are never reported as permission denied", async () => {
  const { AUTHORIZATION_DENIED_MESSAGE } = await loadPermissionModule();
  const cases: ReadonlyArray<Readonly<{ name: string; arrange: () => void; configured?: boolean }>> = [
    { name: "HTTP 500", arrange: () => stubEffectiveAccess(() => new Response("{}", { status: 500 })) },
    { name: "HTTP 503", arrange: () => stubEffectiveAccess(() => new Response("{}", { status: 503 })) },
    {
      name: "network failure",
      arrange: () =>
        stubEffectiveAccess(() => {
          throw new TypeError("fetch failed");
        }),
    },
    { name: "malformed body", arrange: () => stubEffectiveAccess(() => new Response("not json", { status: 200 })) },
    { name: "identity runtime not configured", arrange: () => undefined, configured: false },
  ];

  for (const item of cases) {
    installRuntime({ configured: item.configured });
    try {
      item.arrange();
      const { denyUnlessGranted, denyUnlessAnyGranted, forbidUnlessGranted } = await loadPermissionModule();
      for (const result of [await denyUnlessGranted("ai-task.read"), await denyUnlessAnyGranted(["ai-task.read"])]) {
        assert.equal(result.status, "failure", item.name);
        assert.equal(result.status === "failure" ? result.code : "", "authorization-unavailable", item.name);
        assert.notEqual(result.status === "failure" ? result.message : "", AUTHORIZATION_DENIED_MESSAGE, item.name);
      }
      await assert.rejects(() => forbidUnlessGranted("ai-task.read"), /could not be verified/, item.name);
    } finally {
      restore();
    }
  }
});

test("an expired or missing session is reported as authentication required, not as a missing permission", async () => {
  installRuntime();
  try {
    stubEffectiveAccess(() => new Response("{}", { status: 401 }));
    const { denyUnlessGranted, AUTHENTICATION_REQUIRED_MESSAGE, AUTHORIZATION_DENIED_MESSAGE } = await loadPermissionModule();
    const result = await denyUnlessGranted("ai-task.read");
    assert.equal(result.status === "failure" ? result.code : "", "unauthenticated");
    assert.equal(result.status === "failure" ? result.message : "", AUTHENTICATION_REQUIRED_MESSAGE);
    assert.notEqual(result.status === "failure" ? result.message : "", AUTHORIZATION_DENIED_MESSAGE);
  } finally {
    restore();
  }
});

test("the missing-bridge refusal is an authentication/runtime condition, never a permission denial", async () => {
  installRuntime();
  try {
    const { parseProductionExecutionRuntimeConfig } = await import(
      "../src/features/production-execution/server/production-order-runtime-config"
    );
    const { resolveProductionOrderRequestContext } = await import(
      "../src/features/production-execution/server/production-order-request-context"
    );
    const { AUTHORIZATION_DENIED_MESSAGE } = await loadPermissionModule();
    const config = parseProductionExecutionRuntimeConfig();
    assert.equal(config.status, "success");
    if (config.status !== "success") return;
    const refusal = resolveProductionOrderRequestContext(config.config);
    assert.equal(refusal.status, "failure");
    if (refusal.status !== "failure") return;
    assert.equal(refusal.code, "unauthenticated");
    assert.notEqual(refusal.message, AUTHORIZATION_DENIED_MESSAGE);
    assert.doesNotMatch(refusal.message, /permission|yetki/i);
  } finally {
    restore();
  }
});

// ---------------------------------------------------------------------------------------------------------------------
// Static guard: a failed permission check may carry a hard-coded "you lack permission" wording only when the check
// itself reported a real denial. Otherwise the message must come from the check (runtime/session condition).
// ---------------------------------------------------------------------------------------------------------------------
import { readdirSync, readFileSync } from "node:fs";
import path from "node:path";

function collectSources(directory: string, files: string[] = []): string[] {
  for (const entry of readdirSync(directory, { withFileTypes: true })) {
    const full = path.join(directory, entry.name);
    if (entry.isDirectory()) collectSources(full, files);
    else if (/\.(ts|tsx)$/.test(entry.name)) files.push(full);
  }
  return files;
}

test("no loader or action reports a runtime failure of the permission check as a missing permission", () => {
  const sourceRoot = path.resolve(__dirname, "..", "src");
  const offenders: string[] = [];
  for (const file of collectSources(sourceRoot)) {
    const lines = readFileSync(file, "utf8").split("\n");
    lines.forEach((line, index) => {
      const declaration = line.match(/const (\w+) = await denyUnless(?:Any)?Granted\(/);
      if (!declaration) return;
      const variable = declaration[1];
      let end = index;
      while (end < lines.length && !lines[end].includes(";")) end++;
      const start = lines.findIndex((candidate, at) => at >= end && candidate.includes(`${variable}.status === "failure"`));
      if (start < 0 || start > end + 6) return;
      for (let at = start; at < Math.min(start + 10, lines.length); at++) {
        const literal = lines[at].match(/message: ("(?:[^"\\]|\\.)*")/);
        if (literal && !lines[at].includes(`${variable}.code`) && !lines[at].includes(`${variable}.message`)) {
          offenders.push(`${path.relative(sourceRoot, file).split(path.sep).join("/")}:${at + 1}`);
          break;
        }
      }
    });
  }
  assert.deepEqual(offenders, []);
});

// Known debt, bounded: callers of the null-returning fetchCurrentEffectiveAccess() cannot tell "denied" from
// "unavailable". New code must use fetchCurrentEffectiveAccessResult()/denyUnlessGranted(); the count may only fall.
const NULL_RETURNING_ACCESS_CALL_SITES = 44;

test("no new caller adopts the null-returning effective-access fetch (use the classified result)", () => {
  const sourceRoot = path.resolve(__dirname, "..", "src");
  let sites = 0;
  for (const file of collectSources(sourceRoot)) {
    sites += (readFileSync(file, "utf8").match(/= await fetchCurrentEffectiveAccess\(\)/g) ?? []).length;
  }
  assert.ok(sites <= NULL_RETURNING_ACCESS_CALL_SITES, `${sites} call sites (baseline ${NULL_RETURNING_ACCESS_CALL_SITES})`);
});
