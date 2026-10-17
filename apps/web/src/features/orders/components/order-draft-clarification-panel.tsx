import {
  CircleAlert,
  Mail,
  MessageCircle,
  Send,
  ShieldCheck,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";

import type {
  OrderDraft,
  OrderFieldIssue,
  OrderIntakeSource,
} from "../domain/order-draft";
import {
  getOrderFieldIssueLabel,
  getOrderIntakeSourceLabel,
} from "../presentation/order-draft-labels";

type OrderDraftClarificationPanelProps = Readonly<{
  orderDraft: OrderDraft;
}>;

type ClarificationQuestion = Readonly<{
  id: string;
  issueId: string;
  field: string;
  question: string;
  blocking: boolean;
}>;

function getPreferredReplySource(
  orderDraft: OrderDraft,
): OrderIntakeSource | undefined {
  const whatsappSource = orderDraft.sources.find(
    (source) => source.source === "whatsapp",
  );

  if (whatsappSource) {
    return "whatsapp";
  }

  const emailSource = orderDraft.sources.find(
    (source) => source.source === "email",
  );

  if (emailSource) {
    return "email";
  }

  return undefined;
}

function createClarificationQuestion(
  issue: OrderFieldIssue,
): ClarificationQuestion {
  switch (issue.field) {
    case "productId":
      return {
        id: `question-${issue.id}`,
        issueId: issue.id,
        field: issue.field,
        question:
          "Talep ettiğiniz ürünün ürün kodunu veya tam ürün adını paylaşabilir misiniz?",
        blocking: issue.blocking,
      };

    case "requestedDeliveryDate":
      return {
        id: `question-${issue.id}`,
        issueId: issue.id,
        field: issue.field,
        question:
          "Siparişin teslim edilmesini istediğiniz kesin tarihi paylaşabilir misiniz?",
        blocking: issue.blocking,
      };

    case "unitPrice":
      return {
        id: `question-${issue.id}`,
        issueId: issue.id,
        field: issue.field,
        question:
          "Bu sipariş için geçerli fiyat teklifini veya anlaşmalı birim fiyatı doğrulayabilir misiniz?",
        blocking: issue.blocking,
      };

    case "customerPurchaseOrderNumber":
      return {
        id: `question-${issue.id}`,
        issueId: issue.id,
        field: issue.field,
        question:
          "Müşteri sipariş numaranızı paylaşabilir misiniz?",
        blocking: issue.blocking,
      };

    default:
      return {
        id: `question-${issue.id}`,
        issueId: issue.id,
        field: issue.field,
        question:
          `${issue.message} Bu bilgiyi doğrulayabilir veya tamamlayabilir misiniz?`,
        blocking: issue.blocking,
      };
  }
}

function getSourceIcon(
  source: OrderIntakeSource | undefined,
) {
  if (source === "whatsapp") {
    return MessageCircle;
  }

  return Mail;
}

export function OrderDraftClarificationPanel({
  orderDraft,
}: OrderDraftClarificationPanelProps) {
  const questions = orderDraft.issues.map(
    createClarificationQuestion,
  );

  const preferredSource =
    getPreferredReplySource(orderDraft);

  const SourceIcon = getSourceIcon(
    preferredSource,
  );

  const blockingQuestionCount =
    questions.filter(
      (question) => question.blocking,
    ).length;

  if (questions.length === 0) {
    return (
      <Card className="order-draft-clarification-panel">
        <div className="order-draft-clarification-panel__empty">
          <ShieldCheck aria-hidden="true" />

          <div>
            <h2>Açıklayıcı soru gerekmiyor</h2>

            <p>
              Sipariş taslağında müşteriden ek bilgi
              alınmasını gerektiren açık bir sorun
              bulunmuyor.
            </p>
          </div>
        </div>
      </Card>
    );
  }

  return (
    <Card className="order-draft-clarification-panel">
      <div className="order-draft-clarification-panel__heading">
        <div>
          <p>Yapay zekâ iletişim taslağı</p>

          <h2>Müşteriden bilgi iste</h2>
        </div>

        <Badge
          tone={
            blockingQuestionCount > 0
              ? "danger"
              : "information"
          }
        >
          <CircleAlert aria-hidden="true" />

          {blockingQuestionCount > 0
            ? `${blockingQuestionCount} zorunlu soru`
            : `${questions.length} açıklama sorusu`}
        </Badge>
      </div>

      <div className="order-draft-clarification-panel__channel">
        <SourceIcon aria-hidden="true" />

        <div>
          <span>Önerilen yanıt kanalı</span>

          <strong>
            {preferredSource
              ? getOrderIntakeSourceLabel(
                  preferredSource,
                )
              : "Bağlı iletişim kanalı bulunamadı"}
          </strong>
        </div>
      </div>

      <ol className="order-draft-clarification-panel__questions">
        {questions.map((question, index) => {
          const issue = orderDraft.issues.find(
            (item) =>
              item.id === question.issueId,
          );

          return (
            <li key={question.id}>
              <span>
                {index + 1}
              </span>

              <div>
                <div>
                  <strong>
                    {issue
                      ? getOrderFieldIssueLabel(
                          issue.type,
                        )
                      : "Açıklama gerekiyor"}
                  </strong>

                  {question.blocking ? (
                    <Badge tone="danger">
                      Engelleyici
                    </Badge>
                  ) : null}
                </div>

                <p>{question.question}</p>

                <small>{question.field}</small>
              </div>
            </li>
          );
        })}
      </ol>

      <div className="order-draft-clarification-panel__actions">
        <p>
          Yapay zekâ yalnızca mesaj taslağı hazırlar. Gönderim,
          yetkili kullanıcı onayı olmadan yapılamaz.
        </p>

        <Button
          disabled={!preferredSource}
          leadingIcon={<Send />}
          title={
            preferredSource
              ? "Hazırlanan açıklama sorularını kullanıcı onayına sun"
              : "Mesaj gönderilebilecek bağlı bir iletişim kanalı bulunamadı."
          }
          variant="primary"
        >
          Mesaj taslağını incele
        </Button>
      </div>
    </Card>
  );
}