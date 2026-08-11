import Link from "next/link";
import type { ReactNode } from "react";
import {
  AlertTriangle,
  BrainCircuit,
  CalendarClock,
  CheckCircle2,
  ClipboardList,
  Clock,
  Info,
  Settings,
  Wrench,
  Activity,
  TrendingDown,
  Database,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import type {
  MaintenanceRequestSummary,
  MaintenanceWorkOrderSummary,
  MaintenanceWorkspaceSummary,
  PreventiveMaintenancePlanSummary,
} from "../domain/maintenance";
import type { MaintenanceConditionWorkspace } from "../domain/maintenance-condition";
import {
  buildMaintenanceCriticalAlerts,
  formatMaintenanceAge,
  getMaintenanceIntervalLabel,
  getMaintenancePriorityLabel,
  getMaintenancePriorityTone,
  getMaintenanceStatusLabel,
  getMaintenanceTypeLabel,
  getMaintenanceWorkOrderStatusTone,
  isActiveMaintenanceWorkOrder,
  isOpenMaintenanceRequest,
  isOverduePreventivePlan,
} from "../presentation/maintenance-labels";
import { MaintenanceDashboardKpi } from "./maintenance-dashboard-kpi";
import {
  MaintenanceAssetCell,
  MaintenanceEmptyState,
  MaintenanceSectionHeader,
  MaintenanceTableActionGroup,
  MaintenanceTableActionLink,
  MaintenanceTableIdentifierCell,
} from "./maintenance-table-primitives";
import { MaintenanceWorkspaceNav } from "./maintenance-workspace-nav";
import "./maintenance-workspace.css";

type MaintenanceDashboardViewProps = Readonly<{
  summary: MaintenanceWorkspaceSummary;
  requests: readonly MaintenanceRequestSummary[];
  workOrders: readonly MaintenanceWorkOrderSummary[];
  preventivePlans: readonly PreventiveMaintenancePlanSummary[];
  conditionWorkspace?: MaintenanceConditionWorkspace;
  conditionOverviewError?: string;
}>;

function sumActiveAnomalyCount(workspace: MaintenanceConditionWorkspace): number {
  return workspace.assets.reduce((total, asset) => total + asset.activeAnomalyCount, 0);
}

type PredictiveKpiTileProps = Readonly<{
  tone: "critical" | "degraded" | "watch" | "insufficient" | "anomaly";
  icon: ReactNode;
  label: string;
  value: number;
  description: string;
}>;

function PredictiveKpiTile({ tone, icon, label, value, description }: PredictiveKpiTileProps) {
  return (
    <article
      className={`maintenance-predictive-kpi-tile maintenance-predictive-kpi-tile--${tone}`}
      aria-label={`${label}: ${value}`}
    >
      <span className="maintenance-predictive-kpi-tile__accent" aria-hidden="true" />
      <span className="maintenance-predictive-kpi-tile__icon" aria-hidden="true">
        {icon}
      </span>
      <div className="maintenance-predictive-kpi-tile__content">
        <span className="maintenance-predictive-kpi-tile__label">{label}</span>
        <strong className="maintenance-predictive-kpi-tile__value">{value}</strong>
        <small className="maintenance-predictive-kpi-tile__desc">{description}</small>
      </div>
    </article>
  );
}

function formatDate(iso?: string): string {
  if (!iso) {
    return "—";
  }

  return new Date(iso).toLocaleDateString("tr-TR", {
    day: "2-digit",
    month: "short",
    year: "numeric",
  });
}

export function MaintenanceDashboardView({
  summary,
  requests,
  workOrders,
  preventivePlans,
  conditionWorkspace,
  conditionOverviewError,
}: MaintenanceDashboardViewProps) {
  const activeWorkOrders = [...workOrders]
    .filter((item) => isActiveMaintenanceWorkOrder(item.status))
    .sort((a, b) => b.updatedAtUtc.localeCompare(a.updatedAtUtc))
    .slice(0, 8);

  const openRequests = [...requests]
    .filter((item) => isOpenMaintenanceRequest(item.status))
    .sort((a, b) => b.updatedAtUtc.localeCompare(a.updatedAtUtc))
    .slice(0, 8);

  const activePlans = [...preventivePlans]
    .filter((plan) => plan.status === "active")
    .sort((a, b) => {
      const aDue = a.nextDueAtUtc ? Date.parse(a.nextDueAtUtc) : Number.MAX_SAFE_INTEGER;
      const bDue = b.nextDueAtUtc ? Date.parse(b.nextDueAtUtc) : Number.MAX_SAFE_INTEGER;
      return aDue - bDue;
    })
    .slice(0, 8);

  const alerts = buildMaintenanceCriticalAlerts({ requests, workOrders, preventivePlans });

  const conditionOverview = conditionWorkspace?.overview;
  const openAnomalyCount = conditionWorkspace ? sumActiveAnomalyCount(conditionWorkspace) : 0;

  return (
    <main className="production-execution-page maintenance-dashboard-page">
      <header className="maintenance-dashboard-header production-execution-header">
        <div className="maintenance-dashboard-header__intro">
          <p>BAKIM &amp; ARIZA</p>
          <h1>Bakım &amp; Arıza</h1>
          <span>
            Makine, kalıp ve yardımcı ekipman bakım işlerini ve arıza kayıtlarını yönetin.
          </span>
        </div>
        <div className="maintenance-dashboard-header__actions">
          <Link
            href="/bakim/arizalar/yeni"
            className="maintenance-dashboard-action maintenance-dashboard-action--primary"
          >
            <AlertTriangle aria-hidden="true" />
            Yeni Arıza Kaydı
          </Link>
          <Link
            href="/bakim/emirler/yeni"
            className="maintenance-dashboard-action maintenance-dashboard-action--secondary"
          >
            <Wrench aria-hidden="true" />
            Yeni Bakım Emri
          </Link>
          <Link
            href="/bakim/planlar/yeni"
            className="maintenance-dashboard-action maintenance-dashboard-action--tertiary"
          >
            <CalendarClock aria-hidden="true" />
            Periyodik Bakım Planı
          </Link>
        </div>
      </header>

      <MaintenanceWorkspaceNav active="overview" />

      <section
        className="production-execution-kpis maintenance-dashboard-kpis"
        aria-label="Bakım göstergeleri"
      >
        <MaintenanceDashboardKpi
          href="#ariza-kayitlari"
          tone="paused"
          icon={<AlertTriangle />}
          label="Açık Arızalar"
          value={summary.openRequestCount}
          description="Açık arıza kayıtları"
        />
        <MaintenanceDashboardKpi
          href="#aktif-bakim-isleri"
          tone="in-progress"
          icon={<ClipboardList />}
          label="Açık Bakım Emirleri"
          value={summary.openWorkOrderCount}
          description="Aktif bakım emirleri"
        />
        <MaintenanceDashboardKpi
          tone="critical"
          icon={<Settings />}
          label="Kritik Bakımlar"
          value={summary.criticalWorkOrderCount}
          description="Kritik öncelikli emirler"
        />
        <MaintenanceDashboardKpi
          tone="in-progress"
          icon={<Wrench />}
          label="Devam Eden"
          value={summary.inProgressCount}
          description="Çalışması devam eden"
        />
        <MaintenanceDashboardKpi
          tone="ready"
          icon={<Clock />}
          label="Bekleyen"
          value={summary.waitingCount}
          description="Beklemedeki emirler"
        />
        <MaintenanceDashboardKpi
          href="#periyodik-bakim-planlari"
          tone="critical"
          icon={<CalendarClock />}
          label="Geciken Periyodik Bakım"
          value={summary.overduePreventiveCount}
          description="Vadesi geçmiş planlar"
        />
      </section>

      <Card className="maintenance-predictive-overview-card production-execution-panel">
        <header className="maintenance-predictive-overview-card__header">
          <div className="maintenance-predictive-overview-card__intro">
            <span className="maintenance-predictive-overview-card__badge" aria-hidden="true">
              <BrainCircuit />
            </span>
            <div className="maintenance-predictive-overview-card__copy">
              <h2 className="maintenance-predictive-overview-card__title">Kestirici Bakım Uyarıları</h2>
              <p className="maintenance-predictive-overview-card__subtitle">
                Edge gözlemleri ve bakım geçmişinden türetilen durum, eğilim ve anomali kanıtları.
              </p>
            </div>
          </div>
          <Link
            href="/bakim/durum-zekasi"
            className="maintenance-predictive-overview-card__cta maintenance-dashboard-action maintenance-dashboard-action--secondary"
          >
            <Activity aria-hidden="true" />
            Kestirici Bakımı İncele
          </Link>
        </header>

        <div className="maintenance-predictive-overview-card__body">
          {conditionOverviewError ? (
            <div role="alert" className="maintenance-predictive-overview-card__error">
              <p>{conditionOverviewError}</p>
            </div>
          ) : conditionOverview ? (
            <>
              {conditionOverview.monitoredAssetCount === 0 ? (
                <p className="maintenance-predictive-overview-card__empty">
                  Henüz izlenen varlık yok. Durum profilleri eşleştirilmiş makine/varlıklar için
                  görüntülenir.
                </p>
              ) : null}
              <section
                className="maintenance-predictive-overview-kpis"
                aria-label="Kestirici bakım durum özeti"
              >
                <PredictiveKpiTile
                  tone="critical"
                  icon={<AlertTriangle />}
                  label="Kritik"
                  value={conditionOverview.criticalCount}
                  description="Kritik sapma profili"
                />
                <PredictiveKpiTile
                  tone="degraded"
                  icon={<TrendingDown />}
                  label="Bozulma Eğilimi"
                  value={conditionOverview.degradedCount}
                  description="Sapma eğilimi"
                />
                <PredictiveKpiTile
                  tone="watch"
                  icon={<Info />}
                  label="İzlenmeli"
                  value={conditionOverview.watchCount}
                  description="İzleme gerektiren"
                />
                <PredictiveKpiTile
                  tone="insufficient"
                  icon={<Database />}
                  label="Yetersiz Veri"
                  value={conditionOverview.insufficientDataCount}
                  description="Yetersiz örnek"
                />
                <PredictiveKpiTile
                  tone="anomaly"
                  icon={<Activity />}
                  label="Açık Anomali"
                  value={openAnomalyCount}
                  description="Aktif anomali kaydı"
                />
              </section>
            </>
          ) : null}
        </div>
      </Card>

      <section className="maintenance-dashboard-priority">
        <Card className="production-execution-panel" id="aktif-bakim-isleri">
          <MaintenanceSectionHeader title="Aktif Bakım İşleri" />
          {activeWorkOrders.length === 0 ? (
            <MaintenanceEmptyState
              icon={<ClipboardList aria-hidden="true" size={18} />}
              title="Aktif bakım emri bulunmuyor"
              description="Şu anda devam eden veya planlanmış bakım emri yok."
            />
          ) : (
            <div className="maintenance-table-wrap maintenance-table-wrap--desktop-fit">
              <table className="maintenance-table maintenance-table--compact maintenance-table--work-orders">
                <colgroup>
                  <col className="maintenance-table__col--order" />
                  <col className="maintenance-table__col--asset" />
                  <col className="maintenance-table__col--type" />
                  <col className="maintenance-table__col--priority" />
                  <col className="maintenance-table__col--status" />
                  <col className="maintenance-table__col--technician" />
                  <col className="maintenance-table__col--date" />
                  <col className="maintenance-table__col--actions" />
                </colgroup>
                <thead>
                  <tr>
                    <th>Emir No</th>
                    <th>Varlık</th>
                    <th>Tür</th>
                    <th>Öncelik</th>
                    <th>Durum</th>
                    <th>Teknisyen</th>
                    <th>Plan</th>
                    <th>İşlem</th>
                  </tr>
                </thead>
                <tbody>
                  {activeWorkOrders.map((workOrder) => (
                    <tr key={workOrder.id}>
                      <td>
                        <MaintenanceTableIdentifierCell
                          href={`/bakim/emirler/${workOrder.id}`}
                          primary={<strong>{workOrder.workOrderNumber}</strong>}
                        />
                      </td>
                      <td>
                        <MaintenanceAssetCell
                          assetReference={workOrder.assetReference}
                          assetType={workOrder.assetType}
                          assetName={workOrder.assetName}
                          href={`/bakim/varliklar/${encodeURIComponent(workOrder.assetReference)}`}
                        />
                      </td>
                      <td className="maintenance-table__cell--compact">
                        {getMaintenanceTypeLabel(workOrder.type)}
                      </td>
                      <td>
                        <Badge tone={getMaintenancePriorityTone(workOrder.priority)}>
                          {getMaintenancePriorityLabel(workOrder.priority)}
                        </Badge>
                      </td>
                      <td>
                        <Badge tone={getMaintenanceWorkOrderStatusTone(workOrder.status)}>
                          {getMaintenanceStatusLabel(workOrder.status)}
                        </Badge>
                      </td>
                      <td className="maintenance-table__cell--compact">
                        {workOrder.assignedTechnicianSubjectId ?? "—"}
                      </td>
                      <td className="maintenance-table__cell--compact">
                        {formatDate(workOrder.plannedStartUtc)}
                      </td>
                      <td>
                        <MaintenanceTableActionGroup>
                          <MaintenanceTableActionLink href={`/bakim/emirler/${workOrder.id}`}>
                            Aç
                          </MaintenanceTableActionLink>
                        </MaintenanceTableActionGroup>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </Card>

        <Card className="production-execution-panel maintenance-dashboard-alerts-panel">
          <MaintenanceSectionHeader title="Kritik Arıza ve Bakım Uyarıları" />
          {alerts.length === 0 ? (
            <MaintenanceEmptyState
              icon={<CheckCircle2 aria-hidden="true" size={18} />}
              title="Kritik bakım uyarısı yok"
              description="Şu anda acil müdahale gerektiren kayıt bulunmuyor."
            />
          ) : (
            <ul className="maintenance-alert-list">
              {alerts.map((alert) => (
                <li key={alert.id} className="maintenance-alert-item">
                  <div
                    className={
                      alert.tone === "danger"
                        ? "maintenance-alert-item__icon maintenance-alert-item__icon--danger"
                        : "maintenance-alert-item__icon maintenance-alert-item__icon--warning"
                    }
                  >
                    <AlertTriangle aria-hidden="true" size={16} />
                  </div>
                  <div className="maintenance-alert-item__body">
                    <strong>{alert.title}</strong>
                    <span>
                      {alert.reference} · {alert.reason}
                    </span>
                    <Badge tone={alert.tone}>{alert.statusLabel}</Badge>
                  </div>
                  <MaintenanceTableActionLink href={alert.href}>Detay</MaintenanceTableActionLink>
                </li>
              ))}
            </ul>
          )}
        </Card>
      </section>

      <section className="maintenance-dashboard-section" id="ariza-kayitlari">
        <Card className="production-execution-panel production-execution-panel--wide">
          <MaintenanceSectionHeader
            title="Arıza Kayıtları"
            actionHref="/bakim/arizalar/yeni"
            actionLabel="Yeni Arıza Kaydı"
          />
          {openRequests.length === 0 ? (
            <MaintenanceEmptyState
              icon={<AlertTriangle aria-hidden="true" size={18} />}
              title="Açık arıza kaydı bulunmuyor"
              description="Yeni bir arıza kaydı oluşturarak süreci başlatabilirsiniz."
            />
          ) : (
            <div className="maintenance-table-wrap maintenance-table-wrap--desktop-fit">
              <table className="maintenance-table maintenance-table--compact maintenance-table--requests">
                <colgroup>
                  <col className="maintenance-table__col--order" />
                  <col className="maintenance-table__col--asset" />
                  <col className="maintenance-table__col--title" />
                  <col className="maintenance-table__col--type" />
                  <col className="maintenance-table__col--priority" />
                  <col className="maintenance-table__col--status" />
                  <col className="maintenance-table__col--age" />
                  <col className="maintenance-table__col--actions-wide" />
                </colgroup>
                <thead>
                  <tr>
                    <th>Kayıt No</th>
                    <th>Varlık</th>
                    <th>Başlık</th>
                    <th>Tür</th>
                    <th>Öncelik</th>
                    <th>Durum</th>
                    <th>Yaş</th>
                    <th>İşlem</th>
                  </tr>
                </thead>
                <tbody>
                  {openRequests.map((request) => (
                    <tr key={request.id}>
                      <td>
                        <MaintenanceTableIdentifierCell
                          href={`/bakim/arizalar/${request.id}`}
                          primary={<strong>{request.requestNumber}</strong>}
                        />
                      </td>
                      <td>
                        <MaintenanceAssetCell
                          assetReference={request.assetReference}
                          assetType={request.assetType}
                          assetName={request.assetName}
                          href={`/bakim/varliklar/${encodeURIComponent(request.assetReference)}`}
                        />
                      </td>
                      <td className="maintenance-table__cell--wrap">{request.title}</td>
                      <td className="maintenance-table__cell--compact">
                        {getMaintenanceTypeLabel(request.type)}
                      </td>
                      <td>
                        <Badge tone={getMaintenancePriorityTone(request.priority)}>
                          {getMaintenancePriorityLabel(request.priority)}
                        </Badge>
                      </td>
                      <td>
                        <Badge tone="neutral">{getMaintenanceStatusLabel(request.status)}</Badge>
                      </td>
                      <td className="maintenance-table__cell--compact">
                        {formatMaintenanceAge(request.reportedAtUtc)}
                      </td>
                      <td>
                        <MaintenanceTableActionGroup>
                          <MaintenanceTableActionLink href={`/bakim/arizalar/${request.id}`}>
                            İncele
                          </MaintenanceTableActionLink>
                          {request.status === "open" || request.status === "triaged" ? (
                            <MaintenanceTableActionLink
                              href={`/bakim/arizalar/${request.id}?convert=1`}
                              title="Bakım Emrine Dönüştür"
                            >
                              Dönüştür
                            </MaintenanceTableActionLink>
                          ) : null}
                        </MaintenanceTableActionGroup>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </Card>
      </section>

      <section className="maintenance-dashboard-section" id="periyodik-bakim-planlari">
        <Card className="production-execution-panel production-execution-panel--wide">
          <MaintenanceSectionHeader
            title="Periyodik Bakım Planları"
            actionHref="/bakim/planlar/yeni"
            actionLabel="Yeni Plan"
          />
          {activePlans.length === 0 ? (
            <MaintenanceEmptyState
              icon={<CalendarClock aria-hidden="true" size={18} />}
              title="Aktif periyodik bakım planı bulunmuyor"
              description="Periyodik bakım planı ekleyerek vadeleri otomatik izleyin."
            />
          ) : (
            <div className="maintenance-table-wrap maintenance-table-wrap--desktop-fit">
              <table className="maintenance-table maintenance-table--compact maintenance-table--preventive">
                <colgroup>
                  <col className="maintenance-table__col--code" />
                  <col className="maintenance-table__col--plan" />
                  <col className="maintenance-table__col--asset" />
                  <col className="maintenance-table__col--interval" />
                  <col className="maintenance-table__col--date" />
                  <col className="maintenance-table__col--status" />
                </colgroup>
                <thead>
                  <tr>
                    <th>Kod</th>
                    <th>Plan</th>
                    <th>Varlık</th>
                    <th>Periyot</th>
                    <th>Sonraki Bakım</th>
                    <th>Durum</th>
                  </tr>
                </thead>
                <tbody>
                  {activePlans.map((plan) => (
                    <tr
                      key={plan.id}
                      className={
                        isOverduePreventivePlan(plan) ? "maintenance-table__row--urgent" : undefined
                      }
                    >
                      <td>
                        <MaintenanceTableIdentifierCell primary={<strong>{plan.code}</strong>} />
                      </td>
                      <td className="maintenance-table__cell--wrap">{plan.name}</td>
                      <td>
                        <MaintenanceAssetCell
                          assetReference={plan.assetReference}
                          assetType={plan.assetType}
                        />
                      </td>
                      <td className="maintenance-table__cell--compact">
                        {getMaintenanceIntervalLabel(plan.intervalType, plan.intervalValue)}
                      </td>
                      <td>
                        {plan.nextDueAtUtc ? (
                          <Badge tone={isOverduePreventivePlan(plan) ? "danger" : "neutral"}>
                            {formatDate(plan.nextDueAtUtc)}
                          </Badge>
                        ) : (
                          "—"
                        )}
                      </td>
                      <td>
                        <Badge tone="neutral">{getMaintenanceStatusLabel(plan.status)}</Badge>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </Card>
      </section>
    </main>
  );
}
