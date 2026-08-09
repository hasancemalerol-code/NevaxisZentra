import Link from "next/link";
import {
  AlertTriangle,
  ArrowLeft,
  BarChart3,
  CheckCircle2,
  ClipboardList,
  FileWarning,
  Lock,
  ShieldCheck,
  TrendingUp,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import {
  formatQualityPercent,
} from "@/features/quality-control/domain/quality-control";
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
      <main className="production-execution-page">
        <header className="production-execution-header">
          <p>KALİTE RAPORLAMA</p>
          <h1>Kalite Raporları</h1>
        </header>
        <div role="alert" className="state-panel error-state">
          <ShieldCheck aria-hidden="true" />
          <div>
            <h3>Raporlama verisi yüklenemedi</h3>
            <p>{result.message}</p>
          </div>
        </div>
      </main>
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
    <main className="production-execution-page">
      <header className="production-execution-header">
        <Link href="/kalite" className="production-execution-header__link">
          <ArrowLeft aria-hidden="true" />
          Kalite ana sayfası
        </Link>
        <p>KALİTE RAPORLAMA</p>
        <h1>Kalite Raporları</h1>
        <span>
          Kontrol performansı, uygunsuzluk trendleri ve başarısızlık dağılımını salt okunur
          raporlama görünümünde izleyin.
        </span>
      </header>

      <section className="production-execution-kpis" aria-label="Kalite raporlama özeti">
        <Card className="production-execution-kpis__item--total">
          <ClipboardList aria-hidden="true" />
          <div>
            <span>Toplam Kontrol</span>
            <strong>{overview.totalInspectionCount}</strong>
          </div>
        </Card>
        <Card className="production-execution-kpis__item--completed">
          <CheckCircle2 aria-hidden="true" />
          <div>
            <span>Geçme Oranı</span>
            <strong>{formatQualityPercent(overview.passRatePercent)}</strong>
          </div>
        </Card>
        <Card className="production-execution-kpis__item--ready">
          <TrendingUp aria-hidden="true" />
          <div>
            <span>İlk Geçiş Verimi</span>
            <strong>{formatQualityPercent(overview.firstPassYieldPercent)}</strong>
          </div>
        </Card>
        <Card className="production-execution-kpis__item--in-progress">
          <BarChart3 aria-hidden="true" />
          <div>
            <span>Serbest Bırakma Oranı</span>
            <strong>{formatQualityPercent(overview.releaseRatePercent)}</strong>
          </div>
        </Card>
        <Card className="production-execution-kpis__item--paused">
          <AlertTriangle aria-hidden="true" />
          <div>
            <span>Başarısız Kontrol</span>
            <strong>{overview.failedInspectionCount}</strong>
          </div>
        </Card>
        <Card className="production-execution-kpis__item--paused">
          <FileWarning aria-hidden="true" />
          <div>
            <span>Açık NCR</span>
            <strong>{overview.openNcrCount}</strong>
            <small>Kritik: {overview.criticalNcrCount}</small>
          </div>
        </Card>
        <Card className="production-execution-kpis__item--in-progress">
          <Lock aria-hidden="true" />
          <div>
            <span>Aktif Blokesi</span>
            <strong>{overview.activeHoldCount}</strong>
            <small>Bloklu emir: {overview.blockingProductionOrderCount}</small>
          </div>
        </Card>
        <Card className="production-execution-kpis__item--ready">
          <ShieldCheck aria-hidden="true" />
          <div>
            <span>Düzeltici Aksiyon</span>
            <strong>{overview.openCorrectiveActionCount}</strong>
            <small>Geciken: {overview.overdueCorrectiveActionCount}</small>
          </div>
        </Card>
      </section>

      <section className="production-execution-workspace" aria-label="Analitik özet">
        <div className="production-execution-workspace__heading">
          <div>
            <p>ANALİTİK</p>
            <h2>NCR, blokesi ve aksiyon dağılımı</h2>
          </div>
        </div>
        <div className="production-execution-grid">
          <Card className="production-execution-panel">
            <header>
              <h3>NCR</h3>
            </header>
            <dl className="production-execution-details">
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
          </Card>
          <Card className="production-execution-panel">
            <header>
              <h3>Blokesi</h3>
            </header>
            <dl className="production-execution-details">
              <div>
                <dt>Aktif</dt>
                <dd>{holdAnalytics.activeCount}</dd>
              </div>
              <div>
                <dt>Serbest</dt>
                <dd>{holdAnalytics.releasedCount}</dd>
              </div>
              <div>
                <dt>Etkilenen emir</dt>
                <dd>{holdAnalytics.affectedProductionOrderCount}</dd>
              </div>
            </dl>
          </Card>
          <Card className="production-execution-panel">
            <header>
              <h3>Düzeltici aksiyon</h3>
            </header>
            <dl className="production-execution-details">
              <div>
                <dt>Açık</dt>
                <dd>{correctiveActionAnalytics.openCount}</dd>
              </div>
              <div>
                <dt>Devam eden</dt>
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
          </Card>
        </div>
      </section>

      <section className="production-execution-workspace" aria-label="Trend tablosu">
        <div className="production-execution-workspace__heading">
          <div>
            <p>TREND</p>
            <h2>{periodLabel(period)} kalite trendi</h2>
          </div>
          <form className="production-execution-form-row" method="get">
            <select name="period" defaultValue={period}>
              <option value="day">Günlük</option>
              <option value="week">Haftalık</option>
              <option value="month">Aylık</option>
            </select>
            <button type="submit" className="production-execution-header__link">
              Dönemi güncelle
            </button>
          </form>
        </div>

        <Card className="production-execution-panel production-execution-panel--wide">
          <table className="production-execution-table">
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
                  <td colSpan={7}>Trend verisi yok.</td>
                </tr>
              ) : (
                trends.map((row) => (
                  <tr key={row.periodKey}>
                    <td>
                      <strong>{row.periodKey}</strong>
                      <span>{new Date(row.periodStartUtc).toLocaleDateString("tr-TR")}</span>
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
        </Card>
      </section>

      <section className="production-execution-workspace" aria-label="Başarısızlık dağılımı">
        <div className="production-execution-workspace__heading">
          <div>
            <p>BAŞARISIZLIK</p>
            <h2>Başarısızlık dağılımı</h2>
          </div>
        </div>

        <Card className="production-execution-panel production-execution-panel--wide">
          <table className="production-execution-table">
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
                  <td colSpan={4}>Başarısızlık kaydı yok.</td>
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
        </Card>
      </section>

      <footer className="production-execution-footer-note">
        <ShieldCheck aria-hidden="true" />
        Kalite raporları salt okunurdur; serbest bırakma ve NCR işlemleri operasyon ekranlarından yapılır.
      </footer>
    </main>
  );
}
