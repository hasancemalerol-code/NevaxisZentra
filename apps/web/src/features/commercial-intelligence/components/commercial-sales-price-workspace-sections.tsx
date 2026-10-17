"use client";

import { useEffect, useMemo, useState, type ReactNode } from "react";

import {
  COMMERCIAL_SALES_PRICE_TEMPORAL_TYPE,
} from "./commercial-intelligence-presentation";
import { CommercialIntelligenceCustomerRow } from "./commercial-intelligence-customer-row";
import { formatCount, formatCurrency } from "../domain/commercial-intelligence-formatters";
import type {
  CommercialSalesPriceAssessmentDetail,
  CommercialSalesPriceComparisonDetail,
  CommercialSalesPriceWorkspacePayload,
} from "../types/commercial-intelligence-types";
import {
  loadCommercialSalesPriceAssessmentDetailAction,
  loadCommercialSalesPriceComparisonDetailAction,
} from "../server/commercial-intelligence-actions";
import { detailValueClass } from "./commercial-no-data";

export type SalesPriceHistoryResultType = "" | "assessment" | "temporal" | "customer";

export type SalesPriceHistoryFilters = Readonly<{
  resultType: SalesPriceHistoryResultType;
  outcome: string;
  materialId: string;
  customer: string;
  evidence: string;
  dateFrom: string;
  dateTo: string;
}>;

export type SalesPriceHistoryRow = Readonly<{
  kind: "assessment" | "comparison";
  resultTypeKey: "assessment" | "temporal" | "customer";
  resultTypeLabel: string;
  id: string;
  reference: string;
  primaryLabel: string;
  materialLabel: string;
  materialId: string;
  customerReference: string | null;
  customerLabel: string;
  outcomeLabel: string;
  outcome: string;
  varianceLabel: string;
  evidenceLabel: string;
  evidenceStatus: string;
  evidenceStatusLabel: string;
  evaluatedLabel: string;
  evaluatedAtUtc: string;
  profileCode: string;
}>;

const HISTORY_FILTER_KEYS = [
  "historyType",
  "historyOutcome",
  "historyMaterial",
  "historyCustomer",
  "historyEvidence",
  "historyFrom",
  "historyTo",
] as const;

export function readSalesPriceHistoryFilters(
  searchParams: URLSearchParams,
): SalesPriceHistoryFilters {
  const resultType = searchParams.get("historyType") ?? "";
  const normalizedResultType = (
    resultType === "assessment"
    || resultType === "temporal"
    || resultType === "customer"
  ) ? resultType : "" as SalesPriceHistoryResultType;

  return {
    resultType: normalizedResultType,
    outcome: searchParams.get("historyOutcome") ?? "",
    materialId: searchParams.get("historyMaterial") ?? "",
    customer: searchParams.get("historyCustomer") ?? "",
    evidence: searchParams.get("historyEvidence") ?? "",
    dateFrom: searchParams.get("historyFrom") ?? "",
    dateTo: searchParams.get("historyTo") ?? "",
  };
}

export function buildSalesPriceHistoryFilterParams(
  current: URLSearchParams,
  updates: Partial<Record<(typeof HISTORY_FILTER_KEYS)[number], string>>,
): URLSearchParams {
  const params = new URLSearchParams(current.toString());
  params.set("tab", "history");
  for (const [key, value] of Object.entries(updates)) {
    if (!value) params.delete(key);
    else params.set(key, value);
  }
  return params;
}

export function clearSalesPriceHistoryFilterParams(current: URLSearchParams): URLSearchParams {
  const params = new URLSearchParams(current.toString());
  params.set("tab", "history");
  for (const key of HISTORY_FILTER_KEYS) params.delete(key);
  return params;
}

export function buildSalesPriceHistoryRows(
  workspace: CommercialSalesPriceWorkspacePayload,
): readonly SalesPriceHistoryRow[] {
  const assessmentRows: SalesPriceHistoryRow[] = workspace.assessments.map((item) => ({
    kind: "assessment",
    resultTypeKey: "assessment",
    resultTypeLabel: "Fiyat analizi",
    id: item.id,
    reference: item.assessmentReference,
    primaryLabel: item.comparisonDimensionLabel,
    materialLabel: item.materialLabel,
    materialId: item.materialId,
    customerReference: item.customerReference,
    customerLabel: item.customerLabel,
    outcomeLabel: item.outcomeLabel,
    outcome: item.assessmentOutcome,
    varianceLabel: item.varianceLabel,
    evidenceLabel: `${formatCount(item.comparableObservationCount)} / ${formatCount(item.excludedObservationCount)}`,
    evidenceStatus: item.evidenceStatus,
    evidenceStatusLabel: item.evidenceLabel,
    evaluatedLabel: item.evaluatedLabel,
    evaluatedAtUtc: item.evaluatedAtUtc,
    profileCode: item.profileCode,
  }));

  const comparisonRows: SalesPriceHistoryRow[] = workspace.comparisons.map((item) => ({
    kind: "comparison",
    resultTypeKey: item.comparisonType === COMMERCIAL_SALES_PRICE_TEMPORAL_TYPE ? "temporal" : "customer",
    resultTypeLabel: item.comparisonTypeLabel,
    id: item.id,
    reference: item.comparisonReference,
    primaryLabel: item.comparisonTypeLabel,
    materialLabel: item.materialLabel,
    materialId: item.materialId,
    customerReference: item.customerReference,
    customerLabel: item.customerLabel,
    outcomeLabel: item.outcomeLabel,
    outcome: item.comparisonOutcome,
    varianceLabel: item.comparisonType === COMMERCIAL_SALES_PRICE_TEMPORAL_TYPE
      ? item.signedVarianceLabel
      : item.customerSpreadLabel,
    evidenceLabel: `${formatCount(item.comparableObservationCount)} / ${formatCount(item.excludedObservationCount)}`,
    evidenceStatus: item.evidenceStatus,
    evidenceStatusLabel: item.evidenceLabel,
    evaluatedLabel: item.evaluatedLabel,
    evaluatedAtUtc: item.evaluatedAtUtc,
    profileCode: item.profileCode,
  }));

  return [...assessmentRows, ...comparisonRows]
    .sort((left, right) => Date.parse(right.evaluatedAtUtc) - Date.parse(left.evaluatedAtUtc));
}

export function filterSalesPriceHistoryRows(
  rows: readonly SalesPriceHistoryRow[],
  filters: SalesPriceHistoryFilters,
): readonly SalesPriceHistoryRow[] {
  const fromMs = filters.dateFrom ? Date.parse(`${filters.dateFrom}T00:00:00.000Z`) : null;
  const toMs = filters.dateTo ? Date.parse(`${filters.dateTo}T23:59:59.999Z`) : null;

  return rows.filter((item) => {
    if (filters.resultType && item.resultTypeKey !== filters.resultType) return false;
    if (filters.outcome && item.outcome !== filters.outcome) return false;
    if (filters.materialId && item.materialId !== filters.materialId) return false;
    if (filters.customer && item.customerReference !== filters.customer) return false;
    if (filters.evidence && item.evidenceStatus !== filters.evidence) return false;

    const evaluatedMs = Date.parse(item.evaluatedAtUtc);
    if (fromMs !== null && Number.isFinite(fromMs) && evaluatedMs < fromMs) return false;
    if (toMs !== null && Number.isFinite(toMs) && evaluatedMs > toMs) return false;
    return true;
  });
}

type SalesPriceHistoryFilterRailProps = Readonly<{
  workspace: CommercialSalesPriceWorkspacePayload;
  filters: SalesPriceHistoryFilters;
  onFilterChange: (updates: Partial<Record<(typeof HISTORY_FILTER_KEYS)[number], string>>) => void;
  onClearFilters: () => void;
}>;

export function SalesPriceHistoryFilterRail({
  workspace,
  filters,
  onFilterChange,
  onClearFilters,
}: SalesPriceHistoryFilterRailProps) {
  const materialOptions = useMemo(() => {
    const map = new Map<string, string>();
    for (const item of workspace.assessments) map.set(item.materialId, item.materialLabel);
    for (const item of workspace.comparisons) map.set(item.materialId, item.materialLabel);
    return [...map.entries()].sort((left, right) => left[1].localeCompare(right[1], "tr"));
  }, [workspace.assessments, workspace.comparisons]);

  const customerOptions = useMemo(() => {
    const map = new Map<string, string>();
    for (const item of workspace.assessments) {
      if (item.customerReference) map.set(item.customerReference, item.customerLabel);
    }
    for (const item of workspace.comparisons) {
      if (item.customerReference) map.set(item.customerReference, item.customerLabel);
    }
    return [...map.entries()].sort((left, right) => left[1].localeCompare(right[1], "tr"));
  }, [workspace.assessments, workspace.comparisons]);

  const evidenceOptions = (() => {
    const values = new Set<string>();
    for (const item of workspace.assessments) values.add(item.evidenceStatus);
    for (const item of workspace.comparisons) values.add(item.evidenceStatus);
    return [...values].map((value) => {
      const label = workspace.assessments.find((item) => item.evidenceStatus === value)?.evidenceLabel
        ?? workspace.comparisons.find((item) => item.evidenceStatus === value)?.evidenceLabel
        ?? value;
      return { value, label };
    });
  })();

  const hasActiveFilters = Boolean(
    filters.resultType
    || filters.outcome
    || filters.materialId
    || filters.customer
    || filters.evidence
    || filters.dateFrom
    || filters.dateTo,
  );

  return (
    <div
      className="commint-filter-rail"
      data-testid="commercial-sales-price-history-filter-rail"
      aria-label="Geçmiş filtreleri"
    >
      <label className="commint-filter-rail__field">
        <span className="commint-filter-rail__label">Sonuç türü</span>
        <select
          value={filters.resultType}
          onChange={(event) => onFilterChange({ historyType: event.target.value })}
          aria-label="Sonuç türü filtresi"
          data-testid="commercial-sales-price-history-filter-result-type"
        >
          <option value="">Tüm kayıtlar</option>
          <option value="assessment">Fiyat analizi</option>
          <option value="temporal">Dönemsel sapma</option>
          <option value="customer">Müşteri karşılaştırması</option>
        </select>
      </label>
      <label className="commint-filter-rail__field">
        <span className="commint-filter-rail__label">Sonuç</span>
        <select
          value={filters.outcome}
          onChange={(event) => onFilterChange({ historyOutcome: event.target.value })}
          aria-label="Sonuç filtresi"
          data-testid="commercial-sales-price-history-filter-outcome"
        >
          <option value="">Tüm sonuçlar</option>
          <option value="attention">Dikkat gerektirir</option>
          <option value="positive-price-variance">Pozitif fiyat sapması</option>
          <option value="within-reference-range">Referans aralığında</option>
          <option value="insufficient-data">Veri yetersiz</option>
          <option value="source-conflict">Kaynak uyuşmazlığı</option>
        </select>
      </label>
      <label className="commint-filter-rail__field">
        <span className="commint-filter-rail__label">Malzeme</span>
        <select
          value={filters.materialId}
          onChange={(event) => onFilterChange({ historyMaterial: event.target.value })}
          aria-label="Malzeme filtresi"
          data-testid="commercial-sales-price-history-filter-material"
        >
          <option value="">Tüm malzemeler</option>
          {materialOptions.map(([materialId, materialLabel]) => (
            <option key={materialId} value={materialId}>{materialLabel}</option>
          ))}
        </select>
      </label>
      <label className="commint-filter-rail__field">
        <span className="commint-filter-rail__label">Müşteri</span>
        <select
          value={filters.customer}
          onChange={(event) => onFilterChange({ historyCustomer: event.target.value })}
          aria-label="Müşteri filtresi"
          data-testid="commercial-sales-price-history-filter-customer"
        >
          <option value="">Tüm müşteriler</option>
          {customerOptions.map(([customerReference, customerLabel]) => (
            <option key={customerReference} value={customerReference}>{customerLabel}</option>
          ))}
        </select>
      </label>
      <label className="commint-filter-rail__field">
        <span className="commint-filter-rail__label">Kanıt durumu</span>
        <select
          value={filters.evidence}
          onChange={(event) => onFilterChange({ historyEvidence: event.target.value })}
          aria-label="Kanıt durumu filtresi"
          data-testid="commercial-sales-price-history-filter-evidence"
        >
          <option value="">Tüm kanıt durumları</option>
          {evidenceOptions.map((option) => (
            <option key={option.value} value={option.value}>{option.label}</option>
          ))}
        </select>
      </label>
      <label className="commint-filter-rail__field">
        <span className="commint-filter-rail__label">Başlangıç</span>
        <input
          type="date"
          value={filters.dateFrom}
          onChange={(event) => onFilterChange({ historyFrom: event.target.value })}
          aria-label="Başlangıç tarihi"
          data-testid="commercial-sales-price-history-filter-date-from"
        />
      </label>
      <label className="commint-filter-rail__field">
        <span className="commint-filter-rail__label">Bitiş</span>
        <input
          type="date"
          value={filters.dateTo}
          onChange={(event) => onFilterChange({ historyTo: event.target.value })}
          aria-label="Bitiş tarihi"
          data-testid="commercial-sales-price-history-filter-date-to"
        />
      </label>
      <button
        type="button"
        className="commint-button commint-filter-rail__clear"
        onClick={onClearFilters}
        disabled={!hasActiveFilters}
        data-testid="commercial-sales-price-history-filter-clear"
      >
        Filtreleri temizle
      </button>
    </div>
  );
}

type ExecutiveRecord = Readonly<{
  kind: "assessment" | "comparison";
  id: string;
  reference: string;
  resultTypeLabel: string;
  outcomeLabel: string;
  outcomeKey: string;
  leadingDriverLabel: string;
  comparableObservationCount: number;
  excludedObservationCount: number;
  evidenceLabel: string;
  provenanceDisclaimer: string;
}>;

type SalesPriceExecutiveDriverSummaryProps = Readonly<{
  workspace: CommercialSalesPriceWorkspacePayload;
  onOpenDetail: (kind: "assessment" | "comparison", id: string) => void;
}>;

function SalesPriceExecutiveDriverSummaryLoaded({
  record,
  workspace,
  onOpenDetail,
}: Readonly<{
  record: ExecutiveRecord;
  workspace: CommercialSalesPriceWorkspacePayload;
  onOpenDetail: (kind: "assessment" | "comparison", id: string) => void;
}>) {
  const [leadingDriver, setLeadingDriver] = useState(record.leadingDriverLabel);
  const [supportingDrivers, setSupportingDrivers] = useState<readonly string[]>([]);
  const [driverStatus, setDriverStatus] = useState<"loading" | "ready">("loading");

  useEffect(() => {
    let cancelled = false;

    void (async () => {
      if (record.kind === "assessment") {
        const result = await loadCommercialSalesPriceAssessmentDetailAction(record.id);
        if (cancelled) return;
        if (result.status === "failure") {
          setLeadingDriver(record.leadingDriverLabel);
          setSupportingDrivers([]);
          setDriverStatus("ready");
          return;
        }
        const leading = result.detail.drivers.find((driver) => driver.isLeadingDriver);
        const supporting = result.detail.drivers
          .filter((driver) => !driver.isLeadingDriver)
          .slice(0, 3)
          .map((driver) => driver.reasonLabel);
        setLeadingDriver(leading?.reasonLabel ?? record.leadingDriverLabel);
        setSupportingDrivers(supporting);
        setDriverStatus("ready");
        return;
      }

      const result = await loadCommercialSalesPriceComparisonDetailAction(record.id);
      if (cancelled) return;
      if (result.status === "failure") {
        setLeadingDriver(record.leadingDriverLabel);
        setSupportingDrivers([]);
        setDriverStatus("ready");
        return;
      }
      const leading = result.detail.drivers.find((driver) => driver.isLeadingDriver);
      const supporting = result.detail.drivers
        .filter((driver) => !driver.isLeadingDriver)
        .slice(0, 3)
        .map((driver) => driver.reasonLabel);
      setLeadingDriver(leading?.reasonLabel ?? record.leadingDriverLabel);
      setSupportingDrivers(supporting);
      setDriverStatus("ready");
    })();

    return () => {
      cancelled = true;
    };
  }, [record]);

  return (
    <section
      className="commint-panel commint-panel--driver-summary"
      data-testid="commercial-sales-price-executive-driver-summary"
    >
      <div className="commint-driver-summary__header">
        <div>
          <h2 className="commint-panel__title">Yönetici etken özeti</h2>
          <p className="commint-panel__trace">{record.reference}</p>
        </div>
        <span className={`commint-badge commint-badge--${resolveOutcomeBadgeClass(record.outcomeKey)}`}>
          {record.outcomeLabel}
        </span>
      </div>
      <div className="commint-driver-summary__grid">
        <div className="commint-driver-summary__item">
          <span className="commint-detail-field__label">Sonuç türü</span>
          <span className="commint-driver-summary__value">{record.resultTypeLabel}</span>
        </div>
        <div className="commint-driver-summary__item">
          <span className="commint-detail-field__label">Karşılaştırılan / hariç kayıt</span>
          <span className="commint-driver-summary__value">
            {formatCount(record.comparableObservationCount)} / {formatCount(record.excludedObservationCount)}
          </span>
        </div>
        <div className="commint-driver-summary__item">
          <span className="commint-detail-field__label">Kanıt durumu</span>
          <span className="commint-driver-summary__value">{record.evidenceLabel}</span>
        </div>
      </div>
      <div className="commint-driver-summary__drivers" data-testid="commercial-sales-price-leading-driver">
        <p className="commint-driver-summary__leading-label">Başlıca etken</p>
        <p
          className="commint-driver-summary__leading-value"
          data-testid={driverStatus === "ready" ? "commercial-sales-price-executive-driver-ready" : undefined}
        >
          {driverStatus === "loading" ? "Yükleniyor…" : leadingDriver}
        </p>
        {supportingDrivers.length > 0 ? (
          <div data-testid="commercial-sales-price-supporting-drivers">
            <p className="commint-driver-summary__supporting-label">Destekleyici sürücüler</p>
            <ul className="commint-driver-list">
              {supportingDrivers.map((driver) => (
                <li key={driver}>{driver}</li>
              ))}
            </ul>
          </div>
        ) : driverStatus === "ready" ? (
          <p className="commint-supplier-row__meta">Destekleyen başka etken yok</p>
        ) : null}
      </div>
      <p className="commint-supplier-row__meta">{workspace.provenanceDisclaimer}</p>
      <button
        type="button"
        className="commint-button"
        onClick={() => onOpenDetail(record.kind, record.id)}
        data-testid="commercial-sales-price-executive-detail-link"
      >
        Kayıt detayını aç
      </button>
    </section>
  );
}

export function SalesPriceExecutiveDriverSummary({
  workspace,
  onOpenDetail,
}: SalesPriceExecutiveDriverSummaryProps) {
  const latestRecord = useMemo<ExecutiveRecord | null>(() => {
    const rows = buildSalesPriceHistoryRows(workspace);
    const latest = rows[0];
    if (!latest) return null;
    return {
      kind: latest.kind,
      id: latest.id,
      reference: latest.reference,
      resultTypeLabel: latest.resultTypeLabel,
      outcomeLabel: latest.outcomeLabel,
      outcomeKey: latest.outcome,
      leadingDriverLabel: latest.kind === "assessment"
        ? workspace.assessments.find((item) => item.id === latest.id)?.leadingDriverLabel ?? "—"
        : workspace.comparisons.find((item) => item.id === latest.id)?.leadingDriverLabel ?? "—",
      comparableObservationCount: latest.kind === "assessment"
        ? workspace.assessments.find((item) => item.id === latest.id)?.comparableObservationCount ?? 0
        : workspace.comparisons.find((item) => item.id === latest.id)?.comparableObservationCount ?? 0,
      excludedObservationCount: latest.kind === "assessment"
        ? workspace.assessments.find((item) => item.id === latest.id)?.excludedObservationCount ?? 0
        : workspace.comparisons.find((item) => item.id === latest.id)?.excludedObservationCount ?? 0,
      evidenceLabel: latest.evidenceStatusLabel,
      provenanceDisclaimer: workspace.provenanceDisclaimer,
    };
  }, [workspace]);

  if (!latestRecord) {
    return (
      <section
        className="commint-panel commint-panel--driver-summary"
        data-testid="commercial-sales-price-executive-driver-summary"
      >
        <h2 className="commint-panel__title">Yönetici etken özeti</h2>
        <p className="commint-empty">Henüz değerlendirme kaydı yok</p>
      </section>
    );
  }

  return (
    <SalesPriceExecutiveDriverSummaryLoaded
      key={`${latestRecord.kind}-${latestRecord.id}`}
      record={latestRecord}
      workspace={workspace}
      onOpenDetail={onOpenDetail}
    />
  );
}

function resolveOutcomeBadgeClass(outcome: string): string {
  if (outcome === "attention") return "attention";
  if (outcome === "positive-price-variance") return "favorable";
  if (outcome === "source-conflict" || outcome === "insufficient-data") return "conflict";
  return "tie";
}

function DetailField({ label, value }: Readonly<{ label: string; value: string }>) {
  return (
    <div className="commint-detail-field">
      <span className="commint-detail-field__label">{label}</span>
      <span className={detailValueClass(value)}>{value}</span>
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

type SalesPriceDetailDrawerBodyProps = Readonly<{
  detailKind: "comparison" | "assessment";
  comparisonDetail: CommercialSalesPriceComparisonDetail | null;
  assessmentDetail: CommercialSalesPriceAssessmentDetail | null;
}>;

export function SalesPriceDetailDrawerBody({
  detailKind,
  comparisonDetail,
  assessmentDetail,
}: SalesPriceDetailDrawerBodyProps) {
  if (detailKind === "comparison" && comparisonDetail) {
    const { summary } = comparisonDetail;
    const isTemporal = summary.comparisonType === COMMERCIAL_SALES_PRICE_TEMPORAL_TYPE;
    const leadingDriver = comparisonDetail.drivers.find((driver) => driver.isLeadingDriver);
    const supportingDrivers = comparisonDetail.drivers.filter((driver) => !driver.isLeadingDriver).slice(0, 3);
    const baselineCustomer = comparisonDetail.customerResults.find(
      (row) => row.positionState === "lowest-comparable-price" || row.positionState === "tied-comparable-price",
    );

    return (
      <div data-testid="commercial-sales-price-detail-ready">
        <DetailSection title="Kimlik ve kapsam" testId="identity">
          <div className="commint-detail-grid">
            <DetailField label="Referans" value={summary.comparisonReference} />
            <DetailField label="Sonuç türü" value={summary.comparisonTypeLabel} />
            <DetailField label="Profil" value={summary.profileCode} />
            <DetailField label="Malzeme" value={summary.materialLabel} />
            <DetailField label="Müşteri kapsamı" value={summary.customerLabel || "Tüm müşteriler"} />
          </div>
        </DetailSection>
        <DetailSection title="Fiyat bağlamı" testId="price-context">
          <div className="commint-detail-grid">
            <DetailField label="Para birimi" value={summary.currencyCode || "—"} />
            <DetailField label="Birim" value={summary.normalizedUnit || "—"} />
            <DetailField label="Brüt/net" value={comparisonDetail.priceBasisGrossNetLabel} />
            <DetailField label="Vergi bazı" value={comparisonDetail.priceBasisTaxLabel} />
            <DetailField label="İskonto bazı" value={comparisonDetail.priceBasisDiscountLabel} />
            <DetailField label="Navlun bazı" value={comparisonDetail.priceBasisFreightLabel} />
          </div>
        </DetailSection>
        <DetailSection title="Pencere ve medyan" testId="windows-medians">
          <div className="commint-detail-grid">
            <DetailField label="Referans penceresi" value={summary.referenceWindowLabel || "—"} />
            <DetailField label="Güncel pencere" value={summary.currentWindowLabel} />
            <DetailField
              label="Referans medyan"
              value={summary.referenceMedianUnitPrice !== null
                ? formatCurrency(summary.referenceMedianUnitPrice, summary.currencyCode)
                : "—"}
            />
            <DetailField
              label="Güncel medyan"
              value={summary.currentMedianUnitPrice !== null
                ? formatCurrency(summary.currentMedianUnitPrice, summary.currencyCode)
                : "—"}
            />
            <DetailField
              label="İmzalı sapma"
              value={isTemporal ? summary.signedVarianceLabel : "—"}
            />
            <DetailField
              label="Müşteri yayılımı"
              value={!isTemporal ? summary.customerSpreadLabel : "—"}
            />
          </div>
        </DetailSection>
        <DetailSection title="Kanıt ve sonuç" testId="evidence-outcome">
          <div className="commint-detail-grid">
            <DetailField label="Aday" value={formatCount(summary.candidateObservationCount)} />
            <DetailField label="Karşılaştırılabilir" value={formatCount(summary.comparableObservationCount)} />
            <DetailField label="Hariç tutulan" value={formatCount(summary.excludedObservationCount)} />
            <DetailField label="Sonuç" value={summary.outcomeLabel} />
            <DetailField
              label="Konum durumu"
              value={baselineCustomer?.positionStateLabel ?? "—"}
            />
            <DetailField label="Kanıt durumu" value={summary.evidenceLabel} />
          </div>
        </DetailSection>
        <DetailSection title="Sürücüler" testId="drivers">
          <DetailField label="Başlıca etken" value={leadingDriver?.reasonLabel ?? summary.leadingDriverLabel ?? "—"} />
          {supportingDrivers.length > 0 ? (
            <ul className="commint-driver-list" data-testid="commercial-sales-price-detail-supporting-drivers">
              {supportingDrivers.map((driver) => (
                <li key={driver.id}>{driver.reasonLabel}</li>
              ))}
            </ul>
          ) : (
            <p className="commint-supplier-row__meta">Destekleyen başka etken yok</p>
          )}
        </DetailSection>
        <DetailSection title="Köken" testId="provenance">
          <div className="commint-detail-grid">
            <DetailField label="Kanıt kaynağı" value={comparisonDetail.sourceEvidenceLabel} />
            <DetailField label="Köken" value={comparisonDetail.sourceProvenance || "—"} />
            <DetailField label="Oluşturulma" value={summary.evaluatedLabel} />
          </div>
        </DetailSection>
        {comparisonDetail.customerResults.length > 0 ? (
          <DetailSection title="Müşteri konumları" testId="customer-positions">
            <div className="commint-supplier-list">
              {comparisonDetail.customerResults.map((row) => (
                <CommercialIntelligenceCustomerRow
                  key={row.id}
                  row={row}
                  currencyCode={summary.currencyCode}
                  normalizedUnit={summary.normalizedUnit}
                />
              ))}
            </div>
          </DetailSection>
        ) : null}
      </div>
    );
  }

  if (detailKind === "assessment" && assessmentDetail) {
    const { summary } = assessmentDetail;
    const leadingDriver = assessmentDetail.drivers.find((driver) => driver.isLeadingDriver);
    const supportingDrivers = assessmentDetail.drivers.filter((driver) => !driver.isLeadingDriver).slice(0, 3);

    return (
      <div data-testid="commercial-sales-price-detail-ready">
        <DetailSection title="Kimlik ve kapsam" testId="identity">
          <div className="commint-detail-grid">
            <DetailField label="Referans" value={summary.assessmentReference} />
            <DetailField label="Sonuç türü" value="Fiyat analizi" />
            <DetailField label="Profil" value={summary.profileCode} />
            <DetailField label="Malzeme" value={summary.materialLabel} />
            <DetailField label="Müşteri kapsamı" value={summary.customerLabel} />
          </div>
        </DetailSection>
        <DetailSection title="Fiyat bağlamı" testId="price-context">
          <div className="commint-detail-grid">
            <DetailField label="Para birimi" value={summary.currencyCode || "—"} />
            <DetailField label="Birim" value={summary.normalizedUnit || "—"} />
            <DetailField label="Brüt/net" value={assessmentDetail.priceBasisGrossNetLabel} />
            <DetailField label="Vergi bazı" value={assessmentDetail.priceBasisTaxLabel} />
            <DetailField label="İskonto bazı" value={assessmentDetail.priceBasisDiscountLabel} />
            <DetailField label="Navlun bazı" value={assessmentDetail.priceBasisFreightLabel} />
          </div>
        </DetailSection>
        <DetailSection title="Pencere ve medyan" testId="windows-medians">
          <div className="commint-detail-grid">
            <DetailField label="Güncel pencere" value={summary.windowLabel} />
            <DetailField
              label="Güncel birim fiyat"
              value={formatCurrency(summary.currentUnitPrice, summary.currencyCode)}
            />
            <DetailField
              label="Referans medyan"
              value={summary.referenceMedianUnitPrice !== null
                ? formatCurrency(summary.referenceMedianUnitPrice, summary.currencyCode)
                : "—"}
            />
            <DetailField label="İmzalı sapma" value={summary.varianceLabel} />
            <DetailField label="Müşteri yayılımı" value="—" />
          </div>
        </DetailSection>
        <DetailSection title="Kanıt ve sonuç" testId="evidence-outcome">
          <div className="commint-detail-grid">
            <DetailField label="Aday" value={formatCount(summary.candidateObservationCount)} />
            <DetailField label="Karşılaştırılabilir" value={formatCount(summary.comparableObservationCount)} />
            <DetailField label="Hariç tutulan" value={formatCount(summary.excludedObservationCount)} />
            <DetailField label="Sonuç" value={summary.outcomeLabel} />
            <DetailField label="Konum durumu" value="—" />
            <DetailField label="Kanıt durumu" value={summary.evidenceLabel} />
          </div>
        </DetailSection>
        <DetailSection title="Sürücüler" testId="drivers">
          <DetailField label="Başlıca etken" value={leadingDriver?.reasonLabel ?? summary.leadingDriverLabel ?? "—"} />
          {supportingDrivers.length > 0 ? (
            <ul className="commint-driver-list" data-testid="commercial-sales-price-detail-supporting-drivers">
              {supportingDrivers.map((driver) => (
                <li key={driver.id}>{driver.reasonLabel}</li>
              ))}
            </ul>
          ) : (
            <p className="commint-supplier-row__meta">Destekleyen başka etken yok</p>
          )}
        </DetailSection>
        <DetailSection title="Köken" testId="provenance">
          <div className="commint-detail-grid">
            <DetailField label="Kanıt kaynağı" value={summary.sourceEvidenceLabel} />
            <DetailField label="Köken" value={assessmentDetail.sourceProvenance || "—"} />
            <DetailField label="Oluşturulma" value={summary.evaluatedLabel} />
          </div>
        </DetailSection>
        {assessmentDetail.exclusionReasons.length > 0 ? (
          <DetailSection title="Dışlama nedenleri" testId="exclusions">
            <ul className="commint-driver-list">
              {assessmentDetail.exclusionReasons.map((reason) => (
                <li key={reason}>{reason}</li>
              ))}
            </ul>
          </DetailSection>
        ) : null}
      </div>
    );
  }

  return null;
}
