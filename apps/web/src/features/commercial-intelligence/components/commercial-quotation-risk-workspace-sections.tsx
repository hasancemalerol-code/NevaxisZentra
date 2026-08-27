"use client";

import { useMemo, type ReactNode } from "react";

import {
  formatCount,
  formatCurrency,
  formatIncidenceDeltaPoints,
  formatRatioAsPercent,
  formatRemainingValidityDays,
} from "../domain/commercial-intelligence-formatters";
import {
  getQuotationRiskDriverFamilyLabel,
} from "../domain/commercial-quotation-risk-labels";
import type {
  CommercialQuotationRiskAssessmentDetail,
  CommercialQuotationRiskAssessmentSummary,
  CommercialQuotationRiskComparisonDetail,
  CommercialQuotationRiskWorkspacePayload,
} from "../types/commercial-quotation-risk-types";
import {
  buildQuotationRiskAssessmentPrimaryLabel,
  buildQuotationRiskAssessmentSecondaryLabel,
} from "./commercial-quotation-risk-presentation";

export type QuotationRiskAssessmentFilters = Readonly<{
  outcome: string;
  customer: string;
  materialId: string;
  driverFamily: string;
  dateFrom: string;
  dateTo: string;
  search: string;
}>;

const ASSESSMENT_FILTER_KEYS = [
  "assessmentOutcome",
  "assessmentCustomer",
  "assessmentMaterial",
  "assessmentDriverFamily",
  "assessmentFrom",
  "assessmentTo",
  "assessmentSearch",
] as const;

export function readQuotationRiskAssessmentFilters(
  searchParams: URLSearchParams,
): QuotationRiskAssessmentFilters {
  return {
    outcome: searchParams.get("assessmentOutcome") ?? "",
    customer: searchParams.get("assessmentCustomer") ?? "",
    materialId: searchParams.get("assessmentMaterial") ?? "",
    driverFamily: searchParams.get("assessmentDriverFamily") ?? "",
    dateFrom: searchParams.get("assessmentFrom") ?? "",
    dateTo: searchParams.get("assessmentTo") ?? "",
    search: searchParams.get("assessmentSearch") ?? "",
  };
}

export function buildQuotationRiskAssessmentFilterParams(
  current: URLSearchParams,
  updates: Partial<Record<(typeof ASSESSMENT_FILTER_KEYS)[number], string>>,
): URLSearchParams {
  const params = new URLSearchParams(current.toString());
  params.set("tab", "assessments");
  for (const [key, value] of Object.entries(updates)) {
    if (!value) params.delete(key);
    else params.set(key, value);
  }
  return params;
}

export function clearQuotationRiskAssessmentFilterParams(current: URLSearchParams): URLSearchParams {
  const params = new URLSearchParams(current.toString());
  params.set("tab", "assessments");
  for (const key of ASSESSMENT_FILTER_KEYS) params.delete(key);
  return params;
}

export function filterQuotationRiskAssessments(
  rows: readonly CommercialQuotationRiskAssessmentSummary[],
  filters: QuotationRiskAssessmentFilters,
): readonly CommercialQuotationRiskAssessmentSummary[] {
  const fromMs = filters.dateFrom ? Date.parse(`${filters.dateFrom}T00:00:00.000Z`) : null;
  const toMs = filters.dateTo ? Date.parse(`${filters.dateTo}T23:59:59.999Z`) : null;
  const search = filters.search.trim().toLowerCase();

  return rows.filter((item) => {
    if (filters.customer && item.customerReference !== filters.customer) return false;
    if (filters.materialId && item.materialId !== filters.materialId) return false;
    if (filters.driverFamily && getQuotationRiskDriverFamilyLabel(item.leadingDriverCode) !== filters.driverFamily) {
      return false;
    }

    const evaluatedMs = Date.parse(item.evaluatedAtUtc);
    if (fromMs !== null && Number.isFinite(fromMs) && evaluatedMs < fromMs) return false;
    if (toMs !== null && Number.isFinite(toMs) && evaluatedMs > toMs) return false;

    if (search) {
      const haystack = [
        item.assessmentReference,
        item.quotationReference,
        item.customerReference,
        item.customerLabel,
        item.materialLabel,
        item.outcomeLabel,
        item.leadingDriverLabel,
        item.varianceLabel,
      ].join(" ").toLowerCase();
      if (!haystack.includes(search)) return false;
    }

    return true;
  });
}

type QuotationRiskAssessmentFilterRailProps = Readonly<{
  workspace: CommercialQuotationRiskWorkspacePayload;
  filters: QuotationRiskAssessmentFilters;
  onFilterChange: (updates: Partial<Record<(typeof ASSESSMENT_FILTER_KEYS)[number], string>>) => void;
  onClearFilters: () => void;
  isRefreshing?: boolean;
}>;

export function QuotationRiskAssessmentFilterRail({
  workspace,
  filters,
  onFilterChange,
  onClearFilters,
  isRefreshing = false,
}: QuotationRiskAssessmentFilterRailProps) {
  const materialOptions = useMemo(() => {
    const map = new Map<string, string>();
    for (const item of workspace.assessments) map.set(item.materialId, item.materialLabel);
    return [...map.entries()].sort((left, right) => left[1].localeCompare(right[1], "tr"));
  }, [workspace.assessments]);

  const customerOptions = useMemo(() => {
    const map = new Map<string, string>();
    for (const item of workspace.assessments) {
      map.set(item.customerReference, item.customerLabel);
    }
    return [...map.entries()].sort((left, right) => left[1].localeCompare(right[1], "tr"));
  }, [workspace.assessments]);

  const driverFamilyOptions = useMemo(() => {
    const values = new Set<string>();
    for (const item of workspace.assessments) {
      values.add(getQuotationRiskDriverFamilyLabel(item.leadingDriverCode));
    }
    return [...values].sort((left, right) => left.localeCompare(right, "tr"));
  }, [workspace.assessments]);

  const hasActiveFilters = Boolean(
    filters.outcome
    || filters.customer
    || filters.materialId
    || filters.driverFamily
    || filters.dateFrom
    || filters.dateTo
    || filters.search.trim(),
  );

  return (
    <div
      className="commint-filter-rail"
      data-testid="commercial-quotation-risk-assessments-filter-rail"
      aria-label="Değerlendirme filtreleri"
    >
      <label className="commint-filter-rail__field">
        <span className="commint-filter-rail__label">Sonuç</span>
        <select
          value={filters.outcome}
          onChange={(event) => onFilterChange({ assessmentOutcome: event.target.value })}
          aria-label="Sonuç filtresi"
          data-testid="commercial-quotation-risk-assessments-filter-outcome"
          disabled={isRefreshing}
        >
          <option value="">Tüm sonuçlar</option>
          <option value="within-threshold">Eşik içinde</option>
          <option value="attention">Dikkat gerektirir</option>
          <option value="insufficient-data">Yetersiz veri</option>
          <option value="source-conflict">Kaynak çakışması</option>
        </select>
      </label>
      <label className="commint-filter-rail__field">
        <span className="commint-filter-rail__label">Müşteri</span>
        <select
          value={filters.customer}
          onChange={(event) => onFilterChange({ assessmentCustomer: event.target.value })}
          aria-label="Müşteri filtresi"
          data-testid="commercial-quotation-risk-assessments-filter-customer"
        >
          <option value="">Tüm müşteriler</option>
          {customerOptions.map(([customerReference, customerLabel]) => (
            <option key={customerReference} value={customerReference}>{customerLabel}</option>
          ))}
        </select>
      </label>
      <label className="commint-filter-rail__field">
        <span className="commint-filter-rail__label">Malzeme</span>
        <select
          value={filters.materialId}
          onChange={(event) => onFilterChange({ assessmentMaterial: event.target.value })}
          aria-label="Malzeme filtresi"
          data-testid="commercial-quotation-risk-assessments-filter-material"
        >
          <option value="">Tüm malzemeler</option>
          {materialOptions.map(([materialId, materialLabel]) => (
            <option key={materialId} value={materialId}>{materialLabel}</option>
          ))}
        </select>
      </label>
      <label className="commint-filter-rail__field">
        <span className="commint-filter-rail__label">Sürücü ailesi</span>
        <select
          value={filters.driverFamily}
          onChange={(event) => onFilterChange({ assessmentDriverFamily: event.target.value })}
          aria-label="Sürücü ailesi filtresi"
          data-testid="commercial-quotation-risk-assessments-filter-driver-family"
        >
          <option value="">Tüm sürücü aileleri</option>
          {driverFamilyOptions.map((option) => (
            <option key={option} value={option}>{option}</option>
          ))}
        </select>
      </label>
      <label className="commint-filter-rail__field">
        <span className="commint-filter-rail__label">Başlangıç</span>
        <input
          type="date"
          value={filters.dateFrom}
          onChange={(event) => onFilterChange({ assessmentFrom: event.target.value })}
          aria-label="Başlangıç tarihi"
          data-testid="commercial-quotation-risk-assessments-filter-date-from"
        />
      </label>
      <label className="commint-filter-rail__field">
        <span className="commint-filter-rail__label">Bitiş</span>
        <input
          type="date"
          value={filters.dateTo}
          onChange={(event) => onFilterChange({ assessmentTo: event.target.value })}
          aria-label="Bitiş tarihi"
          data-testid="commercial-quotation-risk-assessments-filter-date-to"
        />
      </label>
      <label className="commint-filter-rail__field commint-filter-rail__field--wide">
        <span className="commint-filter-rail__label">Arama</span>
        <input
          type="search"
          value={filters.search}
          onChange={(event) => onFilterChange({ assessmentSearch: event.target.value })}
          placeholder="Referans, teklif, müşteri veya malzeme"
          aria-label="Değerlendirme araması"
          data-testid="commercial-quotation-risk-assessments-filter-search"
        />
      </label>
      <button
        type="button"
        className="commint-button commint-filter-rail__clear"
        onClick={onClearFilters}
        disabled={!hasActiveFilters}
        data-testid="commercial-quotation-risk-assessments-filter-clear"
      >
        Filtreleri temizle
      </button>
    </div>
  );
}

function DetailField({ label, value }: Readonly<{ label: string; value: string }>) {
  return (
    <div className="commint-detail-field">
      <span className="commint-detail-field__label">{label}</span>
      <span className="commint-detail-field__value">{value}</span>
    </div>
  );
}

function DetailSection({
  title,
  testId,
  children,
}: Readonly<{ title: string; testId: string; children: ReactNode }>) {
  return (
    <section className="commint-detail-section" data-drawer-section={testId}>
      <h3>{title}</h3>
      {children}
    </section>
  );
}

type QuotationRiskAssessmentDetailDrawerBodyProps = Readonly<{
  assessmentDetail: CommercialQuotationRiskAssessmentDetail;
}>;

export function QuotationRiskAssessmentDetailDrawerBody({
  assessmentDetail,
}: QuotationRiskAssessmentDetailDrawerBodyProps) {
  const { summary } = assessmentDetail;
  const leadingDriver = assessmentDetail.drivers.find((driver) => driver.isLeadingDriver);
  const supportingDrivers = assessmentDetail.drivers
    .filter((driver) => !driver.isLeadingDriver)
    .sort((left, right) => left.sortOrder - right.sortOrder)
    .slice(0, 5);
  const candidateCount = summary.candidateObservationCount;
  const comparableCount = summary.comparableObservationCount;
  const excludedCount = summary.excludedObservationCount;

  return (
    <div data-testid="commercial-quotation-risk-assessment-detail-ready">
      <DetailSection title="Kimlik ve kapsam" testId="identity">
        <div className="commint-detail-grid">
          <DetailField label="Referans" value={summary.assessmentReference} />
          <DetailField label="Teklif referansı" value={summary.quotationReference} />
          <DetailField label="Profil" value={summary.profileCode} />
          <DetailField label="Malzeme" value={summary.materialLabel} />
          <DetailField label="Müşteri" value={`${summary.customerLabel} (${summary.customerReference})`} />
        </div>
      </DetailSection>
      <DetailSection title="Teklif bağlamı" testId="quotation-context">
        <div className="commint-detail-grid">
          <DetailField label="Para birimi" value={summary.currencyCode || "—"} />
          <DetailField label="Birim" value={summary.unitOfMeasure || "—"} />
          <DetailField label="Teklif fiyatı" value={summary.quotedPriceLabel} />
          <DetailField label="Referans fiyat" value={summary.referencePriceLabel} />
          <DetailField label="Sapma" value={summary.varianceLabel} />
          <DetailField label="Ödeme vadesi" value={`${formatCount(summary.paymentTermDays)} gün`} />
          <DetailField label="Kalan geçerlilik" value={summary.remainingValidityLabel} />
          <DetailField label="Referans penceresi" value={summary.windowLabel} />
        </div>
      </DetailSection>
      <DetailSection title="Fiyat bazı" testId="price-basis">
        <div className="commint-detail-grid">
          <DetailField label="Brüt/net" value={assessmentDetail.grossNetBasisLabel} />
          <DetailField label="Vergi bazı" value={assessmentDetail.taxBasisLabel} />
          <DetailField label="İskonto bazı" value={assessmentDetail.discountBasisLabel} />
          <DetailField label="Navlun bazı" value={assessmentDetail.freightBasisLabel} />
        </div>
      </DetailSection>
      <DetailSection title="Kanıt ve sonuç" testId="evidence-outcome">
        <div className="commint-detail-grid">
          <DetailField label="Aday" value={formatCount(candidateCount)} />
          <DetailField label="Karşılaştırılabilir" value={formatCount(comparableCount)} />
          <DetailField label="Hariç tutulan" value={formatCount(excludedCount)} />
          <DetailField
            label="Payda doğrulaması"
            value={`${formatCount(candidateCount)} = ${formatCount(comparableCount)} + ${formatCount(excludedCount)}`}
          />
          <DetailField label="Sonuç" value={summary.outcomeLabel} />
          <DetailField label="Kanıt durumu" value={summary.evidenceLabel} />
        </div>
      </DetailSection>
      <DetailSection title="Sürücüler" testId="drivers">
        <DetailField label="Önde gelen sürücü" value={leadingDriver?.reasonLabel ?? summary.leadingDriverLabel ?? "—"} />
        {supportingDrivers.length > 0 ? (
          <ul className="commint-driver-list" data-testid="commercial-quotation-risk-assessment-supporting-drivers">
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
          <DetailField label="Kanıt kaynağı" value={summary.evidenceLabel} />
          <DetailField label="Köken" value={assessmentDetail.sourceProvenance || "—"} />
          <DetailField label="Kural seti" value={summary.ruleSetVersion} />
          <DetailField label="Değerlendirme" value={summary.evaluatedLabel} />
        </div>
      </DetailSection>
    </div>
  );
}

type QuotationRiskComparisonDetailPanelProps = Readonly<{
  comparisonDetail: CommercialQuotationRiskComparisonDetail;
}>;

export function QuotationRiskComparisonDetailPanel({
  comparisonDetail,
}: QuotationRiskComparisonDetailPanelProps) {
  const { summary } = comparisonDetail;
  const leadingDriver = comparisonDetail.drivers.find((driver) => driver.isLeadingDriver);
  const supportingDrivers = comparisonDetail.drivers
    .filter((driver) => !driver.isLeadingDriver)
    .sort((left, right) => left.sortOrder - right.sortOrder)
    .slice(0, 5);

  const referenceCandidate = comparisonDetail.referenceCandidateAssessmentCount;
  const referenceComparable = comparisonDetail.referenceComparableAssessmentCount;
  const referenceExcluded = comparisonDetail.referenceExcludedAssessmentCount;
  const currentCandidate = comparisonDetail.currentCandidateAssessmentCount;
  const currentComparable = comparisonDetail.currentComparableAssessmentCount;
  const currentExcluded = comparisonDetail.currentExcludedAssessmentCount;

  return (
    <div data-testid="commercial-quotation-risk-comparison-detail-ready">
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
      <DetailSection title="Risk sıklığı" testId="incidence">
        <div className="commint-detail-grid">
          <DetailField label="Referans risk sıklığı" value={summary.referenceIncidenceLabel} />
          <DetailField label="Güncel risk sıklığı" value={summary.currentIncidenceLabel} />
          <DetailField
            label="Risk sıklığı değişimi"
            value={summary.incidenceDeltaLabel === "Hesaplanamadı"
              ? "Hesaplanamadı"
              : `Risk sıklığı değişimi: ${formatIncidenceDeltaPoints(summary.riskIncidenceDelta)}`}
          />
          <DetailField label="Sonuç" value={summary.outcomeLabel} />
          <DetailField label="Kanıt durumu" value={summary.evidenceLabel} />
        </div>
      </DetailSection>
      <DetailSection title="Pencere sayımları" testId="window-counts">
        <div className="commint-detail-grid">
          <DetailField
            label="Referans aday"
            value={referenceCandidate === null ? "Hesaplanamadı" : formatCount(referenceCandidate)}
          />
          <DetailField
            label="Referans karşılaştırılabilir"
            value={referenceComparable === null ? "Hesaplanamadı" : formatCount(referenceComparable)}
          />
          <DetailField
            label="Referans hariç tutulan"
            value={referenceExcluded === null ? "Hesaplanamadı" : formatCount(referenceExcluded)}
          />
          <DetailField
            label="Güncel aday"
            value={currentCandidate === null ? "Hesaplanamadı" : formatCount(currentCandidate)}
          />
          <DetailField
            label="Güncel karşılaştırılabilir"
            value={currentComparable === null ? "Hesaplanamadı" : formatCount(currentComparable)}
          />
          <DetailField
            label="Güncel hariç tutulan"
            value={currentExcluded === null ? "Hesaplanamadı" : formatCount(currentExcluded)}
          />
          <DetailField label="Risk taşıyan" value={formatCount(comparisonDetail.riskBearingAssessmentCount)} />
          <DetailField label="Fiyat riski" value={formatCount(comparisonDetail.priceRiskCount)} />
          <DetailField label="Geçerlilik riski" value={formatCount(comparisonDetail.expiryRiskCount)} />
          <DetailField label="Ödeme vadesi riski" value={formatCount(comparisonDetail.paymentTermRiskCount)} />
          <DetailField label="Yetersiz veri" value={formatCount(comparisonDetail.insufficientDataCount)} />
          <DetailField label="Kaynak çakışması" value={formatCount(comparisonDetail.sourceConflictCount)} />
        </div>
      </DetailSection>
      <DetailSection title="Sürücüler" testId="drivers">
        <DetailField label="Önde gelen sürücü" value={leadingDriver?.reasonLabel ?? summary.leadingDriverLabel ?? "—"} />
        {supportingDrivers.length > 0 ? (
          <ul className="commint-driver-list" data-testid="commercial-quotation-risk-comparison-supporting-drivers">
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

export function buildAssessmentTablePrimaryLabel(
  assessment: CommercialQuotationRiskAssessmentSummary,
): string {
  return buildQuotationRiskAssessmentPrimaryLabel(assessment);
}

export function buildAssessmentTableSecondaryLabel(
  assessment: CommercialQuotationRiskAssessmentSummary,
): string {
  return buildQuotationRiskAssessmentSecondaryLabel(assessment);
}

export function formatAssessmentEvidenceLabel(
  assessment: CommercialQuotationRiskAssessmentSummary,
): string {
  return `${formatCount(assessment.comparableObservationCount)} / ${formatCount(assessment.excludedObservationCount)}`;
}

export function formatAssessmentQuotedPrice(
  assessment: CommercialQuotationRiskAssessmentSummary,
): string {
  return formatCurrency(assessment.quotedComparableUnitPrice, assessment.currencyCode);
}

export function formatAssessmentRemainingValidity(
  assessment: CommercialQuotationRiskAssessmentSummary,
): string {
  return assessment.remainingValidityDays !== null
    ? formatRemainingValidityDays(assessment.remainingValidityDays)
    : "Belirtilmemiş";
}

export function formatDriverVarianceLabel(
  normalizedVariance: number | null,
): string {
  return formatRatioAsPercent(normalizedVariance);
}
