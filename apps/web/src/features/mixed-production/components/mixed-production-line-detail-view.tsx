"use client";

import Link from "next/link";
import { useMemo, useState } from "react";
import { ArrowLeft } from "lucide-react";

import { ContextPanel } from "@/components/workspace/context-panel";
import { EventTimeline } from "@/components/workspace/event-timeline";
import { LineTopology } from "@/components/workspace/line-topology";
import { OperationalBadge, mapOperationalStatus } from "@/components/workspace/operational-status";
import { StatusStrip } from "@/components/workspace/status-strip";
import { WorkspaceHeader } from "@/components/workspace/workspace-header";

import { ContextKnowledgePanel } from "@/features/context-knowledge/components/context-knowledge-panel";
import type { WorkerContextPackageApi } from "@/features/context-knowledge/server/context-knowledge-api-client";
import { ThroughputOptimizerPanel } from "@/features/throughput-optimizer/components/throughput-optimizer-panel";
import type { ThroughputOptimizerWorkspaceApi } from "@/features/throughput-optimizer/server/throughput-optimizer-api-client";
import { ThroughputIntelligencePanel } from "@/features/throughput-intelligence/components/throughput-intelligence-panel";
import type {
  ScenarioComparisonRowApi,
  ThroughputIntelligenceOverviewApi,
} from "@/features/throughput-intelligence/server/throughput-intelligence-api-client";

import type {
  MixedProductionLineDetailApi,
  MixedProductionPlaybackApi,
  MixedProductionStationSnapshotApi,
} from "../server/mixed-production-api-client";

type MixedProductionLineDetailViewProps = Readonly<{
  detail: MixedProductionLineDetailApi;
  playback: MixedProductionPlaybackApi | null;
  throughput?: {
    overview: ThroughputIntelligenceOverviewApi;
    comparison: readonly ScenarioComparisonRowApi[];
  } | null;
  contextPackage?: WorkerContextPackageApi | null;
  optimizerWorkspace?: ThroughputOptimizerWorkspaceApi | null;
}>;

function buildContextRows(station: MixedProductionStationSnapshotApi | undefined) {
  if (!station) {
    return [];
  }

  return [
    { label: "Durum", value: station.state },
    { label: "Kaynak", value: station.resourceCode ? `${station.resourceCode} (${station.resourceType ?? "—"})` : "—" },
    { label: "Operasyon", value: station.operationCode },
    { label: "Operatör", value: station.operatorCode },
    { label: "WIP", value: station.bufferQuantity != null ? `${station.bufferQuantity}/${station.bufferCapacity ?? "—"}` : null },
    { label: "GOOD", value: station.goodCount },
    { label: "REJECT", value: station.rejectCount },
  ];
}

export function MixedProductionLineDetailView({ detail, playback, throughput, contextPackage, optimizerWorkspace }: MixedProductionLineDetailViewProps) {
  const { line, stations, timeline } = detail;
  const [selectedStationId, setSelectedStationId] = useState<string | null>(stations[0]?.stationId ?? null);
  const selectedStation = stations.find((s) => s.stationId === selectedStationId);
  const latestPlaybackFrame = playback?.frames[playback.frames.length - 1] ?? null;

  const timelineEntries = useMemo(
    () =>
      timeline.map((entry) => ({
        id: entry.eventId,
        occurredAtUtc: entry.occurredAtUtc,
        category: entry.sourceCategory,
        eventType: entry.eventType,
        summary: entry.summary,
        quantity: entry.quantity,
      })),
    [timeline],
  );

  return (
    <main className="fact-workspace" data-testid="mixed-production-line-detail-view">
      <WorkspaceHeader
        context="ÜRETİM · HAT OPERASYONU"
        title={line.name}
        meta={`${line.code} · ${line.currentProductCode ?? "—"} · ${line.currentOrderReference ?? "—"} · Vardiya ${line.currentShiftCode ?? "—"}`}
        breadcrumb={
          <Link href="/uretim/hatlar">
            <ArrowLeft aria-hidden="true" size={14} />
            Hatlar
          </Link>
        }
      />

      <StatusStrip
        aria-label="Hat özet"
        items={[
          { label: "Hat durumu", value: line.lineState },
          { label: "GOOD (bugün)", value: line.goodOutputToday },
          { label: "REJECT (bugün)", value: line.rejectOutputToday },
          { label: "İstasyon", value: stations.length },
          { label: "Olay", value: timeline.length },
        ]}
      />

      <section className="fact-master-detail" aria-label="Hat topolojisi ve istasyon detayı">
        <div className="fact-master-detail__detail" style={{ display: "grid", gap: "10px" }}>
          <section className="fact-section">
            <header className="fact-section__header">
              <h2 className="fact-section__title">Hat topolojisi</h2>
              <OperationalBadge status={mapOperationalStatus(line.lineState)} label={line.lineState} />
            </header>
            <div className="fact-section__body">
              <LineTopology
                stations={stations.map((s) => ({
                  id: s.stationId,
                  code: s.code,
                  name: s.name,
                  state: s.state,
                }))}
                selectedStationId={selectedStationId}
                onSelectStation={setSelectedStationId}
              />
            </div>
          </section>

          <section className="fact-section" data-testid="mixed-line-timeline">
            <header className="fact-section__header">
              <h2 className="fact-section__title">Üretim zaman çizelgesi</h2>
              <span className="fact-workspace__meta">{detail.playbackWindowLabel}</span>
            </header>
            <div className="fact-section__body">
              <EventTimeline entries={timelineEntries} />
            </div>
          </section>
        </div>

        <div className="fact-master-detail__master">
          <ContextPanel title={selectedStation ? `${selectedStation.code} · ${selectedStation.name}` : "İstasyon seçin"} rows={buildContextRows(selectedStation)} />

          {latestPlaybackFrame ? (
            <section className="fact-section" style={{ marginTop: "8px" }} data-testid="mixed-line-playback">
              <header className="fact-section__header">
                <h2 className="fact-section__title">Playback</h2>
                <OperationalBadge status={mapOperationalStatus(latestPlaybackFrame.lineState)} label={latestPlaybackFrame.lineState} />
              </header>
              <div className="fact-section__body">
                <p className="fact-workspace__meta">
                  {new Date(latestPlaybackFrame.timestampUtc).toLocaleString("tr-TR")}
                </p>
                <table className="fact-data-grid">
                  <thead>
                    <tr>
                      <th>İstasyon</th>
                      <th>Durum</th>
                      <th>WIP</th>
                      <th>Operatör</th>
                    </tr>
                  </thead>
                  <tbody>
                    {latestPlaybackFrame.stations.map((station) => (
                      <tr key={station.stationCode}>
                        <td className="fact-numeric">{station.stationCode}</td>
                        <td>{station.state}</td>
                        <td className="fact-numeric">{station.bufferQuantity ?? "—"}</td>
                        <td>{station.operatorCode ?? "—"}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </section>
          ) : null}
        </div>
      </section>

      {throughput ? (
        <ThroughputIntelligencePanel overview={throughput.overview} comparison={throughput.comparison} />
      ) : null}

      {contextPackage ? <ContextKnowledgePanel package={contextPackage} /> : null}

      {optimizerWorkspace ? <ThroughputOptimizerPanel workspace={optimizerWorkspace} /> : null}
    </main>
  );
}
