"use client";

import Link from "next/link";
import { useMemo, useState } from "react";

import { ArrowLeft } from "lucide-react";
import { ContextPanel } from "@/components/workspace/context-panel";
import { EventTimeline } from "@/components/workspace/event-timeline";
import { LineTopology } from "@/components/workspace/line-topology";
import { OperationalBadge, mapOperationalStatus } from "@/components/workspace/operational-status";
import { StatusStrip } from "@/components/workspace/status-strip";
import { PageHeader } from "@/components/ui/page";

import { ContextKnowledgePanel } from "@/features/context-knowledge/components/context-knowledge-panel";
import type { WorkerContextPackageApi } from "@/features/context-knowledge/server/context-knowledge-api-client";
import { getProductionResourceTypeLabel } from "@/features/production-resources/domain/production-resources-labels";
import { ThroughputOptimizerPanel } from "@/features/throughput-optimizer/components/throughput-optimizer-panel";
import type { ThroughputOptimizerWorkspaceApi } from "@/features/throughput-optimizer/server/throughput-optimizer-api-client";
import { ThroughputIntelligencePanel } from "@/features/throughput-intelligence/components/throughput-intelligence-panel";
import type {
  ScenarioComparisonRowApi,
  ThroughputIntelligenceOverviewApi,
} from "@/features/throughput-intelligence/server/throughput-intelligence-api-client";
import { eventCategoryLabel, lineHeaderFacts, lineStateLabel } from "@/lib/presentation/throughput-labels";

import type {
  MixedProductionLineDetailApi,
  MixedProductionPlaybackApi,
  MixedProductionStationSnapshotApi,
} from "../server/mixed-production-api-client";

import "./mixed-production.css";

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

export function buildStationContextRows(station: MixedProductionStationSnapshotApi | undefined) {
  if (!station) {
    return [];
  }

  const rows: { label: string; value: string | number | null | undefined }[] = [
    { label: "Durum", value: lineStateLabel(station.state) },
    {
      label: "Kaynak",
      value: station.resourceCode
        ? `${station.resourceCode}${station.resourceType ? ` (${getProductionResourceTypeLabel(station.resourceType)})` : ""}`
        : null,
    },
    { label: "Operasyon", value: station.operationCode },
    { label: "Operatör", value: station.operatorCode },
    {
      label: "Devam eden iş",
      value: station.bufferQuantity != null ? `${station.bufferQuantity}${station.bufferCapacity != null ? ` / ${station.bufferCapacity}` : ""}` : null,
    },
    { label: "İyi", value: station.goodCount },
    { label: "Hurda", value: station.rejectCount },
  ];

  // Empty facts are dropped instead of printing a column of dashes.
  return rows.filter((row) => row.value !== null && row.value !== undefined && row.value !== "");
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
        category: eventCategoryLabel(entry.sourceCategory),
        // The technical event code is not customer copy; the summary carries the meaning.
        eventType: "",
        summary: entry.summary,
        quantity: entry.quantity,
      })),
    [timeline],
  );

  return (
    <div className="fact-workspace mlp-detail" data-testid="mixed-production-line-detail-view">
      <Link className="mlp-back-link" href="/uretim/hatlar">
        <ArrowLeft aria-hidden="true" />
        <span>Üretim hatları</span>
      </Link>

      <PageHeader
        eyebrow="ÜRETİM"
        title={line.name}
        description={lineHeaderFacts(line)}
        actions={<Link className="button button-primary button-medium" href="/planlama/yeni"><span>Üretim Emri Planla</span></Link>}
      />

      <StatusStrip
        aria-label="Hat özet"
        items={[
          { label: "Hat durumu", value: lineStateLabel(line.lineState) },
          { label: "İyi (bugün)", value: line.goodOutputToday || "—" },
          { label: "Hurda (bugün)", value: line.rejectOutputToday || "—" },
          { label: "Olay", value: timeline.length || "—" },
        ]}
      />

      {contextPackage ? <ContextKnowledgePanel package={contextPackage} /> : null}

      <section className="mlp-layout" aria-label="Hat topolojisi ve istasyon detayı">
        <section className="fact-section mlp-layout__topology">
          <header className="fact-section__header">
            <h2 className="fact-section__title">Hat topolojisi</h2>
          </header>
          <div className="fact-section__body">
            <LineTopology
              stations={stations.map((s) => ({
                id: s.stationId,
                code: s.code,
                name: s.name,
                state: s.state,
                statusLabel: lineStateLabel(s.state),
              }))}
              selectedStationId={selectedStationId}
              onSelectStation={setSelectedStationId}
            />
          </div>
        </section>

        {stations.length > 1 ? <div className="mlp-layout__station">
          <ContextPanel
            title={selectedStation ? `${selectedStation.code} · ${selectedStation.name}` : "İstasyon seçin"}
            rows={buildStationContextRows(selectedStation)}
          />
        </div> : null}

        <section className="fact-section mlp-layout__timeline" data-testid="mixed-line-timeline">
          <header className="fact-section__header">
            <h2 className="fact-section__title">Üretim zaman çizelgesi</h2>
            {detail.playbackWindowLabel && detail.playbackWindowLabel.replace(/[\s—–-]/g, "") ? <span className="fact-workspace__meta">{detail.playbackWindowLabel}</span> : null}
          </header>
          <div className="fact-section__body">
            <EventTimeline entries={timelineEntries} />
          </div>
        </section>

        {latestPlaybackFrame ? (
          <section className="fact-section mlp-layout__playback" data-testid="mixed-line-playback">
            <header className="fact-section__header">
              <h2 className="fact-section__title">Son durum kaydı</h2>
              <OperationalBadge status={mapOperationalStatus(latestPlaybackFrame.lineState)} label={latestPlaybackFrame.lineState} />
            </header>
            <div className="fact-section__body">
              <p className="fact-workspace__meta">
                {new Date(latestPlaybackFrame.timestampUtc).toLocaleString("tr-TR")}
              </p>
              <div className="mlp-table-scroll" tabIndex={0}>
                <table aria-label="Son durum kaydı" className="fact-data-grid">
                  <thead>
                    <tr>
                      <th>İstasyon</th>
                      <th>Durum</th>
                      <th>Devam eden iş</th>
                      <th>Operatör</th>
                    </tr>
                  </thead>
                  <tbody>
                    {latestPlaybackFrame.stations.map((station) => (
                      <tr key={station.stationCode}>
                        <td className="fact-numeric">{station.stationCode}</td>
                        <td>{lineStateLabel(station.state)}</td>
                        <td className="fact-numeric">{station.bufferQuantity ?? "—"}</td>
                        <td>{station.operatorCode ?? "—"}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            </div>
          </section>
        ) : null}
      </section>

      {throughput ? (
        <ThroughputIntelligencePanel overview={throughput.overview} comparison={throughput.comparison} embedded />
      ) : null}

      {optimizerWorkspace ? <ThroughputOptimizerPanel workspace={optimizerWorkspace} /> : null}
    </div>
  );
}
