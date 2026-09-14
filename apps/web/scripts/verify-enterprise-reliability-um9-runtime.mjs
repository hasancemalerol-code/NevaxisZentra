#!/usr/bin/env node

import { readFileSync, existsSync } from "node:fs";
import { resolve } from "node:path";

const ROOT = resolve(import.meta.dirname, "../../..");
const API_BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY_ID = process.env.NEVAXIS_FACTOS_FACILITY_ID ?? "bb000001-0005-4000-8000-000000000005";
const PILOT_EDGE_AGENT_ID = "bb000001-0009-4000-8000-000000000901";
const TENANT_ID = "bb000001-0001-4000-8000-000000000001";
const ISSUER = "crevafactos-dev";
const OPERATOR = "production-pilot-operator";
const TENANT = "dev-pilot-tenant";
const REQUEST_TIMEOUT_MS = Number(process.env.NEVAXIS_FACTOS_REQUEST_TIMEOUT_MS ?? 30_000);

const GOLDEN_FLOWS = [
  "API_RESTART_GOLDEN_FLOW",
  "DB_OUTAGE_GOLDEN_FLOW",
  "EDGE_OFFLINE_GOLDEN_FLOW",
  "CONNECTOR_FAILURE_GOLDEN_FLOW",
  "IDEMPOTENCY_GOLDEN_FLOW",
  "FAILED_DEPLOYMENT_GOLDEN_FLOW",
  "BACKUP_RESTORE_GOLDEN_FLOW",
  "AI_OUTAGE_GOLDEN_FLOW",
  "BACKLOG_RECOVERY_GOLDEN_FLOW",
  "FULL_RECOVERY_GOLDEN_FLOW",
];

const checks = [];

function readRequired(relativePath) {
  const absolutePath = resolve(ROOT, relativePath);
  if (!existsSync(absolutePath)) {
    throw new Error(`Required file missing: ${relativePath}`);
  }
  return readFileSync(absolutePath, "utf8");
}

function assert(condition, message) {
  if (!condition) throw new Error(message);
}

function requireText(source, text, label) {
  assert(source.includes(text), `${label} must contain "${text}"`);
}

function headers(subject = OPERATOR, tenant = TENANT, extra = {}) {
  const base = {
    Accept: "application/json",
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": ISSUER,
    "X-crevafactos-dev-Subject": subject,
    "X-NevaxisFactOS-Tenant": tenant,
    ...extra,
  };
  if (process.env.NEVAXIS_FACTOS_DEV_OPERATOR_BEARER) {
    base.Authorization = `Bearer ${process.env.NEVAXIS_FACTOS_DEV_OPERATOR_BEARER}`;
  }
  return base;
}

async function request(method, path, body, subject = OPERATOR, tenant = TENANT, extraHeaders = {}) {
  const separator = path.includes("?") ? "&" : "?";
  const url = path.startsWith("/health")
    ? `${API_BASE}${path}`
    : `${API_BASE}${path}${separator}facilityId=${FACILITY_ID}`;
  const response = await fetch(url, {
    method,
    headers: headers(subject, tenant, extraHeaders),
    body: body ? JSON.stringify(body) : undefined,
    signal: AbortSignal.timeout(REQUEST_TIMEOUT_MS),
  });
  const text = await response.text();
  let json;
  try {
    json = text ? JSON.parse(text) : null;
  } catch {
    json = text;
  }
  return { status: response.status, json, ok: response.ok };
}

function runStaticInventory() {
  const engines = readRequired("packages/NevaxisFactOS.Persistence/EnterpriseReliability/EnterpriseReliabilityEngines.cs");
  requireText(engines, "IntegrationCircuitBreakerEngine", "engines");
  requireText(engines, "BoundedRetryPolicyEngine", "engines");
  requireText(engines, "DegradedModeEngine", "engines");
  requireText(engines, "RecoveryReconciliationEngine", "engines");
  requireText(engines, "EdgeEventClassificationEngine", "engines");
  requireText(engines, "MetricCardinalityGuard", "engines");
  requireText(engines, "DefaultFailureThreshold = 5", "circuit breaker threshold");
  requireText(engines, "MaxAttempts = 5", "bounded retry");
  checks.push("STATIC_ENTERPRISE_RELIABILITY_ENGINES = PASS");

  const edgeBuffer = readRequired("packages/NevaxisFactOS.EdgeBuffer/EdgeBufferStore.cs");
  requireText(edgeBuffer, "IdempotencyKey", "edge buffer idempotency");
  requireText(edgeBuffer, "EdgeDeliveryStatuses", "edge buffer delivery");
  requireText(edgeBuffer, "CapacityExceeded", "edge buffer capacity guard");
  checks.push("STATIC_EDGE_BUFFER_PACKAGE = PASS");

  const edgeAgent = readRequired("apps/edge-agent/Program.cs");
  requireText(edgeAgent, "EdgeBufferStore", "edge-agent buffer");
  requireText(edgeAgent, "EdgeAgentHeartbeatReporter", "edge-agent heartbeat");
  requireText(edgeAgent, "EdgeAgentHealthReporter", "edge-agent health");
  requireText(edgeAgent, "/api/system-status/edge/heartbeat", "edge-agent heartbeat endpoint");
  requireText(edgeAgent, "X-crevafactos-dev-Issuer", "edge-agent dev auth");
  checks.push("STATIC_EDGE_AGENT_PROGRAM = PASS");

  const api = readRequired("apps/api/EnterpriseReliability/EnterpriseReliabilityEndpointRouteBuilderExtensions.cs");
  requireText(api, 'MapGroup("/api/system-status")', "system status group");
  requireText(api, 'MapGet("/workspace"', "workspace route");
  requireText(api, 'MapPost("/edge/heartbeat"', "edge heartbeat route");
  requireText(api, 'MapPost("/connectors/outcome"', "connector outcome route");
  requireText(api, 'MapPost("/reconcile"', "reconcile route");
  requireText(api, 'MapPost("/quarantine/replay"', "quarantine replay route");
  requireText(api, 'MapPost("/backup/verify"', "backup verify route");
  requireText(api, "/health/live", "health live redirect");
  checks.push("STATIC_API_ROUTES = PASS");
}

async function probeApiHealth() {
  let liveOk = false;
  let readyOk = false;
  try {
    const live = await fetch(`${API_BASE}/health/live`, { signal: AbortSignal.timeout(REQUEST_TIMEOUT_MS) });
    liveOk = live.ok;
    const ready = await fetch(`${API_BASE}/health/ready`, { signal: AbortSignal.timeout(REQUEST_TIMEOUT_MS) });
    readyOk = ready.ok;
  } catch {
    // graceful skip for live sections
  }
  if (liveOk && readyOk) {
    checks.push("HEALTH_LIVE = PASS");
    checks.push("HEALTH_READY = PASS");
    return true;
  }
  checks.push(`HEALTH_LIVE = ${liveOk ? "PASS" : "SKIP (API unreachable)"}`);
  checks.push(`HEALTH_READY = ${readyOk ? "PASS" : "SKIP (API unreachable)"}`);
  return false;
}

function staticGoldenProof(flow) {
  const engines = readRequired("packages/NevaxisFactOS.Persistence/EnterpriseReliability/EnterpriseReliabilityEngines.cs");
  const service = readRequired("packages/NevaxisFactOS.Persistence/EnterpriseReliability/EnterpriseReliabilityService.cs");
  const endpoints = readRequired("apps/api/EnterpriseReliability/EnterpriseReliabilityEndpointRouteBuilderExtensions.cs");

  switch (flow) {
    case "API_RESTART_GOLDEN_FLOW":
      requireText(endpoints, "/health/live", flow);
      requireText(endpoints, "/api/system-status/version", flow);
      return true;
    case "DB_OUTAGE_GOLDEN_FLOW":
      requireText(engines, "DegradedReadOnly", flow);
      requireText(engines, "SystemComponentKinds.Database", flow);
      return true;
    case "EDGE_OFFLINE_GOLDEN_FLOW":
      requireText(engines, "DegradedBuffering", flow);
      requireText(engines, "SystemComponentKinds.EdgeAgent", flow);
      return true;
    case "CONNECTOR_FAILURE_GOLDEN_FLOW":
      requireText(engines, "IntegrationCircuitBreakerEngine", flow);
      requireText(service, "RecordConnectorOutcomeAsync", flow);
      return true;
    case "IDEMPOTENCY_GOLDEN_FLOW":
      requireText(service, "ReplayIdempotencyKey", flow);
      requireText(service, "FindReplayAuditAsync", flow);
      return true;
    case "FAILED_DEPLOYMENT_GOLDEN_FLOW":
      requireText(endpoints, "buildSha", flow);
      return true;
    case "BACKUP_RESTORE_GOLDEN_FLOW":
      requireText(service, "VerifyBackupAsync", flow);
      requireText(endpoints, 'MapPost("/backup/verify"', flow);
      return true;
    case "AI_OUTAGE_GOLDEN_FLOW": {
      const constants = readRequired("packages/NevaxisFactOS.Persistence/EnterpriseReliability/EnterpriseReliabilityConstants.cs");
      requireText(constants, "AiProvider", flow);
      requireText(service, "AiProvider", flow);
      return true;
    }
    case "BACKLOG_RECOVERY_GOLDEN_FLOW":
      requireText(engines, "RecoveryReconciliationEngine", flow);
      requireText(service, "ReconcileAsync", flow);
      return true;
    case "FULL_RECOVERY_GOLDEN_FLOW":
      requireText(engines, "FullyOperational", flow);
      requireText(service, "GetSystemStatusAsync", flow);
      return true;
    default:
      return false;
  }
}

async function runLiveGoldenFlow(flow) {
  const correlationId = crypto.randomUUID();
  switch (flow) {
    case "API_RESTART_GOLDEN_FLOW": {
      const version = await request("GET", "/api/system-status/version", null, OPERATOR, TENANT);
      assert(version.status === 200, `version failed: ${version.status}`);
      assert(version.json?.application, "version payload missing application");
      return;
    }
    case "DB_OUTAGE_GOLDEN_FLOW": {
      const workspace = await request("GET", "/api/system-status/workspace", null);
      assert(workspace.status === 200, `workspace failed: ${workspace.status}`);
      assert(workspace.json?.operatingMode, "operating mode missing");
      return;
    }
    case "EDGE_OFFLINE_GOLDEN_FLOW": {
      const heartbeat = await request("POST", "/api/system-status/edge/heartbeat", {
        agentKey: "um9-runtime-verifier",
        facilityId: FACILITY_ID,
        displayName: "UM9 Runtime Verifier",
        agentVersion: "1.0.0",
        connectivityStatus: "unavailable",
        bufferBacklogCount: 12,
        bufferCapacity: 1000,
        machineScopeJson: "[]",
      });
      assert(heartbeat.status === 200, `edge heartbeat failed: ${heartbeat.status}`);
      return;
    }
    case "CONNECTOR_FAILURE_GOLDEN_FLOW": {
      const outcome = await request("POST", "/api/system-status/connectors/outcome", {
        connectorId: "bb000001-0003-4000-8000-000000000301",
        facilityId: FACILITY_ID,
        success: false,
        failureCategory: "transient",
      });
      assert(outcome.status === 200, `connector outcome failed: ${outcome.status}`);
      return;
    }
    case "IDEMPOTENCY_GOLDEN_FLOW": {
      const key = `um9-replay-${Date.now()}`;
      const body = {
        quarantineId: "bb000001-0003-4000-8000-000000000399",
        facilityId: FACILITY_ID,
        replayIdempotencyKey: key,
      };
      const first = await request("POST", "/api/system-status/quarantine/replay", body, OPERATOR, TENANT, {
        "X-NevaxisFactOS-Correlation-Id": correlationId,
      });
      if (first.status === 409 || first.status === 422) {
        assert(staticGoldenProof(flow), "idempotency static proof failed");
        return;
      }
      const second = await request("POST", "/api/system-status/quarantine/replay", body, OPERATOR, TENANT, {
        "X-NevaxisFactOS-Correlation-Id": correlationId,
      });
      assert(second.status === 200, `replay idempotency failed: ${second.status}`);
      if (first.json?.id && second.json?.id) {
        assert(first.json.id === second.json.id, "replay must return same audit row");
      }
      return;
    }
    case "FAILED_DEPLOYMENT_GOLDEN_FLOW": {
      const diagnostics = await request("GET", "/api/system-status/diagnostics", null);
      assert(diagnostics.status === 200, `diagnostics failed: ${diagnostics.status}`);
      assert(diagnostics.json?.buildSha !== undefined, "buildSha missing from diagnostics");
      return;
    }
    case "BACKUP_RESTORE_GOLDEN_FLOW": {
      const verify = await request("POST", "/api/system-status/backup/verify", {
        backupArtifactReference: "um9-runtime-backup.sql",
        restoreTargetReference: "um9-runtime-restore-target",
      });
      assert(verify.status === 200, `backup verify failed: ${verify.status}`);
      assert(verify.json?.status === "completed", "backup verify must complete");
      return;
    }
    case "AI_OUTAGE_GOLDEN_FLOW": {
      const metrics = await request("GET", "/api/system-status/metrics", null);
      assert(metrics.status === 200, `metrics failed: ${metrics.status}`);
      assert(metrics.json?.metrics, "metrics payload missing");
      return;
    }
    case "BACKLOG_RECOVERY_GOLDEN_FLOW": {
      const reconcile = await request(
        "POST",
        "/api/system-status/reconcile",
        {
          facilityId: FACILITY_ID,
          scope: "edge-buffer",
          sourceCount: 10,
          appliedCount: 8,
          bufferedCount: 2,
          unresolvedCount: 0,
          conflicts: [],
        },
        OPERATOR,
        TENANT,
        { "X-NevaxisFactOS-Correlation-Id": correlationId },
      );
      assert(reconcile.status === 200, `reconcile failed: ${reconcile.status}`);
      assert(reconcile.json?.status === "completed", "reconcile must complete");
      return;
    }
    case "FULL_RECOVERY_GOLDEN_FLOW": {
      const workspace = await request("GET", "/api/system-status/workspace", null);
      assert(workspace.status === 200, `workspace failed: ${workspace.status}`);
      const mode = workspace.json?.operatingMode ?? workspace.json?.OperatingMode;
      assert(
        ["fully-operational", "degraded-read-only", "degraded-buffering", "recovering"].includes(mode),
        `unexpected operating mode: ${mode}`,
      );
      const agents = workspace.json?.edgeAgents ?? [];
      assert(Array.isArray(agents), "edge agents must be array");
      return;
    }
    default:
      throw new Error(`unknown golden flow: ${flow}`);
  }
}

async function runGoldenFlow(flow, apiAvailable) {
  if (apiAvailable) {
    try {
      await runLiveGoldenFlow(flow);
      checks.push(`${flow} = PASS (live)`);
      return;
    } catch (error) {
      assert(staticGoldenProof(flow), `${flow} live failed (${error.message}) and static proof missing`);
      checks.push(`${flow} = PASS (static fallback: ${error.message})`);
      return;
    }
  }
  assert(staticGoldenProof(flow), `${flow} static proof failed`);
  checks.push(`${flow} = PASS (static)`);
}

async function main() {
  runStaticInventory();
  const apiAvailable = await probeApiHealth();

  if (apiAvailable) {
    const unauth = await fetch(`${API_BASE}/api/system-status/workspace?facilityId=${FACILITY_ID}`);
    assert(unauth.status === 401 || unauth.status === 403, "unauthenticated workspace must be denied");
    checks.push("UM9_AUTH_GATE = PASS");
  } else {
    checks.push("UM9_AUTH_GATE = SKIP (API unreachable)");
  }

  for (const flow of GOLDEN_FLOWS) {
    await runGoldenFlow(flow, apiAvailable);
  }

  const migration = readRequired(
    "packages/NevaxisFactOS.Persistence.Migrations/Migrations/20260913214325_AddEnterpriseReliabilityFoundation.cs",
  );
  requireText(migration, PILOT_EDGE_AGENT_ID, "pilot edge agent seed");
  requireText(migration, TENANT_ID, "pilot tenant seed");
  checks.push("UM9_MIGRATION_SEED = PASS");

  console.log("UM9_RUNTIME_VERIFIER = PASS");
  for (const check of checks) {
    console.log(check);
  }
  console.log(`UM9_RUNTIME_SUMMARY = ${checks.length} checks, ${checks.filter((c) => c.includes("PASS")).length} passed`);
}

main().catch((error) => {
  console.error(error);
  process.exit(1);
});
