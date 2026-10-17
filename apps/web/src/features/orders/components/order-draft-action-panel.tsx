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
import {
  getOrderApprovalStatusLabel,
  getOrderTransferStatusLabel,
} from "../presentation/order-draft-labels";

const ORDER_APPROVAL_SERVICE_READY = false;
const ORDER_ERP_TRANSFER_SERVICE_READY = false;

const ORDER_APPROVAL_UNAVAILABLE_REASON =
  "Onay gönderme servisi henüz kullanıma hazır değil.";
const ORDER_ERP_TRANSFER_UNAVAILABLE_REASON =
  "ERP aktarım servisi henüz kullanıma hazır değil.";

type OrderDraftActionPanelProps = Readonly<{
  orderDraft: OrderDraft;
}>;

type BadgeTone =
  | "neutral"
  | "information"
  | "success"
  | "danger"
  | "ai";

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

  const approvalDisabledReason = !ORDER_APPROVAL_SERVICE_READY
    ? ORDER_APPROVAL_UNAVAILABLE_REASON
    : !canSubmitForApproval
      ? "Onaya gönderilebilmesi için engelleyici sorunlar çözülmeli ve taslak onaya hazır olmalıdır."
      : undefined;

  const transferDisabledReason = !ORDER_ERP_TRANSFER_SERVICE_READY
    ? ORDER_ERP_TRANSFER_UNAVAILABLE_REASON
    : !canTransfer
      ? "ERP aktarımı için yetkili onayı ve tamamlanmış doğrulama kontrolleri gereklidir."
      : undefined;

  const approvalDisabled =
    !ORDER_APPROVAL_SERVICE_READY || !canSubmitForApproval;
  const transferDisabled =
    !ORDER_ERP_TRANSFER_SERVICE_READY || !canTransfer;

  return (
    <Card className="order-draft-action-panel">
      <div className="order-draft-action-panel__heading">
        <div>
          <p>İnsan Onayı</p>
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

            {getOrderApprovalStatusLabel(
              orderDraft.approval.status,
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

            {getOrderTransferStatusLabel(
              orderDraft.transfer.status,
            )}
          </Badge>
        </div>
      </div>

      <div className="order-draft-action-panel__actions">
        <Button
          disabled={approvalDisabled}
          leadingIcon={<ShieldCheck />}
          title={approvalDisabledReason}
          variant="secondary"
        >
          Onaya gönder
        </Button>

        <Button
          disabled={transferDisabled}
          leadingIcon={<Send />}
          title={transferDisabledReason}
          variant="primary"
        >
          ERP&apos;ye aktar
        </Button>
      </div>

      <p className="order-draft-action-panel__notice">
        Yapay zekâ sipariş içeriğini kullanıcı onayı olmadan
        kesinleştiremez veya bağlı ERP sistemine
        aktaramaz.
      </p>
    </Card>
  );
}