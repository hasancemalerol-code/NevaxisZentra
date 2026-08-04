import {
  CheckCircle2,
  CircleAlert,
  Clock3,
  Database,
  PlayCircle,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";

import type { AiTask } from "../domain/ai-task";
import {
  formatAiTaskDateTime,
} from "../presentation/ai-task-formatters";
import {
  getAiTaskTargetSystemDescription,
  getAiTaskTargetSystemLabel,
} from "../presentation/ai-task-target-system-labels";

type AiTaskExecutionPanelProps = Readonly<{
  task: Pick<
    AiTask,
    "execution" | "recommendation" | "status"
  >;
}>;

type ExecutionState =
  | "not-started"
  | "in-progress"
  | "completed"
  | "failed";

function resolveExecutionState(
  task: AiTaskExecutionPanelProps["task"],
): ExecutionState {
  if (!task.execution?.startedAt) {
    return "not-started";
  }

  if (
    task.status === "failed" ||
    task.execution.errorCode ||
    task.execution.errorMessage
  ) {
    return "failed";
  }

  if (task.execution.completedAt) {
    return "completed";
  }

  return "in-progress";
}

function getExecutionStateLabel(
  state: ExecutionState,
): string {
  switch (state) {
    case "completed":
      return "Uygulama tamamlandı";

    case "failed":
      return "Uygulama başarısız";

    case "in-progress":
      return "Uygulama devam ediyor";

    case "not-started":
    default:
      return "Henüz uygulanmadı";
  }
}

function getExecutionStateTone(
  state: ExecutionState,
): "neutral" | "information" | "success" | "danger" {
  switch (state) {
    case "completed":
      return "success";

    case "failed":
      return "danger";

    case "in-progress":
      return "information";

    case "not-started":
    default:
      return "neutral";
  }
}

function ExecutionStateIcon({
  state,
}: Readonly<{
  state: ExecutionState;
}>) {
  switch (state) {
    case "completed":
      return <CheckCircle2 aria-hidden="true" />;

    case "failed":
      return <CircleAlert aria-hidden="true" />;

    case "in-progress":
      return <PlayCircle aria-hidden="true" />;

    case "not-started":
    default:
      return <Clock3 aria-hidden="true" />;
  }
}

export function AiTaskExecutionPanel({
  task,
}: AiTaskExecutionPanelProps) {
  const execution = task.execution;
  const state = resolveExecutionState(task);

  const targetSystem =
    execution?.targetSystem ??
    task.recommendation.targetSystem;

  return (
    <Card
      className="ai-task-execution-panel"
      data-execution-state={state}
    >
      <div className="ai-task-execution-panel__heading">
        <div>
          <p>
            Sistem uygulaması
          </p>

          <h2>
            Uygulama sonucu
          </h2>
        </div>

        <Badge tone={getExecutionStateTone(state)}>
          <ExecutionStateIcon state={state} />
          {getExecutionStateLabel(state)}
        </Badge>
      </div>

      <dl className="ai-task-execution-panel__details">
        <div>
          <dt>
            <Database aria-hidden="true" />
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
            Başlangıç
          </dt>

          <dd>
            {formatAiTaskDateTime(
              execution?.startedAt,
            )}
          </dd>
        </div>

        <div>
          <dt>
            Tamamlanma
          </dt>

          <dd>
            {formatAiTaskDateTime(
              execution?.completedAt,
            )}
          </dd>
        </div>

        <div>
          <dt>
            Dış sistem referansı
          </dt>

          <dd>
            {execution?.externalReferenceId ??
              "Tanımlanmadı"}
          </dd>
        </div>
      </dl>

      {execution?.previousValue ||
      execution?.appliedValue ? (
        <div className="ai-task-execution-panel__change">
          <div>
            <span>
              Önceki değer
            </span>

            <strong>
              {execution.previousValue ??
                "Tanımlanmadı"}
            </strong>
          </div>

          <span aria-hidden="true">
            →
          </span>

          <div>
            <span>
              Uygulanan değer
            </span>

            <strong>
              {execution.appliedValue ??
                "Tanımlanmadı"}
            </strong>
          </div>
        </div>
      ) : null}

      {state === "failed" ? (
        <div
          className="ai-task-execution-panel__error"
          role="alert"
        >
          <CircleAlert aria-hidden="true" />

          <div>
            <strong>
              {execution?.errorCode ??
                "UYGULAMA_HATASI"}
            </strong>

            <p>
              {execution?.errorMessage ??
                "Uygulama işlemi tamamlanamadı."}
            </p>
          </div>
        </div>
      ) : null}

      {state === "not-started" ? (
        <p className="ai-task-execution-panel__empty">
          Bu görev için henüz bir sistem uygulaması
          başlatılmadı.
        </p>
      ) : null}
    </Card>
  );
}