import Link from "next/link";
import {
  AlertTriangle,
  BarChart3,
  CheckCircle2,
  ClipboardCheck,
  ClipboardList,
  FileWarning,
  Lock,
  Plus,
  Timer,
  TrendingUp,
  TriangleAlert,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import {
  formatQualityPercent,
  getQualityNcrSeverityTone,
  getQualityStatusLabel,
} from "../domain/quality-control";
import type {
  QualityControlPlanSummary,
  QualityInspectionSummary,
  QualityNcrSummary,
  QualityReportingOverview,
  QualityWorkspaceSummary,
} from "../domain/quality-control";
import type { QualityDashboardCorrectiveAction } from "../server/quality-control-reader";
import {
  buildQualityCriticalAlerts,
  formatQualityAge,
  getQualityOverallResultLabel,
  getQualityReleaseStatusLabel,
  isCorrectiveActionOverdue,
} from "../presentation/quality-dashboard-labels";
import { QualityWorkspaceNav } from "./quality-workspace-nav";
import { QualityDashboardKpi } from "./quality-dashboard-kpi";
import "./quality-workspace.css";

type QualityDashboardViewProps = Readonly<{
  summary: QualityWorkspaceSummary;
  plans: readonly QualityControlPlanSummary[];
  inspections: readonly QualityInspectionSummary[];
  overview: QualityReportingOverview | null;
  openNcrs: readonly QualityNcrSummary[];
  activeHoldNcrs: readonly QualityNcrSummary[];
  correctiveActions: readonly QualityDashboardCorrectiveAction[];
}>;

function getOverallResultTone(result: string): "success" | "danger" | "neutral" | "information" {
  if (result === "pass") {
    return "success";
  }

  if (result === "fail") {
    return "danger";
  }

  return "neutral";
}

function getReleaseTone(status: string): "success" | "danger" | "neutral" | "information" {
  if (status === "released") {
    return "success";
  }

  if (status === "rejected") {
    return "danger";
  }

  if (status === "ready-for-review") {
    return "information";
  }

  return "neutral";
}

export function QualityDashboardView({
  summary,
  plans,
  inspections,
  overview,
  openNcrs,
  activeHoldNcrs,
  correctiveActions,
}: QualityDashboardViewProps) {
  const activeInspections = inspections
    .filter((item) => item.status === "draft" || item.status === "in-progress")
    .slice(0, 5);
  const recentInspections = [...inspections]
    .sort((a, b) => b.updatedAtUtc.localeCompare(a.updatedAtUtc))
    .slice(0, 5);
  const activePlans = [...plans]
    .filter((plan) => plan.status === "active")
    .sort((a, b) => b.updatedAtUtc.localeCompare(a.updatedAtUtc))
    .slice(0, 5);
  const alerts = buildQualityCriticalAlerts({
    inspections,
    ncrs: openNcrs,
    correctiveActions,
  });

  return (
    <main className="production-execution-page quality-dashboard-page">
      <header className="quality-dashboard-header production-execution-header">
        <div className="quality-dashboard-header__intro">
          <p>KALİTE KONTROL</p>
          <h1>Kalite Kontrol</h1>
          <span>
            Üretim kalite kontrollerini, uygunsuzlukları ve kalite blokelerini yönetin.
          </span>
        </div>
        <div className="quality-dashboard-header__actions">
          <Link
            href="/kalite/kontroller/yeni"
            className="quality-dashboard-action quality-dashboard-action--primary"
          >
            <ClipboardCheck aria-hidden="true" />
            Yeni Kalite Kontrolü
          </Link>
          <Link
            href="/kalite/planlar/yeni"
            className="quality-dashboard-action quality-dashboard-action--secondary"
          >
            <Plus aria-hidden="true" />
            Yeni Kontrol Planı
          </Link>
          <Link
            href="/kalite/raporlama"
            className="quality-dashboard-action quality-dashboard-action--tertiary"
          >
            <BarChart3 aria-hidden="true" />
            Kalite Raporları
          </Link>
        </div>
      </header>

      <QualityWorkspaceNav active="overview" />

      <section
        className="production-execution-kpis quality-dashboard-kpis"
        aria-label="Kalite göstergeleri"
      >
        <QualityDashboardKpi
          href="#aktif-kalite-kontrolleri"
          tone="in-progress"
          icon={<ClipboardList />}
          label="Açık Kontroller"
          value={summary.openInspectionCount}
          description="Aktif kalite kontrolleri"
        />
        <QualityDashboardKpi
          tone="ready"
          icon={<AlertTriangle />}
          label="İnceleme Bekleyenler"
          value={summary.pendingReviewCount}
          description="Serbest bırakma incelemesi"
        />
        <QualityDashboardKpi
          tone="paused"
          icon={<TriangleAlert />}
          label="Başarısız Kontroller"
          value={summary.failedInspectionCount}
          description="Başarısız sonuç"
        />
        <QualityDashboardKpi
          href="/kalite/ncr"
          tone="paused"
          icon={<FileWarning />}
          label="Açık NCR"
          value={summary.openNcrCount}
          description="Uygunsuzluk raporları"
        />
        <QualityDashboardKpi
          href="/kalite/ncr?hold=active"
          tone="in-progress"
          icon={<Lock />}
          label="Aktif Kalite Blokeleri"
          value={summary.activeHoldCount}
          description="Kalite blokesi aktif"
        />
        <QualityDashboardKpi
          tone="ready"
          icon={<Timer />}
          label="Aksiyon Bekleyenler"
          value={summary.actionPendingCount}
          description="Düzeltici aksiyon"
        />
      </section>

      {overview ? (
        <Card className="production-execution-panel">
          <header className="quality-dashboard-section__header">
            <h2>Kalite Raporları</h2>
            <Link href="/kalite/raporlama">Tüm raporları gör</Link>
          </header>
          <div className="quality-dashboard-reporting-preview" aria-label="Raporlama özeti">
            <div>
              <span>İlk Geçiş Verimi</span>
              <strong>{formatQualityPercent(overview.firstPassYieldPercent)}</strong>
            </div>
            <div>
              <span>Geçme Oranı</span>
              <strong>{formatQualityPercent(overview.passRatePercent)}</strong>
            </div>
            <div>
              <span>Serbest Bırakma Oranı</span>
              <strong>{formatQualityPercent(overview.releaseRatePercent)}</strong>
            </div>
            <div>
              <span>Serbest Bırakılanlar</span>
              <strong>{overview.releasedInspectionCount}</strong>
            </div>
          </div>
        </Card>
      ) : null}

      <section className="quality-dashboard-priority">
        <Card className="production-execution-panel" id="aktif-kalite-kontrolleri">
          <header className="quality-dashboard-section__header">
            <h2 aria-label="Aktif Kontroller">Aktif Kalite Kontrolleri</h2>
          </header>
          {activeInspections.length === 0 ? (
            <p className="quality-dashboard-empty">Aktif kalite kontrolü bulunmuyor.</p>
          ) : (
            <div className="quality-dashboard-section">
              {activeInspections.map((inspection) => (
                <Link
                  key={inspection.id}
                  href={`/kalite/kontroller/${inspection.id}`}
                  className="quality-inspection-row"
                >
                  <div className="quality-inspection-row__primary">
                    <strong>{inspection.inspectionNumber}</strong>
                    <div className="quality-inspection-row__meta">
                      <span>{inspection.productionOrderReference ?? inspection.productionOrderId}</span>
                      <span>{inspection.productReference ?? "Ürün yok"}</span>
                    </div>
                  </div>
                  <div className="quality-inspection-row__field">
                    <span>Kontrol Planı</span>
                    <strong>{inspection.planCode ?? "—"}</strong>
                  </div>
                  <div className="quality-inspection-row__field">
                    <span>Durum</span>
                    <Badge tone="information">{getQualityStatusLabel(inspection.status)}</Badge>
                  </div>
                  <div className="quality-inspection-row__field">
                    <span>Serbest Bırakma</span>
                    <Badge tone={getReleaseTone(inspection.releaseStatus)}>
                      {getQualityReleaseStatusLabel(inspection.releaseStatus)}
                    </Badge>
                  </div>
                  <div className="quality-inspection-row__actions">
                    <Badge tone={getOverallResultTone(inspection.overallResult)}>
                      {getQualityOverallResultLabel(inspection.overallResult)}
                    </Badge>
                    <span className="quality-inspection-row__open">Kontrolü Aç</span>
                  </div>
                </Link>
              ))}
            </div>
          )}
        </Card>

        <Card className="production-execution-panel">
          <header className="quality-dashboard-section__header">
            <h2>Kritik Kalite Uyarıları</h2>
          </header>
          {alerts.length === 0 ? (
            <p className="quality-dashboard-empty">Kritik kalite uyarısı bulunmuyor.</p>
          ) : (
            <ul className="quality-alert-list">
              {alerts.map((alert) => (
                <li key={alert.id} className="quality-alert-item">
                  <div
                    className={
                      alert.tone === "danger"
                        ? "quality-alert-item__icon quality-alert-item__icon--danger"
                        : "quality-alert-item__icon"
                    }
                  >
                    <AlertTriangle aria-hidden="true" size={16} />
                  </div>
                  <div className="quality-alert-item__body">
                    <strong>{alert.title}</strong>
                    <span>
                      {alert.reference} · {alert.reason}
                    </span>
                    <Badge tone={alert.tone}>{alert.statusLabel}</Badge>
                  </div>
                  <Link href={alert.href}>Detay</Link>
                </li>
              ))}
            </ul>
          )}
        </Card>
      </section>

      <section className="quality-dashboard-section">
        <Card className="production-execution-panel production-execution-panel--wide">
          <header className="quality-dashboard-section__header">
            <h2>Açık NCR</h2>
            <Link href="/kalite/ncr">Tüm NCR&apos;ları Gör</Link>
          </header>
          {openNcrs.length === 0 ? (
            <p className="quality-dashboard-empty">Açık NCR bulunmuyor.</p>
          ) : (
            <div className="quality-dashboard-table-wrap">
              <table className="quality-dashboard-table">
                <thead>
                  <tr>
                    <th>NCR No</th>
                    <th>Üretim Emri</th>
                    <th>Ürün</th>
                    <th>Şiddet</th>
                    <th>Durum</th>
                    <th>Blokesi</th>
                    <th>Sorumlu</th>
                    <th>Yaş</th>
                  </tr>
                </thead>
                <tbody>
                  {openNcrs.map((ncr) => (
                    <tr key={ncr.id}>
                      <td>
                        <Link href={`/kalite/ncr/${ncr.id}`}>
                          <strong>{ncr.ncrNumber}</strong>
                        </Link>
                        <small>{ncr.title}</small>
                      </td>
                      <td>{ncr.productionOrderReference ?? ncr.productionOrderId ?? "—"}</td>
                      <td>{ncr.productReference ?? "—"}</td>
                      <td>
                        <Badge tone={getQualityNcrSeverityTone(ncr.severity)}>
                          {getQualityStatusLabel(ncr.severity)}
                        </Badge>
                      </td>
                      <td>{getQualityStatusLabel(ncr.status)}</td>
                      <td>{getQualityStatusLabel(ncr.holdStatus)}</td>
                      <td>{ncr.ownerSubjectId ?? "—"}</td>
                      <td>{formatQualityAge(ncr.detectedAtUtc)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </Card>
      </section>

      <section className="quality-dashboard-priority">
        <Card className="production-execution-panel">
          <header className="quality-dashboard-section__header">
            <h2>Aktif Kalite Blokeleri</h2>
            <Link href="/kalite/ncr?hold=active">Tümünü gör</Link>
          </header>
          {activeHoldNcrs.length === 0 ? (
            <p className="quality-dashboard-empty">Aktif kalite blokesi bulunmuyor.</p>
          ) : (
            <div className="quality-dashboard-table-wrap">
              <table className="quality-dashboard-table">
                <thead>
                  <tr>
                    <th>NCR</th>
                    <th>Üretim Emri</th>
                    <th>Sebep</th>
                    <th>Durum</th>
                    <th>Yaş</th>
                  </tr>
                </thead>
                <tbody>
                  {activeHoldNcrs.map((ncr) => (
                    <tr key={ncr.id}>
                      <td>
                        <Link href={`/kalite/ncr/${ncr.id}`}>
                          <strong>{ncr.ncrNumber}</strong>
                        </Link>
                      </td>
                      <td>{ncr.productionOrderReference ?? "—"}</td>
                      <td>{ncr.title}</td>
                      <td>
                        <Badge tone="information">Kalite Blokesi: Aktif</Badge>
                      </td>
                      <td>{formatQualityAge(ncr.detectedAtUtc)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </Card>

        <Card className="production-execution-panel">
          <header className="quality-dashboard-section__header">
            <h2>Düzeltici Aksiyonlar</h2>
          </header>
          {correctiveActions.length === 0 ? (
            <p className="quality-dashboard-empty">Açık düzeltici aksiyon bulunmuyor.</p>
          ) : (
            <div className="quality-dashboard-table-wrap">
              <table className="quality-dashboard-table">
                <thead>
                  <tr>
                    <th>Başlık</th>
                    <th>NCR</th>
                    <th>Sorumlu</th>
                    <th>Termin</th>
                    <th>Durum</th>
                  </tr>
                </thead>
                <tbody>
                  {correctiveActions.map((action) => (
                    <tr key={action.id}>
                      <td>
                        <Link href={`/kalite/ncr/${action.ncrId}`}>
                          <strong>{action.title}</strong>
                        </Link>
                      </td>
                      <td>{action.ncrNumber}</td>
                      <td>{action.ownerSubjectId ?? "—"}</td>
                      <td>
                        {action.dueAtUtc
                          ? new Date(action.dueAtUtc).toLocaleDateString("tr-TR")
                          : "—"}
                      </td>
                      <td>
                        <Badge
                          tone={
                            isCorrectiveActionOverdue(action) ? "information" : "neutral"
                          }
                        >
                          {getQualityStatusLabel(action.status)}
                        </Badge>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </Card>
      </section>

      <section className="quality-dashboard-priority">
        <Card className="production-execution-panel">
          <header className="quality-dashboard-section__header">
            <h2>Kontrol Planları</h2>
            <Link href="/kalite/planlar/yeni">Yeni Kontrol Planı</Link>
          </header>
          {activePlans.length === 0 ? (
            <p className="quality-dashboard-empty">Aktif kontrol planı bulunmuyor.</p>
          ) : (
            <div className="quality-dashboard-plans">
              {activePlans.map((plan) => (
                <div key={plan.id} className="quality-plan-row">
                  <div className="quality-plan-row__meta">
                    <strong>{plan.code}</strong>
                    <span>{plan.name}</span>
                    <div className="quality-plan-row__tags">
                      <span>Rev. {plan.revision}</span>
                      {plan.productReference ? <span>{plan.productReference}</span> : null}
                      {plan.operationReference ? <span>{plan.operationReference}</span> : null}
                    </div>
                  </div>
                  <Badge tone={plan.status === "active" ? "success" : "neutral"}>
                    {getQualityStatusLabel(plan.status)}
                  </Badge>
                </div>
              ))}
            </div>
          )}
        </Card>

        <Card className="production-execution-panel">
          <header className="quality-dashboard-section__header">
            <h2 aria-label="Son Kontroller">Son Kalite Kontrolleri</h2>
          </header>
          {recentInspections.length === 0 ? (
            <p className="quality-dashboard-empty">Son kontrol kaydı bulunmuyor.</p>
          ) : (
            <div className="quality-dashboard-table-wrap">
              <table className="quality-dashboard-table">
                <thead>
                  <tr>
                    <th>Kontrol</th>
                    <th>Üretim Emri</th>
                    <th>Ürün</th>
                    <th>Plan</th>
                    <th>Sonuç</th>
                    <th>Serbest Bırakma</th>
                    <th>Güncelleme</th>
                  </tr>
                </thead>
                <tbody>
                  {recentInspections.map((inspection) => (
                    <tr key={inspection.id}>
                      <td>
                        <Link href={`/kalite/kontroller/${inspection.id}`}>
                          <strong>{inspection.inspectionNumber}</strong>
                        </Link>
                      </td>
                      <td>{inspection.productionOrderReference ?? "—"}</td>
                      <td>{inspection.productReference ?? "—"}</td>
                      <td>{inspection.planCode ?? "—"}</td>
                      <td>
                        <Badge tone={getOverallResultTone(inspection.overallResult)}>
                          {getQualityOverallResultLabel(inspection.overallResult)}
                        </Badge>
                      </td>
                      <td>
                        <Badge tone={getReleaseTone(inspection.releaseStatus)}>
                          {getQualityReleaseStatusLabel(inspection.releaseStatus)}
                        </Badge>
                      </td>
                      <td>{formatQualityAge(inspection.updatedAtUtc)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </Card>
      </section>

      <Card className="production-execution-panel">
        <header className="quality-dashboard-section__header">
          <h2>Hızlı İşlemler</h2>
        </header>
        <div className="quality-dashboard-quick-actions">
          <Link href="/kalite/kontroller/yeni">
            <ClipboardCheck aria-hidden="true" />
            Yeni Kalite Kontrolü
          </Link>
          <Link href="/kalite/planlar/yeni">
            <Plus aria-hidden="true" />
            Yeni Kontrol Planı
          </Link>
          <Link href="/kalite/ncr">
            <FileWarning aria-hidden="true" />
            NCR&apos;ları Gör
          </Link>
          <Link href="/kalite/raporlama">
            <TrendingUp aria-hidden="true" />
            Kalite Raporları
          </Link>
        </div>
      </Card>

      <footer className="production-execution-footer-note">
        <CheckCircle2 aria-hidden="true" />
        Kalite blokesi üretim emri durumundan ayrı izlenir; üretim durumu salt okunurdur.
      </footer>
    </main>
  );
}
