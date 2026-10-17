"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";

import { Button } from "@/components/ui/button";
import type { QualityControlPlanSummary } from "../domain/quality-control";
import type { ProductionOrderSummary } from "../../production-execution/domain/production-order";
import type { createQualityInspectionAction } from "../server/quality-control-writer";
import { customerMessage } from "@/lib/presentation/customer-message";
import { enumLabel } from "@/lib/presentation/enum-labels";

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
  const [selectedPlanId, setSelectedPlanId] = useState(plans[0]?.id ?? "");
  const selectedOrder = orders.find((order) => order.id === selectedOrderId);
  const canSubmit = selectedOrderId !== "" && selectedPlanId !== "";

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
          <p>{customerMessage(message)}</p>
        </div>
      ) : null}

      <section className="quality-form-section" aria-label="Üretim emri">
        <h2>Üretim Emri Seçimi</h2>
        <div className="quality-form-field">
          <label htmlFor="productionOrderId">Üretim emri seçin <span className="zx-required" aria-hidden="true">*</span></label>
          <select
            id="productionOrderId"
            name="productionOrderId"
            className="quality-form-select"
            required
            value={selectedOrderId}
            onChange={(event) => setSelectedOrderId(event.target.value)}
          >
            <option value="" disabled>
              Üretim emri seçin
            </option>
            {orders.map((order) => (
              <option key={order.id} value={order.id}>
                {order.referenceCode} · {order.title}
              </option>
            ))}
          </select>
          {orders.length === 0 ? <small className="quality-form-hint">Kayıtlı üretim emri yok.</small> : null}
        </div>
      </section>

      <section className="quality-form-section" aria-label="Üretim bağlamı">
        <h2>Üretim Bağlamı</h2>
        <dl className="quality-context-strip">
          <div>
            <dt>Referans</dt>
            <dd className={selectedOrder?.referenceCode ? undefined : "quality-context-strip__pending"}>{selectedOrder?.referenceCode ?? "Seçim bekleniyor"}</dd>
          </div>
          <div>
            <dt>Başlık</dt>
            <dd className={selectedOrder?.title ? undefined : "quality-context-strip__pending"}>{selectedOrder?.title ?? "Seçim bekleniyor"}</dd>
          </div>
          <div>
            <dt>Durum</dt>
            <dd className={selectedOrder?.status ? undefined : "quality-context-strip__pending"}>{selectedOrder?.status ? enumLabel(selectedOrder.status) : "Seçim bekleniyor"}</dd>
          </div>
        </dl>
      </section>

      <section className="quality-form-section" aria-label="Kontrol planı">
        <h2>Kontrol Planı</h2>
        <div className="quality-form-field">
          <label htmlFor="qualityControlPlanId">Plan seçin <span className="zx-required" aria-hidden="true">*</span></label>
          <select
            id="qualityControlPlanId"
            name="qualityControlPlanId"
            className="quality-form-select"
            required
            value={selectedPlanId}
            onChange={(event) => setSelectedPlanId(event.target.value)}
          >
            <option value="" disabled>
              Plan seçin
            </option>
            {plans.map((plan) => (
              <option key={plan.id} value={plan.id}>
                {plan.code} · {plan.name}
              </option>
            ))}
          </select>
          {plans.length === 0 ? <small className="quality-form-hint">Etkin kontrol planı yok.</small> : null}
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
        <Button type="submit" variant="primary" disabled={pending || !canSubmit}>
          Kontrolü Başlat
        </Button>
        <Button type="button" variant="secondary" onClick={() => router.push("/kalite")}>
          Vazgeç
        </Button>
      </div>
    </form>
  );
}
