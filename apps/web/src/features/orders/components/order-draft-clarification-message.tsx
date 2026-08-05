"use client";

import {
  useMemo,
  useState,
} from "react";
import {
  CircleAlert,
  Mail,
  MessageCircle,
  PencilLine,
  Send,
  ShieldCheck,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";

import type {
  OrderDraft,
  OrderIntakeSource,
} from "../domain/order-draft";
import { getOrderIntakeSourceLabel } from "../presentation/order-draft-labels";

type OrderDraftClarificationMessageProps = Readonly<{
  orderDraft: OrderDraft;
}>;

function resolveReplySource(
  orderDraft: OrderDraft,
): OrderIntakeSource | undefined {
  if (
    orderDraft.sources.some(
      (source) => source.source === "whatsapp",
    )
  ) {
    return "whatsapp";
  }

  if (
    orderDraft.sources.some(
      (source) => source.source === "email",
    )
  ) {
    return "email";
  }

  return undefined;
}

function createQuestionForField(
  field: string,
  fallbackMessage: string,
): string {
  switch (field) {
    case "productId":
      return "Talep ettiğiniz ürünün ürün kodunu veya tam ürün adını paylaşabilir misiniz?";

    case "requestedDeliveryDate":
      return "Sipariş için istediğiniz kesin teslim tarihini paylaşabilir misiniz?";

    case "unitPrice":
      return "Bu sipariş için geçerli birim fiyatı veya teklif referansını doğrulayabilir misiniz?";

    case "customerPurchaseOrderNumber":
      return "Müşteri sipariş numaranızı paylaşabilir misiniz?";

    default:
      return `${fallbackMessage} Bu bilgiyi doğrulayabilir misiniz?`;
  }
}

function createInitialMessage(
  orderDraft: OrderDraft,
): string {
  const customerName =
    orderDraft.customer.customerName ??
    "Değerli müşterimiz";

  const questions = orderDraft.issues.map(
    (issue, index) =>
      `${index + 1}. ${createQuestionForField(
        issue.field,
        issue.message,
      )}`,
  );

  return [
    `Merhaba ${customerName},`,
    "",
    `"${orderDraft.title}" sipariş talebinizi işleme alabilmemiz için aşağıdaki bilgileri doğrulamanızı rica ederiz:`,
    "",
    ...questions,
    "",
    "Bilgileri ilettiğinizde sipariş taslağınızı güncelleyip onayınıza sunacağız.",
  ].join("\n");
}

function getChannelIcon(
  source: OrderIntakeSource | undefined,
) {
  return source === "whatsapp"
    ? MessageCircle
    : Mail;
}

export function OrderDraftClarificationMessage({
  orderDraft,
}: OrderDraftClarificationMessageProps) {
  const replySource = resolveReplySource(
    orderDraft,
  );

  const initialMessage = useMemo(
    () => createInitialMessage(orderDraft),
    [orderDraft],
  );

  const [message, setMessage] =
    useState(initialMessage);

  const ChannelIcon = getChannelIcon(
    replySource,
  );

  const hasChanges =
    message !== initialMessage;

  const canSubmitForReview =
    Boolean(replySource) &&
    message.trim().length > 0;

  return (
    <Card className="order-draft-clarification-message">
      <div className="order-draft-clarification-message__heading">
        <div>
          <p>Gönderim önizlemesi</p>
          <h2>Açıklama mesajı taslağı</h2>
        </div>

        <Badge tone="ai">
          <PencilLine aria-hidden="true" />
          AI tarafından hazırlandı
        </Badge>
      </div>

      <div className="order-draft-clarification-message__meta">
        <div>
          <ChannelIcon aria-hidden="true" />

          <span>
            {replySource
              ? getOrderIntakeSourceLabel(
                  replySource,
                )
              : "İletişim kanalı bulunamadı"}
          </span>
        </div>

        <Badge
          tone={
            orderDraft.issues.some(
              (issue) => issue.blocking,
            )
              ? "danger"
              : "information"
          }
        >
          <CircleAlert aria-hidden="true" />
          {orderDraft.issues.length} açık konu
        </Badge>
      </div>

      <label className="order-draft-clarification-message__editor">
        <span>Mesaj içeriği</span>

        <textarea
          onChange={(event) => {
            setMessage(event.target.value);
          }}
          rows={12}
          value={message}
        />
      </label>

      <div className="order-draft-clarification-message__notice">
        <ShieldCheck aria-hidden="true" />

        <p>
          Bu mesaj müşteriye otomatik gönderilmez.
          Gönderimden önce yetkili kullanıcı mesajı
          incelemeli ve açıkça onaylamalıdır.
        </p>
      </div>

      <div className="order-draft-clarification-message__actions">
        <Button
          disabled={!hasChanges}
          onClick={() => {
            setMessage(initialMessage);
          }}
          variant="secondary"
        >
          Taslağı sıfırla
        </Button>

        <Button
          disabled={!canSubmitForReview}
          leadingIcon={<Send />}
          title={
            canSubmitForReview
              ? "Mesaj taslağını kullanıcı onayına sun"
              : "Mesaj gönderimi için bağlı bir iletişim kanalı ve geçerli mesaj içeriği gereklidir."
          }
          variant="primary"
        >
          Onaya gönder
        </Button>
      </div>
    </Card>
  );
}