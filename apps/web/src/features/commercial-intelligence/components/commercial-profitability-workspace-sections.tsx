"use client";

import type { ReactNode } from "react";

import {
  formatCount,
  formatRatioAsPercent,
} from "../domain/commercial-intelligence-formatters";
import type {
  CommercialProfitabilityComparisonDetail,
  CommercialProfitabilityComparisonSummary,
  CommercialProfitabilityWorkspacePayload,
} from "../types/commercial-profitability-types";
import {
  buildCommercialProfitabilityFilterParams,
  clearCommercialProfitabilityFilterParams,
  readCommercialProfitabilityComparisonFilters,
  toCommercialProfitabilityApiFilters,
  type CommercialProfitabilityComparisonFilters,
} from "../application/commercial-profitability-filter";
import { reconcileProfitabilityAssessmentCounts } from "./commercial-profitability-presentation";

export {
  buildCommercialProfitabilityFilterParams,
  clearCommercialProfitabilityFilterParams,
  readCommercialProfitabilityComparisonFilters,
  toCommercialProfitabilityApiFilters,
  type CommercialProfitabilityComparisonFilters,
};

export function inferProfitabilityUnavailableReason(
  comparison: CommercialProfitabilityComparisonSummary | null,
): string | null {
  if (!comparison) return "Karşılaştırma kaydı yok";
  if (comparison.comparisonOutcome === "source-conflict") return "Kaynak çakışması";
  if (comparison.comparisonOutcome === "insufficient-data") return "Karşılaştırma için yeterli veri yok";
  return null;
}

function DetailField({ label, value }: Readonly<{ label: string; value: string }>) {
  return (
    <div className="commint-detail-field">
      <p className="commint-detail-field__label">{label}</p>
      <p className="commint-detail-field__value">{value}</p>
    </div>
  );
}

function DetailSection({
  title,
  children,
  testId,
}: Readonly<{ title: string; children: ReactNode; testId?: string }>) {
  return (
    <section className="commint-detail-section" data-testid={testId ? `commercial-profitability-detail-${testId}` : undefined}>
      <h3 className="commint-detail-section__title">{title}</h3>
      {children}
    </section>
  );
}

const FILTER_KEYS = [
  "comparisonType",
  "profileId",
  "materialId",
  "customerReference",
  "outcome",
  "evidenceStatus",
  "windowFrom",
  "windowTo",
] as const;

type CommercialProfitabilityComparisonFilterRailProps = Readonly<{
  workspace: CommercialProfitabilityWorkspacePayload;
  filters: CommercialProfitabilityComparisonFilters;
  onFilterChange: (updates: Partial<Record<(typeof FILTER_KEYS)[number], string>>) => void;
  onClear: () => void;
}>;

export function CommercialProfitabilityComparisonFilterRail({
  workspace,
  filters,
  onFilterChange,
  onClear,
}: CommercialProfitabilityComparisonFilterRailProps) {
  const materialOptions = [...new Set(workspace.comparisons.map((item) => item.materialId).filter(Boolean))] as string[];
  const customerOptions = [...new Set(workspace.comparisons.map((item) => item.customerReference).filter(Boolean))] as string[];

  return (
    <div className="commint-filter-rail" data-testid="commercial-profitability-filter-rail">
      <label className="commint-filter-field">
        <span>Karşılaştırma türü</span>
        <select
          value={filters.comparisonType}
          onChange={(event) => onFilterChange({ comparisonType: event.target.value })}
        >
          <option value="">Tümü</option>
          <option value="temporal-contribution-trend">Zamansal katkı eğilimi</option>
          <option value="cost-component-mix-change">Maliyet bileşeni karışımı</option>
          <option value="customer-contribution-position">Müşteri katkı konumu</option>
          <option value="material-contribution-position">Ürün katkı konumu</option>
        </select>
      </label>
      <label className="commint-filter-field">
        <span>Profil</span>
        <select
          value={filters.profileId}
          onChange={(event) => onFilterChange({ profileId: event.target.value })}
        >
          <option value="">Tümü</option>
          {workspace.profiles.map((profile) => (
            <option key={profile.id} value={profile.id}>{profile.profileCode}</option>
          ))}
        </select>
      </label>
      <label className="commint-filter-field">
        <span>Malzeme</span>
        <select
          value={filters.materialId}
          onChange={(event) => onFilterChange({ materialId: event.target.value })}
        >
          <option value="">Tümü</option>
          {materialOptions.map((materialId) => {
            const label = workspace.comparisons.find((item) => item.materialId === materialId)?.materialLabel ?? materialId;
            return <option key={materialId} value={materialId}>{label}</option>;
          })}
        </select>
      </label>
      <label className="commint-filter-field">
        <span>Müşteri</span>
        <select
          value={filters.customerReference}
          onChange={(event) => onFilterChange({ customerReference: event.target.value })}
        >
          <option value="">Tümü</option>
          {customerOptions.map((customerReference) => (
            <option key={customerReference} value={customerReference}>{customerReference}</option>
          ))}
        </select>
      </label>
      <label className="commint-filter-field">
        <span>Sonuç</span>
        <select
          value={filters.outcome}
          onChange={(event) => onFilterChange({ outcome: event.target.value })}
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
          onChange={(event) => onFilterChange({ evidenceStatus: event.target.value })}
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
          onChange={(event) => onFilterChange({ windowFrom: event.target.value })}
        />
      </label>
      <label className="commint-filter-field">
        <span>Pencere bitişi</span>
        <input
          type="date"
          value={filters.windowTo}
          onChange={(event) => onFilterChange({ windowTo: event.target.value })}
        />
      </label>
      <button type="button" className="commint-button commint-button--ghost" onClick={onClear}>
        Filtreleri temizle
      </button>
    </div>
  );
}

type CommercialProfitabilityComparisonDetailPanelProps = Readonly<{
  comparisonDetail: CommercialProfitabilityComparisonDetail;
}>;

export function CommercialProfitabilityComparisonDetailPanel({
  comparisonDetail,
}: CommercialProfitabilityComparisonDetailPanelProps) {
  const { summary } = comparisonDetail;
  const leadingDriver = comparisonDetail.drivers.find((driver) => driver.isLeadingDriver);
  const supportingDrivers = comparisonDetail.drivers
    .filter((driver) => !driver.isLeadingDriver)
    .sort((left, right) => left.sortOrder - right.sortOrder)
    .slice(0, 6);
  const denominatorValid = reconcileProfitabilityAssessmentCounts(
    summary.candidateAssessmentCount,
    summary.comparableAssessmentCount,
    summary.excludedAssessmentCount,
  );

  return (
    <div data-testid="commercial-profitability-comparison-detail-ready">
      <DetailSection title="Kimlik ve kapsam" testId="identity">
        <div className="commint-detail-grid">
          <DetailField label="Referans" value={summary.comparisonReference} />
          <DetailField label="Karşılaştırma türü" value={summary.comparisonTypeLabel} />
          <DetailField label="Profil" value={summary.profileCode} />
          <DetailField label="Malzeme" value={summary.materialLabel} />
          <DetailField label="Müşteri kapsamı" value={summary.customerLabel || "Tüm müşteriler"} />
        </div>
      </DetailSection>
      <DetailSection title="Pencereler" testId="windows">
        <div className="commint-detail-grid">
          <DetailField label="Referans penceresi" value={summary.referenceWindowLabel || "—"} />
          <DetailField label="Güncel pencere" value={summary.currentWindowLabel} />
        </div>
      </DetailSection>
      <DetailSection title="Katkı marjı" testId="margin">
        <div className="commint-detail-grid">
          <DetailField label="Referans katkı marjı" value={summary.referenceMarginLabel} />
          <DetailField label="Güncel katkı marjı" value={summary.currentMarginLabel} />
          <DetailField
            label="Katkı marjı değişimi"
            value={summary.marginDeltaLabel === "—" ? "Hesaplanamadı" : summary.marginDeltaLabel}
          />
          <DetailField label="Referans birim katkı" value={comparisonDetail.referenceMedianUnitContributionLabel} />
          <DetailField label="Güncel birim katkı" value={comparisonDetail.currentMedianUnitContributionLabel} />
          <DetailField label="Birim katkı değişimi" value={comparisonDetail.unitContributionDeltaLabel} />
          <DetailField label="Sonuç" value={summary.outcomeLabel} />
          <DetailField label="Kanıt durumu" value={summary.evidenceLabel} />
        </div>
      </DetailSection>
      <DetailSection title="Kapsam sayımları" testId="counts">
        <div className="commint-detail-grid">
          <DetailField label="Aday değerlendirme" value={formatCount(summary.candidateAssessmentCount)} />
          <DetailField label="Karşılaştırılabilir" value={formatCount(summary.comparableAssessmentCount)} />
          <DetailField label="Hariç tutulan" value={formatCount(summary.excludedAssessmentCount)} />
          <DetailField
            label="Payda doğrulaması"
            value={denominatorValid
              ? `${formatCount(summary.comparableAssessmentCount)} + ${formatCount(summary.excludedAssessmentCount)}`
              : "Doğrulanamadı"}
          />
          <DetailField label="Tam kapsam" value={formatCount(comparisonDetail.completeCoverageCount)} />
          <DetailField label="Kısmi kapsam" value={formatCount(comparisonDetail.partialCoverageCount)} />
          <DetailField label="Yetersiz veri" value={formatCount(comparisonDetail.insufficientDataCount)} />
          <DetailField label="Kaynak çakışması" value={formatCount(comparisonDetail.sourceConflictCount)} />
        </div>
      </DetailSection>
      {comparisonDetail.results.length > 0 ? (
        <DetailSection title="Sonuç satırları" testId="results">
          <div className="commint-table-wrap">
            <table className="commint-table">
              <thead>
                <tr>
                  <th>Kapsam</th>
                  <th>Referans marj</th>
                  <th>Güncel marj</th>
                  <th>Marj farkı</th>
                  <th>Maliyet payı farkı</th>
                  <th>Konum</th>
                </tr>
              </thead>
              <tbody>
                {comparisonDetail.results.map((row) => (
                  <tr key={row.id}>
                    <td>
                      {row.componentKindLabel !== "—"
                        ? row.componentKindLabel
                        : row.customerLabel !== "—"
                          ? row.customerLabel
                          : row.materialLabel}
                    </td>
                    <td>{row.referenceMarginLabel}</td>
                    <td>{row.currentMarginLabel}</td>
                    <td>{row.marginDeltaLabel}</td>
                    <td>{row.costShareDeltaLabel}</td>
                    <td>{row.positionStateLabel}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </DetailSection>
      ) : null}
      <DetailSection title="Sürücüler" testId="drivers">
        <DetailField label="Önde gelen sürücü" value={leadingDriver?.reasonLabel ?? summary.leadingDriverLabel ?? "—"} />
        {supportingDrivers.length > 0 ? (
          <ul className="commint-driver-list" data-testid="commercial-profitability-comparison-supporting-drivers">
            {supportingDrivers.map((driver) => (
              <li key={driver.id}>
                {driver.reasonLabel}
                {driver.normalizedVarianceLabel !== "—" ? ` · ${driver.normalizedVarianceLabel}` : ""}
                {driver.supportingText ? ` · ${driver.supportingText}` : ""}
              </li>
            ))}
          </ul>
        ) : (
          <p className="commint-supplier-row__meta">Destekleyici sürücü yok</p>
        )}
      </DetailSection>
      <DetailSection title="Köken" testId="provenance">
        <div className="commint-detail-grid">
          <DetailField label="Köken" value={comparisonDetail.sourceProvenance || "—"} />
          <DetailField label="Kural seti" value={summary.ruleSetVersion} />
          <DetailField label="Değerlendirme" value={summary.evaluatedLabel} />
        </div>
      </DetailSection>
    </div>
  );
}

export function formatProfitabilityVarianceTone(
  outcome: string,
): "neutral" | "positive" | "negative" | "warning" {
  if (outcome === "contribution-improvement") return "positive";
  if (outcome === "contribution-deterioration") return "negative";
  if (outcome === "insufficient-data" || outcome === "source-conflict") return "warning";
  return "neutral";
}

export function formatProfitabilityMarginDeltaDisplay(deltaLabel: string): string {
  if (deltaLabel === "—") return "Hesaplanamadı";
  return deltaLabel;
}

export function formatDriverVarianceLabel(normalizedVariance: number | null): string {
  return formatRatioAsPercent(normalizedVariance);
}
