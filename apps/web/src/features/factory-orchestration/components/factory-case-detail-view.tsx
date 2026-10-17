"use client";

import Link from "next/link";
import { ArrowLeft, ShieldAlert } from "lucide-react";

import { getFactoryBlockerLabel } from "../domain/factory-orchestration-labels";
import type { FactoryCaseDetailApi } from "../server/factory-orchestration-api-client";
import "@/features/commercial-intelligence/components/commercial-intelligence-workspace.css";
import { displayLabel } from "@/lib/presentation/display-label";
import { enumLabel, humanizeEnum } from "@/lib/presentation/enum-labels";
import { formatDateTime } from "@/lib/presentation/format";

type FactoryCaseDetailViewProps = Readonly<{
  detail: FactoryCaseDetailApi;
  /** Digital Workforce is deferred: only an explicitly allowed deployment shows it (decided on the server). */
  showWorkforce?: boolean;
}>;

const EDGE_LABELS: Record<string, string> = {
  blocks: "engelliyor",
  "blocked-by": "tarafından engelleniyor",
  "depends-on": "bağımlı",
  requires: "gerektiriyor",
  triggers: "tetikliyor",
  feeds: "besliyor",
};

/** A readable dependency step: known domains get their Turkish name, anything opaque becomes "Adım N". */
function nodeLabel(key: string, index: number): string {
  const head = key.split(/[:/]/)[0] ?? "";
  const resolved = enumLabel(head);
  const opaque = !head || /^[0-9a-f-]{16,}$/i.test(head) || resolved === humanizeEnum(head) && /\d/.test(head);
  return opaque ? `Adım ${index + 1}` : resolved;
}

function edgeLabel(edgeType: string): string {
  return EDGE_LABELS[edgeType.toLowerCase()] ?? "bağlantılı";
}

export function FactoryCaseDetailView({ detail, showWorkforce = false }: FactoryCaseDetailViewProps) {
  const root = detail.rootCase;
  const primaryBlocker = detail.blockers[0];

  return (
    <div className="commint-page" data-testid="factory-case-detail">
      <header className="commint-executive">
        <div className="commint-executive__top">
          <div className="commint-executive__intro">
            <p className="commint-executive__eyebrow">FABRİKA VAKASI</p>
            <h1>{root.title}</h1>
            <p className="commint-executive__subtitle">
              {enumLabel(root.severity)} · {enumLabel(root.status)} · {enumLabel(root.primaryDomain)}
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
        <h2>Bu vakayı ne engelliyor?</h2>
        <p>{detail.blockerExplanation ?? getFactoryBlockerLabel(root.currentBlockerCode)}</p>
        {primaryBlocker?.isStale ? (
          <p className="commint-readonly-note" data-testid="factory-case-stale-blocker">
            <ShieldAlert aria-hidden="true" size={14} />
            Bu engel uzun süredir değişmedi.
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
          {detail.dependencies.length === 0 ? <li>Bu vaka için tanımlı bağımlılık yok.</li> : null}
          {detail.dependencies.map((edge, index) => (
            <li key={`${edge.fromNodeKey}-${edge.toNodeKey}-${edge.edgeType}`}>
              {nodeLabel(edge.fromNodeKey, index * 2)} {edgeLabel(edge.edgeType)}: {nodeLabel(edge.toNodeKey, index * 2 + 1)}
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
          {detail.timeline.length === 0 ? <li>Henüz kayıtlı hareket yok.</li> : null}
          {detail.timeline.map((entry) => (
            <li key={entry.id}>
              <time dateTime={entry.createdAtUtc}>{formatDateTime(entry.createdAtUtc)}</time>
              {": "}
              {displayLabel(entry.description, enumLabel(entry.actionType))}
            </li>
          ))}
        </ul>
      </section>

      {showWorkforce ? (
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
      ) : null}
    </div>
  );
}
