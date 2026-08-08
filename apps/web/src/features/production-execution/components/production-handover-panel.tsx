"use client";

import { useCallback, useState } from "react";
import { useRouter } from "next/navigation";
import { ClipboardList, Plus } from "lucide-react";

import type { ShiftHandoverSummary } from "../domain/production-execution-resources";
import type { createHandoverAction } from "../server/production-order-writer";

type HandoverPanelProps = Readonly<{
  orderId: string;
  version: number;
  status: string;
  currentShiftCode?: string;
  handovers: ShiftHandoverSummary[];
  onCreateHandover: typeof createHandoverAction;
}>;

type PanelState =
  | { kind: "idle" }
  | { kind: "create-dialog" }
  | { kind: "pending" }
  | { kind: "error"; message: string };

function formatHandoverDate(iso: string): string {
  try {
    return new Date(iso).toLocaleDateString("tr-TR", {
      day: "2-digit",
      month: "short",
      hour: "2-digit",
      minute: "2-digit",
    });
  } catch {
    return iso;
  }
}

export function ProductionHandoverPanel({
  orderId,
  version,
  status,
  currentShiftCode,
  handovers,
  onCreateHandover,
}: HandoverPanelProps) {
  const router = useRouter();
  const [panelState, setPanelState] = useState<PanelState>({ kind: "idle" });
  const [note, setNote] = useState("");
  const [openIssues, setOpenIssues] = useState("");

  const canCreate =
    !!currentShiftCode &&
    (status === "ready" || status === "in-progress" || status === "paused");

  const handleCreateSubmit = useCallback(async () => {
    if (!note.trim()) return;
    setPanelState({ kind: "pending" });
    try {
      const result = await onCreateHandover(
        orderId,
        version,
        note.trim(),
        openIssues.trim() || undefined,
      );
      if (result.status === "failure") {
        setPanelState({ kind: "error", message: result.message ?? "Devir notu oluşturulamadı." });
      } else {
        setPanelState({ kind: "idle" });
        setNote("");
        setOpenIssues("");
        router.refresh();
      }
    } catch {
      setPanelState({ kind: "error", message: "Bir hata oluştu." });
    }
  }, [orderId, version, note, openIssues, onCreateHandover, router]);

  return (
    <div className="production-panel production-handover-panel">
      <div className="production-panel__header">
        <ClipboardList className="production-panel__header-icon" aria-hidden="true" />
        <h3>VARDİYA DEVİR NOTU</h3>
      </div>

      {/* Recent handovers timeline */}
      {handovers.length > 0 ? (
        <ol className="handover-timeline">
          {handovers.map((h) => (
            <li key={h.id} className="handover-timeline__item">
              <div className="handover-timeline__meta">
                <time>{formatHandoverDate(h.createdAtUtc)}</time>
              </div>
              <div className="handover-timeline__body">
                <p className="handover-timeline__note">{h.note}</p>
                {h.openIssuesSummary && (
                  <div className="handover-timeline__issues">
                    <strong>Açık Konular:</strong>
                    <p>{h.openIssuesSummary}</p>
                  </div>
                )}
              </div>
            </li>
          ))}
        </ol>
      ) : (
        <div className="handover-empty-state">
          <p>Henüz devir notu oluşturulmadı</p>
        </div>
      )}

      {/* Create action */}
      {canCreate && (
        <div className="production-panel__actions">
          {panelState.kind === "idle" && (
            <button
              type="button"
              className="btn btn--secondary btn--sm"
              onClick={() => setPanelState({ kind: "create-dialog" })}
            >
              <Plus aria-hidden="true" />
              Devir Notu Ekle
            </button>
          )}

          {panelState.kind === "create-dialog" && (
            <div className="production-panel__dialog">
              <h4>Vardiya Devir Notu</h4>
              <label className="form-label">
                Operasyonel not <span className="form-required">*</span>
                <textarea
                  className="form-textarea"
                  placeholder="Üretim durumunu, yapılan işlemleri ve sonraki vardiyaya aktarılacak bilgileri yazın"
                  rows={3}
                  value={note}
                  onChange={(e) => setNote(e.target.value)}
                />
              </label>
              <label className="form-label">
                Açık konular
                <textarea
                  className="form-textarea"
                  placeholder="Çözülmemiş sorunlar, takip edilmesi gereken konular (isteğe bağlı)"
                  rows={2}
                  value={openIssues}
                  onChange={(e) => setOpenIssues(e.target.value)}
                />
              </label>
              <div className="production-panel__dialog-actions">
                <button
                  type="button"
                  className="btn btn--primary btn--sm"
                  disabled={!note.trim()}
                  onClick={handleCreateSubmit}
                >
                  Devir Notunu Kaydet
                </button>
                <button
                  type="button"
                  className="btn btn--ghost btn--sm"
                  onClick={() => { setPanelState({ kind: "idle" }); setNote(""); setOpenIssues(""); }}
                >
                  İptal
                </button>
              </div>
            </div>
          )}

          {panelState.kind === "pending" && (
            <p className="production-panel__pending">Devir notu kaydediliyor…</p>
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

      {!canCreate && !currentShiftCode && (
        <p className="handover-empty-state__hint">Devir notu oluşturmak için aktif vardiya ataması gereklidir</p>
      )}
    </div>
  );
}
