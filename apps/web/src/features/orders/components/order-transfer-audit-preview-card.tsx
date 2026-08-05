import {
  FileClock,
  ShieldCheck,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";

import type { OrderTransferAuditRecord } from "../application/order-transfer-audit-record";
import { formatOrderDateTime } from "../presentation/order-draft-formatters";
import {
  getOrderTransferAuditFollowUpLabel,
  getOrderTransferAuditOutcomeDescription,
  getOrderTransferAuditOutcomeLabel,
  getOrderTransferAuditOutcomeTone,
} from "../presentation/order-transfer-audit-labels";
import { getOrderTargetSystemLabel } from "../presentation/order-target-system-labels";

type OrderTransferAuditPreviewCardProps =
  Readonly<{
    auditRecord: OrderTransferAuditRecord;
  }>;

export function OrderTransferAuditPreviewCard({
  auditRecord,
}: OrderTransferAuditPreviewCardProps) {
  return (
    <section className="order-transfer-execution-preview__audit">
      <div>
        <FileClock aria-hidden="true" />

        <div>
          <span>Üretilen audit kaydı</span>

          <strong>{auditRecord.id}</strong>
        </div>

        <Badge
          tone={getOrderTransferAuditOutcomeTone(
            auditRecord.outcome,
          )}
        >
          {getOrderTransferAuditFollowUpLabel(
            auditRecord,
          )}
        </Badge>
      </div>

      <p className="order-transfer-execution-preview__audit-description">
        {getOrderTransferAuditOutcomeDescription(
          auditRecord.outcome,
        )}
      </p>

      <dl>
        <div>
          <dt>Komut kimliği</dt>
          <dd>{auditRecord.commandId}</dd>
        </div>

        <div>
          <dt>Onay referansı</dt>
          <dd>{auditRecord.approvalId}</dd>
        </div>

        <div>
          <dt>Sonuç</dt>
          <dd>
            {getOrderTransferAuditOutcomeLabel(
              auditRecord.outcome,
            )}
          </dd>
        </div>

        <div>
          <dt>Hedef sistem</dt>
          <dd>
            {getOrderTargetSystemLabel(
              auditRecord.targetSystem,
            )}
          </dd>
        </div>

        <div>
          <dt>İşlemi başlatan</dt>
          <dd>
            {auditRecord.requestedByUserId}
          </dd>
        </div>

        <div>
          <dt>Başlangıç</dt>
          <dd>
            {formatOrderDateTime(
              auditRecord.startedAt,
            )}
          </dd>
        </div>

        <div>
          <dt>Tamamlanma</dt>
          <dd>
            {formatOrderDateTime(
              auditRecord.finishedAt,
            )}
          </dd>
        </div>

        <div>
          <dt>Kayıt zamanı</dt>
          <dd>
            {formatOrderDateTime(
              auditRecord.recordedAt,
            )}
          </dd>
        </div>
      </dl>

      {auditRecord.externalOrderId ? (
        <div className="order-transfer-execution-preview__audit-reference">
          <ShieldCheck aria-hidden="true" />

          <span>Harici sipariş numarası</span>

          <strong>
            {auditRecord.externalOrderId}
          </strong>
        </div>
      ) : null}
    </section>
  );
}