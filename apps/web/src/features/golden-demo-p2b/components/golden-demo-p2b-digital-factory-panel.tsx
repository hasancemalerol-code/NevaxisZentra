"use client";

import { useState } from "react";

import {
  formatGoldenDemoNumber,
  getGoldenDemoPlanActionStatusLabel,
} from "@/features/golden-demo-p1/domain/golden-demo-p1-labels";
import {
  getGoldenDemoAgentCardSummary,
  getGoldenDemoDigitalWorkerLabel,
  getGoldenDemoEvidenceSourceLabel,
  getGoldenDemoEvidenceStatusChip,
  parseGoldenDemoEvidenceAgentCode,
  translateGoldenDemoApiText,
  translateGoldenDemoFieldLabel,
  type GoldenDemoAgentPresentationContext,
} from "@/features/golden-demo-p1/domain/golden-demo-presentation-copy";

import type {
  GoldenDemoP2BAgentEvidenceApi,
  GoldenDemoP2BDigitalFactoryTeamApi,
} from "../server/golden-demo-p2b-api-client";

type GoldenDemoApprovalSheetApi = GoldenDemoP2BDigitalFactoryTeamApi["approvalSheet"];

function formatTimestamp(value?: string | null) {
  if (!value) {
    return "—";
  }

  return new Intl.DateTimeFormat("tr-TR", {
    dateStyle: "short",
    timeStyle: "medium",
  }).format(new Date(value));
}

function formatApprovalTimestamp(value?: string | null) {
  if (!value) {
    return "—";
  }

  const date = new Date(value);
  const day = date.toLocaleDateString("tr-TR", {
    day: "2-digit",
    month: "2-digit",
    year: "numeric",
  });
  const time = date.toLocaleTimeString("tr-TR", {
    hour: "2-digit",
    minute: "2-digit",
  });

  return `${day} · ${time}`;
}

function formatApprovalAuthority(role: string) {
  return translateGoldenDemoApiText(role)
    .split("/")
    .map((part) =>
      part
        .trim()
        .split(" ")
        .map((word) => word.charAt(0).toLocaleUpperCase("tr-TR") + word.slice(1))
        .join(" "),
    )
    .join(" / ");
}

function GoldenDemoApprovalDecisionCard({
  sheet,
  agents,
  pending,
  recoveryGoodParts,
  onApprove,
  onEvidenceSelect,
}: Readonly<{
  sheet: GoldenDemoApprovalSheetApi;
  agents: readonly GoldenDemoP2BAgentEvidenceApi[];
  pending: boolean;
  recoveryGoodParts: number;
  onApprove: () => void;
  onEvidenceSelect: (agent: GoldenDemoP2BAgentEvidenceApi) => void;
}>) {
  const interventionTitle =
    translateGoldenDemoApiText(sheet.recommendedIntervention, recoveryGoodParts) ||
    "Kontrollü Teknik Müdahale";
  const supportingMessage = translateGoldenDemoApiText(sheet.whyRecommended, recoveryGoodParts);
  const operationalGuardrail = translateGoldenDemoApiText(sheet.operationalRisk, recoveryGoodParts);
  const evidenceSources = Array.from(
    new Map(
      sheet.evidenceReferences
        .map((reference) => {
          const agentCode = parseGoldenDemoEvidenceAgentCode(reference);
          if (!agentCode) {
            return null;
          }

          return [agentCode, getGoldenDemoEvidenceSourceLabel(agentCode)] as const;
        })
        .filter((entry): entry is readonly [string, string] => entry !== null),
    ).entries(),
  );

  const storySteps = sheet.isApproved
    ? [
        { label: "Kanıtlar toplandı", state: "complete" as const },
        { label: "Fırsat hesaplandı", state: "complete" as const },
        { label: "Güvenli sınır", state: "complete" as const },
        { label: "Onay verildi", state: "complete" as const },
        { label: "Yürütmeye devam", state: "active" as const },
      ]
    : [
        { label: "Kanıtlar toplandı", state: "complete" as const },
        { label: "Fırsat hesaplandı", state: "complete" as const },
        { label: "Güvenli sınır", state: "complete" as const },
        { label: "Onay istendi", state: "active" as const },
        { label: "Yürütme bekliyor", state: "pending" as const },
      ];

  return (
    <section
      className={`golden-demo-approval-card${sheet.isApproved ? " golden-demo-approval-card--approved" : ""}`}
      data-testid="golden-demo-p2b-approval-sheet"
    >
      <div className="golden-demo-approval-card__header">
        <div className="golden-demo-approval-card__header-copy">
          <p className="golden-demo-approval-card__eyebrow">
            {sheet.isApproved ? "ONAY KAYDI" : "ONAY GEREKLİ"}
          </p>
          {sheet.isApproved ? (
            <span className="golden-demo-status-chip golden-demo-status-chip--approval-granted">ONAYLANDI</span>
          ) : sheet.isApprovalRequired ? (
            <span className="golden-demo-status-chip golden-demo-status-chip--approval-pending">
              İNSAN ONAYI BEKLİYOR
            </span>
          ) : null}
        </div>
        <p className="golden-demo-approval-card__boundary-note">
          Dijital Çalışan güvenli aksiyon sınırına ulaştı; yetkili onay olmadan ilerleyemez.
        </p>
      </div>

      <div className="golden-demo-approval-card__layout">
        <div className="golden-demo-approval-card__main">
          <h2 className="golden-demo-approval-card__title">{interventionTitle}</h2>
          <p className="golden-demo-approval-card__message">{supportingMessage}</p>

          <ol className="golden-demo-approval-story" aria-label="Dijital Çalışan karar akışı">
            {storySteps.map((step) => (
              <li
                key={step.label}
                className={`golden-demo-approval-story__step golden-demo-approval-story__step--${step.state}`}
              >
                <span>{step.label}</span>
              </li>
            ))}
          </ol>

          <div className="golden-demo-approval-kpi-grid">
            <article className="golden-demo-approval-kpi">
              <p className="golden-demo-approval-kpi__label">Beklenen Duruş</p>
              <p className="golden-demo-approval-kpi__value">{sheet.expectedInterruptionMinutes} dk</p>
            </article>
            <article className="golden-demo-approval-kpi">
              <p className="golden-demo-approval-kpi__label">Mevcut Üretim Maruziyeti</p>
              <p className="golden-demo-approval-kpi__value">
                {formatGoldenDemoNumber(sheet.currentProductionExposureGoodParts)} iyi parça
              </p>
            </article>
            <article className="golden-demo-approval-kpi">
              <p className="golden-demo-approval-kpi__label">Mevcut Beklenen Çıktı</p>
              <p className="golden-demo-approval-kpi__value">
                {formatGoldenDemoNumber(sheet.currentExpectedShiftGoodParts)}
              </p>
            </article>
            <article className="golden-demo-approval-kpi">
              <p className="golden-demo-approval-kpi__label">Kurtarılmış Beklenen Çıktı</p>
              <p className="golden-demo-approval-kpi__value">
                {formatGoldenDemoNumber(sheet.recoveredExpectedShiftGoodParts)}
              </p>
            </article>
            <article
              className="golden-demo-approval-kpi golden-demo-approval-kpi--emphasis"
              data-testid="golden-demo-p2b-approval-recovery"
            >
              <p className="golden-demo-approval-kpi__label">Kurtarma Fırsatı</p>
              <p className="golden-demo-approval-kpi__value">
                +{formatGoldenDemoNumber(sheet.recoveryOpportunityGoodParts)} iyi parça
              </p>
            </article>
          </div>

          <div className="golden-demo-approval-guardrail">
            <p className="golden-demo-approval-guardrail__eyebrow">Operasyonel Güvence</p>
            <p className="golden-demo-approval-guardrail__title">{interventionTitle}</p>
            <p className="golden-demo-approval-guardrail__detail">{operationalGuardrail}</p>
          </div>

          <div className="golden-demo-evidence-pack">
            <div className="golden-demo-evidence-pack__header">
              <p className="golden-demo-evidence-pack__eyebrow">Kanıt Paketi</p>
              <p className="golden-demo-evidence-pack__summary">
                {evidenceSources.length} doğrulanmış kanıt kaynağı karar paketine bağlandı.
              </p>
            </div>
            <ul className="golden-demo-evidence-pack__chips">
              {evidenceSources.map(([agentCode, label]) => {
                const agent = agents.find((item) => item.agentCode === agentCode);

                return (
                  <li key={agentCode}>
                    {agent ? (
                      <button
                        type="button"
                        className="golden-demo-evidence-chip"
                        data-testid={`golden-demo-p2b-evidence-${agentCode}`}
                        onClick={() => onEvidenceSelect(agent)}
                      >
                        <span aria-hidden="true">✓</span>
                        {label}
                      </button>
                    ) : (
                      <span className="golden-demo-evidence-chip golden-demo-evidence-chip--static">
                        <span aria-hidden="true">✓</span>
                        {label}
                      </span>
                    )}
                  </li>
                );
              })}
            </ul>
          </div>
        </div>

        <aside className="golden-demo-approval-card__aside">
          <div className="golden-demo-approval-meta">
            <div className="golden-demo-approval-meta__item">
              <p className="golden-demo-approval-meta__label">Onay Yetkisi</p>
              <p className="golden-demo-approval-meta__value">{formatApprovalAuthority(sheet.approvingRole)}</p>
            </div>
            <div className="golden-demo-approval-meta__item">
              <p className="golden-demo-approval-meta__label">Talep Eden</p>
              <p className="golden-demo-approval-meta__value">
                {translateGoldenDemoApiText(sheet.requestedByActor)}
              </p>
            </div>
            <div className="golden-demo-approval-meta__item">
              <p className="golden-demo-approval-meta__label">Talep Zamanı</p>
              <p className="golden-demo-approval-meta__value">{formatApprovalTimestamp(sheet.requestedAtUtc)}</p>
            </div>
          </div>

          {sheet.isApproved ? (
            <div className="golden-demo-approval-record" data-testid="golden-demo-p2b-approval-record">
              <p className="golden-demo-approval-record__label">Onay Durumu</p>
              <p className="golden-demo-approval-record__value">
                {translateGoldenDemoApiText(sheet.approvalStatus)}
              </p>
              <p className="golden-demo-approval-record__detail">
                Onay zamanı: {formatApprovalTimestamp(sheet.approvedAtUtc)}
              </p>
            </div>
          ) : sheet.isApprovalRequired ? (
            <div className="golden-demo-approval-actions">
              <button
                type="button"
                className="commint-button commint-button--primary"
                disabled={pending}
                data-testid="golden-demo-p2b-approve-button"
                onClick={onApprove}
              >
                {pending ? "Onaylanıyor…" : "Onayla ve Devam Et"}
              </button>
            </div>
          ) : (
            <p className="golden-demo-approval-card__message">Bu plan için onay gerekmez.</p>
          )}
        </aside>
      </div>
    </section>
  );
}

function statusChipClass(agentCode: string): string {
  switch (agentCode) {
    case "machine-performance":
      return "golden-demo-status-chip golden-demo-status-chip--detected";
    case "maintenance":
      return "golden-demo-status-chip golden-demo-status-chip--ready";
    case "energy":
      return "golden-demo-status-chip golden-demo-status-chip--measured";
    default:
      return "golden-demo-status-chip";
  }
}

function AgentEvidenceDrawer({
  agent,
  onClose,
  presentationContext,
}: Readonly<{
  agent: GoldenDemoP2BAgentEvidenceApi;
  onClose: () => void;
  presentationContext: GoldenDemoAgentPresentationContext;
}>) {
  return (
    <>
      <button type="button" className="commint-drawer-backdrop" aria-label="Kanıt detayını kapat" onClick={onClose} />
      <aside className="commint-drawer" aria-label="Dijital Çalışan kanıtı" data-testid="golden-demo-p2b-agent-evidence-drawer">
        <div className="commint-drawer__header">
          <h2>Dijital Çalışan Kanıtı</h2>
          <p>{getGoldenDemoDigitalWorkerLabel(agent.agentCode)}</p>
          <button type="button" className="commint-button" onClick={onClose}>
            Kapat
          </button>
        </div>
        <div className="commint-drawer__body">
          <section>
            <h3>İncelenen Veriler</h3>
            <ul>
              {agent.dataInputsReviewed.map((item) => (
                <li key={item}>{translateGoldenDemoApiText(item)}</li>
              ))}
            </ul>
          </section>

          <section>
            <h3>Hesaplama</h3>
            <dl>
              {Object.entries(agent.calculationInputs).map(([key, value]) => (
                <div key={`in-${key}`}>
                  <dt>{translateGoldenDemoFieldLabel(key)}</dt>
                  <dd>{value}</dd>
                </div>
              ))}
              {Object.entries(agent.calculationResults).map(([key, value]) => (
                <div key={`out-${key}`}>
                  <dt>{translateGoldenDemoFieldLabel(key)}</dt>
                  <dd>{value}</dd>
                </div>
              ))}
            </dl>
          </section>

          <section>
            <h3>Sonuç</h3>
            <p>
              {getGoldenDemoAgentCardSummary(agent.agentCode, presentationContext)}
            </p>
          </section>

          <section>
            <h3>Güven</h3>
            <p>{translateGoldenDemoApiText(agent.confidence)}</p>
          </section>

          <section>
            <h3>Kanıt Durumu</h3>
            <p>{getGoldenDemoEvidenceStatusChip(agent.agentCode, agent.evidenceClassification)}</p>
          </section>

          {agent.insufficientEvidenceState ? (
            <section data-testid="golden-demo-p2b-insufficient-evidence">
              <h3>Yetersiz Kanıt</h3>
              <p>{translateGoldenDemoApiText(agent.insufficientEvidenceState)}</p>
            </section>
          ) : null}

          <section>
            <h3>Üretilen Aksiyon</h3>
            <p>{translateGoldenDemoApiText(agent.resultingAction)}</p>
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
  presentationContext,
}: Readonly<{
  team: GoldenDemoP2BDigitalFactoryTeamApi;
  pending: boolean;
  onApprove: () => void;
  presentationContext: GoldenDemoAgentPresentationContext;
}>) {
  const [selectedAgent, setSelectedAgent] = useState<GoldenDemoP2BAgentEvidenceApi | null>(null);
  const sheet = team.approvalSheet;
  const recoveryGoodParts = presentationContext.recoveryGoodParts;

  return (
    <div className="golden-demo-digital-stack">
      <section className="commint-panel" data-testid="golden-demo-p2b-digital-factory-team">
        <h2 className="commint-panel__title">Dijital Çalışan Ekibi</h2>
        <p className="commint-panel__hint">
          Her Dijital Çalışan gerçek runtime verisini analiz etti. Detay için bir karta tıklayın.
        </p>
        <ul className="golden-demo-agent-grid">
          {team.agents.map((agent) => (
            <li key={agent.agentCode}>
              <button
                type="button"
                className="golden-demo-agent-card"
                data-testid={`golden-demo-p2b-agent-${agent.agentCode}`}
                onClick={() => setSelectedAgent(agent)}
              >
                <strong>{getGoldenDemoDigitalWorkerLabel(agent.agentCode)}</strong>
                <span>{getGoldenDemoAgentCardSummary(agent.agentCode, presentationContext)}</span>
                <span className={statusChipClass(agent.agentCode)}>
                  {agent.insufficientEvidenceState
                    ? translateGoldenDemoApiText(agent.insufficientEvidenceState)
                    : getGoldenDemoEvidenceStatusChip(agent.agentCode, agent.evidenceClassification)}
                </span>
              </button>
            </li>
          ))}
        </ul>
      </section>

      <section className="commint-panel" data-testid="golden-demo-p2b-orchestrator">
        <h2 className="commint-panel__title">FactOS Orchestrator</h2>
        <h3>Ne oluyor?</h3>
        <p>{translateGoldenDemoApiText(team.orchestrator.whatIsHappening, recoveryGoodParts)}</p>
        <h3>Neden?</h3>
        <p>{translateGoldenDemoApiText(team.orchestrator.whySummary, recoveryGoodParts)}</p>
        <h3>Üretim sonucu</h3>
        <p>Mevcut beklenen vardiya çıktısı: {team.orchestrator.currentExpectedShiftGoodParts}</p>
        <p>Kurtarılmış beklenen vardiya çıktısı: {team.orchestrator.recoveredExpectedShiftGoodParts}</p>
        <p data-testid="golden-demo-p2b-orchestrator-recovery">
          +{formatGoldenDemoNumber(team.orchestrator.productionRecoveryGoodParts)} iyi parça / vardiya
        </p>
        <h3>Sonraki adım</h3>
        <p>{translateGoldenDemoApiText(team.orchestrator.whatShouldHappenNext, recoveryGoodParts)}</p>
        <h3>Gerekli kontrol</h3>
        <p>
          {team.orchestrator.humanApprovalRequired ? "İnsan onayı gerekli" : "İnsan onayı gerekmez"} ·{" "}
          {translateGoldenDemoApiText(team.orchestrator.approvingRole)}
        </p>
        <p>{translateGoldenDemoApiText(team.orchestrator.requiredControlReason, recoveryGoodParts)}</p>
        <h3>Beklenen operasyonel sonuç</h3>
        <p>{translateGoldenDemoApiText(team.orchestrator.expectedOperationalOutcome, recoveryGoodParts)}</p>
        <h3>Kullanılan girdiler</h3>
        <ul>
          {team.orchestrator.inputsUsed.map((input) => (
            <li key={input}>{translateGoldenDemoApiText(input)}</li>
          ))}
        </ul>
        {team.orchestrator.insufficientSpecialistEvidence.length > 0 ? (
          <>
            <h3>Yetersiz uzman kanıtı</h3>
            <ul data-testid="golden-demo-p2b-orchestrator-insufficient">
              {team.orchestrator.insufficientSpecialistEvidence.map((item) => (
                <li key={item}>{translateGoldenDemoApiText(item, recoveryGoodParts)}</li>
              ))}
            </ul>
          </>
        ) : null}
      </section>

      <GoldenDemoApprovalDecisionCard
        sheet={sheet}
        agents={team.agents}
        pending={pending}
        recoveryGoodParts={recoveryGoodParts}
        onApprove={onApprove}
        onEvidenceSelect={setSelectedAgent}
      />

      <section className="commint-panel" data-testid="golden-demo-p2b-plan-traceability">
        <h2 className="commint-panel__title">Kurtarma Planı → Operasyonel İş İzlenebilirliği</h2>
        <p className="commint-panel__hint">
          FactOS yalnızca önermedi; operasyonel iş oluşturdu.
        </p>
        <ul>
          {team.planActions.map((action) => (
            <li key={action.actionId}>
              <strong>
                {action.sortOrder}. {action.title}
              </strong>{" "}
              · {translateGoldenDemoApiText(action.responsibleRole)} ·{" "}
              {getGoldenDemoPlanActionStatusLabel(action.status)}
              <p>Kanıt gereksinimi: {translateGoldenDemoApiText(action.evidenceRequired)}</p>
              <p>Doğrulama: {translateGoldenDemoApiText(action.validationCriteria)}</p>
              <p>
                Kaynak: {getGoldenDemoDigitalWorkerLabel(action.sourceAgentCode)} ({action.sourceAgentCode})
              </p>
              <p>Teknik bulgu: {translateGoldenDemoApiText(action.relatedTechnicalFinding)}</p>
              <p>
                Plan {action.planId} · İş {action.workReferenceId}
                {action.completedAtUtc ? ` · Tamamlandı ${formatTimestamp(action.completedAtUtc)}` : ""}
              </p>
            </li>
          ))}
        </ul>
      </section>

      <section className="commint-panel" data-testid="golden-demo-p2b-activity-timeline">
        <h2 className="commint-panel__title">Dijital Çalışan Aktivite Zaman Çizelgesi</h2>
        <ul>
          {team.activityTimeline.map((item) => (
            <li key={`${item.timestampUtc}-${item.action}-${item.sourceReference}`}>
              <strong>{formatTimestamp(item.timestampUtc)}</strong> ·{" "}
              {translateGoldenDemoApiText(item.actor)} · {translateGoldenDemoApiText(item.action)} ·{" "}
              {translateGoldenDemoApiText(item.status)}
              <p className="commint-table__secondary">
                {item.sourceReference} · {translateGoldenDemoApiText(item.evidenceType)}
                {item.resultSummary ? ` · ${translateGoldenDemoApiText(item.resultSummary, recoveryGoodParts)}` : ""}
              </p>
            </li>
          ))}
        </ul>
      </section>

      <section className="commint-panel" data-testid="golden-demo-p2b-intervention-timeline">
        <h2 className="commint-panel__title">Müdahale Yürütme Zaman Çizelgesi</h2>
        <ul>
          {team.interventionTimeline.map((stage) => (
            <li key={stage.stageCode} data-status={stage.status}>
              <strong>{translateGoldenDemoApiText(stage.stageLabel)}</strong> ·{" "}
              {translateGoldenDemoApiText(stage.status)} · {translateGoldenDemoApiText(stage.actor)}
              {stage.occurredAtUtc ? ` · ${formatTimestamp(stage.occurredAtUtc)}` : ""}
            </li>
          ))}
        </ul>
        <p className="commint-panel__hint">
          FactOS teknik müdahaleyi koordine etti ve kurtarma işini izledi. Yetkili operatör/bakım rolü fiziksel
          müdahaleyi uyguladı.
        </p>
      </section>

      {selectedAgent ? (
        <AgentEvidenceDrawer
          agent={selectedAgent}
          presentationContext={presentationContext}
          onClose={() => setSelectedAgent(null)}
        />
      ) : null}
    </div>
  );
}
