"use client";

import { useCallback, useRef, useState } from "react";
import { useRouter } from "next/navigation";

import type { ProductionOrderStatus } from "../domain/production-order";
import {
  SCRAP_REASON_CODES,
  getScrapReasonLabel,
  requiresComment,
} from "../domain/production-execution-entry";
import type {
  recordGoodProductionAction,
  recordScrapAction,
} from "../server/production-order-writer";

type OperatorActionState =
  | { kind: "idle" }
  | { kind: "good-form" }
  | { kind: "scrap-form" }
  | { kind: "pending"; action: string }
  | { kind: "error"; message: string };

export type ProductionOperatorActionsProps = Readonly<{
  orderId: string;
  status: ProductionOrderStatus;
  version: number;
  onRecordGood: typeof recordGoodProductionAction;
  onRecordScrap: typeof recordScrapAction;
  plannedQuantity?: number;
  goodQuantity: number;
}>;

export function ProductionOperatorActions({
  orderId,
  status,
  version,
  onRecordGood,
  onRecordScrap,
  plannedQuantity,
  goodQuantity,
}: ProductionOperatorActionsProps) {
  const router = useRouter();
  const [state, setState] = useState<OperatorActionState>({ kind: "idle" });
  const goodQtyRef = useRef<HTMLInputElement>(null);
  const goodCommentRef = useRef<HTMLTextAreaElement>(null);
  const scrapQtyRef = useRef<HTMLInputElement>(null);
  const scrapReasonRef = useRef<HTMLSelectElement>(null);
  const scrapCommentRef = useRef<HTMLTextAreaElement>(null);
  const currentVersion = useRef(version);
  currentVersion.current = version;

  const canRecord = status === "in-progress";

  const handleGoodSubmit = useCallback(async () => {
    const qty = Number(goodQtyRef.current?.value ?? "0");
    const comment = goodCommentRef.current?.value?.trim() || undefined;

    if (!qty || qty <= 0) {
      setState({ kind: "error", message: "Geçerli bir miktar girin." });
      return;
    }

    setState({ kind: "pending", action: "İyi üretim kaydediliyor" });
    const result = await onRecordGood(orderId, currentVersion.current, qty, comment);

    if (result.status === "failure") {
      const msg =
        result.code === "conflict"
          ? `Çakışma: ${result.message}. Lütfen sayfayı yenileyin.`
          : (result.message ?? "Kayıt başarısız oldu.");
      setState({ kind: "error", message: msg });
      return;
    }

    setState({ kind: "idle" });
    router.refresh();
  }, [orderId, onRecordGood, router]);

  const handleScrapSubmit = useCallback(async () => {
    const qty = Number(scrapQtyRef.current?.value ?? "0");
    const reason = scrapReasonRef.current?.value ?? "";
    const comment = scrapCommentRef.current?.value?.trim() || undefined;

    if (!qty || qty <= 0) {
      setState({ kind: "error", message: "Geçerli bir miktar girin." });
      return;
    }

    if (!reason) {
      setState({ kind: "error", message: "Fire nedeni seçin." });
      return;
    }

    if (requiresComment(reason) && !comment) {
      setState({
        kind: "error",
        message: "'Diğer' nedeni seçildiğinde yorum zorunludur.",
      });
      return;
    }

    setState({ kind: "pending", action: "Fire kaydediliyor" });
    const result = await onRecordScrap(
      orderId,
      currentVersion.current,
      qty,
      reason,
      comment,
    );

    if (result.status === "failure") {
      const msg =
        result.code === "conflict"
          ? `Çakışma: ${result.message}. Lütfen sayfayı yenileyin.`
          : (result.message ?? "Kayıt başarısız oldu.");
      setState({ kind: "error", message: msg });
      return;
    }

    setState({ kind: "idle" });
    router.refresh();
  }, [orderId, onRecordScrap, router]);

  if (!canRecord) {
    return null;
  }

  const isPending = state.kind === "pending";

  return (
    <div className="production-operator-actions">
      <div className="production-operator-actions__heading">
        <p className="production-operator-actions__eyebrow">OPERATÖR GİRİŞİ</p>
        <h2 className="production-operator-actions__title">
          Üretim miktarı kaydet
        </h2>
      </div>

      {state.kind === "error" && (
        <div
          className="production-operator-actions__error"
          role="alert"
        >
          <span>{state.message}</span>
          <button
            type="button"
            className="button button-ghost button-small"
            onClick={() => setState({ kind: "idle" })}
          >
            Kapat
          </button>
        </div>
      )}

      {state.kind === "idle" && (
        <div className="production-operator-actions__buttons">
          <button
            type="button"
            className="button button-primary button-large"
            onClick={() => setState({ kind: "good-form" })}
          >
            + İyi Üretim
          </button>
          <button
            type="button"
            className="button button-warning button-large"
            onClick={() => setState({ kind: "scrap-form" })}
          >
            + Fire Kaydı
          </button>
        </div>
      )}

      {state.kind === "good-form" && (
        <div
          className="workflow-action-dialog"
          role="dialog"
          aria-modal="true"
          aria-label="İyi Üretim Kaydı"
        >
          <div className="workflow-action-dialog__content">
            <h3 className="workflow-action-dialog__title">İyi Üretim Kaydı</h3>

            {plannedQuantity !== undefined && (
              <p className="workflow-action-dialog__hint">
                Planlanan:{" "}
                <strong>
                  {plannedQuantity.toLocaleString("tr-TR")} adet
                </strong>
                {" — "}
                Şimdiye kadar:{" "}
                <strong>
                  {goodQuantity.toLocaleString("tr-TR")} adet
                </strong>
              </p>
            )}

            <div className="workflow-action-dialog__field">
              <label htmlFor="good-qty-input">Miktar (adet)</label>
              <input
                id="good-qty-input"
                ref={goodQtyRef}
                type="number"
                min="1"
                step="1"
                placeholder="0"
                className="input input-large"
                // eslint-disable-next-line jsx-a11y/no-autofocus
                autoFocus
              />
            </div>

            <div className="workflow-action-dialog__field">
              <label htmlFor="good-comment-input">
                Yorum (isteğe bağlı)
              </label>
              <textarea
                id="good-comment-input"
                ref={goodCommentRef}
                rows={2}
                className="workflow-action-dialog__textarea"
                placeholder="Üretim notu…"
              />
            </div>

            <div className="workflow-action-dialog__footer">
              <button
                type="button"
                className="button button-ghost button-small"
                onClick={() => setState({ kind: "idle" })}
                disabled={isPending}
              >
                İptal
              </button>
              <button
                type="button"
                className="button button-primary button-small"
                onClick={handleGoodSubmit}
                disabled={isPending}
              >
                {isPending ? "Kaydediliyor…" : "Kaydet"}
              </button>
            </div>
          </div>
        </div>
      )}

      {state.kind === "scrap-form" && (
        <div
          className="workflow-action-dialog"
          role="dialog"
          aria-modal="true"
          aria-label="Fire Kaydı"
        >
          <div className="workflow-action-dialog__content">
            <h3 className="workflow-action-dialog__title">Fire Kaydı</h3>

            <div className="workflow-action-dialog__field">
              <label htmlFor="scrap-qty-input">Fire miktarı (adet)</label>
              <input
                id="scrap-qty-input"
                ref={scrapQtyRef}
                type="number"
                min="1"
                step="1"
                placeholder="0"
                className="input input-large"
                // eslint-disable-next-line jsx-a11y/no-autofocus
                autoFocus
              />
            </div>

            <div className="workflow-action-dialog__field">
              <label htmlFor="scrap-reason-select">Fire Nedeni</label>
              <select
                id="scrap-reason-select"
                ref={scrapReasonRef}
                className="input"
                defaultValue=""
              >
                <option value="" disabled>
                  Neden seçin…
                </option>
                {SCRAP_REASON_CODES.map((code) => (
                  <option key={code} value={code}>
                    {getScrapReasonLabel(code)}
                  </option>
                ))}
              </select>
            </div>

            <div className="workflow-action-dialog__field">
              <label htmlFor="scrap-comment-input">
                Yorum{" "}
                <span style={{ color: "#8495aa", fontWeight: 500 }}>
                  — &ldquo;Diğer&rdquo; seçilirse zorunlu
                </span>
              </label>
              <textarea
                id="scrap-comment-input"
                ref={scrapCommentRef}
                rows={2}
                className="workflow-action-dialog__textarea"
                placeholder="Fire açıklaması…"
              />
            </div>

            <div className="workflow-action-dialog__footer">
              <button
                type="button"
                className="button button-ghost button-small"
                onClick={() => setState({ kind: "idle" })}
                disabled={isPending}
              >
                İptal
              </button>
              <button
                type="button"
                className="button button-warning button-small"
                onClick={handleScrapSubmit}
                disabled={isPending}
              >
                {isPending ? "Kaydediliyor…" : "Fire Kaydet"}
              </button>
            </div>
          </div>
        </div>
      )}

      {state.kind === "pending" && (
        <p className="production-operator-actions__pending" aria-live="polite">
          {state.action}…
        </p>
      )}
    </div>
  );
}
