"use client";

import { useMemo, useState } from "react";
import { ChevronDown, ChevronRight } from "lucide-react";

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

const SMALL_TREE_NODE_LIMIT = 14;

function countNodes(node: FactoryHierarchyNode): number {
  return 1 + (node.children ?? []).reduce((total, child) => total + countNodes(child), 0);
}

function containsActive(node: FactoryHierarchyNode, selection: FactoryWorkspaceSelection): boolean {
  return (node.children ?? []).some((child) => isNodeActive(child, selection) || containsActive(child, selection));
}

type BranchProps = Readonly<{
  node: FactoryHierarchyNode;
  selection: FactoryWorkspaceSelection;
  onSelect: (next: FactoryWorkspaceSelection) => void;
  depth?: number;
  expandAll: boolean;
  overrides: ReadonlyMap<string, boolean>;
  onToggle: (id: string, next: boolean) => void;
}>;

function HierarchyBranch({ node, selection, onSelect, depth = 0, expandAll, overrides, onToggle }: BranchProps) {
  const active = isNodeActive(node, selection);
  const hasChildren = Boolean(node.children && node.children.length > 0);
  // Large trees open to the area level and along the selected path; every node can be opened or closed.
  const expanded = overrides.get(node.id) ?? (expandAll || depth < 2 || containsActive(node, selection) || active);
  const treeTitle = node.label && node.label !== node.code ? `${node.code} ${node.label}` : node.code;

  return (
    <li className="factory-hierarchy-tree__item">
      <div className="factory-hierarchy-tree__row">
      {hasChildren ? (
        <button
          type="button"
          className="factory-hierarchy-tree__toggle"
          aria-expanded={expanded}
          aria-label={`${node.code} alt öğelerini ${expanded ? "gizle" : "göster"}`}
          onClick={() => onToggle(node.id, !expanded)}
        >
          {expanded ? <ChevronDown aria-hidden="true" size={14} /> : <ChevronRight aria-hidden="true" size={14} />}
        </button>
      ) : (
        <span className="factory-hierarchy-tree__toggle factory-hierarchy-tree__toggle--leaf" aria-hidden="true" />
      )}
      <button
        type="button"
        className="factory-hierarchy-tree__button"
        data-active={active}
        data-testid={`factory-hierarchy-${node.kind}-${node.code}`}
        onClick={() => onSelect(selectionForNode(node))}
        style={{ paddingLeft: `${8 + depth * 10}px` }}
      >
        <span className="factory-hierarchy-tree__kind">{KIND_LABELS[node.kind]}</span>
        <span className="factory-hierarchy-tree__name" title={treeTitle}>
          <strong>{node.code}</strong>
          {node.label && node.label !== node.code ? <span> {node.label}</span> : null}
        </span>
        {node.state ? (
          <OperationalBadge label={node.state} status={node.stateTone ?? "idle"} />
        ) : <span aria-hidden="true" />}
      </button>
      </div>
      {hasChildren && expanded ? (
        <ul className="factory-hierarchy-tree">
          {node.children!.map((child) => (
            <HierarchyBranch
              key={child.id}
              depth={depth + 1}
              node={child}
              onSelect={onSelect}
              selection={selection}
              expandAll={expandAll}
              overrides={overrides}
              onToggle={onToggle}
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

  const [overrides, setOverrides] = useState<ReadonlyMap<string, boolean>>(() => new Map());
  const expandAll = Boolean(selection.search) || countNodes(filteredRoot) <= SMALL_TREE_NODE_LIMIT;
  const toggle = (id: string, next: boolean) => setOverrides((current) => new Map(current).set(id, next));

  return (
    <ul className="factory-hierarchy-tree" data-testid="factory-hierarchy-tree">
      <HierarchyBranch
        node={filteredRoot}
        onSelect={onSelect}
        selection={selection}
        expandAll={expandAll}
        overrides={overrides}
        onToggle={toggle}
      />
    </ul>
  );
}
