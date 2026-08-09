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
import { Button } from "@/components/ui/button";
import { QualityPageShell } from "@/features/quality-control/components/quality-page-shell";
import "@/features/quality-control/components/quality-workspace.css";
import { formatQualityPercent } from "@/features/quality-control/domain/quality-control";
import type { QualityReportingTrendPeriod } from "@/features/quality-control/domain/quality-control";
import { readQualityReportingPage } from "@/features/quality-control/server/quality-control-reader";

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

export default async function QualityReportingPage({ searchParams }: PageProps) {
  const { period: periodParam } = await searchParams;
  const period = parsePeriod(periodParam);
  const result = await readQualityReportingPage(period);

  if (result.status === "failure") {
    return (
      <QualityPageShell
        backHref="/kalite"
        backLabel="Kalite ana sayfası"
        eyebrow="KALİTE RAPORLAMA"
        title="Kalite Raporları"
        nav="reporting"
      >
        <div role="alert" className="quality-error-panel">
          <ShieldCheck aria-hidden="true" />
          <div>
            <h3>Raporlama verisi yüklenemedi</h3>
            <p>{result.message}</p>
          </div>
        </div>
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

  return (
    <QualityPageShell
      backHref="/kalite"
      backLabel="Kalite ana sayfası"
      eyebrow="KALİTE RAPORLAMA"
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
          </div>
        </div>
        <div className="production-execution-kpis__item--ready">
          <TrendingUp aria-hidden="true" />
          <div>
            <span>İlk Geçiş Verimi</span>
            <strong>{formatQualityPercent(overview.firstPassYieldPercent)}</strong>
          </div>
        </div>
        <div className="production-execution-kpis__item--in-progress">
          <BarChart3 aria-hidden="true" />
          <div>
            <span>Serbest Bırakma Oranı</span>
            <strong>{formatQualityPercent(overview.releaseRatePercent)}</strong>
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
            <span>Aktif Blokesi</span>
            <strong>{overview.activeHoldCount}</strong>
            <small>Bloklu emir: {overview.blockingProductionOrderCount}</small>
          </div>
        </div>
        <div className="production-execution-kpis__item--ready">
          <ShieldCheck aria-hidden="true" />
          <div>
            <span>Düzeltici Aksiyon</span>
            <strong>{overview.openCorrectiveActionCount}</strong>
            <small>Geciken: {overview.overdueCorrectiveActionCount}</small>
          </div>
        </div>
      </section>

      <div className="quality-detail-grid">
        <section className="quality-section" aria-label="NCR analitiği">
          <header className="quality-section__header">
            <h2>NCR</h2>
          </header>
          <dl className="quality-detail-dl">
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
              <dt>Kritik / Majör / Minör</dt>
              <dd>
                {ncrAnalytics.criticalCount} / {ncrAnalytics.majorCount} / {ncrAnalytics.minorCount}
              </dd>
            </div>
          </dl>
        </section>
        <section className="quality-section" aria-label="Blokesi analitiği">
          <header className="quality-section__header">
            <h2>Kalite Blokeleri</h2>
          </header>
          <dl className="quality-detail-dl">
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
          <dl className="quality-detail-dl">
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
          <form className="quality-filter-panel" method="get">
            <div className="quality-filter-panel__field">
              <label htmlFor="report-period">Dönem</label>
              <select id="report-period" name="period" className="quality-form-select" defaultValue={period}>
                <option value="day">Günlük</option>
                <option value="week">Haftalık</option>
                <option value="month">Aylık</option>
              </select>
            </div>
            <div className="quality-filter-panel__actions">
              <Button type="submit" variant="secondary" size="medium">
                Dönemi Güncelle
              </Button>
            </div>
          </form>
        </header>
        <div className="quality-table-wrap">
          <table className="quality-table">
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
                      <div className="quality-table__identifier">
                        <strong>{row.periodKey}</strong>
                        <small>{new Date(row.periodStartUtc).toLocaleDateString("tr-TR")}</small>
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
        <div className="quality-table-wrap">
          <table className="quality-table">
            <thead>
              <tr>
                <th>Boyut</th>
                <th>Anahtar</th>
                <th>Etiket</th>
                <th>Adet</th>
              </tr>
            </thead>
            <tbody>
              {failures.length === 0 ? (
                <tr>
                  <td colSpan={4}>
                    <span className="quality-empty-state">Başarısızlık kaydı yok.</span>
                  </td>
                </tr>
              ) : (
                failures.map((item) => (
                  <tr key={`${item.dimension}-${item.key}`}>
                    <td>{item.dimension}</td>
                    <td>{item.key}</td>
                    <td>{item.label ?? "—"}</td>
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
