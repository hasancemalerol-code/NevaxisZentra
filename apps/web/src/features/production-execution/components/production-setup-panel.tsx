"use client";

import { useCallback, useState } from "react";
import { useRouter } from "next/navigation";
import { Settings, CheckCircle2, Clock } from "lucide-react";

import type { ProductionSetupSessionSummary } from "../domain/production-execution-operations";
import {
  SETUP_TYPES,
  getSetupTypeLabel,
  formatDuration,
  formatUtcShort,
} from "../domain/production-execution-operations";
import type { ProductionOrderStatus } from "../domain/production-order";
import type {
  startSetupSessionAction,
  completeSetupSessionAction,
} from "../server/production-order-writer";

type SetupPanelProps = Readonly<{
  orderId: string;
  status: ProductionOrderStatus;
  version: number;
  activeSetup?: ProductionSetupSessionSummary;
  recentSessions: ProductionSetupSessionSummary[];
  totalSetupSeconds: number;
  onStartSetup: typeof startSetupSessionAction;
  onCompleteSetup: typeof completeSetupSessionAction;
}>;

type PanelState =
  | { kind: "idle" }
  | { kind: "start-dialog" }
  | { kind: "pending"; action: string }
  | { kind: "error"; message: string };

export function ProductionSetupPanel({
  orderId,
  status,
  version,
  activeSetup,
  recentSessions,
  totalSetupSeconds,
  onStartSetup,
  onCompleteSetup,
}: SetupPanelProps) {
  const router = useRouter();
  const [panelState, setPanelState] = useState<PanelState>({ kind: "idle" });
  const [selectedType, setSelectedType] = useState("initial-setup");
  const [comment, setComment] = useState("");

  const canStart = (status === "ready" || status === "paused") && !activeSetup;
  const canComplete = activeSetup !== undefined;
  const isPending = panelState.kind === "pending";

  const handleStartSubmit = useCallback(async () => {
    setPanelState({ kind: "pending", action: "Setup başlatılıyor…" });
    try {
      const result = await onStartSetup(orderId, version, selectedType, comment.trim() || undefined);
      if (result.status === "failure") {
        setPanelState({ kind: "error", message: result.message ?? "Setup başlatılamadı." });
      } else {
        setPanelState({ kind: "idle" });
        setComment("");
        router.refresh();
      }
    } catch {
      setPanelState({ kind: "error", message: "Bir hata oluştu." });
    }
  }, [orderId, version, selectedType, comment, onStartSetup, router]);

  const handleComplete = useCallback(async () => {
    if (!activeSetup) return;
    setPanelState({ kind: "pending", action: "Setup tamamlanıyor…" });
    try {
      const result = await onCompleteSetup(orderId, activeSetup.id, version);
      if (result.status === "failure") {
        setPanelState({ kind: "error", message: result.message ?? "Setup tamamlanamadı." });
      } else {
        setPanelState({ kind: "idle" });
        router.refresh();
      }
    } catch {
      setPanelState({ kind: "error", message: "Bir hata oluştu." });
    }
  }, [orderId, activeSetup, version, onCompleteSetup, router]);

  return (
    <div className="ops-panel">
      <div className="ops-panel__header">
        <Settings aria-hidden="true" className="ops-panel__header-icon ops-panel__header-icon--primary" />
        <div>
          <p className="ops-panel__eyebrow">SETUP & DEĞİŞİM</p>
          <h2>Setup Yönetimi</h2>
        </div>
        <div className="ops-panel__header-total">
          <span className="ops-panel__header-total-label">Toplam setup</span>
          <strong className="ops-panel__header-total-value">
            {formatDuration(totalSetupSeconds)}
          </strong>
        </div>
      </div>

      {activeSetup && (
        <div className="ops-panel__active-session ops-panel__active-session--setup">
          <div className="ops-panel__active-badge">
            <span className="ops-panel__active-dot ops-panel__active-dot--primary" />
            Aktif Setup
          </div>
          <div className="ops-panel__active-content">
            <div className="ops-panel__active-reason">
              <strong>{getSetupTypeLabel(activeSetup.setupType)}</strong>
            </div>
            <div className="ops-panel__active-meta">
              <Clock aria-hidden="true" />
              <span>Başlangıç: {formatUtcShort(activeSetup.startedAtUtc)}</span>
            </div>
            {activeSetup.comment && (
              <p className="ops-panel__active-comment">{activeSetup.comment}</p>
            )}
          </div>
        </div>
      )}

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

      {panelState.kind === "start-dialog" && (
        <div className="workflow-action-dialog">
          <div className="workflow-action-dialog__content">
            <h3 className="workflow-action-dialog__title">Setup Başlat</h3>
            <p className="workflow-action-dialog__description">
              Setup türünü seçin. Makine ve kalıp değişimleri burada kayıt altına alınır.
            </p>

            <div className="workflow-action-dialog__field">
              <label className="workflow-action-dialog__label">Setup Türü</label>
              <div className="ops-panel__reason-grid">
                {SETUP_TYPES.map((s) => (
                  <button
                    key={s.code}
                    type="button"
                    className={`ops-panel__reason-btn${selectedType === s.code ? " ops-panel__reason-btn--selected" : ""}`}
                    onClick={() => setSelectedType(s.code)}
                  >
                    {s.label}
                  </button>
                ))}
              </div>
            </div>

            <div className="workflow-action-dialog__field">
              <label className="workflow-action-dialog__label" htmlFor="setup-comment">
                Açıklama (isteğe bağlı)
              </label>
              <textarea
                id="setup-comment"
                className="workflow-action-dialog__textarea"
                rows={2}
                placeholder="Ek not…"
                value={comment}
                onChange={(e) => setComment(e.target.value)}
              />
            </div>

            <div className="workflow-action-dialog__footer">
              <button
                type="button"
                className="button button-ghost button-medium"
                onClick={() => { setPanelState({ kind: "idle" }); setComment(""); }}
                disabled={isPending}
              >
                İptal
              </button>
              <button
                type="button"
                className="button button-medium"
                style={{ background: "#155eef", color: "#fff", borderColor: "transparent" }}
                onClick={handleStartSubmit}
                disabled={isPending}
              >
                {isPending ? "Başlatılıyor…" : "Setup Başlat"}
              </button>
            </div>
          </div>
        </div>
      )}

      <div className="ops-panel__actions">
        {canStart && panelState.kind === "idle" && (
          <button
            type="button"
            className="button button-medium"
            style={{ background: "#155eef", color: "#fff", borderColor: "transparent" }}
            onClick={() => setPanelState({ kind: "start-dialog" })}
          >
            <Settings aria-hidden="true" />
            Setup Başlat
          </button>
        )}

        {canComplete && panelState.kind !== "start-dialog" && (
          <button
            type="button"
            className="button button-medium"
            onClick={handleComplete}
            disabled={isPending}
          >
            <CheckCircle2 aria-hidden="true" />
            {isPending ? "Tamamlanıyor…" : "Setup Tamamla"}
          </button>
        )}
      </div>

      {recentSessions.length > 0 && (
        <div className="ops-panel__history">
          <p className="ops-panel__history-label">Son Setuplar</p>
          <ul className="ops-panel__history-list">
            {recentSessions.slice(0, 5).map((s) => (
              <li key={s.id} className="ops-panel__history-item">
                <div className="ops-panel__history-item-header">
                  <span className="ops-panel__history-item-name">
                    {getSetupTypeLabel(s.setupType)}
                  </span>
                  {s.durationSeconds !== undefined ? (
                    <span className="ops-panel__history-item-duration">
                      {formatDuration(s.durationSeconds)}
                    </span>
                  ) : (
                    <span className="ops-panel__history-item-active">Devam ediyor</span>
                  )}
                </div>
                <span className="ops-panel__history-item-time">
                  {formatUtcShort(s.startedAtUtc)}
                  {s.completedAtUtc ? ` — ${formatUtcShort(s.completedAtUtc)}` : ""}
                </span>
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
}
