"use client";

import { useRouter } from "next/navigation";
import { useState, useTransition } from "react";
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

import { transitionAiTaskAction } from "../server/ai-task-actions";
import type { AiTask, AiTaskStatus } from "../domain/ai-task";
import {
  canExecuteAiTask,
  getAllowedAiTaskTransitions,
} from "../domain/ai-task-policy";
import {
  getAiTaskApprovalStateLabel,
  getAiTaskStatusLabel,
} from "../presentation/ai-task-labels";

type AiTaskActionPanelClientProps = Readonly<{
  task: AiTask;
  version: number;
  canReview: boolean;
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

export function AiTaskActionPanelClient({
  task,
  version,
  canReview,
}: AiTaskActionPanelClientProps) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [rejectNote, setRejectNote] = useState("");
  const [showRejectForm, setShowRejectForm] = useState(false);

  const allowedTransitions = getAllowedAiTaskTransitions(task.status);
  const executionDecision = canExecuteAiTask(task);

  const canSubmitForReview =
    canReview && allowedTransitions.includes("awaiting-review");

  const canApprove =
    canReview && task.status === "awaiting-approval";

  const canReject =
    canReview && (
      task.status === "awaiting-review" ||
      task.status === "awaiting-approval"
    );

  const canDefer =
    canReview && allowedTransitions.includes("deferred");

  const canStartExecution =
    canReview &&
    executionDecision.allowed &&
    allowedTransitions.includes("in-progress");

  const canRetry =
    canReview &&
    task.status === "failed" &&
    allowedTransitions.includes("awaiting-review");

  const isFinal =
    task.status === "completed" ||
    task.status === "rejected" ||
    task.status === "cancelled";

  function runTransition(targetStatus: AiTaskStatus, note?: string) {
    setErrorMessage(null);

    startTransition(async () => {
      const result = await transitionAiTaskAction({
        taskId: task.id,
        expectedVersion: version,
        targetStatus,
        note,
      });

      if (result.status === "failure") {
        setErrorMessage(result.message);
        return;
      }

      setShowRejectForm(false);
      setRejectNote("");
      router.refresh();
    });
  }

  return (
    <Card
      className="ai-task-action-panel"
      data-status={task.status}
      data-testid="ai-task-action-panel"
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
            {getAiTaskApprovalStateLabel(task.approval.state)}
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

      {errorMessage ? (
        <div className="ai-task-action-panel__warning" role="alert">
          <Clock3 aria-hidden="true" />
          <span>{errorMessage}</span>
        </div>
      ) : null}

      {!executionDecision.allowed && task.status === "approved" ? (
        <div className="ai-task-action-panel__warning" role="status">
          <Clock3 aria-hidden="true" />
          <span>{executionDecision.reason}</span>
        </div>
      ) : null}

      {!canReview && !isFinal ? (
        <p className="ai-task-action-panel__footnote">
          Bu görev için inceleme veya onay yetkiniz bulunmuyor.
        </p>
      ) : null}

      <div
        aria-label="Görev karar aksiyonları"
        className="ai-task-action-panel__actions"
      >
        {canSubmitForReview ? (
          <Button
            disabled={isPending}
            leadingIcon={<CirclePlay />}
            onClick={() => runTransition("awaiting-review")}
            variant="secondary"
          >
            İncelemeye gönder
          </Button>
        ) : null}

        {canApprove ? (
          <Button
            disabled={isPending}
            leadingIcon={<Check />}
            onClick={() => runTransition("approved")}
          >
            Görevi onayla
          </Button>
        ) : null}

        {canStartExecution ? (
          <Button
            disabled
            leadingIcon={<CirclePlay />}
            title="Otomatik uygulama bu pakette açılmadı."
          >
            Uygulamayı başlat
          </Button>
        ) : null}

        {canRetry ? (
          <Button
            disabled={isPending}
            leadingIcon={<RefreshCw />}
            onClick={() => runTransition("awaiting-review")}
            variant="secondary"
          >
            Yeniden değerlendir
          </Button>
        ) : null}

        {canDefer ? (
          <Button
            disabled={isPending}
            leadingIcon={<RotateCcw />}
            onClick={() => runTransition("deferred")}
            variant="secondary"
          >
            Ertele
          </Button>
        ) : null}

        {canReject && !showRejectForm ? (
          <Button
            disabled={isPending}
            leadingIcon={<X />}
            onClick={() => setShowRejectForm(true)}
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

      {showRejectForm ? (
        <form
          className="ai-task-action-panel__reject-form"
          onSubmit={(event) => {
            event.preventDefault();
            if (!rejectNote.trim()) {
              setErrorMessage("Ret gerekçesi zorunludur.");
              return;
            }

            runTransition("rejected", rejectNote.trim());
          }}
        >
          <label htmlFor="ai-task-reject-note">
            Ret gerekçesi
          </label>

          <textarea
            id="ai-task-reject-note"
            onChange={(event) => setRejectNote(event.target.value)}
            placeholder="Görevin neden reddedildiğini Türkçe olarak açıklayın."
            required
            rows={3}
            value={rejectNote}
          />

          <div className="ai-task-action-panel__reject-actions">
            <Button disabled={isPending} type="submit" variant="secondary">
              Reddet
            </Button>

            <Button
              disabled={isPending}
              onClick={() => {
                setShowRejectForm(false);
                setRejectNote("");
                setErrorMessage(null);
              }}
              type="button"
              variant="secondary"
            >
              Vazgeç
            </Button>
          </div>
        </form>
      ) : null}

      <small className="ai-task-action-panel__footnote">
        Kritik ve ERP bağlantılı işlemler kullanıcı kararı olmadan uygulanmaz.
        Durum geçişleri sunucu tarafından doğrulanır ve denetim geçmişine yazılır.
      </small>
    </Card>
  );
}
