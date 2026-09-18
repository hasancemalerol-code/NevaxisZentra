import assert from "node:assert/strict";
import test from "node:test";
import type { ReactElement } from "react";
import { renderToStaticMarkup } from "react-dom/server";

import { buildFactoryHierarchy, filterHierarchyBySearch, findHierarchyNode } from "../src/features/factory-workspace/application/build-factory-hierarchy";
import {
  buildFactoryWorkspaceHref,
  readFactoryWorkspaceSelection,
  resolveActiveSelectionKind,
} from "../src/features/factory-workspace/application/factory-workspace-selection";
import { FactoryHierarchyTree } from "../src/features/factory-workspace/components/factory-hierarchy-tree";
import { FactoryMachineIntelligence } from "../src/features/factory-workspace/components/factory-machine-intelligence";
import type { FactoryHierarchyNode } from "../src/features/factory-workspace/domain/factory-workspace-types";

const lines = [
  {
    lineId: "line-1",
    code: "MIX-01",
    name: "Mixed Line 01",
    currentOrderReference: "PO-24001",
    currentProductCode: "SKU-1",
    currentShiftCode: "A",
    lineState: "RUNNING",
    goodOutputToday: 120,
    rejectOutputToday: 2,
  },
];

const resources = [
  {
    resourceId: "r1",
    code: "INJ-001",
    name: "Injection 01",
    resourceType: "machine",
    machineRegistryCode: "MCH-INJ-001",
    stationId: "s1",
    stationCode: "INJ-ST-01",
    stationName: "Injection Station 01",
    stationKind: "process",
    lineId: "line-1",
    lineCode: "MIX-01",
    lineName: "Mixed Line 01",
    areaCode: "AREA-01",
    state: "RUNNING",
    operationCode: "INJ-CYCLE",
    operatorCode: "OP-01",
    orderReference: "PO-24001",
    goodCount: 80,
    rejectCount: 1,
    bufferQuantity: 4,
    bufferCapacity: 20,
    isStarved: false,
    isBlocked: false,
  },
  {
    resourceId: "r2",
    code: "ASM-01",
    name: "Assembly 01",
    resourceType: "human",
    stationId: "s2",
    stationCode: "ASM-01",
    stationName: "Assembly 01",
    stationKind: "assembly",
    lineId: "line-1",
    lineCode: "MIX-01",
    lineName: "Mixed Line 01",
    areaCode: "AREA-01",
    state: "WAITING",
    operationCode: "ASM",
    operatorCode: "OP-02",
    orderReference: "PO-24001",
    goodCount: 40,
    rejectCount: 1,
    isStarved: true,
    isBlocked: false,
  },
];

test("factory hierarchy builds area line station resource tree", () => {
  const hierarchy = buildFactoryHierarchy("FAC-01", lines, resources);
  assert.equal(hierarchy.kind, "factory");
  assert.equal(hierarchy.children?.[0]?.kind, "area");
  assert.equal(hierarchy.children?.[0]?.children?.[0]?.kind, "line");
  assert.equal(hierarchy.children?.[0]?.children?.[0]?.children?.[0]?.kind, "station");
  assert.equal(
    hierarchy.children?.[0]?.children?.[0]?.children?.[0]?.children?.[0]?.kind,
    "resource",
  );
});

test("factory workspace selection reads and builds contextual href", () => {
  const selection = readFactoryWorkspaceSelection({
    alan: "AREA-01",
    hat: "line-1",
    istasyon: "s1",
    kaynak: "r1",
    q: "inj",
  });

  assert.equal(selection.areaCode, "AREA-01");
  assert.equal(selection.lineId, "line-1");
  assert.equal(buildFactoryWorkspaceHref("/fabrika", selection), "/fabrika?alan=AREA-01&hat=line-1&istasyon=s1&kaynak=r1&q=inj");
  assert.equal(resolveActiveSelectionKind(selection), "resource");
});

test("factory hierarchy search filters by machine or station code", () => {
  const hierarchy = buildFactoryHierarchy("FAC-01", lines, resources);
  const filtered = filterHierarchyBySearch(hierarchy, "inj");
  const resourceNode = findHierarchyNode(filtered, { resourceId: "r1" });
  assert.ok(resourceNode);
});

test("factory hierarchy tree renders selectable nodes", () => {
  const hierarchy = buildFactoryHierarchy("FAC-01", lines, resources);
  const markup = renderToStaticMarkup(
    <FactoryHierarchyTree
      root={hierarchy}
      selection={{ lineId: "line-1" }}
      onSelect={() => undefined}
    /> as ReactElement,
  );

  assert.match(markup, /data-testid="factory-hierarchy-tree"/);
  assert.match(markup, /MIX-01/);
  assert.match(markup, /INJ-001/);
});

test("factory machine intelligence uses evidence-backed decision pattern", () => {
  const markup = renderToStaticMarkup(
    <FactoryMachineIntelligence
      machineCode="MCH-INJ-001"
      detail={{
        machineId: "m1",
        latestTelemetry: [],
        cycleProjection: {
          machineId: "m1",
          observedCycleCount: 10,
          latestObservedCounter: 10,
          lastCycleAtUtc: "2026-01-01T00:00:00Z",
          lastCycleDurationMs: 5400,
          averageRecentCycleDurationMs: 5000,
          updatedAtUtc: "2026-01-01T00:00:00Z",
        },
        cycleHistory: [],
        signalProjection: null,
        stateTransitions: [],
        downtimeEpisodes: [],
        activeAlarms: [],
        alarmHistory: [],
        setupEpisodes: [],
        scrapObservations: [],
        operationalSummary: null,
        recentEvents: [],
      }}
      risk={{
        riskId: "risk-1",
        riskType: "cycle",
        riskState: "warning",
        machineCode: "MCH-INJ-001",
        observedSummary: "Çevrim süresi yükseldi",
        predictedSummary: "Throughput kaybı",
        timeToImpactLabel: "45 dk",
        confidenceBand: "yüksek",
        horizon: "kısa",
        evidence: ["Soğutma sapması"],
        recommendedActions: ["Profil kontrolü"],
        dataQuality: "good",
        generatedAtUtc: "2026-01-01T00:00:00Z",
      }}
    /> as ReactElement,
  );

  assert.match(markup, /Ne oldu\?/);
  assert.match(markup, /Neden\?/);
  assert.match(markup, /Etki/);
  assert.match(markup, /Öneri/);
});
