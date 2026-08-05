"use client";

import { useMemo } from "react";
import {
  CheckCircle2,
  CircleAlert,
  Database,
  KeyRound,
  ShieldCheck,
  UserRound,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import { Card } from "@/components/ui/card";

import { createOrderTransferCommand } from "../application/order-transfer-command";
import type { OrderDraft } from "../domain/order-draft";
import {
  getOrderTransferCommandResultDescription,
  getOrderTransferCommandResultLabel,
  getOrderTransferReadinessCheckLabel,
} from "../presentation/order-transfer-command-labels";
import { getOrderTargetSystemLabel } from "../presentation/order-target-system-labels";

type OrderTransferCommandPanelProps = Readonly<{
  orderDraft: OrderDraft;
  currentUserId?: string;
}>;

export function OrderTransferCommandPanel({
  orderDraft,
  currentUserId = "preview-user",
}: OrderTransferCommandPanelProps) {
  const result = useMemo(
    () =>
      createOrderTransferCommand({
        orderDraft,
        requestedByUserId: currentUserId,
        requestedAt: orderDraft.updatedAt,
      }),
    [currentUserId, orderDraft],
  );

  return (
    <Card className="order-transfer-command-panel">
      <div className="order-transfer-command-panel__heading">
        <div>
          <p>Güvenli işlem hazırlığı</p>
          <h2>Aktarım komutu</h2>
        </div>

        <Badge tone={result.success ? "success" : "danger"}>
          {result.success ? (
            <CheckCircle2 aria-hidden="true" />
          ) : (
            <CircleAlert aria-hidden="true" />
          )}

          {getOrderTransferCommandResultLabel(result)}
        </Badge>
      </div>

      <div
        className="order-transfer-command-panel__summary"
        data-success={result.success}
      >
        {result.success ? (
          <ShieldCheck aria-hidden="true" />
        ) : (
          <CircleAlert aria-hidden="true" />
        )}

        <div>
          <strong>
            {getOrderTransferCommandResultLabel(result)}
          </strong>

          <p>
            {getOrderTransferCommandResultDescription(result)}
          </p>
        </div>
      </div>

      {result.success ? (
        <dl className="order-transfer-command-panel__details">
          <div>
            <dt>
              <KeyRound aria-hidden="true" />
              Komut kimliği
            </dt>

            <dd>{result.command.commandId}</dd>
          </div>

          <div>
            <dt>
              <Database aria-hidden="true" />
              Hedef sistem
            </dt>

            <dd>
              {getOrderTargetSystemLabel(
                result.command.targetSystem,
              )}
            </dd>
          </div>

          <div>
            <dt>
              <ShieldCheck aria-hidden="true" />
              Onay kaydı
            </dt>

            <dd>{result.command.approvalId}</dd>
          </div>

          <div>
            <dt>
              <UserRound aria-hidden="true" />
              İşlemi başlatan
            </dt>

            <dd>{result.command.requestedByUserId}</dd>
          </div>
        </dl>
      ) : (
        <div className="order-transfer-command-panel__failures">
          <strong>Tamamlanması gereken kontroller</strong>

          <ul>
            {result.failedChecks.map((checkId) => (
              <li key={checkId}>
                <CircleAlert aria-hidden="true" />

                <span>
                  {getOrderTransferReadinessCheckLabel(
                    checkId,
                  )}
                </span>
              </li>
            ))}
          </ul>
        </div>
      )}

      <div className="order-transfer-command-panel__actions">
        <p>
          Bu ekran yalnızca komutu hazırlar. Gerçek bağlı
          sistem aktarımı ayrı bir sunucu işlemi ve son
          yetki doğrulaması gerektirir.
        </p>

        <Button
          disabled={!result.success}
          leadingIcon={<Database />}
          title={
            result.success
              ? "Aktarım komutunu sunucu işlemine ilet"
              : "Eksik kontroller tamamlanmadan aktarım başlatılamaz."
          }
          variant="primary"
        >
          Aktarımı başlat
        </Button>
      </div>
    </Card>
  );
}