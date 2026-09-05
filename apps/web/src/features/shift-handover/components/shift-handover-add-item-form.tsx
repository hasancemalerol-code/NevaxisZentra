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
      style={{ marginTop: "1rem" }}
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
        Başlık
        <input name="title" required />
      </label>
      <label>
        Açıklama
        <textarea name="description" rows={3} />
      </label>
      <label>
        Öncelik
        <select defaultValue="medium" name="severity">
          {Object.entries(SHIFT_HANDOVER_ITEM_SEVERITIES).map(([value, text]) => (
            <option key={value} value={value}>{text}</option>
          ))}
        </select>
      </label>
      <label>
        <input name="requiresFollowUp" type="checkbox" />
        Takip gerektirir
      </label>
      <Button type="submit">Madde Ekle</Button>
    </form>
  );
}
