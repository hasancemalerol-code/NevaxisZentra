import { execSync, spawn } from "node:child_process";
import { existsSync, readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const currentFilePath = fileURLToPath(import.meta.url);
const scriptsDirectory = dirname(currentFilePath);
const webRoot = resolve(scriptsDirectory, "..");
const projectRoot = resolve(webRoot, "../..");

const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://localhost:5000";
const WEB = process.env.NEVAXIS_FACTOS_WEB_BASE_URL ?? "http://localhost:3001";
const TENANT = process.env.NEVAXIS_FACTOS_TENANT ?? "dev-pilot-tenant";
const OPERATOR = process.env.NEVAXIS_FACTOS_SUBJECT ?? "production-pilot-operator";
const ISSUER = "crevafactos-dev";
const FACILITY = process.env.NEVAXIS_FACTOS_FACILITY_ID ?? "bb000001-0005-4000-8000-000000000005";
const PILOT_WO = "bb000001-0081-4000-8000-000000000081";
const PILOT_PLAN = "bb000001-0082-4000-8000-000000000082";
const PILOT_OVERDUE_DUE = "bb000001-0086-4000-8000-000000000086";

function fail(message) {
  console.error(`Maintenance integration gate verification failed: ${message}`);
  process.exit(1);
}

function readRequired(relativePath) {
  const absolutePath = resolve(projectRoot, relativePath);
  if (!existsSync(absolutePath)) {
    fail(`Required file is missing: ${relativePath}`);
  }
  return readFileSync(absolutePath, "utf8").replaceAll("\r\n", "\n");
}

function requireText(source, expected, label) {
  if (!source.includes(expected)) {
    fail(`${label} must contain "${expected}".`);
  }
}

function forbidText(source, forbidden, label) {
  if (source.includes(forbidden)) {
    fail(`${label} must not contain "${forbidden}".`);
  }
}

function headers(subject = OPERATOR, extra = {}) {
  return {
    Accept: "application/json",
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": ISSUER,
    "X-crevafactos-dev-Subject": subject,
    "X-NevaxisZentra-Tenant": TENANT,
    ...extra,
  };
}

function getListeningPid(port) {
  try {
    const output = execSync(`netstat -ano | findstr ":${port}"`, { encoding: "utf8" });
    for (const line of output.split(/\r?\n/)) {
      if (line.includes("LISTENING")) {
        const parts = line.trim().split(/\s+/);
        const pid = Number(parts.at(-1));
        if (Number.isFinite(pid) && pid > 0) {
          return pid;
        }
      }
    }
  } catch {
    return null;
  }
  return null;
}

function stopPid(pid, label) {
  if (!pid) {
    return;
  }
  try {
    execSync(`taskkill /PID ${pid} /F`, { stdio: "ignore" });
    console.log(`Stopped ${label} PID ${pid}.`);
  } catch {
    console.warn(`Could not stop ${label} PID ${pid}.`);
  }
}

async function waitForHealth(timeoutMs = 120000) {
  const started = Date.now();
  while (Date.now() - started < timeoutMs) {
    try {
      const response = await fetch(`${BASE}/health/live`);
      if (response.ok) {
        return true;
      }
    } catch {
      // retry
    }
    await new Promise((resolvePromise) => setTimeout(resolvePromise, 2000));
  }
  return false;
}

async function jsonRequest(method, path, body, subject = OPERATOR) {
  const response = await fetch(`${BASE}${path}`, {
    method,
    headers: headers(subject),
    body: body === undefined ? undefined : JSON.stringify(body),
  });
  const text = await response.text();
  let parsed = null;
  if (text) {
    try {
      parsed = JSON.parse(text);
    } catch {
      parsed = text;
    }
  }
  return { status: response.status, body: parsed };
}

console.log("Running maintenance integration gate verifier.");

// Package wiring — 1L + 2L + 3L + UI-1
requireText(readRequired("apps/api/Program.cs"), "MapMaintenanceEndpoints", "Program.cs");
requireText(readRequired("apps/api/Program.cs"), "AddNevaxisZentraMaintenance", "Program.cs");
requireText(
  readRequired("apps/api/Maintenance/MaintenanceEndpointRouteBuilderExtensions.cs"),
  "/requests/{requestId:guid}/convert-to-work-order",
  "1L endpoints",
);
requireText(
  readRequired("apps/api/Maintenance/MaintenanceExecutionEndpointRouteBuilderExtensions.cs"),
  "/preventive-dues/{dueId:guid}/create-work-order",
  "2L endpoints",
);
requireText(
  readRequired("apps/api/Maintenance/MaintenanceReportingEndpointRouteBuilderExtensions.cs"),
  'MapGet("/overview"',
  "3L reporting endpoints",
);
requireText(
  readRequired("apps/web/src/features/maintenance/components/maintenance-dashboard-view.tsx"),
  "MaintenanceTableActionGroup",
  "UI-1 dashboard",
);
requireText(
  readRequired("apps/web/src/features/maintenance/components/maintenance-table-primitives.tsx"),
  "MaintenanceAssetCell",
  "UI-1 primitives",
);

const duplicatePid = getListeningPid(5011);
if (duplicatePid) {
  stopPid(duplicatePid, "duplicate API on 5011");
}

const canonicalPidBefore = getListeningPid(5000);
if (!canonicalPidBefore) {
  fail(`canonical API must be listening on ${BASE}.`);
}

console.log(`Canonical API URL: ${BASE}`);
console.log(`Canonical API PID: ${canonicalPidBefore}`);

try {
  const gateMarker = `GATE-${Date.now()}`;

  const createRequest = await jsonRequest("POST", `/api/maintenance/requests?facilityId=${FACILITY}`, {
    assetType: "machine",
    assetReference: "GATE-TEST-MACHINE",
    assetName: "Gate Test Machine",
    type: "fault",
    priority: "normal",
    title: `${gateMarker} request`,
    description: "Maintenance integration gate request probe",
    source: "manual",
  });
  if (createRequest.status !== 201) {
    fail(`create request must return HTTP 201, got ${createRequest.status}.`);
  }
  const requestId = createRequest.body?.id;
  const requestVersion = createRequest.body?.version;
  if (!requestId || !requestVersion) {
    fail("create request must return entityId and version.");
  }

  const readRequest = await jsonRequest("GET", `/api/maintenance/requests/${requestId}?facilityId=${FACILITY}`);
  if (readRequest.status !== 200 || !readRequest.body?.requestNumber?.startsWith("MR-")) {
    fail("read request must return MR numbering.");
  }

  const convert = await jsonRequest(
    "POST",
    `/api/maintenance/requests/${requestId}/convert-to-work-order?facilityId=${FACILITY}`,
    { expectedVersion: requestVersion },
  );
  if (convert.status !== 201) {
    fail(`convert request must return HTTP 201, got ${convert.status}.`);
  }
  const workOrderId = convert.body?.id;
  let workOrderVersion = convert.body?.version;
  if (!workOrderId || !workOrderVersion) {
    fail("convert must return work order id and version.");
  }

  const readWorkOrder = await jsonRequest("GET", `/api/maintenance/work-orders/${workOrderId}?facilityId=${FACILITY}`);
  if (readWorkOrder.status !== 200 || !readWorkOrder.body?.workOrderNumber?.startsWith("MWO-")) {
    fail("read work order must return MWO numbering.");
  }

  const assign = await jsonRequest(
    "POST",
    `/api/maintenance/work-orders/${workOrderId}/assign?facilityId=${FACILITY}`,
    { expectedVersion: workOrderVersion, assignedTechnicianSubjectId: "production-pilot-operator" },
  );
  if (assign.status !== 200) {
    fail(`assign must return HTTP 200, got ${assign.status}.`);
  }
  workOrderVersion = assign.body?.version ?? workOrderVersion;

  const start = await jsonRequest(
    "POST",
    `/api/maintenance/work-orders/${workOrderId}/start?facilityId=${FACILITY}`,
    { expectedVersion: workOrderVersion, note: "Gate start" },
  );
  if (start.status !== 200) {
    fail(`start must return HTTP 200, got ${start.status}.`);
  }
  workOrderVersion = start.body?.version ?? workOrderVersion;

  const note = await jsonRequest(
    "POST",
    `/api/maintenance/work-orders/${workOrderId}/notes?facilityId=${FACILITY}`,
    { noteType: "observation", text: `${gateMarker} note`, isImportant: false },
  );
  if (note.status !== 201) {
    fail(`create work note must return HTTP 201, got ${note.status}.`);
  }

  const wait = await jsonRequest(
    "POST",
    `/api/maintenance/work-orders/${workOrderId}/wait?facilityId=${FACILITY}`,
    { expectedVersion: workOrderVersion, note: "Gate wait" },
  );
  if (wait.status !== 200) {
    fail(`wait must return HTTP 200, got ${wait.status}.`);
  }
  workOrderVersion = wait.body?.version ?? workOrderVersion;

  const resume = await jsonRequest(
    "POST",
    `/api/maintenance/work-orders/${workOrderId}/resume?facilityId=${FACILITY}`,
    { expectedVersion: workOrderVersion, note: "Gate resume" },
  );
  if (resume.status !== 200) {
    fail(`resume must return HTTP 200, got ${resume.status}.`);
  }
  workOrderVersion = resume.body?.version ?? workOrderVersion;

  const partUsage = await jsonRequest(
    "POST",
    `/api/maintenance/work-orders/${workOrderId}/parts?facilityId=${FACILITY}`,
    {
      partReference: "GATE-PART-001",
      partName: "Gate test filter",
      quantity: 1,
      unit: "adet",
      sourceSystem: "maintenance-gate",
      estimatedUnitCost: 100,
      actualUnitCost: 95,
    },
  );
  if (partUsage.status !== 201) {
    fail(`part usage must return HTTP 201, got ${partUsage.status}.`);
  }

  const invalidPart = await jsonRequest(
    "POST",
    `/api/maintenance/work-orders/${workOrderId}/parts?facilityId=${FACILITY}`,
    {
      partReference: "GATE-PART-ZERO",
      quantity: 0,
      unit: "adet",
    },
  );
  if (invalidPart.status !== 400) {
    fail(`quantity <= 0 must return HTTP 400, got ${invalidPart.status}.`);
  }

  const missingCompletionNote = await jsonRequest(
    "POST",
    `/api/maintenance/work-orders/${workOrderId}/complete?facilityId=${FACILITY}`,
    { expectedVersion: workOrderVersion, completionNote: "   " },
  );
  if (missingCompletionNote.status !== 400) {
    fail(`empty completion note must return HTTP 400, got ${missingCompletionNote.status}.`);
  }

  const complete = await jsonRequest(
    "POST",
    `/api/maintenance/work-orders/${workOrderId}/complete?facilityId=${FACILITY}`,
    { expectedVersion: workOrderVersion, completionNote: `${gateMarker} completed`, resolutionCode: "repaired" },
  );
  if (complete.status !== 200) {
    fail(`complete must return HTTP 200, got ${complete.status}.`);
  }
  workOrderVersion = complete.body?.version ?? workOrderVersion;

  const terminalStart = await jsonRequest(
    "POST",
    `/api/maintenance/work-orders/${workOrderId}/start?facilityId=${FACILITY}`,
    { expectedVersion: workOrderVersion, note: "Gate invalid restart" },
  );
  if (terminalStart.status !== 400) {
    fail(`terminal work order restart must return HTTP 400, got ${terminalStart.status}.`);
  }

  const timeline = await jsonRequest("GET", `/api/maintenance/work-orders/${workOrderId}/timeline?facilityId=${FACILITY}`);
  if (timeline.status !== 200 || !Array.isArray(timeline.body) || timeline.body.length === 0) {
    fail("timeline must return entries including gate lifecycle.");
  }

  const costSummary = await jsonRequest(
    "GET",
    `/api/maintenance/work-orders/${workOrderId}/cost-summary?facilityId=${FACILITY}`,
  );
  if (costSummary.status !== 200 || typeof costSummary.body?.totalMaintenanceCost !== "number") {
    fail("cost summary must expose total maintenance cost.");
  }

  const assetHistory = await jsonRequest("GET", `/api/maintenance/assets/IM-120/history?facilityId=${FACILITY}`);
  if (assetHistory.status !== 200 || !Array.isArray(assetHistory.body?.workOrders)) {
    fail("asset history must combine maintenance records.");
  }

  const dateTo = new Date().toISOString();
  const dateFrom = new Date(Date.now() - 365 * 24 * 60 * 60 * 1000).toISOString();
  const overview = await jsonRequest(
    "GET",
    `/api/maintenance/reporting/overview?facilityId=${FACILITY}&dateFrom=${encodeURIComponent(dateFrom)}&dateTo=${encodeURIComponent(dateTo)}`,
  );
  if (overview.status !== 200 || typeof overview.body?.openWorkOrders !== "number") {
    fail("reliability overview must return open work order count.");
  }

  const reliability = await jsonRequest(
    "GET",
    `/api/maintenance/reporting/reliability?facilityId=${FACILITY}&dateFrom=${encodeURIComponent(dateFrom)}&dateTo=${encodeURIComponent(dateTo)}`,
  );
  if (reliability.status !== 200) {
    fail(`reliability endpoint must return HTTP 200, got ${reliability.status}.`);
  }
  if (reliability.body?.mtbf?.mtbfAvailable !== false) {
    fail("MTBF must remain unavailable without trusted runtime source.");
  }
  const mttr = reliability.body?.mttr;
  if (!mttr || mttr.eligibleRepairCount < 1) {
    fail("MTTR must include eligible completed corrective repairs.");
  }
  const computedMean = Math.round((mttr.totalRepairMinutes / mttr.eligibleRepairCount) * 100) / 100;
  if (mttr.meanRepairMinutes !== computedMean) {
    fail(`MTTR mean must match underlying rows (${computedMean}).`);
  }

  const repeat30 = await jsonRequest(
    "GET",
    `/api/maintenance/reporting/reliability?facilityId=${FACILITY}&dateFrom=${encodeURIComponent(new Date(Date.now() - 30 * 86400000).toISOString())}&dateTo=${encodeURIComponent(dateTo)}`,
  );
  const repeat90 = await jsonRequest(
    "GET",
    `/api/maintenance/reporting/reliability?facilityId=${FACILITY}&dateFrom=${encodeURIComponent(new Date(Date.now() - 90 * 86400000).toISOString())}&dateTo=${encodeURIComponent(dateTo)}`,
  );
  if (repeat30.status !== 200 || repeat90.status !== 200) {
    fail("repeat failure windows must return HTTP 200.");
  }
  const hydraulicRepeat = (repeat90.body?.repeatFailures ?? []).find(
    (item) => item.assetReference === "IM-120" && item.failureCategory === "hydraulic",
  );
  if (!hydraulicRepeat) {
    fail("expected deterministic IM-120 hydraulic repeat failure.");
  }

  const dueRead = await jsonRequest("GET", `/api/maintenance/preventive-dues/${PILOT_OVERDUE_DUE}?facilityId=${FACILITY}`);
  if (dueRead.status !== 200) {
    fail(`preventive due read must return HTTP 200, got ${dueRead.status}.`);
  }

  if (dueRead.body?.status === "due") {
    const dueVersion = dueRead.body.version;
    const firstDueWo = await jsonRequest(
      "POST",
      `/api/maintenance/preventive-dues/${PILOT_OVERDUE_DUE}/create-work-order?facilityId=${FACILITY}`,
      { expectedVersion: dueVersion },
    );
    if (firstDueWo.status !== 201) {
      fail(`due -> work order must succeed, got ${firstDueWo.status}.`);
    }

    const staleDueWo = await jsonRequest(
      "POST",
      `/api/maintenance/preventive-dues/${PILOT_OVERDUE_DUE}/create-work-order?facilityId=${FACILITY}`,
      { expectedVersion: dueVersion },
    );
    if (staleDueWo.status !== 409) {
      fail(`stale due conversion must return HTTP 409, got ${staleDueWo.status}.`);
    }

    const dueAfter = await jsonRequest("GET", `/api/maintenance/preventive-dues/${PILOT_OVERDUE_DUE}?facilityId=${FACILITY}`);
    const idempotentDueWo = await jsonRequest(
      "POST",
      `/api/maintenance/preventive-dues/${PILOT_OVERDUE_DUE}/create-work-order?facilityId=${FACILITY}`,
      { expectedVersion: dueAfter.body.version },
    );
    if (idempotentDueWo.status !== 201 || idempotentDueWo.body?.id !== firstDueWo.body?.id) {
      fail("duplicate due conversion must return the same work order idempotently.");
    }
  } else if (dueRead.body?.status === "work-order-created" && dueRead.body?.workOrderId) {
    const idempotentDueWo = await jsonRequest(
      "POST",
      `/api/maintenance/preventive-dues/${PILOT_OVERDUE_DUE}/create-work-order?facilityId=${FACILITY}`,
      { expectedVersion: dueRead.body.version },
    );
    if (idempotentDueWo.status !== 201 || idempotentDueWo.body?.id !== dueRead.body.workOrderId) {
      fail("existing due work order must be returned idempotently.");
    }
    const staleIdempotentDueWo = await jsonRequest(
      "POST",
      `/api/maintenance/preventive-dues/${PILOT_OVERDUE_DUE}/create-work-order?facilityId=${FACILITY}`,
      { expectedVersion: Math.max(0, dueRead.body.version - 1) },
    );
    if (staleIdempotentDueWo.status !== 201 || staleIdempotentDueWo.body?.id !== dueRead.body.workOrderId) {
      fail("converted due must remain idempotent without creating duplicate work orders.");
    }
  }

  const restartRequest = await jsonRequest("POST", `/api/maintenance/requests?facilityId=${FACILITY}`, {
    assetType: "machine",
    assetReference: "GATE-RESTART-MACHINE",
    assetName: "Gate Restart Machine",
    type: "fault",
    priority: "low",
    title: `${gateMarker} restart probe`,
    description: "Restart persistence probe",
    source: "manual",
  });
  if (restartRequest.status !== 201) {
    fail(`restart probe create must return HTTP 201, got ${restartRequest.status}.`);
  }
  const restartRequestId = restartRequest.body.id;

  stopPid(canonicalPidBefore, "canonical API on 5000");
  const restarted = spawn(
    process.platform === "win32" ? "dotnet.exe" : "dotnet",
    ["run", "--project", "apps/api/NevaxisZentra.Api.csproj", "--no-launch-profile"],
    {
      cwd: projectRoot,
      detached: true,
      stdio: "ignore",
      env: { ...process.env, ASPNETCORE_URLS: "http://localhost:5000" },
    },
  );
  restarted.unref();

  if (!(await waitForHealth())) {
    fail("API restart persistence probe failed — API did not recover on canonical port.");
  }

  const afterRestart = await jsonRequest("GET", `/api/maintenance/requests/${restartRequestId}?facilityId=${FACILITY}`);
  if (afterRestart.status !== 200 || afterRestart.body?.title !== `${gateMarker} restart probe`) {
    fail("restart persistence probe must retain created request after API restart.");
  }

  const routes = [
    ["/bakim", "dashboard"],
    ["/bakim/planlama", "planning"],
    ["/bakim/raporlama", "reporting"],
    [`/bakim/emirler/${PILOT_WO}`, "work-order detail"],
    ["/bakim/varliklar/IM-120", "asset page"],
  ];
  for (const [path, label] of routes) {
    const status = await fetch(`${WEB}${path}`).then((response) => response.status);
    if (status !== 200) {
      fail(`${label} route ${path} must return HTTP 200, got ${status}.`);
    }
  }

  const bakimHtml = await fetch(`${WEB}/bakim`).then((response) => response.text());
  forbidText(bakimHtml, "Bakım Emrini Aç", "live /bakim HTML");
  forbidText(bakimHtml, "İnceleBakım", "live /bakim HTML");
} catch (error) {
  fail(error instanceof Error ? error.message : String(error));
}

const canonicalPidAfter = getListeningPid(5000);
const duplicatePidAfter = getListeningPid(5011);
console.log(`Canonical API PID after gate: ${canonicalPidAfter ?? "unknown"}`);
if (duplicatePidAfter) {
  console.warn(`Duplicate API instance still listening on 5011 (PID ${duplicatePidAfter}).`);
}

console.log("Maintenance integration gate verification passed.");
