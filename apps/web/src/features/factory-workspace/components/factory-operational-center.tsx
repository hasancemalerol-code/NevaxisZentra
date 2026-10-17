"use client";

import Link from "next/link";
import { useMemo, useState } from "react";

import { ContextPanel } from "@/components/workspace/context-panel";
import { EventTimeline } from "@/components/workspace/event-timeline";
import { LineTopology } from "@/components/workspace/line-topology";
import { OperationalBadge, mapOperationalStatus } from "@/components/workspace/operational-status";
import { knownStateLabel } from "@/lib/presentation/enum-labels";
import { StatusStrip } from "@/components/workspace/status-strip";
import { ThroughputIntelligencePanel } from "@/features/throughput-intelligence/components/throughput-intelligence-panel";
import { ThroughputOptimizerPanel } from "@/features/throughput-optimizer/components/throughput-optimizer-panel";
import { ContinuousOptimizationOpportunityStrip } from "@/features/continuous-optimization/components/continuous-optimization-control-center-view";
import { buildScenarioRecoveryHref } from "@/features/scenario-recovery/application/scenario-recovery-links";
import type { MixedProductionStationSnapshotApi } from "@/features/mixed-production/server/mixed-production-api-client";
import { collectLineStates } from "../application/build-factory-hierarchy";
import { resolveActiveSelectionKind } from "../application/factory-workspace-selection";
import type { FactoryWorkspacePayload } from "../domain/factory-workspace-types";

type LineRow = Readonly<{
  lineId: string;
  lineCode: string;
  lineName: string;
  state: string;
  order?: string;
  good: number;
  reject: number;
}>;

function LineRowsList({
  rows,
  onSelectLine,
}: Readonly<{ rows: readonly LineRow[]; onSelectLine: (lineId: string) => void }>) {
  return (
    <div className="factory-operational-rows" role="group" aria-label="Hat durumları">
      <div className="factory-operational-row factory-operational-row--head" aria-hidden="true">
        <span>Hat</span>
        <span>Durum</span>
        <span>Emir</span>
        <span className="fact-numeric">İyi üretim</span>
        <span className="fact-numeric">Fire</span>
      </div>
      {rows.map((line) => (
        <button
          key={line.lineId}
          type="button"
          className="factory-operational-row"
          onClick={() => onSelectLine(line.lineId)}
        >
          <div className="factory-operational-row__primary">
            <strong title={line.lineCode}>{line.lineCode}</strong>
            <span title={line.lineName}>{line.lineName}</span>
          </div>
          <span>
            <OperationalBadge label={line.state} status={mapOperationalStatus(line.state)} />
          </span>
          <span className="factory-operational-row__order" data-label="Emir">{line.order ?? "Emir yok"}</span>
          <span className="fact-numeric" data-label="İyi üretim">{line.good}</span>
          <span className="fact-numeric" data-label="Fire">{line.reject}</span>
        </button>
      ))}
    </div>
  );
}

type FactoryOperationalCenterProps = Readonly<{
  workspace: FactoryWorkspacePayload;
  onSelectLine: (lineId: string) => void;
  onSelectStation: (lineId: string, stationId: string) => void;
}>;

function buildStationRows(station: MixedProductionStationSnapshotApi | undefined) {
  if (!station) return [];
  return [
    { label: "Durum", value: knownStateLabel(station.state) ?? station.state },
    { label: "Operasyon", value: station.operationCode ?? "—" },
    { label: "Kaynak", value: station.resourceCode ?? "—" },
    { label: "Operatör", value: station.operatorCode ?? "—" },
    { label: "İyi üretim", value: station.goodCount },
    { label: "Fire", value: station.rejectCount },
    { label: "Ara stok", value: station.bufferQuantity != null ? `${station.bufferQuantity}/${station.bufferCapacity ?? "—"}` : "—" },
  ];
}

export function FactoryOperationalCenter({
  workspace,
  onSelectLine,
  onSelectStation,
}: FactoryOperationalCenterProps) {
  const activeKind = resolveActiveSelectionKind(workspace.selection);
  const [selectedStationId, setSelectedStationId] = useState<string | null>(
    workspace.selection.stationId ?? workspace.lineDetail?.stations[0]?.stationId ?? null,
  );

  const areaLines = useMemo(() => {
    if (!workspace.selection.areaCode) {
      return workspace.resources.resources;
    }
    return workspace.resources.resources.filter(
      (resource) => resource.areaCode === workspace.selection.areaCode,
    );
  }, [workspace.resources.resources, workspace.selection.areaCode]);

  const lineRows = useMemo(() => {
    const canonicalStates = collectLineStates(workspace.hierarchy);
    const map = new Map<string, LineRow>();
    for (const resource of workspace.resources.resources) {
      const current = map.get(resource.lineId);
      if (!current) {
        map.set(resource.lineId, {
          lineId: resource.lineId,
          lineCode: resource.lineCode,
          lineName: resource.lineName,
          state: canonicalStates.get(resource.lineId) ?? resource.state,
          order: resource.orderReference,
          good: resource.goodCount,
          reject: resource.rejectCount,
        });
      } else {
        map.set(resource.lineId, {
          ...current,
          good: current.good + resource.goodCount,
          reject: current.reject + resource.rejectCount,
        });
      }
    }
    return [...map.values()];
  }, [workspace.hierarchy, workspace.resources.resources]);

  const selectedStation = workspace.lineDetail?.stations.find(
    (station) => station.stationId === (workspace.selection.stationId ?? selectedStationId),
  );

  if (activeKind === "factory" && lineRows.length === 0) {
    return (
      <div data-testid="factory-level-view" className="factory-operational-empty">
        <p>Hat tanımı eklendiğinde hat durumları, üretilen ve reddedilen adetler burada listelenir.</p>
        {workspace.activeOrderCount != null ? <p>Aktif üretim emri: <strong>{workspace.activeOrderCount}</strong></p> : null}
      </div>
    );
  }

  if (activeKind === "factory") {
    return (
      <div data-testid="factory-level-view" className="factory-view-stack">
        <section className="fact-section" aria-label="Hat durumları">
          <header className="fact-section__header">
            <h2 className="fact-section__title">Hatlar</h2>
            <span className="factory-operational-count">
              {lineRows.length} hat
              {workspace.activeOrderCount != null ? ` · ${workspace.activeOrderCount} aktif emir` : ""}
            </span>
          </header>
          <div className="fact-section__body">
            <LineRowsList rows={lineRows} onSelectLine={onSelectLine} />
          </div>
        </section>

        {workspace.risks.length > 0 ? (
          <section className="fact-section" aria-label="Fabrika sinyalleri">
            <header className="fact-section__header">
              <h2 className="fact-section__title">Fabrika sinyalleri</h2>
              <Link href="/fabrika-zekasi">Tüm sinyaller</Link>
            </header>
            <ul className="factory-signal-list">
              {workspace.risks.slice(0, 4).map((risk) => (
                <li key={risk.riskId}>
                  <strong>{risk.machineCode}</strong>
                  <p>{risk.observedSummary}</p>
                  <p>{risk.predictedSummary}</p>
                  <small>{risk.horizon} · {risk.confidenceBand}</small>
                </li>
              ))}
            </ul>
          </section>
        ) : null}

        {workspace.continuousOptimization.length > 0 ? (
          <ContinuousOptimizationOpportunityStrip opportunities={workspace.continuousOptimization} />
        ) : null}
      </div>
    );
  }

  if (activeKind === "area") {
    const areaLineIds = new Set(areaLines.map((resource) => resource.lineId));
    const rows = lineRows.filter((line) =>
      workspace.resources.resources.some(
        (resource) => resource.lineCode === line.lineCode && areaLineIds.has(resource.lineId),
      ),
    );

    return (
      <div data-testid="factory-area-view" className="factory-view-stack">
        <StatusStrip
          aria-label="Alan özet"
          items={[
            { label: "Alan", value: workspace.selection.areaCode ?? "—" },
            { label: "Hat", value: rows.length },
            { label: "Kaynak", value: areaLines.length },
            { label: "Çalışan", value: areaLines.filter((resource) => resource.state === "RUNNING").length },
          ]}
        />
        <LineRowsList rows={rows} onSelectLine={onSelectLine} />
      </div>
    );
  }

  if ((activeKind === "line" || activeKind === "station" || activeKind === "resource") && workspace.lineDetail) {
    const { line, stations, timeline } = workspace.lineDetail;
    const timelineEntries = timeline.slice(-12).map((entry) => ({
      id: entry.eventId,
      occurredAtUtc: entry.occurredAtUtc,
      category: entry.sourceCategory,
      eventType: entry.eventType,
      summary: entry.summary,
      quantity: entry.quantity,
    }));

    return (
      <div data-testid="factory-line-view" className="factory-view-stack">
        <StatusStrip
          aria-label="Hat özet"
          items={[
            { label: "Durum", value: knownStateLabel(line.lineState) ?? line.lineState },
            { label: "Emir", value: line.currentOrderReference ?? "—" },
            { label: "İyi üretim", value: line.goodOutputToday },
            { label: "Fire", value: line.rejectOutputToday },
            { label: "Vardiya", value: line.currentShiftCode ?? workspace.shift?.shiftLabel ?? "—" },
          ]}
        />

        <section className="fact-section">
          <header className="fact-section__header">
            <h2 className="fact-section__title">Hat topolojisi</h2>
            <div className="fact-section__actions">
              <Link
                href={buildScenarioRecoveryHref({
                  lineId: line.lineId,
                  entryPoint: "FABRIKA",
                  constraintStationCode: workspace.throughput?.overview?.constraints?.[0]?.stationCode,
                })}
                data-testid="factory-simulate-recovery-link"
              >
                Simüle et
              </Link>
              <OperationalBadge label={line.lineState} status={mapOperationalStatus(line.lineState)} />
            </div>
          </header>
          <div className="fact-section__body">
            <LineTopology
              stations={stations.map((station) => ({
                id: station.stationId,
                code: station.code,
                name: station.name,
                state: station.state,
              }))}
              selectedStationId={workspace.selection.stationId ?? selectedStationId}
              onSelectStation={(stationId) => {
                setSelectedStationId(stationId);
                onSelectStation(line.lineId, stationId);
              }}
            />
          </div>
        </section>

        {selectedStation ? (
          <section className="fact-section" data-testid="factory-station-view">
            <header className="fact-section__header">
              <h2 className="fact-section__title">{selectedStation.code} · {selectedStation.name}</h2>
            </header>
            <div className="fact-section__body">
              <ContextPanel title="İstasyon durumu" rows={buildStationRows(selectedStation)} />
            </div>
          </section>
        ) : null}

        <section className="fact-section">
          <header className="fact-section__header">
            <h2 className="fact-section__title">Son anlamlı değişiklikler</h2>
          </header>
          <div className="fact-section__body">
            <EventTimeline entries={timelineEntries} />
          </div>
        </section>

        {workspace.throughput ? (
          <ThroughputIntelligencePanel
            comparison={workspace.throughput.comparison}
            overview={workspace.throughput.overview}
          />
        ) : null}

        {workspace.continuousOptimization.length > 0 ? (
          <ContinuousOptimizationOpportunityStrip opportunities={workspace.continuousOptimization} />
        ) : null}

        {workspace.optimizer ? <ThroughputOptimizerPanel workspace={workspace.optimizer} /> : null}
      </div>
    );
  }

  return (
    <div className="state-panel">
      <p>Seçili bağlam için operasyonel veri bulunamadı.</p>
      <Link href="/uretim/hatlar">Hat listesine git</Link>
    </div>
  );
}
