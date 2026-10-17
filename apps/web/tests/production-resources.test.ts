import assert from "node:assert/strict";
import test from "node:test";

import {
  buildProductionResourcesFilterParams,
  clearProductionResourcesFilterParams,
  filterProductionResources,
  hasActiveProductionResourcesFilters,
  readProductionResourcesFilters,
} from "../src/features/production-resources/application/production-resources-filter";
import type { ProductionResourceRowApi } from "../src/features/production-resources/server/production-resources-api-client";
import { PRODUCTION_RESOURCES_ROUTE_PATH } from "../src/features/production-resources/domain/production-resources-labels";
import { navigationItems } from "../src/lib/navigation/primary-navigation";

function createResource(overrides: Partial<ProductionResourceRowApi> = {}): ProductionResourceRowApi {
  return {
    resourceId: "resource-1",
    code: "INJ-001",
    name: "Injection 01",
    resourceType: "machine",
    stationId: "station-1",
    stationCode: "ST-10",
    stationName: "Injection",
    stationKind: "process",
    lineId: "line-1",
    lineCode: "MIX-01",
    lineName: "Mix Line 01",
    areaCode: "MIX-AREA-01",
    state: "RUNNING",
    goodCount: 10,
    rejectCount: 1,
    isStarved: false,
    isBlocked: false,
    ...overrides,
  };
}

test("production resources route is registered in navigation", () => {
  const item = navigationItems.find((candidate) => candidate.href === PRODUCTION_RESOURCES_ROUTE_PATH);
  assert.ok(item);
  assert.equal(item?.label, "Makineler ve Kaynaklar");
});

test("production resources filters preserve deep-link query params", () => {
  const params = buildProductionResourcesFilterParams(
    new URLSearchParams("lineId=line-1&search=inj"),
    { resourceType: "machine" },
  );
  assert.equal(params.get("resourceType"), "machine");
  assert.equal(params.get("lineId"), "line-1");
  assert.equal(params.get("search"), "inj");
});

test("production resources clear filters removes filter keys only", () => {
  const cleared = clearProductionResourcesFilterParams(
    new URLSearchParams("lineId=line-1&resourceType=machine&state=running&search=inj"),
  );
  assert.equal(cleared.get("lineId"), null);
  assert.equal(cleared.get("resourceType"), null);
});

test("production resources filter applies line, type, state and search", () => {
  const resources = [
    createResource(),
    createResource({
      resourceId: "resource-2",
      code: "OP-ASM-01",
      resourceType: "human",
      state: "IDLE",
      lineId: "line-2",
    }),
  ];

  const filtered = filterProductionResources(resources, {
    lineId: "line-1",
    resourceType: "machine",
    state: "running",
    search: "inj",
  });

  assert.equal(filtered.length, 1);
  assert.equal(filtered[0]?.code, "INJ-001");
});

test("task center navigation uses operational product label", () => {
  const item = navigationItems.find((candidate) => candidate.href === "/" && candidate.exact);
  assert.ok(item);
  assert.equal(item?.label, "Görev Merkezi");
});

test("production resources active filter detection", () => {
  assert.equal(hasActiveProductionResourcesFilters(readProductionResourcesFilters(new URLSearchParams())), false);
  assert.equal(
    hasActiveProductionResourcesFilters(readProductionResourcesFilters(new URLSearchParams("resourceType=machine"))),
    true,
  );
});
