import Link from "next/link";
import {
  ClipboardCheck,
  FileWarning,
  Lock,
  Timer,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import type { ProductionOrderTraceability } from "../domain/quality-control";
import { getQualityStatusLabel } from "../domain/quality-control";

type QualityTraceabilityPanelProps = Readonly<{
  traceability: ProductionOrderTraceability | null;
}>;

type TimelineEntry = Readonly<{
  id: string;
  kind: "inspection" | "ncr" | "hold" | "action";
  timestamp: string;
  title: string;
  subtitle: string;
  status: string;
  href?: string;
}>;

function buildTimeline(traceability: ProductionOrderTraceability): readonly TimelineEntry[] {
  const entries: TimelineEntry[] = [];

  for (const inspection of traceability.inspections) {
    entries.push({
      id: `inspection-${inspection.id}`,
      kind: "inspection",
      timestamp: inspection.completedAtUtc ?? "1970-01-01T00:00:00Z",
      title: inspection.inspectionNumber,
      subtitle: `${inspection.results.length} ölçüm kaydı`,
      status: inspection.overallResult,
      href: `/kalite/kontroller/${inspection.id}`,
    });
  }

  for (const ncr of traceability.nonconformances) {
    entries.push({
      id: `ncr-${ncr.id}`,
      kind: "ncr",
      timestamp: ncr.detectedAtUtc,
      title: ncr.ncrNumber,
      subtitle: getQualityStatusLabel(ncr.severity),
      status: ncr.status,
      href: `/kalite/ncr/${ncr.id}`,
    });
  }

  for (const hold of traceability.holds) {
    entries.push({
      id: `hold-${hold.id}`,
      kind: "hold",
      timestamp: hold.placedAtUtc,
      title: getQualityStatusLabel(hold.scope),
      subtitle: hold.reason,
      status: hold.status,
    });
  }

  for (const action of traceability.correctiveActions) {
    entries.push({
      id: `action-${action.id}`,
      kind: "action",
      timestamp: action.completedAtUtc ?? action.dueAtUtc ?? "1970-01-01T00:00:00Z",
      title: action.title,
      subtitle: action.ownerSubjectId ?? "Sahipsiz",
      status: action.status,
    });
  }

  return entries.sort((a, b) => b.timestamp.localeCompare(a.timestamp));
}

function timelineIcon(kind: TimelineEntry["kind"]) {
  switch (kind) {
    case "inspection":
      return ClipboardCheck;
    case "ncr":
      return FileWarning;
    case "hold":
      return Lock;
    default:
      return Timer;
  }
}

function timelineLabel(kind: TimelineEntry["kind"]): string {
  switch (kind) {
    case "inspection":
      return "Kontrol";
    case "ncr":
      return "NCR";
    case "hold":
      return "Blokesi";
    default:
      return "Düzeltici aksiyon";
  }
}

export function QualityTraceabilityPanel({ traceability }: QualityTraceabilityPanelProps) {
  if (!traceability) {
    return (
      <section className="quality-section quality-section--wide" aria-label="Kalite izlenebilirliği">
        <header className="quality-section__header">
          <h2>Kalite İzlenebilirliği</h2>
        </header>
        <p className="quality-empty-state">Üretim emri için izlenebilirlik verisi alınamadı.</p>
      </section>
    );
  }

  const timeline = buildTimeline(traceability);

  return (
    <section className="quality-section quality-section--wide" aria-label="Kalite izlenebilirliği">
      <header className="quality-section__header">
        <div>
          <h2>Kalite İzlenebilirliği</h2>
          <p>
            {traceability.productionOrderReference ?? traceability.productionOrderId} ·{" "}
            {traceability.productReference ?? "—"}
          </p>
        </div>
        <Badge tone="information">{timeline.length} olay</Badge>
      </header>

      <dl className="quality-detail-dl">
        <div>
          <dt>Kontroller</dt>
          <dd>{traceability.inspections.length}</dd>
        </div>
        <div>
          <dt>NCR</dt>
          <dd>{traceability.nonconformances.length}</dd>
        </div>
        <div>
          <dt>Blokesi</dt>
          <dd>{traceability.holds.length}</dd>
        </div>
        <div>
          <dt>Düzeltici Aksiyon</dt>
          <dd>{traceability.correctiveActions.length}</dd>
        </div>
      </dl>

      <ul className="quality-ca-list" aria-label="Kalite izlenebilirlik zaman çizelgesi">
        {timeline.length === 0 ? (
          <li className="quality-empty-state">Kayıt yok.</li>
        ) : (
          timeline.map((entry) => {
            const Icon = timelineIcon(entry.kind);
            const content = (
              <>
                <span className="quality-dashboard-kpi__icon" aria-hidden="true">
                  <Icon />
                </span>
                <div className="quality-ca-item__body">
                  <strong>{entry.title}</strong>
                  <span>
                    {timelineLabel(entry.kind)} · {entry.subtitle}
                  </span>
                  <div className="quality-ca-item__meta">
                    <span>{new Date(entry.timestamp).toLocaleString("tr-TR")}</span>
                  </div>
                </div>
                <Badge tone={entry.status === "fail" || entry.status === "critical" ? "danger" : "neutral"}>
                  {getQualityStatusLabel(entry.status)}
                </Badge>
              </>
            );

            return (
              <li key={entry.id} className="quality-ca-item">
                {entry.href ? <Link href={entry.href}>{content}</Link> : content}
              </li>
            );
          })
        )}
      </ul>
    </section>
  );
}
