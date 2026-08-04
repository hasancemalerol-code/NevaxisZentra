import Link from "next/link";
import {
  Bot,
  CalendarClock,
  CircleDollarSign,
  Gauge,
  UserRound,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";

import { isAiTaskOverdue } from "../application/ai-task-query";
import type { AiTask } from "../domain/ai-task";
import {
  AI_TASK_PRIORITY_TONES,
  AI_TASK_RISK_LEVEL_TONES,
  AI_TASK_STATUS_TONES,
  getAiTaskActionTypeLabel,
  getAiTaskCategoryLabel,
  getAiTaskPriorityLabel,
  getAiTaskRiskLevelLabel,
  getAiTaskStatusLabel,
} from "../presentation/ai-task-labels";

type AiTaskCardProps = Readonly<{
  task: AiTask;
  href?: string;
  now?: Date;
}>;

const DATE_TIME_FORMATTER = new Intl.DateTimeFormat(
  "tr-TR",
  {
    dateStyle: "short",
    timeStyle: "short",
    timeZone: "Europe/Istanbul",
  },
);

const NUMBER_FORMATTER = new Intl.NumberFormat(
  "tr-TR",
  {
    maximumFractionDigits: 0,
  },
);

const PERCENT_FORMATTER = new Intl.NumberFormat(
  "tr-TR",
  {
    style: "percent",
    maximumFractionDigits: 0,
  },
);

function formatDateTime(value: string): string {
  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return "Geçersiz tarih";
  }

  return DATE_TIME_FORMATTER.format(date);
}

function formatFinancialImpact(
  task: Pick<AiTask, "impact">,
): string | undefined {
  const { financialAmount, currency } = task.impact;

  if (
    financialAmount === undefined ||
    !currency
  ) {
    return undefined;
  }

  return `${NUMBER_FORMATTER.format(
    financialAmount,
  )} ${currency}`;
}

export function AiTaskCard({
  task,
  href = `/ai-gorevleri/${task.id}`,
  now = new Date(),
}: AiTaskCardProps) {
  const overdue = isAiTaskOverdue(task, now);
  const financialImpact = formatFinancialImpact(task);

  return (
    <Card
      className="ai-task-card"
      data-overdue={overdue}
      data-priority={task.priority}
      data-risk-level={task.riskLevel}
      data-status={task.status}
    >
      <article aria-labelledby={`${task.id}-title`}>
        <header className="ai-task-card__header">
          <div className="ai-task-card__identity">
            <span
              aria-hidden="true"
              className="ai-task-card__ai-icon"
            >
              <Bot />
            </span>

            <div>
              <p className="ai-task-card__category">
                {getAiTaskCategoryLabel(task.category)}
              </p>

              <h3
                className="ai-task-card__title"
                id={`${task.id}-title`}
              >
                <Link href={href}>
                  {task.title}
                </Link>
              </h3>
            </div>
          </div>

          <div
            aria-label="Görev durumları"
            className="ai-task-card__badges"
          >
            <Badge tone={AI_TASK_STATUS_TONES[task.status]}>
              {getAiTaskStatusLabel(task.status)}
            </Badge>

            <Badge tone={AI_TASK_PRIORITY_TONES[task.priority]}>
              {getAiTaskPriorityLabel(task.priority)}
            </Badge>

            <Badge tone={AI_TASK_RISK_LEVEL_TONES[task.riskLevel]}>
              {getAiTaskRiskLevelLabel(task.riskLevel)}
            </Badge>

            {overdue ? (
              <Badge tone="danger">
                Gecikmiş
              </Badge>
            ) : null}
          </div>
        </header>

        <p className="ai-task-card__summary">
          {task.summary}
        </p>

        <div className="ai-task-card__recommendation">
          <div className="ai-task-card__recommendation-heading">
            <span>
              AI önerisi
            </span>

            <Badge tone="ai">
              {getAiTaskActionTypeLabel(
                task.recommendation.actionType,
              )}
            </Badge>
          </div>

          <strong>
            {task.recommendation.title}
          </strong>

          <p>
            {task.recommendation.description}
          </p>
        </div>

        <dl className="ai-task-card__metrics">
          <div>
            <dt>
              <Gauge aria-hidden="true" />
              AI güveni
            </dt>
            <dd>
              {PERCENT_FORMATTER.format(
                task.confidence.score,
              )}
            </dd>
          </div>

          {financialImpact ? (
            <div>
              <dt>
                <CircleDollarSign aria-hidden="true" />
                Finansal etki
              </dt>
              <dd>
                {financialImpact}
              </dd>
            </div>
          ) : null}

          <div>
            <dt>
              <UserRound aria-hidden="true" />
              Sorumlu
            </dt>
            <dd>
              {task.assignee?.displayName ?? "Atanmadı"}
            </dd>
          </div>

          <div>
            <dt>
              <CalendarClock aria-hidden="true" />
              Son tarih
            </dt>
            <dd>
              {task.dueAt
                ? formatDateTime(task.dueAt)
                : "Tanımlanmadı"}
            </dd>
          </div>
        </dl>

        <footer className="ai-task-card__footer">
          <div className="ai-task-card__references">
            {task.productionOrderId ? (
              <span>
                Üretim emri: {task.productionOrderId}
              </span>
            ) : null}

            {task.machineId ? (
              <span>
                Makine: {task.machineId}
              </span>
            ) : null}

            {task.moldId ? (
              <span>
                Kalıp: {task.moldId}
              </span>
            ) : null}

            {task.salesOrderId ? (
              <span>
                Sipariş/Teklif: {task.salesOrderId}
              </span>
            ) : null}
          </div>

          <Link
            aria-label={`${task.title} görevini incele`}
            className="ai-task-card__detail-link"
            href={href}
          >
            Görevi incele
          </Link>
        </footer>
      </article>
    </Card>
  );
}