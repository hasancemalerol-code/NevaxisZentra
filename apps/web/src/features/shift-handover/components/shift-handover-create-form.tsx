"use client";

import { Button } from "@/components/ui/button";
import { SHIFT_HANDOVER_SHIFT_PERIODS } from "../domain/shift-handover-constants";
import { createShiftHandoverAction } from "../server/shift-handover-actions";

async function submitCreateShiftHandover(formData: FormData) {
  await createShiftHandoverAction(formData);
}

export function ShiftHandoverCreateForm() {
  return (
    <form action={submitCreateShiftHandover} className="shift-handover-form-grid">
      <label>
        Vardiya tarihi <span className="zx-required" aria-hidden="true">*</span>
        <input name="shiftDate" required type="date" />
      </label>
      <label>
        Vardiya
        <select defaultValue="morning" name="shiftPeriod">
          {Object.entries(SHIFT_HANDOVER_SHIFT_PERIODS).map(([value, text]) => (
            <option key={value} value={value}>{text}</option>
          ))}
        </select>
      </label>
      <label>
        Departman veya alan <span className="zx-required" aria-hidden="true">*</span>
        <input name="department" placeholder="Örn. Üretim" required />
      </label>
      <label className="shift-handover-field--wide">
        Özet
        <textarea name="summary" rows={2} />
      </label>
      <div className="shift-handover-form-actions">
        <Button type="submit">Taslak Oluştur</Button>
      </div>
    </form>
  );
}
