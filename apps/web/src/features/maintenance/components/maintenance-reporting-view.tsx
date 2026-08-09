import Link from "next/link";
import {
  AlertTriangle,
  BarChart3,
  Clock,
  Repeat,
  ShieldAlert,
  TrendingUp,
  Wrench,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import type {
  MaintenanceFailureBreakdownRow,
  MaintenanceReliabilityOverview,
  MaintenanceReliabilityReport,
} from "../domain/maintenance";
import {
  formatMaintenanceCurrency,
  formatMaintenanceDateTime,
  getMaintenanceConditionStatusLabel,
  getMaintenanceConditionStatusTone,
  getMaintenanceFailureCategoryLabel,
} from "../presentation/maintenance-labels";
import { MaintenanceDashboardKpi } from "./maintenance-dashboard-kpi";
import { MaintenanceWorkspaceNav } from "./maintenance-workspace-nav";
import "./maintenance-workspace.css";

type MaintenanceReportingViewProps = Readonly<{
  overview: MaintenanceReliabilityOverview;
  report: MaintenanceReliabilityReport;
  failureBreakdown: readonly MaintenanceFailureBreakdownRow[];
  range: Readonly<{ dateFromUtc: string; dateToUtc: string }>;
}>;

function formatMinutes(value?: number): string {
  if (value === undefined || Number.isNaN(value)) {
    return "—";
  }

  return `${Math.round(value)} dk`;
}

function formatPercent(value?: number): string {
  if (value === undefined || Number.isNaN(value)) {
    return "—";
  }

  return `%${value.toFixed(1)}`;
}

function AgingRow({
  label,
  buckets,
}: Readonly<{
  label: string;
  buckets: MaintenanceReliabilityReport["backlogAging"]["openRequests"];
}>) {
  return (
    <div className="maintenance-report-aging-row">
      <span>{label}</span>
      <div className="maintenance-report-aging-buckets">
        <span>0-1: {buckets.day0To1}</span>
        <span>2-3: {buckets.day2To3}</span>
        <span>4-7: {buckets.day4To7}</span>
        <span>8-14: {buckets.day8To14}</span>
        <span>15+: {buckets.day15Plus}</span>
      </div>
    </div>
  );
}

export function MaintenanceReportingView({
  overview,
  report,
  failureBreakdown,
  range,
}: MaintenanceReportingViewProps) {
  const cost = overview.costAnalytics;

  return (
    <main className="production-execution-page maintenance-page">
      <header className="maintenance-page-header production-execution-header">
        <div className="maintenance-page-header__intro">
          <p>BAKIM &amp; ARIZA</p>
          <h1>Bakım Güvenilirlik ve Analiz</h1>
          <span>
            Arıza tekrarlarını, bakım sürelerini, periyodik bakım uyumunu ve varlık güvenilirliğini
            izleyin.
          </span>
        </div>
      </header>

      <MaintenanceWorkspaceNav active="reporting" />

      <div className="maintenance-page-content">
        <p className="maintenance-report-period">
          Rapor dönemi: {formatMaintenanceDateTime(range.dateFromUtc)} —{" "}
          {formatMaintenanceDateTime(range.dateToUtc)}
        </p>

        <section className="maintenance-kpi-grid" aria-label="Güvenilirlik KPI">
          <MaintenanceDashboardKpi
            icon={<AlertTriangle />}
            label="Açık Arızalar"
            value={overview.openRequests}
            description="Açık arıza kayıtları"
            tone="paused"
          />
          <MaintenanceDashboardKpi
            icon={<ShieldAlert />}
            label="Kritik Bakım İşleri"
            value={overview.criticalOpenWorkOrders}
            description="Kritik öncelikli açık işler"
            tone="critical"
          />
          <MaintenanceDashboardKpi
            icon={<Repeat />}
            label="Tekrarlayan Arızalar"
            value={overview.repeatFaultCountPeriod}
            description="Dönem içi tekrar imzası"
            tone="critical"
          />
          <MaintenanceDashboardKpi
            icon={<Clock />}
            label="Ortalama Onarım Süresi"
            value={formatMinutes(overview.averageRepairDurationMinutes)}
            description="Uygun tamamlanan düzeltici bakım"
            tone="in-progress"
          />
          <MaintenanceDashboardKpi
            icon={<Wrench />}
            label="Geciken Periyodik Bakım"
            value={overview.overduePreventiveCount}
            description="Vadesi geçmiş periyodik bakım"
            tone="critical"
          />
          <MaintenanceDashboardKpi
            icon={<TrendingUp />}
            label="Periyodik Bakım Uyum Oranı"
            value={formatPercent(overview.preventiveCompletionRate)}
            description="Tamamlanan / vadesi gelen"
            tone="ready"
          />
        </section>

        <div className="maintenance-report-grid">
          <Card className="maintenance-section">
            <div className="maintenance-section__header">
              <BarChart3 aria-hidden="true" />
              <div>
                <h2>Güvenilirlik Göstergeleri</h2>
                <p>Deterministik MTTR ve dürüst MTBF durumu</p>
              </div>
            </div>
            <dl className="maintenance-report-metrics">
              <div>
                <dt>{report.mttr.label}</dt>
                <dd>{formatMinutes(report.mttr.meanRepairMinutes)}</dd>
                <small>
                  Uygun kayıt: {report.mttr.eligibleRepairCount}, hariç:{" "}
                  {report.mttr.excludedRepairCount}
                </small>
              </div>
              <div>
                <dt>MTBF</dt>
                {report.mtbf.mtbfAvailable ? (
                  <dd>{report.mtbf.mtbfHours ?? "—"} saat</dd>
                ) : (
                  <>
                    <dd>MTBF: Veri yetersiz</dd>
                    <small>
                      Güvenilir çalışma süresi kaynağı henüz bağlı değil. {report.mtbf.reason}
                    </small>
                  </>
                )}
              </div>
              <div>
                <dt>Periyodik Bakım Uyumu</dt>
                <dd>{formatPercent(report.preventiveCompliance.preventiveCompletionRate)}</dd>
                <small>{report.preventiveCompliance.ruleDescription}</small>
              </div>
            </dl>
          </Card>

          <Card className="maintenance-section">
            <div className="maintenance-section__header">
              <Clock aria-hidden="true" />
              <div>
                <h2>Bakım Backlog Yaşlandırma</h2>
                <p>Açık kayıtların gün bazlı dağılımı</p>
              </div>
            </div>
            <div className="maintenance-report-aging">
              <AgingRow label="Açık arızalar" buckets={report.backlogAging.openRequests} />
              <AgingRow label="Açık bakım işleri" buckets={report.backlogAging.openWorkOrders} />
              <AgingRow
                label="Kritik açık işler"
                buckets={report.backlogAging.criticalOpenWorkOrders}
              />
              <AgingRow label="Bekleyen işler" buckets={report.backlogAging.waitingWorkOrders} />
            </div>
          </Card>
        </div>

        <Card className="maintenance-section">
          <div className="maintenance-section__header">
            <Repeat aria-hidden="true" />
            <div>
              <h2>Tekrarlayan Arızalar</h2>
              <p>Aynı varlık ve arıza kategorisi imzası</p>
            </div>
          </div>
          <div className="maintenance-table-wrap">
            <table className="maintenance-table">
              <thead>
                <tr>
                  <th>Varlık</th>
                  <th>Arıza Kategorisi</th>
                  <th>Son 30 Gün</th>
                  <th>Son 90 Gün</th>
                  <th>Son Olay</th>
                  <th>Açık Bakım</th>
                  <th>Durum</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {report.repeatFailures.length === 0 ? (
                  <tr>
                    <td colSpan={8}>Tekrarlayan arıza kaydı yok.</td>
                  </tr>
                ) : (
                  report.repeatFailures.map((item) => (
                    <tr key={item.failureSignature}>
                      <td>{item.assetReference}</td>
                      <td>{getMaintenanceFailureCategoryLabel(item.failureCategory)}</td>
                      <td>{item.failuresLast30Days}</td>
                      <td>{item.failuresLast90Days}</td>
                      <td>
                        {item.lastOccurrenceUtc
                          ? formatMaintenanceDateTime(item.lastOccurrenceUtc)
                          : "—"}
                      </td>
                      <td>{item.openRelatedWorkOrderCount}</td>
                      <td>
                        <Badge tone={getMaintenanceConditionStatusTone(item.conditionStatus)}>
                          {getMaintenanceConditionStatusLabel(item.conditionStatus)}
                        </Badge>
                      </td>
                      <td>
                        <Link
                          href={`/bakim/varliklar/${encodeURIComponent(item.assetReference)}`}
                          className="maintenance-inline-link"
                        >
                          Varlık Geçmişini Aç
                        </Link>
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </Card>

        <Card className="maintenance-section">
          <div className="maintenance-section__header">
            <BarChart3 aria-hidden="true" />
            <div>
              <h2>Arıza Dağılımı</h2>
              <p>Kategori bazlı deterministik özet</p>
            </div>
          </div>
          <div className="maintenance-table-wrap">
            <table className="maintenance-table">
              <thead>
                <tr>
                  <th>Kategori</th>
                  <th>Arıza Sayısı</th>
                  <th>Tekrar</th>
                  <th>Ortalama Onarım</th>
                  <th>Pay</th>
                </tr>
              </thead>
              <tbody>
                {failureBreakdown.length === 0 ? (
                  <tr>
                    <td colSpan={5}>Seçili dönemde arıza kaydı yok.</td>
                  </tr>
                ) : (
                  failureBreakdown.map((row) => (
                    <tr key={row.groupKey}>
                      <td>{getMaintenanceFailureCategoryLabel(row.groupLabel)}</td>
                      <td>{row.count}</td>
                      <td>{row.repeatCount}</td>
                      <td>{formatMinutes(row.averageRepairMinutes)}</td>
                      <td>%{row.percentage.toFixed(1)}</td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </Card>

        {cost ? (
          <Card className="maintenance-section">
            <div className="maintenance-section__header">
              <TrendingUp aria-hidden="true" />
              <div>
                <h2>Bakım Maliyeti</h2>
                <p>
                  Veri tamlığı: {cost.dataCompletenessStatus} ({cost.recordsWithCost} kayıt /
                  {cost.recordsWithoutCost} eksik)
                </p>
              </div>
            </div>
            <dl className="maintenance-report-metrics">
              <div>
                <dt>Toplam Bakım Maliyeti</dt>
                <dd>{formatMaintenanceCurrency(cost.totalMaintenanceCost)}</dd>
              </div>
              <div>
                <dt>Bakım Emri Başına Ortalama</dt>
                <dd>{formatMaintenanceCurrency(cost.averageCostPerCompletedWorkOrder)}</dd>
              </div>
            </dl>
            {cost.topCostAssets.length > 0 ? (
              <div className="maintenance-table-wrap">
                <table className="maintenance-table">
                  <thead>
                    <tr>
                      <th>Varlık</th>
                      <th>Toplam Maliyet</th>
                    </tr>
                  </thead>
                  <tbody>
                    {cost.topCostAssets.map((asset) => (
                      <tr key={asset.assetReference}>
                        <td>{asset.assetReference}</td>
                        <td>{formatMaintenanceCurrency(asset.totalCost)}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : null}
          </Card>
        ) : null}
      </div>
    </main>
  );
}
