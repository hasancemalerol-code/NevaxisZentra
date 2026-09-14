#!/usr/bin/env node

import { readFileSync } from "node:fs";
import { resolve } from "node:path";

const ROOT = resolve(import.meta.dirname, "../../..");

function requireText(file, text, label) {
  const content = readFileSync(resolve(ROOT, file), "utf8");
  if (!content.includes(text)) {
    throw new Error(`${label} missing in ${file}`);
  }
}

requireText("apps/api/GoldenDemoP2/GoldenDemoP2EndpointRouteBuilderExtensions.cs", 'MapGet("/presentation-status"', "P2 presentation endpoint");
requireText("apps/api/GoldenDemoP2/GoldenDemoP2EndpointRouteBuilderExtensions.cs", 'MapPost("/events/{eventId:guid}/reset"', "P2 reset endpoint");
requireText("packages/NevaxisFactOS.Persistence/GoldenDemoP2/GoldenDemoP2Service.cs", "ResetPilotEventAsync", "P2 reset service");
requireText("apps/web/src/features/golden-demo-p2/components/golden-demo-p2-presentation-panel.tsx", "golden-demo-p2-reset", "P2 reset UI");

console.log("GOLDEN_DEMO_P2_RUNTIME_VERIFIER = PASS");
