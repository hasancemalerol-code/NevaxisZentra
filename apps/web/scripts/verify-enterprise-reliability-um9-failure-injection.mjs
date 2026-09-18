#!/usr/bin/env node

import { readFileSync, existsSync } from "node:fs";
import { resolve } from "node:path";

const ROOT = resolve(import.meta.dirname, "../../..");
const checks = [];

function readRequired(relativePath) {
  const absolutePath = resolve(ROOT, relativePath);
  if (!existsSync(absolutePath)) throw new Error(`Required file missing: ${relativePath}`);
  return readFileSync(absolutePath, "utf8");
}

function assert(condition, message) {
  if (!condition) throw new Error(message);
}

function requireText(source, text, label) {
  assert(source.includes(text), `${label} must contain "${text}"`);
}

const BOUNDED_SCENARIOS = [
  {
    name: "CONNECTOR_CIRCUIT_OPEN",
    file: "packages/NevaxisZentra.Persistence/EnterpriseReliability/EnterpriseReliabilityEngines.cs",
    markers: ["integration.circuit-open", "DefaultFailureThreshold = 5", "DefaultCooldown"],
  },
  {
    name: "RETRY_EXHAUSTION",
    file: "packages/NevaxisZentra.Persistence/EnterpriseReliability/EnterpriseReliabilityEngines.cs",
    markers: ["MaxAttempts = 5", "retry.exhausted", "retry.non-retriable"],
  },
  {
    name: "DATABASE_DEGRADED_READ_ONLY",
    file: "packages/NevaxisZentra.Persistence/EnterpriseReliability/EnterpriseReliabilityEngines.cs",
    markers: ["DegradedReadOnly", "BlocksUnsafeMutation"],
  },
  {
    name: "EDGE_BUFFER_CAPACITY",
    file: "packages/NevaxisZentra.EdgeBuffer/EdgeBufferStore.cs",
    markers: ["CapacityExceeded", "GetEffectiveMaxBufferedEvents"],
  },
  {
    name: "EDGE_CRITICAL_DROP_BLOCKED",
    file: "packages/NevaxisZentra.Persistence/EnterpriseReliability/EnterpriseReliabilityEngines.cs",
    markers: ["AllowsSilentDrop", "EdgeEventClassifications.Critical", "EdgeEventClassifications.Disposable"],
  },
  {
    name: "RECONCILIATION_CONFLICT",
    file: "packages/NevaxisZentra.Persistence/EnterpriseReliability/EnterpriseReliabilityService.cs",
    markers: ["Reconciliation conflict guard blocked", "EnterpriseReliabilityFailureCodes.Conflict"],
  },
  {
    name: "AI_PROVIDER_DEGRADATION",
    file: "packages/NevaxisZentra.Persistence/EnterpriseReliability/EnterpriseReliabilityConstants.cs",
    markers: ["AiProvider", "SystemComponentKinds"],
  },
  {
    name: "EDGE_OFFLINE_BUFFERING",
    file: "packages/NevaxisZentra.Persistence/EnterpriseReliability/EnterpriseReliabilityEngines.cs",
    markers: ["DegradedBuffering", "SystemComponentKinds.EdgeAgent"],
  },
];

function runStaticFailureInjection() {
  for (const scenario of BOUNDED_SCENARIOS) {
    const source = readRequired(scenario.file);
    for (const marker of scenario.markers) {
      requireText(source, marker, scenario.name);
    }
    checks.push(`${scenario.name} = PASS (static)`);
  }

  const edgeScenarios = readRequired("packages/NevaxisZentra.EdgeBuffer/EdgeReliabilityScenarios.cs");
  requireText(edgeScenarios, "BuildOfflineBufferEvents", "offline buffer scenario");
  requireText(edgeScenarios, "CreatePoisonEvent", "poison event scenario");
  requireText(edgeScenarios, "BuildPartialFailureBatch", "partial failure scenario");
  checks.push("EDGE_RELIABILITY_SCENARIOS = PASS (static)");

  const runbooks = [
    "docs/runbooks/api-down.md",
    "docs/runbooks/db-down.md",
    "docs/runbooks/connector-down.md",
    "docs/runbooks/edge-agent-offline.md",
  ];
  for (const runbook of runbooks) {
    const content = readRequired(runbook);
    assert(content.length > 100, `${runbook} must document recovery steps`);
    checks.push(`${runbook} = PASS (documented)`);
  }
}

async function main() {
  runStaticFailureInjection();

  console.log("UM9_FAILURE_INJECTION_VERIFIER = PASS");
  for (const check of checks) {
    console.log(check);
  }
  console.log(`UM9_FAILURE_INJECTION_SUMMARY = ${checks.filter((c) => c.includes("PASS")).length}/${checks.length} passed`);
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
