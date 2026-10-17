"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { LoaderCircle, RefreshCw } from "lucide-react";

import { evaluateMaintenanceConditionAction } from "../server/maintenance-evaluation-actions";
import { customerMessage } from "@/lib/presentation/customer-message";

/** An assessment older than this is shown as possibly outdated (the evaluation's own freshness threshold). */
export const MAINTENANCE_EVALUATION_STALE_AFTER_MINUTES = 30;

export function isMaintenanceEvaluationOutdated(lastEvaluatedAtUtc: string | undefined, now: Date = new Date()): boolean {
  if (!lastEvaluatedAtUtc) {
    return true;
  }

  const evaluatedAt = new Date(lastEvaluatedAtUtc).getTime();
  if (Number.isNaN(evaluatedAt)) {
    return true;
  }

  return now.getTime() - evaluatedAt > MAINTENANCE_EVALUATION_STALE_AFTER_MINUTES * 60_000;
}

function formatEvaluatedAt(iso?: string): string {
  if (!iso) {
    return "henüz değerlendirilmedi";
  }

  return new Date(iso).toLocaleString("tr-TR", { day: "2-digit", month: "short", hour: "2-digit", minute: "2-digit" });
}

export function MaintenanceEvaluateControl({
  lastEvaluatedAtUtc,
  canManage,
}: Readonly<{ lastEvaluatedAtUtc?: string; canManage: boolean }>) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [error, setError] = useState<string | null>(null);
  const outdated = isMaintenanceEvaluationOutdated(lastEvaluatedAtUtc);
  // A page that was never evaluated is a neutral starting state, not a warning; only an aged evaluation is flagged.
  const neverEvaluated = !lastEvaluatedAtUtc;
  const showStaleWarning = outdated && !neverEvaluated;

  const update = () =>
    startTransition(async () => {
      setError(null);
      const result = await evaluateMaintenanceConditionAction();
      if (result.status === "failure") {
        setError(result.message);
        return;
      }

      router.refresh();
    });

  return (
    <div
      className={`zx-notice zx-notice--${showStaleWarning ? "warning" : "neutral"} zx-notice--with-actions maintenance-evaluate-control`}
      data-testid="maintenance-evaluate-control"
    >
      <RefreshCw aria-hidden="true" />
      <div className="zx-notice__body">
        <span className="zx-notice__title">Kural tabanlı değerlendirme</span>
        <p className="maintenance-ai-muted">
          Son değerlendirme: {formatEvaluatedAt(lastEvaluatedAtUtc)}. Bu sayfayı açmak değerlendirmeyi çalıştırmaz.
        </p>
        {showStaleWarning ? (
          <p className="maintenance-ai-stale-warning" role="status">
            Değerlendirme güncel olmayabilir. Makine verisi değiştiyse değerlendirmeyi güncelleyin.
          </p>
        ) : null}
        {error ? (
          <p role="alert" className="maintenance-ai-message error">
            {customerMessage(error)}
          </p>
        ) : null}
      </div>
      {canManage ? (
        <div className="zx-notice__actions">
          <button
            type="button"
            className="zx-button zx-button--primary"
            onClick={update}
            disabled={isPending}
            data-testid="maintenance-evaluate-button"
          >
            {isPending ? <LoaderCircle aria-hidden="true" className="zx-spin" /> : <RefreshCw aria-hidden="true" />}
            Değerlendirmeyi güncelle
          </button>
        </div>
      ) : null}
    </div>
  );
}
