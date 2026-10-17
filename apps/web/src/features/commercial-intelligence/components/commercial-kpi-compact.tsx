import { isNoDataValue } from "./commercial-no-data";

type CompactKpi = Readonly<{ id: string; label: string; value: string; caption?: string }>;

const ZERO_VALUE = /^0([.,]0+)?$/;

/** True when no figure in the set carries real data (every value is a dash, "yok" or a plain zero). */
export function isKpiSetEmpty(items: readonly Readonly<{ value: string }>[]): boolean {
  return items.length > 0 && items.every((item) => isNoDataValue(item.value) || ZERO_VALUE.test(item.value.trim()));
}

/** One compact card for a KPI set with no data: label and muted value per row instead of a wall of tall cards. */
export function CommercialKpiCompact({
  items,
  ariaLabel,
  testId,
}: Readonly<{ items: readonly CompactKpi[]; ariaLabel: string; testId?: string }>) {
  return (
    <section className="commint-kpi-compact" aria-label={ariaLabel} data-testid={testId}>
      <h2 className="commint-kpi-compact__title">Göstergeler</h2>
      <ul className="commint-kpi-compact__list">
        {items.map((item) => (
          <li key={item.id} className="commint-kpi-compact__row" data-kpi-id={item.id}>
            <span className="commint-kpi-compact__label">{item.label}</span>
            <span className="commint-kpi-compact__value">{isNoDataValue(item.value) ? "—" : item.value}</span>
            {item.caption ? <span className="commint-kpi-compact__caption">{item.caption}</span> : null}
          </li>
        ))}
      </ul>
    </section>
  );
}
