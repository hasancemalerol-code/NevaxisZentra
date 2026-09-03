import Link from "next/link";
import {
  ArrowUpRight,
  Building2,
  CalendarClock,
  UsersRound,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";

import type { AiTaskCenterItem } from "../domain/ai-task-center-item";
import {
  AI_TASK_PRIORITY_TONES,
  AI_TASK_STATUS_TONES,
} from "../presentation/ai-task-labels";
import {
  AI_TASK_ESCALATION_STATE_TONES,
  AI_TASK_SLA_STATE_TONES,
  getAiTaskEscalationBadgeLabel,
  getAiTaskSlaDisplayText,
} from "../presentation/ai-task-sla-labels";

type AiTaskCenterItemCardProps = Readonly<{
  item: AiTaskCenterItem;
  now?: Date;
}>;

function resolveAssigneeInitials(label: string | null): string {
  if (!label) {
    return "?";
  }

  const parts = label.trim().split(/\s+/).filter(Boolean);
  if (parts.length === 0) {
    return "?";
  }

  if (parts.length === 1) {
    return parts[0]!.slice(0, 2).toUpperCase();
  }

  return `${parts[0]![0] ?? ""}${parts[parts.length - 1]![0] ?? ""}`.toUpperCase();
}

function resolveAcknowledgmentLabel(item: AiTaskCenterItem): string | null {
  if (item.kind !== "task") {
    return null;
  }

  if (item.acknowledgedAtUtc) {
    return "Teslim alındı";
  }

  if (item.assignedMembershipId || item.assignedTeamChannelId) {
    return "Teslim bekleniyor";
  }

  return null;
}

export function AiTaskCenterItemCard({
  item,
  now = new Date(),
}: AiTaskCenterItemCardProps) {
  const slaText = getAiTaskSlaDisplayText(item.slaState, item.slaDueAt, now);
  const escalationBadge = item.escalationLevel
    ? getAiTaskEscalationBadgeLabel(item.escalationLevel)
    : null;
  const acknowledgmentLabel = resolveAcknowledgmentLabel(item);
  const assigneeInitials = resolveAssigneeInitials(item.assigneeLabel);
  const priorityTone = item.priority ? AI_TASK_PRIORITY_TONES[item.priority] : "neutral";
  const slaTone = item.slaState ? AI_TASK_SLA_STATE_TONES[item.slaState] : "neutral";
  const escalationTone = item.escalationState
    ? AI_TASK_ESCALATION_STATE_TONES[item.escalationState]
    : "neutral";
  const statusTone = item.statusCode && item.statusCode in AI_TASK_STATUS_TONES
    ? AI_TASK_STATUS_TONES[item.statusCode as keyof typeof AI_TASK_STATUS_TONES]
    : "neutral";

  return (
    <article
      className="ai-task-center-card"
      data-escalation-level={item.escalationLevel ?? 0}
      data-priority={item.priority ?? "none"}
      data-sla-state={item.slaState ?? "none"}
      data-testid={`ai-task-center-item-${item.nativeSourceKind}-${item.nativeSourceId}`}
    >
      <div className="ai-task-center-card__accent" aria-hidden="true" />

      <header className="ai-task-center-card__top">
        <Badge tone="neutral">{item.sourceLabel}</Badge>

        {item.priorityLabel ? (
          <Badge tone={priorityTone}>{item.priorityLabel}</Badge>
        ) : null}

        {slaText ? (
          <Badge tone={slaTone}>{slaText}</Badge>
        ) : null}

        {escalationBadge && item.escalationState === "active" ? (
          <Badge tone={escalationTone}>{escalationBadge}</Badge>
        ) : null}
      </header>

      <div className="ai-task-center-card__main">
        <h3 className="ai-task-center-card__title">
          <Link href={item.deepLinkPath}>{item.title}</Link>
        </h3>

        {item.summary ? (
          <p className="ai-task-center-card__summary">{item.summary}</p>
        ) : null}

        <div className="ai-task-center-card__context">
          {item.facilityLabel ? (
            <span className="ai-task-center-card__meta">
              <Building2 aria-hidden="true" />
              {item.facilityLabel}
            </span>
          ) : null}

          <span className="ai-task-center-card__meta ai-task-center-card__assignee">
            <span
              aria-hidden="true"
              className="ai-task-center-card__avatar"
            >
              {assigneeInitials}
            </span>
            <span>
              {item.assigneeLabel ?? "Atanmamış"}
              {item.teamLabel ? (
                <>
                  {" · "}
                  <UsersRound aria-hidden="true" className="ai-task-center-card__team-icon" />
                  {item.teamLabel}
                </>
              ) : null}
            </span>
          </span>
        </div>
      </div>

      <div className="ai-task-center-card__status-row">
        {item.statusLabel ? (
          <Badge tone={statusTone}>{item.statusLabel}</Badge>
        ) : null}

        {item.dueAtLabel ? (
          <span className="ai-task-center-card__due">
            <CalendarClock aria-hidden="true" />
            {item.dueAtLabel}
          </span>
        ) : null}

        {acknowledgmentLabel ? (
          <span
            className="ai-task-center-card__ack"
            data-acknowledged={Boolean(item.acknowledgedAtUtc)}
          >
            {acknowledgmentLabel}
          </span>
        ) : null}
      </div>

      <footer className="ai-task-center-card__footer">
        <div className="ai-task-center-card__related">
          {item.relatedContext ? (
            <span>{item.relatedContext}</span>
          ) : (
            <span className="ai-task-center-card__origin">{item.sourceLabel}</span>
          )}
        </div>

        <Link
          aria-label={`${item.title} görevini aç`}
          className="ai-task-center-card__action"
          href={item.deepLinkPath}
        >
          Görevi Aç
          <ArrowUpRight aria-hidden="true" />
        </Link>
      </footer>
    </article>
  );
}
