import { formatCount } from "../domain/commercial-intelligence-formatters";
import type { CommercialSalesPriceCustomerResult } from "../types/commercial-intelligence-types";

type CommercialIntelligenceCustomerRowProps = Readonly<{
  row: CommercialSalesPriceCustomerResult;
  currencyCode?: string | null;
  normalizedUnit?: string | null;
  showExcludedCount?: boolean;
  excludedObservationCount?: number;
}>;

export function CommercialIntelligenceCustomerRow({
  row,
  currencyCode,
  normalizedUnit,
  showExcludedCount = false,
  excludedObservationCount = 0,
}: CommercialIntelligenceCustomerRowProps) {
  return (
    <div
      className={`commint-supplier-row${row.positionState === "lowest-comparable-price" ? " commint-supplier-row--baseline" : ""}`}
      data-customer-rank={row.priceRank}
      data-customer-tied={row.isTied ? "true" : "false"}
    >
      <div className="commint-supplier-row__top">
        <p className="commint-supplier-row__title">
          {row.customerLabel}
          {row.isTied ? <span className="commint-badge commint-badge--tie">Eşit sıra</span> : null}
          {row.positionState === "lowest-comparable-price" ? (
            <span className="commint-badge commint-badge--baseline">Fiyat baz çizgisi</span>
          ) : null}
        </p>
        <span className="commint-badge">Sıra {row.priceRank}</span>
      </div>
      <p className="commint-supplier-row__meta">
        Medyan: {row.customerMedianLabel}
        {currencyCode && normalizedUnit ? ` / ${normalizedUnit}` : ""}
        {" · "}
        Konum: {row.relativePositionLabel}
        {" · "}
        Karşılaştırılabilir: {formatCount(row.comparableObservationCount)}
        {showExcludedCount ? ` · Dışlanan: ${formatCount(excludedObservationCount)}` : ""}
        {" · "}
        {row.positionStateLabel}
      </p>
    </div>
  );
}
