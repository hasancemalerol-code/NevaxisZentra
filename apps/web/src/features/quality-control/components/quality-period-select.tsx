"use client";

import type { ChangeEvent } from "react";

export type QualityPeriodSelectProps = Readonly<{
  value: "day" | "week" | "month";
}>;

/** Reporting period switch: changing the value applies it immediately; the submit button only exists for no-script use. */
export function QualityPeriodSelect({ value }: QualityPeriodSelectProps) {
  function handleChange(event: ChangeEvent<HTMLSelectElement>) {
    event.currentTarget.form?.requestSubmit();
  }

  return (
    <form className="quality-period-switch" method="get">
      <label htmlFor="report-period">Dönem</label>
      <select
        id="report-period"
        name="period"
        className="quality-form-select"
        defaultValue={value}
        onChange={handleChange}
      >
        <option value="day">Günlük</option>
        <option value="week">Haftalık</option>
        <option value="month">Aylık</option>
      </select>
      <noscript>
        <button type="submit" className="quality-period-switch__submit">
          Uygula
        </button>
      </noscript>
    </form>
  );
}
