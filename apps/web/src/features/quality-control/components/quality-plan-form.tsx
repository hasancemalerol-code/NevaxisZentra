"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";

import type { createQualityControlPlanAction } from "../server/quality-control-writer";

export type QualityPlanFormProps = Readonly<{
  onCreate: typeof createQualityControlPlanAction;
}>;

export function QualityPlanForm({ onCreate }: QualityPlanFormProps) {
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
          code: String(formData.get("code") ?? ""),
          name: String(formData.get("name") ?? ""),
          productReference: String(formData.get("productReference") ?? "") || undefined,
          operationReference: String(formData.get("operationReference") ?? "") || undefined,
          status: String(formData.get("status") ?? "active"),
          revision: String(formData.get("revision") ?? "1"),
          inspectionDefinitions: [
            {
              code: String(formData.get("defCode") ?? "MEAS-001"),
              name: String(formData.get("defName") ?? "Ölçüm"),
              inspectionType: "measurement",
              sequence: 1,
              unit: String(formData.get("unit") ?? "mm") || undefined,
              nominalValue: Number(formData.get("nominal") ?? 0),
              lowerLimit: Number(formData.get("lower") ?? 0),
              upperLimit: Number(formData.get("upper") ?? 0),
              required: true,
              critical: formData.get("critical") === "on",
            },
          ],
        }).then((result) => {
          setPending(false);
          if (result.status === "failure") {
            setMessage(result.message ?? "Plan oluşturulamadı.");
            return;
          }
          router.push("/kalite");
          router.refresh();
        });
      }}
    >
      <header>
        <h2>Yeni Kontrol Planı</h2>
      </header>

      {message ? (
        <div role="alert" className="state-panel error-state">
          <p>{message}</p>
        </div>
      ) : null}

      <label>
        Kod
        <input name="code" required defaultValue="QC-PLAN-NEW" />
      </label>
      <label>
        Ad
        <input name="name" required defaultValue="Yeni kalite planı" />
      </label>
      <label>
        Ürün
        <input name="productReference" placeholder="Ürün referansı" />
      </label>
      <label>
        Operasyon
        <input name="operationReference" placeholder="Operasyon referansı" />
      </label>
      <label>
        Revizyon
        <input name="revision" defaultValue="1" />
      </label>
      <label>
        Durum
        <select name="status" defaultValue="active">
          <option value="draft">Taslak</option>
          <option value="active">Aktif</option>
        </select>
      </label>

      <fieldset>
        <legend>İlk ölçüm tanımı</legend>
        <label>
          Kod
          <input name="defCode" defaultValue="MEAS-001" />
        </label>
        <label>
          Ad
          <input name="defName" defaultValue="Boyut ölçümü" />
        </label>
        <label>
          Birim
          <input name="unit" defaultValue="mm" />
        </label>
        <label>
          Nominal
          <input name="nominal" type="number" step="0.001" defaultValue="10" />
        </label>
        <label>
          Alt limit
          <input name="lower" type="number" step="0.001" defaultValue="9.8" />
        </label>
        <label>
          Üst limit
          <input name="upper" type="number" step="0.001" defaultValue="10.2" />
        </label>
        <label>
          <input name="critical" type="checkbox" /> Kritik
        </label>
      </fieldset>

      <button type="submit" className="production-execution-action-button" disabled={pending}>
        Planı oluştur
      </button>
    </form>
  );
}
