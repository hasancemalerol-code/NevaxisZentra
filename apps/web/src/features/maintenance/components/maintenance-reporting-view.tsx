import { enumLabel } from "@/lib/presentation/enum-labels";
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
import { customerMessage } from "@/lib/presentation/customer-message";

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

function formatReportDay(iso: string): string {
  return new Date(iso).toLocaleDateString("tr-TR", { day: "2-digit", month: "short", year: "numeric" });
}

function formatPercent(value?: number): string {
  if (value === undefined || Number.isNaN(value)) {
    return "—";
  }

  return `%${value.toLocaleString("tr-TR", { minimumFractionDigits: 1, maximumFractionDigits: 1 })}`;
}

function AgingRow({
  label,
  buckets,
}: Readonly<{
  label: string;
  buckets: MaintenanceReliabilityReport["backlogAging"]["openRequests"];
}>) {
  const bucketList = [
    { name: "0–1 gün", count: buckets.day0To1 },
    { name: "2–3 gün", count: buckets.day2To3 },
    { name: "4–7 gün", count: buckets.day4To7 },
    { name: "8–14 gün", count: buckets.day8To14 },
    { name: "15+ gün", count: buckets.day15Plus },
  ];
  const peak = Math.max(1, ...bucketList.map((bucket) => bucket.count));

  return (
    <div className="maintenance-report-aging-row">
      <span>{label}</span>
      <div className="maintenance-report-aging-buckets" role="img" aria-label={`${label}: ${bucketList.map((bucket) => `${bucket.name} ${bucket.count}`).join(", ")}`}>
        {bucketList.map((bucket) => (
          <span className="maintenance-report-aging-bucket" key={bucket.name} data-empty={bucket.count === 0}>
            <i aria-hidden="true" style={{ height: `${Math.max(4, Math.round((bucket.count / peak) * 28))}px` }} />
            <b>{bucket.count}</b>
            <small>{bucket.name}</small>
          </span>
        ))}
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
  const costEmpty =
    !cost ||
    (cost.totalMaintenanceCost === undefined &&
      cost.averageCostPerCompletedWorkOrder === undefined &&
      cost.topCostAssets.length === 0);
  const compliance = report.preventiveCompliance;
  const completedCount = compliance.completedOnTimeCount + compliance.completedLateCount;
  // A rate is only shown once at least one due maintenance was completed; otherwise the counts say what is going on.
  const hasPreventiveData = compliance.dueCount > 0 && completedCount > 0;
  const preventiveNote =
    compliance.dueCount > 0 && completedCount === 0
      ? "Henüz tamamlanan yok"
      : "Vadesi gelen bakım yok";

  return (
    <div className="production-execution-page maintenance-page">
      <header className="maintenance-page-header production-execution-header">
        <div className="maintenance-page-header__intro">
          <p>BAKIM VE ARIZA</p>
          <h1>Raporlama</h1>
          <span>
            Bakım güvenilirliği: arıza tekrarları, bakım süreleri, periyodik bakım uyumu ve varlık
            güvenilirliği.
          </span>
        </div>
      </header>

      <MaintenanceWorkspaceNav active="reporting" />

      <div className="maintenance-page-content">
        <p className="maintenance-report-period">
          Rapor dönemi: {formatReportDay(range.dateFromUtc)} — {formatReportDay(range.dateToUtc)}
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
            description="Dönem içinde yinelenen arıza sayısı"
            tone="critical"
          />
          <MaintenanceDashboardKpi
            icon={<Clock />}
            label="Ort. Onarım Süresi"
            value={formatMinutes(overview.averageRepairDurationMinutes)}
            description="Tamamlanan düzeltici bakım"
            tone="in-progress"
          />
          <MaintenanceDashboardKpi
            icon={<Wrench />}
            label="Geciken Bakım"
            value={overview.overduePreventiveCount}
            description="Vadesi geçmiş periyodik bakım"
            tone="critical"
          />
          <MaintenanceDashboardKpi
            icon={<TrendingUp />}
            label="Bakım Uyum Oranı"
            value={hasPreventiveData ? formatPercent(overview.preventiveCompletionRate) : "—"}
            description={hasPreventiveData ? "Tamamlanan / vadesi gelen" : preventiveNote}
            tone="ready"
          />
        </section>

        <div className="maintenance-report-grid">
          <Card className="maintenance-section">
            <div className="maintenance-section__header">
              <BarChart3 aria-hidden="true" />
              <div>
                <h2>Güvenilirlik Göstergeleri</h2>
                <p>Ortalama onarım süresi (MTTR) ve arızalar arası süre (MTBF) durumu</p>
              </div>
            </div>
            <dl className="maintenance-report-metrics">
              <div>
                <dt>{/[a-z]/i.test(report.mttr.label) && /eligible|completed/i.test(report.mttr.label) ? "MTTR (ort. onarım)" : report.mttr.label}</dt>
                <dd>{formatMinutes(report.mttr.meanRepairMinutes)}</dd>
                <dd>
                  <small>
                    Hesaba katılan onarım: {report.mttr.eligibleRepairCount}, dışarıda bırakılan:{" "}
                    {report.mttr.excludedRepairCount}
                  </small>
                </dd>
              </div>
              <div>
                <dt>MTBF</dt>
                {report.mtbf.mtbfAvailable ? (
                  <dd>{report.mtbf.mtbfHours ?? "—"} saat</dd>
                ) : (
                  <>
                    <dd className="maintenance-report-metrics__muted">—</dd>
                    <dd>
                      <small>
                        Veri yetersiz. Güvenilir çalışma süresi kaynağı henüz bağlı değil.{" "}
                        {customerMessage(report.mtbf.reason, "MTBF, güvenilir bir çalışma süresi kaynağı bağlandığında hesaplanır.")}
                      </small>
                    </dd>
                  </>
                )}
              </div>
              <div>
                <dt>Periyodik Bakım Uyumu</dt>
                <dd>{hasPreventiveData ? formatPercent(report.preventiveCompliance.preventiveCompletionRate) : "—"}</dd>
                <dd>
                  <small>
                    {customerMessage(
                      report.preventiveCompliance.ruleDescription,
                      "Dönem içinde vadesi gelen periyodik bakımların zamanında tamamlanma oranı.",
                    )}
                  </small>
                </dd>
              </div>
            </dl>
          </Card>

          <Card className="maintenance-section">
            <div className="maintenance-section__header">
              <Clock aria-hidden="true" />
              <div>
                <h2>Bakım iş yükü yaşlandırma</h2>
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
            {cost && costEmpty ? (
              <p className="maintenance-report-note">Bakım maliyeti: bu dönem için henüz maliyet girilmedi.</p>
            ) : null}
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
          <div className="maintenance-table-wrap" tabIndex={0} role="region" aria-label="Tekrarlayan Arızalar (kaydırılabilir)">
            <table aria-label="Tekrarlayan Arızalar" className="maintenance-table maintenance-table--report">
              <thead>
                <tr>
                  <th>Varlık</th>
                  <th>Arıza Kategorisi</th>
                  <th>Son 30 Gün</th>
                  <th className="maintenance-col--low">Son 90 Gün</th>
                  <th>Son Olay</th>
                  <th className="maintenance-col--low">Açık Bakım</th>
                  <th>Durum</th>
                  <th>İşlemler</th>
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
                      <td className="maintenance-col--low">{item.failuresLast90Days}</td>
                      <td>
                        {item.lastOccurrenceUtc
                          ? formatMaintenanceDateTime(item.lastOccurrenceUtc)
                          : "—"}
                      </td>
                      <td className="maintenance-col--low">{item.openRelatedWorkOrderCount}</td>
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

          <div className="maintenance-section__header maintenance-section__header--sub">
            <BarChart3 aria-hidden="true" />
            <div>
              <h2>Arıza Dağılımı</h2>
              <p>Kategori bazlı özet</p>
            </div>
          </div>
          <div className="maintenance-table-wrap" tabIndex={0} role="region" aria-label="Arıza Dağılımı (kaydırılabilir)">
            <table aria-label="Arıza Dağılımı" className="maintenance-table maintenance-table--report">
              <thead>
                <tr>
                  <th>Kategori</th>
                  <th>Arıza Sayısı</th>
                  <th>Tekrar</th>
                  <th>Ort. Onarım</th>
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
                      <td>%{row.percentage.toLocaleString("tr-TR", { minimumFractionDigits: 1, maximumFractionDigits: 1 })}</td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </Card>

        {cost && !costEmpty ? (
          <Card className="maintenance-section">
            <div className="maintenance-section__header">
              <TrendingUp aria-hidden="true" />
              <div>
                <h2>Bakım Maliyeti</h2>
                <p>
                  Veri tamlığı: {enumLabel(cost.dataCompletenessStatus)} · {cost.recordsWithCost} kayıtta
                  maliyet var, {cost.recordsWithoutCost} kayıtta eksik
                </p>
              </div>
            </div>
            {cost.totalMaintenanceCost === undefined && cost.averageCostPerCompletedWorkOrder === undefined ? (
              <p className="maintenance-empty-state">Bu dönem için henüz maliyet girilmedi.</p>
            ) : (
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
            )}
            {cost.topCostAssets.length > 0 ? (
              <div className="maintenance-table-wrap" tabIndex={0} role="region" aria-label="Bakım Maliyeti (kaydırılabilir)">
                <table aria-label="Bakım Maliyeti" className="maintenance-table">
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
    </div>
  );
}
