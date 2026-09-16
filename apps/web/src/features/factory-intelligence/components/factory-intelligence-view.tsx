"use client";

import Link from "next/link";
import { ArrowUpRight, Activity } from "lucide-react";

import { OperationalBadge } from "@/components/workspace/operational-status";
import { StatusStrip } from "@/components/workspace/status-strip";
import { WorkspaceHeader } from "@/components/workspace/workspace-header";

import type {
  FactoryIntelligenceOverviewApi,
  FactoryIntelligenceRiskCardApi,
} from "../server/factory-intelligence-api-client";

type FactoryIntelligenceViewProps = Readonly<{
  overview: FactoryIntelligenceOverviewApi;
  risks: readonly FactoryIntelligenceRiskCardApi[];
}>;

function riskStatusVariant(riskState: string): "warning" | "fault" | "pending" | "idle" {
  if (riskState === "critical") return "fault";
  if (riskState === "warning") return "warning";
  if (riskState === "watch") return "pending";
  return "idle";
}

export function FactoryIntelligenceView({ overview, risks }: FactoryIntelligenceViewProps) {
  return (
    <main className="fact-workspace" data-testid="factory-intelligence-view">
      <WorkspaceHeader
        context="SİNYAL · RİSK · ÖNGÖRÜ"
        title="Fabrika Zekâsı"
        meta="Operasyonel sinyallerden üretilen açıklanabilir risk analizi — kesinlik iddiası yoktur"
        actions={
          <Link href="/fabrika-karar-merkezi" className="button button-secondary button-medium">
            Karar Merkezi
            <ArrowUpRight aria-hidden="true" size={16} />
          </Link>
        }
      />

      <StatusStrip
        aria-label="Öngörü özet"
        items={[
          { label: "Aktif öngörü", value: overview.activePredictions },
          { label: "Watch", value: overview.watchCount },
          { label: "Warning", value: overview.warningCount },
          { label: "Critical", value: overview.criticalCount },
          { label: "Önlenen (24s)", value: overview.avertedLast24h },
          { label: "Veri kalitesi", value: overview.dataQualitySummary },
        ]}
      />

      <section className="fact-section" data-testid="factory-intelligence-active-risks">
        <header className="fact-section__header">
          <h2 className="fact-section__title">Aktif öngörüler</h2>
          <span className="fact-workspace__meta">{risks.length} kayıt</span>
        </header>
        <div className="fact-section__body">
          {risks.length === 0 ? (
            <p className="production-execution-empty__desc">Şu anda aktif öngörülen risk yok.</p>
          ) : (
            <table className="fact-data-grid">
              <thead>
                <tr>
                  <th>Durum</th>
                  <th>Makine / Emir</th>
                  <th>Gözlem</th>
                  <th>Öngörü</th>
                  <th>Ufuk</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {risks.map((risk) => (
                  <tr key={risk.riskId}>
                    <td>
                      <OperationalBadge status={riskStatusVariant(risk.riskState)} label={risk.riskState} />
                    </td>
                    <td>
                      <strong>{risk.machineCode}</strong>
                      {risk.orderReference ? <span className="fact-workspace__meta"> · {risk.orderReference}</span> : null}
                    </td>
                    <td>{risk.observedSummary}</td>
                    <td>{risk.predictedSummary}</td>
                    <td className="fact-numeric">{risk.horizon}</td>
                    <td>
                      {risk.linkedCaseId ? (
                        <Link href={`/fabrika-karar-merkezi/vakalar/${risk.linkedCaseId}`} className="button button-ghost button-medium">
                          Vaka
                        </Link>
                      ) : (
                        <Activity aria-hidden="true" size={14} />
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </section>
    </main>
  );
}
