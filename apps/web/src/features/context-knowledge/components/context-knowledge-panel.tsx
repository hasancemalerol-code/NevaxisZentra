"use client";

import { AlertTriangle } from "lucide-react";

import { StatusStrip } from "@/components/workspace/status-strip";
import { humanizeEnum } from "@/lib/presentation/enum-labels";
import { lineStateLabel } from "@/lib/presentation/throughput-labels";

import type { WorkerContextPackageApi } from "../server/context-knowledge-api-client";

type ContextKnowledgePanelProps = Readonly<{
  package: WorkerContextPackageApi;
}>;

const ITEM_KIND_LABELS: Readonly<Record<string, string>> = {
  event: "Olay",
  related_case: "İlgili vaka",
  verified_knowledge: "Doğrulanmış bilgi",
  knowledge: "Bilgi",
  state: "Durum",
  document: "Belge",
  order: "Üretim emri",
  task: "Görev",
};

function itemKindLabel(kind: string): string {
  return ITEM_KIND_LABELS[kind.trim().toLowerCase()] ?? humanizeEnum(kind);
}

export function ContextKnowledgePanel({ package: contextPackage }: ContextKnowledgePanelProps) {
  const canonical = contextPackage.canonicalState;
  const eventCount = contextPackage.items.filter((item) => item.itemKind === "event").length;
  const caseCount = contextPackage.items.filter((item) => item.itemKind === "related_case").length;
  const knowledgeCount = contextPackage.verifiedKnowledge.length;
  const hasConflict = canonical?.resolutionStatus === "CONFLICTED" || contextPackage.failureState === "CONFLICTED_STATE";
  const insufficient = contextPackage.failureState === "INSUFFICIENT_CONTEXT";

  const currentState = canonical?.resolvedStateValue ?? canonical?.resolutionStatus;
  // Only facts the customer can act on: internal telemetry (payload size) and empty counters stay out of the strip.
  const stripItems = [
    ...(currentState ? [{ label: "Güncel durum", value: lineStateLabel(currentState) }] : []),
    ...(eventCount > 0 ? [{ label: "Bağlam olayı", value: eventCount }] : []),
    ...(caseCount > 0 ? [{ label: "İlgili vaka", value: caseCount }] : []),
    ...(knowledgeCount > 0 ? [{ label: "Doğrulanmış bilgi", value: knowledgeCount }] : []),
  ];

  if (stripItems.length === 0 && !hasConflict && !insufficient && contextPackage.items.length === 0) {
    return null;
  }

  return (
    <div className="fact-workspace mlp-panel" data-testid="context-knowledge-panel">
      {stripItems.length > 0 ? <StatusStrip aria-label="Bağlam durumu" items={stripItems} /> : null}

      {hasConflict ? (
        <div className="mlp-notice" data-testid="context-conflict-warning" role="alert">
          <AlertTriangle aria-hidden="true" className="mlp-notice__icon" />
          <div>
            <strong className="mlp-notice__title">Çakışan Kaynak Durumu</strong>
            <p className="mlp-notice__text">Tek bir güncel durum belirlenemedi.</p>
          </div>
        </div>
      ) : null}

      {insufficient ? (
        <div className="mlp-notice" data-testid="context-insufficient-warning" role="alert">
          <AlertTriangle aria-hidden="true" className="mlp-notice__icon" />
          <div>
            <strong className="mlp-notice__title">Veri Yetersiz</strong>
            <p className="mlp-notice__text">Güvenilir veri yetersiz; karar vermeden önce ek veri gerekir.</p>
          </div>
        </div>
      ) : null}

      {contextPackage.items.length > 0 ? (
        <section className="fact-section" aria-label="İlgili bağlam">
          <header className="fact-section__header">
            <h2 className="fact-section__title">İlgili Bağlam</h2>
          </header>
          <div className="fact-section__body">
            <ul className="fact-workspace__meta mlp-context-list" data-testid="context-item-list">
              {contextPackage.items.slice(0, 8).map((item) => (
                <li key={`${item.itemKind}:${item.referenceId}`}>
                  <span className="mlp-context-list__kind">{itemKindLabel(item.itemKind)}</span> {item.summary}
                </li>
              ))}
            </ul>
            {contextPackage.items.length > 8 ? (
              <p className="fact-workspace__meta">+{contextPackage.items.length - 8} ek öğe (bütçe sınırı içinde)</p>
            ) : null}
          </div>
        </section>
      ) : null}
    </div>
  );
}
