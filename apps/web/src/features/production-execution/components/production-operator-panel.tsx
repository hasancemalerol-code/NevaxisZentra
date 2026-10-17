"use client";

import { displayActor } from "@/lib/presentation/display-label";
import { useCallback, useState } from "react";
import { useRouter } from "next/navigation";
import { UserCheck, UserPlus, UserX } from "lucide-react";

import type { OperatorAssignmentSummary, OperatorAssignmentRole } from "../domain/production-execution-resources";
import { OPERATOR_ASSIGNMENT_ROLES, getOperatorRoleLabel } from "../domain/production-execution-resources";
import type { assignOperatorAction, releaseOperatorAction } from "../server/production-order-writer";
import { customerMessage } from "@/lib/presentation/customer-message";

type OperatorPanelProps = Readonly<{
  orderId: string;
  version: number;
  status: string;
  primaryOperatorSubjectId?: string;
  activeOperatorCount?: number;
  recentAssignments: OperatorAssignmentSummary[];
  onAssignOperator: typeof assignOperatorAction;
  onReleaseOperator: typeof releaseOperatorAction;
}>;

type PanelState =
  | { kind: "idle" }
  | { kind: "assign-dialog" }
  | { kind: "release-dialog"; assignmentId: string; subjectId: string }
  | { kind: "pending"; action: string }
  | { kind: "error"; message: string };

function shortSubject(id: string): string {
  return displayActor(id);
  return id;
}

export function ProductionOperatorPanel({
  orderId,
  version,
  status,
  primaryOperatorSubjectId,
  activeOperatorCount,
  recentAssignments,
  onAssignOperator,
  onReleaseOperator,
}: OperatorPanelProps) {
  const router = useRouter();
  const [panelState, setPanelState] = useState<PanelState>({ kind: "idle" });
  const [subjectId, setSubjectId] = useState("");
  const [assignmentRole, setAssignmentRole] = useState<OperatorAssignmentRole>("operator");
  const [releaseReason, setReleaseReason] = useState("");

  const canAssign = status === "ready" || status === "in-progress" || status === "paused";
  const activeAssignments = recentAssignments.filter((a) => a.isActive);

  const handleAssignSubmit = useCallback(async () => {
    if (!subjectId.trim()) return;
    setPanelState({ kind: "pending", action: "Operatör atanıyor…" });
    try {
      const result = await onAssignOperator(orderId, version, subjectId.trim(), assignmentRole);
      if (result.status === "failure") {
        setPanelState({ kind: "error", message: result.message ?? "Operatör atanamadı." });
      } else {
        setPanelState({ kind: "idle" });
        setSubjectId("");
        router.refresh();
      }
    } catch {
      setPanelState({ kind: "error", message: "Bir hata oluştu." });
    }
  }, [orderId, version, subjectId, assignmentRole, onAssignOperator, router]);

  const handleReleaseSubmit = useCallback(async (assignmentId: string) => {
    if (!releaseReason.trim()) return;
    setPanelState({ kind: "pending", action: "Operatör görevden çıkarılıyor…" });
    try {
      const result = await onReleaseOperator(orderId, assignmentId, version, releaseReason.trim());
      if (result.status === "failure") {
        setPanelState({ kind: "error", message: result.message ?? "Operatör çıkarılamadı." });
      } else {
        setPanelState({ kind: "idle" });
        setReleaseReason("");
        router.refresh();
      }
    } catch {
      setPanelState({ kind: "error", message: "Bir hata oluştu." });
    }
  }, [orderId, version, releaseReason, onReleaseOperator, router]);

  return (
    <div className="production-panel production-operator-panel">
      <div className="production-panel__header">
        <UserCheck className="production-panel__header-icon" aria-hidden="true" />
        <h3>OPERATÖRLER</h3>
        {activeOperatorCount !== undefined && activeOperatorCount > 0 && (
          <span className="production-panel__header-count">{activeOperatorCount}</span>
        )}
      </div>

      {/* Active assignments */}
      {activeAssignments.length > 0 ? (
        <ul className="operator-list">
          {activeAssignments.map((a) => (
            <li key={a.id} className="operator-list__item">
              <div className="operator-list__item-info">
                <span className="operator-list__role-badge">
                  {getOperatorRoleLabel(a.assignmentRole)}
                </span>
                <span className="operator-list__subject">
                  {a.subjectId === primaryOperatorSubjectId
                    ? <strong>{shortSubject(a.subjectId)}</strong>
                    : shortSubject(a.subjectId)}
                </span>
              </div>
              {canAssign && (
                <button
                  type="button"
                  className="btn btn--ghost btn--sm btn--icon-only"
                  title="Görevden çıkar"
                  onClick={() => {
                    setReleaseReason("");
                    setPanelState({ kind: "release-dialog", assignmentId: a.id, subjectId: a.subjectId });
                  }}
                >
                  <UserX aria-hidden="true" />
                </button>
              )}
            </li>
          ))}
        </ul>
      ) : (
        <div className="operator-empty-state">
          <p>Henüz operatör atanmadı</p>
          {!primaryOperatorSubjectId && canAssign && (
            <p className="operator-empty-state__hint">Ana operatör atanmadan üretim başlatılamaz</p>
          )}
        </div>
      )}

      {/* Actions */}
      {canAssign && (
        <div className="production-panel__actions">
          {panelState.kind === "idle" && (
            <button
              type="button"
              className="btn btn--secondary btn--sm"
              onClick={() => setPanelState({ kind: "assign-dialog" })}
            >
              <UserPlus aria-hidden="true" />
              Operatör Ekle
            </button>
          )}

          {panelState.kind === "assign-dialog" && (
            <div className="production-panel__dialog">
              <h4>Operatör Ata</h4>
              <label className="form-label">
                Operatör SubjectId
                <input
                  type="text"
                  className="form-input"
                  placeholder="Subject ID"
                  value={subjectId}
                  onChange={(e) => setSubjectId(e.target.value)}
                />
              </label>
              <label className="form-label">
                Görev rolü
                <select
                  className="form-select"
                  value={assignmentRole}
                  onChange={(e) => setAssignmentRole(e.target.value as OperatorAssignmentRole)}
                >
                  {OPERATOR_ASSIGNMENT_ROLES.map((role) => (
                    <option key={role} value={role}>{getOperatorRoleLabel(role)}</option>
                  ))}
                </select>
              </label>
              <div className="production-panel__dialog-actions">
                <button
                  type="button"
                  className="btn btn--primary btn--sm"
                  disabled={!subjectId.trim()}
                  onClick={handleAssignSubmit}
                >
                  Ata
                </button>
                <button
                  type="button"
                  className="btn btn--ghost btn--sm"
                  onClick={() => { setPanelState({ kind: "idle" }); setSubjectId(""); }}
                >
                  İptal
                </button>
              </div>
            </div>
          )}

          {panelState.kind === "release-dialog" && (
            <div className="production-panel__dialog">
              <h4>Görevden Çıkar</h4>
              <p className="production-panel__dialog-subject">
                {shortSubject(panelState.subjectId)}
              </p>
              <label className="form-label">
                Neden <span className="form-required">*</span>
                <textarea
                  className="form-textarea"
                  placeholder="Görevden çıkarma nedenini yazın"
                  rows={2}
                  value={releaseReason}
                  onChange={(e) => setReleaseReason(e.target.value)}
                />
              </label>
              <div className="production-panel__dialog-actions">
                <button
                  type="button"
                  className="btn btn--danger btn--sm"
                  disabled={!releaseReason.trim()}
                  onClick={() => handleReleaseSubmit(panelState.assignmentId)}
                >
                  Görevden Çıkar
                </button>
                <button
                  type="button"
                  className="btn btn--ghost btn--sm"
                  onClick={() => { setPanelState({ kind: "idle" }); setReleaseReason(""); }}
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
              <p>{customerMessage(panelState.message)}</p>
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
