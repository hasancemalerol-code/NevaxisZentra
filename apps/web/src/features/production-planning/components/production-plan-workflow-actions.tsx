"use client";

import { useCallback, useRef, useState } from "react";
import { useRouter } from "next/navigation";

import type {
  approveAction,
  rejectAction,
  requestRevisionAction,
  submitForReviewAction,
} from "../server/production-plan-workflow-actions";

export type ProductionPlanWorkflowActionsProps = Readonly<{
  planId: string;
  planStatus: string;
  version: number;
  planTitle: string;
  onSubmitForReview: typeof submitForReviewAction;
  onRequestRevision: typeof requestRevisionAction;
  onApprove: typeof approveAction;
  onReject: typeof rejectAction;
}>;

type WorkflowActionState =
  | { kind: "idle" }
  | { kind: "confirm-submit" }
  | { kind: "confirm-revision" }
  | { kind: "confirm-approve" }
  | { kind: "confirm-reject" }
  | { kind: "pending"; action: string }
  | { kind: "error"; message: string };

function canSubmitForReview(status: string): boolean {
  return status === "draft" || status === "needs-review";
}

function canRequestRevision(status: string): boolean {
  return status === "ready-for-approval";
}

function canApproveOrReject(status: string): boolean {
  return status === "ready-for-approval";
}

function getStatusLabel(status: string): string {
  const labels: Record<string, string> = {
    draft: "Taslak",
    "needs-review": "İnceleme gerekiyor",
    "ready-for-approval": "Onaya hazır",
    approved: "Onaylandı",
    rejected: "Reddedildi",
  };
  return labels[status] ?? status;
}

type InlineDialogProps = Readonly<{
  title: string;
  children: React.ReactNode;
  onCancel: () => void;
  onConfirm: () => void;
  confirmLabel: string;
  confirmTone?: "primary" | "danger";
  isPending: boolean;
}>;

function InlineDialog({
  title,
  children,
  onCancel,
  onConfirm,
  confirmLabel,
  confirmTone = "primary",
  isPending,
}: InlineDialogProps) {
  return (
    <div
      className="workflow-action-dialog"
      role="dialog"
      aria-modal="true"
      aria-label={title}
    >
      <div className="workflow-action-dialog__content">
        <h3 className="workflow-action-dialog__title">{title}</h3>
        {children}
        <div className="workflow-action-dialog__footer">
          <button
            type="button"
            className="button button-ghost button-small"
            onClick={onCancel}
            disabled={isPending}
          >
            İptal
          </button>
          <button
            type="button"
            className={`button button-small${confirmTone === "danger" ? " button-destructive" : " button-primary"}`}
            onClick={onConfirm}
            disabled={isPending}
          >
            {isPending ? "İşleniyor…" : confirmLabel}
          </button>
        </div>
      </div>
    </div>
  );
}

export function ProductionPlanWorkflowActions({
  planId,
  planStatus,
  version,
  planTitle,
  onSubmitForReview,
  onRequestRevision,
  onApprove,
  onReject,
}: ProductionPlanWorkflowActionsProps) {
  const router = useRouter();
  const [actionState, setActionState] = useState<WorkflowActionState>({
    kind: "idle",
  });
  const commentRef = useRef<HTMLTextAreaElement>(null);
  const reasonRef = useRef<HTMLTextAreaElement>(null);
  const noteRef = useRef<HTMLTextAreaElement>(null);

  const isPending = actionState.kind === "pending";

  const handleError = useCallback((message: string) => {
    setActionState({ kind: "error", message });
  }, []);

  const handleSuccess = useCallback(() => {
    setActionState({ kind: "idle" });
    router.refresh();
  }, [router]);

  const executeAction = useCallback(
    async (label: string, fn: () => Promise<{ status: string; message?: string }>) => {
      setActionState({ kind: "pending", action: label });
      try {
        const result = await fn();
        if (result.status === "success") {
          handleSuccess();
        } else {
          handleError(
            (result as { status: string; message?: string }).message ??
              `${label} işlemi başarısız oldu.`,
          );
        }
      } catch {
        handleError(`${label} sırasında beklenmeyen bir hata oluştu.`);
      }
    },
    [handleSuccess, handleError],
  );

  const doSubmitForReview = useCallback(async () => {
    const comment = commentRef.current?.value.trim();
    await executeAction("İncelemeye gönder", () =>
      onSubmitForReview(planId, version, comment || undefined),
    );
  }, [executeAction, onSubmitForReview, planId, version]);

  const doRequestRevision = useCallback(async () => {
    const reason = reasonRef.current?.value.trim() ?? "";
    if (reason.length < 3) {
      handleError("Revizyon gerekçesi en az 3 karakter olmalıdır.");
      return;
    }
    await executeAction("Revizyon iste", () =>
      onRequestRevision(planId, version, reason),
    );
  }, [executeAction, onRequestRevision, planId, version, handleError]);

  const doApprove = useCallback(async () => {
    const note = noteRef.current?.value.trim();
    await executeAction("Onayla", () =>
      onApprove(planId, version, note || undefined),
    );
  }, [executeAction, onApprove, planId, version]);

  const doReject = useCallback(async () => {
    const reason = reasonRef.current?.value.trim() ?? "";
    if (reason.length < 3) {
      handleError("Red gerekçesi en az 3 karakter olmalıdır.");
      return;
    }
    await executeAction("Reddet", () =>
      onReject(planId, version, reason),
    );
  }, [executeAction, onReject, planId, version, handleError]);

  const dismiss = useCallback(() => {
    setActionState({ kind: "idle" });
  }, []);

  const showSubmit = canSubmitForReview(planStatus);
  const showRevision = canRequestRevision(planStatus);
  const showApproveReject = canApproveOrReject(planStatus);

  if (!showSubmit && !showRevision && !showApproveReject) {
    return null;
  }

  return (
    <div className="production-plan-workflow-actions">
      <div className="production-plan-workflow-actions__heading">
        <p>Onay akışı aksiyonları</p>
        <span>
          Güncel durum:{" "}
          <strong>{getStatusLabel(planStatus)}</strong>
        </span>
      </div>

      {actionState.kind === "error" ? (
        <div
          className="production-plan-workflow-actions__error"
          role="alert"
        >
          <span>{actionState.message}</span>
          <button
            type="button"
            className="button button-ghost button-small"
            onClick={dismiss}
          >
            Kapat
          </button>
        </div>
      ) : null}

      {actionState.kind === "idle" ? (
        <div className="production-plan-workflow-actions__buttons">
          {showSubmit ? (
            <button
              type="button"
              className="button button-primary button-small"
              onClick={() => setActionState({ kind: "confirm-submit" })}
            >
              İncelemeye Gönder
            </button>
          ) : null}

          {showRevision ? (
            <button
              type="button"
              className="button button-secondary button-small"
              onClick={() => {
                if (reasonRef.current) reasonRef.current.value = "";
                setActionState({ kind: "confirm-revision" });
              }}
            >
              Revizyon İste
            </button>
          ) : null}

          {showApproveReject ? (
            <>
              <button
                type="button"
                className="button button-primary button-small"
                onClick={() => {
                  if (noteRef.current) noteRef.current.value = "";
                  setActionState({ kind: "confirm-approve" });
                }}
              >
                Onayla
              </button>

              <button
                type="button"
                className="button button-destructive button-small"
                onClick={() => {
                  if (reasonRef.current) reasonRef.current.value = "";
                  setActionState({ kind: "confirm-reject" });
                }}
              >
                Reddet
              </button>
            </>
          ) : null}
        </div>
      ) : null}

      {actionState.kind === "confirm-submit" ? (
        <InlineDialog
          title="İncelemeye gönder"
          onCancel={dismiss}
          onConfirm={doSubmitForReview}
          confirmLabel="İncelemeye Gönder"
          isPending={isPending}
        >
          <p className="workflow-action-dialog__description">
            <strong>{planTitle}</strong> planı onay incelemesine gönderilecek.
            Onaylanana kadar plan kuyrukta bekleyecektir.
          </p>
          <label className="workflow-action-dialog__label">
            Yorum (isteğe bağlı)
            <textarea
              ref={commentRef}
              className="workflow-action-dialog__textarea"
              placeholder="İnceleyiciye not ekleyebilirsiniz…"
              maxLength={2000}
              rows={3}
            />
          </label>
        </InlineDialog>
      ) : null}

      {actionState.kind === "confirm-revision" ? (
        <InlineDialog
          title="Revizyon iste"
          onCancel={dismiss}
          onConfirm={doRequestRevision}
          confirmLabel="Revizyon İste"
          confirmTone="danger"
          isPending={isPending}
        >
          <p className="workflow-action-dialog__description">
            <strong>{planTitle}</strong> planı revizyon için geri gönderilecek.
          </p>
          <label className="workflow-action-dialog__label">
            Revizyon gerekçesi <span aria-hidden="true">*</span>
            <textarea
              ref={reasonRef}
              className="workflow-action-dialog__textarea"
              placeholder="Revizyon neden gerekiyor?"
              maxLength={2000}
              required
              rows={3}
            />
          </label>
        </InlineDialog>
      ) : null}

      {actionState.kind === "confirm-approve" ? (
        <InlineDialog
          title="Planı onayla"
          onCancel={dismiss}
          onConfirm={doApprove}
          confirmLabel="Onayla"
          isPending={isPending}
        >
          <p className="workflow-action-dialog__description">
            <strong>{planTitle}</strong> planı onaylanacak.
            Bu işlem yetkili insan onayıdır ve geri alınamaz.
          </p>
          <label className="workflow-action-dialog__label">
            Onay notu (isteğe bağlı)
            <textarea
              ref={noteRef}
              className="workflow-action-dialog__textarea"
              placeholder="Onay için not ekleyebilirsiniz…"
              maxLength={2000}
              rows={3}
            />
          </label>
        </InlineDialog>
      ) : null}

      {actionState.kind === "confirm-reject" ? (
        <InlineDialog
          title="Planı reddet"
          onCancel={dismiss}
          onConfirm={doReject}
          confirmLabel="Reddet"
          confirmTone="danger"
          isPending={isPending}
        >
          <p className="workflow-action-dialog__description">
            <strong>{planTitle}</strong> planı reddedilecek.
          </p>
          <label className="workflow-action-dialog__label">
            Red gerekçesi <span aria-hidden="true">*</span>
            <textarea
              ref={reasonRef}
              className="workflow-action-dialog__textarea"
              placeholder="Planı neden reddediyorsunuz?"
              maxLength={2000}
              required
              rows={3}
            />
          </label>
        </InlineDialog>
      ) : null}

      {actionState.kind === "pending" ? (
        <div className="production-plan-workflow-actions__pending" aria-live="polite">
          <span>{actionState.action} işlemi gerçekleştiriliyor…</span>
        </div>
      ) : null}
    </div>
  );
}
