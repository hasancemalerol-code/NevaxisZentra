/**
 * One no-data pattern for every Ticari Zekâ figure: a missing value is a muted dash-like note, never a bold number.
 */
const NO_DATA_VALUE = /^(—|–|-|Yok|Eksik|Hesaplanamadı|0 \/ 0|— \/ —)$/;

const ZERO = /^0([.,]0+)?$/;

export function isNoDataValue(value: unknown): boolean {
  return typeof value === "string" && NO_DATA_VALUE.test(value.trim());
}

/** Missing values and plain zeros share one quiet style so a zero never outweighs a dash beside it. */
export function kpiValueClass(value: unknown): string {
  return isNoDataValue(value) || (typeof value === "string" && ZERO.test(value.trim())) ? "commint-kpi-card__value commint-kpi-card__value--empty" : "commint-kpi-card__value";
}

export function detailValueClass(value: unknown): string {
  return isNoDataValue(value) ? "commint-detail-field__value commint-detail-field__value--empty" : "commint-detail-field__value";
}
