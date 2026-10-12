"use client";

import { useState, useTransition } from "react";
import { useRouter } from "next/navigation";
import { RefreshCw } from "lucide-react";

import { evaluateMaintenanceConditionAction } from "../server/maintenance-evaluation-actions";

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
    <div className="maintenance-evaluate-control" data-testid="maintenance-evaluate-control">
      <p className="maintenance-ai-muted">
        Son değerlendirme: {formatEvaluatedAt(lastEvaluatedAtUtc)}. Bu sayfayı açmak değerlendirmeyi çalıştırmaz.
      </p>
      {outdated ? (
        <p className="maintenance-ai-stale-warning" role="status">
          Değerlendirme güncel olmayabilir. Makine verisi değiştiyse değerlendirmeyi güncelleyin.
        </p>
      ) : null}
      {canManage ? (
        <button type="button" onClick={update} disabled={isPending} data-testid="maintenance-evaluate-button">
          <RefreshCw aria-hidden="true" /> Değerlendirmeyi güncelle
        </button>
      ) : null}
      {error ? (
        <p role="alert" className="maintenance-ai-message error">
          {error}
        </p>
      ) : null}
    </div>
  );
}
