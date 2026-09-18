#!/usr/bin/env node

import { readFileSync } from "node:fs";
import { resolve } from "node:path";

const ROOT = resolve(import.meta.dirname, "../../..");

const requiredPaths = [
  "apps/web/src/app/(app)/golden-demo/uretim-kurtarma/page.tsx",
  "apps/web/src/features/golden-demo-p1/components/golden-demo-p1-workspace-view.tsx",
  "apps/web/src/features/golden-demo-p1/server/golden-demo-p1-api-client.ts",
  "apps/api/GoldenDemoP1/GoldenDemoP1EndpointRouteBuilderExtensions.cs",
  "packages/NevaxisZentra.Persistence/GoldenDemoP1/GoldenDemoP1Engines.cs",
  "tests/NevaxisZentra.Persistence.Tests/GoldenDemoP1FoundationTests.cs",
];

for (const relativePath of requiredPaths) {
  readFileSync(resolve(ROOT, relativePath), "utf8");
}

const workspaceView = readFileSync(
  resolve(ROOT, "apps/web/src/features/golden-demo-p1/components/golden-demo-p1-workspace-view.tsx"),
  "utf8",
);

if (!workspaceView.includes("data-testid=\"golden-demo-recovery-good-parts\"")) {
  throw new Error("Primary KPI test id missing");
}

if (workspaceView.match(/\+\s*\d+\s*\/\s*vardiya/i) && !workspaceView.includes("productionRecoveryGoodParts")) {
  throw new Error("Hard-coded recovery KPI detected");
}

const apiEndpoints = readFileSync(
  resolve(ROOT, "apps/api/GoldenDemoP1/GoldenDemoP1EndpointRouteBuilderExtensions.cs"),
  "utf8",
);

for (const route of ["/workspace", "/approve", "/execute", "/complete-intervention", "/verify"]) {
  if (!apiEndpoints.includes(route)) {
    throw new Error(`API route ${route} missing`);
  }
}

console.log("GOLDEN_DEMO_P1_RUNTIME_VERIFIER = PASS");
