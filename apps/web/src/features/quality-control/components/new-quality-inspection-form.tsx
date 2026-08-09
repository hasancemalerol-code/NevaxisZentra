"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";

import { Button } from "@/components/ui/button";
import type { QualityControlPlanSummary } from "../domain/quality-control";
import type { ProductionOrderSummary } from "../../production-execution/domain/production-order";
import type { createQualityInspectionAction } from "../server/quality-control-writer";

export type NewQualityInspectionFormProps = Readonly<{
  orders: readonly ProductionOrderSummary[];
  plans: readonly QualityControlPlanSummary[];
  onCreate: typeof createQualityInspectionAction;
}>;

export function NewQualityInspectionForm({
  orders,
  plans,
  onCreate,
}: NewQualityInspectionFormProps) {
  const router = useRouter();
  const [pending, setPending] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [selectedOrderId, setSelectedOrderId] = useState(orders[0]?.id ?? "");
  const selectedOrder = orders.find((order) => order.id === selectedOrderId);

  return (
    <form
      className="quality-section quality-section--wide quality-form-stack"
      onSubmit={(event) => {
        event.preventDefault();
        const formData = new FormData(event.currentTarget);
        setPending(true);
        setMessage(null);
        void onCreate({
          productionOrderId: String(formData.get("productionOrderId") ?? ""),
          qualityControlPlanId: String(formData.get("qualityControlPlanId") ?? ""),
          notes: String(formData.get("notes") ?? "") || undefined,
        }).then((result) => {
          setPending(false);
          if (result.status === "failure") {
            setMessage(result.message ?? "Kontrol oluşturulamadı.");
            return;
          }
          router.push(`/kalite/kontroller/${result.id}`);
          router.refresh();
        });
      }}
    >
      {message ? (
        <div role="alert" className="quality-error-panel">
          <p>{message}</p>
        </div>
      ) : null}

      <section className="quality-form-section" aria-label="Üretim emri">
        <h3>Üretim Emri</h3>
        <div className="quality-form-field">
          <label htmlFor="productionOrderId">Üretim emri seçin</label>
          <select
            id="productionOrderId"
            name="productionOrderId"
            className="quality-form-select"
            required
            value={selectedOrderId}
            onChange={(event) => setSelectedOrderId(event.target.value)}
          >
            {orders.map((order) => (
              <option key={order.id} value={order.id}>
                {order.referenceCode} · {order.title}
              </option>
            ))}
          </select>
        </div>
      </section>

      <section className="quality-form-section" aria-label="Üretim bağlamı">
        <h3>Üretim Bağlamı</h3>
        <dl className="quality-detail-dl">
          <div>
            <dt>Referans</dt>
            <dd>{selectedOrder?.referenceCode ?? "—"}</dd>
          </div>
          <div>
            <dt>Başlık</dt>
            <dd>{selectedOrder?.title ?? "—"}</dd>
          </div>
          <div>
            <dt>Durum</dt>
            <dd>{selectedOrder?.status ?? "—"}</dd>
          </div>
        </dl>
      </section>

      <section className="quality-form-section" aria-label="Kontrol planı">
        <h3>Kontrol Planı</h3>
        <div className="quality-form-field">
          <label htmlFor="qualityControlPlanId">Plan seçin</label>
          <select
            id="qualityControlPlanId"
            name="qualityControlPlanId"
            className="quality-form-select"
            required
            defaultValue={plans[0]?.id ?? ""}
          >
            {plans.map((plan) => (
              <option key={plan.id} value={plan.id}>
                {plan.code} · {plan.name}
              </option>
            ))}
          </select>
        </div>
      </section>

      <section className="quality-form-section" aria-label="Not">
        <div className="quality-form-field">
          <label htmlFor="notes">Not</label>
          <textarea
            id="notes"
            name="notes"
            className="quality-form-textarea"
            rows={3}
            placeholder="Opsiyonel not"
          />
        </div>
      </section>

      <div className="quality-form-actions">
        <Button type="submit" variant="primary" disabled={pending}>
          Kontrolü Başlat
        </Button>
      </div>
    </form>
  );
}
