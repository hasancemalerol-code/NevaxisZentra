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
  label: string;
  icon: LucideIcon;
}>;

const SOURCE_PRESENTATIONS: Readonly<
  Record<OrderIntakeSource, SourcePresentation>
> = {
  email: {
    label: "E-posta",
    icon: Mail,
  },
  whatsapp: {
    label: "WhatsApp",
    icon: MessageCircle,
  },
  erp: {
    label: "ERP",
    icon: Database,
  },
  "customer-portal": {
    label: "Müşteri Portalı",
    icon: Globe2,
  },
  "sales-representative": {
    label: "Satış Temsilcisi",
    icon: UserRound,
  },
  "web-form": {
    label: "Web Formu",
    icon: Globe2,
  },
  "file-upload": {
    label: "Dosya Yükleme",
    icon: FileUp,
  },
};

const STATUS_LABELS: Readonly<
  Record<OrderDraftStatus, string>
> = {
  collecting: "Veri toplanıyor",
  "needs-review": "İnceleme gerekiyor",
  "ready-for-approval": "Onaya hazır",
  approved: "Onaylandı",
  rejected: "Reddedildi",
  transferring: "ERP'ye aktarılıyor",
  transferred: "ERP'ye aktarıldı",
  "transfer-failed": "Aktarım başarısız",
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

function getConfidenceLabel(
  confidenceLevel: OrderMatchConfidence,
): string {
  switch (confidenceLevel) {
    case "high":
      return "Yüksek güven";

    case "medium":
      return "Orta güven";

    case "low":
    default:
      return "Düşük güven";
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

function formatConfidence(
  confidence: number,
): string {
  return new Intl.NumberFormat("tr-TR", {
    style: "percent",
    maximumFractionDigits: 0,
  }).format(confidence);
}

function formatDateTime(
  value: string,
): string {
  return new Intl.DateTimeFormat("tr-TR", {
    dateStyle: "short",
    timeStyle: "short",
  }).format(new Date(value));
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
                {presentation.label}
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

          {STATUS_LABELS[orderDraft.status]}
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
          {getConfidenceLabel(
            orderDraft.confidenceLevel,
          )}
          · {formatConfidence(
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
            {orderDraft.approval.status === "approved"
              ? "Onaylandı"
              : orderDraft.approval.status === "pending"
                ? "Onay bekliyor"
                : orderDraft.approval.status === "rejected"
                  ? "Reddedildi"
                  : "Henüz gönderilmedi"}
          </dd>
        </div>

        <div>
          <dt>ERP aktarımı</dt>

          <dd>
            {orderDraft.transfer.status === "completed"
              ? "Tamamlandı"
              : orderDraft.transfer.status === "in-progress"
                ? "Devam ediyor"
                : orderDraft.transfer.status === "failed"
                  ? "Başarısız"
                  : "Başlatılmadı"}
          </dd>
        </div>
      </dl>

      <div className="order-draft-card__footer">
        <span>
          Son güncelleme:
          {" "}
          {formatDateTime(orderDraft.updatedAt)}
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