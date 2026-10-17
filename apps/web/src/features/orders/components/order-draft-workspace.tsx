import {
  CircleAlert,
  FileCheck2,
  Inbox,
  Layers3,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";

import type { OrderDraft } from "../domain/order-draft";
import { OrderDraftCard } from "./order-draft-card";

type OrderDraftWorkspaceProps = Readonly<{
  orderDrafts: readonly OrderDraft[];
}>;

export function OrderDraftWorkspace({
  orderDrafts,
}: OrderDraftWorkspaceProps) {
  const needsReviewCount = orderDrafts.filter(
    (orderDraft) =>
      orderDraft.status === "needs-review",
  ).length;

  const approvedCount = orderDrafts.filter(
    (orderDraft) =>
      orderDraft.status === "approved" ||
      orderDraft.status === "transferred",
  ).length;

  const blockingIssueCount = orderDrafts.reduce(
    (total, orderDraft) =>
      total +
      orderDraft.issues.filter(
        (issue) => issue.blocking,
      ).length,
    0,
  );

  if (orderDrafts.length === 0) {
    return (
      <Card className="order-draft-workspace__empty">
        <Inbox aria-hidden="true" />

        <div>
          <h2>Sipariş taslağı bulunamadı</h2>

          <p>
            E-posta, WhatsApp, ERP veya diğer bağlı
            kanallardan sipariş verisi geldiğinde yapay zekâ
            tarafından hazırlanan taslaklar burada
            listelenecek.
          </p>
        </div>
      </Card>
    );
  }

  return (
    <div className="order-draft-workspace">
      <div className="order-draft-workspace__summary">
        <Badge tone="neutral">
          <Layers3 aria-hidden="true" />
          {orderDrafts.length} taslak
        </Badge>

        <Badge tone="information">
          <FileCheck2 aria-hidden="true" />
          {needsReviewCount} inceleme bekliyor
        </Badge>

        <Badge tone="success">
          <FileCheck2 aria-hidden="true" />
          {approvedCount} onaylandı
        </Badge>

        {blockingIssueCount > 0 ? (
          <Badge tone="danger">
            <CircleAlert aria-hidden="true" />
            {blockingIssueCount} engelleyici sorun
          </Badge>
        ) : null}
      </div>

      <div className="order-draft-workspace__list">
        {orderDrafts.map((orderDraft) => (
          <OrderDraftCard
            key={orderDraft.id}
            orderDraft={orderDraft}
          />
        ))}
      </div>
    </div>
  );
}