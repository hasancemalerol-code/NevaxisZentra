"use client";

import Link from "next/link";
import { useMemo, useState } from "react";

import { ContextPanel } from "@/components/workspace/context-panel";
import { EventTimeline } from "@/components/workspace/event-timeline";
import { LineTopology } from "@/components/workspace/line-topology";
import { OperationalBadge, mapOperationalStatus } from "@/components/workspace/operational-status";
import { StatusStrip } from "@/components/workspace/status-strip";
import { ThroughputIntelligencePanel } from "@/features/throughput-intelligence/components/throughput-intelligence-panel";
import { ThroughputOptimizerPanel } from "@/features/throughput-optimizer/components/throughput-optimizer-panel";
import { buildScenarioRecoveryHref } from "@/features/scenario-recovery/application/scenario-recovery-links";
import type { MixedProductionStationSnapshotApi } from "@/features/mixed-production/server/mixed-production-api-client";
import { resolveActiveSelectionKind } from "../application/factory-workspace-selection";
import type { FactoryWorkspacePayload } from "../domain/factory-workspace-types";

type FactoryOperationalCenterProps = Readonly<{
  workspace: FactoryWorkspacePayload;
  onSelectLine: (lineId: string) => void;
  onSelectStation: (lineId: string, stationId: string) => void;
}>;

function buildStationRows(station: MixedProductionStationSnapshotApi | undefined) {
  if (!station) return [];
  return [
    { label: "Durum", value: station.state },
    { label: "Operasyon", value: station.operationCode ?? "—" },
    { label: "Kaynak", value: station.resourceCode ?? "—" },
    { label: "Operatör", value: station.operatorCode ?? "—" },
    { label: "GOOD", value: station.goodCount },
    { label: "REJECT", value: station.rejectCount },
    { label: "WIP", value: station.bufferQuantity != null ? `${station.bufferQuantity}/${station.bufferCapacity ?? "—"}` : "—" },
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
    const map = new Map<string, { lineCode: string; lineName: string; state: string; order?: string; good: number; reject: number }>();
    for (const resource of workspace.resources.resources) {
      const current = map.get(resource.lineId);
      if (!current) {
        map.set(resource.lineId, {
          lineCode: resource.lineCode,
          lineName: resource.lineName,
          state: resource.state,
          order: resource.orderReference,
          good: resource.goodCount,
          reject: resource.rejectCount,
        });
      } else {
        current.good += resource.goodCount;
        current.reject += resource.rejectCount;
      }
    }
    return [...map.values()];
  }, [workspace.resources.resources]);

  const selectedStation = workspace.lineDetail?.stations.find(
    (station) => station.stationId === (workspace.selection.stationId ?? selectedStationId),
  );

  if (activeKind === "factory") {
    return (
      <div data-testid="factory-level-view">
        <StatusStrip
          aria-label="Fabrika özet"
          items={[
            { label: "Hat", value: lineRows.length },
            { label: "Kaynak", value: workspace.resources.status.total },
            { label: "Çalışan", value: workspace.resources.status.running },
            { label: "Risk", value: workspace.resources.status.atRisk },
            { label: "Aktif emir", value: workspace.activeOrderCount ?? "—" },
          ]}
        />

        <section className="fact-section" aria-label="Hat durumları">
          <header className="fact-section__header">
            <h2 className="fact-section__title">Hatlar</h2>
          </header>
          <div className="fact-section__body factory-operational-rows">
            {lineRows.map((line) => (
              <button
                key={line.lineCode}
                type="button"
                className="factory-operational-row"
                onClick={() => {
                  const lineId = workspace.resources.resources.find((resource) => resource.lineCode === line.lineCode)?.lineId;
                  if (lineId) onSelectLine(lineId);
                }}
              >
                <div className="factory-operational-row__primary">
                  <strong>{line.lineCode}</strong>
                  <span>{line.lineName}</span>
                </div>
                <OperationalBadge label={line.state} status={mapOperationalStatus(line.state)} />
                <span>{line.order ?? "—"}</span>
                <span className="fact-numeric">{line.good}</span>
                <span className="fact-numeric">{line.reject}</span>
              </button>
            ))}
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
      <div data-testid="factory-area-view">
        <StatusStrip
          aria-label="Alan özet"
          items={[
            { label: "Alan", value: workspace.selection.areaCode ?? "—" },
            { label: "Hat", value: rows.length },
            { label: "Kaynak", value: areaLines.length },
            { label: "Çalışan", value: areaLines.filter((resource) => resource.state === "RUNNING").length },
          ]}
        />
        <div className="factory-operational-rows">
          {rows.map((line) => (
            <button
              key={line.lineCode}
              type="button"
              className="factory-operational-row"
              onClick={() => {
                const lineId = workspace.resources.resources.find((resource) => resource.lineCode === line.lineCode)?.lineId;
                if (lineId) onSelectLine(lineId);
              }}
            >
              <div className="factory-operational-row__primary">
                <strong>{line.lineCode}</strong>
                <span>{line.lineName}</span>
              </div>
              <OperationalBadge label={line.state} status={mapOperationalStatus(line.state)} />
              <span>{line.order ?? "—"}</span>
              <span className="fact-numeric">{line.good}</span>
              <span className="fact-numeric">{line.reject}</span>
            </button>
          ))}
        </div>
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
      <div data-testid="factory-line-view">
        <StatusStrip
          aria-label="Hat özet"
          items={[
            { label: "Durum", value: line.lineState },
            { label: "Emir", value: line.currentOrderReference ?? "—" },
            { label: "GOOD", value: line.goodOutputToday },
            { label: "REJECT", value: line.rejectOutputToday },
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
