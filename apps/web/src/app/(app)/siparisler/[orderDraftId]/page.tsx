import { OrderTransferReadinessPanel } from "@/features/orders/components/order-transfer-readiness-panel";
import { OrderTransferTargetCard } from "@/features/orders/components/order-transfer-target-card";
import {
  getOrderTargetSystemDescription,
  getOrderTargetSystemLabel,
} from "@/features/orders/presentation/order-target-system-labels";
import { OrderDraftSourceEvidencePanel } from "@/features/orders/components/order-draft-source-evidence-panel";
import { OrderDraftClarificationMessage } from "@/features/orders/components/order-draft-clarification-message";
import { OrderDraftClarificationPanel } from "@/features/orders/components/order-draft-clarification-panel";
import { OrderDraftTimeline } from "@/features/orders/components/order-draft-timeline";
import {
  formatOrderConfidence,
  formatOrderDate,
  formatOrderDateTime,
  formatOrderMoney,
  formatOrderQuantity,
} from "@/features/orders/presentation/order-draft-formatters";
import {
  getOrderApprovalStatusLabel,
  getOrderFieldIssueLabel,
  getOrderIntakeSourceLabel,
  getOrderTransferStatusLabel,
} from "@/features/orders/presentation/order-draft-labels";
import {
  getOrderApprovalRoleDescription,
  getOrderApprovalRoleLabel,
} from "@/features/orders/presentation/order-approval-role-labels";
import Link from "next/link";
import { notFound } from "next/navigation";
import {
  ArrowLeft,
  CheckCircle2,
  CircleAlert,
  Database,
  FileText,
  Mail,
  MessageCircle,
  PackageSearch,
  ShieldCheck,
  UserRound,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import {
  PageContent,
  PageHeader,
  Section,
} from "@/components/ui/page";
import { OrderDraftActionPanel } from "@/features/orders/components/order-draft-action-panel";
import { findOrderDraftById } from "@/features/orders/data/order-draft-preview-data";
import type {
  OrderIntakeSource,
  OrderMatchConfidence,
} from "@/features/orders/domain/order-draft";

type OrderDraftDetailPageProps = Readonly<{
  params: Promise<{
    orderDraftId: string;
  }>;
}>;

type BadgeTone =
  | "neutral"
  | "information"
  | "success"
  | "danger"
  | "ai";

function getConfidenceTone(
  confidence: OrderMatchConfidence,
): BadgeTone {
  if (confidence === "high") {
    return "success";
  }

  if (confidence === "medium") {
    return "information";
  }

  return "danger";
}

function getSourceIcon(
  source: OrderIntakeSource,
) {
  if (source === "email") {
    return Mail;
  }

  if (source === "whatsapp") {
    return MessageCircle;
  }

  if (source === "erp") {
    return Database;
  }

  return FileText;
}

export default async function OrderDraftDetailPage({
  params,
}: OrderDraftDetailPageProps) {
  const { orderDraftId } = await params;
  const orderDraft = findOrderDraftById(orderDraftId);

  if (!orderDraft) {
    notFound();
  }

  const blockingIssueCount =
    orderDraft.issues.filter(
      (issue) => issue.blocking,
    ).length;

  return (
    <PageContent>
      <Link
        className="order-draft-detail__back-link"
        href="/siparisler"
      >
        <ArrowLeft aria-hidden="true" />
        Sipariş taslaklarına dön
      </Link>

      <PageHeader
        actions={
          <Badge
            tone={getConfidenceTone(
              orderDraft.confidenceLevel,
            )}
          >
            AI güveni
            {" "}
            {formatOrderConfidence(
              orderDraft.overallConfidence,
            )}
          </Badge>
        }
        description="Kaynak mesajları, AI eşleşmelerini, açık sorunları ve ERP aktarım hazırlığını inceleyin."
        eyebrow="AI Sipariş Taslağı"
        preview
        title={orderDraft.title}
      />

      <Section
        action={
          <Badge
            tone={
              blockingIssueCount > 0
                ? "danger"
                : "success"
            }
          >
            {blockingIssueCount > 0 ? (
              <CircleAlert aria-hidden="true" />
            ) : (
              <CheckCircle2 aria-hidden="true" />
            )}

            {blockingIssueCount > 0
              ? `${blockingIssueCount} engelleyici sorun`
              : "Engelleyici sorun yok"}
          </Badge>
        }
        description="AI tarafından farklı kanallardan toplanan mesajlar ve dosyalar."
        title="Kaynak kayıtlar"
      >
        <div className="order-draft-detail__source-list">
          {orderDraft.sources.map((source) => {
            const Icon = getSourceIcon(source.source);

            return (
              <Card
                className="order-draft-detail__source-card"
                key={source.id}
              >
                <div className="order-draft-detail__source-icon">
                  <Icon aria-hidden="true" />
                </div>

                <div>
                  <div className="order-draft-detail__source-heading">
                    <h2>
                      {getOrderIntakeSourceLabel(source.source)}
                    </h2>

                    <Badge tone="neutral">
                      {formatOrderDateTime(source.receivedAt)}
                    </Badge>
                  </div>

                  <strong>
                    {source.subject ??
                      source.senderName ??
                      "Kaynak kayıt"}
                  </strong>

                  <p>
                    {source.messagePreview ??
                      "Mesaj önizlemesi bulunmuyor."}
                  </p>

                  {source.attachmentNames?.length ? (
                    <ul>
                      {source.attachmentNames.map(
                        (attachmentName) => (
                          <li key={attachmentName}>
                            <FileText aria-hidden="true" />
                            {attachmentName}
                          </li>
                        ),
                      )}
                    </ul>
                  ) : null}
                </div>
              </Card>
            );
          })}
        </div>
      </Section>

      <Section
        action={
          <Badge
            tone={getConfidenceTone(
              orderDraft.customer.confidenceLevel,
            )}
          >
            {formatOrderConfidence(
              orderDraft.customer.confidence,
            )}
            {" "}
            güven
          </Badge>
        }
        description="ERP ve kaynak mesajlardan eşleştirilen müşteri bilgileri."
        title="Müşteri eşleşmesi"
      >
        <Card className="order-draft-detail__customer-card">
          <UserRound aria-hidden="true" />

          <dl>
            <div>
              <dt>Müşteri</dt>
              <dd>
                {orderDraft.customer.customerName ??
                  "Eşleşme bekleniyor"}
              </dd>
            </div>

            <div>
              <dt>Müşteri kodu</dt>
              <dd>
                {orderDraft.customer.customerCode ??
                  "Tanımlanmadı"}
              </dd>
            </div>

            <div>
              <dt>E-posta</dt>
              <dd>
                {orderDraft.customer.email ??
                  "Tanımlanmadı"}
              </dd>
            </div>

            <div>
              <dt>Telefon</dt>
              <dd>
                {orderDraft.customer.phone ??
                  "Tanımlanmadı"}
              </dd>
            </div>
          </dl>
        </Card>
      </Section>

      <Section
        action={
          <Badge tone="information">
            <PackageSearch aria-hidden="true" />
            {orderDraft.lines.length} satır
          </Badge>
        }
        description="AI tarafından eşleştirilen ürün, miktar, fiyat ve termin bilgileri."
        title="Sipariş satırları"
      >
        <div className="order-draft-detail__line-list">
          {orderDraft.lines.map((line) => (
            <Card
              className="order-draft-detail__line-card"
              key={line.lineId}
            >
              <div className="order-draft-detail__line-heading">
                <div>
                  <p>{line.productCode ?? "Ürün kodu yok"}</p>
                  <h2>
                    {line.productName ??
                      line.requestedDescription}
                  </h2>
                </div>

                <Badge
                  tone={getConfidenceTone(
                    line.confidenceLevel,
                  )}
                >
                  {formatOrderConfidence(line.confidence)}
                  {" "}
                  güven
                </Badge>
              </div>

              <dl>
                <div>
                  <dt>Miktar</dt>
                  <dd>
                    {formatOrderQuantity(
                      line.quantity,
                      line.unit,
                    )}
                  </dd>
                </div>

                <div>
                  <dt>Birim fiyat</dt>
                  <dd>
                    {formatOrderMoney(
                      line.unitPrice,
                      line.currency,
                    )}
                  </dd>
                </div>

                <div>
                  <dt>Termin</dt>
                  <dd>
                    {formatOrderDate(
                      line.requestedDeliveryDate,
                    )}
                  </dd>
                </div>

                <div>
                  <dt>Varyant</dt>
                  <dd>
                    {line.variant ??
                      "Tanımlanmadı"}
                  </dd>
                </div>
              </dl>
            </Card>
          ))}
        </div>
      </Section>

      <Section
        action={
          <Badge
            tone={
              orderDraft.issues.length > 0
                ? "danger"
                : "success"
            }
          >
            <CircleAlert aria-hidden="true" />
            {orderDraft.issues.length} açık sorun
          </Badge>
        }
        description="Eksik, çelişkili veya düşük güvenli alanlar onay öncesinde çözülmelidir."
        title="AI doğrulama sonuçları"
      >
        {orderDraft.issues.length > 0 ? (
          <div className="order-draft-detail__issue-list">
            {orderDraft.issues.map((issue) => (
              <Card
                className="order-draft-detail__issue-card"
                key={issue.id}
              >
                <CircleAlert aria-hidden="true" />

                <div>
                  <div>
                    <strong>
                      {getOrderFieldIssueLabel(issue.type)}
                    </strong>

                    {issue.blocking ? (
                      <Badge tone="danger">
                        Engelleyici
                      </Badge>
                    ) : null}
                  </div>

                  <p>{issue.message}</p>
                  <small>{issue.field}</small>
                </div>
              </Card>
            ))}
          </div>
        ) : (
          <Card className="order-draft-detail__empty-card">
            <CheckCircle2 aria-hidden="true" />
            Açık doğrulama sorunu bulunmuyor.
          </Card>
        )}
      </Section>

      <Section
        action={
          <Badge tone="ai">
            <ShieldCheck aria-hidden="true" />
            Human-in-the-Loop
          </Badge>
        }
        description="Sipariş yalnızca yetkili onayından sonra bağlı ERP sistemine aktarılabilir."
        title="Onay ve ERP aktarımı"
      >
        <Card className="order-draft-detail__approval-card">
          <dl>
            <div>
              <dt>Onay durumu</dt>
              <dd>
                {getOrderApprovalStatusLabel(
                  orderDraft.approval.status,
                )}
              </dd>
            </div>

            <div>
              <dt>Gerekli rol</dt>
              <dd
                title={getOrderApprovalRoleDescription(
                  orderDraft.approval.requiredRole,
                )}
              >
                {getOrderApprovalRoleLabel(
                  orderDraft.approval.requiredRole,
                )}
              </dd>
            </div>

            <div>
              <dt>Onaylayan</dt>
              <dd>
                {orderDraft.approval.approvedBy ??
                  "Henüz onaylanmadı"}
              </dd>
            </div>

            <div>
              <dt>Aktarım durumu</dt>
              <dd>
                {getOrderTransferStatusLabel(
                  orderDraft.transfer.status,
                )}
              </dd>
            </div>

            <div>
              <dt>Hedef sistem</dt>
              <dd>
                {getOrderTargetSystemLabel(orderDraft.transfer.targetSystem)}
              </dd>
            </div>

            <div>
              <dt>Dış sipariş numarası</dt>
              <dd>
                {orderDraft.transfer.externalOrderId ??
                  "Henüz oluşturulmadı"}
              </dd>
            </div>
          </dl>
        </Card>
      </Section>
    
      <OrderDraftActionPanel
        orderDraft={orderDraft}
      />
      <OrderDraftSourceEvidencePanel
        orderDraft={orderDraft}
      />
      <OrderDraftClarificationPanel
        orderDraft={orderDraft}
      />
      <OrderDraftClarificationMessage
        orderDraft={orderDraft}
      />
      <OrderTransferReadinessPanel
        orderDraft={orderDraft}
      />
      <OrderTransferTargetCard
        orderDraft={orderDraft}
      />
      <OrderDraftTimeline
        orderDraft={orderDraft}
      /></PageContent>
  );
}