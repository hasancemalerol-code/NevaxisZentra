"use client";

import "./factory-network.css";

import { useMemo, useState, useTransition } from "react";
import { CheckCircle2, PlayCircle, Send, XCircle } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";

import {
  formatNetworkDateTime,
  formatNetworkPercent,
  getActivityEventLabel,
  getAllocationStatusBadgeTone,
  getAllocationStatusLabel,
  getDemandRiskBadgeTone,
  getDemandRiskLevelLabel,
  getNetworkActionTypeLabel,
  getNetworkAutonomyModeLabel,
  getNetworkTriggerTypeLabel,
  getPlanningStateLabel,
  getReadinessLabel,
  getRecommendedActionLabel,
  getScenarioStatusLabel,
} from "../domain/factory-network-labels";
import {
  approveNetworkAllocationAction,
  executeNetworkAllocationAction,
  simulateFactoryNetworkAction,
} from "../server/factory-network-actions";
import type {
  FactoryNetworkWorkspaceApi,
  NetworkAllocationApi,
  NetworkAllocationEvaluationApi,
  NetworkFacilityStateApi,
} from "../server/factory-network-api-client";

const WORKSPACE_TABS = [
  { id: "overview", label: "Ağ Özeti" },
  { id: "capacity", label: "Kapasite" },
  { id: "allocations", label: "Tahsisler" },
  { id: "opportunities", label: "Fırsatlar" },
  { id: "simulator", label: "Simülatör" },
  { id: "decision", label: "Karar Detayı" },
  { id: "timeline", label: "Zaman Çizelgesi" },
  { id: "comparison", label: "Fabrika Karşılaştırma" },
] as const;

type WorkspaceTabId = (typeof WORKSPACE_TABS)[number]["id"];

type FactoryNetworkWorkspaceClientProps = Readonly<{
  workspace: FactoryNetworkWorkspaceApi;
  allocationEvaluation: NetworkAllocationEvaluationApi | null;
}>;

function resolveFacilityName(
  facilityStates: readonly NetworkFacilityStateApi[],
  facilityId: string,
): string {
  return facilityStates.find((state) => state.facilityId === facilityId)?.facilityName ?? facilityId;
}

function parseReasonSummary(allocation: NetworkAllocationApi): string {
  if (!allocation.reasonJson || allocation.reasonJson === "{}") {
    return "Karar gerekçesi henüz oluşturulmadı.";
  }

  try {
    const parsed = JSON.parse(allocation.reasonJson) as Record<string, unknown>;
    if (typeof parsed.summary === "string") {
      return parsed.summary;
    }
    if (typeof parsed.rationale === "string") {
      return parsed.rationale;
    }
  } catch {
    return "Karar gerekçesi okunamadı.";
  }

  return "Karar gerekçesi mevcut.";
}

function parseImpactSummary(json: string): string {
  if (!json || json === "{}") {
    return "Etki analizi henüz hesaplanmadı.";
  }

  try {
    const parsed = JSON.parse(json) as Record<string, unknown>;
    if (typeof parsed.summary === "string") {
      return parsed.summary;
    }
    if (typeof parsed.expectedBenefit === "string") {
      return parsed.expectedBenefit;
    }
  } catch {
    return "Etki analizi okunamadı.";
  }

  return "Etki analizi mevcut.";
}

export function FactoryNetworkWorkspaceClient({
  workspace,
  allocationEvaluation,
}: FactoryNetworkWorkspaceClientProps) {
  const [activeTab, setActiveTab] = useState<WorkspaceTabId>("overview");
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isPending, startTransition] = useTransition();

  const [simulatorOrderId, setSimulatorOrderId] = useState(
    workspace.demand[0]?.orderId ?? "",
  );
  const [simulatorSourceId, setSimulatorSourceId] = useState(
    workspace.facilityStates[0]?.facilityId ?? "",
  );
  const [simulatorDestinationId, setSimulatorDestinationId] = useState(
    workspace.facilityStates[1]?.facilityId ?? workspace.facilityStates[0]?.facilityId ?? "",
  );
  const [simulatorActionType, setSimulatorActionType] = useState("move-full-order");
  const [simulatorQuantity, setSimulatorQuantity] = useState(
    String(workspace.demand[0]?.remainingQuantity ?? 1),
  );

  const pendingAllocation = workspace.activeAllocations.find(
    (allocation) => allocation.status === "approval-pending",
  );
  const readyAllocation = workspace.activeAllocations.find(
    (allocation) => allocation.status === "ready" || allocation.status === "approved",
  );

  const recommendedFacilityId = useMemo(() => {
    const recommended = allocationEvaluation?.facilityComparisons.find((row) => row.feasible);
    return recommended?.facilityId;
  }, [allocationEvaluation]);

  function runApproval(approved: boolean) {
    const allocation = pendingAllocation;
    if (!allocation) {
      return;
    }

    startTransition(async () => {
      setMessage(null);
      setError(null);
      const result = await approveNetworkAllocationAction({
        allocationId: allocation.id,
        concurrencyToken: allocation.concurrencyToken,
        approved,
      });
      if (result.status === "failure") {
        setError(result.message);
        return;
      }
      setMessage(result.message);
    });
  }

  function runExecution() {
    const allocation = readyAllocation;
    if (!allocation) {
      return;
    }

    startTransition(async () => {
      setMessage(null);
      setError(null);
      const result = await executeNetworkAllocationAction({
        allocationId: allocation.id,
        concurrencyToken: allocation.concurrencyToken,
        overrideStaleGuard: false,
      });
      if (result.status === "failure") {
        setError(result.message);
        return;
      }
      setMessage(result.message);
    });
  }

  function runSimulation() {
    startTransition(async () => {
      setMessage(null);
      setError(null);
      const result = await simulateFactoryNetworkAction({
        networkId: workspace.network.id,
        sourceFacilityId: simulatorSourceId,
        destinationFacilityId: simulatorDestinationId,
        orderId: simulatorOrderId,
        quantity: Number(simulatorQuantity) || 0,
        actionType: simulatorActionType,
        maxScenarios: 3,
      });
      if (result.status === "failure") {
        setError(result.message);
        return;
      }
      setMessage(result.message);
    });
  }

  return (
    <div className="factory-network-workspace" data-testid="factory-network-workspace-client">
      <div className="factory-network-workspace__tabs" role="tablist" aria-label="Fabrika ağı görünümleri">
        {WORKSPACE_TABS.map((tab) => (
          <button
            key={tab.id}
            type="button"
            role="tab"
            aria-selected={activeTab === tab.id}
            className={`factory-network-workspace__tab${activeTab === tab.id ? " factory-network-workspace__tab--active" : ""}`}
            onClick={() => setActiveTab(tab.id)}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {message ? <p className="factory-network-workspace__message" role="status">{message}</p> : null}
      {error ? <p className="factory-network-workspace__error" role="alert">{error}</p> : null}

      {activeTab === "overview" ? (
        <section className="factory-network-workspace__section" data-testid="factory-network-overview">
          <div className="commint-kpi-grid">
            <Card>
              <span>Ağ kullanımı</span>
              <strong>{formatNetworkPercent(workspace.metrics.networkUtilizationPercent)}</strong>
            </Card>
            <Card>
              <span>Riskli sipariş</span>
              <strong>{workspace.metrics.ordersAtRisk}</strong>
            </Card>
            <Card>
              <span>Kurtarılan sipariş</span>
              <strong>{workspace.metrics.ordersRecovered}</strong>
            </Card>
            <Card>
              <span>Kapasite dengesizliği</span>
              <strong>{formatNetworkPercent(workspace.metrics.capacityImbalancePercent)}</strong>
            </Card>
            <Card>
              <span>Tesisler arası karar</span>
              <strong>{workspace.metrics.activeCrossPlantDecisions}</strong>
            </Card>
            <Card>
              <span>Plan uyumu</span>
              <strong>{formatNetworkPercent(workspace.metrics.networkPlanAdherencePercent)}</strong>
            </Card>
          </div>

          <div className="factory-network-factory-grid">
            {workspace.facilityStates.map((facility) => (
              <article key={facility.facilityId} className="factory-network-factory-card">
                <div className="factory-network-factory-card__header">
                  <div>
                    <p className="factory-network-factory-card__name">{facility.facilityName}</p>
                    <p className="factory-network-factory-card__meta">{facility.timezone}</p>
                  </div>
                  <Badge tone={facility.loadPercent > 90 ? "danger" : facility.loadPercent > 75 ? "warning" : "success"}>
                    {getPlanningStateLabel(facility.planningState)}
                  </Badge>
                </div>
                <p>Yük: {formatNetworkPercent(facility.loadPercent)}</p>
                <p>Boş kapasite: {formatNetworkPercent(facility.availableCapacityPercent)}</p>
                <p>Riskli sipariş: {facility.ordersAtRisk}</p>
                <p>Aktif vaka: {facility.activeCases}</p>
              </article>
            ))}
          </div>

          <div className="commint-two-column">
            <Card>
              <h3>Ağ dayanıklılığı</h3>
              <p>Kapasite yedekliliği: {workspace.resilience.capacityRedundancyScore.toFixed(1)}</p>
              <p>Proses yedekliliği: {workspace.resilience.processRedundancyScore.toFixed(1)}</p>
              <p>Kalıp yedekliliği: {workspace.resilience.moldRedundancyScore.toFixed(1)}</p>
            </Card>
            <Card>
              <h3>Politika</h3>
              <p>Otonomi: {getNetworkAutonomyModeLabel(workspace.policy.autonomyMode)}</p>
              <p>Tesis sayısı: {workspace.facilities.length}</p>
            </Card>
          </div>
        </section>
      ) : null}

      {activeTab === "capacity" ? (
        <section className="factory-network-workspace__section" data-testid="factory-network-capacity">
          {workspace.capacityMap.length === 0 ? (
            <p>Kapasite haritası verisi yok.</p>
          ) : (
            workspace.capacityMap.map((row) => (
              <Card key={row.facilityId}>
                <div className="factory-network-capacity-bar">
                  <div className="factory-network-capacity-bar__label">
                    <strong>{row.facilityName}</strong>
                    <span>
                      {row.realizable ? "Gerçekleştirilebilir" : "Kısıtlı"} · Boş{" "}
                      {formatNetworkPercent(row.unusedPercent)}
                    </span>
                  </div>
                  <div className="factory-network-capacity-bar__track">
                    <div
                      className="factory-network-capacity-bar__segment factory-network-capacity-bar__segment--committed"
                      style={{ width: `${row.committedPercent}%` }}
                      title={`Taahhüt: ${formatNetworkPercent(row.committedPercent)}`}
                    />
                    <div
                      className="factory-network-capacity-bar__segment factory-network-capacity-bar__segment--at-risk"
                      style={{ width: `${row.atRiskPercent}%` }}
                      title={`Riskli: ${formatNetworkPercent(row.atRiskPercent)}`}
                    />
                    <div
                      className="factory-network-capacity-bar__segment factory-network-capacity-bar__segment--overloaded"
                      style={{ width: `${row.overloadedPercent}%` }}
                      title={`Aşım: ${formatNetworkPercent(row.overloadedPercent)}`}
                    />
                    <div
                      className="factory-network-capacity-bar__segment factory-network-capacity-bar__segment--unused"
                      style={{ width: `${row.unusedPercent}%` }}
                      title={`Boş: ${formatNetworkPercent(row.unusedPercent)}`}
                    />
                  </div>
                  <p>
                    Kullanılabilir: {formatNetworkPercent(row.availablePercent)} · Taahhüt:{" "}
                    {formatNetworkPercent(row.committedPercent)}
                  </p>
                </div>
              </Card>
            ))
          )}
        </section>
      ) : null}

      {activeTab === "allocations" ? (
        <section className="factory-network-workspace__section" data-testid="factory-network-allocations">
          <ul className="factory-network-allocation-list">
            {workspace.activeAllocations.length === 0 ? (
              <li>Aktif tahsis bulunamadı.</li>
            ) : (
              workspace.activeAllocations.map((allocation) => (
                <li key={allocation.id}>
                  <strong>{getNetworkActionTypeLabel(allocation.actionType)}</strong>
                  <p>
                    {resolveFacilityName(workspace.facilityStates, allocation.sourceFacilityId)} →{" "}
                    {resolveFacilityName(workspace.facilityStates, allocation.destinationFacilityId)}
                  </p>
                  <p>
                    {getNetworkTriggerTypeLabel(allocation.triggerType)} ·{" "}
                    {formatNetworkDateTime(allocation.createdAtUtc)}
                  </p>
                  <Badge tone={getAllocationStatusBadgeTone(allocation.status)}>
                    {getAllocationStatusLabel(allocation.status)}
                  </Badge>
                  <p>{parseReasonSummary(allocation)}</p>
                </li>
              ))
            )}
          </ul>

          <Card>
            <h3>Talep kuyruğu</h3>
            <ul>
              {workspace.demand.length === 0 ? (
                <li>Ağ talebi yok.</li>
              ) : (
                workspace.demand.slice(0, 8).map((demand) => (
                  <li key={demand.orderId}>
                    <strong>{demand.orderId}</strong>
                    <p>
                      {resolveFacilityName(workspace.facilityStates, demand.assignedFacilityId)} · Kalan{" "}
                      {demand.remainingQuantity} · Termin {formatNetworkDateTime(demand.deadlineUtc)}
                    </p>
                    <Badge tone={getDemandRiskBadgeTone(demand.riskLevel)}>
                      {getDemandRiskLevelLabel(demand.riskLevel)}
                    </Badge>
                    <p>
                      Malzeme: {getReadinessLabel(demand.materialReady)} · Kalıp:{" "}
                      {getReadinessLabel(demand.moldReady)}
                    </p>
                  </li>
                ))
              )}
            </ul>
          </Card>
        </section>
      ) : null}

      {activeTab === "opportunities" ? (
        <section className="factory-network-workspace__section" data-testid="factory-network-opportunities">
          <ul className="factory-network-opportunity-list">
            {workspace.opportunities.length === 0 ? (
              <li>Şu anda değerlendirilen ağ fırsatı yok.</li>
            ) : (
              workspace.opportunities.map((opportunity) => (
                <li key={opportunity.opportunityCode}>
                  <strong>{opportunity.title}</strong>
                  <p>{opportunity.summary}</p>
                  <p>
                    {resolveFacilityName(workspace.facilityStates, opportunity.sourceFacilityId)} →{" "}
                    {resolveFacilityName(workspace.facilityStates, opportunity.destinationFacilityId)} · Sipariş{" "}
                    {opportunity.orderId}
                  </p>
                  <p>Beklenen fayda skoru: {opportunity.expectedBenefitScore.toFixed(1)}</p>
                </li>
              ))
            )}
          </ul>
        </section>
      ) : null}

      {activeTab === "simulator" ? (
        <section className="factory-network-workspace__section" data-testid="factory-network-simulator">
          <Card>
            <h3>Ağ tahsis simülatörü</h3>
            <div className="factory-network-simulator-form">
              <div className="factory-network-simulator-form__row">
                <label>
                  Sipariş
                  <select value={simulatorOrderId} onChange={(event) => setSimulatorOrderId(event.target.value)}>
                    {workspace.demand.map((demand) => (
                      <option key={demand.orderId} value={demand.orderId}>
                        {demand.orderId}
                      </option>
                    ))}
                  </select>
                </label>
                <label>
                  Kaynak fabrika
                  <select
                    value={simulatorSourceId}
                    onChange={(event) => setSimulatorSourceId(event.target.value)}
                  >
                    {workspace.facilityStates.map((facility) => (
                      <option key={facility.facilityId} value={facility.facilityId}>
                        {facility.facilityName}
                      </option>
                    ))}
                  </select>
                </label>
                <label>
                  Hedef fabrika
                  <select
                    value={simulatorDestinationId}
                    onChange={(event) => setSimulatorDestinationId(event.target.value)}
                  >
                    {workspace.facilityStates.map((facility) => (
                      <option key={facility.facilityId} value={facility.facilityId}>
                        {facility.facilityName}
                      </option>
                    ))}
                  </select>
                </label>
              </div>
              <div className="factory-network-simulator-form__row">
                <label>
                  Aksiyon tipi
                  <select
                    value={simulatorActionType}
                    onChange={(event) => setSimulatorActionType(event.target.value)}
                  >
                    <option value="move-full-order">Siparişi tam taşı</option>
                    <option value="move-remaining-operations">Kalan operasyonları taşı</option>
                    <option value="split-order">Siparişi böl</option>
                    <option value="temporary-capacity-shift">Geçici kapasite kaydırma</option>
                  </select>
                </label>
                <label>
                  Miktar
                  <input
                    type="number"
                    min="0"
                    value={simulatorQuantity}
                    onChange={(event) => setSimulatorQuantity(event.target.value)}
                  />
                </label>
              </div>
              <div className="factory-network-workspace__action-buttons">
                <Button
                  variant="secondary"
                  leadingIcon={<PlayCircle aria-hidden="true" size={16} />}
                  disabled={isPending || !simulatorOrderId}
                  onClick={runSimulation}
                >
                  Simüle Et
                </Button>
              </div>
            </div>
          </Card>

          <Card>
            <h3>Son senaryolar</h3>
            <ul>
              {workspace.scenarios.length === 0 ? (
                <li>Simülasyon senaryosu yok.</li>
              ) : (
                workspace.scenarios.slice(0, 5).map((scenario) => (
                  <li key={scenario.id}>
                    <strong>{scenario.title}</strong>
                    <p>
                      {scenario.scenarioCode} · {getNetworkActionTypeLabel(scenario.actionType)} · Skor{" "}
                      {scenario.score.toFixed(1)}
                    </p>
                    <Badge tone="information">{getScenarioStatusLabel(scenario.status)}</Badge>
                  </li>
                ))
              )}
            </ul>
          </Card>
        </section>
      ) : null}

      {activeTab === "decision" ? (
        <section className="factory-network-workspace__section" data-testid="factory-network-decision">
          {pendingAllocation || readyAllocation ? (
            <Card>
              <h3>Aktif karar</h3>
              {pendingAllocation ? (
                <>
                  <p>
                    <strong>{getNetworkActionTypeLabel(pendingAllocation.actionType)}</strong>
                  </p>
                  <p>{parseReasonSummary(pendingAllocation)}</p>
                  <p>Beklenen etki: {parseImpactSummary(pendingAllocation.expectedImpactJson)}</p>
                  <Badge tone={getAllocationStatusBadgeTone(pendingAllocation.status)}>
                    {getAllocationStatusLabel(pendingAllocation.status)}
                  </Badge>
                </>
              ) : null}
              {readyAllocation && !pendingAllocation ? (
                <>
                  <p>
                    <strong>{getNetworkActionTypeLabel(readyAllocation.actionType)}</strong>
                  </p>
                  <p>{parseReasonSummary(readyAllocation)}</p>
                  <Badge tone={getAllocationStatusBadgeTone(readyAllocation.status)}>
                    {getAllocationStatusLabel(readyAllocation.status)}
                  </Badge>
                </>
              ) : null}

              <div className="factory-network-workspace__action-buttons">
                <Button
                  variant="primary"
                  leadingIcon={<CheckCircle2 aria-hidden="true" size={16} />}
                  disabled={!pendingAllocation || isPending}
                  onClick={() => runApproval(true)}
                >
                  Onayla
                </Button>
                <Button
                  variant="danger"
                  leadingIcon={<XCircle aria-hidden="true" size={16} />}
                  disabled={!pendingAllocation || isPending}
                  onClick={() => runApproval(false)}
                >
                  Reddet
                </Button>
                <Button
                  variant="secondary"
                  leadingIcon={<Send aria-hidden="true" size={16} />}
                  disabled={!readyAllocation || isPending}
                  onClick={runExecution}
                >
                  Uygula
                </Button>
              </div>
            </Card>
          ) : (
            <Card>
              <h3>Aktif karar</h3>
              <p>Onay veya uygulama bekleyen tahsis bulunamadı.</p>
            </Card>
          )}

          {allocationEvaluation ? (
            <Card>
              <h3>Değerlendirme özeti</h3>
              <p>
                <strong>{getRecommendedActionLabel(allocationEvaluation.recommendedAction)}</strong>
              </p>
              <p>{allocationEvaluation.rationale}</p>
            </Card>
          ) : null}
        </section>
      ) : null}

      {activeTab === "timeline" ? (
        <section className="factory-network-workspace__section" data-testid="factory-network-timeline">
          <ul className="factory-network-activity-feed">
            {workspace.activityFeed.length === 0 ? (
              <li>Ağ aktivite kaydı yok.</li>
            ) : (
              workspace.activityFeed.map((event) => (
                <li key={event.id}>
                  <strong>{getActivityEventLabel(event.eventType)}</strong>
                  <p>{event.summary}</p>
                  <span>{formatNetworkDateTime(event.occurredAtUtc)}</span>
                </li>
              ))
            )}
          </ul>
        </section>
      ) : null}

      {activeTab === "comparison" ? (
        <section className="factory-network-workspace__section" data-testid="factory-network-comparison">
          {allocationEvaluation?.recommendationExplanation ? (
            <div className="factory-network-explanation" data-testid="factory-network-why-factory">
              <h3>Neden bu fabrika?</h3>
              <p>{allocationEvaluation.recommendationExplanation}</p>
            </div>
          ) : null}

          {allocationEvaluation && allocationEvaluation.facilityComparisons.length > 0 ? (
            <Card>
              <h3>Fabrika karşılaştırması</h3>
              <table className="factory-network-comparison-table">
                <thead>
                  <tr>
                    <th>Fabrika</th>
                    <th>Uygun</th>
                    <th>Kapasite</th>
                    <th>Termin</th>
                    <th>Kalite</th>
                    <th>Malzeme</th>
                    <th>Bakım</th>
                    <th>Risk</th>
                    <th>Güven</th>
                  </tr>
                </thead>
                <tbody>
                  {allocationEvaluation.facilityComparisons.map((row) => (
                    <tr
                      key={row.facilityId}
                      className={
                        row.facilityId === recommendedFacilityId
                          ? "factory-network-comparison-table__recommended"
                          : undefined
                      }
                    >
                      <td>
                        <strong>{row.facilityName}</strong>
                        {row.recommendationReason ? <p>{row.recommendationReason}</p> : null}
                      </td>
                      <td>{row.feasible ? "Evet" : "Hayır"}</td>
                      <td>{row.capacityScore.toFixed(1)}</td>
                      <td>{row.deadlineScore.toFixed(1)}</td>
                      <td>{row.qualityScore.toFixed(1)}</td>
                      <td>{row.materialScore.toFixed(1)}</td>
                      <td>{row.maintenanceScore.toFixed(1)}</td>
                      <td>{row.riskScore.toFixed(1)}</td>
                      <td>{row.confidenceScore.toFixed(1)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </Card>
          ) : (
            <Card>
              <h3>Fabrika karşılaştırması</h3>
              <p>Karşılaştırma verisi henüz oluşturulmadı.</p>
            </Card>
          )}
        </section>
      ) : null}

      <p className="commint-table__secondary">
        Veri tazeliği: {formatNetworkDateTime(workspace.dataFreshnessUtc)}
      </p>
    </div>
  );
}
