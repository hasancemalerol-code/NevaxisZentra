import {
  Check,
  CirclePlay,
  Clock3,
  RefreshCw,
  RotateCcw,
  ShieldCheck,
  X,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";

import type { AiTask } from "../domain/ai-task";
import {
  canExecuteAiTask,
  getAllowedAiTaskTransitions,
} from "../domain/ai-task-policy";
import {
  getAiTaskApprovalStateLabel,
  getAiTaskStatusLabel,
} from "../presentation/ai-task-labels";

type AiTaskActionPanelProps = Readonly<{
  task: AiTask;
}>;

function getActionExplanation(task: AiTask): string {
  if (task.status === "awaiting-approval") {
    return "Bu görev, önerilen işlem uygulanmadan önce yetkili kullanıcı onayı bekliyor.";
  }

  if (task.status === "awaiting-review") {
    return "Görev içeriği, kaynak veriler ve AI gerekçesi bir kullanıcı tarafından incelenmeli.";
  }

  if (task.status === "approved") {
    return "Görev onaylandı. Uygulama işlemi kullanıcı tarafından başlatılabilir.";
  }

  if (task.status === "in-progress") {
    return "Önerilen işlem uygulanıyor. Tamamlanma veya hata sonucu bekleniyor.";
  }

  if (task.status === "failed") {
    return "Son uygulama girişimi başarısız oldu. Hata nedeni incelendikten sonra görev yeniden değerlendirilebilir.";
  }

  if (task.status === "deferred") {
    return "Görev ertelendi. Yeni değerlendirme zamanı veya ek bilgi bekleniyor.";
  }

  if (task.status === "completed") {
    return "Görev ve ilişkili işlem tamamlandı.";
  }

  if (task.status === "rejected") {
    return "Görev yetkili kullanıcı tarafından reddedildi.";
  }

  if (task.status === "cancelled") {
    return "Görev iptal edildi ve artık uygulanamaz.";
  }

  return "Görev taslak durumda. İnceleme sürecine gönderilmeden önce içeriği doğrulanmalı.";
}

export function AiTaskActionPanel({
  task,
}: AiTaskActionPanelProps) {
  const allowedTransitions = getAllowedAiTaskTransitions(
    task.status,
  );

  const executionDecision = canExecuteAiTask(task);

  const canSubmitForReview =
    allowedTransitions.includes("awaiting-review");

  const canApprove =
    task.status === "awaiting-approval";

  const canReject =
    task.status === "awaiting-review" ||
    task.status === "awaiting-approval";

  const canDefer =
    allowedTransitions.includes("deferred");

  const canStartExecution =
    executionDecision.allowed &&
    allowedTransitions.includes("in-progress");

  const canRetry =
    task.status === "failed" &&
    allowedTransitions.includes("awaiting-review");

  const isFinal =
    task.status === "completed" ||
    task.status === "rejected" ||
    task.status === "cancelled";

  return (
    <Card
      className="ai-task-action-panel"
      data-status={task.status}
    >
      <div className="ai-task-action-panel__heading">
        <div>
          <p>
            Human-in-the-Loop
          </p>

          <h2>
            Görev kararı
          </h2>
        </div>

        <div className="ai-task-action-panel__states">
          <Badge tone="neutral">
            {getAiTaskStatusLabel(task.status)}
          </Badge>

          <Badge
            tone={
              task.approval.state === "approved"
                ? "success"
                : task.approval.state === "rejected"
                  ? "danger"
                  : task.approval.state === "pending" ||
                      task.approval.state === "required"
                    ? "warning"
                    : "neutral"
            }
          >
            <ShieldCheck aria-hidden="true" />
            {getAiTaskApprovalStateLabel(
              task.approval.state,
            )}
          </Badge>
        </div>
      </div>

      <p className="ai-task-action-panel__description">
        {getActionExplanation(task)}
      </p>

      {task.approval.decisionNote ? (
        <div className="ai-task-action-panel__decision-note">
          <strong>
            Karar notu
          </strong>

          <p>
            {task.approval.decisionNote}
          </p>
        </div>
      ) : null}

      {!executionDecision.allowed &&
      task.status === "approved" ? (
        <div
          className="ai-task-action-panel__warning"
          role="status"
        >
          <Clock3 aria-hidden="true" />

          <span>
            {executionDecision.reason}
          </span>
        </div>
      ) : null}

      <div
        aria-label="Görev karar aksiyonları"
        className="ai-task-action-panel__actions"
      >
        {canSubmitForReview ? (
          <Button
            disabled
            leadingIcon={<CirclePlay />}
            title="Gerçek görev güncelleme servisi henüz bağlı değil."
            variant="secondary"
          >
            İncelemeye gönder
          </Button>
        ) : null}

        {canApprove ? (
          <Button
            disabled
            leadingIcon={<Check />}
            title="Gerçek onay servisi henüz bağlı değil."
          >
            Görevi onayla
          </Button>
        ) : null}

        {canStartExecution ? (
          <Button
            disabled
            leadingIcon={<CirclePlay />}
            title="Gerçek uygulama servisi henüz bağlı değil."
          >
            Uygulamayı başlat
          </Button>
        ) : null}

        {canRetry ? (
          <Button
            disabled
            leadingIcon={<RefreshCw />}
            title="Gerçek yeniden deneme servisi henüz bağlı değil."
            variant="secondary"
          >
            Yeniden değerlendir
          </Button>
        ) : null}

        {canDefer ? (
          <Button
            disabled
            leadingIcon={<RotateCcw />}
            title="Gerçek erteleme servisi henüz bağlı değil."
            variant="secondary"
          >
            Ertele
          </Button>
        ) : null}

        {canReject ? (
          <Button
            disabled
            leadingIcon={<X />}
            title="Gerçek ret servisi henüz bağlı değil."
            variant="secondary"
          >
            Reddet
          </Button>
        ) : null}

        {isFinal ? (
          <Badge tone="neutral">
            İşlem kapalı
          </Badge>
        ) : null}
      </div>

      <small className="ai-task-action-panel__footnote">
        Kritik ve ERP bağlantılı işlemler kullanıcı kararı
        olmadan uygulanmaz. Bu ekrandaki aksiyonlar yalnızca
        arayüz önizlemesidir.
      </small>
    </Card>
  );
}