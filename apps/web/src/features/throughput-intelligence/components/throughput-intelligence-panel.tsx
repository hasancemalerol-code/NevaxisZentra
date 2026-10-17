"use client";

import { LineTopology } from "@/components/workspace/line-topology";
import { StatusStrip } from "@/components/workspace/status-strip";
import { constraintTypeLabel, dataQualityLabel, lineStateLabel, scenarioEligibilityLabel } from "@/lib/presentation/throughput-labels";

import type {
  ScenarioComparisonRowApi,
  ThroughputIntelligenceOverviewApi,
} from "../server/throughput-intelligence-api-client";

type ThroughputIntelligencePanelProps = Readonly<{
  overview: ThroughputIntelligenceOverviewApi;
  comparison: readonly ScenarioComparisonRowApi[];
  /**
   * True when the host page already shows the line topology and the event count (the line detail page).
   * The panel then leaves both out so the page does not repeat them.
   */
  embedded?: boolean;
}>;

export function ThroughputIntelligencePanel({ overview, comparison, embedded = false }: ThroughputIntelligencePanelProps) {
  const currentConstraint = overview.constraints.find((c) => c.role === "CURRENT");
  const secondaryConstraint = overview.constraints.find((c) => c.role === "SECONDARY");

  // A stopped line with no events or target has no rate to report; "—" reads as "no data", not as a measured zero.
  const hasTarget = overview.productionGap.requiredGoodPerHour > 0;
  const hasMeasurement = overview.eventsConsidered > 0 || overview.measurement.goodUnitsPerHour > 0;
  // Facts without data are left out instead of repeating empty tiles.
  const stripItems = [
    { label: "İyi/saat", value: hasMeasurement ? overview.measurement.goodUnitsPerHour.toFixed(0) : "—" },
    { label: "Hedef", value: hasTarget ? overview.productionGap.requiredGoodPerHour.toFixed(0) : "—" },
    { label: "Fark", value: hasTarget ? overview.productionGap.gapPerHour.toFixed(0) : "—" },
    { label: "Veri kalitesi", value: dataQualityLabel(overview.dataQuality) },
    ...(embedded ? [] : [{ label: "Değerlendirilen olay", value: overview.eventsConsidered }]),
  ].filter((item) => item.value !== "—");

  return (
    <div className="fact-workspace mlp-panel" data-testid="throughput-intelligence-panel">
      <StatusStrip aria-label="Verim durumu" items={stripItems} />

      <section className="fact-section" aria-label="Mevcut kısıt">
        <header className="fact-section__header">
          <h2 className="fact-section__title">Mevcut kısıt</h2>
        </header>
        <div className="fact-section__body">
          {currentConstraint ? (
            <div className="fact-constraint-indicator" data-testid="current-constraint">
              <strong>{currentConstraint.stationCode}</strong>
              <span>{constraintTypeLabel(currentConstraint.constraintType)}</span>
              <span>−{currentConstraint.impactOnGoodThroughputPerHour.toFixed(0)} iyi/saat</span>
            </div>
          ) : (
            <p className="mlp-empty-note">Bu hat için belirgin bir kısıt tespit edilemedi.</p>
          )}
          {secondaryConstraint ? (
            <p className="fact-workspace__meta">
              İkincil: {secondaryConstraint.stationCode} · {constraintTypeLabel(secondaryConstraint.constraintType)}
            </p>
          ) : null}
          {currentConstraint ? <p className="fact-workspace__meta">{currentConstraint.explanation}</p> : null}
        </div>
      </section>

      <section className="fact-section" aria-label={embedded ? "İstasyon verimi" : "Hat topolojisi verim"}>
        <header className="fact-section__header">
          <h2 className="fact-section__title">{embedded ? "İstasyon verimi" : "Hat topolojisi · verim"}</h2>
        </header>
        <div className="fact-section__body">
          {embedded ? null : (
            <LineTopology
              stations={overview.topology.map((s) => ({
                id: s.stationId,
                code: s.code,
                name: s.name,
                state: s.state,
                statusLabel: s.isCurrentConstraint ? `Kısıt · ${lineStateLabel(s.state)}` : lineStateLabel(s.state),
              }))}
            />
          )}
          <div className="mlp-table-scroll" tabIndex={0}>
            <table aria-label="İstasyon verimi" className="fact-data-grid" data-testid="throughput-topology-grid">
              <thead>
                <tr>
                  <th>İstasyon</th>
                  <th>Çevrim (sn)</th>
                  <th>Kapasite (adet/saat)</th>
                  <th>İyi</th>
                  <th>Hurda</th>
                  <th>Devam eden iş</th>
                </tr>
              </thead>
              <tbody>
                {overview.topology.map((station) => (
                  <tr key={station.stationId} className={station.isCurrentConstraint ? "fact-row--highlight" : undefined}>
                    <td>
                      {station.name ? `${station.code} · ${station.name}` : station.code}
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
        </div>
      </section>

      <section className="fact-section" aria-label="Kayıp ağacı">
        <header className="fact-section__header">
          <h2 className="fact-section__title">Verim kayıp ağacı</h2>
          {overview.lossTree.nodes.length > 0 ? (
            <span className="fact-workspace__meta">Uzlaşma: {dataQualityLabel(overview.lossTree.reconciliationQuality)}</span>
          ) : null}
        </header>
        <div className="fact-section__body">
          {overview.lossTree.nodes.length === 0 ? (
            <p className="mlp-empty-note" data-testid="loss-tree-empty">
              Henüz kayıp verisi yok. Üretim olayları geldikçe kayıp nedenleri burada listelenir.
            </p>
          ) : (
            <div className="mlp-table-scroll" tabIndex={0}>
              <table aria-label="Verim kayıp ağacı" className="fact-data-grid" data-testid="loss-tree-grid">
                <thead>
                  <tr>
                    <th>Kayıp</th>
                    <th>İyi/saat</th>
                    <th>Kalite</th>
                  </tr>
                </thead>
                <tbody>
                  {overview.lossTree.nodes.map((node, index) => (
                    <tr key={`${node.category}-${index}`}>
                      <td>{node.label}</td>
                      <td>{node.lostGoodUnitsPerHour.toFixed(1)}</td>
                      <td>{dataQualityLabel(node.attributionQuality)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
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
                  <div>{row.predictedGoodThroughputPerHour.toFixed(0)} iyi/saat</div>
                  <div>Fark {row.deltaGoodThroughputPerHour >= 0 ? "+" : ""}{row.deltaGoodThroughputPerHour.toFixed(0)}</div>
                  <div>{row.constraintBeforeCode ?? "—"} → {row.constraintAfterCode ?? "—"}</div>
                  <div>{scenarioEligibilityLabel(row.eligibility)}</div>
                </div>
              ))}
            </div>
          </div>
        </section>
      ) : null}
    </div>
  );
}
