"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";

import { Button } from "@/components/ui/button";
import {
  MAINTENANCE_ASSET_TYPE_OPTIONS,
  MAINTENANCE_INTERVAL_TYPE_OPTIONS,
} from "../presentation/maintenance-form-options";
import type { createPreventiveMaintenancePlanAction } from "../server/maintenance-writer";

type MaintenancePlanCreateFormProps = Readonly<{
  onCreate: typeof createPreventiveMaintenancePlanAction;
}>;

export function MaintenancePlanCreateForm({
  onCreate,
}: MaintenancePlanCreateFormProps) {
  const router = useRouter();
  const [pending, setPending] = useState(false);
  const [message, setMessage] = useState<string | null>(null);

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
          <p>{message}</p>
        </div>
      ) : null}

      <div className="maintenance-form-grid">
        <div className="maintenance-form-field">
          <label htmlFor="code">Plan kodu</label>
          <input
            className="maintenance-form-input"
            disabled={pending}
            id="code"
            name="code"
            required
          />
        </div>
        <div className="maintenance-form-field">
          <label htmlFor="name">Plan adı</label>
          <input
            className="maintenance-form-input"
            disabled={pending}
            id="name"
            name="name"
            required
          />
        </div>
        <div className="maintenance-form-field">
          <label htmlFor="assetReference">Varlık referansı</label>
          <input
            className="maintenance-form-input"
            disabled={pending}
            id="assetReference"
            name="assetReference"
            required
          />
        </div>
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
          <label htmlFor="intervalValue">Periyot değeri</label>
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
        </div>
        <div className="maintenance-form-field">
          <label htmlFor="nextDueAtUtc">Sonraki bakım</label>
          <input
            className="maintenance-form-input"
            disabled={pending}
            id="nextDueAtUtc"
            name="nextDueAtUtc"
            type="datetime-local"
          />
        </div>
      </div>

      <div className="maintenance-form-field">
        <label htmlFor="description">Açıklama</label>
        <textarea
          className="maintenance-form-textarea"
          disabled={pending}
          id="description"
          name="description"
        />
      </div>

      <div className="maintenance-form-actions">
        <Button disabled={pending} type="submit" variant="primary">
          {pending ? "Kaydediliyor…" : "Periyodik planı oluştur"}
        </Button>
      </div>
    </form>
  );
}
