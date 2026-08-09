"use client";

import { useCallback, useState } from "react";
import { useRouter } from "next/navigation";

import type {
  approveErpWritebackAction,
  executeErpWritebackAction,
  rejectErpWritebackAction,
  submitErpWritebackAction,
} from "../server/erp-writeback-actions";

export type ErpWritebackApprovalActionsProps = Readonly<{
  writebackId: string;
  writebackStatus: string;
  version: number;
  onSubmit: typeof submitErpWritebackAction;
  onApprove: typeof approveErpWritebackAction;
  onReject: typeof rejectErpWritebackAction;
  onExecute: typeof executeErpWritebackAction;
}>;

type ActionState =
  | { kind: "idle" }
  | { kind: "pending"; action: string }
  | { kind: "error"; message: string };

function canSubmit(status: string): boolean {
  return status === "draft";
}

function canApproveOrReject(status: string): boolean {
  return status === "pending-approval";
}

function canExecute(status: string): boolean {
  return status === "approved";
}

export function ErpWritebackApprovalActions({
  writebackId,
  writebackStatus,
  version,
  onSubmit,
  onApprove,
  onReject,
  onExecute,
}: ErpWritebackApprovalActionsProps) {
  const router = useRouter();
  const [actionState, setActionState] = useState<ActionState>({ kind: "idle" });

  const executeAction = useCallback(
    async (label: string, action: () => Promise<{ status: string; message?: string }>) => {
      setActionState({ kind: "pending", action: label });
      const result = await action();

      if (result.status === "failure") {
        setActionState({ kind: "error", message: result.message ?? "İşlem başarısız." });
        return;
      }

      setActionState({ kind: "idle" });
      router.refresh();
    },
    [router],
  );

  const isPending = actionState.kind === "pending";
  const showSubmit = canSubmit(writebackStatus);
  const showApproveReject = canApproveOrReject(writebackStatus);
  const showExecute = canExecute(writebackStatus);
  const hasActions = showSubmit || showApproveReject || showExecute;

  if (!hasActions && actionState.kind === "idle") {
    return (
      <p className="erp-action-hint">
        Bu durum için kullanılabilir işlem yok. Kayıt salt okunur görüntülenir.
      </p>
    );
  }

  return (
    <div className="erp-writeback-approval-panel">
      <div className="erp-writeback-actions">
        {showSubmit ? (
          <button
            type="button"
            className="button button-primary button-small"
            disabled={isPending}
            onClick={() =>
              executeAction("Onaya Gönder", () => onSubmit(writebackId, version))
            }
          >
            {isPending ? "İşleniyor…" : "Onaya Gönder"}
          </button>
        ) : null}

        {showApproveReject ? (
          <>
            <button
              type="button"
              className="button button-primary button-small"
              disabled={isPending}
              onClick={() =>
                executeAction("Onayla", () => onApprove(writebackId, version))
              }
            >
              {isPending ? "İşleniyor…" : "Onayla"}
            </button>
            <button
              type="button"
              className="button button-destructive button-small"
              disabled={isPending}
              onClick={() =>
                executeAction("Reddet", () => onReject(writebackId, version))
              }
            >
              {isPending ? "İşleniyor…" : "Reddet"}
            </button>
          </>
        ) : null}

        {showExecute ? (
          <button
            type="button"
            className="button button-primary button-small"
            disabled={isPending}
            onClick={() =>
              executeAction("Uygula", () => onExecute(writebackId, version))
            }
          >
            {isPending ? "İşleniyor…" : "Uygula"}
          </button>
        ) : null}
      </div>

      {actionState.kind === "error" ? (
        <p className="erp-writeback-action-error" role="alert">
          {actionState.message}
        </p>
      ) : null}
    </div>
  );
}
