"use client";

import { useCallback, useState } from "react";
import { useRouter } from "next/navigation";
import { AlertTriangle, Clock, StopCircle } from "lucide-react";

import type {
  ProductionDowntimeSummary,
  ProductionDowntimeReasonCode,
} from "../domain/production-execution-operations";
import {
  PLANNED_DOWNTIME_REASONS,
  UNPLANNED_DOWNTIME_REASONS,
  getDowntimeReasonLabel,
  getDowntimeCategoryLabel,
  formatDuration,
  formatUtcShort,
  requiresDowntimeComment,
} from "../domain/production-execution-operations";
import type { ProductionOrderStatus } from "../domain/production-order";
import type {
  startDowntimeAction,
  endDowntimeAction,
  recoverDowntimeAction,
} from "../server/production-order-writer";
import { customerMessage } from "@/lib/presentation/customer-message";

type DowntimePanelProps = Readonly<{
  orderId: string;
  status: ProductionOrderStatus;
  version: number;
  activeDowntime?: ProductionDowntimeSummary;
  recentDowntimes: ProductionDowntimeSummary[];
  totalDowntimeSeconds: number;
  downtimeIsStale?: boolean;
  onStartDowntime: typeof startDowntimeAction;
  onEndDowntime: typeof endDowntimeAction;
  onRecoverDowntime?: typeof recoverDowntimeAction;
}>;

type PanelState =
  | { kind: "idle" }
  | { kind: "start-dialog" }
  | { kind: "recover-dialog" }
  | { kind: "pending"; action: string }
  | { kind: "error"; message: string };

export function ProductionDowntimePanel({
  orderId,
  status,
  version,
  activeDowntime,
  recentDowntimes,
  totalDowntimeSeconds,
  downtimeIsStale,
  onStartDowntime,
  onEndDowntime,
  onRecoverDowntime,
}: DowntimePanelProps) {
  const router = useRouter();
  const [panelState, setPanelState] = useState<PanelState>({ kind: "idle" });
  const [selectedReason, setSelectedReason] = useState<ProductionDowntimeReasonCode>("machine-fault");
  const [comment, setComment] = useState("");
  const [recoveryResolution, setRecoveryResolution] = useState("");
  const [resumeProduction, setResumeProduction] = useState(false);

  const canStart = status === "in-progress" && !activeDowntime;
  const canEnd = activeDowntime !== undefined;
  const commentRequired = requiresDowntimeComment(selectedReason);

  const handleStartSubmit = useCallback(async () => {
    if (commentRequired && !comment.trim()) return;
    setPanelState({ kind: "pending", action: "Duruş kaydediliyor…" });
    try {
      const result = await onStartDowntime(orderId, version, selectedReason, comment.trim() || undefined);
      if (result.status === "failure") {
        setPanelState({ kind: "error", message: result.message ?? "Duruş başlatılamadı." });
      } else {
        setPanelState({ kind: "idle" });
        setComment("");
        router.refresh();
      }
    } catch {
      setPanelState({ kind: "error", message: "Bir hata oluştu." });
    }
  }, [orderId, version, selectedReason, comment, commentRequired, onStartDowntime, router]);

  const handleEnd = useCallback(async () => {
    if (!activeDowntime) return;
    setPanelState({ kind: "pending", action: "Duruş kapatılıyor…" });
    try {
      const result = await onEndDowntime(orderId, activeDowntime.id, version);
      if (result.status === "failure") {
        setPanelState({ kind: "error", message: result.message ?? "Duruş kapatılamadı." });
      } else {
        setPanelState({ kind: "idle" });
        router.refresh();
      }
    } catch {
      setPanelState({ kind: "error", message: "Bir hata oluştu." });
    }
  }, [orderId, activeDowntime, version, onEndDowntime, router]);

  const handleRecoverSubmit = useCallback(async () => {
    if (!activeDowntime || !onRecoverDowntime || !recoveryResolution.trim()) return;
    setPanelState({ kind: "pending", action: "Duruş uzlaştırılıyor…" });
    try {
      const result = await onRecoverDowntime(orderId, activeDowntime.id, version, recoveryResolution.trim(), resumeProduction);
      if (result.status === "failure") {
        setPanelState({ kind: "error", message: result.message ?? "Uzlaştırma başarısız." });
      } else {
        setPanelState({ kind: "idle" });
        setRecoveryResolution("");
        setResumeProduction(false);
        router.refresh();
      }
    } catch {
      setPanelState({ kind: "error", message: "Bir hata oluştu." });
    }
  }, [orderId, activeDowntime, version, recoveryResolution, resumeProduction, onRecoverDowntime, router]);

  const isPending = panelState.kind === "pending";

  return (
    <div className="ops-panel">
      <div className="ops-panel__header">
        <AlertTriangle aria-hidden="true" className="ops-panel__header-icon ops-panel__header-icon--warning" />
        <div>
          <p className="ops-panel__eyebrow">DURUŞ YÖNETİMİ</p>
          <h2>Duruş Takibi</h2>
        </div>
        <div className="ops-panel__header-total">
          <span className="ops-panel__header-total-label">Toplam duruş</span>
          <strong className="ops-panel__header-total-value">
            {formatDuration(totalDowntimeSeconds)}
          </strong>
        </div>
      </div>

      {activeDowntime && (
        <div className={`ops-panel__active-session ops-panel__active-session--downtime${downtimeIsStale ? " ops-panel__active-session--stale" : ""}`}>
          <div className="ops-panel__active-badge">
            <span className="ops-panel__active-dot ops-panel__active-dot--warning" />
            Aktif Duruş
            {downtimeIsStale && (
              <span className="stale-badge">
                <Clock aria-hidden="true" />
                Uzun süredir açık
              </span>
            )}
          </div>
          <div className="ops-panel__active-content">
            <div className="ops-panel__active-reason">
              <strong>{getDowntimeReasonLabel(activeDowntime.reasonCode)}</strong>
              <span className="ops-panel__active-category">
                {getDowntimeCategoryLabel(activeDowntime.category)}
              </span>
            </div>
            <div className="ops-panel__active-meta">
              <Clock aria-hidden="true" />
              <span>Başlangıç: {formatUtcShort(activeDowntime.startedAtUtc)}</span>
            </div>
            {activeDowntime.comment && (
              <p className="ops-panel__active-comment">{activeDowntime.comment}</p>
            )}
            {downtimeIsStale && onRecoverDowntime && panelState.kind === "idle" && (
              <button
                type="button"
                className="button button-ghost button-medium stale-recover-btn"
                onClick={() => { setRecoveryResolution(""); setResumeProduction(false); setPanelState({ kind: "recover-dialog" }); }}
              >
                Duruşu Uzlaştır / Kapat
              </button>
            )}
          </div>
        </div>
      )}

      {panelState.kind === "error" && (
        <div role="alert" className="ops-panel__error">
          {customerMessage(panelState.message)}
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
            <h3 className="workflow-action-dialog__title">Duruş Başlat</h3>
            <p className="workflow-action-dialog__description">
              Duruş nedenini seçin. Sipariş duraklatılacak ve duruş kaydedilecektir.
            </p>

            <div className="workflow-action-dialog__field">
              <label className="workflow-action-dialog__label">
                Planlı Duruşlar
              </label>
              <div className="ops-panel__reason-grid">
                {PLANNED_DOWNTIME_REASONS.map((r) => (
                  <button
                    key={r.code}
                    type="button"
                    className={`ops-panel__reason-btn${selectedReason === r.code ? " ops-panel__reason-btn--selected" : ""}`}
                    onClick={() => setSelectedReason(r.code)}
                  >
                    {r.label}
                  </button>
                ))}
              </div>
            </div>

            <div className="workflow-action-dialog__field">
              <label className="workflow-action-dialog__label">
                Plansız Duruşlar
              </label>
              <div className="ops-panel__reason-grid">
                {UNPLANNED_DOWNTIME_REASONS.map((r) => (
                  <button
                    key={r.code}
                    type="button"
                    className={`ops-panel__reason-btn${selectedReason === r.code ? " ops-panel__reason-btn--selected" : ""}`}
                    onClick={() => setSelectedReason(r.code)}
                  >
                    {r.label}
                  </button>
                ))}
              </div>
            </div>

            {commentRequired && (
              <div className="workflow-action-dialog__field">
                <label className="workflow-action-dialog__label" htmlFor="downtime-comment">
                  Açıklama <span style={{ color: "#bd2c36" }}>*</span>
                </label>
                <textarea
                  id="downtime-comment"
                  className="workflow-action-dialog__textarea"
                  rows={2}
                  placeholder="Duruş açıklaması zorunlu…"
                  value={comment}
                  onChange={(e) => setComment(e.target.value)}
                />
              </div>
            )}

            {!commentRequired && (
              <div className="workflow-action-dialog__field">
                <label className="workflow-action-dialog__label" htmlFor="downtime-comment-opt">
                  Açıklama (isteğe bağlı)
                </label>
                <textarea
                  id="downtime-comment-opt"
                  className="workflow-action-dialog__textarea"
                  rows={2}
                  placeholder="Ek not…"
                  value={comment}
                  onChange={(e) => setComment(e.target.value)}
                />
              </div>
            )}

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
                className="button button-warning button-medium"
                onClick={handleStartSubmit}
                disabled={isPending || (commentRequired && !comment.trim())}
              >
                {isPending ? "Kaydediliyor…" : "Duruş Başlat"}
              </button>
            </div>
          </div>
        </div>
      )}

      {panelState.kind === "recover-dialog" && (
        <div className="workflow-action-dialog">
          <div className="workflow-action-dialog__content">
            <h3 className="workflow-action-dialog__title">Duruşu Uzlaştır / Kapat</h3>
            <p className="workflow-action-dialog__description">
              Uzun süredir açık olan bu duruş kaydını kapatmak için bir çözüm notu girin. Bu işlem geri alınamaz.
            </p>

            <div className="workflow-action-dialog__field">
              <label className="workflow-action-dialog__label" htmlFor="recovery-resolution">
                Çözüm Açıklaması <span style={{ color: "#bd2c36" }}>*</span>
              </label>
              <textarea
                id="recovery-resolution"
                className="workflow-action-dialog__textarea"
                rows={3}
                maxLength={500}
                placeholder="Duruş neden kapandı, ne yapıldı?…"
                value={recoveryResolution}
                onChange={(e) => setRecoveryResolution(e.target.value)}
              />
            </div>

            <div className="workflow-action-dialog__field">
              <label className="workflow-action-dialog__label recovery-checkbox-label">
                <input
                  type="checkbox"
                  checked={resumeProduction}
                  onChange={(e) => setResumeProduction(e.target.checked)}
                />
                Üretime devam et (Paused → InProgress)
              </label>
            </div>

            <div className="workflow-action-dialog__footer">
              <button
                type="button"
                className="button button-ghost button-medium"
                onClick={() => { setPanelState({ kind: "idle" }); setRecoveryResolution(""); }}
                disabled={isPending}
              >
                İptal
              </button>
              <button
                type="button"
                className="button button-warning button-medium"
                onClick={handleRecoverSubmit}
                disabled={isPending || !recoveryResolution.trim()}
              >
                {isPending ? "Uzlaştırılıyor…" : "Duruşu Kapat"}
              </button>
            </div>
          </div>
        </div>
      )}

      {panelState.kind === "pending" && panelState.action && (
        <div className="ops-panel__pending">{panelState.action}</div>
      )}

      <div className="ops-panel__actions">
        {canStart && panelState.kind === "idle" && (
          <button
            type="button"
            className="button button-warning button-medium"
            onClick={() => setPanelState({ kind: "start-dialog" })}
          >
            <AlertTriangle aria-hidden="true" />
            Duruş Başlat
          </button>
        )}

        {canEnd && panelState.kind !== "start-dialog" && (
          <button
            type="button"
            className="button button-medium"
            onClick={handleEnd}
            disabled={isPending}
          >
            <StopCircle aria-hidden="true" />
            {isPending ? "Kapatılıyor…" : "Duruşu Bitir"}
          </button>
        )}
      </div>

      {recentDowntimes.length > 0 && (
        <div className="ops-panel__history">
          <p className="ops-panel__history-label">Son Duruşlar</p>
          <ul className="ops-panel__history-list">
            {recentDowntimes.slice(0, 5).map((d) => (
              <li key={d.id} className="ops-panel__history-item">
                <div className="ops-panel__history-item-header">
                  <span className="ops-panel__history-item-name">
                    {getDowntimeReasonLabel(d.reasonCode)}
                  </span>
                  {d.durationSeconds !== undefined ? (
                    <span className="ops-panel__history-item-duration">
                      {formatDuration(d.durationSeconds)}
                    </span>
                  ) : (
                    <span className="ops-panel__history-item-active">Devam ediyor</span>
                  )}
                </div>
                <span className="ops-panel__history-item-time">
                  {formatUtcShort(d.startedAtUtc)}
                  {d.endedAtUtc ? ` — ${formatUtcShort(d.endedAtUtc)}` : ""}
                </span>
              </li>
            ))}
          </ul>
        </div>
      )}
    </div>
  );
}
