import {
  CheckCircle2,
  CircleAlert,
  CircleDashed,
  Database,
  ShieldCheck,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";

import {
  evaluateOrderTransferReadiness,
  type OrderTransferReadinessCheckId,
} from "../application/order-transfer-readiness";
import type { OrderDraft } from "../domain/order-draft";
import { getOrderTransferStatusLabel } from "../presentation/order-draft-labels";
import { getOrderTargetSystemLabel } from "../presentation/order-target-system-labels";

type OrderTransferReadinessPanelProps = Readonly<{
  orderDraft: OrderDraft;
}>;

type ReadinessCheckPresentation = Readonly<{
  label: string;
  description: string;
}>;

function getReadinessCheckPresentation(
  checkId: OrderTransferReadinessCheckId,
  orderDraft: OrderDraft,
): ReadinessCheckPresentation {
  switch (checkId) {
    case "blocking-issues":
      return {
        label: "Engelleyici veri kontrolü",
        description:
          orderDraft.issues.some(
            (issue) => issue.blocking,
          )
            ? "Sipariş taslağındaki engelleyici sorunlar çözülmeden aktarım başlatılamaz."
            : "Sipariş taslağında aktarımı durduran eksik veya çelişkili bilgi bulunmuyor.",
      };

    case "authorized-approval":
      return {
        label: "Yetkili kullanıcı onayı",
        description:
          orderDraft.approval.status === "approved"
            ? "Sipariş taslağı yetkili kullanıcı tarafından onaylandı."
            : "Sipariş, yetkili kullanıcı açıkça onaylamadan bağlı sisteme aktarılamaz.",
      };

    case "target-system":
      return {
        label: "Hedef sistem",
        description: orderDraft.transfer.targetSystem
          ? `Aktarım hedefi ${getOrderTargetSystemLabel(
              orderDraft.transfer.targetSystem,
            )} olarak belirlendi.`
          : "ERP, MES veya Zentra aktarım hedefi henüz belirlenmedi.",
      };

    case "transfer-state":
      return {
        label: "Aktarım durumu",
        description:
          `Mevcut aktarım durumu: ${getOrderTransferStatusLabel(
            orderDraft.transfer.status,
          )}.`,
      };
  }
}

export function OrderTransferReadinessPanel({
  orderDraft,
}: OrderTransferReadinessPanelProps) {
  const readiness =
    evaluateOrderTransferReadiness(orderDraft);

  return (
    <Card className="order-transfer-readiness">
      <div className="order-transfer-readiness__heading">
        <div>
          <p>Human-in-the-Loop kontrolü</p>

          <h2>Aktarım hazırlığı</h2>
        </div>

        <Badge
          tone={
            readiness.isReady
              ? "success"
              : "warning"
          }
        >
          {readiness.isReady ? (
            <CheckCircle2 aria-hidden="true" />
          ) : (
            <CircleAlert aria-hidden="true" />
          )}

          {readiness.isReady
            ? "Aktarıma hazır"
            : `${readiness.passedCheckCount}/${readiness.totalCheckCount} kontrol tamam`}
        </Badge>
      </div>

      <div
        className="order-transfer-readiness__summary"
        data-ready={readiness.isReady}
      >
        {readiness.isReady ? (
          <ShieldCheck aria-hidden="true" />
        ) : (
          <Database aria-hidden="true" />
        )}

        <div>
          <strong>
            {readiness.isReady
              ? "Sipariş bağlı sisteme aktarılabilir"
              : "Aktarım koşulları henüz tamamlanmadı"}
          </strong>

          <p>
            {readiness.isReady
              ? "Zorunlu kontroller başarıyla tamamlandı. Aktarım yine de yalnızca yetkili kullanıcının açık işlemiyle başlatılmalıdır."
              : "Başarısız kontroller çözülmeden AI veya sistem otomatik aktarım gerçekleştiremez."}
          </p>
        </div>
      </div>

      <ol className="order-transfer-readiness__checks">
        {readiness.checks.map((check) => {
          const presentation =
            getReadinessCheckPresentation(
              check.id,
              orderDraft,
            );

          return (
            <li
              data-passed={check.passed}
              key={check.id}
            >
              <span className="order-transfer-readiness__check-icon">
                {check.passed ? (
                  <CheckCircle2 aria-hidden="true" />
                ) : (
                  <CircleDashed aria-hidden="true" />
                )}
              </span>

              <div>
                <strong>
                  {presentation.label}
                </strong>

                <p>
                  {presentation.description}
                </p>
              </div>
            </li>
          );
        })}
      </ol>
    </Card>
  );
}