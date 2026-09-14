"use client";

import { useState } from "react";

import { Badge } from "@/components/ui/badge";
import { formatGoldenDemoNumber } from "@/features/golden-demo-p1/domain/golden-demo-p1-labels";

import type {
  GoldenDemoP2BAgentEvidenceApi,
  GoldenDemoP2BDigitalFactoryTeamApi,
} from "../server/golden-demo-p2b-api-client";

function formatTimestamp(value?: string | null) {
  if (!value) {
    return "—";
  }

  return new Intl.DateTimeFormat("tr-TR", {
    dateStyle: "short",
    timeStyle: "medium",
  }).format(new Date(value));
}

function AgentEvidenceDrawer({
  agent,
  onClose,
}: Readonly<{
  agent: GoldenDemoP2BAgentEvidenceApi;
  onClose: () => void;
}>) {
  return (
    <>
      <button type="button" className="commint-drawer-backdrop" aria-label="Kanıt detayını kapat" onClick={onClose} />
      <aside className="commint-drawer" aria-label="Ajan kanıtı" data-testid="golden-demo-p2b-agent-evidence-drawer">
        <div className="commint-drawer__header">
          <h2>Agent Evidence</h2>
          <p>{agent.agentLabel}</p>
          <button type="button" onClick={onClose}>
            Kapat
          </button>
        </div>
        <div className="commint-drawer__body">
          <section>
            <h3>Data Reviewed</h3>
            <ul>
              {agent.dataInputsReviewed.map((item) => (
                <li key={item}>{item}</li>
              ))}
            </ul>
          </section>

          <section>
            <h3>Calculation</h3>
            <dl>
              {Object.entries(agent.calculationInputs).map(([key, value]) => (
                <div key={`in-${key}`}>
                  <dt>{key}</dt>
                  <dd>{value}</dd>
                </div>
              ))}
              {Object.entries(agent.calculationResults).map(([key, value]) => (
                <div key={`out-${key}`}>
                  <dt>{key}</dt>
                  <dd>{value}</dd>
                </div>
              ))}
            </dl>
          </section>

          <section>
            <h3>Conclusion</h3>
            <p>{agent.conclusion || agent.summary}</p>
          </section>

          <section>
            <h3>Confidence</h3>
            <p>{agent.confidence}</p>
          </section>

          <section>
            <h3>Evidence Classification</h3>
            <p>{agent.evidenceClassification}</p>
          </section>

          {agent.insufficientEvidenceState ? (
            <section data-testid="golden-demo-p2b-insufficient-evidence">
              <h3>Insufficient Evidence</h3>
              <p>{agent.insufficientEvidenceState}</p>
            </section>
          ) : null}

          <section>
            <h3>Resulting Action</h3>
            <p>{agent.resultingAction}</p>
          </section>

          <p className="commint-table__secondary">
            {agent.machineCode} · {formatTimestamp(agent.generatedAtUtc)}
          </p>
        </div>
      </aside>
    </>
  );
}

export function GoldenDemoP2BDigitalFactoryPanel({
  team,
  pending,
  onApprove,
}: Readonly<{
  team: GoldenDemoP2BDigitalFactoryTeamApi;
  pending: boolean;
  onApprove: () => void;
}>) {
  const [selectedAgent, setSelectedAgent] = useState<GoldenDemoP2BAgentEvidenceApi | null>(null);
  const sheet = team.approvalSheet;

  return (
    <>
      <section className="commint-panel" data-testid="golden-demo-p2b-digital-factory-team">
        <h2>Dijital Fabrika Ekibi</h2>
        <p className="commint-table__secondary">
          Her uzman gerçek runtime verisini analiz etti. Detay için bir ajana tıklayın.
        </p>
        <ul className="commint-action-row">
          {team.agents.map((agent) => (
            <li key={agent.agentCode}>
              <button
                type="button"
                data-testid={`golden-demo-p2b-agent-${agent.agentCode}`}
                onClick={() => setSelectedAgent(agent)}
              >
                <strong>{agent.agentLabel}</strong>
                <span>{agent.summary}</span>
                {agent.insufficientEvidenceState ? (
                  <Badge>{agent.insufficientEvidenceState}</Badge>
                ) : (
                  <Badge>{agent.evidenceClassification}</Badge>
                )}
              </button>
            </li>
          ))}
        </ul>
      </section>

      <section className="commint-panel" data-testid="golden-demo-p2b-activity-timeline">
        <h2>Digital Factory Team Activity</h2>
        <ul>
          {team.activityTimeline.map((item) => (
            <li key={`${item.timestampUtc}-${item.action}-${item.sourceReference}`}>
              <strong>{formatTimestamp(item.timestampUtc)}</strong> · {item.actor} · {item.action} · {item.status}
              <p className="commint-table__secondary">
                {item.sourceReference} · {item.evidenceType}
                {item.resultSummary ? ` · ${item.resultSummary}` : ""}
              </p>
            </li>
          ))}
        </ul>
      </section>

      <section className="commint-panel" data-testid="golden-demo-p2b-orchestrator">
        <h1>FactOS Orchestrator</h1>
        <h2>What is happening?</h2>
        <p>{team.orchestrator.whatIsHappening}</p>
        <h2>Why?</h2>
        <p>{team.orchestrator.whySummary}</p>
        <h2>Production consequence</h2>
        <p>Mevcut beklenen vardiya çıktısı: {team.orchestrator.currentExpectedShiftGoodParts}</p>
        <p>Kurtarılmış beklenen vardiya çıktısı: {team.orchestrator.recoveredExpectedShiftGoodParts}</p>
        <p data-testid="golden-demo-p2b-orchestrator-recovery">
          +{formatGoldenDemoNumber(team.orchestrator.productionRecoveryGoodParts)} good parts / shift
        </p>
        <h2>What should happen next?</h2>
        <p>{team.orchestrator.whatShouldHappenNext}</p>
        <h2>Required control</h2>
        <p>
          {team.orchestrator.humanApprovalRequired ? "Human approval required" : "Human approval not required"} ·{" "}
          {team.orchestrator.approvingRole}
        </p>
        <p>{team.orchestrator.requiredControlReason}</p>
        <h2>Expected operational outcome</h2>
        <p>{team.orchestrator.expectedOperationalOutcome}</p>
        <h3>Inputs used</h3>
        <ul>
          {team.orchestrator.inputsUsed.map((input) => (
            <li key={input}>{input}</li>
          ))}
        </ul>
        {team.orchestrator.insufficientSpecialistEvidence.length > 0 ? (
          <>
            <h3>Insufficient specialist evidence</h3>
            <ul data-testid="golden-demo-p2b-orchestrator-insufficient">
              {team.orchestrator.insufficientSpecialistEvidence.map((item) => (
                <li key={item}>{item}</li>
              ))}
            </ul>
          </>
        ) : null}
      </section>

      <section className="commint-panel" data-testid="golden-demo-p2b-approval-sheet">
        <h1>{sheet.isApproved ? "Approval Record" : "Approval Required"}</h1>
        <p>
          <strong>{sheet.recommendedIntervention}</strong>
        </p>
        <p>{sheet.whyRecommended}</p>
        <dl>
          <div>
            <dt>Expected interruption duration</dt>
            <dd>{sheet.expectedInterruptionMinutes} dk</dd>
          </div>
          <div>
            <dt>Current production exposure</dt>
            <dd>{sheet.currentProductionExposureGoodParts} iyi parça</dd>
          </div>
          <div>
            <dt>Current expected output</dt>
            <dd>{sheet.currentExpectedShiftGoodParts}</dd>
          </div>
          <div>
            <dt>Recovered expected output</dt>
            <dd>{sheet.recoveredExpectedShiftGoodParts}</dd>
          </div>
          <div>
            <dt>Recovery opportunity</dt>
            <dd>+{sheet.recoveryOpportunityGoodParts} iyi parça</dd>
          </div>
          <div>
            <dt>Operational risk</dt>
            <dd>{sheet.operationalRisk}</dd>
          </div>
          <div>
            <dt>Approving role</dt>
            <dd>{sheet.approvingRole}</dd>
          </div>
          <div>
            <dt>Requested at</dt>
            <dd>{formatTimestamp(sheet.requestedAtUtc)}</dd>
          </div>
          <div>
            <dt>Requested by</dt>
            <dd>{sheet.requestedByActor}</dd>
          </div>
        </dl>
        <h3>Evidence references</h3>
        <ul>
          {sheet.evidenceReferences.map((reference) => (
            <li key={reference}>{reference}</li>
          ))}
        </ul>
        {sheet.isApproved ? (
          <div data-testid="golden-demo-p2b-approval-record">
            <p>Approved by: {sheet.approvedBySubjectId}</p>
            <p>Approved at: {formatTimestamp(sheet.approvedAtUtc)}</p>
            <p>Approval status: {sheet.approvalStatus}</p>
            <p>Audit reference: {sheet.auditReference}</p>
          </div>
        ) : sheet.isApprovalRequired ? (
          <button type="button" disabled={pending} data-testid="golden-demo-p2b-approve-button" onClick={onApprove}>
            APPROVE
          </button>
        ) : (
          <p>Approval not required for this plan.</p>
        )}
      </section>

      <section className="commint-panel" data-testid="golden-demo-p2b-plan-traceability">
        <h2>Recovery Plan → Real Work Traceability</h2>
        <p className="commint-table__secondary">
          FactOS did not only recommend this. It created operational work.
        </p>
        <ul>
          {team.planActions.map((action) => (
            <li key={action.actionId}>
              <strong>
                {action.sortOrder}. {action.title}
              </strong>{" "}
              · {action.responsibleRole} · {action.status}
              <p>Evidence required: {action.evidenceRequired}</p>
              <p>Validation: {action.validationCriteria}</p>
              <p>
                Source: {action.sourceAgentLabel} ({action.sourceAgentCode})
              </p>
              <p>Technical finding: {action.relatedTechnicalFinding}</p>
              <p>
                Plan {action.planId} · Work {action.workReferenceId}
                {action.completedAtUtc ? ` · Completed ${formatTimestamp(action.completedAtUtc)}` : ""}
              </p>
            </li>
          ))}
        </ul>
      </section>

      <section className="commint-panel" data-testid="golden-demo-p2b-intervention-timeline">
        <h2>Intervention Execution Timeline</h2>
        <ul>
          {team.interventionTimeline.map((stage) => (
            <li key={stage.stageCode} data-status={stage.status}>
              <strong>{stage.stageLabel}</strong> · {stage.status} · {stage.actor}
              {stage.occurredAtUtc ? ` · ${formatTimestamp(stage.occurredAtUtc)}` : ""}
            </li>
          ))}
        </ul>
        <p className="commint-table__secondary">
          FactOS coordinated the technical intervention and tracked recovery work. Authorized operator/maintenance role
          executed the physical intervention.
        </p>
      </section>

      {selectedAgent ? (
        <AgentEvidenceDrawer agent={selectedAgent} onClose={() => setSelectedAgent(null)} />
      ) : null}
    </>
  );
}
