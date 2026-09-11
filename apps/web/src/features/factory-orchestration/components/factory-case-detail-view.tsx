"use client";

import Link from "next/link";
import { ArrowLeft, ShieldAlert } from "lucide-react";

import { getFactoryBlockerLabel } from "../domain/factory-orchestration-labels";
import type { FactoryCaseDetailApi } from "../server/factory-orchestration-api-client";
import "@/features/commercial-intelligence/components/commercial-intelligence-workspace.css";

type FactoryCaseDetailViewProps = Readonly<{
  detail: FactoryCaseDetailApi;
}>;

export function FactoryCaseDetailView({ detail }: FactoryCaseDetailViewProps) {
  const root = detail.rootCase;
  const primaryBlocker = detail.blockers[0];

  return (
    <main className="commint-page" data-testid="factory-case-detail">
      <header className="commint-executive">
        <div className="commint-executive__top">
          <div className="commint-executive__intro">
            <p className="commint-executive__eyebrow">FABRİKA VAKASI</p>
            <h1>{root.title}</h1>
            <p className="commint-executive__subtitle">
              {root.severity} · {root.status} · {root.primaryDomain}
            </p>
          </div>
          <div className="commint-executive__actions">
            <Link href="/operasyon/yonetim-merkezi" className="commint-button commint-button--ghost">
              <ArrowLeft aria-hidden="true" size={16} />
              Operasyon Merkezi
            </Link>
          </div>
        </div>
      </header>

      <section className="commint-panel" data-testid="factory-case-blocker-section">
        <h2>Bu Vakayı Ne Engelliyor?</h2>
        <p>{detail.blockerExplanation ?? getFactoryBlockerLabel(root.currentBlockerCode)}</p>
        {primaryBlocker?.isStale ? (
          <p className="commint-readonly-note" data-testid="factory-case-stale-blocker">
            <ShieldAlert aria-hidden="true" size={14} />
            Bu bloker uzun süredir değişmedi.
          </p>
        ) : null}
        {detail.hasDeadlock ? (
          <p className="commint-partial-error" data-testid="factory-case-deadlock">
            Bağımlılık döngüsü tespit edildi.
          </p>
        ) : null}
        {detail.hasConflict ? (
          <p className="commint-partial-error" data-testid="factory-case-conflict">
            Çapraz alan veri çakışması çözüm bekliyor.
          </p>
        ) : null}
      </section>

      <section className="commint-panel" data-testid="factory-case-next-action">
        <h2>Sıradaki Adım</h2>
        <p>{detail.nextAction ?? "Koordinatör sonraki adımı belirliyor."}</p>
      </section>

      <section className="commint-panel" data-testid="factory-case-dependency-graph">
        <h2>Bağımlılık Grafiği</h2>
        <ol className="commint-timeline">
          {detail.dependencies.map((edge) => (
            <li key={`${edge.fromNodeKey}-${edge.toNodeKey}-${edge.edgeType}`}>
              {edge.fromNodeKey} → {edge.toNodeKey} ({edge.edgeType})
            </li>
          ))}
        </ol>
      </section>

      <section className="commint-panel" data-testid="factory-case-recovery-plan">
        <h2>Kurtarma Planı</h2>
        <p>{detail.recoveryPlan.objective}</p>
        <ul>
          {detail.recoveryPlan.actions.map((action) => (
            <li key={action}>{action}</li>
          ))}
        </ul>
      </section>

      <section className="commint-panel" data-testid="factory-case-timeline">
        <h2>Zaman Çizelgesi</h2>
        <ul className="commint-timeline">
          {detail.timeline.map((entry) => (
            <li key={entry.id}>
              {entry.createdAtUtc}: {entry.description || entry.actionType}
            </li>
          ))}
        </ul>
      </section>

      <section className="commint-panel" data-testid="factory-case-digital-workforce">
        <h2>Dijital Çalışanlar</h2>
        <ul>
          <li>
            {root.primaryWorkerName ?? root.primaryWorkerCode ?? root.primaryWorkerId}
            {" · "}
            {root.status}
          </li>
          {detail.childCases.map((child) => (
            <li key={child.id}>
              {child.primaryWorkerName ?? child.primaryWorkerCode ?? child.primaryWorkerId}
              {" · "}
              {child.title}
            </li>
          ))}
        </ul>
      </section>
    </main>
  );
}
