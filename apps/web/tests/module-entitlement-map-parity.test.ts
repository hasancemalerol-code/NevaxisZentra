import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import path from "node:path";
import test from "node:test";

import { resolveEntitlementCodeFromPath } from "../src/features/saas-subscription/domain/saas-subscription-labels";

// The API (C#) is authoritative for module access; the web map only mirrors it for navigation. A route known
// to one side and not the other makes a shipped page show "module not in your subscription" for a licensed
// tenant, so the two maps must stay identical.
const repositoryRoot = path.resolve(__dirname, "..", "..", "..");
const csharp = readFileSync(
  path.join(repositoryRoot, "packages", "NevaxisZentra.Persistence", "SaasSubscription", "SaasSubscriptionModuleEntitlements.cs"),
  "utf8",
);
const web = readFileSync(
  path.join(repositoryRoot, "apps", "web", "src", "features", "saas-subscription", "domain", "saas-subscription-labels.ts"),
  "utf8",
);

function csharpRoutes(): Map<string, string> {
  const codeNames = new Map<string, string>(
    [...csharp.matchAll(/public const string (\w+) = "(module\.[a-z-]+)";/g)].map((m) => [m[1], m[2]]),
  );
  const routes = new Map<string, string>();
  for (const m of csharp.matchAll(/\("(\/[a-z0-9\/-]+)", EntitlementCodes\.(\w+)\)/g)) {
    const code = codeNames.get(m[2]);
    assert.ok(code, `unknown entitlement constant ${m[2]}`);
    routes.set(m[1], code);
  }
  return routes;
}

function webRoutes(): Map<string, string> {
  const routes = new Map<string, string>();
  for (const m of web.matchAll(/\{ prefix: "(\/[a-z0-9\/-]+)", code: "(module\.[a-z-]+)" \}/g)) {
    routes.set(m[1], m[2]);
  }
  return routes;
}

test("API and web module entitlement route maps are identical", () => {
  const api = csharpRoutes();
  const mirror = webRoutes();
  assert.ok(api.size > 20, "the C# map was parsed");
  assert.deepEqual([...api.entries()].sort(), [...mirror.entries()].sort());
});

test("the executive command center and decision center resolve to the production module", () => {
  assert.equal(resolveEntitlementCodeFromPath("/yonetici-komuta-merkezi"), "module.production");
  assert.equal(resolveEntitlementCodeFromPath("/fabrika-karar-merkezi"), "module.production");
  assert.equal(csharpRoutes().get("/yonetici-komuta-merkezi"), "module.production");
  assert.equal(csharpRoutes().get("/fabrika-karar-merkezi"), "module.production");
});
