"use client";

import type { CommercialMaterialCostProfitabilityFilters } from "../application/commercial-material-cost-profitability-filter";
import type {
  CommercialCostDriverProfitabilityTableRow,
  CommercialMaterialCostProfitabilityWorkspacePayload,
  CommercialMaterialProfitabilityTableRow,
} from "../types/commercial-material-cost-profitability-types";
import {
  formatProfitabilityMarginDeltaDisplay,
  formatProfitabilityVarianceTone,
  inferProfitabilityUnavailableReason,
} from "./commercial-profitability-workspace-sections";
import { CommercialProfitabilityComparisonDetailPanel } from "./commercial-profitability-workspace-sections";
import { resolveProfitabilityMaterialLabel } from "./commercial-profitability-presentation";

export {
  CommercialProfitabilityComparisonDetailPanel,
  inferProfitabilityUnavailableReason,
  formatProfitabilityMarginDeltaDisplay,
  formatProfitabilityVarianceTone,
};

const FILTER_KEYS = [
  "materialId",
  "profileId",
  "outcome",
  "evidenceStatus",
  "windowFrom",
  "windowTo",
  "page",
  "lens",
] as const;

type MaterialCostProfitabilityFilterRailProps = Readonly<{
  workspace: CommercialMaterialCostProfitabilityWorkspacePayload;
  filters: CommercialMaterialCostProfitabilityFilters;
  onFilterChange: (updates: Partial<Record<(typeof FILTER_KEYS)[number], string>>) => void;
  onClear: () => void;
}>;

export function CommercialMaterialCostProfitabilityFilterRail({
  workspace,
  filters,
  onFilterChange,
  onClear,
}: MaterialCostProfitabilityFilterRailProps) {
  const materialOptions = [
    ...new Set([
      ...workspace.materialComparisons.map((item) => item.materialId),
      ...workspace.costMixComparisons.map((item) => item.materialId),
    ].filter(Boolean)),
  ] as string[];

  return (
    <div className="commint-filter-rail" data-testid="commercial-material-cost-profitability-filter-rail">
      <label className="commint-filter-field">
        <span>Malzeme referansı</span>
        <input
          type="search"
          value={filters.materialId}
          onChange={(event) => onFilterChange({ materialId: event.target.value, page: "1" })}
          placeholder="Malzeme kimliği"
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
      {materialOptions.length > 0 ? (
        <label className="commint-filter-field">
          <span>Hızlı malzeme</span>
          <select
            value={filters.materialId}
            onChange={(event) => onFilterChange({ materialId: event.target.value, page: "1" })}
          >
            <option value="">Tümü</option>
            {materialOptions.map((materialId) => (
              <option key={materialId} value={materialId}>{resolveProfitabilityMaterialLabel(materialId)}</option>
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

type MaterialProfitabilityComparisonTableProps = Readonly<{
  rows: readonly CommercialMaterialProfitabilityTableRow[];
  selectedComparisonId: string | null;
  onSelect: (comparisonId: string) => void;
}>;

export function CommercialMaterialProfitabilityComparisonTable({
  rows,
  selectedComparisonId,
  onSelect,
}: MaterialProfitabilityComparisonTableProps) {
  return (
    <div className="commint-table-wrap" data-testid="commercial-material-cost-profitability-material-table">
      <table className="commint-table">
        <thead>
          <tr>
            <th>Malzeme</th>
            <th>Referans marj</th>
            <th>Güncel marj</th>
            <th>Marj farkı</th>
            <th>Konum</th>
            <th>Sonuç</th>
            <th>Kanıt</th>
            <th>Kapsam</th>
            <th>Sürücü</th>
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
                data-testid={`commercial-material-cost-profitability-material-row-${row.id}`}
              >
                <td>
                  <button
                    type="button"
                    className="commint-table__link"
                    onClick={() => onSelect(row.id)}
                    aria-pressed={selectedComparisonId === row.id}
                  >
                    {row.materialLabel}
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
                <td>{row.leadingDriverLabel}</td>
                <td>{row.evaluatedLabel}</td>
              </tr>
            );
          })}
        </tbody>
      </table>
    </div>
  );
}

type CostDriverProfitabilityComparisonTableProps = Readonly<{
  rows: readonly CommercialCostDriverProfitabilityTableRow[];
  selectedComparisonId: string | null;
  onSelect: (comparisonId: string) => void;
}>;

export function CommercialCostDriverProfitabilityComparisonTable({
  rows,
  selectedComparisonId,
  onSelect,
}: CostDriverProfitabilityComparisonTableProps) {
  return (
    <div className="commint-table-wrap" data-testid="commercial-material-cost-profitability-cost-table">
      <table className="commint-table">
        <thead>
          <tr>
            <th>Karşılaştırma</th>
            <th>Malzeme</th>
            <th>Maliyet bileşeni</th>
            <th>Marj farkı</th>
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
                data-testid={`commercial-material-cost-profitability-cost-row-${row.id}`}
              >
                <td>
                  <button
                    type="button"
                    className="commint-table__link"
                    onClick={() => onSelect(row.id)}
                    aria-pressed={selectedComparisonId === row.id}
                  >
                    {row.comparisonReference}
                  </button>
                </td>
                <td>{row.materialLabel}</td>
                <td>{row.leadingDriverLabel}</td>
                <td className={`commint-variance--${tone}`}>
                  {formatProfitabilityMarginDeltaDisplay(row.marginDeltaLabel)}
                </td>
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
