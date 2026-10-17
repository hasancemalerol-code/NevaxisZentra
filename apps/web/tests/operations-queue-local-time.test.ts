import assert from "node:assert/strict";
import { readFileSync } from "node:fs";
import path from "node:path";
import test from "node:test";

import { formatDateTime as formatDetailDateTime } from "../src/lib/presentation/format";
import { formatDate, formatDateTime as formatCommercialDateTime } from "../src/features/commercial-intelligence/domain/commercial-intelligence-formatters";

// P2-B: the operations command centre queue and its linked detail pages must render the same instant identically,
// in facility local time (Europe/Istanbul, +03:00), regardless of the server process timezone.
const INSTANT = "2026-10-15T09:46:00Z";

function withProcessTimeZone<T>(zone: string, run: () => T): T {
  const previous = process.env.TZ;
  process.env.TZ = zone;
  try {
    return run();
  } finally {
    if (previous === undefined) delete process.env.TZ;
    else process.env.TZ = previous;
  }
}

test("queue (list) and detail formatters render the same instant as Türkiye local time", () => {
  for (const zone of ["UTC", "America/New_York", "Europe/Istanbul"]) {
    withProcessTimeZone(zone, () => {
      const detail = formatDetailDateTime(INSTANT);
      const commercial = formatCommercialDateTime(INSTANT);
      assert.match(detail, /12:46/, `detail @${zone}`);
      assert.doesNotMatch(detail, /09:46/, `detail @${zone}`);
      assert.equal(commercial, detail, `commercial detail vs list @${zone}`);
      assert.match(formatDate("2026-10-15T22:30:00Z"), /16/, "date rolls over to the local day");
    });
  }
});

test("operations queue source uses the shared local-time formatter, not a UTC one", () => {
  const source = readFileSync(
    path.resolve(__dirname, "..", "src/features/operations-command-center/server/operations-command-center-queries.ts"),
    "utf8",
  );
  assert.match(source, /import \{ formatDateTime \} from "@\/lib\/presentation\/format"/);
  assert.doesNotMatch(source, /formatDateTime,?\s*\}\s*from "\.\.\/\.\.\/commercial-intelligence/);
  const commercial = readFileSync(
    path.resolve(__dirname, "..", "src/features/commercial-intelligence/domain/commercial-intelligence-formatters.ts"),
    "utf8",
  );
  assert.doesNotMatch(commercial, /timeZone: "UTC"/);
});

import { formatNetworkDateTime } from "../src/features/factory-network/domain/factory-network-labels";
import { formatMaterialDateOnly, formatMaterialDateTime } from "../src/features/materials/presentation/material-labels";

test("remaining date-time renderers are pinned to the facility time zone regardless of process TZ", () => {
  const outputs = ["UTC", "America/New_York", "Europe/Istanbul"].map((zone) =>
    withProcessTimeZone(zone, () => [
      formatNetworkDateTime(INSTANT),
      formatMaterialDateTime(INSTANT),
      formatMaterialDateOnly("2026-10-15T22:30:00Z"),
    ]),
  );
  assert.deepEqual(outputs[1], outputs[0]);
  assert.deepEqual(outputs[2], outputs[0]);
  assert.match(outputs[0][0], /12[.:]46/);
  assert.match(outputs[0][1], /12[.:]46/);
  assert.match(outputs[0][2], /16/);
});

test("every list/detail renderer names the facility time zone", () => {
  const files = [
    "components/workspace/event-timeline.tsx",
    "features/works/components/works-workspace-view.tsx",
    "features/factory-network/domain/factory-network-labels.ts",
    "app/(app)/bakim/emirler/[id]/page.tsx",
    "app/(app)/bakim/arizalar/[id]/page.tsx",
    "features/materials/presentation/material-labels.ts",
    "features/apps-catalog/server/apps-operational-analytics-builder.ts",
    "features/executive-command-center/application/executive-command-center-builder.ts",
  ];
  for (const file of files) {
    const text = readFileSync(path.resolve(__dirname, "..", "src", file), "utf8");
    assert.match(text, /timeZone: FACILITY_TIME_ZONE/, file);
  }
});
