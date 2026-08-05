import {
  CheckCircle2,
  CircleAlert,
  Clock3,
  Database,
  ExternalLink,
  ShieldCheck,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";

import type { OrderDraft } from "../domain/order-draft";
import { formatOrderDateTime } from "../presentation/order-draft-formatters";
import { getOrderTransferStatusLabel } from "../presentation/order-draft-labels";
import {
  getOrderTargetSystemDescription,
  getOrderTargetSystemLabel,
} from "../presentation/order-target-system-labels";

type OrderTransferTargetCardProps = Readonly<{
  orderDraft: OrderDraft;
}>;

type TransferTone =
  | "neutral"
  | "information"
  | "success"
  | "danger";

function getTransferTone(
  status: OrderDraft["transfer"]["status"],
): TransferTone {
  switch (status) {
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

function getTransferIcon(
  status: OrderDraft["transfer"]["status"],
) {
  switch (status) {
    case "completed":
      return CheckCircle2;

    case "failed":
      return CircleAlert;

    case "pending":
    case "in-progress":
      return Clock3;

    case "not-started":
    default:
      return Database;
  }
}

export function OrderTransferTargetCard({
  orderDraft,
}: OrderTransferTargetCardProps) {
  const transfer = orderDraft.transfer;
  const tone = getTransferTone(transfer.status);
  const StatusIcon = getTransferIcon(transfer.status);

  return (
    <Card className="order-transfer-target-card">
      <div className="order-transfer-target-card__heading">
        <div>
          <p>Bağlı sistem aktarımı</p>
          <h2>
            {getOrderTargetSystemLabel(
              transfer.targetSystem,
            )}
          </h2>
        </div>

        <Badge tone={tone}>
          <StatusIcon aria-hidden="true" />
          {getOrderTransferStatusLabel(
            transfer.status,
          )}
        </Badge>
      </div>

      <p className="order-transfer-target-card__description">
        {getOrderTargetSystemDescription(
          transfer.targetSystem,
        )}
      </p>

      <dl className="order-transfer-target-card__details">
        <div>
          <dt>
            <Database aria-hidden="true" />
            Hedef sistem
          </dt>

          <dd>
            {getOrderTargetSystemLabel(
              transfer.targetSystem,
            )}
          </dd>
        </div>

        <div>
          <dt>
            <ExternalLink aria-hidden="true" />
            Harici sipariş numarası
          </dt>

          <dd>
            {transfer.externalOrderId ??
              "Henüz oluşturulmadı"}
          </dd>
        </div>

        <div>
          <dt>
            <Clock3 aria-hidden="true" />
            Aktarım başlangıcı
          </dt>

          <dd>
            {formatOrderDateTime(
              transfer.startedAt,
            )}
          </dd>
        </div>

        <div>
          <dt>
            <CheckCircle2 aria-hidden="true" />
            Tamamlanma zamanı
          </dt>

          <dd>
            {formatOrderDateTime(
              transfer.completedAt,
            )}
          </dd>
        </div>
      </dl>

      {transfer.status === "failed" ? (
        <div className="order-transfer-target-card__error">
          <CircleAlert aria-hidden="true" />

          <div>
            <strong>Aktarım tamamlanamadı</strong>

            <p>
              {transfer.errorMessage ??
                transfer.errorCode ??
                "Bağlı sistem aktarımı sırasında bilinmeyen bir hata oluştu."}
            </p>
          </div>
        </div>
      ) : (
        <div className="order-transfer-target-card__safety">
          <ShieldCheck aria-hidden="true" />

          <p>
            Sipariş, yetkili kullanıcı onayı olmadan
            bağlı sisteme aktarılamaz. Aktarım sonucu
            audit geçmişine kaydedilir.
          </p>
        </div>
      )}
    </Card>
  );
}