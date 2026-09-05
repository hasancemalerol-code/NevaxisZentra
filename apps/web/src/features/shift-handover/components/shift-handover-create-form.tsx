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
        Vardiya Tarihi
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
        Departman / Alan
        <input name="department" required />
      </label>
      <label>
        Özet
        <textarea name="summary" rows={3} />
      </label>
      <Button type="submit">Taslak Oluştur</Button>
    </form>
  );
}
