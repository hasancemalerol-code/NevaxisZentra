"use client";

import { useCallback, useState } from "react";
import { useRouter } from "next/navigation";
import { ArrowRightLeft, AlertTriangle, CheckCircle2, RefreshCw } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";

import type { createTransferRequestAction, executeTransferAction } from "../server/production-plan-transfer-actions";

export type ProductionPlanTransferPanelProps = Readonly<{
  planId: string;
  planStatus: string;
  version: number;
  planTitle: string;
  transferStatus: string;
  onCreateTransferRequest: typeof createTransferRequestAction;
  onExecuteTransfer: typeof executeTransferAction;
}>;

type TransferPanelState =
  | { kind: "idle" }
  | { kind: "confirm-request" }
  | { kind: "pending-request" }
  | { kind: "request-created"; transferRequestId: string; targetSystem: string; requestedAtUtc: string }
  | { kind: "confirm-execute"; transferRequestId: string }
  | { kind: "pending-execute" }
  | { kind: "execute-done"; transferStatus: string; externalReference: string | null; failureMessage: string | null; retryable: boolean; reconciliationRequired: boolean; transferRequestId: string }
  | { kind: "error"; message: string };

const TARGET_SYSTEMS = [
  { value: "erp", label: "ERP" },
  { value: "mes", label: "MES" },
] as const;

function getTransferStatusBadgeTone(status: string): "success" | "warning" | "danger" | "information" | "neutral" {
  if (status === "succeeded" || status === "completed") return "success";
  if (status === "failed") return "danger";
  if (status === "uncertain") return "warning";
  if (status === "processing" || status === "in-progress") return "information";
  if (status === "requested" || status === "pending") return "neutral";
  return "neutral";
}

function getTransferStatusLabel(status: string): string {
  const labels: Record<string, string> = {
    "not-started": "Başlatılmadı",
    "pending": "Beklemede",
    "in-progress": "İşleniyor",
    "requested": "Talep Edildi",
    "processing": "İşleniyor",
    "succeeded": "Aktarım Başarılı",
    "completed": "Tamamlandı",
    "failed": "Başarısız",
    "uncertain": "Belirsiz",
  };
  return labels[status] ?? status;
}

type RequestDialogProps = Readonly<{
  onCancel: () => void;
  onConfirm: (targetSystem: string) => void;
  isPending: boolean;
}>;

function RequestDialog({ onCancel, onConfirm, isPending }: RequestDialogProps) {
  const [targetSystem, setTargetSystem] = useState<string>("erp");

  return (
    <div
      className="workflow-action-dialog"
      role="dialog"
      aria-modal="true"
      aria-label="Transfer Talebi Oluştur"
    >
      <div className="workflow-action-dialog__header">
        <h3>Transfer Talebi Oluştur</h3>
        <p>
          Hedef sistemi seçin ve transfer talebini oluşturun.
          Talep oluşturulduktan sonra yetkili kullanıcı transferi uygulayabilir.
        </p>
      </div>

      <div className="workflow-action-dialog__form">
        <label className="workflow-action-dialog__field">
          <span>Hedef Sistem *</span>
          <select
            value={targetSystem}
            onChange={(e) => { setTargetSystem(e.target.value); }}
            disabled={isPending}
          >
            {TARGET_SYSTEMS.map((sys) => (
              <option key={sys.value} value={sys.value}>
                {sys.label}
              </option>
            ))}
          </select>
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
          onClick={() => { onConfirm(targetSystem); }}
          disabled={isPending}
        >
          {isPending ? "Oluşturuluyor…" : "Transfer Talebi Oluştur"}
        </button>
      </div>
    </div>
  );
}

type ExecuteDialogProps = Readonly<{
  onCancel: () => void;
  onConfirm: () => void;
  isPending: boolean;
}>;

function ExecuteDialog({ onCancel, onConfirm, isPending }: ExecuteDialogProps) {
  return (
    <div
      className="workflow-action-dialog"
      role="dialog"
      aria-modal="true"
      aria-label="Transferi Uygula"
    >
      <div className="workflow-action-dialog__header">
        <h3>Transferi Uygula</h3>
        <p>
          Üretim planı hedef sisteme aktarılacak. Bu işlem harici sisteme
          yazma gerçekleştirir. Onaylıyor musunuz?
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
          {isPending ? "Aktarılıyor…" : "Transferi Uygula"}
        </button>
      </div>
    </div>
  );
}

export function ProductionPlanTransferPanel({
  planId,
  planStatus,
  version,
  planTitle,
  transferStatus,
  onCreateTransferRequest,
  onExecuteTransfer,
}: ProductionPlanTransferPanelProps) {
  const router = useRouter();
  const [state, setState] = useState<TransferPanelState>({ kind: "idle" });

  const isScheduled = planStatus === "scheduled";
  const isTransferSucceeded = transferStatus === "succeeded" || transferStatus === "completed";
  const isTransferUncertain = transferStatus === "uncertain";

  const handleCreateRequest = useCallback(
    async (targetSystem: string) => {
      setState({ kind: "pending-request" });

      const result = await onCreateTransferRequest(planId, version, targetSystem);

      if (result.status === "success") {
        setState({
          kind: "request-created",
          transferRequestId: result.value.transferRequestId,
          targetSystem: result.value.targetSystem,
          requestedAtUtc: result.value.requestedAtUtc,
        });
        router.refresh();
        return;
      }

      setState({
        kind: "error",
        message:
          result.code === "transfer-already-succeeded"
            ? "Bu plan zaten başarıyla aktarılmış."
            : result.code === "transfer-already-requested"
              ? "Bu plan için zaten bir transfer talebi mevcut."
              : result.code === "concurrency-conflict"
                ? "Plan başka bir kullanıcı tarafından güncellendi. Sayfa yenilenerek tekrar deneyin."
                : result.message,
      });
    },
    [planId, version, onCreateTransferRequest, router],
  );

  const handleExecuteTransfer = useCallback(
    async (transferRequestId: string) => {
      setState({ kind: "pending-execute" });

      const result = await onExecuteTransfer(planId, transferRequestId, version);

      if (result.status === "success") {
        setState({
          kind: "execute-done",
          transferStatus: result.value.status,
          externalReference: result.value.externalReference,
          failureMessage: result.value.safeFailureMessage,
          retryable: result.value.retryable,
          reconciliationRequired: result.value.reconciliationRequired,
          transferRequestId,
        });
        router.refresh();
        return;
      }

      setState({
        kind: "error",
        message:
          result.code === "concurrency-conflict"
            ? "Plan başka bir kullanıcı tarafından güncellendi. Sayfayı yenileyin."
            : result.code === "invalid-transition"
              ? "Planın mevcut durumu transfere izin vermiyor."
              : result.message,
      });
    },
    [planId, version, onExecuteTransfer, router],
  );

  if (!isScheduled) {
    return null;
  }

  return (
    <section className="production-plan-workflow-actions">
      <div className="production-plan-workflow-actions__heading">
        <div>
          <p>ERP/MES Aktarım</p>
          <h2>Kontrollü sistem aktarımı</h2>
          <span>
            Zamanlanmış plan yetkili kullanıcı onayı olmadan aktarılmaz.
            Aktarım talebi oluşturulduktan sonra ayrıca uygulanması gerekir.
          </span>
        </div>

        <Badge tone={getTransferStatusBadgeTone(transferStatus)}>
          <ArrowRightLeft aria-hidden="true" />
          {getTransferStatusLabel(transferStatus)}
        </Badge>
      </div>

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

      {isTransferSucceeded ? (
        <Card>
          <div className="workflow-action-success-state">
            <CheckCircle2 aria-hidden="true" />
            <div>
              <strong>Aktarım Başarılı</strong>
              <span>
                Plan başarıyla hedef sisteme aktarıldı.
                Yeni bir transfer talebi oluşturulamaz.
              </span>
            </div>
          </div>
        </Card>
      ) : isTransferUncertain ? (
        <Card>
          <div className="workflow-action-uncertain-state">
            <AlertTriangle aria-hidden="true" />
            <div>
              <strong>Manuel Mutabakat Gerekli</strong>
              <span>
                Aktarım durumu belirsiz. Harici sistemde manuel kontrol yapılması
                gerekmektedir. Otomatik yeniden deneme yapılmaz.
              </span>
            </div>
          </div>
        </Card>
      ) : state.kind === "execute-done" ? (
        <Card>
          <div className="workflow-action-result">
            {state.transferStatus === "succeeded" ? (
              <>
                <CheckCircle2 aria-hidden="true" />
                <div>
                  <strong>Aktarım Başarılı</strong>
                  {state.externalReference ? (
                    <span>Harici Referans: {state.externalReference}</span>
                  ) : null}
                </div>
              </>
            ) : state.reconciliationRequired ? (
              <>
                <AlertTriangle aria-hidden="true" />
                <div>
                  <strong>Manuel Mutabakat Gerekli</strong>
                  <span>Aktarım durumu doğrulanamadı. Harici sistemde kontrol edin.</span>
                </div>
              </>
            ) : (
              <>
                <AlertTriangle aria-hidden="true" />
                <div>
                  <strong>Aktarım Başarısız</strong>
                  {state.failureMessage ? (
                    <span>{state.failureMessage}</span>
                  ) : null}
                  {state.retryable ? (
                    <button
                      type="button"
                      className="button button-secondary button-small"
                      onClick={() => {
                        setState({ kind: "confirm-execute", transferRequestId: state.transferRequestId });
                      }}
                    >
                      <RefreshCw aria-hidden="true" />
                      Tekrar Dene
                    </button>
                  ) : (
                    <span>Bu hata yeniden denenebilir değildir.</span>
                  )}
                </div>
              </>
            )}
          </div>
        </Card>
      ) : state.kind === "request-created" ? (
        <Card>
          <div className="workflow-action-request-created">
            <div>
              <strong>Transfer Talebi Oluşturuldu</strong>
              <span>
                Hedef sistem: {state.targetSystem.toUpperCase()} —{" "}
                {new Date(state.requestedAtUtc).toLocaleString("tr-TR")}
              </span>
            </div>

            {state.kind === "request-created" && state.transferRequestId ? (
              <button
                type="button"
                className="button button-primary"
                onClick={() => {
                  setState({
                    kind: "confirm-execute",
                    transferRequestId: state.transferRequestId,
                  });
                }}
                aria-label={`${planTitle} transferini uygula`}
              >
                <ArrowRightLeft aria-hidden="true" />
                Transferi Uygula
              </button>
            ) : null}
          </div>
        </Card>
      ) : state.kind === "confirm-execute" || state.kind === "pending-execute" ? (
        <ExecuteDialog
          onCancel={() => { setState({ kind: "idle" }); }}
          onConfirm={() => {
            if (state.kind === "confirm-execute") {
              void handleExecuteTransfer(state.transferRequestId);
            }
          }}
          isPending={state.kind === "pending-execute"}
        />
      ) : state.kind === "confirm-request" || state.kind === "pending-request" ? (
        <RequestDialog
          onCancel={() => { setState({ kind: "idle" }); }}
          onConfirm={handleCreateRequest}
          isPending={state.kind === "pending-request"}
        />
      ) : (
        <div className="production-plan-workflow-actions__buttons">
          <button
            type="button"
            className="button button-secondary"
            onClick={() => { setState({ kind: "confirm-request" }); }}
            aria-label={`${planTitle} için transfer talebi oluştur`}
          >
            <ArrowRightLeft aria-hidden="true" />
            Transfer Talebi Oluştur
          </button>
        </div>
      )}
    </section>
  );
}
