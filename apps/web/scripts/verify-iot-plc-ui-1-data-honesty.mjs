#!/usr/bin/env node
import { readFileSync } from "node:fs";
import { dirname, resolve } from "node:path";
import { fileURLToPath } from "node:url";

const ROOT = resolve(dirname(fileURLToPath(import.meta.url)), "../../..");

function fail(message) {
  console.error(`IOT-PLC-UI-1 data-honesty verification failed: ${message}`);
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

console.log("Running IOT-PLC-UI-1 data-honesty verifier.");

const queries = readRequired("apps/web/src/features/iot-plc/server/iot-plc-queries.ts");
const formatters = readRequired("apps/web/src/features/iot-plc/domain/iot-plc-formatters.ts");
const view = readRequired("apps/web/src/features/iot-plc/components/iot-plc-workspace-view.tsx");
const presentation = readRequired("apps/web/src/features/iot-plc/components/iot-plc-presentation.ts");

assert("classifyEndpointHealth present", queries.includes("function classifyEndpointHealth("));
assert(
  "classifyEndpointHealth stale threshold returns unknown",
  /ageSeconds > staleThresholdSeconds[\s\S]*return "unknown"/.test(queries),
);
assert(
  "classifyEndpointHealth missing evidence returns unknown",
  /!endpoint\.lastObservedConnectionState \|\| !endpoint\.lastObservedAtUtc[\s\S]*return "unknown"/.test(queries),
);

assert("unavailable KPI uses em dash", queries.includes('=== null ? "—"'));
assert("formatIotPlcCount unavailable uses em dash", formatters.includes('return "—"'));
assert("formatIotPlcDateTime unavailable uses em dash", formatters.includes('if (!utc) return "—"'));
assert(
  "no fabricated zero when unavailable in KPI builder",
  queries.includes('onlineCount === null ? "—"')
    && queries.includes('degradedOfflineCount === null ? "—"')
    && queries.includes('activeSignals === null ? "—"')
    && queries.includes('freshSignals === null ? "—"')
    && queries.includes('attentionAssessments === null ? "—"'),
);

assert("formatIotPlcRatioPercent uses Intl percent", formatters.includes('style: "percent"'));
assert("formatIotPlcRatioPercent documents 0-1 fractions", formatters.includes("0–1 fractions"));
assert("formatIotPlcRatioPercent does not multiply by 100", !formatters.includes("* 100") && !formatters.includes("*100"));
assert(
  "view does not double-scale ratio percent",
  !view.includes("formatIotPlcRatioPercent(ratio * 100)")
    && !view.includes("formatIotPlcRatioPercent(slice.ratio * 100)"),
);

assert(
  "online count excludes unknown health buckets",
  queries.includes('endpoint.healthBucket === "online"'),
);
assert(
  "online count not inferred from raw connection state",
  !queries.includes('lastObservedConnectionState === "online"'),
);
assert(
  "degraded/offline KPI uses health bucket not raw state",
  queries.includes('endpoint.healthBucket === "degraded" || endpoint.healthBucket === "offline"'),
);

assert("quality distribution empty when no observations", queries.includes("actualObservationCount <= 0"));
assert(
  "quality distribution uses reconciliation helper",
  queries.includes("reconcileIotPlcQualityDistribution"),
);
assert(
  "quality counts derived from strict 0-1 fractions",
  presentation.includes("validateIotPlcSignalAssessmentRatioContract")
    && presentation.includes("isIotPlcRatioFraction"),
);
assert(
  "no dual-scale ratio normalization",
  !presentation.includes("value / 100") && !presentation.includes("normalizeIotPlcRatioFraction"),
);
assert(
  "contract-compliant assessment selection",
  queries.includes("selectSignalAssessmentForQualityDistribution"),
);
assert(
  "view uses centralized quality slice formatter",
  view.includes("formatIotPlcQualitySliceLine"),
);
assert(
  "no raw ratio-times-denominator count bug",
  !queries.includes("Math.round(ratio * denominator)"),
);
assert(
  "quality bar width uses count ratio not pre-scaled percent",
  presentation.includes("(slice.count / denominator) * 100"),
);
assert("full-ring unknown health donut", presentation.includes("active.length === 1 && active[0].count === total"));
forbidText(view, "200 · %10.000", "workspace view");
forbidText(view, "%10.000", "workspace view");

forbidText(view, "bb000001", "workspace view");
assert(
  "raw entity ids not rendered in workspace cells",
  !/<td>\{[^}]*\.id\}<\/td>/.test(view) && !/<dd>\{[^}]*\.id\}<\/dd>/.test(view),
);
assert(
  "assessment ids used internally for keys and loaders",
  view.includes("assessment.id") && view.includes("assessmentKey"),
);

console.log("IOT-PLC-UI-1 data-honesty verification passed.");
