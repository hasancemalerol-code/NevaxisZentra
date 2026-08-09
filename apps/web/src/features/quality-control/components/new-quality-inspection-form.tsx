"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";

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

  return (
    <form
      className="production-execution-panel"
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
      <header>
        <h2>Üretim emri bağlantısı</h2>
        <p>Üretim emri bilgileri salt okunurdur; kalite kaydı yalnızca referans bağlar.</p>
      </header>

      {message ? (
        <div role="alert" className="state-panel error-state">
          <p>{message}</p>
        </div>
      ) : null}

      <label>
        Üretim emri
        <select name="productionOrderId" required defaultValue={orders[0]?.id ?? ""}>
          {orders.map((order) => (
            <option key={order.id} value={order.id}>
              {order.referenceCode} · {order.title}
            </option>
          ))}
        </select>
      </label>

      <label>
        Kontrol planı
        <select name="qualityControlPlanId" required defaultValue={plans[0]?.id ?? ""}>
          {plans.map((plan) => (
            <option key={plan.id} value={plan.id}>
              {plan.code} · {plan.name}
            </option>
          ))}
        </select>
      </label>

      <label>
        Not
        <textarea name="notes" rows={3} placeholder="Opsiyonel not" />
      </label>

      <button type="submit" className="production-execution-action-button" disabled={pending}>
        Kalite kontrolü oluştur
      </button>
    </form>
  );
}
