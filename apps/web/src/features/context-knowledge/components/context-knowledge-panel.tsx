"use client";

import { StatusStrip } from "@/components/workspace/status-strip";

import type { WorkerContextPackageApi } from "../server/context-knowledge-api-client";

type ContextKnowledgePanelProps = Readonly<{
  package: WorkerContextPackageApi;
}>;

export function ContextKnowledgePanel({ package: contextPackage }: ContextKnowledgePanelProps) {
  const canonical = contextPackage.canonicalState;
  const eventCount = contextPackage.items.filter((item) => item.itemKind === "event").length;
  const caseCount = contextPackage.items.filter((item) => item.itemKind === "related_case").length;
  const knowledgeCount = contextPackage.verifiedKnowledge.length;
  const hasConflict = canonical?.resolutionStatus === "CONFLICTED" || contextPackage.failureState === "CONFLICTED_STATE";
  const insufficient = contextPackage.failureState === "INSUFFICIENT_CONTEXT";

  return (
    <div className="fact-workspace" data-testid="context-knowledge-panel">
      <StatusStrip
        aria-label="Bağlam durumu"
        items={[
          {
            label: "Kanonik durum",
            value: canonical?.resolvedStateValue ?? canonical?.resolutionStatus ?? "—",
          },
          { label: "Olay", value: eventCount },
          { label: "Vaka", value: caseCount },
          { label: "Bilgi", value: knowledgeCount },
          { label: "Boyut", value: `${contextPackage.metrics.finalPayloadBytes} B` },
        ]}
      />

      {hasConflict ? (
        <p className="fact-workspace__meta" data-testid="context-conflict-warning" role="alert">
          Çakışan kaynak durumu — kanonik durum üretilmedi.
        </p>
      ) : null}

      {insufficient ? (
        <p className="fact-workspace__meta" data-testid="context-insufficient-warning" role="alert">
          Yetersiz güvenilir bağlam — worker karar vermeden önce ek veri gerekir.
        </p>
      ) : null}

      <section className="fact-section" aria-label="Bağlam öğeleri">
        <header className="fact-section__header">
          <h2 className="fact-section__title">Seçili bağlam</h2>
        </header>
        <div className="fact-section__body">
          <ul className="fact-workspace__meta" data-testid="context-item-list">
            {contextPackage.items.slice(0, 8).map((item) => (
              <li key={`${item.itemKind}:${item.referenceId}`}>
                [{item.itemKind}] {item.summary}
              </li>
            ))}
          </ul>
          {contextPackage.items.length > 8 ? (
            <p className="fact-workspace__meta">+{contextPackage.items.length - 8} ek öğe (bütçe sınırı içinde)</p>
          ) : null}
        </div>
      </section>
    </div>
  );
}
