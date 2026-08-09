import Link from "next/link";
import {
  ClipboardCheck,
  FileWarning,
  GitBranch,
  Lock,
  Timer,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";

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
      <Card className="production-execution-panel production-execution-panel--wide">
        <header>
          <GitBranch aria-hidden="true" />
          <h2>Kalite izlenebilirliği</h2>
        </header>
        <p>Üretim emri için izlenebilirlik verisi alınamadı.</p>
      </Card>
    );
  }

  const timeline = buildTimeline(traceability);

  return (
    <Card className="production-execution-panel production-execution-panel--wide">
      <header>
        <div>
          <p>İZLENEBİLİRLİK</p>
          <h2>Kalite izlenebilirliği</h2>
          <span>
            {traceability.productionOrderReference ?? traceability.productionOrderId} ·{" "}
            {traceability.productReference ?? "—"}
          </span>
        </div>
        <Badge tone="information">{timeline.length} olay</Badge>
      </header>

      <div className="production-execution-grid">
        <dl className="production-execution-details">
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
            <dt>Düzeltici aksiyon</dt>
            <dd>{traceability.correctiveActions.length}</dd>
          </div>
        </dl>
      </div>

      <ul className="production-execution-list" aria-label="Kalite izlenebilirlik zaman çizelgesi">
        {timeline.length === 0 ? (
          <li>Kayıt yok.</li>
        ) : (
          timeline.map((entry) => {
            const Icon = timelineIcon(entry.kind);
            const content = (
              <>
                <Icon aria-hidden="true" />
                <div>
                  <strong>{entry.title}</strong>
                  <span>
                    {timelineLabel(entry.kind)} · {entry.subtitle}
                  </span>
                  <em>{new Date(entry.timestamp).toLocaleString("tr-TR")}</em>
                </div>
                <Badge tone={entry.status === "fail" || entry.status === "critical" ? "danger" : "neutral"}>
                  {getQualityStatusLabel(entry.status)}
                </Badge>
              </>
            );

            return (
              <li key={entry.id}>
                {entry.href ? <Link href={entry.href}>{content}</Link> : content}
              </li>
            );
          })
        )}
      </ul>
    </Card>
  );
}
