"use client";

import { useRouter } from "next/navigation";
import { useState } from "react";

import { Button } from "@/components/ui/button";
import {
  MAINTENANCE_ASSET_TYPE_OPTIONS,
  MAINTENANCE_PRIORITY_OPTIONS,
  MAINTENANCE_REQUEST_TYPE_OPTIONS,
} from "../presentation/maintenance-form-options";
import type { createMaintenanceWorkOrderAction } from "../server/maintenance-writer";
import { MaintenanceAssetReferenceField } from "./maintenance-asset-reference-field";
import type { MaintenanceAssetOption } from "../domain/maintenance-asset-option";
import { customerMessage } from "@/lib/presentation/customer-message";

type MaintenanceWorkOrderCreateFormProps = Readonly<{
  assetOptions?: readonly MaintenanceAssetOption[];
  onCreate: typeof createMaintenanceWorkOrderAction;
}>;

export function MaintenanceWorkOrderCreateForm({
  onCreate,
  assetOptions = [],
}: MaintenanceWorkOrderCreateFormProps) {
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
        setPending(true);
        setMessage(null);
        void onCreate({
          assetType: String(formData.get("assetType") ?? ""),
          assetReference: String(formData.get("assetReference") ?? ""),
          assetName: String(formData.get("assetName") ?? "") || undefined,
          type: String(formData.get("type") ?? ""),
          priority: String(formData.get("priority") ?? ""),
          title: String(formData.get("title") ?? ""),
          description: String(formData.get("description") ?? ""),
        }).then((result) => {
          if (result.status === "failure") {
            setPending(false);
            setMessage(result.message ?? "Bakım emri oluşturulamadı.");
            return;
          }

          router.push(`/bakim/emirler/${result.id}`);
          router.refresh();
        });
      }}
    >
      {message ? (
        <div className="maintenance-error-panel" role="alert">
          <p>{customerMessage(message)}</p>
        </div>
      ) : null}

      <div className="maintenance-form-grid maintenance-form-grid--four">
        <div className="maintenance-form-field">
          <label htmlFor="title">Başlık <span className="zx-required" aria-hidden="true">*</span></label>
          <input
            className="maintenance-form-input"
            disabled={pending}
            id="title"
            name="title"
            required
          />
        </div>
        <MaintenanceAssetReferenceField disabled={pending} options={assetOptions} />
        <div className="maintenance-form-field">
          <label htmlFor="type">Emir türü</label>
          <select
            className="maintenance-form-select"
            defaultValue="corrective"
            disabled={pending}
            id="type"
            name="type"
          >
            {MAINTENANCE_REQUEST_TYPE_OPTIONS.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </div>
        <div className="maintenance-form-field">
          <label htmlFor="priority">Öncelik</label>
          <select
            className="maintenance-form-select"
            defaultValue="normal"
            disabled={pending}
            id="priority"
            name="priority"
          >
            {MAINTENANCE_PRIORITY_OPTIONS.map((option) => (
              <option key={option.value} value={option.value}>
                {option.label}
              </option>
            ))}
          </select>
        </div>
        <details className="maintenance-form-more">
          <summary>Varlık adı ve türü (isteğe bağlı)</summary>
          <div className="maintenance-form-more__grid">
        <div className="maintenance-form-field">
          <label htmlFor="assetName">Varlık adı</label>
          <input
            className="maintenance-form-input"
            disabled={pending}
            id="assetName"
            name="assetName"
          />
          <small className="maintenance-form-hint">İsteğe bağlı; listeden seçince otomatik dolar.</small>
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
          </div>
        </details>
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
          {pending ? "Kaydediliyor…" : "Bakım emrini oluştur"}
        </Button>
        <Button type="button" variant="secondary" onClick={() => router.push("/bakim")}>
          Vazgeç
        </Button>
      </div>
    </form>
  );
}
