import {
  ArrowRightLeft,
  Bell,
  CheckCircle2,
  CirclePlay,
  Hand,
  History,
  ShieldCheck,
  UserPlus,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";

import type { AiTaskAuditEvent } from "../domain/ai-task";
import {
  getAiTaskAuditEventLabel,
  getAiTaskAuditMetadataSummary,
  getAiTaskAuditTransitionLabel,
} from "../presentation/ai-task-audit-labels";
import { getAiTaskStatusLabel } from "../presentation/ai-task-labels";
import { formatAiTaskShortDateTime } from "../presentation/ai-task-formatters";

type AiTaskDetailTimelineProps = Readonly<{
  events: readonly AiTaskAuditEvent[];
}>;

function resolveTimelineIcon(eventType: AiTaskAuditEvent["eventType"]) {
  switch (eventType) {
    case "assigned":
    case "reassigned":
    case "routed":
      return <UserPlus aria-hidden="true" />;
    case "acknowledged":
      return <Hand aria-hidden="true" />;
    case "escalated":
    case "sla-warning":
      return <Bell aria-hidden="true" />;
    case "approved":
    case "execution-completed":
      return <CheckCircle2 aria-hidden="true" />;
    case "execution-started":
      return <CirclePlay aria-hidden="true" />;
    case "approval-requested":
    case "rejected":
      return <ShieldCheck aria-hidden="true" />;
    case "priority-changed":
    case "due-date-changed":
    case "status-changed":
      return <ArrowRightLeft aria-hidden="true" />;
    default:
      return <History aria-hidden="true" />;
  }
}

export function AiTaskDetailTimeline({
  events,
}: AiTaskDetailTimelineProps) {
  if (events.length === 0) {
    return (
      <p className="ai-task-detail-timeline__empty">
        Henüz denetim kaydı bulunmuyor.
      </p>
    );
  }

  return (
    <ol
      className="ai-task-detail-timeline"
      data-testid="ai-task-audit-timeline"
    >
      {events.map((event) => {
        const metadata = getAiTaskAuditMetadataSummary(event.metadataJson);
        const transition = getAiTaskAuditTransitionLabel(
          event,
          getAiTaskStatusLabel,
        );

        return (
          <li
            className="ai-task-detail-timeline__item ai-task-detail__audit-card"
            key={event.id}
          >
            <span aria-hidden="true" className="ai-task-detail-timeline__icon">
              {resolveTimelineIcon(event.eventType)}
            </span>

            <div className="ai-task-detail-timeline__content">
              <div className="ai-task-detail-timeline__top">
                <strong>{getAiTaskAuditEventLabel(event.eventType)}</strong>
                <time dateTime={event.occurredAt}>
                  {formatAiTaskShortDateTime(event.occurredAt)}
                </time>
              </div>

              <p className="ai-task-detail-timeline__actor">
                {event.actorLabel}
              </p>

              {event.note ? (
                <small className="ai-task-detail-timeline__note">
                  {event.note}
                </small>
              ) : metadata ? (
                <small className="ai-task-detail-timeline__note">
                  {metadata}
                </small>
              ) : null}

              {transition ? (
                <Badge tone="neutral">{transition}</Badge>
              ) : null}
            </div>
          </li>
        );
      })}
    </ol>
  );
}
