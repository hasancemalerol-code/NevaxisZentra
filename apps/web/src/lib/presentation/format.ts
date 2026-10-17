/**
 * One formatter layer for every customer-facing number, percentage and date.
 * Turkish locale, facility local time, never ISO/UTC strings, never a raw week key.
 */



import { enumLabel, localizeStatusWords } from "./enum-labels";

const LOCALE = "tr-TR";
export const FACILITY_TIME_ZONE = "Europe/Istanbul";
const EMPTY = "—";

type DateInput = string | number | Date | null | undefined;

function toDate(value: DateInput): Date | null {
  if (value === null || value === undefined || value === "") return null;
  const date = value instanceof Date ? value : new Date(value);
  return Number.isNaN(date.getTime()) ? null : date;
}

export function formatNumber(value: number | null | undefined, fractionDigits = 0): string {
  if (typeof value !== "number" || !Number.isFinite(value)) return EMPTY;
  return new Intl.NumberFormat(LOCALE, { minimumFractionDigits: fractionDigits, maximumFractionDigits: fractionDigits }).format(value);
}

/** `percent` is already a percentage (35 becomes "%35,0"). */
export function formatPercent(percent: number | null | undefined, fractionDigits = 1): string {
  if (typeof percent !== "number" || !Number.isFinite(percent)) return EMPTY;
  return `%${formatNumber(percent, fractionDigits)}`;
}

/**
 * `ratio` is a 0-1 fraction (0.35 becomes "%35,0"). A value above 1.5 is treated as an already-multiplied percentage,
 * so a source that sends 35 instead of 0.35 can never print as 3500%.
 */
export function formatRatioAsPercent(ratio: number | null | undefined, fractionDigits = 1): string {
  if (typeof ratio !== "number" || !Number.isFinite(ratio)) return EMPTY;
  return formatPercent(Math.abs(ratio) > 1.5 ? ratio : ratio * 100, fractionDigits);
}

export function formatDateTime(value: DateInput): string {
  const date = toDate(value);
  if (!date) return EMPTY;
  return new Intl.DateTimeFormat(LOCALE, { day: "2-digit", month: "short", year: "numeric", hour: "2-digit", minute: "2-digit", timeZone: FACILITY_TIME_ZONE }).format(date);
}

export function formatDate(value: DateInput): string {
  const date = toDate(value);
  if (!date) return EMPTY;
  return new Intl.DateTimeFormat(LOCALE, { day: "2-digit", month: "short", year: "numeric", timeZone: FACILITY_TIME_ZONE }).format(date);
}

export function formatTime(value: DateInput): string {
  const date = toDate(value);
  if (!date) return EMPTY;
  return new Intl.DateTimeFormat(LOCALE, { hour: "2-digit", minute: "2-digit", timeZone: FACILITY_TIME_ZONE }).format(date);
}

export function formatDateRange(from: DateInput, to: DateInput): string {
  const start = toDate(from);
  const end = toDate(to);
  if (!start && !end) return EMPTY;
  if (start && end) return `${formatDate(start)} – ${formatDate(end)}`;
  return formatDate(start ?? end);
}

/** "2026-W42" becomes "42. hafta 2026". Other values are returned unchanged. */
export function formatWeekKey(key: string | null | undefined): string {
  const match = /^(\d{4})-W(\d{1,2})$/.exec((key ?? "").trim());
  return match ? `${Number(match[2])}. hafta ${match[1]}` : (key ?? EMPTY);
}

export function formatMinutes(minutes: number | null | undefined): string {
  if (typeof minutes !== "number" || !Number.isFinite(minutes)) return EMPTY;
  if (minutes < 60) return `${formatNumber(minutes, 0)} dk`;
  const hours = Math.floor(minutes / 60);
  const rest = Math.round(minutes % 60);
  return rest === 0 ? `${hours} sa` : `${hours} sa ${rest} dk`;
}

/**
 * Evidence panels carry free-form "field: value" strings from the backend. Dates become local tr-TR dates, plain numbers
 * lose their trailing zeros, and machine words (open, fresh, requires-review) become Turkish labels.
 */
export function formatEvidenceValue(value: string | null | undefined): string {
  const text = (value ?? "").trim();
  if (!text) return EMPTY;
  if (/^\d{4}-\d{2}-\d{2}T\d{2}:\d{2}/.test(text)) return formatDateTime(text);
  if (/^-?\d+\.\d+$/.test(text)) return formatNumber(Number(text), 2).replace(/,00$/, "");
  if (/^[a-z]+([-_][a-z]+)*$/.test(text)) return enumLabel(text);
  return text;
}

/** A generated sentence ("Kaynak tazeliği: fresh | ERP güncelleme: 2026-10-13T16:56:57Z") made readable for customers. */
export function formatEvidenceSentence(text: string | null | undefined): string {
  return localizeStatusWords(text).replace(/\d{4}-\d{2}-\d{2}T\d{2}:\d{2}[^\s|;,)]*/g, (iso) => formatDateTime(iso));
}

/** Business reference codes (PO-2026-001) must not break at hyphens inside a table cell: hyphens become non-breaking. */
export function keepCode(value: string | null | undefined): string {
  return (value ?? "").replace(/-/g, "‑");
}
