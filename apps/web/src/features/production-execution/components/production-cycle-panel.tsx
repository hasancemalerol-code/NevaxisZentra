"use client";

import { useCallback, useState } from "react";
import { useRouter } from "next/navigation";
import { Activity, Timer } from "lucide-react";

import type { ProductionCycleEntrySummary } from "../domain/production-execution-operations";
import {
  formatDurationMs,
  formatUtcShort,
} from "../domain/production-execution-operations";
import type { ProductionOrderStatus } from "../domain/production-order";
import type { recordManualCycleAction } from "../server/production-order-writer";

type CyclePanelProps = Readonly<{
  orderId: string;
  status: ProductionOrderStatus;
  version: number;
  manualCycleCount: number;
  averageManualCycleMilliseconds?: number;
  lastCycleAtUtc?: string;
  recentCycles: ProductionCycleEntrySummary[];
  onRecordCycle: typeof recordManualCycleAction;
}>;

type PanelState =
  | { kind: "idle" }
  | { kind: "record-dialog" }
  | { kind: "pending" }
  | { kind: "error"; message: string };

function parseDurationInput(value: string): number | null {
  const trimmed = value.trim();
  const asNumber = parseFloat(trimmed);
  if (!Number.isFinite(asNumber) || asNumber <= 0) return null;
  return Math.round(asNumber * 1000);
}

export function ProductionCyclePanel({
  orderId,
  status,
  version,
  manualCycleCount,
  averageManualCycleMilliseconds,
  lastCycleAtUtc,
  recentCycles,
  onRecordCycle,
}: CyclePanelProps) {
  const router = useRouter();
  const [panelState, setPanelState] = useState<PanelState>({ kind: "idle" });
  const [durationInput, setDurationInput] = useState("");
  const [quantityInput, setQuantityInput] = useState("");
  const [comment, setComment] = useState("");

  const canRecord = status === "in-progress";
  const isPending = panelState.kind === "pending";

  const handleRecord = useCallback(async () => {
    const durationMs = parseDurationInput(durationInput);
    if (durationMs === null) return;

    const quantity = quantityInput.trim()
      ? parseFloat(quantityInput.trim())
      : undefined;

    if (quantity !== undefined && (!Number.isFinite(quantity) || quantity < 0)) {
      setPanelState({ kind: "error", message: "Miktar geçersiz." });
      return;
    }

    setPanelState({ kind: "pending" });
    try {
      const result = await onRecordCycle(
        orderId,
        version,
        durationMs,
        quantity,
        comment.trim() || undefined,
      );
      if (result.status === "failure") {
        setPanelState({ kind: "error", message: result.message ?? "Çevrim kaydedilemedi." });
      } else {
        setPanelState({ kind: "idle" });
        setDurationInput("");
        setQuantityInput("");
        setComment("");
        router.refresh();
      }
    } catch {
      setPanelState({ kind: "error", message: "Bir hata oluştu." });
    }
  }, [orderId, version, durationInput, quantityInput, comment, onRecordCycle, router]);

  const durationMs = parseDurationInput(durationInput);

  return (
    <div className="ops-panel">
      <div className="ops-panel__header">
        <Activity aria-hidden="true" className="ops-panel__header-icon ops-panel__header-icon--success" />
        <div>
          <p className="ops-panel__eyebrow">MANUEL ÇEVRİM TAKİBİ</p>
          <h2>Manuel Çevrimler</h2>
        </div>
        <div className="ops-panel__header-total">
          <span className="ops-panel__header-total-label">Toplam çevrim</span>
          <strong className="ops-panel__header-total-value">{manualCycleCount}</strong>
        </div>
      </div>

      <div className="ops-panel__cycle-kpis">
        <div className="ops-panel__cycle-kpi">
          <span>Ortalama Çevrim</span>
          <strong>
            {averageManualCycleMilliseconds !== undefined
              ? formatDurationMs(averageManualCycleMilliseconds)
              : "—"}
          </strong>
        </div>
        <div className="ops-panel__cycle-kpi">
          <span>Son Çevrim</span>
          <strong>
            {lastCycleAtUtc ? formatUtcShort(lastCycleAtUtc) : "—"}
          </strong>
        </div>
      </div>

      <p className="ops-panel__cycle-note">
        Manuel giriş — makine bağlantısı yok.
      </p>

      {panelState.kind === "error" && (
        <div role="alert" className="ops-panel__error">
          {panelState.message}
          <button
            type="button"
            className="ops-panel__error-dismiss"
            onClick={() => setPanelState({ kind: "idle" })}
          >
            Kapat
          </button>
        </div>
      )}

      {panelState.kind === "record-dialog" && (
        <div className="workflow-action-dialog">
          <div className="workflow-action-dialog__content">
            <h3 className="workflow-action-dialog__title">Çevrim Kaydet</h3>
            <p className="workflow-action-dialog__description">
              Gözlemlenen çevrim süresini saniye olarak girin.
              Bu kayıt üretim miktarını otomatik artırmaz.
            </p>

            <div className="workflow-action-dialog__field">
              <label className="workflow-action-dialog__label" htmlFor="cycle-duration">
                Çevrim Süresi (saniye) <span style={{ color: "#bd2c36" }}>*</span>
              </label>
              <input
                id="cycle-duration"
                type="number"
                className="input input-large"
                placeholder="örn. 31.2"
                min="0.1"
                step="0.1"
                value={durationInput}
                onChange={(e) => setDurationInput(e.target.value)}
              />
              {durationMs !== null && (
                <p className="ops-panel__cycle-preview">
                  <Timer aria-hidden="true" />
                  {formatDurationMs(durationMs)}
                </p>
              )}
            </div>

            <div className="workflow-action-dialog__field">
              <label className="workflow-action-dialog__label" htmlFor="cycle-qty">
                Üretilen Miktar (isteğe bağlı)
              </label>
              <input
                id="cycle-qty"
                type="number"
                className="input"
                placeholder="Bu çevrimde üretilen parça sayısı"
                min="0"
                step="1"
                value={quantityInput}
                onChange={(e) => setQuantityInput(e.target.value)}
              />
              <p className="workflow-action-dialog__hint">
                Miktar yalnızca çevrim kaydına eklenir. Üretim miktar cetveli etkilenmez.
              </p>
            </div>

            <div className="workflow-action-dialog__field">
              <label className="workflow-action-dialog__label" htmlFor="cycle-comment">
                Açıklama (isteğe bağlı)
              </label>
              <textarea
                id="cycle-comment"
                className="workflow-action-dialog__textarea"
                rows={2}
                placeholder="Çevrim notu…"
                value={comment}
                onChange={(e) => setComment(e.target.value)}
              />
            </div>

            <div className="workflow-action-dialog__footer">
              <button
                type="button"
                className="button button-ghost button-medium"
                onClick={() => { setPanelState({ kind: "idle" }); setDurationInput(""); setQuantityInput(""); setComment(""); }}
                disabled={isPending}
              >
                İptal
              </button>
              <button
                type="button"
                className="button button-medium"
                style={{ background: "#10873a", color: "#fff", borderColor: "transparent" }}
                onClick={handleRecord}
                disabled={isPending || durationMs === null}
              >
                {isPending ? "Kaydediliyor…" : "Çevrimi Kaydet"}
              </button>
            </div>
          </div>
        </div>
      )}

      <div className="ops-panel__actions">
        {canRecord && panelState.kind === "idle" && (
          <button
            type="button"
            className="button button-medium"
            style={{ background: "#10873a", color: "#fff", borderColor: "transparent" }}
            onClick={() => setPanelState({ kind: "record-dialog" })}
          >
            <Activity aria-hidden="true" />
            Çevrim Kaydet
          </button>
        )}
      </div>

      {recentCycles.length > 0 && (
        <div className="ops-panel__history">
          <p className="ops-panel__history-label">Son Çevrimler</p>
          <ul className="ops-panel__history-list">
            {recentCycles.slice(0, 8).map((c) => (
              <li key={c.id} className="ops-panel__history-item">
                <div className="ops-panel__history-item-header">
                  <span className="ops-panel__history-item-name">
                    Çevrim #{c.cycleNumber}
                  </span>
                  <span className="ops-panel__history-item-duration">
                    {formatDurationMs(c.cycleDurationMilliseconds)}
                  </span>
                </div>
                <span className="ops-panel__history-item-time">
                  {formatUtcShort(c.recordedAtUtc)}
                  {c.quantityProduced !== undefined ? ` · ${c.quantityProduced} adet` : ""}
                </span>
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
}
