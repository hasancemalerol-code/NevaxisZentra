"use client";

import { useCallback, useState } from "react";
import { useRouter } from "next/navigation";
import { CalendarCheck } from "lucide-react";

import type { scheduleAction } from "../server/production-plan-scheduling-actions";

export type ProductionPlanSchedulingActionsProps = Readonly<{
  planId: string;
  planStatus: string;
  version: number;
  planTitle: string;
  onSchedule: typeof scheduleAction;
}>;

type SchedulingState =
  | { kind: "idle" }
  | { kind: "open-form" }
  | { kind: "pending" }
  | { kind: "error"; message: string };

function canSchedule(status: string): boolean {
  return status === "approved";
}

type SchedulingFormProps = Readonly<{
  onCancel: () => void;
  onSubmit: (
    plannedStartAtUtc: string,
    plannedEndAtUtc: string,
    shiftName?: string,
    schedulingComment?: string,
  ) => void;
  isPending: boolean;
}>;

function SchedulingForm({
  onCancel,
  onSubmit,
  isPending,
}: SchedulingFormProps) {
  const [plannedStart, setPlannedStart] = useState("");
  const [plannedEnd, setPlannedEnd] = useState("");
  const [shiftName, setShiftName] = useState("");
  const [schedulingComment, setSchedulingComment] = useState("");
  const [formError, setFormError] = useState<string | null>(null);

  const handleSubmit = useCallback(() => {
    setFormError(null);

    if (!plannedStart) {
      setFormError("Planlanan başlangıç tarihi zorunludur.");
      return;
    }

    if (!plannedEnd) {
      setFormError("Planlanan bitiş tarihi zorunludur.");
      return;
    }

    const start = new Date(plannedStart);
    const end = new Date(plannedEnd);

    if (isNaN(start.getTime()) || isNaN(end.getTime())) {
      setFormError("Geçersiz tarih formatı.");
      return;
    }

    if (start >= end) {
      setFormError("Başlangıç tarihi bitiş tarihinden önce olmalıdır.");
      return;
    }

    onSubmit(
      start.toISOString(),
      end.toISOString(),
      shiftName.trim() || undefined,
      schedulingComment.trim() || undefined,
    );
  }, [plannedStart, plannedEnd, shiftName, schedulingComment, onSubmit]);

  return (
    <div
      className="workflow-action-dialog"
      role="dialog"
      aria-modal="true"
      aria-label="Planla"
    >
      <div className="workflow-action-dialog__header">
        <h3>Üretim Planını Zamanla</h3>
        <p>
          Onaylanmış plan için üretim zamanlaması tanımlayın.
          Bu işlem geri alınamaz ve planı zamanlandı durumuna geçirir.
        </p>
      </div>

      {formError ? (
        <div className="workflow-action-dialog__error" role="alert">
          {formError}
        </div>
      ) : null}

      <div className="workflow-action-dialog__form">
        <label className="workflow-action-dialog__field">
          <span>Planlanan Başlangıç *</span>
          <input
            type="datetime-local"
            value={plannedStart}
            onChange={(e) => { setPlannedStart(e.target.value); }}
            disabled={isPending}
          />
        </label>

        <label className="workflow-action-dialog__field">
          <span>Planlanan Bitiş *</span>
          <input
            type="datetime-local"
            value={plannedEnd}
            onChange={(e) => { setPlannedEnd(e.target.value); }}
            disabled={isPending}
          />
        </label>

        <label className="workflow-action-dialog__field">
          <span>Vardiya Adı (isteğe bağlı)</span>
          <input
            type="text"
            value={shiftName}
            onChange={(e) => { setShiftName(e.target.value); }}
            placeholder="Gece vardiyası, Sabah vardiyası..."
            maxLength={100}
            disabled={isPending}
          />
        </label>

        <label className="workflow-action-dialog__field">
          <span>Planlama Notu (isteğe bağlı)</span>
          <textarea
            value={schedulingComment}
            onChange={(e) => { setSchedulingComment(e.target.value); }}
            placeholder="Zamanlama kararına ilişkin açıklama..."
            maxLength={500}
            rows={3}
            disabled={isPending}
          />
        </label>
      </div>

      <div className="workflow-action-dialog__actions">
        <button
          type="button"
          className="button button-ghost button-small"
          onClick={onCancel}
          disabled={isPending}
        >
          İptal
        </button>

        <button
          type="button"
          className="button button-primary button-small"
          onClick={handleSubmit}
          disabled={isPending}
        >
          {isPending ? "Zamanlanıyor…" : "Zamanla"}
        </button>
      </div>
    </div>
  );
}

export function ProductionPlanSchedulingActions({
  planId,
  planStatus,
  version,
  planTitle,
  onSchedule,
}: ProductionPlanSchedulingActionsProps) {
  const router = useRouter();
  const [state, setState] = useState<SchedulingState>({ kind: "idle" });

  const handleScheduleSubmit = useCallback(
    async (
      plannedStartAtUtc: string,
      plannedEndAtUtc: string,
      shiftName?: string,
      schedulingComment?: string,
    ) => {
      setState({ kind: "pending" });

      const result = await onSchedule(
        planId,
        version,
        plannedStartAtUtc,
        plannedEndAtUtc,
        shiftName,
        schedulingComment,
      );

      if (result.status === "success") {
        setState({ kind: "idle" });
        router.refresh();
        return;
      }

      setState({
        kind: "error",
        message:
          result.code === "concurrency-conflict"
            ? "Plan başka bir kullanıcı tarafından güncellendi. Sayfa yenilenerek tekrar deneyin."
            : result.code === "schedule-blocked-by-risk"
              ? "Plan engelleyici riskler içerdiğinden zamanlanamaz."
              : result.code === "invalid-transition"
                ? "Planın mevcut durumu zamanlamaya izin vermiyor."
                : result.message,
      });
    },
    [planId, version, onSchedule, router],
  );

  if (!canSchedule(planStatus)) {
    return null;
  }

  return (
    <section className="production-plan-workflow-actions">
      <div className="production-plan-workflow-actions__heading">
        <div>
          <p>Zamanlama</p>
          <h2>Üretim planını zamanla</h2>
          <span>
            Onaylanmış plan için üretim zaman çizelgesini tanımlayın.
            Zamanlama yapılmadan ERP/MES aktarımı başlatılamaz.
          </span>
        </div>
      </div>

      {state.kind === "error" ? (
        <div className="workflow-action-error" role="alert">
          <strong>Zamanlama başarısız:</strong> {state.message}
          <button
            type="button"
            className="button button-ghost button-small"
            onClick={() => { setState({ kind: "idle" }); }}
          >
            Kapat
          </button>
        </div>
      ) : null}

      {state.kind === "open-form" || state.kind === "pending" ? (
        <SchedulingForm
          onCancel={() => { setState({ kind: "idle" }); }}
          onSubmit={handleScheduleSubmit}
          isPending={state.kind === "pending"}
        />
      ) : (
        <div className="production-plan-workflow-actions__buttons">
          <button
            type="button"
            className="button button-primary"
            onClick={() => { setState({ kind: "open-form" }); }}
            aria-label={`${planTitle} planını zamanla`}
          >
            <CalendarCheck aria-hidden="true" />
            Planla
          </button>
        </div>
      )}
    </section>
  );
}
