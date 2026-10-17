"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";

import { Button } from "@/components/ui/button";
import type { createQualityControlPlanAction } from "../server/quality-control-writer";
import { customerMessage } from "@/lib/presentation/customer-message";

export type QualityPlanFormProps = Readonly<{
  onCreate: typeof createQualityControlPlanAction;
}>;

export function QualityPlanForm({ onCreate }: QualityPlanFormProps) {
  const router = useRouter();
  const [pending, setPending] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [rowIds, setRowIds] = useState<readonly number[]>([1]);
  const [nextRowId, setNextRowId] = useState(2);

  function addRow() {
    setRowIds((current) => [...current, nextRowId]);
    setNextRowId((current) => current + 1);
  }

  function removeRow(id: number) {
    setRowIds((current) => (current.length > 1 ? current.filter((rowId) => rowId !== id) : current));
  }

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
          inspectionDefinitions: rowIds.map((rowId, index) => ({
            code: String(formData.getAll("defCode")[index] ?? "MEAS-001"),
            name: String(formData.getAll("defName")[index] ?? "Ölçüm"),
            inspectionType: "measurement",
            sequence: index + 1,
            unit: String(formData.getAll("unit")[index] ?? "mm") || undefined,
            nominalValue: Number(formData.getAll("nominal")[index] ?? 0),
            lowerLimit: Number(formData.getAll("lower")[index] ?? 0),
            upperLimit: Number(formData.getAll("upper")[index] ?? 0),
            required: true,
            critical: formData.get(`critical-${rowId}`) === "on",
          })),
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
          <p>{customerMessage(message)}</p>
        </div>
      ) : null}

      <section className="quality-form-section" aria-label="Plan bilgileri">
        <h2>Plan Bilgileri</h2>
        <div className="quality-form-grid">
          <div className="quality-form-field">
            <label htmlFor="code">Kod <span className="zx-required" aria-hidden="true">*</span></label>
            <input id="code" name="code" className="quality-form-input" required placeholder="Örn. QC-PLAN-001" />
          </div>
          <div className="quality-form-field">
            <label htmlFor="name">Ad <span className="zx-required" aria-hidden="true">*</span></label>
            <input id="name" name="name" className="quality-form-input" required placeholder="Plan adı" />
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
        <h2>Ürün / Operasyon</h2>
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
        <h2>Kontrol Noktaları</h2>
        <div className="quality-table-wrap" tabIndex={0} role="region" aria-label="Kontrol noktaları (kaydırılabilir)">
          <table aria-label="Kontrol Noktaları" className="quality-table quality-table--form">
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
                <th>
                  <span className="quality-visually-hidden">İşlem</span>
                </th>
              </tr>
            </thead>
            <tbody>
              {rowIds.map((rowId) => (
                <tr key={rowId}>
                  <td>
                    <input name="defCode" aria-label="Kontrol noktası kodu" className="quality-form-input" required placeholder="Örn. MEAS-001" />
                  </td>
                  <td>
                    <input name="defName" aria-label="Kontrol noktası adı" className="quality-form-input" required placeholder="Kontrol noktası adı" />
                  </td>
                  <td><span className="quality-static-chip">Ölçüm</span></td>
                  <td>
                    <input name="unit" aria-label="Birim" className="quality-form-input" placeholder="mm" />
                  </td>
                  <td>
                    <input name="nominal" aria-label="Nominal değer" type="number" step="0.001" className="quality-form-input" placeholder="0" />
                  </td>
                  <td>
                    <input name="lower" aria-label="Alt limit" type="number" step="0.001" className="quality-form-input" placeholder="0" />
                  </td>
                  <td>
                    <input name="upper" aria-label="Üst limit" type="number" step="0.001" className="quality-form-input" placeholder="0" />
                  </td>
                  <td><span className="quality-static-chip quality-static-chip--on">Evet</span></td>
                  <td>
                    <label className="quality-inline-check"><input name={`critical-${rowId}`} type="checkbox" />
                      <span>Kritik</span></label>
                  </td>
                  <td>
                    {rowIds.length > 1 ? (
                      <Button type="button" variant="secondary" size="small" onClick={() => removeRow(rowId)}>
                        Kaldır
                      </Button>
                    ) : null}
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
        <div className="quality-form-actions">
          <Button type="button" variant="secondary" size="small" onClick={addRow}>
            Kontrol noktası ekle
          </Button>
        </div>
      </section>

      <div className="quality-form-actions">
        <Button type="submit" variant="primary" disabled={pending}>
          Planı Oluştur
        </Button>
        <Button type="button" variant="secondary" onClick={() => router.push("/kalite")}>
          Vazgeç
        </Button>
      </div>
    </form>
  );
}
