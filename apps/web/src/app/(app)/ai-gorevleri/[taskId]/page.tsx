import { notFound } from "next/navigation";
import {
  Bot,
  Database,
  Gauge,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import { PageContent, Section } from "@/components/ui/page";
import { EmptyState } from "@/components/ui/states";
import { AiTaskClosurePanelClient } from "@/features/ai-task-center/components/ai-task-closure-panel-client";
import { AiTaskActionPanelClient } from "@/features/ai-task-center/components/ai-task-action-panel-client";
import { AiTaskDetailTimeline } from "@/features/ai-task-center/components/ai-task-detail-timeline";
import { AiTaskOperationsPanelClient } from "@/features/ai-task-center/components/ai-task-operations-panel-client";
import { AiTaskExecutionPanel } from "@/features/ai-task-center/components/ai-task-execution-panel";
import { AiTaskImpactPanel } from "@/features/ai-task-center/components/ai-task-impact-panel";
import { AiTaskSourceReferenceSection } from "@/features/ai-task-center/components/ai-task-source-reference-section";
import { AiTaskTeamsContextSection } from "@/features/teams/components/ai-task-teams-context-section";
import { AiTaskTeamsMessageSourceLink } from "@/features/teams/components/ai-task-teams-message-source-link";
import {
  getAiTaskSourceTypeDescription,
  getAiTaskSourceTypeLabel,
} from "@/features/ai-task-center/presentation/ai-task-source-labels";
import {
  AI_TASK_PRIORITY_TONES,
  AI_TASK_RISK_LEVEL_TONES,
  AI_TASK_STATUS_TONES,
  getAiTaskCategoryLabel,
  getAiTaskPriorityLabel,
  getAiTaskRiskLevelLabel,
  getAiTaskStatusLabel,
} from "@/features/ai-task-center/presentation/ai-task-labels";
import { readAiTaskDetail } from "@/features/ai-task-center/server/ai-task-queries";
import { AiTaskDetailOverviewAiCardClient } from "@/features/ai-task-center/components/ai-task-detail-overview-ai-card-client";
import { AiTaskDecisionIntelligencePanelClient } from "@/features/ai-task-center/components/ai-task-decision-intelligence-panel-client";
import { readAiTaskDecisionWorkspace } from "@/features/ai-task-center/server/ai-task-decision-intelligence-queries";
import {
  AI_TASK_ESCALATION_STATE_TONES,
  AI_TASK_SLA_STATE_TONES,
  getAiTaskEscalationBadgeLabel,
  getAiTaskSlaDisplayText,
} from "@/features/ai-task-center/presentation/ai-task-sla-labels";
import type { AiTask } from "@/features/ai-task-center/domain/ai-task";
import { formatAiTaskShortDateTime } from "@/features/ai-task-center/presentation/ai-task-formatters";

type PageParams = Readonly<{
  taskId: string;
}>;

const PERCENT_FORMATTER = new Intl.NumberFormat("tr-TR", {
  style: "percent",
  maximumFractionDigits: 0,
});

function buildContextChips(
  taskReference: string,
  task: AiTask,
): readonly string[] {
  const chips: string[] = [taskReference];

  if (task.productionOrderId) {
    chips.push(`Üretim emri: ${task.productionOrderId}`);
  }

  if (task.machineId) {
    chips.push(`Makine: ${task.machineId}`);
  }

  if (task.moldId) {
    chips.push(`Kalıp: ${task.moldId}`);
  }

  if (task.materialId) {
    chips.push(`Malzeme: ${task.materialId}`);
  }

  if (task.salesOrderId) {
    chips.push(`Sipariş: ${task.salesOrderId}`);
  }

  return chips;
}

export default async function AiTaskDetailPage({
  params,
}: Readonly<{
  params: Promise<PageParams>;
}>) {
  const { taskId } = await params;
  const [result, decisionResult] = await Promise.all([
    readAiTaskDetail(taskId),
    readAiTaskDecisionWorkspace(taskId),
  ]);

  if (result.status === "failure") {
    if (result.code === "not-found") {
      notFound();
    }

    return (
      <PageContent>
        <EmptyState
          description={result.message}
          title="Görev yüklenemedi"
        />
      </PageContent>
    );
  }

  const { task, version, taskReference, sourceModule, sourceEntityId } = result.detail;
  const { operations } = result;
  const slaDueAt = task.sla?.resolutionDueAt
    ?? task.sla?.responseDueAt
    ?? task.sla?.acknowledgmentDueAt
    ?? task.dueAt;
  const slaDisplay = getAiTaskSlaDisplayText(task.slaState, slaDueAt);
  const escalationBadge = task.escalationLevel > 0
    ? getAiTaskEscalationBadgeLabel(task.escalationLevel)
    : null;
  const contextChips = buildContextChips(taskReference, task);

  return (
    <PageContent data-testid="ai-task-detail-page">
      <header className="ai-task-detail-header">
        <div className="ai-task-detail-header__main">
          <p className="ai-task-detail-header__eyebrow">
            {getAiTaskCategoryLabel(task.category)}
          </p>
          <h1 className="ai-task-detail-header__title">{task.title}</h1>
          <p className="ai-task-detail-header__summary">{task.summary}</p>
          <div className="ai-task-detail-header__chips">
            {contextChips.map((chip) => (
              <span className="ai-task-detail-header__chip" key={chip}>
                {chip}
              </span>
            ))}
          </div>
        </div>

        <div className="ai-task-detail-header__badges">
          <Badge tone={AI_TASK_STATUS_TONES[task.status]}>
            {getAiTaskStatusLabel(task.status)}
          </Badge>
          <Badge tone={AI_TASK_PRIORITY_TONES[task.priority]}>
            {getAiTaskPriorityLabel(task.priority)}
          </Badge>
          <Badge tone={AI_TASK_RISK_LEVEL_TONES[task.riskLevel]}>
            {getAiTaskRiskLevelLabel(task.riskLevel)}
          </Badge>
          {slaDisplay ? (
            <Badge tone={AI_TASK_SLA_STATE_TONES[task.slaState]}>
              {slaDisplay}
            </Badge>
          ) : null}
          {escalationBadge && task.escalationState === "active" ? (
            <Badge tone={AI_TASK_ESCALATION_STATE_TONES[task.escalationState]}>
              {escalationBadge}
            </Badge>
          ) : null}
        </div>
      </header>

      <div className="ai-task-detail-workspace">
        <div className="ai-task-detail-workspace__main">
          <Section
            description="Görev özeti, AI değerlendirmesi ve operasyonel bağlam."
            title="Görev genel bakış"
          >
            <div className="ai-task-detail-overview">
              <Card className="ai-task-detail-overview__card">
                <div className="ai-task-detail-overview__metric">
                  <Gauge aria-hidden="true" />
                  <div>
                    <span>AI güveni</span>
                    <strong>{PERCENT_FORMATTER.format(task.confidence.score)}</strong>
                  </div>
                </div>
                <p>{task.confidence.explanation}</p>
              </Card>

              <AiTaskDetailOverviewAiCardClient
                latestAnalysis={
                  decisionResult.status === "success"
                    ? decisionResult.workspace.latestAnalysis
                    : null
                }
                task={task}
              />
            </div>
          </Section>

          {decisionResult.status === "success" ? (
            <Section
              description="AI triage, SLA riski, ilişkili görevler ve sonraki aksiyon önerileri."
              title="Karar desteği"
            >
              <AiTaskDecisionIntelligencePanelClient
                capabilities={decisionResult.capabilities}
                task={task}
                taskId={taskId}
                taskVersion={version}
                workspace={decisionResult.workspace}
              />
            </Section>
          ) : null}

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

          <AiTaskActionPanelClient
            canReview={result.canReview}
            task={task}
            version={version}
          />

          <AiTaskClosurePanelClient
            operations={operations}
            task={task}
            version={version}
          />

          <AiTaskImpactPanel impact={task.impact} />

          <AiTaskExecutionPanel task={task} />

          {task.sources.length > 0 ? (
            <Section
              action={
                <Badge tone="neutral">
                  <Database aria-hidden="true" />
                  {task.sources.length} kaynak
                </Badge>
              }
              description="AI değerlendirmesinde kullanılan operasyon verileri."
              title="Kaynak veriler"
            >
              <div className="ai-task-detail__source-list">
                {task.sources.map((source, index) => (
                  <Card
                    className="ai-task-detail__source-card"
                    key={`${source.sourceType}-${source.sourceId ?? index}`}
                  >
                    <div>
                      <strong>{source.sourceLabel}</strong>
                      <Badge tone="neutral">
                        {getAiTaskSourceTypeLabel(source.sourceType)}
                      </Badge>
                    </div>
                    <p>{source.valueSummary ?? "Değer özeti bulunmuyor"}</p>
                    <small title={getAiTaskSourceTypeDescription(source.sourceType)}>
                      Gözlem: {source.observedAt
                        ? formatAiTaskShortDateTime(source.observedAt)
                        : "Tanımlanmadı"}
                    </small>
                  </Card>
                ))}
              </div>
            </Section>
          ) : null}

          <Section
            action={
              <Badge tone="neutral">
                <Bot aria-hidden="true" />
                {task.auditTrail.length} kayıt
              </Badge>
            }
            description="Görevin oluşturulmasından son durumuna kadar değiştirilemez işlem geçmişi."
            title="Aktivite zaman çizelgesi"
          >
            <AiTaskDetailTimeline events={task.auditTrail} />
          </Section>
        </div>

        <AiTaskOperationsPanelClient
          operations={operations}
          task={task}
          teamChannelLabels={operations.teamChannelLabels}
          version={version}
        />
      </div>
    </PageContent>
  );
}
