import Link from "next/link";

import type {
  OperationalValueBreakdownItemApi,
  OperationalValueCaseDetailApi,
  OperationalValueControlCenterApi,
  OperationalValueEconomicInputSnapshotApi,
} from "../server/operational-value-api-client";

import "../operational-value.css";

type OperationalValueControlCenterViewProps = Readonly<{
  controlCenter: OperationalValueControlCenterApi;
}>;

function formatNumber(value: number, fractionDigits = 0): string {
  return value.toLocaleString("tr-TR", {
    maximumFractionDigits: fractionDigits,
    minimumFractionDigits: fractionDigits,
  });
}

function formatCurrency(value: number, currency = "USD"): string {
  const symbol = currency === "TRY" ? "₺" : "$";
  return `${symbol}${formatNumber(value, 2)}`;
}

function formatMinutes(minutes: number): string {
  if (minutes >= 60) {
    const hours = minutes / 60;
    return `${formatNumber(hours, 1)} sa`;
  }
  return `${formatNumber(minutes, 0)} dk`;
}

function statusBadgeClass(status: string): string {
  const normalized = status.toUpperCase();
  if (normalized === "VERIFIED_OPERATIONAL" || normalized === "VERIFIED_ECONOMIC") {
    return "operational-value-badge--verified";
  }
  if (normalized === "OBSERVING" || normalized === "PREDICTED") {
    return "operational-value-badge--observing";
  }
  if (normalized === "CONVERTED") {
    return "operational-value-badge--economic";
  }
  return "operational-value-badge--unavailable";
}

function economicStatusLabel(status: string): string {
  switch (status.toUpperCase()) {
    case "NOT_CONFIGURED":
      return "Yapılandırılmadı";
    case "OPERATIONAL_ONLY":
      return "Yalnızca operasyonel";
    case "CONVERTED":
      return "Ekonomik dönüşüm";
    case "INSUFFICIENT_INPUTS":
      return "Yetersiz girdi";
    default:
      return status;
  }
}

function BreakdownList({
  items,
  emptyLabel,
}: Readonly<{ items: readonly OperationalValueBreakdownItemApi[]; emptyLabel: string }>) {
  if (items.length === 0) {
    return <p className="operational-value-item__meta">{emptyLabel}</p>;
  }

  return items.map((item) => (
    <div key={`${item.dimensionType}:${item.dimensionId}:${item.valueCategory}`} className="operational-value-item">
      <div className="operational-value-item__row">
        <div className="operational-value-item__title">{item.label}</div>
        <span className="operational-value-badge">{item.valueCategory}</span>
        <span className="operational-value-badge">{item.attributionClass}</span>
      </div>
      <div className="operational-value-item__meta">
        {formatNumber(item.operationalValue, 1)} {item.unit} · {item.caseCount} vaka
        {item.economicValue != null
          ? ` · ${formatCurrency(item.economicValue, "USD")}`
          : ""}
      </div>
    </div>
  ));
}

function CaseDetailPanel({ caseDetail }: Readonly<{ caseDetail: OperationalValueCaseDetailApi }>) {
  return (
    <div className="operational-value-item">
      <div className="operational-value-item__row">
        <div className="operational-value-item__title">{caseDetail.title}</div>
        <span className={`operational-value-badge ${statusBadgeClass(caseDetail.status)}`}>
          {caseDetail.status}
        </span>
        <span className="operational-value-badge">{caseDetail.valueCategory}</span>
      </div>
      <div className="operational-value-item__meta">{caseDetail.problemSummary}</div>
      <div className="operational-value-detail-grid">
        <div className="operational-value-detail-cell">
          <div className="operational-value-detail-cell__label">Baseline</div>
          <div className="operational-value-detail-cell__value">
            {formatNumber(caseDetail.baselineValue, 1)} {caseDetail.unit}
          </div>
          <div className="operational-value-item__meta">
            {caseDetail.baselineMethod} · {caseDetail.baselineSource}
          </div>
        </div>
        <div className="operational-value-detail-cell">
          <div className="operational-value-detail-cell__label">Gerçekleşen</div>
          <div className="operational-value-detail-cell__value">
            {formatNumber(caseDetail.actualValue, 1)} {caseDetail.unit}
          </div>
        </div>
        <div className="operational-value-detail-cell">
          <div className="operational-value-detail-cell__label">Delta</div>
          <div className="operational-value-detail-cell__value">
            +{formatNumber(caseDetail.deltaValue, 1)} {caseDetail.unit}
          </div>
        </div>
        <div className="operational-value-detail-cell">
          <div className="operational-value-detail-cell__label">Doğrulama</div>
          <div className="operational-value-detail-cell__value">{caseDetail.verificationOutcome}</div>
        </div>
        <div className="operational-value-detail-cell">
          <div className="operational-value-detail-cell__label">Zeka maliyeti</div>
          <div className="operational-value-detail-cell__value">
            {formatCurrency(caseDetail.intelligenceCost)}
          </div>
        </div>
        <div className="operational-value-detail-cell">
          <div className="operational-value-detail-cell__label">Ekonomik değer</div>
          <div className="operational-value-detail-cell__value">
            {caseDetail.economicValue != null
              ? formatCurrency(caseDetail.economicValue)
              : economicStatusLabel(caseDetail.economicConversionStatus)}
          </div>
        </div>
        {caseDetail.netValue != null ? (
          <div className="operational-value-detail-cell">
            <div className="operational-value-detail-cell__label">Net değer</div>
            <div className="operational-value-detail-cell__value">
              {formatCurrency(caseDetail.netValue)}
            </div>
          </div>
        ) : null}
        {caseDetail.roiPercent != null ? (
          <div className="operational-value-detail-cell">
            <div className="operational-value-detail-cell__label">ROI</div>
            <div className="operational-value-detail-cell__value">
              {formatNumber(caseDetail.roiPercent, 1)}%
            </div>
          </div>
        ) : null}
      </div>
      {caseDetail.evidence.length > 0 ? (
        <div className="operational-value-item__meta">
          Kanıt: {caseDetail.evidence.map((item) => item.detail).join(" · ")}
        </div>
      ) : null}
    </div>
  );
}

function EconomicInputsPanel({
  inputs,
  canManage,
}: Readonly<{ inputs?: OperationalValueEconomicInputSnapshotApi; canManage: boolean }>) {
  if (!inputs) {
    return (
      <p className="operational-value-item__meta">
        Ekonomik girdi yapılandırması bulunamadı. Operasyonel değer gösterilir; finansal dönüşüm yapılamaz.
      </p>
    );
  }

  return (
    <>
      {!canManage ? (
        <p className="operational-value-readonly-note">
          Ekonomik girdiler salt okunur görünümde. Düzenleme yetkisi gerekir.
        </p>
      ) : null}
      <div className="operational-value-item__meta">
        Sürüm {inputs.versionCode} · {inputs.currency} · {inputs.effectiveFromUtc.slice(0, 10)}
        {inputs.isActive ? " · aktif" : ""}
      </div>
      <div className="operational-value-economic-grid">
        <EconomicField label="Birim katkı" value={inputs.unitContributionValue} currency={inputs.currency} />
        <EconomicField label="Hurda birim maliyeti" value={inputs.scrapUnitCost} currency={inputs.currency} />
        <EconomicField label="Duruş / saat" value={inputs.downtimeCostPerHour} currency={inputs.currency} />
        <EconomicField label="İşgücü / saat" value={inputs.laborCostPerHour} currency={inputs.currency} />
        <EconomicField label="Mesai / saat" value={inputs.overtimeCostPerHour} currency={inputs.currency} />
        <EconomicField label="Changeover / dk" value={inputs.changeoverCostPerMinute} currency={inputs.currency} />
        <EconomicField label="Enerji birim" value={inputs.energyUnitCost} currency={inputs.currency} />
        <EconomicField label="Gecikme cezası" value={inputs.lateDeliveryPenalty} currency={inputs.currency} />
      </div>
    </>
  );
}

function EconomicField({
  label,
  value,
  currency,
}: Readonly<{ label: string; value?: number; currency: string }>) {
  return (
    <div className="operational-value-economic-field">
      <div className="operational-value-economic-field__label">{label}</div>
      <div className="operational-value-economic-field__value">
        {value != null ? formatCurrency(value, currency) : "—"}
      </div>
    </div>
  );
}

export function OperationalValueControlCenterView({
  controlCenter,
}: OperationalValueControlCenterViewProps) {
  const { summary, intelligenceCost, reportPreview } = controlCenter;
  const currency = summary.currency ?? reportPreview.currency ?? "USD";
  const featuredCase = controlCenter.featuredCase ?? controlCenter.recentCases[0];
  const hasEconomicValue =
    summary.economicConversionStatus.toUpperCase() === "CONVERTED"
    && summary.verifiedEconomicValue != null;

  return (
    <div className="operational-value-shell" data-testid="operational-value-control-center">
      <header className="fact-section__header">
        <div>
          <p className="fact-section__eyebrow">ULTRA I</p>
          <h1>Operasyonel Değer</h1>
          <p className="fact-section__subtitle">
            Doğrulanmış operasyonel kazanım, maliyet ve savunulabilir ROI yüzeyi
          </p>
        </div>
        <div className="operational-value-item__row">
          <Link className="shift-ops-cta" href="/intelligence-runtime">
            Zeka maliyeti
          </Link>
          <Link className="shift-ops-cta" href="/fabrika">
            Fabrika
          </Link>
        </div>
      </header>

      <section className="operational-value-summary" data-testid="operational-value-summary">
        <div className="operational-value-summary__item">
          <div className="operational-value-summary__label">Doğrulanmış sonuç</div>
          <div className="operational-value-summary__value">{summary.verifiedOutcomes}</div>
        </div>
        <div className="operational-value-summary__item">
          <div className="operational-value-summary__label">İyi çıktı kazanımı</div>
          <div className="operational-value-summary__value">
            +{formatNumber(summary.goodOutputGainedUnits, 0)}
          </div>
          <div className="operational-value-summary__meta">adet</div>
        </div>
        <div className="operational-value-summary__item">
          <div className="operational-value-summary__label">Önlenen duruş</div>
          <div className="operational-value-summary__value">
            {formatMinutes(summary.downtimeAvoidedMinutes)}
          </div>
        </div>
        <div className="operational-value-summary__item">
          <div className="operational-value-summary__label">Korunan geciken sipariş</div>
          <div className="operational-value-summary__value">{summary.lateOrdersProtected}</div>
        </div>
        <div className="operational-value-summary__item">
          <div className="operational-value-summary__label">Azalan insan dokunuşu</div>
          <div className="operational-value-summary__value">{summary.humanTouchesReduced}</div>
        </div>
        <div className="operational-value-summary__item">
          <div className="operational-value-summary__label">Zeka maliyeti</div>
          <div className="operational-value-summary__value">
            {formatCurrency(summary.intelligenceCost, currency)}
          </div>
        </div>
        {hasEconomicValue ? (
          <div className="operational-value-summary__item operational-value-summary__item--economic">
            <div className="operational-value-summary__label">Doğrulanmış ekonomik değer</div>
            <div className="operational-value-summary__value">
              {formatCurrency(summary.verifiedEconomicValue!, currency)}
            </div>
            <div className="operational-value-summary__meta">
              {economicStatusLabel(summary.economicConversionStatus)}
            </div>
          </div>
        ) : (
          <div className="operational-value-summary__item">
            <div className="operational-value-summary__label">Ekonomik değer</div>
            <div className="operational-value-summary__value">—</div>
            <div className="operational-value-summary__meta">
              {economicStatusLabel(summary.economicConversionStatus)}
            </div>
          </div>
        )}
      </section>

      <section className="operational-value-section">
        <div className="operational-value-section__header">
          <h2>Değer trendi</h2>
        </div>
        <div className="operational-value-section__body" data-testid="operational-value-trend">
          {controlCenter.trend.length === 0 ? (
            <p className="operational-value-item__meta">Trend verisi henüz oluşmadı.</p>
          ) : (
            <div className="operational-value-trend-grid">
              {controlCenter.trend.map((point) => (
                <div key={point.periodStartUtc} className="operational-value-trend-point">
                  <div className="operational-value-trend-point__label">{point.periodLabel}</div>
                  <div className="operational-value-trend-point__value">
                    +{formatNumber(point.goodOutputGainedUnits, 0)} adet
                  </div>
                  <div className="operational-value-item__meta">
                    {point.verifiedOutcomes} doğrulanmış · {formatMinutes(point.downtimeAvoidedMinutes)}
                  </div>
                  {point.costPerVerifiedOutcome != null ? (
                    <div className="operational-value-item__meta">
                      {formatCurrency(point.costPerVerifiedOutcome, currency)} / sonuç
                    </div>
                  ) : null}
                </div>
              ))}
            </div>
          )}
        </div>
      </section>

      <section className="operational-value-section">
        <div className="operational-value-section__header">
          <h2>Kırılım</h2>
          <div className="operational-value-breakdown-tabs">
            <span className="operational-value-breakdown-tab operational-value-breakdown-tab--active">Kategori</span>
            <span className="operational-value-breakdown-tab">Hat</span>
            <span className="operational-value-breakdown-tab">İş akışı</span>
          </div>
        </div>
        <div className="operational-value-section__body" data-testid="operational-value-breakdown">
          <BreakdownList
            items={controlCenter.breakdownByCategory}
            emptyLabel="Kategori kırılımı bulunamadı."
          />
          {controlCenter.breakdownByLine.length > 0 ? (
            <>
              <p className="operational-value-item__meta">Hat kırılımı</p>
              <BreakdownList items={controlCenter.breakdownByLine} emptyLabel="" />
            </>
          ) : null}
          {controlCenter.breakdownByWorkflow.length > 0 ? (
            <>
              <p className="operational-value-item__meta">İş akışı kırılımı</p>
              <BreakdownList items={controlCenter.breakdownByWorkflow} emptyLabel="" />
            </>
          ) : null}
        </div>
      </section>

      <section className="operational-value-section">
        <div className="operational-value-section__header">
          <h2>Vaka detayı</h2>
        </div>
        <div className="operational-value-section__body" data-testid="operational-value-case-detail">
          {featuredCase ? (
            <CaseDetailPanel caseDetail={featuredCase} />
          ) : (
            <p className="operational-value-item__meta">Doğrulanmış değer vakası bulunamadı.</p>
          )}
        </div>
      </section>

      <section className="operational-value-section">
        <div className="operational-value-section__header">
          <h2>Ekonomik girdi yapılandırması</h2>
        </div>
        <div className="operational-value-section__body" data-testid="operational-value-economic-inputs">
          <EconomicInputsPanel
            inputs={controlCenter.activeEconomicInputs}
            canManage={controlCenter.canManageEconomicInputs}
          />
        </div>
      </section>

      <section className="operational-value-section">
        <div className="operational-value-section__header">
          <h2>Zeka maliyeti bağlantısı</h2>
          <Link className="shift-ops-cta" href="/intelligence-runtime">
            Intelligence Runtime
          </Link>
        </div>
        <div className="operational-value-section__body">
          <div className="operational-value-cost-link">
            <div className="operational-value-detail-grid">
              <div className="operational-value-detail-cell">
                <div className="operational-value-detail-cell__label">Toplam zeka maliyeti</div>
                <div className="operational-value-detail-cell__value">
                  {formatCurrency(intelligenceCost.totalIntelligenceCost, currency)}
                </div>
              </div>
              <div className="operational-value-detail-cell">
                <div className="operational-value-detail-cell__label">Doğrulanmış sonuç maliyeti</div>
                <div className="operational-value-detail-cell__value">
                  {intelligenceCost.costPerVerifiedOutcome != null
                    ? formatCurrency(intelligenceCost.costPerVerifiedOutcome, currency)
                    : "—"}
                </div>
              </div>
              <div className="operational-value-detail-cell">
                <div className="operational-value-detail-cell__label">Deterministik oran</div>
                <div className="operational-value-detail-cell__value">
                  {(intelligenceCost.deterministicRouteRate * 100).toFixed(1)}%
                </div>
              </div>
              <div className="operational-value-detail-cell">
                <div className="operational-value-detail-cell__label">Frontier model oranı</div>
                <div className="operational-value-detail-cell__value">
                  {(intelligenceCost.frontierModelCallRate * 100).toFixed(1)}%
                </div>
              </div>
            </div>
            <p className="operational-value-item__meta">{intelligenceCost.message}</p>
          </div>
        </div>
      </section>

      <section className="operational-value-section">
        <div className="operational-value-section__header">
          <h2>Müşteri raporu önizleme</h2>
          {controlCenter.canExport ? (
            <span className="operational-value-badge operational-value-badge--verified">EXPORT</span>
          ) : null}
        </div>
        <div className="operational-value-section__body" data-testid="operational-value-report-preview">
          <div className="operational-value-report">
            <div className="operational-value-report__title">
              {reportPreview.periodLabel} · {reportPreview.verifiedOutcomes} doğrulanmış sonuç
            </div>
            <ul className="operational-value-report__list">
              {reportPreview.operationalImprovements.map((item) => (
                <li key={item}>{item}</li>
              ))}
            </ul>
            <div className="operational-value-item__meta">
              Zeka maliyeti: {formatCurrency(reportPreview.intelligenceCost, currency)}
              {reportPreview.verifiedEconomicValue != null
                ? ` · Ekonomik değer: ${formatCurrency(reportPreview.verifiedEconomicValue, currency)}`
                : ` · ${economicStatusLabel(reportPreview.economicConversionStatus)}`}
              {reportPreview.netValue != null
                ? ` · Net: ${formatCurrency(reportPreview.netValue, currency)}`
                : ""}
              {reportPreview.roiPercent != null
                ? ` · ROI: ${formatNumber(reportPreview.roiPercent, 1)}%`
                : ""}
              {reportPreview.paybackStatus ? ` · Geri ödeme: ${reportPreview.paybackStatus}` : ""}
            </div>
            <p className="operational-value-report__method">{reportPreview.methodologySummary}</p>
          </div>
        </div>
      </section>
    </div>
  );
}
