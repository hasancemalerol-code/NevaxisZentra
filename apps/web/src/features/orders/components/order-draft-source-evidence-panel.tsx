import {
  Database,
  FileText,
  Link2,
  Mail,
  MessageCircle,
  ShieldCheck,
  Upload,
  UserRound,
  type LucideIcon,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";

import type {
  OrderDraft,
  OrderIntakeSource,
} from "../domain/order-draft";
import { formatOrderDateTime } from "../presentation/order-draft-formatters";
import { getOrderIntakeSourceLabel } from "../presentation/order-draft-labels";

type OrderDraftSourceEvidencePanelProps = Readonly<{
  orderDraft: OrderDraft;
}>;

type SourcePresentation = Readonly<{
  icon: LucideIcon;
  description: string;
}>;

const SOURCE_PRESENTATIONS = {
  email: {
    icon: Mail,
    description:
      "Sipariş talebi bağlı e-posta kanalından alındı.",
  },
  whatsapp: {
    icon: MessageCircle,
    description:
      "Sipariş talebi bağlı WhatsApp konuşmasından alındı.",
  },
  erp: {
    icon: Database,
    description:
      "Müşteri, ürün veya ticari bilgiler bağlı ERP sisteminden doğrulandı.",
  },
  "customer-portal": {
    icon: UserRound,
    description:
      "Sipariş bilgileri müşteri portalı üzerinden iletildi.",
  },
  "sales-representative": {
    icon: UserRound,
    description:
      "Sipariş talebi satış temsilcisi tarafından kaydedildi.",
  },
  "web-form": {
    icon: FileText,
    description:
      "Sipariş talebi web formu üzerinden toplandı.",
  },
  "file-upload": {
    icon: Upload,
    description:
      "Sipariş bilgileri yüklenen dosya veya ekten çıkarıldı.",
  },
} satisfies Record<
  OrderIntakeSource,
  SourcePresentation
>;

function getSourcePresentation(
  source: OrderIntakeSource,
): SourcePresentation {
  return SOURCE_PRESENTATIONS[source];
}

export function OrderDraftSourceEvidencePanel({
  orderDraft,
}: OrderDraftSourceEvidencePanelProps) {
  return (
    <Card className="order-draft-source-evidence">
      <div className="order-draft-source-evidence__heading">
        <div>
          <p>Kaynak ve audit</p>
          <h2>Sipariş kanıtları</h2>
        </div>

        <Badge tone="information">
          <Link2 aria-hidden="true" />
          {orderDraft.sources.length} kaynak
        </Badge>
      </div>

      <div className="order-draft-source-evidence__notice">
        <ShieldCheck aria-hidden="true" />

        <p>
          AI tarafından çıkarılan sipariş bilgileri,
          kaynak mesajlar ve bağlı sistem kayıtlarıyla
          birlikte saklanır. Kullanıcı düzeltmeleri ve
          onay geçmişi audit kaydına eklenir.
        </p>
      </div>

      <ol className="order-draft-source-evidence__list">
        {orderDraft.sources.map(
          (source, index) => {
            const presentation =
              getSourcePresentation(source.source);

            const Icon = presentation.icon;

            return (
              <li
                key={`${source.source}-${source.receivedAt}-${index}`}
              >
                <span className="order-draft-source-evidence__icon">
                  <Icon aria-hidden="true" />
                </span>

                <div className="order-draft-source-evidence__content">
                  <div className="order-draft-source-evidence__source-heading">
                    <div>
                      <strong>
                        {getOrderIntakeSourceLabel(
                          source.source,
                        )}
                      </strong>

                      <Badge tone="neutral">
                        Kaynak {index + 1}
                      </Badge>
                    </div>

                    <time dateTime={source.receivedAt}>
                      {formatOrderDateTime(
                        source.receivedAt,
                      )}
                    </time>
                  </div>

                  <p>{presentation.description}</p>

                  <div className="order-draft-source-evidence__audit">
                    <Link2 aria-hidden="true" />

                    <span>
                      Sipariş taslağıyla izlenebilir bağ
                      korunuyor
                    </span>
                  </div>
                </div>
              </li>
            );
          },
        )}
      </ol>
    </Card>
  );
}