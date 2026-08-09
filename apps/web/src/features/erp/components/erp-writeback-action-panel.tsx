"use client";

import { useCallback, useState, useTransition } from "react";
import { useRouter } from "next/navigation";

import {
  resolveErpWritebackAvailableActions,
  type ErpWritebackPermissions,
} from "../domain/erp";
import {
  approveErpWritebackAction,
  executeErpWritebackAction,
  rejectErpWritebackAction,
  submitErpWritebackAction,
} from "../server/erp-writeback-workflow-actions";
import "./erp-workspace.css";

type ErpWritebackActionPanelProps = Readonly<{
  writebackId: string;
  status: string;
  version: number;
  permissions: ErpWritebackPermissions;
}>;

type ActionState =
  | { kind: "idle" }
  | { kind: "pending"; action: string }
  | { kind: "error"; message: string };

export function ErpWritebackActionPanel({
  writebackId,
  status,
  version,
  permissions,
}: ErpWritebackActionPanelProps) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [comment, setComment] = useState("");
  const [actionState, setActionState] = useState<ActionState>({ kind: "idle" });

  const actions = resolveErpWritebackAvailableActions(status, permissions);
  const hasActions =
    actions.canSubmit || actions.canApprove || actions.canReject || actions.canExecute;

  const runAction = useCallback(
    (action: string, handler: () => Promise<{ status: string; message?: string }>) => {
      setActionState({ kind: "pending", action });
      startTransition(async () => {
        const result = await handler();
        if (result.status === "failure") {
          setActionState({
            kind: "error",
            message: result.message ?? "İşlem başarısız.",
          });
          return;
        }

        setActionState({ kind: "idle" });
        router.refresh();
      });
    },
    [router],
  );

  if (!hasActions) {
    return null;
  }

  return (
    <section className="erp-section erp-writeback-actions" aria-label="Writeback işlemleri">
      <div className="erp-section__header">
        <h2>İşlemler</h2>
        <p>Yetki ve duruma göre kullanılabilir aksiyonlar</p>
      </div>

      {actionState.kind === "error" ? (
        <div className="erp-writeback-actions__error" role="alert">
          {actionState.message}
        </div>
      ) : null}

      <div className="erp-actions">
        {actions.canSubmit ? (
          <button
            type="button"
            className="erp-action erp-action--primary"
            disabled={isPending}
            onClick={() =>
              runAction("submit", () => submitErpWritebackAction(writebackId, version))
            }
          >
            {isPending && actionState.kind === "pending" && actionState.action === "submit"
              ? "Gönderiliyor…"
              : "Onaya Gönder"}
          </button>
        ) : null}

        {actions.canApprove ? (
          <button
            type="button"
            className="erp-action erp-action--primary"
            disabled={isPending}
            onClick={() =>
              runAction("approve", () =>
                approveErpWritebackAction(writebackId, version, comment || undefined),
              )
            }
          >
            {isPending && actionState.kind === "pending" && actionState.action === "approve"
              ? "Onaylanıyor…"
              : "Onayla"}
          </button>
        ) : null}

        {actions.canReject ? (
          <button
            type="button"
            className="erp-action"
            disabled={isPending}
            onClick={() =>
              runAction("reject", () =>
                rejectErpWritebackAction(writebackId, version, comment || undefined),
              )
            }
          >
            {isPending && actionState.kind === "pending" && actionState.action === "reject"
              ? "Reddediliyor…"
              : "Reddet"}
          </button>
        ) : null}

        {actions.canExecute ? (
          <button
            type="button"
            className="erp-action erp-action--primary"
            disabled={isPending}
            onClick={() =>
              runAction("execute", () => executeErpWritebackAction(writebackId, version))
            }
          >
            {isPending && actionState.kind === "pending" && actionState.action === "execute"
              ? "Uygulanıyor…"
              : "Uygula"}
          </button>
        ) : null}
      </div>

      {(actions.canApprove || actions.canReject) && (
        <label className="erp-writeback-actions__comment">
          <span>Yorum (isteğe bağlı)</span>
          <input
            className="input"
            value={comment}
            onChange={(event) => setComment(event.target.value)}
            disabled={isPending}
            placeholder="Onay veya red notu"
          />
        </label>
      )}

      <p className="erp-simulator-note erp-simulator-note--warning">
        ERP Simulator — gerçek ERP işlemi değildir.
      </p>
    </section>
  );
}
