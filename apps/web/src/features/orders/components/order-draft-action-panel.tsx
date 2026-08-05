import {
  CheckCircle2,
  CircleAlert,
  Database,
  Send,
  ShieldCheck,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";

import {
  canSubmitOrderDraftForApproval,
  canTransferOrderDraft,
  hasBlockingOrderIssues,
  type OrderDraft,
  type OrderDraftApproval,
  type OrderDraftTransfer,
} from "../domain/order-draft";

type OrderDraftActionPanelProps = Readonly<{
  orderDraft: OrderDraft;
}>;

type BadgeTone =
  | "neutral"
  | "information"
  | "success"
  | "danger"
  | "ai";

function getApprovalLabel(
  approval: OrderDraftApproval,
): string {
  switch (approval.status) {
    case "pending":
      return "Onay bekliyor";

    case "approved":
      return "Onaylandı";

    case "rejected":
      return "Reddedildi";

    case "expired":
      return "Onay süresi doldu";

    case "not-required":
    default:
      return "Henüz onaya gönderilmedi";
  }
}

function getApprovalTone(
  approval: OrderDraftApproval,
): BadgeTone {
  switch (approval.status) {
    case "approved":
      return "success";

    case "rejected":
    case "expired":
      return "danger";

    case "pending":
      return "information";

    case "not-required":
    default:
      return "neutral";
  }
}

function getTransferLabel(
  transfer: OrderDraftTransfer,
): string {
  switch (transfer.status) {
    case "pending":
      return "Aktarım bekliyor";

    case "in-progress":
      return "ERP'ye aktarılıyor";

    case "completed":
      return "ERP'ye aktarıldı";

    case "failed":
      return "Aktarım başarısız";

    case "not-started":
    default:
      return "Aktarım başlatılmadı";
  }
}

function getTransferTone(
  transfer: OrderDraftTransfer,
): BadgeTone {
  switch (transfer.status) {
    case "completed":
      return "success";

    case "failed":
      return "danger";

    case "pending":
    case "in-progress":
      return "information";

    case "not-started":
    default:
      return "neutral";
  }
}

export function OrderDraftActionPanel({
  orderDraft,
}: OrderDraftActionPanelProps) {
  const hasBlockingIssues =
    hasBlockingOrderIssues(orderDraft);

  const canSubmitForApproval =
    canSubmitOrderDraftForApproval(orderDraft);

  const canTransfer =
    canTransferOrderDraft(orderDraft);

  return (
    <Card className="order-draft-action-panel">
      <div className="order-draft-action-panel__heading">
        <div>
          <p>Human-in-the-Loop</p>
          <h2>Onay ve sistem uygulaması</h2>
        </div>

        <Badge
          tone={
            hasBlockingIssues
              ? "danger"
              : "success"
          }
        >
          {hasBlockingIssues ? (
            <CircleAlert aria-hidden="true" />
          ) : (
            <ShieldCheck aria-hidden="true" />
          )}

          {hasBlockingIssues
            ? "Engelleyici sorun var"
            : "Kontroller tamam"}
        </Badge>
      </div>

      <div className="order-draft-action-panel__statuses">
        <div>
          <span>Yetkili onayı</span>

          <Badge
            tone={getApprovalTone(
              orderDraft.approval,
            )}
          >
            <CheckCircle2 aria-hidden="true" />
            {getApprovalLabel(
              orderDraft.approval,
            )}
          </Badge>
        </div>

        <div>
          <span>ERP aktarımı</span>

          <Badge
            tone={getTransferTone(
              orderDraft.transfer,
            )}
          >
            <Database aria-hidden="true" />
            {getTransferLabel(
              orderDraft.transfer,
            )}
          </Badge>
        </div>
      </div>

      <div className="order-draft-action-panel__actions">
        <Button
          disabled={!canSubmitForApproval}
          leadingIcon={<ShieldCheck />}
          title={
            canSubmitForApproval
              ? "Sipariş taslağını yetkili onayına gönder"
              : "Onaya gönderilebilmesi için engelleyici sorunlar çözülmeli ve taslak onaya hazır olmalıdır."
          }
          variant="secondary"
        >
          Onaya gönder
        </Button>

        <Button
          disabled={!canTransfer}
          leadingIcon={<Send />}
          title={
            canTransfer
              ? "Onaylanan siparişi ERP sistemine aktar"
              : "ERP aktarımı için yetkili onayı ve tamamlanmış doğrulama kontrolleri gereklidir."
          }
          variant="primary"
        >
          ERP&apos;ye aktar
        </Button>
      </div>

      <p className="order-draft-action-panel__notice">
        AI sipariş içeriğini kullanıcı onayı olmadan
        kesinleştiremez veya bağlı ERP sistemine
        aktaramaz.
      </p>
    </Card>
  );
}