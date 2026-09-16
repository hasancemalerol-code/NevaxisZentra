"use client";

import Link from "next/link";
import { ArrowUpRight, BrainCircuit } from "lucide-react";

import type {
  FactoryIntelligenceOverviewApi,
  FactoryIntelligenceRiskCardApi,
} from "../server/factory-intelligence-api-client";

type FactoryIntelligenceViewProps = Readonly<{
  overview: FactoryIntelligenceOverviewApi;
  risks: readonly FactoryIntelligenceRiskCardApi[];
}>;

export function FactoryIntelligenceView({ overview, risks }: FactoryIntelligenceViewProps) {
  return (
    <main className="commint-page" data-testid="factory-intelligence-view">
      <header className="commint-executive">
        <div className="commint-executive__intro">
          <p className="commint-executive__eyebrow">SİNYAL · RİSK · ÖNGÖRÜ ANALİZİ</p>
          <h1>Fabrika Zekâsı</h1>
          <p className="commint-executive__subtitle">
            Operasyonel sinyallerden üretilen açıklanabilir risk ve öngörü analizi. Kesinlik iddiası yoktur.
          </p>
        </div>
        <div className="commint-executive__actions">
          <Link href="/fabrika-karar-merkezi" className="commint-button commint-button--ghost">
            Karar Merkezi
            <ArrowUpRight aria-hidden="true" size={16} />
          </Link>
        </div>
      </header>

      <section className="commint-kpi-grid" data-testid="factory-intelligence-summary">
        {[
          { label: "Aktif öngörü", value: overview.activePredictions },
          { label: "Watch", value: overview.watchCount },
          { label: "Warning", value: overview.warningCount },
          { label: "Critical", value: overview.criticalCount },
          { label: "Önlenen (24s)", value: overview.avertedLast24h },
          { label: "Veri kalitesi", value: overview.dataQualitySummary },
        ].map((kpi) => (
          <article key={kpi.label} className="commint-kpi-card">
            <div className="commint-kpi-card__header">
              <p className="commint-kpi-card__label">{kpi.label}</p>
              <BrainCircuit aria-hidden="true" size={16} />
            </div>
            <p className="commint-kpi-card__value">{kpi.value}</p>
          </article>
        ))}
      </section>

      <section className="commint-panel" style={{ marginTop: "1.5rem" }} data-testid="factory-intelligence-active-risks">
        <div className="commint-panel__header">
          <h2>Aktif Öngörüler</h2>
          <span className="commint-kpi-card__badge">{risks.length}</span>
        </div>
        {risks.length === 0 ? (
          <p className="commint-kpi-card__detail">Şu anda aktif öngörülen risk yok.</p>
        ) : (
          <div style={{ display: "grid", gap: "1rem" }}>
            {risks.map((risk) => (
              <article key={risk.riskId} className="commint-panel">
                <div className="commint-panel__header">
                  <div>
                    <p className="commint-executive__eyebrow">{risk.riskState} · {risk.confidenceBand}</p>
                    <h3>{risk.machineCode} {risk.orderReference ? `· ${risk.orderReference}` : ""}</h3>
                  </div>
                  <span className="commint-kpi-card__badge">{risk.horizon}</span>
                </div>
                <p className="commint-kpi-card__detail">{risk.observedSummary}</p>
                <p className="commint-kpi-card__detail">{risk.predictedSummary}</p>
                {risk.linkedCaseId ? (
                  <Link href={`/fabrika-karar-merkezi/vakalar/${risk.linkedCaseId}`} className="commint-button" style={{ marginTop: "1rem" }}>
                    Erken uyarı vakası
                  </Link>
                ) : null}
              </article>
            ))}
          </div>
        )}
      </section>
    </main>
  );
}
