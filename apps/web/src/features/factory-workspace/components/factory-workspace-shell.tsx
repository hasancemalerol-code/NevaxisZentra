"use client";

import { useCallback } from "react";
import { usePathname, useRouter } from "next/navigation";

import { FactoryContextBreadcrumb } from "@/components/workspace/factory-context-breadcrumb";
import { Activity, AlertTriangle, CalendarClock, Clock, PauseCircle, ShieldAlert } from "lucide-react";
import type { ReactNode } from "react";

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

const NO_ACTIVE_SHIFT_LABEL = "Aktif vardiya yok";

type StripTone = "neutral" | "success" | "warning" | "danger";

type StripItem = Readonly<{ label: string; value: string | number; icon: ReactNode; tone: StripTone }>;

/** Page-level summary: same count-aware strip grid as everywhere, with an icon and a status colour per tile. */
function FactoryStatusStrip({ items }: Readonly<{ items: readonly StripItem[] }>) {
  return (
    <section className="fact-status-strip factory-status-strip" aria-label="Fabrika çalışma alanı özet">
      {items.map((item) => (
        <article key={item.label} className={`fact-status-strip__item factory-status-strip__item factory-status-strip__item--${item.tone}`}>
          <span className="factory-status-strip__icon" aria-hidden="true">{item.icon}</span>
          <span className="fact-status-strip__label">{item.label}</span>
          <strong className="fact-status-strip__value">{item.value}</strong>
        </article>
      ))}
    </section>
  );
}

function shiftStripValue(label: string | undefined): string {
  if (!label) return "Atanmadı";
  return label === NO_ACTIVE_SHIFT_LABEL ? "—" : label;
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
    <div className="fact-workspace factory-workspace" data-testid="factory-workspace">
      <WorkspaceHeader
        breadcrumb={breadcrumbSegments(workspace).length > 0 ? <FactoryContextBreadcrumb segments={breadcrumbSegments(workspace)} /> : undefined}
        context="FABRİKA"
        meta="Alan, hat, istasyon ve kaynak düzeyinde canlı fabrika durumu"
        title="Fabrikada şu anda ne oluyor?"
        actions={<FactoryCopilotWorkspaceActions />}
      />

      <FactoryWorkspacePrimaryNav pathname={pathname} />

      <FactoryStatusStrip
        items={[
          { label: "Çalışan kaynak", value: workspace.resources.status.running, icon: <Activity />, tone: workspace.resources.status.running > 0 ? "success" : "neutral" },
          { label: "Bekleyen", value: workspace.resources.status.waiting, icon: <Clock />, tone: workspace.resources.status.waiting > 0 ? "warning" : "neutral" },
          { label: "Boşta", value: workspace.resources.resources.filter((resource) => resource.state === "IDLE" || resource.state === "STOPPED").length, icon: <PauseCircle />, tone: "neutral" },
          { label: "Arızalı", value: workspace.resources.status.fault, icon: <AlertTriangle />, tone: workspace.resources.status.fault > 0 ? "danger" : "neutral" },
          { label: "Risk", value: workspace.resources.status.atRisk, icon: <ShieldAlert />, tone: workspace.resources.status.atRisk > 0 ? "warning" : "neutral" },
          { label: "Vardiya", value: shiftStripValue(workspace.shift?.shiftLabel), icon: <CalendarClock />, tone: "neutral" },
        ]}
      />

      <FactoryOnboardingEmptyState
        resourceCount={workspace.resources.resources.length}
        lineCount={new Set(workspace.resources.resources.map((resource) => resource.lineId)).size}
      />

      <div className={`factory-workspace__layout${workspace.resources.resources.length === 0 ? " factory-workspace__layout--empty" : ""}`}>
        <aside className="factory-workspace__panel factory-workspace__panel--tree" aria-label="Fabrika hiyerarşisi">
          <div className="factory-workspace__panel-header">
            <h2>Hiyerarşi</h2>
          </div>
          <div className="factory-workspace__panel-body factory-workspace__tree-body">
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
    </div>
  );
}
