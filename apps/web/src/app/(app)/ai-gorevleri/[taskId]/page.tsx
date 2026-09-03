import { notFound } from "next/navigation";
import {
  Bot,
  CalendarClock,
  CircleDollarSign,
  Database,
  Gauge,
  History,
  UserRound,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import {
  PageContent,
  PageHeader,
  Section,
} from "@/components/ui/page";
import { EmptyState } from "@/components/ui/states";
import { AiTaskActionPanelClient } from "@/features/ai-task-center/components/ai-task-action-panel-client";
import { AiTaskOperationsPanelClient } from "@/features/ai-task-center/components/ai-task-operations-panel-client";
import { AiTaskExecutionPanel } from "@/features/ai-task-center/components/ai-task-execution-panel";
import { AiTaskImpactPanel } from "@/features/ai-task-center/components/ai-task-impact-panel";
import { AiTaskSourceReferenceSection } from "@/features/ai-task-center/components/ai-task-source-reference-section";
import { AiTaskTeamsContextSection } from "@/features/teams/components/ai-task-teams-context-section";
import { AiTaskTeamsMessageSourceLink } from "@/features/teams/components/ai-task-teams-message-source-link";
import {
  getAiTaskAuditMetadataSummary,
  getAiTaskAuditTransitionLabel,
  getAiTaskAuditEventLabel,
} from "@/features/ai-task-center/presentation/ai-task-audit-labels";
import {
  getAiTaskActorRoleDescription,
  getAiTaskActorRoleLabel,
} from "@/features/ai-task-center/presentation/ai-task-role-labels";
import {
  getAiTaskSourceTypeDescription,
  getAiTaskSourceTypeLabel,
} from "@/features/ai-task-center/presentation/ai-task-source-labels";
import {
  getAiTaskTargetSystemDescription,
  getAiTaskTargetSystemLabel,
} from "@/features/ai-task-center/presentation/ai-task-target-system-labels";
import {
  AI_TASK_PRIORITY_TONES,
  AI_TASK_RISK_LEVEL_TONES,
  AI_TASK_STATUS_TONES,
  getAiTaskActionTypeLabel,
  getAiTaskApprovalStateLabel,
  getAiTaskCategoryLabel,
  getAiTaskPriorityLabel,
  getAiTaskRiskLevelLabel,
  getAiTaskStatusLabel,
} from "@/features/ai-task-center/presentation/ai-task-labels";
import { readAiTaskDetail } from "@/features/ai-task-center/server/ai-task-queries";
import {
  AI_TASK_SLA_STATE_TONES,
  formatAiTaskSlaCountdown,
  getAiTaskEscalationLevelLabel,
  getAiTaskEscalationStateLabel,
  getAiTaskSlaStateLabel,
} from "@/features/ai-task-center/presentation/ai-task-sla-labels";

type PageParams = Readonly<{
  taskId: string;
}>;

const DATE_TIME_FORMATTER = new Intl.DateTimeFormat(
  "tr-TR",
  {
    dateStyle: "medium",
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

function formatDateTime(
  value: string | undefined,
): string {
  if (!value) {
    return "Tanımlanmadı";
  }

  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return "Geçersiz tarih";
  }

  return DATE_TIME_FORMATTER.format(date);
}

function formatMoney(
  amount: number | undefined,
  currency: string | undefined,
): string {
  if (amount === undefined || !currency) {
    return "Tanımlanmadı";
  }

  return `${NUMBER_FORMATTER.format(amount)} ${currency}`;
}

export default async function AiTaskDetailPage({
  params,
}: Readonly<{
  params: Promise<PageParams>;
}>) {
  const { taskId } = await params;
  const result = await readAiTaskDetail(taskId);

  if (result.status === "failure") {
    if (result.code === "not-found") {
      notFound();
    }

    return (
      <PageContent>
        <PageHeader
          description="AI görev detayı yüklenemedi."
          eyebrow="AI Görev Merkezi"
          title="Görev detayı"
        />

        <EmptyState
          description={result.message}
          title="Görev yüklenemedi"
        />
      </PageContent>
    );
  }

  const { task, version, taskReference, sourceModule, sourceEntityId } = result.detail;
  const { operations } = result;
  const teamLabel = task.assignedTeamChannelId
    ? operations.teamChannelLabels[task.assignedTeamChannelId] ?? "Ekip kanalı"
    : null;
  const slaCountdown = formatAiTaskSlaCountdown(
    task.sla?.resolutionDueAt
      ?? task.sla?.responseDueAt
      ?? task.sla?.acknowledgmentDueAt
      ?? task.dueAt,
  );

  return (
    <PageContent data-testid="ai-task-detail-page">
      <PageHeader
        description={task.summary}
        eyebrow={getAiTaskCategoryLabel(task.category)}
        title={task.title}
        actions={
          <div className="ai-task-detail__badges">
            <Badge tone={AI_TASK_STATUS_TONES[task.status]}>
              {getAiTaskStatusLabel(task.status)}
            </Badge>

            <Badge tone={AI_TASK_PRIORITY_TONES[task.priority]}>
              {getAiTaskPriorityLabel(task.priority)}
            </Badge>

            <Badge tone={AI_TASK_RISK_LEVEL_TONES[task.riskLevel]}>
              {getAiTaskRiskLevelLabel(task.riskLevel)}
            </Badge>
          </div>
        }
      />

      <AiTaskOperationsPanelClient
        operations={operations}
        task={task}
        teamChannelLabels={operations.teamChannelLabels}
        version={version}
      />

      <AiTaskActionPanelClient
        canReview={result.canReview}
        task={task}
        version={version}
      />

      <AiTaskSourceReferenceSection
        sourceEntityId={sourceEntityId}
        sourceModule={sourceModule}
        taskReference={taskReference}
        taskTitle={task.title}
        teamsMessageSourceLink={
          sourceModule === "teams" && sourceEntityId
            ? <AiTaskTeamsMessageSourceLink messageId={sourceEntityId} />
            : null
        }
      />

      <AiTaskTeamsContextSection taskId={taskId} />

      <AiTaskExecutionPanel task={task} />

      <AiTaskImpactPanel impact={task.impact} />

      <div className="ai-task-detail__summary-grid">
        <Card className="ai-task-detail__summary-card">
          <span>
            <Gauge aria-hidden="true" />
            AI güveni
          </span>

          <strong>
            {PERCENT_FORMATTER.format(task.confidence.score)}
          </strong>

          <small>
            {task.confidence.explanation}
          </small>
        </Card>

        <Card className="ai-task-detail__summary-card">
          <span>
            <UserRound aria-hidden="true" />
            Sorumlu
          </span>

          <strong>
            {task.assignee?.displayName ?? teamLabel ?? "Atanmadı"}
          </strong>

          <small
            title={getAiTaskActorRoleDescription(
              task.assignee?.role,
            )}
          >
            {teamLabel && !task.assignee
              ? `Ekip: ${teamLabel}`
              : getAiTaskActorRoleLabel(task.assignee?.role)}
          </small>
        </Card>

        <Card className="ai-task-detail__summary-card">
          <span>
            <CalendarClock aria-hidden="true" />
            SLA durumu
          </span>

          <strong>
            <Badge tone={AI_TASK_SLA_STATE_TONES[task.slaState]}>
              {getAiTaskSlaStateLabel(task.slaState)}
            </Badge>
          </strong>

          <small>
            {slaCountdown ?? "Aktif SLA hedefi tanımlanmadı"}
            {task.acknowledgedAtUtc
              ? ` · Teslim: ${formatDateTime(task.acknowledgedAtUtc)}`
              : ""}
          </small>
        </Card>

        <Card className="ai-task-detail__summary-card">
          <span>
            <CalendarClock aria-hidden="true" />
            Son tarih
          </span>

          <strong>
            {formatDateTime(task.dueAt)}
          </strong>

          <small>
            Eskalasyon: {getAiTaskEscalationLevelLabel(task.escalationLevel)}
            {" · "}
            {getAiTaskEscalationStateLabel(task.escalationState)}
          </small>
        </Card>

        <Card className="ai-task-detail__summary-card">
          <span>
            <CircleDollarSign aria-hidden="true" />
            Finansal etki
          </span>

          <strong>
            {formatMoney(
              task.impact.financialAmount,
              task.impact.currency,
            )}
          </strong>

          <small>
            {task.impact.summary}
          </small>
        </Card>
      </div>

      <Section
        action={
          <Badge tone="ai">
            <Bot aria-hidden="true" />
            {getAiTaskActionTypeLabel(
              task.recommendation.actionType,
            )}
          </Badge>
        }
        description="AI tarafından tespit edilen sorun, gerekçe ve önerilen aksiyon."
        title="AI değerlendirmesi"
      >
        <div className="ai-task-detail__analysis-grid">
          <Card>
            <h3>
              Tespit edilen sorun
            </h3>

            <p>
              {task.detectedIssue}
            </p>
          </Card>

          <Card>
            <h3>
              Analiz gerekçesi
            </h3>

            <p>
              {task.reasoning}
            </p>
          </Card>

          <Card className="ai-task-detail__recommendation-card">
            <h3>
              {task.recommendation.title}
            </h3>

            <p>
              {task.recommendation.description}
            </p>

            <dl>
              <div>
                <dt>
                  İnsan onayı
                </dt>
                <dd>
                  {getAiTaskApprovalStateLabel(
                    task.approval.state,
                  )}
                </dd>
              </div>

              <div>
                <dt>
                  Hedef sistem
                </dt>
                <dd
                  title={getAiTaskTargetSystemDescription(
                    task.recommendation.targetSystem,
                  )}
                >
                  {getAiTaskTargetSystemLabel(
                    task.recommendation.targetSystem,
                  )}
                </dd>
              </div>

              <div>
                <dt>
                  Geri alınabilir
                </dt>
                <dd>
                  {task.recommendation.reversible
                    ? "Evet"
                    : "Hayır"}
                </dd>
              </div>
            </dl>
          </Card>
        </div>
      </Section>

      <Section
        action={
          <Badge tone="neutral">
            <Database aria-hidden="true" />
            {task.sources.length} kaynak
          </Badge>
        }
        description="AI değerlendirmesinde kullanılan ERP, makine, IoT ve operasyon verileri."
        title="Kaynak veriler"
      >
        <div className="ai-task-detail__source-list">
          {task.sources.map((source, index) => (
            <Card
              className="ai-task-detail__source-card"
              key={`${source.sourceType}-${source.sourceId ?? index}`}
            >
              <div>
                <strong>
                  {source.sourceLabel}
                </strong>

                <Badge tone="neutral">
                  {getAiTaskSourceTypeLabel(source.sourceType)}
                </Badge>
              </div>

              <p>
                {source.valueSummary ?? "Değer özeti bulunmuyor"}
              </p>

              <small
                title={getAiTaskSourceTypeDescription(source.sourceType)}
              >
                Gözlem zamanı:{" "}
                {formatDateTime(source.observedAt)}
              </small>
            </Card>
          ))}
        </div>
      </Section>

      <Section
        action={
          <Badge tone="neutral">
            <History aria-hidden="true" />
            {task.auditTrail.length} kayıt
          </Badge>
        }
        description="Görevin oluşturulmasından son durumuna kadar değiştirilemez işlem geçmişi."
        title="Denetim geçmişi"
      >
        <div
          className="ai-task-detail__audit-list"
          data-testid="ai-task-audit-timeline"
        >
          {task.auditTrail.map((event) => (
            <Card
              className="ai-task-detail__audit-card"
              key={event.id}
            >
              <div>
                <strong>
                  {event.actorLabel}
                </strong>

                <time dateTime={event.occurredAt}>
                  {formatDateTime(event.occurredAt)}
                </time>
              </div>

              <p>
                {getAiTaskAuditEventLabel(event.eventType)}
              </p>

              {event.note ? (
                <small>
                  {event.note}
                </small>
              ) : getAiTaskAuditMetadataSummary(event.metadataJson) ? (
                <small>
                  {getAiTaskAuditMetadataSummary(event.metadataJson)}
                </small>
              ) : null}

              {getAiTaskAuditTransitionLabel(
                event,
                getAiTaskStatusLabel,
              ) ? (
                <span>
                  {getAiTaskAuditTransitionLabel(
                    event,
                    getAiTaskStatusLabel,
                  )}
                </span>
              ) : null}
            </Card>
          ))}
        </div>
      </Section>
    </PageContent>
  );
}
