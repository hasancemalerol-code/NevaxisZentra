"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";

import { Button } from "@/components/ui/button";
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
      className="quality-section quality-section--wide quality-form-stack"
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
      {message ? (
        <div role="alert" className="quality-error-panel">
          <p>{message}</p>
        </div>
      ) : null}

      <section className="quality-form-section" aria-label="Plan bilgileri">
        <h3>Plan Bilgileri</h3>
        <div className="quality-form-grid">
          <div className="quality-form-field">
            <label htmlFor="code">Kod</label>
            <input id="code" name="code" className="quality-form-input" required defaultValue="QC-PLAN-NEW" />
          </div>
          <div className="quality-form-field">
            <label htmlFor="name">Ad</label>
            <input id="name" name="name" className="quality-form-input" required defaultValue="Yeni kalite planı" />
          </div>
          <div className="quality-form-field">
            <label htmlFor="revision">Revizyon</label>
            <input id="revision" name="revision" className="quality-form-input" defaultValue="1" />
          </div>
          <div className="quality-form-field">
            <label htmlFor="status">Durum</label>
            <select id="status" name="status" className="quality-form-select" defaultValue="active">
              <option value="draft">Taslak</option>
              <option value="active">Aktif</option>
            </select>
          </div>
        </div>
      </section>

      <section className="quality-form-section" aria-label="Ürün operasyon">
        <h3>Ürün / Operasyon</h3>
        <div className="quality-form-grid">
          <div className="quality-form-field">
            <label htmlFor="productReference">Ürün</label>
            <input id="productReference" name="productReference" className="quality-form-input" placeholder="Ürün referansı" />
          </div>
          <div className="quality-form-field">
            <label htmlFor="operationReference">Operasyon</label>
            <input id="operationReference" name="operationReference" className="quality-form-input" placeholder="Operasyon referansı" />
          </div>
        </div>
      </section>

      <section className="quality-form-section" aria-label="Kontrol noktaları">
        <h3>Kontrol Noktaları</h3>
        <div className="quality-table-wrap">
          <table className="quality-table">
            <thead>
              <tr>
                <th>Kod</th>
                <th>Ad</th>
                <th>Tip</th>
                <th>Birim</th>
                <th>Nominal</th>
                <th>Alt Limit</th>
                <th>Üst Limit</th>
                <th>Zorunlu</th>
                <th>Kritik</th>
              </tr>
            </thead>
            <tbody>
              <tr>
                <td>
                  <input name="defCode" className="quality-form-input" defaultValue="MEAS-001" />
                </td>
                <td>
                  <input name="defName" className="quality-form-input" defaultValue="Boyut ölçümü" />
                </td>
                <td>Ölçüm</td>
                <td>
                  <input name="unit" className="quality-form-input" defaultValue="mm" />
                </td>
                <td>
                  <input name="nominal" type="number" step="0.001" className="quality-form-input" defaultValue="10" />
                </td>
                <td>
                  <input name="lower" type="number" step="0.001" className="quality-form-input" defaultValue="9.8" />
                </td>
                <td>
                  <input name="upper" type="number" step="0.001" className="quality-form-input" defaultValue="10.2" />
                </td>
                <td>Evet</td>
                <td>
                  <input name="critical" type="checkbox" aria-label="Kritik" />
                </td>
              </tr>
            </tbody>
          </table>
        </div>
      </section>

      <div className="quality-form-actions">
        <Button type="submit" variant="primary" disabled={pending}>
          Planı Oluştur
        </Button>
      </div>
    </form>
  );
}
