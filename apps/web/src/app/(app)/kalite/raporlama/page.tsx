import { TrendBars } from "@/components/ui/trend-bars";
import { formatDate, formatWeekKey } from "@/lib/presentation/format";
import {
  AlertTriangle,
  BarChart3,
  CheckCircle2,
  ClipboardList,
  FileWarning,
  Lock,
  ShieldCheck,
  TrendingUp,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { QualityPageShell } from "@/features/quality-control/components/quality-page-shell";
import "@/features/quality-control/components/quality-workspace.css";
import { QualityPeriodSelect } from "@/features/quality-control/components/quality-period-select";
import { formatQualityPercent } from "@/features/quality-control/domain/quality-control";
import {
  formatQualityTrendLabel,
  hasEnoughQualityTrendData,
} from "@/features/quality-control/presentation/quality-reporting-labels";
import { getQualityCategoryLabel } from "@/features/quality-control/presentation/quality-ui-labels";
import type { QualityReportingTrendPeriod } from "@/features/quality-control/domain/quality-control";
import { readQualityReportingPage } from "@/features/quality-control/server/quality-control-reader";

import { FailureStatePage } from "@/components/ui/failure-state";

type PageProps = Readonly<{
  searchParams: Promise<{ period?: string }>;
}>;

function parsePeriod(value?: string): QualityReportingTrendPeriod {
  if (value === "day" || value === "month") {
    return value;
  }

  return "week";
}

function periodLabel(period: QualityReportingTrendPeriod): string {
  switch (period) {
    case "day":
      return "Günlük";
    case "month":
      return "Aylık";
    default:
      return "Haftalık";
  }
}

const DIMENSION_LABELS: Record<string, string> = {
  "ncr-category": "NCR kategorisi",
  "ncr-severity": "NCR önem derecesi",
  "inspection-type": "Kontrol türü",
  "defect-code": "Hata kodu",
};

export default async function QualityReportingPage({ searchParams }: PageProps) {
  const { period: periodParam } = await searchParams;
  const period = parsePeriod(periodParam);
  const result = await readQualityReportingPage(period);

  if (result.status === "failure") {
    return (
      <QualityPageShell
        backHref="/kalite"
        backLabel="Kalite ana sayfası"
        eyebrow="KALİTE KONTROL"
        title="Kalite Raporları"
        nav="reporting"
      >
        <FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Raporlama verisi yüklenemedi" inline />
      </QualityPageShell>
    );
  }

  const {
    overview,
    trends,
    failures,
    ncrAnalytics,
    holdAnalytics,
    correctiveActionAnalytics,
  } = result;

  const chartPoints = [...trends].reverse().map((row) => ({
    label: formatQualityTrendLabel(period, row.periodKey, row.periodStartUtc),
    passed: row.passCount,
    failed: row.failCount,
    marker: row.ncrCount,
  }));

  return (
    <QualityPageShell
      backHref="/kalite"
      backLabel="Kalite ana sayfası"
      eyebrow="KALİTE KONTROL"
      title="Kalite Raporları"
      subtitle="Kontrol performansı, uygunsuzluk trendleri ve başarısızlık dağılımını salt okunur raporlama görünümünde izleyin."
      nav="reporting"
      footer={
        <footer className="quality-footer-note">
          <ShieldCheck aria-hidden="true" />
          Kalite raporları salt okunurdur; serbest bırakma ve NCR işlemleri operasyon ekranlarından yapılır.
        </footer>
      }
    >
      <section className="quality-reporting-kpis production-execution-kpis" aria-label="Kalite özeti">
        <div className="production-execution-kpis__item--total">
          <ClipboardList aria-hidden="true" />
          <div>
            <span>Toplam Kontrol</span>
            <strong>{overview.totalInspectionCount}</strong>
          </div>
        </div>
        <div className="production-execution-kpis__item--completed">
          <CheckCircle2 aria-hidden="true" />
          <div>
            <span>Geçme Oranı</span>
            <strong>{formatQualityPercent(overview.passRatePercent)}</strong>
            {overview.passRatePercent == null ? <small>Veri yok</small> : null}
          </div>
        </div>
        <div className="production-execution-kpis__item--ready">
          <TrendingUp aria-hidden="true" />
          <div>
            <span>İlk Geçiş Verimi</span>
            <strong>{formatQualityPercent(overview.firstPassYieldPercent)}</strong>
            {overview.firstPassYieldPercent == null ? <small>Veri yok</small> : null}
          </div>
        </div>
        <div className="production-execution-kpis__item--in-progress">
          <BarChart3 aria-hidden="true" />
          <div>
            <span>Serbest Bırakma</span>
            <strong>{formatQualityPercent(overview.releaseRatePercent)}</strong>
            {overview.releaseRatePercent == null ? <small>Veri yok</small> : null}
          </div>
        </div>
        <div className="production-execution-kpis__item--paused">
          <AlertTriangle aria-hidden="true" />
          <div>
            <span>Başarısız Kontrol</span>
            <strong>{overview.failedInspectionCount}</strong>
          </div>
        </div>
        <div className="production-execution-kpis__item--paused">
          <FileWarning aria-hidden="true" />
          <div>
            <span>Açık NCR</span>
            <strong>{overview.openNcrCount}</strong>
            <small>Kritik: {overview.criticalNcrCount}</small>
          </div>
        </div>
        <div className="production-execution-kpis__item--in-progress">
          <Lock aria-hidden="true" />
          <div>
            <span>Aktif Bloke</span>
            <strong>{overview.activeHoldCount}</strong>
            <small>Bloklu emir: {overview.blockingProductionOrderCount}</small>
          </div>
        </div>
        <div className="production-execution-kpis__item--ready">
          <ShieldCheck aria-hidden="true" />
          <div>
            <span>Aksiyonlar</span>
            <strong>{overview.openCorrectiveActionCount}</strong>
            <small>Geciken: {overview.overdueCorrectiveActionCount}</small>
          </div>
        </div>
      </section>

      <div className="quality-summary-grid">
        <section className="quality-section" aria-label="NCR analitiği">
          <header className="quality-section__header">
            <h2>NCR</h2>
          </header>
          <dl className="quality-stat-list">
            <div>
              <dt>Açık</dt>
              <dd>{ncrAnalytics.openCount}</dd>
            </div>
            <div>
              <dt>Çözüldü</dt>
              <dd>{ncrAnalytics.resolvedCount}</dd>
            </div>
            <div>
              <dt>Kapatıldı</dt>
              <dd>{ncrAnalytics.closedCount}</dd>
            </div>
            <div>
              <dt>Kritik</dt>
              <dd>{ncrAnalytics.criticalCount}</dd>
            </div>
            <div>
              <dt>Majör</dt>
              <dd>{ncrAnalytics.majorCount}</dd>
            </div>
            <div>
              <dt>Minör</dt>
              <dd>{ncrAnalytics.minorCount}</dd>
            </div>
          </dl>
        </section>
        <section className="quality-section" aria-label="Blokesi analitiği">
          <header className="quality-section__header">
            <h2>Kalite Blokeleri</h2>
          </header>
          <dl className="quality-stat-list">
            <div>
              <dt>Aktif</dt>
              <dd>{holdAnalytics.activeCount}</dd>
            </div>
            <div>
              <dt>Serbest</dt>
              <dd>{holdAnalytics.releasedCount}</dd>
            </div>
            <div>
              <dt>Etkilenen Emir</dt>
              <dd>{holdAnalytics.affectedProductionOrderCount}</dd>
            </div>
          </dl>
        </section>
        <section className="quality-section" aria-label="Düzeltici aksiyon analitiği">
          <header className="quality-section__header">
            <h2>Düzeltici Aksiyonlar</h2>
          </header>
          <dl className="quality-stat-list">
            <div>
              <dt>Açık</dt>
              <dd>{correctiveActionAnalytics.openCount}</dd>
            </div>
            <div>
              <dt>Devam Eden</dt>
              <dd>{correctiveActionAnalytics.inProgressCount}</dd>
            </div>
            <div>
              <dt>Tamamlanan</dt>
              <dd>{correctiveActionAnalytics.completedCount}</dd>
            </div>
            <div>
              <dt>Geciken</dt>
              <dd>{correctiveActionAnalytics.overdueCount}</dd>
            </div>
          </dl>
        </section>
      </div>

      <section className="quality-section quality-section--wide" aria-label="Trend tablosu">
        <header className="quality-section__header">
          <div>
            <h2>{periodLabel(period)} Kalite Trendi</h2>
          </div>
          <QualityPeriodSelect value={period} />
        </header>
        {hasEnoughQualityTrendData(chartPoints) ? (
          <TrendBars
            ariaLabel={`${periodLabel(period)} kalite trendi: dönem başına geçen ve başarısız kontroller, NCR sayısı`}
            points={chartPoints}
            passedLabel="Geçen kontrol"
            failedLabel="Başarısız kontrol"
            markerLabel="NCR sayısı"
          />
        ) : (
          <p className="quality-empty-state" data-testid="quality-trend-insufficient">
            Yeterli veri yok. Trend grafiği için en az iki dönemde kayıt gerekir; mevcut değerler aşağıdaki tabloda yer alır.
          </p>
        )}
        <div className="quality-table-wrap" tabIndex={0} role="region" aria-label="Dönemsel kalite trendi tablosu">
          <table aria-label="Dönemsel kalite trendi" className="quality-table quality-table--compact">
            <thead>
              <tr>
                <th>Dönem</th>
                <th>Kontrol</th>
                <th>Geçti</th>
                <th>Başarısız</th>
                <th>NCR</th>
                <th>Kritik NCR</th>
                <th>Blokesi</th>
              </tr>
            </thead>
            <tbody>
              {trends.length === 0 ? (
                <tr>
                  <td colSpan={7}>
                    <span className="quality-empty-state">Trend verisi yok.</span>
                  </td>
                </tr>
              ) : (
                trends.map((row) => (
                  <tr key={row.periodKey}>
                    <td>
                      <div className="quality-table__identifier quality-table__identifier--nowrap">
                        <strong>{formatWeekKey(row.periodKey)}</strong>
                        <small>{formatDate(row.periodStartUtc)}</small>
                      </div>
                    </td>
                    <td>{row.inspectionCount}</td>
                    <td>{row.passCount}</td>
                    <td>{row.failCount}</td>
                    <td>{row.ncrCount}</td>
                    <td>{row.criticalNcrCount}</td>
                    <td>{row.holdCount}</td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </section>

      <section className="quality-section quality-section--wide" aria-label="Başarısızlık dağılımı">
        <header className="quality-section__header">
          <h2>Hata Dağılımı</h2>
        </header>
        <div className="quality-table-wrap" tabIndex={0} role="region" aria-label="Hata dağılımı tablosu">
          <table aria-label="Hata Dağılımı" className="quality-table quality-table--compact">
            <thead>
              <tr>
                <th>Boyut</th>
                <th>Başarısızlık nedeni</th>
                <th>Adet</th>
              </tr>
            </thead>
            <tbody>
              {failures.length === 0 ? (
                <tr>
                  <td colSpan={3}>
                    <span className="quality-empty-state">Başarısızlık kaydı yok.</span>
                  </td>
                </tr>
              ) : (
                failures.map((item) => (
                  <tr key={`${item.dimension}-${item.key}`}>
                    <td>{DIMENSION_LABELS[item.dimension] ?? getQualityCategoryLabel(item.dimension)}</td>
                    <td>{getQualityCategoryLabel(item.label ?? item.key)}</td>
                    <td>
                      <Badge tone={item.count > 0 ? "warning" : "neutral"}>{item.count}</Badge>
                    </td>
                  </tr>
                ))
              )}
            </tbody>
          </table>
        </div>
      </section>
    </QualityPageShell>
  );
}
