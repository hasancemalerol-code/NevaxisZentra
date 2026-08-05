import {
  Bot,
  CheckCircle2,
  CircleAlert,
  Database,
  FileClock,
  Inbox,
  ShieldCheck,
  type LucideIcon,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";

import type { OrderDraft } from "../domain/order-draft";
import { formatOrderDateTime } from "../presentation/order-draft-formatters";
import {
  getOrderApprovalStatusLabel,
  getOrderTransferStatusLabel,
} from "../presentation/order-draft-labels";

type OrderDraftTimelineProps = Readonly<{
  orderDraft: OrderDraft;
}>;

type TimelineTone =
  | "neutral"
  | "information"
  | "success"
  | "danger"
  | "ai";

type TimelineEvent = Readonly<{
  id: string;
  title: string;
  description: string;
  occurredAt?: string;
  icon: LucideIcon;
  tone: TimelineTone;
}>;

function getFirstSourceReceivedAt(
  orderDraft: OrderDraft,
): string | undefined {
  return [...orderDraft.sources]
    .map((source) => source.receivedAt)
    .sort(
      (left, right) =>
        new Date(left).getTime() -
        new Date(right).getTime(),
    )[0];
}

function buildTimelineEvents(
  orderDraft: OrderDraft,
): readonly TimelineEvent[] {
  const events: TimelineEvent[] = [
    {
      id: "source-received",
      title: "Sipariş talebi alındı",
      description:
        `${orderDraft.sources.length} kaynak kaydı sipariş bağlamında toplandı.`,
      occurredAt:
        getFirstSourceReceivedAt(orderDraft),
      icon: Inbox,
      tone: "neutral",
    },
    {
      id: "draft-created",
      title: "AI sipariş taslağını oluşturdu",
      description:
        "Müşteri, ürün, miktar, fiyat ve termin bilgileri kaynaklardan çıkarıldı.",
      occurredAt: orderDraft.createdAt,
      icon: Bot,
      tone: "ai",
    },
  ];

  if (
    orderDraft.updatedAt !==
    orderDraft.createdAt
  ) {
    events.push({
      id: "draft-updated",
      title: "Sipariş taslağı güncellendi",
      description:
        "Eşleşmeler, doğrulama sonuçları veya ticari bilgiler güncellendi.",
      occurredAt: orderDraft.updatedAt,
      icon: FileClock,
      tone: "information",
    });
  }

  if (orderDraft.approval.requestedAt) {
    events.push({
      id: "approval-requested",
      title: "Yetkili onayı istendi",
      description:
        getOrderApprovalStatusLabel(
          orderDraft.approval.status,
        ),
      occurredAt:
        orderDraft.approval.requestedAt,
      icon: ShieldCheck,
      tone: "information",
    });
  }

  if (orderDraft.approval.approvedAt) {
    events.push({
      id: "approval-approved",
      title: "Sipariş taslağı onaylandı",
      description: orderDraft.approval.approvedBy
        ? `${orderDraft.approval.approvedBy} tarafından onaylandı.`
        : "Yetkili kullanıcı tarafından onaylandı.",
      occurredAt:
        orderDraft.approval.approvedAt,
      icon: CheckCircle2,
      tone: "success",
    });
  }

  if (orderDraft.approval.rejectedAt) {
    events.push({
      id: "approval-rejected",
      title: "Sipariş taslağı reddedildi",
      description:
        orderDraft.approval.rejectionReason ??
        "Yetkili kullanıcı sipariş taslağını reddetti.",
      occurredAt:
        orderDraft.approval.rejectedAt,
      icon: CircleAlert,
      tone: "danger",
    });
  }

  if (orderDraft.transfer.startedAt) {
    events.push({
      id: "transfer-started",
      title: "ERP aktarımı başlatıldı",
      description:
        getOrderTransferStatusLabel(
          orderDraft.transfer.status,
        ),
      occurredAt:
        orderDraft.transfer.startedAt,
      icon: Database,
      tone: "information",
    });
  }

  if (orderDraft.transfer.completedAt) {
    events.push({
      id: "transfer-completed",
      title: "ERP aktarımı tamamlandı",
      description:
        orderDraft.transfer.externalOrderId
          ? `ERP sipariş numarası: ${orderDraft.transfer.externalOrderId}`
          : "Sipariş bağlı ERP sistemine aktarıldı.",
      occurredAt:
        orderDraft.transfer.completedAt,
      icon: CheckCircle2,
      tone: "success",
    });
  }

  if (
    orderDraft.transfer.status === "failed"
  ) {
    events.push({
      id: "transfer-failed",
      title: "ERP aktarımı başarısız",
      description:
        orderDraft.transfer.errorMessage ??
        orderDraft.transfer.errorCode ??
        "Sipariş bağlı ERP sistemine aktarılamadı.",
      occurredAt:
        orderDraft.transfer.completedAt ??
        orderDraft.transfer.startedAt,
      icon: CircleAlert,
      tone: "danger",
    });
  }

  return events.sort((left, right) => {
    if (!left.occurredAt) {
      return 1;
    }

    if (!right.occurredAt) {
      return -1;
    }

    return (
      new Date(right.occurredAt).getTime() -
      new Date(left.occurredAt).getTime()
    );
  });
}

export function OrderDraftTimeline({
  orderDraft,
}: OrderDraftTimelineProps) {
  const events = buildTimelineEvents(
    orderDraft,
  );

  return (
    <Card className="order-draft-timeline">
      <div className="order-draft-timeline__heading">
        <div>
          <p>Audit geçmişi</p>
          <h2>Sipariş işlem zaman çizelgesi</h2>
        </div>

        <Badge tone="neutral">
          <FileClock aria-hidden="true" />
          {events.length} olay
        </Badge>
      </div>

      <ol className="order-draft-timeline__list">
        {events.map((event) => {
          const Icon = event.icon;

          return (
            <li
              data-tone={event.tone}
              key={event.id}
            >
              <span className="order-draft-timeline__icon">
                <Icon aria-hidden="true" />
              </span>

              <div>
                <div className="order-draft-timeline__event-heading">
                  <strong>{event.title}</strong>

                  <time dateTime={event.occurredAt}>
                    {formatOrderDateTime(
                      event.occurredAt,
                    )}
                  </time>
                </div>

                <p>{event.description}</p>
              </div>
            </li>
          );
        })}
      </ol>
    </Card>
  );
}