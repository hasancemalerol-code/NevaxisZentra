import { DetailList } from "@/components/ui/detail-list";
import { EmptyPanel } from "@/components/ui/empty-panel";
import { Panel } from "@/components/ui/panel";
import { formatDate, formatDateTime } from "@/lib/presentation/format";

import {
  shiftHandoverCategoryLabel,
  shiftHandoverDepartmentLabel,
  shiftHandoverEventLabel,
  shiftHandoverItemStatusLabel,
  shiftHandoverPeriodLabel,
  shiftHandoverSeverityLabel,
  shiftHandoverStatusLabel,
} from "../domain/shift-handover-constants";
import type { RawShiftHandoverDetail } from "../server/shift-handover-api-client";
import { ShiftHandoverFollowUpTasks, ShiftHandoverLifecycleActions } from "./shift-handover-actions";
import { ShiftHandoverAddItemForm } from "./shift-handover-add-item-form";

export function ShiftHandoverDetailView({ detail }: Readonly<{ detail: RawShiftHandoverDetail }>) {
  const openItems = detail.items.filter((item) => item.status !== "resolved");
  const canAddItems = detail.capabilities.canManage && detail.status === "draft";

  return (
    <>
      <Panel
        title="Devir özeti"
        subtitle={canAddItems ? "Kalemleri ekledikten sonra devre hazırlayın; kayıt devir için hazır olarak işaretlenir." : undefined}
        actions={<ShiftHandoverLifecycleActions detail={detail} />}
      >
        <DetailList
          data-testid="shift-handover-detail-facts"
          items={[
            { label: "Durum", value: <span className="shift-handover-status" data-status={detail.status}>{shiftHandoverStatusLabel(detail.status)}</span> },
            { label: "Vardiya tarihi", value: formatDate(detail.shiftDate) },
            { label: "Vardiya periyodu", value: shiftHandoverPeriodLabel(detail.shiftPeriod) },
            { label: "Bölüm", value: shiftHandoverDepartmentLabel(detail.department) },
            { label: "Tesis", value: detail.facilityName },
            { label: "Sorumlu (devreden)", value: detail.outgoingLabel },
            { label: "Teslim alan", value: detail.incomingLabel ?? "Henüz atanmadı" },
            { label: "Son güncelleme", value: formatDateTime(detail.updatedAt) },
          ]}
        />
        <div className="shift-handover-summary-block">
          <span className="shift-handover-summary-label">Devir notu</span>
          <p className="shift-handover-summary">
            {detail.summary.trim() ? detail.summary : "Bu devir için özet girilmemiş."}
          </p>
        </div>
      </Panel>

      <Panel title="Açık kalemler" subtitle={openItems.length > 0 ? `${openItems.length} açık kalem` : undefined}>
        {detail.items.length === 0 ? (
          <EmptyPanel
            compact
            title="Henüz kalem eklenmedi"
            description="Sonraki vardiyanın bilmesi gereken konuları kalem olarak ekleyin."
            action={canAddItems ? (
            <details className="shift-handover-disclosure">
              <summary>Kalem ekle</summary>
              <ShiftHandoverAddItemForm handoverId={detail.id} />
            </details>
          ) : undefined}
          />
        ) : (
          <ul className="shift-handover-items">
            {detail.items.map((item) => (
              <li className="shift-handover-item" key={item.id}>
                <div className="shift-handover-item-header">
                  <strong>{item.title}</strong>
                  <span className="shift-handover-meta">
                    {shiftHandoverCategoryLabel(item.category)} · {shiftHandoverSeverityLabel(item.severity)} · {shiftHandoverItemStatusLabel(item.status)}
                  </span>
                </div>
                {item.description ? <p className="shift-handover-muted">{item.description}</p> : null}
              </li>
            ))}
          </ul>
        )}
        {canAddItems && detail.items.length > 0 ? (
          <details className="shift-handover-disclosure">
            <summary>Kalem ekle</summary>
            <ShiftHandoverAddItemForm handoverId={detail.id} />
          </details>
        ) : null}
      </Panel>

      <Panel title="Takip işleri" subtitle="Takip gerektiren kalemler için görev oluşturun.">
        <ShiftHandoverFollowUpTasks detail={detail} />
      </Panel>

      <Panel title="Geçmiş" subtitle="Kaydın yaşam döngüsü.">
        {detail.events.length === 0 ? (
          <EmptyPanel compact title="Henüz hareket yok" />
        ) : (
          <ol className="shift-handover-timeline">
            {detail.events.map((event) => (
              <li key={event.id}>
                <strong>{shiftHandoverEventLabel(event.eventType)}</strong>
                <span className="shift-handover-muted"> · {event.actorLabel} · {formatDateTime(event.occurredAt)}</span>
              </li>
            ))}
          </ol>
        )}
      </Panel>
    </>
  );
}
