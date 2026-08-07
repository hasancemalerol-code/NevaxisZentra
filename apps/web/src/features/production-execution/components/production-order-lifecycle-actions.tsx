"use client";

import { useCallback, useRef, useState } from "react";
import { useRouter } from "next/navigation";

import type { ProductionOrderStatus } from "../domain/production-order";
import type {
  completeProductionOrderAction,
  pauseProductionOrderAction,
  resumeProductionOrderAction,
  startProductionOrderAction,
} from "../server/production-order-writer";

export type ProductionOrderLifecycleActionsProps = Readonly<{
  orderId: string;
  orderTitle: string;
  status: ProductionOrderStatus;
  version: number;
  onStart: typeof startProductionOrderAction;
  onPause: typeof pauseProductionOrderAction;
  onResume: typeof resumeProductionOrderAction;
  onComplete: typeof completeProductionOrderAction;
}>;

type LifecycleActionState =
  | { kind: "idle" }
  | { kind: "confirm-start" }
  | { kind: "confirm-pause" }
  | { kind: "confirm-resume" }
  | { kind: "confirm-complete" }
  | { kind: "pending"; action: string }
  | { kind: "error"; message: string };

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

export function ProductionOrderLifecycleActions({
  orderId,
  orderTitle,
  status,
  version,
  onStart,
  onPause,
  onResume,
  onComplete,
}: ProductionOrderLifecycleActionsProps) {
  const router = useRouter();
  const [actionState, setActionState] = useState<LifecycleActionState>({
    kind: "idle",
  });
  const pauseReasonRef = useRef<HTMLTextAreaElement>(null);

  const isPending = actionState.kind === "pending";

  const canStart = status === "ready";
  const canPause = status === "in-progress";
  const canResume = status === "paused";
  const canComplete = status === "in-progress";

  const handleError = useCallback((message: string) => {
    setActionState({ kind: "error", message });
  }, []);

  const handleSuccess = useCallback(() => {
    setActionState({ kind: "idle" });
    router.refresh();
  }, [router]);

  const executeAction = useCallback(
    async (
      label: string,
      fn: () => Promise<{ status: string; code?: string; message?: string }>,
    ) => {
      setActionState({ kind: "pending", action: label });

      try {
        const result = await fn();

        if (result.status === "success") {
          handleSuccess();
        } else {
          const msg =
            result.message ?? `${label} işlemi başarısız oldu.`;
          handleError(msg);
        }
      } catch {
        handleError(`${label} sırasında beklenmeyen bir hata oluştu.`);
      }
    },
    [handleSuccess, handleError],
  );

  const doStart = useCallback(async () => {
    await executeAction("Başlat", () => onStart(orderId, version));
  }, [executeAction, onStart, orderId, version]);

  const doPause = useCallback(async () => {
    const reason = pauseReasonRef.current?.value.trim() ?? "";

    if (reason.length < 3) {
      handleError("Duraklatma gerekçesi en az 3 karakter olmalıdır.");
      return;
    }

    await executeAction("Duraklatma", () => onPause(orderId, version, reason));
  }, [executeAction, onPause, orderId, version, handleError]);

  const doResume = useCallback(async () => {
    await executeAction("Devam et", () => onResume(orderId, version));
  }, [executeAction, onResume, orderId, version]);

  const doComplete = useCallback(async () => {
    await executeAction("Tamamla", () => onComplete(orderId, version));
  }, [executeAction, onComplete, orderId, version]);

  const dismiss = useCallback(() => {
    setActionState({ kind: "idle" });
  }, []);

  if (!canStart && !canPause && !canResume && !canComplete) {
    return null;
  }

  return (
    <div className="production-order-lifecycle-actions">
      <div className="production-order-lifecycle-actions__heading">
        <p>Yaşam döngüsü aksiyonları</p>
      </div>

      {actionState.kind === "error" ? (
        <div
          className="production-order-lifecycle-actions__error"
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
        <div className="production-order-lifecycle-actions__buttons">
          {canStart ? (
            <button
              type="button"
              className="button button-primary button-small"
              onClick={() => setActionState({ kind: "confirm-start" })}
            >
              Başlat
            </button>
          ) : null}

          {canPause ? (
            <button
              type="button"
              className="button button-secondary button-small"
              onClick={() => {
                if (pauseReasonRef.current) {
                  pauseReasonRef.current.value = "";
                }
                setActionState({ kind: "confirm-pause" });
              }}
            >
              Duraklatma
            </button>
          ) : null}

          {canComplete ? (
            <button
              type="button"
              className="button button-primary button-small"
              onClick={() => setActionState({ kind: "confirm-complete" })}
            >
              Tamamla
            </button>
          ) : null}

          {canResume ? (
            <button
              type="button"
              className="button button-primary button-small"
              onClick={() => setActionState({ kind: "confirm-resume" })}
            >
              Devam Et
            </button>
          ) : null}
        </div>
      ) : null}

      {actionState.kind === "confirm-start" ? (
        <InlineDialog
          title="Üretimi başlat"
          onCancel={dismiss}
          onConfirm={doStart}
          confirmLabel="Başlat"
          isPending={isPending}
        >
          <p className="workflow-action-dialog__description">
            <strong>{orderTitle}</strong> üretim emri başlatılacak.
            Gerçek başlangıç zamanı sunucu tarafından kaydedilecektir.
          </p>
        </InlineDialog>
      ) : null}

      {actionState.kind === "confirm-pause" ? (
        <InlineDialog
          title="Üretimi duraklatma"
          onCancel={dismiss}
          onConfirm={doPause}
          confirmLabel="Duraklatma"
          isPending={isPending}
        >
          <p className="workflow-action-dialog__description">
            <strong>{orderTitle}</strong> üretim emri duraklatılacak.
          </p>

          <label className="workflow-action-dialog__label">
            Duraklatma gerekçesi <span aria-hidden="true">*</span>
            <textarea
              ref={pauseReasonRef}
              className="workflow-action-dialog__textarea"
              placeholder="Neden duraklatılıyor?"
              maxLength={500}
              required
              rows={3}
            />
          </label>
        </InlineDialog>
      ) : null}

      {actionState.kind === "confirm-resume" ? (
        <InlineDialog
          title="Üretime devam et"
          onCancel={dismiss}
          onConfirm={doResume}
          confirmLabel="Devam Et"
          isPending={isPending}
        >
          <p className="workflow-action-dialog__description">
            <strong>{orderTitle}</strong> üretim emri devam ettirilecek.
          </p>
        </InlineDialog>
      ) : null}

      {actionState.kind === "confirm-complete" ? (
        <InlineDialog
          title="Üretimi tamamla"
          onCancel={dismiss}
          onConfirm={doComplete}
          confirmLabel="Tamamla"
          isPending={isPending}
        >
          <p className="workflow-action-dialog__description">
            <strong>{orderTitle}</strong> üretim emri tamamlandı olarak
            işaretlenecek. Gerçek bitiş zamanı sunucu tarafından kaydedilecektir.
          </p>
        </InlineDialog>
      ) : null}

      {actionState.kind === "pending" ? (
        <div
          className="production-order-lifecycle-actions__pending"
          aria-live="polite"
        >
          <span>{actionState.action} işlemi gerçekleştiriliyor…</span>
        </div>
      ) : null}
    </div>
  );
}
