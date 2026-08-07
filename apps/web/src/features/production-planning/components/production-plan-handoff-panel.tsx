"use client";

import { useCallback, useState } from "react";
import { useRouter } from "next/navigation";
import { PackageCheck, CheckCircle2, AlertTriangle } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";

import type { prepareHandoffAction } from "../server/production-plan-handoff-actions";

export type ProductionPlanHandoffPanelProps = Readonly<{
  planId: string;
  planStatus: string;
  version: number;
  planTitle: string;
  transferStatus: string;
  onPrepareHandoff: typeof prepareHandoffAction;
}>;

type HandoffPanelState =
  | { kind: "idle" }
  | { kind: "confirm" }
  | { kind: "pending" }
  | { kind: "done"; handoffId: string; preparedAtUtc: string }
  | { kind: "error"; message: string };

function getHandoffEligibilityMessage(
  planStatus: string,
  transferStatus: string,
): string | null {
  if (planStatus !== "scheduled") {
    return "Üretime aktarım hazırlamak için plan zamanlanmış olmalıdır.";
  }
  if (transferStatus === "uncertain") {
    return "Aktarım durumu belirsiz olduğundan handoff yapılamaz. Önce aktarım durumunu mutabakat edin.";
  }
  return null;
}

type ConfirmDialogProps = Readonly<{
  planTitle: string;
  onCancel: () => void;
  onConfirm: () => void;
  isPending: boolean;
}>;

function ConfirmDialog({ planTitle, onCancel, onConfirm, isPending }: ConfirmDialogProps) {
  return (
    <div
      className="workflow-action-dialog"
      role="dialog"
      aria-modal="true"
      aria-label="Üretime Aktarım Hazırla"
    >
      <div className="workflow-action-dialog__header">
        <h3>Üretime Aktarım Hazırla</h3>
        <p>
          <strong>{planTitle}</strong> planının üretim modülü için hazırlık
          kaydı oluşturulacak. Bu işlem üretim planının anlık görüntüsünü alır.
          Devam etmek istiyor musunuz?
        </p>
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
          onClick={onConfirm}
          disabled={isPending}
        >
          {isPending ? "Hazırlanıyor…" : "Üretime Aktarım Hazırla"}
        </button>
      </div>
    </div>
  );
}

export function ProductionPlanHandoffPanel({
  planId,
  planStatus,
  version,
  planTitle,
  transferStatus,
  onPrepareHandoff,
}: ProductionPlanHandoffPanelProps) {
  const router = useRouter();
  const [state, setState] = useState<HandoffPanelState>({ kind: "idle" });

  const isScheduled = planStatus === "scheduled";
  const ineligibilityMessage = getHandoffEligibilityMessage(planStatus, transferStatus);

  const handlePrepare = useCallback(async () => {
    setState({ kind: "pending" });

    const result = await onPrepareHandoff(planId, version);

    if (result.status === "success") {
      setState({
        kind: "done",
        handoffId: result.value.handoffId,
        preparedAtUtc: result.value.preparedAtUtc,
      });
      router.refresh();
      return;
    }

    setState({
      kind: "error",
      message:
        result.code === "handoff-already-prepared"
          ? "Bu plan versiyonu için zaten bir üretim aktarım kaydı mevcut."
          : result.code === "concurrency-conflict"
            ? "Plan başka bir kullanıcı tarafından güncellendi. Sayfayı yenileyin."
            : result.code === "blocking-risk-present"
              ? "Planda çözümlenmemiş engelleyici riskler bulunuyor."
              : result.code === "handoff-not-eligible"
                ? "Plan üretime aktarım için uygun değil."
                : result.message,
    });
  }, [planId, version, onPrepareHandoff, router]);

  if (!isScheduled) {
    return null;
  }

  return (
    <section className="production-plan-workflow-actions">
      <div className="production-plan-workflow-actions__heading">
        <div>
          <p>Üretim Modülü Aktarımı</p>
          <h2>Üretime aktarım hazırlığı</h2>
          <span>
            Zamanlanmış planın üretim modülü tarafından alınabilmesi için
            idempotent aktarım kaydı oluşturur. Gerçek üretim emri bu
            modül tarafından oluşturulur.
          </span>
        </div>

        <Badge tone="information">
          <PackageCheck aria-hidden="true" />
          Zamanlandı
        </Badge>
      </div>

      {ineligibilityMessage ? (
        <Card>
          <div className="workflow-action-info" role="note">
            <AlertTriangle aria-hidden="true" />
            <span>{ineligibilityMessage}</span>
          </div>
        </Card>
      ) : null}

      {state.kind === "error" ? (
        <div className="workflow-action-error" role="alert">
          <strong>İşlem başarısız:</strong> {state.message}
          <button
            type="button"
            className="button button-ghost button-small"
            onClick={() => { setState({ kind: "idle" }); }}
          >
            Kapat
          </button>
        </div>
      ) : null}

      {state.kind === "done" ? (
        <Card>
          <div className="workflow-action-success-state">
            <CheckCircle2 aria-hidden="true" />
            <div>
              <strong>Üretim Aktarım Kaydı Oluşturuldu</strong>
              <span>
                Kayıt ID: {state.handoffId}
              </span>
              <span>
                Hazırlanma zamanı:{" "}
                {new Date(state.preparedAtUtc).toLocaleString("tr-TR")}
              </span>
              <span className="text-muted">
                Üretim modülü tarafından alınmaya hazır.
              </span>
            </div>
          </div>
        </Card>
      ) : state.kind === "confirm" || state.kind === "pending" ? (
        <ConfirmDialog
          planTitle={planTitle}
          onCancel={() => { setState({ kind: "idle" }); }}
          onConfirm={() => { void handlePrepare(); }}
          isPending={state.kind === "pending"}
        />
      ) : !ineligibilityMessage ? (
        <div className="production-plan-workflow-actions__buttons">
          <button
            type="button"
            className="button button-secondary"
            onClick={() => { setState({ kind: "confirm" }); }}
            aria-label={`${planTitle} için üretim aktarım kaydı oluştur`}
          >
            <PackageCheck aria-hidden="true" />
            Üretime Aktarım Hazırla
          </button>
        </div>
      ) : null}
    </section>
  );
}
