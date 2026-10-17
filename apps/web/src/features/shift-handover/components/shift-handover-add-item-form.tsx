"use client";

import { Button } from "@/components/ui/button";
import {
  SHIFT_HANDOVER_ITEM_CATEGORIES,
  SHIFT_HANDOVER_ITEM_SEVERITIES,
} from "../domain/shift-handover-constants";
import { addShiftHandoverItemAction } from "../server/shift-handover-actions";

type ShiftHandoverAddItemFormProps = Readonly<{
  handoverId: string;
}>;

async function submitAddShiftHandoverItem(handoverId: string, formData: FormData) {
  await addShiftHandoverItemAction(handoverId, formData);
}

export function ShiftHandoverAddItemForm({ handoverId }: ShiftHandoverAddItemFormProps) {
  return (
    <form
      action={(formData) => submitAddShiftHandoverItem(handoverId, formData)}
      className="shift-handover-form-grid"
    >
      <label>
        Kategori
        <select defaultValue="general" name="category">
          {Object.entries(SHIFT_HANDOVER_ITEM_CATEGORIES).map(([value, text]) => (
            <option key={value} value={value}>{text}</option>
          ))}
        </select>
      </label>
      <label>
        Başlık <span className="zx-required" aria-hidden="true">*</span>
        <input name="title" required />
      </label>
      <label className="shift-handover-field--wide">
        Açıklama
        <textarea name="description" rows={2} />
      </label>
      <label>
        Öncelik
        <select defaultValue="medium" name="severity">
          {Object.entries(SHIFT_HANDOVER_ITEM_SEVERITIES).map(([value, text]) => (
            <option key={value} value={value}>{text}</option>
          ))}
        </select>
      </label>
      <label className="shift-handover-check">
        <input name="requiresFollowUp" type="checkbox" />
        <span>Takip gerektirir</span>
      </label>
      <div className="shift-handover-form-actions">
        <Button type="submit">Kalem Ekle</Button>
      </div>
    </form>
  );
}
