import assert from "node:assert/strict";
import { readdirSync, readFileSync } from "node:fs";
import Module from "node:module";
import path from "node:path";
import test from "node:test";
import { renderToStaticMarkup } from "react-dom/server";


// Viewing /ayarlar/entegrasyonlar/gercek-fabrika used to POST /api/real-factory-pilot/initialize, which created the
// synthetic PILOT-MES-SOURCE / PILOT-PLC-SOURCE connectors and a SYNTHETIC_PILOT_PASS run in the customer tenant.
// A page load must be strictly read-only; initialization is an explicit, permission-gated, audited POST that the API
// only accepts in Development / Demo / Presentation deployments.
const SESSION_COOKIE = "NevaxisZentra.session=SESSION-VALUE-FOR-TEST; NevaxisZentra.csrf=CSRF-VALUE-FOR-TEST";
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
const moduleLoader = Module as unknown as { _load: (request: string, parent: unknown, isMain: boolean) => unknown };
const originalLoad = moduleLoader._load;
const originalFetch = globalThis.fetch;
const savedEnv = new Map<string, string | undefined>();

type Call = { method: string; path: string };

function installRuntime(grantedPermissions: readonly string[], currentBody: unknown): Call[] {
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
        headers: async () => new Headers({ cookie: SESSION_COOKIE }),
        cookies: async () => ({
          getAll: () =>
            SESSION_COOKIE.split(";").map((part) => {
              const trimmed = part.trim();
              const separator = trimmed.indexOf("=");
              return { name: trimmed.slice(0, separator), value: trimmed.slice(separator + 1) };
            }),
          get: () => undefined,
        }),
      };
    }
    if (request === "next/navigation") {
      const actual = originalLoad.call(this, request, parent, isMain) as Record<string, unknown>;
      return {
        ...actual,
        redirect: (url: string) => {
          throw new Error(`REDIRECT:${url}`);
        },
        forbidden: () => {
          throw new Error("FORBIDDEN");
        },
      };
    }
    if (request === "next/cache") {
      return { revalidatePath: () => undefined };
    }
    return originalLoad.call(this, request, parent, isMain);
  };

  const calls: Call[] = [];
  globalThis.fetch = (async (input: RequestInfo | URL, init?: RequestInit) => {
    const url = new URL(String(input));
    calls.push({ method: (init?.method ?? "GET").toUpperCase(), path: url.pathname });
    if (url.pathname === "/api/admin-access/me/effective-access") {
      return new Response(JSON.stringify({ grantedPermissionKeys: grantedPermissions }), { status: 200 });
    }
    if (url.pathname === "/api/real-factory-pilot/current") {
      return new Response(JSON.stringify(currentBody), { status: 200 });
    }
    if (url.pathname === "/api/real-factory-pilot/initialize") {
      return new Response(JSON.stringify({}), { status: 200 });
    }
    return new Response("{}", { status: 404 });
  }) as typeof fetch;
  return calls;
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

const pilotCalls = (calls: Call[]) => calls.filter((call) => call.path.startsWith("/api/real-factory-pilot"));

test("page load on a fresh Production tenant only issues read-only GETs and reports an empty tenant", async () => {
  const calls = installRuntime(["real-factory-pilot.read", "real-factory-pilot.manage"], {
    workspace: null,
    syntheticInitializationAvailable: false,
  });
  try {
    const { readRealFactoryPilotWorkspaceState } = await import(
      "../src/features/real-factory-pilot/server/real-factory-pilot-reader"
    );
    for (let i = 0; i < 3; i++) {
      const result = await readRealFactoryPilotWorkspaceState();
      assert.equal(result.status, "success", JSON.stringify(result) + JSON.stringify(calls));
      if (result.status !== "success") return;
      assert.equal(result.workspace, null);
      assert.equal(result.syntheticInitializationAvailable, false);
    }

    const pilot = pilotCalls(calls);
    assert.equal(pilot.length, 3);
    for (const call of pilot) {
      assert.deepEqual(call, { method: "GET", path: "/api/real-factory-pilot/current" });
    }
    assert.equal(calls.some((call) => call.method !== "GET"), false, "a page load must never issue a mutating request");
  } finally {
    restore();
  }
});

test("a page load never initializes even when the caller holds the manage permission", async () => {
  const calls = installRuntime(["real-factory-pilot.read", "real-factory-pilot.manage", "real-factory-pilot.run"], {
    workspace: null,
    syntheticInitializationAvailable: true,
  });
  try {
    const { readRealFactoryPilotWorkspaceState } = await import(
      "../src/features/real-factory-pilot/server/real-factory-pilot-reader"
    );
    const result = await readRealFactoryPilotWorkspaceState();
    assert.equal(result.status, "success");
    assert.equal(pilotCalls(calls).some((call) => call.path.endsWith("/initialize")), false);
  } finally {
    restore();
  }
});

test("explicit initialization is authorization-gated: without real-factory-pilot.manage no request is sent", async () => {
  const calls = installRuntime(["real-factory-pilot.read"], { workspace: null, syntheticInitializationAvailable: true });
  try {
    const { initializeRealFactoryPilotAction } = await import(
      "../src/features/real-factory-pilot/server/real-factory-pilot-actions"
    );
    await assert.rejects(
      () => initializeRealFactoryPilotAction(),
      (error: unknown) => {
        const text = String(error);
        assert.match(text, /REDIRECT:.*pilot=denied/, text);
        return true;
      },
    );
    assert.equal(pilotCalls(calls).length, 0);
  } finally {
    restore();
  }
});

test("explicit initialization with the manage permission is a POST (never a GET)", async () => {
  const calls = installRuntime(["real-factory-pilot.read", "real-factory-pilot.manage"], {
    workspace: null,
    syntheticInitializationAvailable: true,
  });
  try {
    const { initializeRealFactoryPilotAction } = await import(
      "../src/features/real-factory-pilot/server/real-factory-pilot-actions"
    );
    await assert.rejects(
      () => initializeRealFactoryPilotAction(),
      /REDIRECT:\/ayarlar\/entegrasyonlar\/gercek-fabrika$/,
    ); // success ends in a redirect back to the page
    const pilot = pilotCalls(calls);
    assert.deepEqual(pilot, [{ method: "POST", path: "/api/real-factory-pilot/initialize" }]);
  } finally {
    restore();
  }
});

async function loadEmptyState() {
  // The component imports the server action module, which reads the runtime config on first import: load it with
  // the packaged-production environment in place so the module cache is not poisoned for the other tests.
  installRuntime([], { workspace: null, syntheticInitializationAvailable: false });
  try {
    return (await import("../src/features/real-factory-pilot/components/real-factory-pilot-empty-state")).RealFactoryPilotEmptyState;
  } finally {
    restore();
  }
}

test("the Production empty state is truthful and offers no synthetic-data control", async () => {
  const RealFactoryPilotEmptyState = await loadEmptyState();
  const html = renderToStaticMarkup(<RealFactoryPilotEmptyState canInitialize={false} notice={null} />);
  assert.match(html, /Henüz gerçek fabrika bağlantısı yok/);
  assert.match(html, /herhangi bir\s+bağlantı veya veri oluşturmaz/);
  assert.doesNotMatch(html, /<form/);
  assert.doesNotMatch(html, /PILOT-|SYNTHETIC_PILOT_PASS/);
});

test("Development/Demo with the manage permission still offers the explicit, labelled synthetic initialization", async () => {
  const RealFactoryPilotEmptyState = await loadEmptyState();
  const html = renderToStaticMarkup(<RealFactoryPilotEmptyState canInitialize notice={null} />);
  assert.match(html, /real-factory-pilot-initialize-form/);
  assert.match(html, /Sentetik pilot verisini oluştur/);
  assert.match(html, /SENTETİK/);
  assert.match(html, /denetim kaydına yazılır/);

  const denied = renderToStaticMarkup(<RealFactoryPilotEmptyState canInitialize={false} notice="denied" />);
  assert.match(denied, /yetkiniz yok/);
});

function sources(directory: string, files: string[] = []): string[] {
  for (const entry of readdirSync(directory, { withFileTypes: true })) {
    const full = path.join(directory, entry.name);
    if (entry.isDirectory()) sources(full, files);
    else if (/\.(ts|tsx)$/.test(entry.name)) files.push(full);
  }
  return files;
}

test("only the explicit server action can reach the initialize endpoint (page, reader and components cannot)", () => {
  const sourceRoot = path.resolve(__dirname, "..", "src");
  const callers = sources(sourceRoot)
    .filter((file) => /\binitializeRealFactoryPilot\b/.test(readFileSync(file, "utf8")))
    .map((file) => path.relative(sourceRoot, file).split(path.sep).join("/"))
    .sort();
  assert.deepEqual(callers, [
    "features/real-factory-pilot/server/real-factory-pilot-actions.ts",
    "features/real-factory-pilot/server/real-factory-pilot-api-client.ts",
  ]);

  const action = readFileSync(
    path.join(sourceRoot, "features", "real-factory-pilot", "server", "real-factory-pilot-actions.ts"),
    "utf8",
  );
  assert.match(action, /^"use server";/);
  // A "use server" module may export only async functions; Next fails the production build otherwise.
  assert.doesNotMatch(action, /^export (const|let|var|type|interface|class|enum)/m);
  assert.match(action, /denyUnlessGranted\("real-factory-pilot\.manage"\)/);

  const page = readFileSync(
    path.join(sourceRoot, "app", "(app)", "ayarlar", "entegrasyonlar", "gercek-fabrika", "page.tsx"),
    "utf8",
  );
  assert.doesNotMatch(page, /initializeRealFactoryPilot|real-factory-pilot-actions/);
  const client = readFileSync(
    path.join(sourceRoot, "features", "real-factory-pilot", "server", "real-factory-pilot-api-client.ts"),
    "utf8",
  );
  assert.match(client, /initialize", \{ method: "POST" \}/);
});
