"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";

import { Button } from "@/components/ui/button";
import {
  MAINTENANCE_ASSET_TYPE_OPTIONS,
  MAINTENANCE_INTERVAL_TYPE_OPTIONS,
} from "../presentation/maintenance-form-options";
import type { createPreventiveMaintenancePlanAction } from "../server/maintenance-writer";
import { MaintenanceAssetReferenceField } from "./maintenance-asset-reference-field";
import type { MaintenanceAssetOption } from "../domain/maintenance-asset-option";
import { customerMessage } from "@/lib/presentation/customer-message";

type MaintenancePlanCreateFormProps = Readonly<{
  assetOptions?: readonly MaintenanceAssetOption[];
  onCreate: typeof createPreventiveMaintenancePlanAction;
}>;

export function MaintenancePlanCreateForm({
  onCreate,
  assetOptions = [],
}: MaintenancePlanCreateFormProps) {
  const router = useRouter();
  const [pending, setPending] = useState(false);
  const [message, setMessage] = useState<string | null>(null);
  const [dueDate, setDueDate] = useState("");
  const [dueTime, setDueTime] = useState("");
  // Same wire value as the former datetime-local control: YYYY-MM-DDTHH:mm (empty when no date is chosen).
  const nextDueValue = dueDate ? `${dueDate}T${dueTime || "00:00"}` : "";

  return (
    <form
      className="maintenance-form-stack"
      method="post"
      onSubmit={(event) => {
        event.preventDefault();
        const formData = new FormData(event.currentTarget);
        const intervalValue = Number(formData.get("intervalValue"));
        setPending(true);
        setMessage(null);
        void onCreate({
          code: String(formData.get("code") ?? ""),
          name: String(formData.get("name") ?? ""),
          assetType: String(formData.get("assetType") ?? ""),
          assetReference: String(formData.get("assetReference") ?? ""),
          intervalType: String(formData.get("intervalType") ?? ""),
          intervalValue,
          nextDueAtUtc: String(formData.get("nextDueAtUtc") ?? "") || undefined,
          description: String(formData.get("description") ?? "") || undefined,
        }).then((result) => {
          if (result.status === "failure") {
            setPending(false);
            setMessage(result.message ?? "Periyodik bakım planı oluşturulamadı.");
            return;
          }

          router.push("/bakim/planlama");
          router.refresh();
        });
      }}
    >
      {message ? (
        <div className="maintenance-error-panel" role="alert">
          <p>{customerMessage(message)}</p>
        </div>
      ) : null}

      <div className="maintenance-form-grid">
        <div className="maintenance-form-field">
          <label htmlFor="code">Plan kodu <span className="zx-required" aria-hidden="true">*</span></label>
          <input
            className="maintenance-form-input"
            disabled={pending}
            id="code"
            name="code"
            required
          />
        </div>
        <div className="maintenance-form-field">
          <label htmlFor="name">Plan adı <span className="zx-required" aria-hidden="true">*</span></label>
          <input
            className="maintenance-form-input"
            disabled={pending}
            id="name"
            name="name"
            required
          />
        </div>
        <MaintenanceAssetReferenceField disabled={pending} options={assetOptions} />
        <div className="maintenance-form-field">
          <label htmlFor="assetType">Varlık türü</label>
          <select
            className="maintenance-form-select"
            defaultValue="machine"
            disabled={pending}
            id="assetType"
            name="assetType"
          >
            {MAINTENANCE_ASSET_TYPE_OPTIONS.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </div>
        <div className="maintenance-form-field">
          <label htmlFor="intervalType">Periyot türü</label>
          <select
            className="maintenance-form-select"
            defaultValue="days"
            disabled={pending}
            id="intervalType"
            name="intervalType"
          >
            {MAINTENANCE_INTERVAL_TYPE_OPTIONS.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </div>
        <div className="maintenance-form-field">
          <label htmlFor="intervalValue">Periyot değeri <span className="zx-required" aria-hidden="true">*</span></label>
          <input
            className="maintenance-form-input"
            defaultValue={30}
            disabled={pending}
            id="intervalValue"
            min={1}
            name="intervalValue"
            required
            step={1}
            type="number"
          />
          <small className="maintenance-form-hint">Birim, seçilen periyot türüdür (varsayılan: gün).</small>
        </div>
        <div className="maintenance-form-field">
          <label htmlFor="nextDueDate">Sonraki bakım</label>
          <div className="maintenance-form-datetime">
            <input
              aria-label="Sonraki bakım tarihi"
              className="maintenance-form-input"
              disabled={pending}
              id="nextDueDate"
              onChange={(event) => setDueDate(event.target.value)}
              type="date"
              value={dueDate}
            />
            <input
              aria-label="Sonraki bakım saati"
              className="maintenance-form-input"
              disabled={pending || !dueDate}
              onChange={(event) => setDueTime(event.target.value)}
              type="time"
              value={dueTime}
            />
          </div>
          <input name="nextDueAtUtc" type="hidden" value={nextDueValue} />
        </div>
        <div className="maintenance-form-field maintenance-form-field--wide">
          <label htmlFor="description">Açıklama</label>
          <textarea
            className="maintenance-form-textarea"
            disabled={pending}
            id="description"
            name="description"
          />
        </div>
      </div>


      <div className="maintenance-form-actions">
        <Button disabled={pending} type="submit" variant="primary">
          {pending ? "Kaydediliyor…" : "Periyodik planı oluştur"}
        </Button>
        <Button type="button" variant="secondary" onClick={() => router.push("/bakim/planlama")}>
          Vazgeç
        </Button>
      </div>
    </form>
  );
}
