"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState, useTransition } from "react";
import {
  AlertTriangle,
  Bot,
  Check,
  ExternalLink,
  GitBranch,
  RefreshCw,
  Shield,
  Sparkles,
  X,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";

import type { AiTask } from "../domain/ai-task";
import type {
  AiTaskDecisionCapabilities,
  AiTaskDecisionRelatedCandidate,
  AiTaskDecisionRelationship,
  AiTaskDecisionWorkspace,
} from "../domain/ai-task-decision-intelligence";
import {
  getAiTaskDecisionConfidenceLabel,
  getAiTaskDecisionImpactLabel,
  getAiTaskDecisionRelationshipLabel,
  getAiTaskDecisionSlaRiskLabel,
  getAiTaskDecisionUrgencyLabel,
} from "../presentation/ai-task-decision-intelligence-labels";
import {
  buildClosureReviewView,
  buildDecisionAssessmentSummary,
  buildPolicyComparisonView,
  buildRelatedCandidateDisplay,
  getAiTaskDecisionActionTypeLabel,
  getAiTaskDecisionDepartmentLabel,
  getAiTaskDecisionRelationshipStatusLabel,
  localizeContributingFactor,
  localizeAiTaskFixtureText,
} from "../presentation/ai-task-decision-intelligence-presentation";
import type { AiTaskPriority, AiTaskStatus } from "../domain/ai-task";
import { getAiTaskPriorityLabel, getAiTaskStatusLabel } from "../presentation/ai-task-labels";
import { resolveAutomationTeamChannelLabel } from "../presentation/ai-task-automation-entity-lookup";
import {
  acceptAiTaskRecommendationAction,
  analyzeAiTaskDecisionAction,
  confirmAiTaskRelationshipAction,
  rejectAiTaskRecommendationAction,
  reviewAiTaskClosureAction,
} from "../server/ai-task-decision-intelligence-actions";

type AiTaskDecisionIntelligencePanelClientProps = Readonly<{
  taskId: string;
  task: AiTask;
  taskVersion: number;
  workspace: AiTaskDecisionWorkspace;
  capabilities: AiTaskDecisionCapabilities;
}>;

function confidenceTone(band: string): "success" | "warning" | "information" | "neutral" | "ai" {
  if (band === "high") return "success";
  if (band === "medium") return "information";
  if (band === "low") return "warning";
  return "neutral";
}

function slaRiskTone(band: string): "success" | "warning" | "information" | "neutral" | "danger" {
  if (band === "critical") return "danger";
  if (band === "high") return "warning";
  if (band === "watch") return "information";
  return "neutral";
}

function closureReadinessTone(readiness: string): "success" | "warning" | "information" | "neutral" | "danger" {
  if (readiness === "ready") return "success";
  if (readiness === "risk-detected") return "danger";
  if (readiness === "review-needed") return "warning";
  return "neutral";
}

export function AiTaskDecisionIntelligencePanelClient({
  taskId,
  task,
  taskVersion,
  workspace: initialWorkspace,
  capabilities,
}: AiTaskDecisionIntelligencePanelClientProps) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [error, setError] = useState<string | null>(null);
  const [currentVersion, setCurrentVersion] = useState(taskVersion);

  const triage = initialWorkspace.latestAnalysis?.triage;
  const pendingRecommendations = initialWorkspace.latestAnalysis?.recommendations.filter(
    (item) => item.status === "pending",
  ) ?? [];
  const policyView = buildPolicyComparisonView(task, triage);
  const assessmentSummary = triage
    ? buildDecisionAssessmentSummary(triage, task.category)
    : null;
  const contributingFactors = triage?.contributingFactors.map(localizeContributingFactor) ?? [];

  function refresh() {
    startTransition(() => router.refresh());
  }

  async function runAnalysis() {
    setError(null);
    const result = await analyzeAiTaskDecisionAction(
      taskId,
      `${taskId}-triage-${Date.now()}`,
      "triage",
      "user-request",
    );
    if (result.status === "failure") {
      setError(result.message);
      return;
    }
    refresh();
  }

  async function runClosureReview() {
    setError(null);
    const result = await reviewAiTaskClosureAction(
      taskId,
      `${taskId}-closure-${Date.now()}`,
    );
    if (result.status === "failure") {
      setError(result.message);
      return;
    }
    refresh();
  }

  async function handleAccept(recommendationId: string) {
    setError(null);
    const result = await acceptAiTaskRecommendationAction(
      taskId,
      recommendationId,
      currentVersion,
    );
    if (result.status === "failure") {
      setError(result.message);
      return;
    }
    setCurrentVersion(result.taskVersion);
    refresh();
  }

  async function handleReject(recommendationId: string) {
    setError(null);
    const result = await rejectAiTaskRecommendationAction(
      taskId,
      recommendationId,
      "Kullanıcı tarafından reddedildi",
    );
    if (result.status === "failure") {
      setError(result.message);
      return;
    }
    setCurrentVersion(result.taskVersion);
    refresh();
  }

  async function handleRelationship(
    candidate: AiTaskDecisionRelatedCandidate,
    status: string,
  ) {
    setError(null);
    const result = await confirmAiTaskRelationshipAction(
      taskId,
      candidate.candidateTaskId,
      candidate.relationshipType,
      status,
      candidate.explanation,
    );
    if (result.status === "failure") {
      setError(result.message);
      return;
    }
    refresh();
  }

  if (!initialWorkspace.providerAvailable) {
    return (
      <Card className="ai-task-decision-panel ai-task-decision-panel--unavailable" data-testid="ai-task-decision-panel">
        <div className="ai-task-decision-panel__header">
          <Sparkles aria-hidden="true" />
          <h3>AI Değerlendirmesi</h3>
        </div>
        <p>{initialWorkspace.providerStatusMessage ?? "AI değerlendirmesi şu anda kullanılamıyor."}</p>
      </Card>
    );
  }

  return (
    <div className="ai-task-decision-panel" data-testid="ai-task-decision-panel">
      {error ? <p className="ai-task-decision-panel__error" role="alert">{error}</p> : null}

      <Card className="ai-task-decision-panel__card" data-testid="ai-task-decision-triage">
        <div className="ai-task-decision-panel__header">
          <Sparkles aria-hidden="true" />
          <h3>AI Değerlendirmesi</h3>
          {capabilities.canAnalyze ? (
            <Button
              disabled={isPending}
              leadingIcon={<RefreshCw />}
              type="button"
              variant="secondary"
              onClick={runAnalysis}
            >
              Analiz et
            </Button>
          ) : null}
        </div>

        {triage ? (
          <>
            <div className="ai-task-decision-panel__metrics">
              <div className="ai-task-decision-panel__metric">
                <span>Önerilen öncelik</span>
                <strong>{getAiTaskPriorityLabel((triage.suggestedPriority ?? "medium") as "low" | "medium" | "high" | "critical")}</strong>
              </div>
              <div className="ai-task-decision-panel__metric">
                <span>Etki</span>
                <strong>{getAiTaskDecisionImpactLabel(triage.operationalImpactBand)}</strong>
              </div>
              <div className="ai-task-decision-panel__metric">
                <span>SLA riski</span>
                <Badge tone={slaRiskTone(triage.slaBreachRiskBand)}>
                  {getAiTaskDecisionSlaRiskLabel(triage.slaBreachRiskBand)}
                </Badge>
              </div>
              <div className="ai-task-decision-panel__metric">
                <span>Önerilen ekip</span>
                <strong>
                  {triage.suggestedTeamChannelId
                    ? resolveAutomationTeamChannelLabel(triage.suggestedTeamChannelId)
                    : getAiTaskDecisionDepartmentLabel(triage.suggestedDepartment)}
                </strong>
              </div>
              <div className="ai-task-decision-panel__metric">
                <span>Güven</span>
                <Badge tone={confidenceTone(triage.confidenceBand)}>
                  {getAiTaskDecisionConfidenceLabel(triage.confidenceBand)}
                </Badge>
              </div>
            </div>

            <div className="ai-task-decision-panel__section-block">
              <p className="ai-task-decision-panel__section-label">Neden?</p>
              <p className="ai-task-decision-panel__rationale">{assessmentSummary}</p>
            </div>

            {contributingFactors.length > 0 ? (
              <div className="ai-task-decision-panel__section-block">
                <p className="ai-task-decision-panel__section-label">Etkileyen faktörler</p>
                <div className="ai-task-decision-panel__chips">
                  {contributingFactors.map((factor) => (
                    <Badge key={factor} tone="neutral">{factor}</Badge>
                  ))}
                </div>
              </div>
            ) : null}
          </>
        ) : (
          <p className="ai-task-decision-panel__muted">Henüz AI analizi çalıştırılmadı.</p>
        )}
      </Card>

      {policyView ? (
        <Card className="ai-task-decision-panel__card ai-task-decision-panel__card--policy" data-testid="ai-task-decision-policy-vs-ai">
          <p className="ai-task-decision-panel__policy-banner">
            Politika sonucu bağlayıcıdır; AI değerlendirmesi tavsiye niteliğindedir.
          </p>
          <div className="ai-task-decision-panel__split">
            <div className="ai-task-decision-panel__compare-column">
              <div className="ai-task-decision-panel__subheader">
                <Shield aria-hidden="true" />
                <h4>Otomasyon Politikası</h4>
              </div>
              <dl className="ai-task-decision-panel__compare-rows">
                {policyView.policyRows.map((row) => (
                  <div key={row.label}>
                    <dt>{row.label}</dt>
                    <dd>{row.value}</dd>
                  </div>
                ))}
              </dl>
            </div>
            <div className="ai-task-decision-panel__compare-column">
              <div className="ai-task-decision-panel__subheader">
                <Bot aria-hidden="true" />
                <h4>AI Değerlendirmesi</h4>
              </div>
              <dl className="ai-task-decision-panel__compare-rows">
                {policyView.aiRows.map((row) => (
                  <div key={row.label}>
                    <dt>{row.label}</dt>
                    <dd>{row.value}</dd>
                  </div>
                ))}
              </dl>
            </div>
          </div>
          {policyView.differences.length > 0 ? (
            <div className="ai-task-decision-panel__differences">
              {policyView.differences.map((diff) => (
                <p key={diff} className="ai-task-decision-panel__diff">{diff}</p>
              ))}
            </div>
          ) : null}
        </Card>
      ) : null}

      {pendingRecommendations.length > 0 ? (
        <Card className="ai-task-decision-panel__card" data-testid="ai-task-decision-next-actions">
          <div className="ai-task-decision-panel__header">
            <AlertTriangle aria-hidden="true" />
            <h3>Önerilen Sonraki Aksiyonlar</h3>
          </div>
          <div className="ai-task-decision-panel__actions">
            {pendingRecommendations.map((recommendation) => (
              <article key={recommendation.id} className="ai-task-decision-panel__action-item">
                <div className="ai-task-decision-panel__action-content">
                  <strong>{localizeAiTaskFixtureText(recommendation.title)}</strong>
                  <p>{localizeAiTaskFixtureText(recommendation.rationale)}</p>
                  <div className="ai-task-decision-panel__chips">
                    <Badge tone={confidenceTone(recommendation.confidenceBand)}>
                      Güven: {getAiTaskDecisionConfidenceLabel(recommendation.confidenceBand)}
                    </Badge>
                    <Badge tone="neutral">
                      Aciliyet: {getAiTaskDecisionUrgencyLabel(recommendation.urgencyBand)}
                    </Badge>
                    <Badge tone="information">
                      {getAiTaskDecisionActionTypeLabel(recommendation.actionType)}
                    </Badge>
                  </div>
                </div>
                {capabilities.canFeedback ? (
                  <div className="ai-task-decision-panel__action-buttons">
                    <Button
                      disabled={isPending}
                      leadingIcon={<Check />}
                      type="button"
                      variant="secondary"
                      onClick={() => handleAccept(recommendation.id)}
                    >
                      Uygula
                    </Button>
                    <Button
                      disabled={isPending}
                      leadingIcon={<X />}
                      type="button"
                      variant="quiet"
                      onClick={() => handleReject(recommendation.id)}
                    >
                      Reddet
                    </Button>
                  </div>
                ) : null}
              </article>
            ))}
          </div>
        </Card>
      ) : null}

      {(initialWorkspace.suggestedRelated.length > 0 || initialWorkspace.relationships.length > 0) ? (
        <Card className="ai-task-decision-panel__card" data-testid="ai-task-decision-related">
          <div className="ai-task-decision-panel__header">
            <GitBranch aria-hidden="true" />
            <h3>Benzer / ilişkili görevler</h3>
          </div>
          <div className="ai-task-decision-panel__related-list">
            {initialWorkspace.suggestedRelated.map((candidate) => (
              <RelatedCandidateRow
                key={candidate.candidateTaskId}
                candidate={candidate}
                canFeedback={capabilities.canFeedback}
                isPending={isPending}
                onConfirm={(status) => handleRelationship(candidate, status)}
              />
            ))}
            {initialWorkspace.relationships.map((relationship) => (
              <ConfirmedRelationshipRow key={relationship.id} relationship={relationship} />
            ))}
          </div>
        </Card>
      ) : null}

      {initialWorkspace.latestAnalysis?.closureReview || capabilities.canAnalyze ? (
        <Card className="ai-task-decision-panel__card" data-testid="ai-task-decision-closure">
          <div className="ai-task-decision-panel__header">
            <Shield aria-hidden="true" />
            <h3>Kapanış hazırlığı</h3>
            {capabilities.canAnalyze ? (
              <Button disabled={isPending} type="button" variant="secondary" onClick={runClosureReview}>
                Kapanış analizi çalıştır
              </Button>
            ) : null}
          </div>
          {initialWorkspace.latestAnalysis?.closureReview ? (
            <ClosureReviewBody review={initialWorkspace.latestAnalysis.closureReview} />
          ) : (
            <p className="ai-task-decision-panel__muted">Kapanış incelemesi henüz çalıştırılmadı.</p>
          )}
        </Card>
      ) : null}

      {triage?.evidenceRecommendations.length ? (
        <Card className="ai-task-decision-panel__card ai-task-decision-panel__card--compact">
          <div className="ai-task-decision-panel__header">
            <h3>Kanıt önerileri</h3>
          </div>
          <div className="ai-task-decision-panel__evidence-list">
            {triage.evidenceRecommendations.map((item) => (
              <div key={item.title} className="ai-task-decision-panel__evidence-item">
                <Badge tone={item.policyMandatory ? "warning" : "ai"}>
                  {item.policyMandatory ? "POLİTİKA GEREĞİ ZORUNLU" : "AI ÖNERİSİ"}
                </Badge>
                <strong>{item.title}</strong>
                <p>{item.rationale}</p>
              </div>
            ))}
          </div>
        </Card>
      ) : null}
    </div>
  );
}

function ClosureReviewBody({
  review,
}: Readonly<{ review: NonNullable<AiTaskDecisionWorkspace["latestAnalysis"]>["closureReview"] }>) {
  if (!review) return null;

  const view = buildClosureReviewView(review);

  return (
    <div className="ai-task-decision-panel__closure">
      <div className="ai-task-decision-panel__closure-status">
        <span>Durum</span>
        <Badge tone={closureReadinessTone(review.closureReadiness)}>
          {view.readinessLabel}
        </Badge>
      </div>
      <p className="ai-task-decision-panel__rationale">{view.explanation}</p>
      {view.concerns.length > 0 ? (
        <div className="ai-task-decision-panel__section-block">
          <p className="ai-task-decision-panel__section-label">Endişeler</p>
          <div className="ai-task-decision-panel__chips">
            {view.concerns.map((concern) => (
              <Badge key={concern} tone="warning">{concern}</Badge>
            ))}
          </div>
        </div>
      ) : null}
      {view.recommendations.length > 0 ? (
        <div className="ai-task-decision-panel__section-block">
          <p className="ai-task-decision-panel__section-label">Önerilen takip</p>
          <div className="ai-task-decision-panel__chips">
            {view.recommendations.map((item) => (
              <Badge key={item} tone="information">{item}</Badge>
            ))}
          </div>
        </div>
      ) : null}
      <div className="ai-task-decision-panel__chips">
        <Badge tone={confidenceTone(review.confidenceBand)}>
          Güven: {view.confidenceLabel}
        </Badge>
      </div>
    </div>
  );
}

function RelatedCandidateRow({
  candidate,
  canFeedback,
  isPending,
  onConfirm,
}: Readonly<{
  candidate: AiTaskDecisionRelatedCandidate;
  canFeedback: boolean;
  isPending: boolean;
  onConfirm: (status: string) => void;
}>) {
  const display = buildRelatedCandidateDisplay(candidate);

  return (
    <article className="ai-task-decision-panel__related-item">
      <div className="ai-task-decision-panel__related-content">
        <div className="ai-task-decision-panel__related-title-row">
          <strong>{display.title}</strong>
          <Link className="ai-task-decision-panel__related-link" href={`/ai-gorevleri/${candidate.candidateTaskId}`}>
            <ExternalLink aria-hidden="true" />
            Görevi aç
          </Link>
        </div>
        <div className="ai-task-decision-panel__chips">
          {display.status ? <Badge tone="neutral">Durum: {display.status}</Badge> : null}
          {display.priority ? <Badge tone="information">Öncelik: {display.priority}</Badge> : null}
          <Badge tone="neutral">{getAiTaskDecisionRelationshipLabel(candidate.relationshipType)}</Badge>
          <Badge tone={confidenceTone(candidate.confidenceBand)}>
            Güven: {getAiTaskDecisionConfidenceLabel(candidate.confidenceBand)}
          </Badge>
        </div>
        <p className="ai-task-decision-panel__related-reason">
          <span>Neden benzer?</span> {display.similarityReason}
        </p>
      </div>
      {canFeedback ? (
        <div className="ai-task-decision-panel__action-buttons">
          <Button disabled={isPending} type="button" variant="quiet" onClick={() => onConfirm("confirmed")}>
            İlişkili olarak işaretle
          </Button>
          <Button disabled={isPending} type="button" variant="quiet" onClick={() => onConfirm("marked-duplicate")}>
            Yinelenen olarak işaretle
          </Button>
          <Button disabled={isPending} type="button" variant="quiet" onClick={() => onConfirm("rejected")}>
            Öneriyi reddet
          </Button>
        </div>
      ) : null}
    </article>
  );
}

function ConfirmedRelationshipRow({
  relationship,
}: Readonly<{ relationship: AiTaskDecisionRelationship }>) {
  return (
    <article className="ai-task-decision-panel__related-item ai-task-decision-panel__related-item--confirmed">
      <div className="ai-task-decision-panel__related-content">
        <div className="ai-task-decision-panel__related-title-row">
          <strong>{relationship.targetTaskTitle}</strong>
          <Link className="ai-task-decision-panel__related-link" href={`/ai-gorevleri/${relationship.targetTaskId}`}>
            <ExternalLink aria-hidden="true" />
            Görevi aç
          </Link>
        </div>
        <div className="ai-task-decision-panel__chips">
          <Badge tone="success">{getAiTaskDecisionRelationshipStatusLabel(relationship.status)}</Badge>
          <Badge tone="neutral">Durum: {getAiTaskStatusLabel(relationship.targetTaskStatus as AiTaskStatus)}</Badge>
          <Badge tone="information">
            Öncelik: {getAiTaskPriorityLabel(relationship.targetTaskPriority as AiTaskPriority)}
          </Badge>
          <Badge tone="neutral">{getAiTaskDecisionRelationshipLabel(relationship.relationshipType)}</Badge>
        </div>
        {relationship.explanation ? (
          <p className="ai-task-decision-panel__related-reason">
            <span>Neden benzer?</span> {relationship.explanation}
          </p>
        ) : null}
      </div>
    </article>
  );
}
