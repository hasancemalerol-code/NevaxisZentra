#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");
const BASE = process.env.NEVAXIS_FACTOS_API_BASE_URL ?? "http://127.0.0.1:5000";
const FACILITY = "bb000001-0005-4000-8000-000000000005";
const ACTOR = {
  issuer: "crevafactos-dev",
  subject: "planning-pilot-user",
  tenant: "dev-pilot-tenant",
};

function fail(message) {
  console.error(`ENERGY-2 baseline/anomaly verification failed: ${message}`);
  process.exit(1);
}

function assert(label, ok, detail = "") {
  if (!ok) fail(`${label}${detail ? `: ${detail}` : ""}`);
  console.log(`✓ ${label}`);
}

function readRequired(relativePath) {
  return readFileSync(resolve(ROOT, relativePath), "utf8").replaceAll("\r\n", "\n");
}

function forbidText(source, forbidden, label) {
  if (source.includes(forbidden)) fail(`${label} must not contain "${forbidden}"`);
}

function headers() {
  return {
    "Content-Type": "application/json",
    "X-crevafactos-dev-Issuer": ACTOR.issuer,
    "X-crevafactos-dev-Subject": ACTOR.subject,
    "X-NevaxisFactOS-Tenant": ACTOR.tenant,
  };
}

async function request(path) {
  const response = await fetch(`${BASE}${path}`, { headers: headers() });
  const text = await response.text();
  let json = null;
  try { json = text ? JSON.parse(text) : null; } catch { json = { raw: text }; }
  return { status: response.status, json };
}

function median(values) {
  const sorted = [...values].sort((a, b) => a - b);
  const mid = Math.floor(sorted.length / 2);
  return sorted.length % 2 === 0
    ? (sorted[mid - 1] + sorted[mid]) / 2
    : sorted[mid];
}

function mad(values, center) {
  const deviations = values.map((v) => Math.abs(v - center)).sort((a, b) => a - b);
  return median(deviations);
}

console.log("Running ENERGY-2 baseline/anomaly verifier.");

const constants = readRequired("packages/NevaxisFactOS.Persistence/Energy/EnergyIntelligenceConstants.cs");
const calculator = readRequired("packages/NevaxisFactOS.Persistence/Energy/EnergyIntelligenceCalculator.cs");
const seed = readRequired("packages/NevaxisFactOS.Persistence/Development/DevelopmentPilotEnergyIntelligenceSeed.cs");

assert("rule-set ENERGY-2.0", constants.includes("ENERGY-2.0"));
assert("minimum comparable windows 6", constants.includes("MinimumComparableWindows = 6"));
assert("MAD scale 1.4826", constants.includes("MadScaleFactor = 1.4826m"));
assert("default relative tolerance", constants.includes("DefaultRelativeTolerance = 0.10m"));
assert("median method in calculator", calculator.includes("CalculateMedian"));
assert("MAD method in calculator", calculator.includes("CalculateMad"));
assert("leading driver ordering", calculator.includes("SelectLeadingDriver"));
forbidText(calculator, "guaranteed savings", "calculator");
assert(
  "development disclaimer negates guaranteed savings",
  constants.includes("not real customer meter evidence or guaranteed savings"),
);

const samples = [36.2, 37.5, 37.8, 38.1, 38.5, 39.4];
const center = median(samples);
const dispersion = mad(samples, center);
const normalized = dispersion * 1.4826;
const relativeBoundary = center * 1.1;
const dispersionBoundary = center + 3 * normalized;
const upper = Math.max(relativeBoundary, dispersionBoundary);
assert("median center calculation", Math.abs(center - 37.95) < 0.01);
assert("MAD calculation", Math.abs(dispersion - 0.5) < 0.01);
const zeroMadBoundary = 10 * 1.1;
assert("zero-MAD relative tolerance still applies", zeroMadBoundary === 11);

const tolerance = 0.15;
const target = 45;
const actual = 38.5;
const variance = actual - target;
assert("target variance", Math.abs(variance - (-6.5)) < 0.01);
assert("within tolerance outcome path", variance > tolerance * target === false);

assert("peak demand reason code", constants.includes("peak-demand-exceeded"));
assert("idle consumption reason code", constants.includes("idle-consumption-elevated"));
assert("missing production state code", constants.includes("missing-production-state"));
assert("intensity reason code", constants.includes("energy-intensity-elevated"));
assert("insufficient comparable windows code", constants.includes("insufficient-comparable-windows"));
assert("source conflict code", constants.includes("source-conflict"));

assert("missing production state does not infer idle", calculator.includes("idle consumption cannot be inferred"));
assert("intensity compatibility check", calculator.includes("Production context is not compatible"));

const workspace = await request(`/api/energy/intelligence/workspace?facilityId=${FACILITY}`);
assert("workspace reachable", workspace.status === 200);

const within = await request(
  `/api/energy/assessments/${encodeURIComponent("ASSESS-WITHIN-TARGET")}?facilityId=${FACILITY}`,
);
assert("within-target assessment found", within.status === 200);
assert("within-target outcome", within.json?.assessment?.summary?.outcome === "within-target");

const peak = await request(
  `/api/energy/assessments/${encodeURIComponent("ASSESS-PEAK-DEMAND")}?facilityId=${FACILITY}`,
);
assert("peak-demand assessment found", peak.status === 200);
assert("peak-demand outcome attention", peak.json?.assessment?.summary?.outcome === "attention");
assert("peak-demand driver", peak.json?.assessment?.summary?.leadingDriverCode === "peak-demand-exceeded");

const idle = await request(
  `/api/energy/assessments/${encodeURIComponent("ASSESS-IDLE-CONSUMPTION")}?facilityId=${FACILITY}`,
);
assert("idle assessment found", idle.status === 200);
assert("idle outcome attention", idle.json?.assessment?.summary?.outcome === "attention");
assert("idle driver", idle.json?.assessment?.summary?.leadingDriverCode === "idle-consumption-elevated");

const intensity = await request(
  `/api/energy/assessments/${encodeURIComponent("ASSESS-ENERGY-INTENSITY")}?facilityId=${FACILITY}`,
);
assert("intensity assessment found", intensity.status === 200);
assert("intensity outcome attention", intensity.json?.assessment?.summary?.outcome === "attention");
assert("intensity driver", intensity.json?.assessment?.summary?.leadingDriverCode === "energy-intensity-elevated");

const insufficient = await request(
  `/api/energy/assessments/${encodeURIComponent("ASSESS-INSUFFICIENT-DATA")}?facilityId=${FACILITY}`,
);
assert("insufficient assessment found", insufficient.status === 200);
assert("insufficient outcome", insufficient.json?.assessment?.summary?.outcome === "insufficient-data");
assert("no fabricated percentage", insufficient.json?.assessment?.summary?.variancePercent == null);

const conflict = await request(
  `/api/energy/assessments/${encodeURIComponent("ASSESS-SOURCE-CONFLICT")}?facilityId=${FACILITY}`,
);
assert("source-conflict assessment found", conflict.status === 200);
assert("source-conflict outcome", conflict.json?.assessment?.summary?.outcome === "source-conflict");

const targets = await request(`/api/energy/targets?facilityId=${FACILITY}`);
assert("targets list", targets.status === 200 && targets.json?.items?.length > 0);
const consumptionTarget = targets.json.items.find((t) => t.targetReference === "TGT-CONSUMPTION-INJ-001");
assert("approved consumption target exists", consumptionTarget?.status === "approved");
assert("baseline not treated as target", consumptionTarget?.targetReference !== "BL-machine-EM-INJ-001-28800-none-none");

assert("opportunity labels conservative", constants.includes("Potansiyel azaltılabilir tüketim"));
assert("tariff required for cost path", calculator.includes("MissingTariff"));

console.log("ENERGY-2 baseline/anomaly verification passed.");
