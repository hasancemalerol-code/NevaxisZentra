"use client";

import { useState } from "react";

import type { MaintenanceAssetOption } from "../domain/maintenance-asset-option";

type MaintenanceAssetReferenceFieldProps = Readonly<{
  disabled: boolean;
  options: readonly MaintenanceAssetOption[];
}>;

const MANUAL_ENTRY = "__manual__";

function fillAssetName(form: HTMLFormElement | null, name: string | undefined) {
  const nameInput = form?.elements.namedItem("assetName");
  if (name && nameInput instanceof HTMLInputElement && nameInput.value.trim() === "") {
    nameInput.value = name;
  }
}

/**
 * Asset reference: a real picker (select) over the factory resource list when it is known, with an explicit
 * "enter a code" escape for assets that are not registered (molds, tools). Without a list it is a plain code input.
 * Picking a known reference fills the (optional) asset name field of the same form when it is still empty.
 */
export function MaintenanceAssetReferenceField({ disabled, options }: MaintenanceAssetReferenceFieldProps) {
  const hasOptions = options.length > 0;
  const [manual, setManual] = useState(!hasOptions);

  return (
    <div className="maintenance-form-field">
      <label htmlFor="assetReference">
        Varlık referansı <span className="zx-required" aria-hidden="true">*</span>
      </label>
      {manual ? (
        <input
          autoComplete="off"
          className="maintenance-form-input"
          disabled={disabled}
          id="assetReference"
          name="assetReference"
          placeholder="Makine, kalıp veya ekipman kodu"
          required
        />
      ) : (
        <select
          className="maintenance-form-select"
          defaultValue=""
          disabled={disabled}
          id="assetReference"
          name="assetReference"
          onChange={(event) => {
            if (event.target.value === MANUAL_ENTRY) {
              setManual(true);
              return;
            }
            const match = options.find((option) => option.reference === event.target.value);
            fillAssetName(event.target.form, match?.name);
          }}
          required
        >
          <option value="" disabled>
            Varlık seçin
          </option>
          {options.map((option) => (
            <option key={option.reference} value={option.reference}>
              {option.name ? `${option.reference} · ${option.name}` : option.reference}
            </option>
          ))}
          <option value={MANUAL_ENTRY}>Listede yok, kodu elle gir</option>
        </select>
      )}
    </div>
  );
}
