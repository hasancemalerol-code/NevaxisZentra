"use client";

import { useCallback } from "react";
import { usePathname, useRouter } from "next/navigation";

import { FactoryContextBreadcrumb } from "@/components/workspace/factory-context-breadcrumb";
import { StatusStrip } from "@/components/workspace/status-strip";
import { WorkspaceHeader } from "@/components/workspace/workspace-header";
import { buildFactoryWorkspaceHref } from "../application/factory-workspace-selection";
import type { FactoryWorkspacePayload, FactoryWorkspaceSelection } from "../domain/factory-workspace-types";
import { FactoryHierarchyTree } from "./factory-hierarchy-tree";
import { FactoryOnboardingEmptyState } from "./factory-onboarding-empty-state";
import { FactoryOperationalCenter } from "./factory-operational-center";
import { FactoryResourceInspector } from "./factory-resource-inspector";
import { FactoryCopilotWorkspaceActions } from "@/features/factory-copilot/components/factory-copilot-workspace-actions";
import { FactoryWorkspacePrimaryNav } from "./factory-workspace-primary-nav";

import "../factory-workspace.css";

type FactoryWorkspaceShellProps = Readonly<{
  workspace: FactoryWorkspacePayload;
}>;

function breadcrumbSegments(workspace: FactoryWorkspacePayload) {
  const segments = [];
  if (workspace.selection.areaCode) {
    segments.push({ label: workspace.selection.areaCode });
  }
  if (workspace.lineDetail) {
    segments.push({
      label: workspace.lineDetail.line.name,
      href: buildFactoryWorkspaceHref("/fabrika", {
        ...workspace.selection,
        stationId: undefined,
        resourceId: undefined,
      }),
    });
  }
  if (workspace.selection.stationId && workspace.lineDetail) {
    const station = workspace.lineDetail.stations.find((item) => item.stationId === workspace.selection.stationId);
    if (station) {
      segments.push({ label: station.name });
    }
  }
  if (workspace.selectedResource) {
    segments.push({ label: workspace.selectedResource.code });
  }
  return segments;
}

export function FactoryWorkspaceShell({ workspace }: FactoryWorkspaceShellProps) {
  const router = useRouter();
  const pathname = usePathname();

  const navigate = useCallback(
    (next: FactoryWorkspaceSelection) => {
      const href = buildFactoryWorkspaceHref(pathname, { ...next, search: workspace.selection.search });
      router.replace(href);
    },
    [pathname, router, workspace.selection.search],
  );

  const onSearchChange = useCallback(
    (search: string) => {
      navigate({ ...workspace.selection, search });
    },
    [navigate, workspace.selection],
  );

  return (
    <main className="fact-workspace factory-workspace" data-testid="factory-workspace">
      <WorkspaceHeader
        breadcrumb={<FactoryContextBreadcrumb segments={breadcrumbSegments(workspace)} />}
        context="FABRİKA"
        meta="Fabrika → Alan → Hat → İstasyon → Kaynak"
        title="Fabrikada şu anda ne oluyor?"
        actions={<FactoryCopilotWorkspaceActions />}
      />

      <StatusStrip
        aria-label="Fabrika çalışma alanı özet"
        items={[
          { label: "Çalışan kaynak", value: workspace.resources.status.running },
          { label: "Bekleyen", value: workspace.resources.status.waiting },
          { label: "Arızalı", value: workspace.resources.status.fault },
          { label: "Risk", value: workspace.resources.status.atRisk },
          { label: "Vardiya", value: workspace.shift?.shiftLabel ?? "—" },
        ]}
      />

      <FactoryWorkspacePrimaryNav pathname={pathname} />

      <FactoryOnboardingEmptyState
        resourceCount={workspace.resources.resources.length}
        lineCount={new Set(workspace.resources.resources.map((resource) => resource.lineId)).size}
      />

      <div className="factory-workspace__layout">
        <aside className="factory-workspace__panel" aria-label="Fabrika hiyerarşisi">
          <div className="factory-workspace__panel-header">
            <h2>Hiyerarşi</h2>
          </div>
          <div className="factory-workspace__panel-body">
            <input
              aria-label="Fabrika arama"
              className="factory-hierarchy-search"
              placeholder="Hat, istasyon, kaynak, emir"
              type="search"
              value={workspace.selection.search ?? ""}
              onChange={(event) => onSearchChange(event.target.value)}
            />
            <FactoryHierarchyTree
              root={workspace.hierarchy}
              selection={workspace.selection}
              onSelect={navigate}
            />
          </div>
        </aside>

        <section className="factory-workspace__panel" aria-label="Operasyonel görünüm">
          <div className="factory-workspace__panel-header">
            <h2>Canlı durum</h2>
          </div>
          <div className="factory-workspace__panel-body">
            <FactoryOperationalCenter
              workspace={workspace}
              onSelectLine={(lineId) =>
                navigate({
                  areaCode: workspace.selection.areaCode,
                  lineId,
                })}
              onSelectStation={(lineId, stationId) =>
                navigate({
                  areaCode: workspace.selection.areaCode,
                  lineId,
                  stationId,
                })}
            />
          </div>
        </section>

        <aside
          className="factory-workspace__panel factory-workspace__panel--inspector"
          aria-label="Bağlam inceleyici"
        >
          <div className="factory-workspace__panel-header">
            <h2>İnceleyici</h2>
          </div>
          <div className="factory-workspace__panel-body">
            <FactoryResourceInspector workspace={workspace} />
          </div>
        </aside>
      </div>
    </main>
  );
}
