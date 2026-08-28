"use client";

import type { CommercialCustomerProfitabilityFilters } from "../application/commercial-customer-profitability-filter";
import type {
  CommercialCustomerProfitabilityTableRow,
  CommercialCustomerProfitabilityWorkspacePayload,
} from "../types/commercial-customer-profitability-types";
import {
  formatProfitabilityMarginDeltaDisplay,
  formatProfitabilityVarianceTone,
  inferProfitabilityUnavailableReason,
} from "./commercial-profitability-workspace-sections";
import { CommercialProfitabilityComparisonDetailPanel } from "./commercial-profitability-workspace-sections";

export {
  CommercialProfitabilityComparisonDetailPanel,
  inferProfitabilityUnavailableReason,
  formatProfitabilityMarginDeltaDisplay,
  formatProfitabilityVarianceTone,
};

const FILTER_KEYS = [
  "customerReference",
  "profileId",
  "outcome",
  "evidenceStatus",
  "windowFrom",
  "windowTo",
  "page",
] as const;

type CustomerProfitabilityFilterRailProps = Readonly<{
  workspace: CommercialCustomerProfitabilityWorkspacePayload;
  filters: CommercialCustomerProfitabilityFilters;
  onFilterChange: (updates: Partial<Record<(typeof FILTER_KEYS)[number], string>>) => void;
  onClear: () => void;
}>;

export function CommercialCustomerProfitabilityFilterRail({
  workspace,
  filters,
  onFilterChange,
  onClear,
}: CustomerProfitabilityFilterRailProps) {
  const customerOptions = [...new Set(workspace.comparisons.map((item) => item.customerReference).filter(Boolean))] as string[];

  return (
    <div className="commint-filter-rail" data-testid="commercial-customer-profitability-filter-rail">
      <label className="commint-filter-field">
        <span>Müşteri referansı</span>
        <input
          type="search"
          value={filters.customerReference}
          onChange={(event) => onFilterChange({ customerReference: event.target.value, page: "1" })}
          placeholder="Müşteri referansı"
        />
      </label>
      <label className="commint-filter-field">
        <span>Profil</span>
        <select
          value={filters.profileId}
          onChange={(event) => onFilterChange({ profileId: event.target.value, page: "1" })}
        >
          <option value="">Tümü</option>
          {workspace.profiles.map((profile) => (
            <option key={profile.id} value={profile.id}>{profile.profileCode}</option>
          ))}
        </select>
      </label>
      <label className="commint-filter-field">
        <span>Sonuç</span>
        <select
          value={filters.outcome}
          onChange={(event) => onFilterChange({ outcome: event.target.value, page: "1" })}
        >
          <option value="">Tümü</option>
          <option value="within-comparison-band">Karşılaştırma bandı içinde</option>
          <option value="contribution-deterioration">Katkı bozulması</option>
          <option value="contribution-improvement">Katkı iyileşmesi</option>
          <option value="insufficient-data">Yetersiz veri</option>
          <option value="source-conflict">Kaynak çakışması</option>
        </select>
      </label>
      <label className="commint-filter-field">
        <span>Kanıt durumu</span>
        <select
          value={filters.evidenceStatus}
          onChange={(event) => onFilterChange({ evidenceStatus: event.target.value, page: "1" })}
        >
          <option value="">Tümü</option>
          <option value="complete-coverage">Tam kapsam</option>
          <option value="partial-coverage">Kısmi kapsam</option>
          <option value="insufficient-data">Yetersiz veri</option>
          <option value="source-conflict">Kaynak çakışması</option>
        </select>
      </label>
      <label className="commint-filter-field">
        <span>Pencere başlangıcı</span>
        <input
          type="date"
          value={filters.windowFrom}
          onChange={(event) => onFilterChange({ windowFrom: event.target.value, page: "1" })}
        />
      </label>
      <label className="commint-filter-field">
        <span>Pencere bitişi</span>
        <input
          type="date"
          value={filters.windowTo}
          onChange={(event) => onFilterChange({ windowTo: event.target.value, page: "1" })}
        />
      </label>
      {customerOptions.length > 0 ? (
        <label className="commint-filter-field">
          <span>Hızlı müşteri</span>
          <select
            value={filters.customerReference}
            onChange={(event) => onFilterChange({ customerReference: event.target.value, page: "1" })}
          >
            <option value="">Tümü</option>
            {customerOptions.map((customerReference) => (
              <option key={customerReference} value={customerReference}>{customerReference}</option>
            ))}
          </select>
        </label>
      ) : null}
      <button type="button" className="commint-button commint-button--ghost" onClick={onClear}>
        Filtreleri temizle
      </button>
    </div>
  );
}

type CustomerProfitabilityComparisonTableProps = Readonly<{
  rows: readonly CommercialCustomerProfitabilityTableRow[];
  selectedComparisonId: string | null;
  onSelect: (comparisonId: string) => void;
}>;

export function CommercialCustomerProfitabilityComparisonTable({
  rows,
  selectedComparisonId,
  onSelect,
}: CustomerProfitabilityComparisonTableProps) {
  return (
    <div className="commint-table-wrap" data-testid="commercial-customer-profitability-table">
      <table className="commint-table">
        <thead>
          <tr>
            <th>Müşteri</th>
            <th>Referans marj</th>
            <th>Güncel marj</th>
            <th>Marj farkı</th>
            <th>Konum</th>
            <th>Sonuç</th>
            <th>Kanıt</th>
            <th>Kapsam</th>
            <th>Değerlendirme</th>
          </tr>
        </thead>
        <tbody>
          {rows.map((row) => {
            const tone = formatProfitabilityVarianceTone(row.comparisonOutcome);
            return (
              <tr
                key={row.id}
                className={selectedComparisonId === row.id ? "commint-table__row--selected" : undefined}
                data-testid={`commercial-customer-profitability-row-${row.id}`}
              >
                <td>
                  <button
                    type="button"
                    className="commint-table__link"
                    onClick={() => onSelect(row.id)}
                    aria-pressed={selectedComparisonId === row.id}
                  >
                    {row.customerLabel}
                  </button>
                </td>
                <td>{row.referenceMarginLabel}</td>
                <td>{row.currentMarginLabel}</td>
                <td className={`commint-variance--${tone}`}>
                  {formatProfitabilityMarginDeltaDisplay(row.marginDeltaLabel)}
                </td>
                <td>{row.positionStateLabel}</td>
                <td>{row.outcomeLabel}</td>
                <td>{row.evidenceLabel}</td>
                <td>{row.coverageLabel}</td>
                <td>{row.evaluatedLabel}</td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}
