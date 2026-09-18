"use client";

import { useMemo } from "react";

import { OperationalBadge } from "@/components/workspace/operational-status";
import { filterHierarchyBySearch } from "../application/build-factory-hierarchy";
import type { FactoryHierarchyNode, FactoryWorkspaceSelection } from "../domain/factory-workspace-types";

type FactoryHierarchyTreeProps = Readonly<{
  root: FactoryHierarchyNode;
  selection: FactoryWorkspaceSelection;
  onSelect: (next: FactoryWorkspaceSelection) => void;
}>;

const KIND_LABELS: Record<FactoryHierarchyNode["kind"], string> = {
  factory: "Fabrika",
  area: "Alan",
  line: "Hat",
  station: "İstasyon",
  resource: "Kaynak",
};

function isNodeActive(node: FactoryHierarchyNode, selection: FactoryWorkspaceSelection): boolean {
  if (selection.resourceId) return node.resourceId === selection.resourceId;
  if (selection.stationId) return node.stationId === selection.stationId && node.kind === "station";
  if (selection.lineId) return node.lineId === selection.lineId && node.kind === "line";
  if (selection.areaCode) return node.areaCode === selection.areaCode && node.kind === "area";
  return node.kind === "factory";
}

function selectionForNode(node: FactoryHierarchyNode): FactoryWorkspaceSelection {
  if (node.kind === "factory") {
    return {};
  }

  return {
    areaCode: node.areaCode,
    lineId: node.lineId,
    stationId: node.stationId,
    resourceId: node.resourceId,
  };
}

function HierarchyBranch({
  node,
  selection,
  onSelect,
  depth = 0,
}: Readonly<{
  node: FactoryHierarchyNode;
  selection: FactoryWorkspaceSelection;
  onSelect: (next: FactoryWorkspaceSelection) => void;
  depth?: number;
}>) {
  const active = isNodeActive(node, selection);

  return (
    <li className="factory-hierarchy-tree__item">
      <button
        type="button"
        className="factory-hierarchy-tree__button"
        data-active={active}
        data-testid={`factory-hierarchy-${node.kind}-${node.code}`}
        onClick={() => onSelect(selectionForNode(node))}
        style={{ paddingLeft: `${8 + depth * 10}px` }}
      >
        <span className="factory-hierarchy-tree__kind">{KIND_LABELS[node.kind]}</span>
        <span>
          <strong>{node.code}</strong>
          <span>{node.label}</span>
        </span>
        {node.state ? (
          <OperationalBadge label={node.state} status={node.stateTone ?? "idle"} />
        ) : null}
      </button>
      {node.children && node.children.length > 0 ? (
        <ul className="factory-hierarchy-tree">
          {node.children.map((child) => (
            <HierarchyBranch
              key={child.id}
              depth={depth + 1}
              node={child}
              onSelect={onSelect}
              selection={selection}
            />
          ))}
        </ul>
      ) : null}
    </li>
  );
}

export function FactoryHierarchyTree({ root, selection, onSelect }: FactoryHierarchyTreeProps) {
  const filteredRoot = useMemo(
    () => (selection.search ? filterHierarchyBySearch(root, selection.search) : root),
    [root, selection.search],
  );

  return (
    <ul className="factory-hierarchy-tree" data-testid="factory-hierarchy-tree">
      <HierarchyBranch node={filteredRoot} onSelect={onSelect} selection={selection} />
    </ul>
  );
}
