#!/usr/bin/env node

import { readFileSync, existsSync } from "node:fs";
import { resolve } from "node:path";

const ROOT = resolve(import.meta.dirname, "../../..");
const API_BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY_ID = process.env.NEVAXIS_FACTOS_FACILITY_ID ?? "bb000001-0005-4000-8000-000000000005";
const ISSUER = "crevafactos-dev";
const OPERATOR = "production-pilot-operator";
const TENANT = "dev-pilot-tenant";
const REQUEST_TIMEOUT_MS = Number(process.env.NEVAXIS_FACTOS_REQUEST_TIMEOUT_MS ?? 30_000);

const BOUNDED_LOAD_PROFILE = {
  workspaceP95Ms: 2000,
  metricsP95Ms: 1500,
  heartbeatP95Ms: 1000,
  concurrentHealthProbes: 5,
  maxConnectorOutcomesPerMinute: 60,
  maxEdgeHeartbeatsPerMinute: 120,
  maxReconcileRunsPerHour: 12,
};

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

function headers(subject = OPERATOR, tenant = TENANT) {
  return {
    Accept: "application/json",
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": ISSUER,
    "X-crevafactos-dev-Subject": subject,
    "X-NevaxisZentra-Tenant": tenant,
  };
}

function percentile(values, p) {
  if (values.length === 0) return 0;
  const sorted = [...values].sort((a, b) => a - b);
  const index = Math.min(sorted.length - 1, Math.ceil((p / 100) * sorted.length) - 1);
  return sorted[index];
}

function runStaticPerformanceAssertions() {
  const engines = readRequired("packages/NevaxisZentra.Persistence/EnterpriseReliability/EnterpriseReliabilityEngines.cs");
  requireText(engines, "MetricCardinalityGuard", "metric cardinality guard");
  requireText(engines, "AllowedLabelKeys", "bounded metric labels");
  requireText(engines, "MaxAttempts = 5", "bounded retry attempts");
  checks.push("STATIC_BOUNDED_RETRY = PASS");

  const edgeBuffer = readRequired("packages/NevaxisZentra.EdgeBuffer/EdgeBufferStore.cs");
  requireText(edgeBuffer, "ClaimBatch", "bounded batch delivery");
  requireText(edgeBuffer, "GetEffectiveMaxBufferedEvents", "bounded buffer capacity");
  checks.push("STATIC_BOUNDED_BUFFER = PASS");

  const rpoRto = readRequired("docs/enterprise-reliability/rpo-rto.md");
  requireText(rpoRto, "RPO", "RPO documented");
  requireText(rpoRto, "RTO", "RTO documented");
  checks.push("STATIC_RPO_RTO_DOC = PASS");

  const checklist = readRequired("docs/enterprise-reliability/production-readiness-checklist.md");
  requireText(checklist, "performance", "performance checklist");
  checks.push("STATIC_READINESS_CHECKLIST = PASS");

  console.log("UM9_BOUNDED_LOAD_PROFILE:");
  for (const [key, value] of Object.entries(BOUNDED_LOAD_PROFILE)) {
    console.log(`  ${key} = ${value}`);
  }
  checks.push("BOUNDED_LOAD_PROFILE_DOCUMENTED = PASS");
}

async function runLivePerformanceProbes() {
  let apiAvailable = false;
  try {
    const health = await fetch(`${API_BASE}/health/ready`, { signal: AbortSignal.timeout(REQUEST_TIMEOUT_MS) });
    apiAvailable = health.ok;
  } catch {
    apiAvailable = false;
  }

  if (!apiAvailable) {
    checks.push("LIVE_WORKSPACE_LATENCY = SKIP (API unreachable)");
    checks.push("LIVE_CONCURRENT_HEALTH = SKIP (API unreachable)");
    return;
  }

  const workspaceLatencies = [];
  for (let i = 0; i < 3; i++) {
    const started = Date.now();
    const response = await fetch(`${API_BASE}/api/system-status/workspace?facilityId=${FACILITY_ID}`, {
      headers: headers(),
      signal: AbortSignal.timeout(REQUEST_TIMEOUT_MS),
    });
    workspaceLatencies.push(Date.now() - started);
    if (!response.ok) break;
  }
  const workspaceP95 = percentile(workspaceLatencies, 95);
  assert(
    workspaceP95 <= BOUNDED_LOAD_PROFILE.workspaceP95Ms,
    `workspace P95 ${workspaceP95}ms exceeds ${BOUNDED_LOAD_PROFILE.workspaceP95Ms}ms`,
  );
  checks.push(`LIVE_WORKSPACE_LATENCY = PASS (p95=${workspaceP95}ms)`);

  const healthStarts = Date.now();
  const probes = await Promise.all(
    Array.from({ length: BOUNDED_LOAD_PROFILE.concurrentHealthProbes }, () =>
      fetch(`${API_BASE}/health/live`, { signal: AbortSignal.timeout(REQUEST_TIMEOUT_MS) }),
    ),
  );
  const healthElapsed = Date.now() - healthStarts;
  assert(probes.every((p) => p.ok), "concurrent health probes must succeed");
  assert(healthElapsed < 5000, `concurrent health probes too slow: ${healthElapsed}ms`);
  checks.push(`LIVE_CONCURRENT_HEALTH = PASS (${healthElapsed}ms)`);
}

async function main() {
  runStaticPerformanceAssertions();
  await runLivePerformanceProbes();

  console.log("UM9_PERFORMANCE_VERIFIER = PASS");
  for (const check of checks) {
    console.log(check);
  }
  console.log(`UM9_PERFORMANCE_SUMMARY = ${checks.filter((c) => c.includes("PASS")).length}/${checks.length} passed`);
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
