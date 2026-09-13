"use client";

import "./adaptive-planning.css";

import { useMemo, useState, useTransition } from "react";
import { CheckCircle2, PlayCircle, XCircle } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";

import {
  formatAdaptiveDateTime,
  formatAdaptiveDurationMinutes,
  formatAdaptivePercent,
  getActivityEventLabel,
  getAdaptiveLifecycleBadgeTone,
  getAdaptiveLifecycleStatusLabel,
  getAutonomyModeLabel,
  getChangeTypeLabel,
  getDeadlineRiskLevelLabel,
  getDeviationSeverityLabel,
  getDeviationTypeLabel,
  getMaterialReadinessLabel,
  getOperationStatusLabel,
  getPlanningHorizonLabel,
  getReplanActionLabel,
  getScenarioStatusLabel,
  getTriggerTypeLabel,
} from "../domain/adaptive-planning-labels";
import {
  approveAdaptiveReplanAction,
  simulateAdaptiveReplanAction,
} from "../server/adaptive-planning-actions";
import type {
  AdaptivePlanningChangeApi,
  AdaptivePlanningOperationApi,
  AdaptivePlanningWorkspaceApi,
  ReplanEvaluationResultApi,
} from "../server/adaptive-planning-api-client";

const WORKSPACE_TABS = [
  { id: "summary", label: "Planlama Özeti" },
  { id: "live", label: "Canlı Plan" },
  { id: "scenarios", label: "Senaryolar" },
  { id: "changes", label: "Plan Değişiklikleri" },
  { id: "history", label: "Plan Geçmişi" },
  { id: "risks", label: "Riskler" },
  { id: "timeline", label: "Gantt" },
] as const;

type WorkspaceTabId = (typeof WORKSPACE_TABS)[number]["id"];

type AdaptivePlanningWorkspaceClientProps = Readonly<{
  workspace: AdaptivePlanningWorkspaceApi;
  replanEvaluation: ReplanEvaluationResultApi | null;
}>;

type ParsedDiffRow = Readonly<{
  orderId: string;
  field: string;
  oldValue: string;
  newValue: string;
  impactSummary: string;
}>;

function parseDiffRows(change: AdaptivePlanningChangeApi | undefined): readonly ParsedDiffRow[] {
  if (!change?.diffJson) {
    return [];
  }

  try {
    const parsed = JSON.parse(change.diffJson) as unknown;
    if (Array.isArray(parsed)) {
      return parsed
        .map((row) => {
          if (typeof row !== "object" || row === null) {
            return null;
          }
          const item = row as Record<string, unknown>;
          return {
            orderId: String(item.orderId ?? "—"),
            field: String(item.field ?? "Alan"),
            oldValue: String(item.oldValue ?? "—"),
            newValue: String(item.newValue ?? "—"),
            impactSummary: String(item.impactSummary ?? ""),
          };
        })
        .filter((row): row is ParsedDiffRow => row !== null);
    }
  } catch {
    return [];
  }

  return [];
}

function buildGanttRows(operations: readonly AdaptivePlanningOperationApi[]) {
  if (operations.length === 0) {
    return { rows: [], windowStart: 0, windowEnd: 0 };
  }

  const timestamps = operations.flatMap((op) => [
    new Date(op.plannedStartUtc).getTime(),
    new Date(op.plannedEndUtc).getTime(),
  ]);
  const windowStart = Math.min(...timestamps);
  const windowEnd = Math.max(...timestamps);
  const span = Math.max(windowEnd - windowStart, 1);

  const machineIds = [...new Set(operations.map((op) => op.machineId ?? "Atanmamış"))];
  const rows = machineIds.map((machineId) => ({
    machineId,
    bars: operations
      .filter((op) => (op.machineId ?? "Atanmamış") === machineId)
      .map((op) => {
        const start = new Date(op.plannedStartUtc).getTime();
        const end = new Date(op.plannedEndUtc).getTime();
        return {
          id: op.id,
          orderId: op.orderId,
          leftPercent: ((start - windowStart) / span) * 100,
          widthPercent: Math.max(((end - start) / span) * 100, 2),
          status: op.status,
        };
      }),
  }));

  return { rows, windowStart, windowEnd };
}

export function AdaptivePlanningWorkspaceClient({
  workspace,
  replanEvaluation,
}: AdaptivePlanningWorkspaceClientProps) {
  const [activeTab, setActiveTab] = useState<WorkspaceTabId>("summary");
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isPending, startTransition] = useTransition();

  const latestChange = workspace.recentChanges[0];
  const diffRows = useMemo(() => parseDiffRows(latestChange), [latestChange]);
  const gantt = useMemo(() => buildGanttRows(workspace.operations), [workspace.operations]);

  const canApprove =
    workspace.activeVersion?.lifecycleStatus === "approval-pending" && workspace.activeVersion;

  function runApproval(approved: boolean) {
    if (!workspace.activeVersion) {
      return;
    }

    startTransition(async () => {
      setMessage(null);
      setError(null);
      const result = await approveAdaptiveReplanAction({
        planVersionId: workspace.activeVersion!.id,
        concurrencyToken: workspace.activeVersion!.concurrencyToken,
        approved,
      });
      if (result.status === "failure") {
        setError(result.message);
        return;
      }
      setMessage(result.message);
    });
  }

  function runSimulation() {
    if (!workspace.activePlan) {
      return;
    }

    const triggerType = workspace.deviations[0]?.deviationType ?? "scheduled-review";
    startTransition(async () => {
      setMessage(null);
      setError(null);
      const result = await simulateAdaptiveReplanAction({
        productionPlanId: workspace.activePlan!.id,
        triggerType,
        triggerContextJson: "{}",
        maxAlternatives: 3,
      });
      if (result.status === "failure") {
        setError(result.message);
        return;
      }
      setMessage(result.message);
    });
  }

  return (
    <div className="adaptive-planning-workspace" data-testid="adaptive-planning-workspace-client">
      <div className="adaptive-planning-workspace__tabs" role="tablist" aria-label="Planlama merkezi görünümleri">
        {WORKSPACE_TABS.map((tab) => (
          <button
            key={tab.id}
            type="button"
            role="tab"
            aria-selected={activeTab === tab.id}
            className={`adaptive-planning-workspace__tab${activeTab === tab.id ? " adaptive-planning-workspace__tab--active" : ""}`}
            onClick={() => setActiveTab(tab.id)}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {message ? <p className="adaptive-planning-workspace__message" role="status">{message}</p> : null}
      {error ? <p className="adaptive-planning-workspace__error" role="alert">{error}</p> : null}

      {activeTab === "summary" ? (
        <section className="adaptive-planning-workspace__section" data-testid="adaptive-planning-summary">
          <div className="commint-kpi-grid">
            <Card>
              <span>Plan uyumu</span>
              <strong>{formatAdaptivePercent(workspace.metrics.planAdherencePercent)}</strong>
            </Card>
            <Card>
              <span>Program uyumu</span>
              <strong>{formatAdaptivePercent(workspace.metrics.scheduleAdherencePercent)}</strong>
            </Card>
            <Card>
              <span>Riskli sipariş</span>
              <strong>{workspace.metrics.ordersAtRisk}</strong>
            </Card>
            <Card>
              <span>Bugün replan</span>
              <strong>{workspace.metrics.replansToday}</strong>
            </Card>
            <Card>
              <span>Planlı kullanım</span>
              <strong>{formatAdaptivePercent(workspace.metrics.plannedUtilizationPercent)}</strong>
            </Card>
            <Card>
              <span>Gerçek kullanım</span>
              <strong>{formatAdaptivePercent(workspace.metrics.actualUtilizationPercent)}</strong>
            </Card>
          </div>

          <div className="commint-two-column">
            <Card>
              <h3>Aktif plan</h3>
              {workspace.activePlan ? (
                <>
                  <p>
                    <strong>{workspace.activePlan.title}</strong>
                  </p>
                  <p>{workspace.activePlan.referenceCode}</p>
                  <Badge tone={getAdaptiveLifecycleBadgeTone(workspace.activePlan.adaptiveLifecycleStatus)}>
                    {getAdaptiveLifecycleStatusLabel(workspace.activePlan.adaptiveLifecycleStatus)}
                  </Badge>
                </>
              ) : (
                <p>Aktif plan bulunamadı.</p>
              )}
            </Card>

            <Card>
              <h3>Politika</h3>
              <p>Otonomi: {getAutonomyModeLabel(workspace.policy.autonomyMode)}</p>
              <p>Dondurma penceresi: {workspace.policy.frozenWindowMinutes} dk</p>
              <p>Sinir eşiği: {workspace.policy.minBenefitThreshold}</p>
            </Card>
          </div>
        </section>
      ) : null}

      {activeTab === "live" ? (
        <section className="adaptive-planning-workspace__section" data-testid="adaptive-planning-live-plan">
          {workspace.activeVersion ? (
            <Card>
              <div className="adaptive-planning-workspace__version-header">
                <div>
                  <h3>
                    Sürüm {workspace.activeVersion.versionNumber} ·{" "}
                    {getPlanningHorizonLabel(workspace.activeVersion.planningHorizon)}
                  </h3>
                  <p>
                    Güven: {formatAdaptivePercent(workspace.activeVersion.confidenceScore)} · Skor:{" "}
                    {workspace.activeVersion.score.toFixed(1)}
                  </p>
                </div>
                <Badge tone={getAdaptiveLifecycleBadgeTone(workspace.activeVersion.lifecycleStatus)}>
                  {getAdaptiveLifecycleStatusLabel(workspace.activeVersion.lifecycleStatus)}
                </Badge>
              </div>
              <p>Yayın: {formatAdaptiveDateTime(workspace.activeVersion.publishedAtUtc)}</p>
            </Card>
          ) : null}

          <ul className="adaptive-planning-operation-list">
            {workspace.operations.length === 0 ? (
              <li>Planlanmış operasyon yok.</li>
            ) : (
              workspace.operations.slice(0, 12).map((operation) => (
                <li key={operation.id}>
                  <strong>{operation.orderId}</strong>
                  <p>
                    {operation.machineId ?? "Makine atanmadı"} · Sıra {operation.sequenceRank} ·{" "}
                    {getOperationStatusLabel(operation.status)}
                  </p>
                  <p>
                    {formatAdaptiveDateTime(operation.plannedStartUtc)} –{" "}
                    {formatAdaptiveDateTime(operation.plannedEndUtc)} · Setup{" "}
                    {formatAdaptiveDurationMinutes(operation.setupMinutes)} ·{" "}
                    {getMaterialReadinessLabel(operation.materialReadinessState)}
                  </p>
                </li>
              ))
            )}
          </ul>
        </section>
      ) : null}

      {activeTab === "scenarios" ? (
        <section className="adaptive-planning-workspace__section" data-testid="adaptive-planning-scenarios">
          <ul>
            {workspace.scenarios.length === 0 ? (
              <li>Değerlendirilmiş senaryo yok.</li>
            ) : (
              workspace.scenarios.map((scenario) => (
                <li key={scenario.id}>
                  <strong>{scenario.title}</strong>
                  <p>
                    {scenario.scenarioCode} · {getTriggerTypeLabel(scenario.triggerType)} · Skor{" "}
                    {scenario.score.toFixed(1)} · Sıra {scenario.rank}
                  </p>
                  <Badge tone="information">{getScenarioStatusLabel(scenario.status)}</Badge>
                </li>
              ))
            )}
          </ul>
        </section>
      ) : null}

      {activeTab === "changes" ? (
        <section className="adaptive-planning-workspace__section" data-testid="adaptive-planning-changes">
          <ul>
            {workspace.recentChanges.length === 0 ? (
              <li>Plan değişikliği kaydı yok.</li>
            ) : (
              workspace.recentChanges.map((change) => (
                <li key={change.id}>
                  <strong>{getChangeTypeLabel(change.changeType)}</strong>
                  <p>
                    {getTriggerTypeLabel(change.triggerType)} · {formatAdaptiveDateTime(change.createdAtUtc)}
                  </p>
                </li>
              ))
            )}
          </ul>

          <Card>
            <h3>Plan farkı</h3>
            {diffRows.length === 0 ? (
              <p>Son değişiklik için yapılandırılmış fark satırı yok.</p>
            ) : (
              <table className="adaptive-planning-diff-table">
                <thead>
                  <tr>
                    <th>Sipariş</th>
                    <th>Alan</th>
                    <th>Eski</th>
                    <th>Yeni</th>
                    <th>Etki</th>
                  </tr>
                </thead>
                <tbody>
                  {diffRows.map((row, index) => (
                    <tr key={`${row.orderId}-${row.field}-${index}`}>
                      <td>{row.orderId}</td>
                      <td>{row.field}</td>
                      <td>{row.oldValue}</td>
                      <td>{row.newValue}</td>
                      <td>{row.impactSummary || "—"}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </Card>
        </section>
      ) : null}

      {activeTab === "history" ? (
        <section className="adaptive-planning-workspace__section" data-testid="adaptive-planning-history">
          <ul className="adaptive-planning-activity-feed">
            {workspace.activityFeed.length === 0 ? (
              <li>Aktivite kaydı yok.</li>
            ) : (
              workspace.activityFeed.map((event, index) => (
                <li key={`${event.eventType}-${event.occurredAtUtc}-${index}`}>
                  <strong>{getActivityEventLabel(event.eventType)}</strong>
                  <p>{event.summary}</p>
                  <span>{formatAdaptiveDateTime(event.occurredAtUtc)}</span>
                </li>
              ))
            )}
          </ul>
        </section>
      ) : null}

      {activeTab === "risks" ? (
        <section className="adaptive-planning-workspace__section" data-testid="adaptive-planning-risks">
          <div className="commint-two-column">
            <Card>
              <h3>Termin riskleri</h3>
              <ul>
                {workspace.risks.deadlineRisks.length === 0 ? (
                  <li>Termin riski yok.</li>
                ) : (
                  workspace.risks.deadlineRisks.map((risk) => (
                    <li key={risk.orderId}>
                      <strong>{risk.orderId}</strong>
                      <p>
                        {getDeadlineRiskLevelLabel(risk.riskLevel)} · Termin{" "}
                        {formatAdaptiveDateTime(risk.deadlineUtc)}
                      </p>
                    </li>
                  ))
                )}
              </ul>
            </Card>

            <Card>
              <h3>Sapma uyarıları</h3>
              <ul>
                {workspace.deviations.length === 0 ? (
                  <li>Aktif sapma yok.</li>
                ) : (
                  workspace.deviations.map((deviation) => (
                    <li key={deviation.id}>
                      <strong>{getDeviationTypeLabel(deviation.deviationType)}</strong>
                      <p>{deviation.summary}</p>
                      <Badge tone={deviation.severity === "critical" ? "danger" : "warning"}>
                        {getDeviationSeverityLabel(deviation.severity)}
                      </Badge>
                    </li>
                  ))
                )}
              </ul>
            </Card>
          </div>

          <Card>
            <h3>Kapasite ve kaynak riskleri</h3>
            <ul>
              {[
                ...workspace.risks.capacityOverloads.map((item) => `Kapasite: ${item}`),
                ...workspace.risks.machineRisks.map((item) => `Makine: ${item}`),
                ...workspace.risks.materialRisks.map((item) => `Malzeme: ${item}`),
                ...workspace.risks.moldRisks.map((item) => `Kalıp: ${item}`),
                ...workspace.risks.qualityRisks.map((item) => `Kalite: ${item}`),
                ...workspace.risks.maintenanceRisks.map((item) => `Bakım: ${item}`),
              ].map((item) => (
                <li key={item}>{item}</li>
              ))}
            </ul>
          </Card>
        </section>
      ) : null}

      {activeTab === "timeline" ? (
        <section className="adaptive-planning-workspace__section" data-testid="adaptive-planning-gantt">
          {gantt.rows.length === 0 ? (
            <p>Gantt için operasyon verisi yok.</p>
          ) : (
            <div className="adaptive-planning-gantt">
              <div className="adaptive-planning-gantt__header">
                <span>
                  {formatAdaptiveDateTime(new Date(gantt.windowStart).toISOString())} –{" "}
                  {formatAdaptiveDateTime(new Date(gantt.windowEnd).toISOString())}
                </span>
              </div>
              {gantt.rows.map((row) => (
                <div key={row.machineId} className="adaptive-planning-gantt__row">
                  <div className="adaptive-planning-gantt__label">{row.machineId}</div>
                  <div className="adaptive-planning-gantt__track">
                    {row.bars.map((bar) => (
                      <div
                        key={bar.id}
                        className="adaptive-planning-gantt__bar"
                        style={{
                          left: `${bar.leftPercent}%`,
                          width: `${bar.widthPercent}%`,
                        }}
                        title={`${bar.orderId} · ${getOperationStatusLabel(bar.status)}`}
                      >
                        <span>{bar.orderId}</span>
                      </div>
                    ))}
                  </div>
                </div>
              ))}
            </div>
          )}
        </section>
      ) : null}

      <section className="adaptive-planning-workspace__section adaptive-planning-workspace__actions" data-testid="adaptive-planning-replan">
        <Card>
          <h3>Replan değerlendirmesi</h3>
          {replanEvaluation ? (
            <>
              <p>
                <strong>{getReplanActionLabel(replanEvaluation.recommendedAction)}</strong>
              </p>
              <p>{replanEvaluation.rationale}</p>
              {replanEvaluation.nervousnessBlocked ? (
                <Badge tone="warning">Sinirlilik koruması aktif</Badge>
              ) : null}
              {replanEvaluation.frozenWindowBlocked ? (
                <Badge tone="warning">Dondurma penceresi engeli</Badge>
              ) : null}
            </>
          ) : (
            <p>Replan değerlendirmesi henüz oluşturulmadı.</p>
          )}

          <div className="adaptive-planning-workspace__action-buttons">
            <Button
              variant="primary"
              leadingIcon={<CheckCircle2 aria-hidden="true" size={16} />}
              disabled={!canApprove || isPending}
              onClick={() => runApproval(true)}
            >
              Onayla
            </Button>
            <Button
              variant="danger"
              leadingIcon={<XCircle aria-hidden="true" size={16} />}
              disabled={!canApprove || isPending}
              onClick={() => runApproval(false)}
            >
              Reddet
            </Button>
            <Button
              variant="secondary"
              leadingIcon={<PlayCircle aria-hidden="true" size={16} />}
              disabled={!workspace.activePlan || isPending}
              onClick={runSimulation}
            >
              Simüle Et
            </Button>
          </div>
        </Card>
      </section>

      <p className="commint-table__secondary">
        Veri tazeliği: {formatAdaptiveDateTime(workspace.dataFreshnessUtc)}
      </p>
    </div>
  );
}
