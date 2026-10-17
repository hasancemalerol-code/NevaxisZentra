"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState, useTransition } from "react";
import { ArrowUpRight } from "lucide-react";

import "@/features/commercial-intelligence/components/commercial-intelligence-workspace.css";
import "./presentation-mode.css";

import {
  FACTORY_DECISION_CENTER_ROUTE,
  getPresentationStatusTone,
  PRESENTATION_MODE_SUBTITLE,
  PRESENTATION_MODE_TITLE,
  PRESENTATION_SCENARIOS,
  type PresentationScenarioDefinition,
} from "../domain/presentation-mode-labels";
import type { PresentationModeDashboard, PresentationScenarioState } from "../server/presentation-mode-reader";
import { presentationModeResetScenarioAction } from "../server/presentation-mode-actions";

type PresentationModeViewProps = Readonly<{
  dashboard: PresentationModeDashboard;
}>;

function statusClassName(status: PresentationScenarioState["status"]): string {
  const tone = getPresentationStatusTone(status);
  return `presentation-mode-scenario-card__status presentation-mode-scenario-card__status--${tone}`;
}

function ScenarioCard({
  definition,
  state,
}: Readonly<{
  definition: PresentationScenarioDefinition;
  state: PresentationScenarioState;
}>) {
  const router = useRouter();
  const [message, setMessage] = useState<string | null>(null);
  const [pending, startTransition] = useTransition();

  function resetScenario() {
    const confirmed = window.confirm(`${definition.title} senaryosunu sıfırlamak istiyor musunuz?`);
    if (!confirmed) {
      return;
    }

    setMessage(null);
    startTransition(async () => {
      const result = await presentationModeResetScenarioAction(
        definition.key,
        state.resetTargetId,
      );
      if (result.status === "success") {
        setMessage("Senaryo hazır duruma getirildi.");
        router.refresh();
      } else {
        setMessage(result.message ?? "Sıfırlama başarısız.");
      }
    });
  }

  const demoHref =
    definition.key === "critical-order-recovery" && state.caseId
      ? `${FACTORY_DECISION_CENTER_ROUTE}/vakalar/${state.caseId}`
      : definition.demoHref;

  return (
    <article
      className="presentation-mode-scenario-card"
      data-testid={`presentation-scenario-${definition.key}`}
    >
      <div className="presentation-mode-scenario-card__header">
        <div>
          <span className="presentation-mode-scenario-card__outcome">{definition.outcomeTag}</span>
          <h2 style={{ marginTop: "0.55rem" }}>{definition.title}</h2>
        </div>
        <span className={statusClassName(state.status)} data-testid={`presentation-status-${definition.key}`}>
          {state.status}
        </span>
      </div>

      <p className="commint-executive__subtitle">{definition.description}</p>

      <div className="presentation-mode-narrative" aria-label={`${definition.title} akışı`}>
        {definition.narrativeSteps.map((step, index) => (
          <span key={step}>
            {index > 0 ? <span className="presentation-mode-narrative__arrow">→</span> : null}
            {step}
          </span>
        ))}
      </div>

      <div className="presentation-mode-scenario-card__capabilities">
        {definition.capabilities.map((capability) => (
          <span key={capability} className="presentation-mode-scenario-card__capability">
            {capability}
          </span>
        ))}
      </div>

      <div className="presentation-mode-scenario-card__actions">
        <Link href={demoHref} className="commint-button commint-button--primary">
          {definition.key === "critical-order-recovery" ? "Senaryoyu Başlat" : "Demoyu Başlat"}
        </Link>
        {definition.factoryDecisionHref ? (
          <Link href={definition.factoryDecisionHref} className="commint-button">
            Fabrika Karar Merkezi&apos;nde Aç
          </Link>
        ) : null}
        <button
          type="button"
          className="commint-button"
          disabled={pending}
          data-testid={`presentation-reset-${definition.key}`}
          onClick={resetScenario}
        >
          {pending ? "Sıfırlanıyor…" : "Senaryoyu Sıfırla"}
        </button>
      </div>

      {message ? (
        <p role="status" className="commint-kpi-card__detail">
          {message}
        </p>
      ) : null}
    </article>
  );
}

export function PresentationModeView({ dashboard }: PresentationModeViewProps) {
  const scenarioStateByKey = new Map(
    dashboard.scenarios.map((scenario) => [scenario.key, scenario]),
  );

  return (
    <div className="commint-page" data-testid="presentation-mode-page">
      <header className="commint-executive">
        <div className="commint-executive__intro">
          <p className="commint-executive__eyebrow">FACTOS</p>
          <h1>{PRESENTATION_MODE_TITLE}</h1>
          <p className="commint-executive__subtitle">{PRESENTATION_MODE_SUBTITLE}</p>
        </div>
      </header>

      <section className="presentation-mode-status-panel" data-testid="presentation-mode-status-panel">
        <p className="commint-executive__eyebrow">SUNUM DURUMU</p>
        <div className="presentation-mode-status-panel__metrics">
          <div>
            <span className="commint-kpi-card__label">Senaryo</span>
            <strong>{dashboard.summary.total}</strong>
          </div>
          <div>
            <span className="commint-kpi-card__label">Hazır</span>
            <strong>{dashboard.summary.ready}</strong>
          </div>
          <div>
            <span className="commint-kpi-card__label">Devam Ediyor</span>
            <strong>
              {dashboard.summary.inProgress +
                dashboard.summary.awaitingApproval +
                dashboard.summary.verification}
            </strong>
          </div>
          <div>
            <span className="commint-kpi-card__label">VERIFIED</span>
            <strong>{dashboard.summary.verified}</strong>
          </div>
        </div>
      </section>

      <section className="presentation-mode-scenario-grid">
        {PRESENTATION_SCENARIOS.map((definition) => {
          const state = scenarioStateByKey.get(definition.key);
          if (!state) {
            return null;
          }

          return <ScenarioCard key={definition.key} definition={definition} state={state} />;
        })}
      </section>

      <section className="presentation-mode-factory-link">
        <Link href={FACTORY_DECISION_CENTER_ROUTE} className="commint-button">
          Fabrika Karar Merkezi&apos;ni Aç
          <ArrowUpRight aria-hidden="true" size={16} />
        </Link>
      </section>
    </div>
  );
}
