"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState, useTransition } from "react";
import {
  AlertTriangle,
  Ban,
  CheckCircle2,
  CircleDashed,
  Clock3,
  ExternalLink,
  RefreshCw,
  SkipForward,
  Unlock,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";

import type { AiTaskPlaybookRunDetail } from "../domain/ai-task-playbook";
import { formatAiTaskShortDateTime } from "../presentation/ai-task-formatters";
import {
  formatAiTaskPlaybookProgress,
  getAiTaskPlaybookDependencyStateLabel,
  getAiTaskPlaybookRunStatusLabel,
  getAiTaskPlaybookRunStatusTone,
  getAiTaskPlaybookRunStepStatusLabel,
  getAiTaskPlaybookRunStepStatusTone,
  getAiTaskPlaybookRunStepVisualStateClass,
  getAiTaskPlaybookStepTypeLabel,
  getAiTaskPlaybookTriggerSourceLabel,
} from "../presentation/ai-task-playbook-labels";
import {
  blockAiTaskPlaybookRunStepAction,
  cancelAiTaskPlaybookRunAction,
  skipAiTaskPlaybookRunStepAction,
  unblockAiTaskPlaybookRunStepAction,
} from "../server/ai-task-playbook-actions";
import type { AiTaskPlaybookCapabilities } from "../server/ai-task-playbook-queries";
import { AiTaskDetailField, AiTaskDetailInput } from "./ai-task-detail-form-controls";
import { customerMessage } from "@/lib/presentation/customer-message";

type AiTaskPlaybookRunDetailClientProps = Readonly<{
  run: AiTaskPlaybookRunDetail;
  capabilities: AiTaskPlaybookCapabilities;
}>;

function StepIcon({ status }: Readonly<{ status: string }>) {
  switch (status) {
    case "completed":
      return <CheckCircle2 aria-hidden="true" />;
    case "in-progress":
      return <Clock3 aria-hidden="true" />;
    case "blocked":
      return <Ban aria-hidden="true" />;
    case "awaiting-approval":
      return <AlertTriangle aria-hidden="true" />;
    default:
      return <CircleDashed aria-hidden="true" />;
  }
}

export function AiTaskPlaybookRunDetailClient({
  run,
  capabilities,
}: AiTaskPlaybookRunDetailClientProps) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [actionError, setActionError] = useState<string | null>(null);
  const [cancelReason, setCancelReason] = useState("");
  const [stepReason, setStepReason] = useState("");
  const [selectedStepId, setSelectedStepId] = useState<string | null>(null);

  const sortedSteps = [...run.steps].sort((a, b) => a.sortOrder - b.sortOrder);
  const isActive = !["completed", "cancelled", "failed"].includes(run.status);

  function refresh() {
    startTransition(() => {
      router.refresh();
    });
  }

  async function handleCancel() {
    if (!cancelReason.trim()) {
      setActionError("İptal nedeni girin.");
      return;
    }
    setActionError(null);
    const result = await cancelAiTaskPlaybookRunAction(run.id, cancelReason.trim());
    if (result.status === "failure") {
      setActionError(result.message);
      return;
    }
    refresh();
  }

  async function handleStepAction(
    action: "block" | "unblock" | "skip",
    stepId: string,
  ) {
    if (!stepReason.trim()) {
      setActionError("İşlem nedeni girin.");
      return;
    }
    setActionError(null);
    const reason = stepReason.trim();
    const result =
      action === "block"
        ? await blockAiTaskPlaybookRunStepAction(run.id, stepId, reason)
        : action === "unblock"
          ? await unblockAiTaskPlaybookRunStepAction(run.id, stepId, reason)
          : await skipAiTaskPlaybookRunStepAction(run.id, stepId, reason);

    if (result.status === "failure") {
      setActionError(result.message);
      return;
    }
    setStepReason("");
    setSelectedStepId(null);
    refresh();
  }

  return (
    <div className="ai-task-playbook-run-detail" data-testid="ai-task-playbook-run-detail">
      {actionError ? (
        <p className="ai-task-playbook-run-detail__error" role="alert">{customerMessage(actionError)}</p>
      ) : null}

      <Card className="ai-task-playbook-run-detail__overview">
        <div className="ai-task-playbook-run-detail__header">
          <div>
            <p className="ai-task-playbook-module__eyebrow">Çalışma</p>
            <h2>{run.playbookName}</h2>
            <p className="ai-task-playbook-run-detail__meta">
              Sürüm v{run.playbookVersion}
              {" · "}
              {getAiTaskPlaybookTriggerSourceLabel(run.triggerSource)}
              {" · "}
              {formatAiTaskShortDateTime(run.startedAtUtc)}
            </p>
          </div>
          <Badge tone={getAiTaskPlaybookRunStatusTone(run.status)}>
            {getAiTaskPlaybookRunStatusLabel(run.status)}
          </Badge>
        </div>

        <div className="ai-task-playbook-run-detail__metrics">
          <div className="ai-task-playbook-run-detail__metric">
            <span>Zorunlu adımlar</span>
            <strong>{formatAiTaskPlaybookProgress(run.requiredStepsCompleted, run.requiredStepsTotal)}</strong>
          </div>
          <div className="ai-task-playbook-run-detail__metric">
            <span>İsteğe bağlı</span>
            <strong>{formatAiTaskPlaybookProgress(run.optionalStepsCompleted, run.optionalStepsTotal)}</strong>
          </div>
          <div className="ai-task-playbook-run-detail__metric">
            <span>Bloke</span>
            <strong>{run.blockedStepsCount}</strong>
          </div>
          <div className="ai-task-playbook-run-detail__metric">
            <span>Onay bekleyen</span>
            <strong>{run.approvalsPending}</strong>
          </div>
        </div>

        {run.atRisk ? (
          <p className="ai-task-playbook-run-detail__risk">Bu çalışma risk altında işaretlenmiş.</p>
        ) : null}

        {run.blockReason ? (
          <p className="ai-task-playbook-run-detail__block-reason">
            <strong>Bloke nedeni:</strong> {run.blockReason}
          </p>
        ) : null}

        {run.cancellationReason ? (
          <p className="ai-task-playbook-run-detail__block-reason">
            <strong>İptal nedeni:</strong> {run.cancellationReason}
          </p>
        ) : null}

        <div className="ai-task-playbook-run-detail__toolbar">
          <Link
            className="ai-task-playbook-inline-link"
            href={`/ai-gorevleri/playbooklar/${run.playbookId}`}
          >
            Playbook tanımını gör
          </Link>
          <Button
            disabled={isPending}
            leadingIcon={<RefreshCw />}
            type="button"
            variant="quiet"
            onClick={refresh}
          >
            Yenile
          </Button>
        </div>
      </Card>

      <Card className="ai-task-playbook-run-detail__flow">
        <div className="ai-task-playbook-section__header">
          <div>
            <h3>Adım akışı</h3>
            <p>Her adımın durumu ve bağımlılık bilgisi.</p>
          </div>
        </div>

        <div className="ai-task-playbook-run-detail__steps">
          {sortedSteps.map((step, index) => (
            <article
              key={step.id}
              className={`ai-task-playbook-step ${getAiTaskPlaybookRunStepVisualStateClass(step.status)}`}
              data-testid={`ai-task-playbook-step-${step.stepKey}`}
            >
              <div className="ai-task-playbook-step__connector" aria-hidden="true">
                {index > 0 ? <span className="ai-task-playbook-step__line" /> : null}
                <span className="ai-task-playbook-step__marker">
                  <StepIcon status={step.status} />
                </span>
              </div>

              <div className="ai-task-playbook-step__body">
                <div className="ai-task-playbook-step__header">
                  <div>
                    <strong>{step.name}</strong>
                    <p className="ai-task-playbook-step__meta">
                      {getAiTaskPlaybookStepTypeLabel(step.stepType)}
                      {" · "}
                      {step.required ? "Zorunlu" : "İsteğe bağlı"}
                    </p>
                  </div>
                  <div className="ai-task-playbook-step__badges">
                    <Badge tone={getAiTaskPlaybookRunStepStatusTone(step.status)}>
                      {getAiTaskPlaybookRunStepStatusLabel(step.status)}
                    </Badge>
                    <Badge tone="neutral">
                      {getAiTaskPlaybookDependencyStateLabel(step.dependencyState)}
                    </Badge>
                  </div>
                </div>

                <div className="ai-task-playbook-step__timeline">
                  {step.readyAtUtc ? (
                    <span>Hazır: {formatAiTaskShortDateTime(step.readyAtUtc)}</span>
                  ) : null}
                  {step.startedAtUtc ? (
                    <span>Başladı: {formatAiTaskShortDateTime(step.startedAtUtc)}</span>
                  ) : null}
                  {step.completedAtUtc ? (
                    <span>Tamamlandı: {formatAiTaskShortDateTime(step.completedAtUtc)}</span>
                  ) : null}
                </div>

                {step.blockReason ? (
                  <p className="ai-task-playbook-step__block-reason">{step.blockReason}</p>
                ) : null}

                {step.linkedTaskId ? (
                  <Link
                    className="ai-task-playbook-inline-link"
                    href={`/ai-gorevleri/${step.linkedTaskId}`}
                  >
                    <ExternalLink aria-hidden="true" />
                    <span>Bağlı görevi aç</span>
                  </Link>
                ) : null}

                {capabilities.canOverride && isActive ? (
                  <div className="ai-task-playbook-step__actions">
                    <button
                      className="ai-task-playbook-step__action"
                      type="button"
                      onClick={() =>
                        setSelectedStepId(selectedStepId === step.id ? null : step.id)
                      }
                    >
                      Müdahale
                    </button>
                  </div>
                ) : null}

                {selectedStepId === step.id ? (
                  <div className="ai-task-playbook-step__override">
                    <AiTaskDetailField label="İşlem nedeni">
                      <AiTaskDetailInput
                        value={stepReason}
                        onChange={(event) => setStepReason(event.target.value)}
                      />
                    </AiTaskDetailField>
                    <div className="ai-task-playbook-step__override-actions">
                      <Button
                        leadingIcon={<Ban />}
                        type="button"
                        variant="secondary"
                        onClick={() => handleStepAction("block", step.id)}
                      >
                        Bloke et
                      </Button>
                      <Button
                        leadingIcon={<Unlock />}
                        type="button"
                        variant="secondary"
                        onClick={() => handleStepAction("unblock", step.id)}
                      >
                        Blokeyi kaldır
                      </Button>
                      <Button
                        leadingIcon={<SkipForward />}
                        type="button"
                        variant="secondary"
                        onClick={() => handleStepAction("skip", step.id)}
                      >
                        Atla
                      </Button>
                    </div>
                  </div>
                ) : null}
              </div>
            </article>
          ))}
        </div>
      </Card>

      {capabilities.canOverride && isActive ? (
        <Card className="ai-task-playbook-run-detail__cancel">
          <h3>Çalışmayı iptal et</h3>
          <AiTaskDetailField label="İptal nedeni">
            <AiTaskDetailInput
              value={cancelReason}
              onChange={(event) => setCancelReason(event.target.value)}
            />
          </AiTaskDetailField>
          <Button type="button" variant="secondary" onClick={handleCancel}>
            Çalışmayı iptal et
          </Button>
        </Card>
      ) : null}
    </div>
  );
}
