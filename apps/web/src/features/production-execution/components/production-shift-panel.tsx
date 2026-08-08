"use client";

import { useCallback, useState } from "react";
import { useRouter } from "next/navigation";
import { Clock, RefreshCw } from "lucide-react";

import type { ShiftAssignmentSummary } from "../domain/production-execution-resources";
import type { assignShiftAction, reassignShiftAction } from "../server/production-order-writer";

type ShiftPanelProps = Readonly<{
  orderId: string;
  version: number;
  status: string;
  currentShiftCode?: string;
  currentShiftStartAtUtc?: string;
  currentShiftEndAtUtc?: string;
  recentAssignments: ShiftAssignmentSummary[];
  onAssignShift: typeof assignShiftAction;
  onReassignShift: typeof reassignShiftAction;
}>;

type PanelState =
  | { kind: "idle" }
  | { kind: "assign-dialog" }
  | { kind: "reassign-dialog" }
  | { kind: "pending"; action: string }
  | { kind: "error"; message: string };

function formatShiftWindow(start: string | undefined, end: string | undefined): string {
  if (!start || !end) return "—";
  try {
    const s = new Date(start).toLocaleTimeString("tr-TR", { hour: "2-digit", minute: "2-digit" });
    const e = new Date(end).toLocaleTimeString("tr-TR", { hour: "2-digit", minute: "2-digit" });
    return `${s} – ${e}`;
  } catch {
    return "—";
  }
}

function formatShiftDate(iso: string | undefined): string {
  if (!iso) return "—";
  try {
    return new Date(iso).toLocaleDateString("tr-TR", { day: "2-digit", month: "long", year: "numeric" });
  } catch {
    return iso;
  }
}

function isExpired(endAtUtc: string | undefined): boolean {
  if (!endAtUtc) return false;
  return new Date(endAtUtc).getTime() < Date.now();
}

export function ProductionShiftPanel({
  orderId,
  version,
  status,
  currentShiftCode,
  currentShiftStartAtUtc,
  currentShiftEndAtUtc,
  recentAssignments,
  onAssignShift,
  onReassignShift,
}: ShiftPanelProps) {
  const router = useRouter();
  const [panelState, setPanelState] = useState<PanelState>({ kind: "idle" });
  const [shiftId, setShiftId] = useState("");
  const [shiftDate, setShiftDate] = useState(() => new Date().toISOString().slice(0, 10));
  const [reason, setReason] = useState("");

  const canAssign = status === "ready" || status === "in-progress" || status === "paused";
  const hasActiveShift = !!currentShiftCode;
  const shiftExpired = isExpired(currentShiftEndAtUtc);

  const handleAssignSubmit = useCallback(async () => {
    if (!shiftId.trim() || !shiftDate.trim()) return;
    setPanelState({ kind: "pending", action: "Vardiya atanıyor…" });
    try {
      const result = hasActiveShift
        ? await onReassignShift(orderId, version, shiftId.trim(), shiftDate.trim(), reason.trim() || "Yeniden atandı")
        : await onAssignShift(orderId, version, shiftId.trim(), shiftDate.trim());
      if (result.status === "failure") {
        setPanelState({ kind: "error", message: result.message ?? "Vardiya atanamadı." });
      } else {
        setPanelState({ kind: "idle" });
        setShiftId("");
        setReason("");
        router.refresh();
      }
    } catch {
      setPanelState({ kind: "error", message: "Bir hata oluştu." });
    }
  }, [orderId, version, shiftId, shiftDate, reason, hasActiveShift, onAssignShift, onReassignShift, router]);

  return (
    <div className="production-panel production-shift-panel">
      <div className="production-panel__header">
        <Clock className="production-panel__header-icon" aria-hidden="true" />
        <h3>VARDİYA</h3>
      </div>

      {/* Current shift state */}
      {hasActiveShift ? (
        <div className={`shift-current-card${shiftExpired ? " shift-current-card--expired" : ""}`}>
          <div className="shift-current-card__code">{currentShiftCode}</div>
          <div className="shift-current-card__window">
            {formatShiftWindow(currentShiftStartAtUtc, currentShiftEndAtUtc)}
          </div>
          <div className="shift-current-card__date">
            {formatShiftDate(currentShiftStartAtUtc)}
          </div>
          {shiftExpired && (
            <div className="shift-current-card__expired-badge">
              Vardiya süresi sona erdi
            </div>
          )}
        </div>
      ) : (
        <div className="shift-empty-state">
          <p>Henüz vardiya atanmadı</p>
        </div>
      )}

      {/* Assignment history */}
      {recentAssignments.length > 0 && (
        <div className="shift-history">
          <h4>Atama geçmişi</h4>
          <ul className="shift-history__list">
            {recentAssignments.map((sa) => (
              <li key={sa.id} className={`shift-history__item${sa.isActive ? " shift-history__item--active" : ""}`}>
                <span className="shift-history__code">{sa.shiftCode}</span>
                <span className="shift-history__date">{sa.shiftDate}</span>
                {sa.releasedAtUtc && (
                  <span className="shift-history__released">
                    Bırakıldı
                  </span>
                )}
              </li>
            ))}
          </ul>
        </div>
      )}

      {/* Actions */}
      {canAssign && (
        <div className="production-panel__actions">
          {panelState.kind === "idle" && (
            <button
              type="button"
              className="btn btn--secondary btn--sm"
              onClick={() => setPanelState({ kind: hasActiveShift ? "reassign-dialog" : "assign-dialog" })}
            >
              <RefreshCw aria-hidden="true" />
              {hasActiveShift ? "Vardiyayı Değiştir" : "Vardiya Ata"}
            </button>
          )}

          {(panelState.kind === "assign-dialog" || panelState.kind === "reassign-dialog") && (
            <div className="production-panel__dialog">
              <h4>{hasActiveShift ? "Vardiya Değiştir" : "Vardiya Ata"}</h4>
              <label className="form-label">
                Vardiya ID
                <input
                  type="text"
                  className="form-input"
                  placeholder="Vardiya GUID'i"
                  value={shiftId}
                  onChange={(e) => setShiftId(e.target.value)}
                />
              </label>
              <label className="form-label">
                Tarih
                <input
                  type="date"
                  className="form-input"
                  value={shiftDate}
                  onChange={(e) => setShiftDate(e.target.value)}
                />
              </label>
              {hasActiveShift && (
                <label className="form-label">
                  Değişiklik nedeni <span className="form-required">*</span>
                  <textarea
                    className="form-textarea"
                    placeholder="Vardiya değiştirme nedenini yazın"
                    rows={2}
                    value={reason}
                    onChange={(e) => setReason(e.target.value)}
                  />
                </label>
              )}
              <div className="production-panel__dialog-actions">
                <button
                  type="button"
                  className="btn btn--primary btn--sm"
                  disabled={!shiftId.trim() || (hasActiveShift && !reason.trim())}
                  onClick={handleAssignSubmit}
                >
                  {hasActiveShift ? "Değiştir" : "Ata"}
                </button>
                <button
                  type="button"
                  className="btn btn--ghost btn--sm"
                  onClick={() => { setPanelState({ kind: "idle" }); setShiftId(""); setReason(""); }}
                >
                  İptal
                </button>
              </div>
            </div>
          )}

          {panelState.kind === "pending" && (
            <p className="production-panel__pending">{panelState.action}</p>
          )}

          {panelState.kind === "error" && (
            <div className="production-panel__error" role="alert">
              <p>{panelState.message}</p>
              <button type="button" className="btn btn--ghost btn--sm" onClick={() => setPanelState({ kind: "idle" })}>
                Kapat
              </button>
            </div>
          )}
        </div>
      )}
    </div>
  );
}
