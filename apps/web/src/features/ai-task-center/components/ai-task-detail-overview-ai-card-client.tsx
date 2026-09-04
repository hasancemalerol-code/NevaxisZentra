"use client";

import { useId, useState } from "react";
import { ChevronDown, Sparkles } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";

import type { AiTask } from "../domain/ai-task";
import type { AiTaskDecisionAnalysis } from "../domain/ai-task-decision-intelligence";
import { formatAiTaskShortDateTime } from "../presentation/ai-task-formatters";
import {
  getAiTaskActionTypeLabel,
  getAiTaskApprovalStateLabel,
} from "../presentation/ai-task-labels";
import {
  getAiTaskTargetSystemDescription,
  getAiTaskTargetSystemLabel,
} from "../presentation/ai-task-target-system-labels";
import {
  getAiTaskDecisionConfidenceLabel,
} from "../presentation/ai-task-decision-intelligence-labels";
import { localizeAiTaskFixtureText } from "../presentation/ai-task-decision-intelligence-presentation";

type AiTaskDetailOverviewAiCardClientProps = Readonly<{
  task: AiTask;
  latestAnalysis?: AiTaskDecisionAnalysis | null;
}>;

export function AiTaskDetailOverviewAiCardClient({
  task,
  latestAnalysis,
}: AiTaskDetailOverviewAiCardClientProps) {
  const [inspectionOpen, setInspectionOpen] = useState(false);
  const inspectionId = useId();
  const pendingRecommendation = latestAnalysis?.recommendations.find(
    (item) => item.status === "pending",
  );

  const confidenceLabel = latestAnalysis?.confidenceBand
    ? getAiTaskDecisionConfidenceLabel(latestAnalysis.confidenceBand)
    : `${Math.round(task.confidence.score * 100)}%`;

  return (
    <Card className="ai-task-detail-overview__card ai-task-detail-overview__card--ai">
      <div className="ai-task-detail-overview__heading">
        <span>
          <Sparkles aria-hidden="true" />
          AI değerlendirmesi
        </span>
        <div className="ai-task-detail-overview__heading-actions">
          <Badge tone="ai">
            {getAiTaskActionTypeLabel(task.recommendation.actionType)}
          </Badge>
          <Button
            aria-controls={inspectionId}
            aria-expanded={inspectionOpen}
            className="ai-task-detail-overview__inspect-button"
            data-testid="ai-task-overview-inspect-button"
            leadingIcon={<ChevronDown className={inspectionOpen ? "ai-task-detail-overview__inspect-icon--open" : undefined} />}
            type="button"
            variant="secondary"
            onClick={() => setInspectionOpen((open) => !open)}
          >
            İncele
          </Button>
        </div>
      </div>

      <h3>{localizeAiTaskFixtureText(task.recommendation.title)}</h3>
      <p>{localizeAiTaskFixtureText(task.recommendation.description)}</p>

      <div
        className={`ai-task-detail-overview__inspection${inspectionOpen ? " ai-task-detail-overview__inspection--open" : ""}`}
        data-testid="ai-task-decision-inspection"
        hidden={!inspectionOpen}
        id={inspectionId}
      >
        <dl className="ai-task-detail-overview__facts">
          <div>
            <dt>Tespit</dt>
            <dd>{localizeAiTaskFixtureText(task.detectedIssue)}</dd>
          </div>
          <div>
            <dt>Gerekçe</dt>
            <dd>{localizeAiTaskFixtureText(task.reasoning)}</dd>
          </div>
          <div>
            <dt>Onay durumu</dt>
            <dd>{getAiTaskApprovalStateLabel(task.approval.state)}</dd>
          </div>
          <div>
            <dt>Hedef sistem</dt>
            <dd title={getAiTaskTargetSystemDescription(task.recommendation.targetSystem)}>
              {getAiTaskTargetSystemLabel(task.recommendation.targetSystem)}
            </dd>
          </div>
          <div>
            <dt>Güven</dt>
            <dd>{confidenceLabel}</dd>
          </div>
          {latestAnalysis ? (
            <div>
              <dt>Analiz zamanı</dt>
              <dd>{formatAiTaskShortDateTime(latestAnalysis.createdAtUtc)}</dd>
            </div>
          ) : null}
          {pendingRecommendation ? (
            <div>
              <dt>İlgili öneri</dt>
              <dd>{localizeAiTaskFixtureText(pendingRecommendation.title)}</dd>
            </div>
          ) : null}
        </dl>

        {latestAnalysis ? (
          <dl className="ai-task-detail-overview__facts ai-task-detail-overview__facts--meta">
            <div>
              <dt>Sağlayıcı</dt>
              <dd>{latestAnalysis.providerName}</dd>
            </div>
            <div>
              <dt>Model</dt>
              <dd>{latestAnalysis.modelName}</dd>
            </div>
          </dl>
        ) : null}
      </div>
    </Card>
  );
}
