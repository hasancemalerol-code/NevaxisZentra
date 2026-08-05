import {
  formatOrderConfidence,
  formatOrderShortDateTime,
} from "../presentation/order-draft-formatters";
import Link from "next/link";
import {
  ArrowRight,
  CheckCircle2,
  CircleAlert,
  Database,
  FileUp,
  Globe2,
  Mail,
  MessageCircle,
  RefreshCw,
  ShoppingBasket,
  UserRound,
  type LucideIcon,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";

import type {
  OrderDraft,
  OrderDraftStatus,
  OrderIntakeSource,
  OrderMatchConfidence,
} from "../domain/order-draft";
import {
  getOrderApprovalStatusLabel,
  getOrderConfidenceLabel,
  getOrderDraftStatusLabel,
  getOrderIntakeSourceLabel,
  getOrderTransferStatusLabel,
} from "../presentation/order-draft-labels";

type OrderDraftCardProps = Readonly<{
  orderDraft: OrderDraft;
}>;

type BadgeTone =
  | "neutral"
  | "information"
  | "success"
  | "danger"
  | "ai";

type SourcePresentation = Readonly<{
  icon: LucideIcon;
}>;

const SOURCE_PRESENTATIONS: Readonly<
  Record<OrderIntakeSource, SourcePresentation>
> = {
  email: {
    icon: Mail,
  },
  whatsapp: {
    icon: MessageCircle,
  },
  erp: {
    icon: Database,
  },
  "customer-portal": {
    icon: Globe2,
  },
  "sales-representative": {
    icon: UserRound,
  },
  "web-form": {
    icon: Globe2,
  },
  "file-upload": {
    icon: FileUp,
  },
};

function getStatusTone(
  status: OrderDraftStatus,
): BadgeTone {
  switch (status) {
    case "approved":
    case "transferred":
      return "success";

    case "needs-review":
    case "ready-for-approval":
    case "collecting":
    case "transferring":
      return "information";

    case "rejected":
    case "transfer-failed":
      return "danger";

    default:
      return "neutral";
  }
}

function getConfidenceTone(
  confidenceLevel: OrderMatchConfidence,
): BadgeTone {
  switch (confidenceLevel) {
    case "high":
      return "success";

    case "medium":
      return "information";

    case "low":
    default:
      return "danger";
  }
}

export function OrderDraftCard({
  orderDraft,
}: OrderDraftCardProps) {
  const blockingIssueCount =
    orderDraft.issues.filter(
      (issue) => issue.blocking,
    ).length;

  return (
    <Card className="order-draft-card">
      <div className="order-draft-card__header">
        <div className="order-draft-card__sources">
          {orderDraft.sources.map((source) => {
            const presentation =
              SOURCE_PRESENTATIONS[source.source];

            const Icon = presentation.icon;

            return (
              <Badge
                key={source.id}
                tone="neutral"
              >
                <Icon aria-hidden="true" />
                {getOrderIntakeSourceLabel(
                  source.source,
                )}
              </Badge>
            );
          })}
        </div>

        <Badge tone={getStatusTone(orderDraft.status)}>
          {orderDraft.status === "approved" ||
          orderDraft.status === "transferred" ? (
            <CheckCircle2 aria-hidden="true" />
          ) : orderDraft.status === "rejected" ||
            orderDraft.status === "transfer-failed" ? (
            <CircleAlert aria-hidden="true" />
          ) : (
            <RefreshCw aria-hidden="true" />
          )}

          {getOrderDraftStatusLabel(
            orderDraft.status,
          )}
        </Badge>
      </div>

      <div className="order-draft-card__content">
        <div>
          <p className="order-draft-card__eyebrow">
            Sipariş taslağı
          </p>

          <h2>{orderDraft.title}</h2>

          <p className="order-draft-card__customer">
            {orderDraft.customer.customerName ??
              "Müşteri eşleşmesi bekleniyor"}
          </p>
        </div>

        <Badge
          tone={getConfidenceTone(
            orderDraft.confidenceLevel,
          )}
        >
          {getOrderConfidenceLabel(
            orderDraft.confidenceLevel,
          )}
          {" · "}
          {formatOrderConfidence(
            orderDraft.overallConfidence,
          )}
        </Badge>
      </div>

      <dl className="order-draft-card__metrics">
        <div>
          <dt>
            <ShoppingBasket aria-hidden="true" />
            Sipariş satırı
          </dt>

          <dd>{orderDraft.lines.length}</dd>
        </div>

        <div>
          <dt>
            <CircleAlert aria-hidden="true" />
            Açık sorun
          </dt>

          <dd>
            {orderDraft.issues.length}

            {blockingIssueCount > 0 ? (
              <span>
                {blockingIssueCount} engelleyici
              </span>
            ) : null}
          </dd>
        </div>

        <div>
          <dt>Onay durumu</dt>

          <dd>
            {getOrderApprovalStatusLabel(
              orderDraft.approval.status,
            )}
          </dd>
        </div>

        <div>
          <dt>ERP aktarımı</dt>

          <dd>
            {getOrderTransferStatusLabel(
              orderDraft.transfer.status,
            )}
          </dd>
        </div>
      </dl>

      <div className="order-draft-card__footer">
        <span>
          Son güncelleme:
          {" "}
          {formatOrderShortDateTime(orderDraft.updatedAt)}
        </span>

        <Link
          className="order-draft-card__link"
          href={`/siparisler/${orderDraft.id}`}
        >
          Taslağı incele
          <ArrowRight aria-hidden="true" />
        </Link>
      </div>
    </Card>
  );
}