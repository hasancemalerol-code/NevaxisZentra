"use client";

import { LineTopology } from "@/components/workspace/line-topology";
import { StatusStrip } from "@/components/workspace/status-strip";

import type {
  ScenarioComparisonRowApi,
  ThroughputIntelligenceOverviewApi,
} from "../server/throughput-intelligence-api-client";

type ThroughputIntelligencePanelProps = Readonly<{
  overview: ThroughputIntelligenceOverviewApi;
  comparison: readonly ScenarioComparisonRowApi[];
}>;

export function ThroughputIntelligencePanel({ overview, comparison }: ThroughputIntelligencePanelProps) {
  const currentConstraint = overview.constraints.find((c) => c.role === "CURRENT");
  const secondaryConstraint = overview.constraints.find((c) => c.role === "SECONDARY");

  return (
    <div className="fact-workspace" data-testid="throughput-intelligence-panel">
      <StatusStrip
        aria-label="Verim durumu"
        items={[
          { label: "GOOD/saat", value: overview.measurement.goodUnitsPerHour.toFixed(0) },
          { label: "Hedef", value: overview.productionGap.requiredGoodPerHour.toFixed(0) },
          { label: "Gap", value: overview.productionGap.gapPerHour.toFixed(0) },
          { label: "Veri kalitesi", value: overview.dataQuality },
          { label: "Olay", value: overview.eventsConsidered },
        ]}
      />

      <section className="fact-section" aria-label="Mevcut kısıt">
        <header className="fact-section__header">
          <h2 className="fact-section__title">Mevcut kısıt</h2>
        </header>
        <div className="fact-section__body">
          {currentConstraint ? (
            <div className="fact-constraint-indicator" data-testid="current-constraint">
              <strong>{currentConstraint.stationCode}</strong>
              <span>{currentConstraint.constraintType}</span>
              <span>−{currentConstraint.impactOnGoodThroughputPerHour.toFixed(0)} GOOD/saat</span>
            </div>
          ) : (
            <p className="fact-workspace__meta">Kısıt belirlenemedi</p>
          )}
          {secondaryConstraint ? (
            <p className="fact-workspace__meta">
              İkincil: {secondaryConstraint.stationCode} · {secondaryConstraint.constraintType}
            </p>
          ) : null}
          {currentConstraint ? <p className="fact-workspace__meta">{currentConstraint.explanation}</p> : null}
        </div>
      </section>

      <section className="fact-section" aria-label="Hat topolojisi verim">
        <header className="fact-section__header">
          <h2 className="fact-section__title">Hat topolojisi · verim</h2>
        </header>
        <div className="fact-section__body">
          <LineTopology
            stations={overview.topology.map((s) => ({
              id: s.stationId,
              code: s.code,
              name: s.name,
              state: s.isCurrentConstraint ? `CONSTRAINT · ${s.state}` : s.state,
            }))}
          />
          <table className="fact-data-grid" data-testid="throughput-topology-grid">
            <thead>
              <tr>
                <th>İstasyon</th>
                <th>Çevrim</th>
                <th>Kapasite</th>
                <th>GOOD</th>
                <th>REJECT</th>
                <th>WIP</th>
              </tr>
            </thead>
            <tbody>
              {overview.topology.map((station) => (
                <tr key={station.stationId} className={station.isCurrentConstraint ? "fact-row--highlight" : undefined}>
                  <td>
                    {station.code}
                    {station.isCurrentConstraint ? " ●" : station.isSecondaryConstraint ? " ○" : ""}
                  </td>
                  <td>{station.effectiveCycleSeconds?.toFixed(1) ?? "—"}</td>
                  <td>{station.effectiveGoodCapacityPerHour?.toFixed(0) ?? "—"}</td>
                  <td>{station.goodCount}</td>
                  <td>{station.rejectCount}</td>
                  <td>{station.wip ?? "—"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>

      <section className="fact-section" aria-label="Kayıp ağacı">
        <header className="fact-section__header">
          <h2 className="fact-section__title">Verim kayıp ağacı</h2>
          <span className="fact-workspace__meta">{overview.lossTree.reconciliationQuality}</span>
        </header>
        <div className="fact-section__body">
          <table className="fact-data-grid" data-testid="loss-tree-grid">
            <thead>
              <tr>
                <th>Kayıp</th>
                <th>GOOD/saat</th>
                <th>Kalite</th>
              </tr>
            </thead>
            <tbody>
              {overview.lossTree.nodes.map((node, index) => (
                <tr key={`${node.category}-${index}`}>
                  <td>{node.label}</td>
                  <td>{node.lostGoodUnitsPerHour.toFixed(1)}</td>
                  <td>{node.attributionQuality}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </section>

      {comparison.length > 0 ? (
        <section className="fact-section" aria-label="Senaryo karşılaştırma">
          <header className="fact-section__header">
            <h2 className="fact-section__title">Senaryo karşılaştırma</h2>
          </header>
          <div className="fact-section__body">
            <div className="fact-scenario-compare" data-testid="scenario-compare">
              {comparison.map((row) => (
                <div key={row.scenarioCode} className="fact-scenario-compare__column">
                  <strong>{row.title}</strong>
                  <div>{row.predictedGoodThroughputPerHour.toFixed(0)} GOOD/saat</div>
                  <div>Δ {row.deltaGoodThroughputPerHour >= 0 ? "+" : ""}{row.deltaGoodThroughputPerHour.toFixed(0)}</div>
                  <div>{row.constraintBeforeCode ?? "—"} → {row.constraintAfterCode ?? "—"}</div>
                  <div>{row.eligibility}</div>
                </div>
              ))}
            </div>
          </div>
        </section>
      ) : null}
    </div>
  );
}
