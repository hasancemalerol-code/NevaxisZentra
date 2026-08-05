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
  canTransferOrderDraft,
  hasBlockingOrderIssues,
  type OrderDraft,
} from "../domain/order-draft";
import { getOrderTransferStatusLabel } from "../presentation/order-draft-labels";
import { getOrderTargetSystemLabel } from "../presentation/order-target-system-labels";

type OrderTransferReadinessPanelProps = Readonly<{
  orderDraft: OrderDraft;
}>;

type ReadinessCheck = Readonly<{
  id: string;
  label: string;
  description: string;
  passed: boolean;
}>;

function buildReadinessChecks(
  orderDraft: OrderDraft,
): readonly ReadinessCheck[] {
  const hasTargetSystem =
    Boolean(orderDraft.transfer.targetSystem);

  const hasApprovedOrder =
    orderDraft.approval.status === "approved";

  const hasNoBlockingIssues =
    !hasBlockingOrderIssues(orderDraft);

  const hasAvailableTransferState =
    orderDraft.transfer.status === "not-started" ||
    orderDraft.transfer.status === "pending" ||
    orderDraft.transfer.status === "failed";

  return [
    {
      id: "blocking-issues",
      label: "Engelleyici veri kontrolü",
      description: hasNoBlockingIssues
        ? "Sipariş taslağında aktarımı durduran eksik veya çelişkili bilgi bulunmuyor."
        : "Sipariş taslağındaki engelleyici sorunlar çözülmeden aktarım başlatılamaz.",
      passed: hasNoBlockingIssues,
    },
    {
      id: "authorized-approval",
      label: "Yetkili kullanıcı onayı",
      description: hasApprovedOrder
        ? "Sipariş taslağı yetkili kullanıcı tarafından onaylandı."
        : "Sipariş, yetkili kullanıcı açıkça onaylamadan bağlı sisteme aktarılamaz.",
      passed: hasApprovedOrder,
    },
    {
      id: "target-system",
      label: "Hedef sistem",
      description: hasTargetSystem
        ? `Aktarım hedefi ${getOrderTargetSystemLabel(
            orderDraft.transfer.targetSystem,
          )} olarak belirlendi.`
        : "ERP, MES veya CrevaFactOS aktarım hedefi henüz belirlenmedi.",
      passed: hasTargetSystem,
    },
    {
      id: "transfer-state",
      label: "Aktarım durumu",
      description: hasAvailableTransferState
        ? `Mevcut durum aktarım işlemi için uygun: ${getOrderTransferStatusLabel(
            orderDraft.transfer.status,
          )}.`
        : `Mevcut aktarım durumu yeni işlem başlatmaya uygun değil: ${getOrderTransferStatusLabel(
            orderDraft.transfer.status,
          )}.`,
      passed: hasAvailableTransferState,
    },
  ];
}

export function OrderTransferReadinessPanel({
  orderDraft,
}: OrderTransferReadinessPanelProps) {
  const checks = buildReadinessChecks(orderDraft);

  const passedCheckCount = checks.filter(
    (check) => check.passed,
  ).length;

  const isReady =
    canTransferOrderDraft(orderDraft);

  return (
    <Card className="order-transfer-readiness">
      <div className="order-transfer-readiness__heading">
        <div>
          <p>Human-in-the-Loop kontrolü</p>

          <h2>Aktarım hazırlığı</h2>
        </div>

        <Badge
          tone={isReady ? "success" : "warning"}
        >
          {isReady ? (
            <CheckCircle2 aria-hidden="true" />
          ) : (
            <CircleAlert aria-hidden="true" />
          )}

          {isReady
            ? "Aktarıma hazır"
            : `${passedCheckCount}/${checks.length} kontrol tamam`}
        </Badge>
      </div>

      <div
        className="order-transfer-readiness__summary"
        data-ready={isReady}
      >
        {isReady ? (
          <ShieldCheck aria-hidden="true" />
        ) : (
          <Database aria-hidden="true" />
        )}

        <div>
          <strong>
            {isReady
              ? "Sipariş bağlı sisteme aktarılabilir"
              : "Aktarım koşulları henüz tamamlanmadı"}
          </strong>

          <p>
            {isReady
              ? "Zorunlu kontroller başarıyla tamamlandı. Aktarım yine de yalnızca yetkili kullanıcının açık işlemiyle başlatılmalıdır."
              : "Başarısız kontroller çözülmeden AI veya sistem otomatik aktarım gerçekleştiremez."}
          </p>
        </div>
      </div>

      <ol className="order-transfer-readiness__checks">
        {checks.map((check) => (
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
              <strong>{check.label}</strong>
              <p>{check.description}</p>
            </div>
          </li>
        ))}
      </ol>
    </Card>
  );
}