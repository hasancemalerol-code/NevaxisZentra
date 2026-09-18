import { mapOperationalStatus, type OperationalStatusVariant } from "@/components/workspace/operational-status";
import type { MixedProductionLineSummaryApi } from "@/features/mixed-production/server/mixed-production-api-client";
import type { ProductionResourceRowApi } from "@/features/production-resources/server/production-resources-api-client";

import type { FactoryHierarchyNode } from "../domain/factory-workspace-types";

function resourceStateTone(resource: ProductionResourceRowApi): OperationalStatusVariant {
  if (resource.isBlocked) return "blocked";
  if (resource.isStarved) return "starved";
  if (resource.state === "FAULT" || resource.state === "FAULTED") return "fault";
  if (resource.state === "RUNNING" || resource.state === "OPERATING") return "running";
  if (resource.state === "WAITING") return "pending";
  return mapOperationalStatus(resource.state);
}

function lineStateTone(lineState: string): OperationalStatusVariant {
  return mapOperationalStatus(lineState);
}

function stationStateFromResources(resources: readonly ProductionResourceRowApi[]): string {
  const fault = resources.find((resource) => resource.state === "FAULT" || resource.state === "FAULTED");
  if (fault) return fault.state;
  const running = resources.find((resource) => resource.state === "RUNNING" || resource.state === "OPERATING");
  if (running) return running.state;
  return resources[0]?.state ?? "IDLE";
}

export function buildFactoryHierarchy(
  facilityLabel: string,
  lines: readonly MixedProductionLineSummaryApi[],
  resources: readonly ProductionResourceRowApi[],
): FactoryHierarchyNode {
  const areaMap = new Map<string, ProductionResourceRowApi[]>();

  for (const resource of resources) {
    const areaCode = resource.areaCode?.trim() || "GENEL";
    const bucket = areaMap.get(areaCode) ?? [];
    bucket.push(resource);
    areaMap.set(areaCode, bucket);
  }

  for (const line of lines) {
    const hasArea = resources.some((resource) => resource.lineId === line.lineId);
    if (!hasArea) {
      const bucket = areaMap.get("GENEL") ?? [];
      areaMap.set("GENEL", bucket);
    }
  }

  const areaNodes: FactoryHierarchyNode[] = [...areaMap.entries()]
    .sort(([left], [right]) => left.localeCompare(right, "tr"))
    .map(([areaCode, areaResources]) => {
      const lineMap = new Map<string, ProductionResourceRowApi[]>();
      for (const resource of areaResources) {
        const bucket = lineMap.get(resource.lineId) ?? [];
        bucket.push(resource);
        lineMap.set(resource.lineId, bucket);
      }

      const lineNodes: FactoryHierarchyNode[] = lines
        .filter((line) => lineMap.has(line.lineId))
        .map((line) => {
          const lineResources = lineMap.get(line.lineId) ?? [];
          const stationMap = new Map<string, ProductionResourceRowApi[]>();

          for (const resource of lineResources) {
            const bucket = stationMap.get(resource.stationId) ?? [];
            bucket.push(resource);
            stationMap.set(resource.stationId, bucket);
          }

          const stationNodes: FactoryHierarchyNode[] = [...stationMap.entries()]
            .sort(([leftId], [rightId]) => {
              const left = lineResources.find((resource) => resource.stationId === leftId);
              const right = lineResources.find((resource) => resource.stationId === rightId);
              return (left?.stationCode ?? "").localeCompare(right?.stationCode ?? "", "tr");
            })
            .map(([stationId, stationResources]) => {
              const station = stationResources[0];
              const resourceNodes: FactoryHierarchyNode[] = stationResources
                .sort((left, right) => left.code.localeCompare(right.code, "tr"))
                .map((resource) => ({
                  id: `resource:${resource.resourceId}`,
                  kind: "resource",
                  label: resource.name,
                  code: resource.code,
                  state: resource.state,
                  stateTone: resourceStateTone(resource),
                  areaCode,
                  lineId: line.lineId,
                  stationId,
                  resourceId: resource.resourceId,
                }));

              return {
                id: `station:${stationId}`,
                kind: "station",
                label: station.stationName,
                code: station.stationCode,
                state: stationStateFromResources(stationResources),
                stateTone: resourceStateTone(stationResources[0]),
                areaCode,
                lineId: line.lineId,
                stationId,
                children: resourceNodes,
              };
            });

          return {
            id: `line:${line.lineId}`,
            kind: "line",
            label: line.name,
            code: line.code,
            state: line.lineState,
            stateTone: lineStateTone(line.lineState),
            areaCode,
            lineId: line.lineId,
            children: stationNodes,
          };
        });

      return {
        id: `area:${areaCode}`,
        kind: "area",
        label: areaCode === "GENEL" ? "Genel Alan" : areaCode,
        code: areaCode,
        areaCode,
        children: lineNodes,
      };
    });

  return {
    id: "factory:root",
    kind: "factory",
    label: facilityLabel,
    code: facilityLabel,
    children: areaNodes,
  };
}

export function findHierarchyNode(
  root: FactoryHierarchyNode,
  selection: Readonly<{ areaCode?: string; lineId?: string; stationId?: string; resourceId?: string }>,
): FactoryHierarchyNode | null {
  if (selection.resourceId) {
    return walkHierarchy(root, (node) => node.resourceId === selection.resourceId);
  }
  if (selection.stationId) {
    return walkHierarchy(root, (node) => node.stationId === selection.stationId);
  }
  if (selection.lineId) {
    return walkHierarchy(root, (node) => node.lineId === selection.lineId && node.kind === "line");
  }
  if (selection.areaCode) {
    return walkHierarchy(root, (node) => node.areaCode === selection.areaCode && node.kind === "area");
  }
  return root;
}

function walkHierarchy(
  node: FactoryHierarchyNode,
  predicate: (node: FactoryHierarchyNode) => boolean,
): FactoryHierarchyNode | null {
  if (predicate(node)) {
    return node;
  }

  for (const child of node.children ?? []) {
    const match = walkHierarchy(child, predicate);
    if (match) {
      return match;
    }
  }

  return null;
}

export function filterHierarchyBySearch(
  root: FactoryHierarchyNode,
  search: string,
): FactoryHierarchyNode {
  const query = search.trim().toLowerCase();
  if (!query) {
    return root;
  }

  function filterNode(node: FactoryHierarchyNode): FactoryHierarchyNode | null {
    const selfMatch =
      node.label.toLowerCase().includes(query)
      || node.code.toLowerCase().includes(query)
      || node.state?.toLowerCase().includes(query);

    const children = (node.children ?? [])
      .map((child) => filterNode(child))
      .filter((child): child is FactoryHierarchyNode => child !== null);

    if (selfMatch || children.length > 0) {
      return { ...node, children };
    }

    return null;
  }

  const filtered = filterNode(root);
  return filtered ?? { ...root, children: [] };
}
