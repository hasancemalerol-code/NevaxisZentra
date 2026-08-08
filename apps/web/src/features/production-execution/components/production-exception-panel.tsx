"use client";

import { useCallback, useState } from "react";
import { useRouter } from "next/navigation";
import { AlertTriangle, CheckCircle2, Eye, MessageSquare, Plus, X } from "lucide-react";

import type {
  ProductionExecutionExceptionSummary,
  ProductionExceptionType,
  ProductionExceptionSeverity,
} from "../domain/production-execution-exceptions";
import {
  PRODUCTION_EXCEPTION_TYPES,
  PRODUCTION_EXCEPTION_SEVERITIES,
  getExceptionTypeLabel,
  getExceptionSeverityLabel,
  getExceptionStatusLabel,
} from "../domain/production-execution-exceptions";
import { formatUtcShort } from "../domain/production-execution-operations";
import { getExceptionOwnershipLabel } from "../domain/production-execution-resources";
import type {
  raiseExceptionAction,
  acknowledgeExceptionAction,
  resolveExceptionAction,
  assignExceptionAction,
  unassignExceptionAction,
} from "../server/production-order-writer";

type ExceptionPanelProps = Readonly<{
  orderId: string;
  version: number;
  currentSubjectId?: string;
  exceptions: ProductionExecutionExceptionSummary[];
  onRaiseException: typeof raiseExceptionAction;
  onAcknowledgeException: typeof acknowledgeExceptionAction;
  onResolveException: typeof resolveExceptionAction;
  onAssignException?: typeof assignExceptionAction;
  onUnassignException?: typeof unassignExceptionAction;
}>;

type PanelState =
  | { kind: "idle" }
  | { kind: "raise-dialog" }
  | { kind: "resolve-dialog"; exceptionId: string }
  | { kind: "assign-dialog"; exceptionId: string }
  | { kind: "pending"; action: string }
  | { kind: "error"; message: string };

function severityClass(severity: ProductionExceptionSeverity): string {
  if (severity === "critical") return "exception-item--critical";
  if (severity === "warning") return "exception-item--warning";
  return "exception-item--info";
}

export function ProductionExceptionPanel({
  orderId,
  version,
  currentSubjectId,
  exceptions,
  onRaiseException,
  onAcknowledgeException,
  onResolveException,
  onAssignException,
  onUnassignException,
}: ExceptionPanelProps) {
  const router = useRouter();
  const [panelState, setPanelState] = useState<PanelState>({ kind: "idle" });
  const [exceptionType, setExceptionType] = useState<ProductionExceptionType>("operator-attention");
  const [severity, setSeverity] = useState<ProductionExceptionSeverity>("warning");
  const [message, setMessage] = useState("");
  const [resolutionNote, setResolutionNote] = useState("");
  const [assignTargetId, setAssignTargetId] = useState("");

  const openExceptions = exceptions.filter((e) => e.status !== "resolved");
  const resolvedExceptions = exceptions.filter((e) => e.status === "resolved");

  const handleRaiseSubmit = useCallback(async () => {
    if (!message.trim()) return;
    setPanelState({ kind: "pending", action: "İstisna kaydediliyor…" });
    try {
      const result = await onRaiseException(orderId, version, exceptionType, severity, message.trim());
      if (result.status === "failure") {
        setPanelState({ kind: "error", message: result.message ?? "İstisna kaydedilemedi." });
      } else {
        setPanelState({ kind: "idle" });
        setMessage("");
        router.refresh();
      }
    } catch {
      setPanelState({ kind: "error", message: "Bir hata oluştu." });
    }
  }, [orderId, version, exceptionType, severity, message, onRaiseException, router]);

  const handleAcknowledge = useCallback(async (exceptionId: string) => {
    setPanelState({ kind: "pending", action: "Onaylanıyor…" });
    try {
      const result = await onAcknowledgeException(orderId, exceptionId, version);
      if (result.status === "failure") {
        setPanelState({ kind: "error", message: result.message ?? "Onaylanamadı." });
      } else {
        setPanelState({ kind: "idle" });
        router.refresh();
      }
    } catch {
      setPanelState({ kind: "error", message: "Bir hata oluştu." });
    }
  }, [orderId, version, onAcknowledgeException, router]);

  const handleResolveSubmit = useCallback(async (exceptionId: string) => {
    if (!resolutionNote.trim()) return;
    setPanelState({ kind: "pending", action: "Çözümleniyor…" });
    try {
      const result = await onResolveException(orderId, exceptionId, version, resolutionNote.trim());
      if (result.status === "failure") {
        setPanelState({ kind: "error", message: result.message ?? "Çözümlenemedi." });
      } else {
        setPanelState({ kind: "idle" });
        setResolutionNote("");
        router.refresh();
      }
    } catch {
      setPanelState({ kind: "error", message: "Bir hata oluştu." });
    }
  }, [orderId, version, resolutionNote, onResolveException, router]);

  const handleAssignException = useCallback(async (exceptionId: string) => {
    if (!assignTargetId.trim() || !onAssignException) return;
    setPanelState({ kind: "pending", action: "İstisna atanıyor…" });
    try {
      const result = await onAssignException(orderId, exceptionId, version, assignTargetId.trim());
      if (result.status === "failure") {
        setPanelState({ kind: "error", message: result.message ?? "İstisna atanamadı." });
      } else {
        setPanelState({ kind: "idle" });
        setAssignTargetId("");
        router.refresh();
      }
    } catch {
      setPanelState({ kind: "error", message: "Bir hata oluştu." });
    }
  }, [orderId, version, assignTargetId, onAssignException, router]);

  const handleUnassignException = useCallback(async (exceptionId: string) => {
    if (!onUnassignException) return;
    setPanelState({ kind: "pending", action: "Atama kaldırılıyor…" });
    try {
      const result = await onUnassignException(orderId, exceptionId, version);
      if (result.status === "failure") {
        setPanelState({ kind: "error", message: result.message ?? "Atama kaldırılamadı." });
      } else {
        setPanelState({ kind: "idle" });
        router.refresh();
      }
    } catch {
      setPanelState({ kind: "error", message: "Bir hata oluştu." });
    }
  }, [orderId, version, onUnassignException, router]);

  const isPending = panelState.kind === "pending";

  return (
    <div className="ops-panel exception-panel">
      <div className="ops-panel__header">
        <AlertTriangle aria-hidden="true" className="ops-panel__header-icon ops-panel__header-icon--warning" />
        <div>
          <p className="ops-panel__eyebrow">OPERASYON İSTİSNALARI</p>
          <h2>İstisna Yönetimi</h2>
        </div>
        <div className="ops-panel__header-total">
          <span className="ops-panel__header-total-label">Açık istisna</span>
          <strong className="ops-panel__header-total-value">{openExceptions.length}</strong>
        </div>
      </div>

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

      {panelState.kind === "pending" && (
        <div className="ops-panel__pending">{panelState.action}</div>
      )}

      {panelState.kind === "raise-dialog" && (
        <div className="workflow-action-dialog">
          <div className="workflow-action-dialog__content">
            <h3 className="workflow-action-dialog__title">İstisna Kaydet</h3>
            <p className="workflow-action-dialog__description">
              Operasyonel bir sorunu kayıt altına alın. Hayatta kritik istisna tamamlamayı engeller.
            </p>

            <div className="workflow-action-dialog__field">
              <label className="workflow-action-dialog__label">İstisna Türü</label>
              <div className="ops-panel__reason-grid">
                {PRODUCTION_EXCEPTION_TYPES.map((t) => (
                  <button
                    key={t}
                    type="button"
                    className={`ops-panel__reason-btn${exceptionType === t ? " ops-panel__reason-btn--selected" : ""}`}
                    onClick={() => setExceptionType(t)}
                  >
                    {getExceptionTypeLabel(t)}
                  </button>
                ))}
              </div>
            </div>

            <div className="workflow-action-dialog__field">
              <label className="workflow-action-dialog__label">Önem Derecesi</label>
              <div className="exception-severity-select">
                {PRODUCTION_EXCEPTION_SEVERITIES.map((s) => (
                  <button
                    key={s}
                    type="button"
                    className={`exception-severity-btn exception-severity-btn--${s}${severity === s ? " exception-severity-btn--selected" : ""}`}
                    onClick={() => setSeverity(s)}
                  >
                    {getExceptionSeverityLabel(s)}
                  </button>
                ))}
              </div>
            </div>

            <div className="workflow-action-dialog__field">
              <label className="workflow-action-dialog__label" htmlFor="exception-message">
                Açıklama <span style={{ color: "#bd2c36" }}>*</span>
              </label>
              <textarea
                id="exception-message"
                className="workflow-action-dialog__textarea"
                rows={3}
                maxLength={500}
                placeholder="İstisna açıklaması…"
                value={message}
                onChange={(e) => setMessage(e.target.value)}
              />
              <span className="workflow-action-dialog__char-count">{message.length}/500</span>
            </div>

            <div className="workflow-action-dialog__footer">
              <button
                type="button"
                className="button button-ghost button-medium"
                onClick={() => { setPanelState({ kind: "idle" }); setMessage(""); }}
                disabled={isPending}
              >
                İptal
              </button>
              <button
                type="button"
                className="button button-warning button-medium"
                onClick={handleRaiseSubmit}
                disabled={isPending || !message.trim()}
              >
                {isPending ? "Kaydediliyor…" : "İstisna Kaydet"}
              </button>
            </div>
          </div>
        </div>
      )}

      {panelState.kind === "resolve-dialog" && (
        <div className="workflow-action-dialog">
          <div className="workflow-action-dialog__content">
            <h3 className="workflow-action-dialog__title">İstisna Çözümle</h3>
            <p className="workflow-action-dialog__description">
              Çözüm notunu girerek istisnayı kapatın. Not zorunludur.
            </p>

            <div className="workflow-action-dialog__field">
              <label className="workflow-action-dialog__label" htmlFor="resolution-note">
                Çözüm Notu <span style={{ color: "#bd2c36" }}>*</span>
              </label>
              <textarea
                id="resolution-note"
                className="workflow-action-dialog__textarea"
                rows={3}
                maxLength={500}
                placeholder="Sorunu nasıl çözdünüz?…"
                value={resolutionNote}
                onChange={(e) => setResolutionNote(e.target.value)}
              />
            </div>

            <div className="workflow-action-dialog__footer">
              <button
                type="button"
                className="button button-ghost button-medium"
                onClick={() => { setPanelState({ kind: "idle" }); setResolutionNote(""); }}
                disabled={isPending}
              >
                İptal
              </button>
              <button
                type="button"
                className="button button-medium"
                onClick={() => handleResolveSubmit(panelState.exceptionId)}
                disabled={isPending || !resolutionNote.trim()}
              >
                {isPending ? "Çözümleniyor…" : "Çözümle"}
              </button>
            </div>
          </div>
        </div>
      )}

      {panelState.kind === "assign-dialog" && (
        <div className="workflow-action-dialog">
          <div className="workflow-action-dialog__content">
            <h3 className="workflow-action-dialog__title">İstisnayı Ata</h3>
            <div className="workflow-action-dialog__field">
              <label className="workflow-action-dialog__label" htmlFor="assign-target">
                Operatör Subject ID <span style={{ color: "#bd2c36" }}>*</span>
              </label>
              <input
                id="assign-target"
                type="text"
                className="workflow-action-dialog__input"
                placeholder="Subject ID"
                value={assignTargetId}
                onChange={(e) => setAssignTargetId(e.target.value)}
              />
            </div>
            <div className="workflow-action-dialog__footer">
              <button
                type="button"
                className="button button-ghost button-medium"
                onClick={() => { setPanelState({ kind: "idle" }); setAssignTargetId(""); }}
              >
                İptal
              </button>
              <button
                type="button"
                className="button button-medium"
                onClick={() => handleAssignException(panelState.exceptionId)}
                disabled={isPending || !assignTargetId.trim()}
              >
                Ata
              </button>
            </div>
          </div>
        </div>
      )}

      {openExceptions.length > 0 && (
        <ul className="exception-panel__list">
          {openExceptions.map((ex) => {
            const ownershipLabel = getExceptionOwnershipLabel(ex.status, ex.assignedToSubjectId, currentSubjectId);
            return (
              <li key={ex.id} className={`exception-item ${severityClass(ex.severity)}`}>
                <div className="exception-item__header">
                  <span className="exception-item__type">{getExceptionTypeLabel(ex.exceptionType)}</span>
                  <span className={`exception-item__severity exception-item__severity--${ex.severity}`}>
                    {getExceptionSeverityLabel(ex.severity)}
                  </span>
                  <span className={`exception-item__status exception-item__status--${ex.status}`}>
                    {getExceptionStatusLabel(ex.status)}
                  </span>
                  {ex.assignedToSubjectId && (
                    <span className={`exception-item__ownership exception-item__ownership--${ownershipLabel === "Bana Atandı" ? "mine" : "other"}`}>
                      {ownershipLabel}
                    </span>
                  )}
                </div>

                <p className="exception-item__message">{ex.message}</p>

                <div className="exception-item__meta">
                  <span>{formatUtcShort(ex.raisedAtUtc)}</span>
                  <span className="exception-item__actor">{ex.raisedBySubjectId}</span>
                </div>

                {panelState.kind === "idle" && (
                  <div className="exception-item__actions">
                    {ex.status === "open" && (
                      <button
                        type="button"
                        className="button button-ghost button-medium"
                        onClick={() => handleAcknowledge(ex.id)}
                        disabled={isPending}
                      >
                        <Eye aria-hidden="true" />
                        Gördüm
                      </button>
                    )}
                    {(ex.status === "open" || ex.status === "acknowledged") && (
                      <button
                        type="button"
                        className="button button-medium"
                        onClick={() => { setResolutionNote(""); setPanelState({ kind: "resolve-dialog", exceptionId: ex.id }); }}
                        disabled={isPending}
                      >
                        <CheckCircle2 aria-hidden="true" />
                        Çözümle
                      </button>
                    )}
                    {onAssignException && (ex.status === "open" || ex.status === "acknowledged") && (
                      <button
                        type="button"
                        className="button button-ghost button-medium"
                        onClick={() => { setAssignTargetId(currentSubjectId ?? ""); setPanelState({ kind: "assign-dialog", exceptionId: ex.id }); }}
                        disabled={isPending}
                      >
                        {!ex.assignedToSubjectId ? "Bana Ata" : "Operatöre Ata"}
                      </button>
                    )}
                    {onUnassignException && ex.assignedToSubjectId && ex.status !== "resolved" && (
                      <button
                        type="button"
                        className="button button-ghost button-medium"
                        onClick={() => handleUnassignException(ex.id)}
                        disabled={isPending}
                      >
                        Atamayı Kaldır
                      </button>
                    )}
                  </div>
                )}
              </li>
            );
          })}
        </ul>
      )}

      {openExceptions.length === 0 && resolvedExceptions.length === 0 && panelState.kind === "idle" && (
        <div className="exception-panel__empty">
          <CheckCircle2 aria-hidden="true" />
          <span>Açık istisna yok</span>
        </div>
      )}

      {resolvedExceptions.length > 0 && (
        <details className="exception-panel__resolved">
          <summary className="exception-panel__resolved-toggle">
            <MessageSquare aria-hidden="true" />
            {resolvedExceptions.length} çözümlenmiş istisna
          </summary>
          <ul className="exception-panel__list exception-panel__list--resolved">
            {resolvedExceptions.slice(0, 5).map((ex) => (
              <li key={ex.id} className="exception-item exception-item--resolved">
                <div className="exception-item__header">
                  <span className="exception-item__type">{getExceptionTypeLabel(ex.exceptionType)}</span>
                  <span className={`exception-item__severity exception-item__severity--${ex.severity}`}>
                    {getExceptionSeverityLabel(ex.severity)}
                  </span>
                  <span className="exception-item__status exception-item__status--resolved">
                    Çözüldü
                  </span>
                </div>
                {ex.resolutionNote && (
                  <p className="exception-item__resolution">{ex.resolutionNote}</p>
                )}
              </li>
            ))}
          </ul>
        </details>
      )}

      {panelState.kind === "idle" && (
        <div className="ops-panel__actions">
          <button
            type="button"
            className="button button-ghost button-medium"
            onClick={() => { setMessage(""); setExceptionType("operator-attention"); setSeverity("warning"); setPanelState({ kind: "raise-dialog" }); }}
          >
            <Plus aria-hidden="true" />
            İstisna Kaydet
          </button>
        </div>
      )}
    </div>
  );
}
