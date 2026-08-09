import Link from "next/link";
import {
  AlertTriangle,
  CalendarClock,
  ClipboardList,
  Clock,
  Settings,
  Wrench,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import type {
  MaintenanceRequestSummary,
  MaintenanceWorkOrderSummary,
  MaintenanceWorkspaceSummary,
  PreventiveMaintenancePlanSummary,
} from "../domain/maintenance";
import {
  buildMaintenanceCriticalAlerts,
  formatMaintenanceAge,
  getMaintenanceAssetTypeLabel,
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
import "./maintenance-workspace.css";

type MaintenanceDashboardViewProps = Readonly<{
  summary: MaintenanceWorkspaceSummary;
  requests: readonly MaintenanceRequestSummary[];
  workOrders: readonly MaintenanceWorkOrderSummary[];
  preventivePlans: readonly PreventiveMaintenancePlanSummary[];
}>;

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

      <section className="maintenance-dashboard-priority">
        <Card className="production-execution-panel" id="aktif-bakim-isleri">
          <header className="maintenance-dashboard-section__header">
            <h2>Aktif Bakım İşleri</h2>
          </header>
          {activeWorkOrders.length === 0 ? (
            <p className="maintenance-dashboard-empty">Aktif bakım emri bulunmuyor.</p>
          ) : (
            <div className="maintenance-table-wrap">
              <table className="maintenance-table maintenance-table--compact">
                <thead>
                  <tr>
                    <th>Emir No</th>
                    <th>Varlık</th>
                    <th>Tür</th>
                    <th>Öncelik</th>
                    <th>Durum</th>
                    <th>Teknisyen</th>
                    <th>Plan</th>
                  </tr>
                </thead>
                <tbody>
                  {activeWorkOrders.map((workOrder) => (
                    <tr key={workOrder.id}>
                      <td>
                        <Link href={`/bakim/emirler/${workOrder.id}`}>
                          <strong>{workOrder.workOrderNumber}</strong>
                        </Link>
                        <Link
                          className="maintenance-table__action"
                          href={`/bakim/emirler/${workOrder.id}`}
                        >
                          Bakım Emrini Aç
                        </Link>
                      </td>
                      <td>
                        <span className="maintenance-table__cell">
                          {workOrder.assetReference}
                        </span>
                        {workOrder.assetName ? (
                          <small>{workOrder.assetName}</small>
                        ) : null}
                      </td>
                      <td>{getMaintenanceTypeLabel(workOrder.type)}</td>
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
                      <td>{workOrder.assignedTechnicianSubjectId ?? "—"}</td>
                      <td>{formatDate(workOrder.plannedStartUtc)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </Card>

        <Card className="production-execution-panel">
          <header className="maintenance-dashboard-section__header">
            <h2>Kritik Arıza ve Bakım Uyarıları</h2>
          </header>
          {alerts.length === 0 ? (
            <p className="maintenance-dashboard-empty">Kritik uyarı bulunmuyor.</p>
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
                  <Link href={alert.href}>Detay</Link>
                </li>
              ))}
            </ul>
          )}
        </Card>
      </section>

      <section className="maintenance-dashboard-section" id="ariza-kayitlari">
        <Card className="production-execution-panel production-execution-panel--wide">
          <header className="maintenance-dashboard-section__header">
            <h2>Arıza Kayıtları</h2>
            <Link href="/bakim/arizalar/yeni">Yeni Arıza Kaydı</Link>
          </header>
          {openRequests.length === 0 ? (
            <p className="maintenance-dashboard-empty">Açık arıza kaydı bulunmuyor.</p>
          ) : (
            <div className="maintenance-table-wrap">
              <table className="maintenance-table maintenance-table--compact">
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
                        <strong>{request.requestNumber}</strong>
                      </td>
                      <td>
                        <span className="maintenance-table__cell">
                          {request.assetReference}
                        </span>
                        <small>{getMaintenanceAssetTypeLabel(request.assetType)}</small>
                      </td>
                      <td className="maintenance-table__cell--wrap">{request.title}</td>
                      <td>{getMaintenanceTypeLabel(request.type)}</td>
                      <td>
                        <Badge tone={getMaintenancePriorityTone(request.priority)}>
                          {getMaintenancePriorityLabel(request.priority)}
                        </Badge>
                      </td>
                      <td>{getMaintenanceStatusLabel(request.status)}</td>
                      <td>{formatMaintenanceAge(request.reportedAtUtc)}</td>
                      <td className="maintenance-table__actions">
                        <Link href={`/bakim/arizalar/${request.id}`}>İncele</Link>
                        {(request.status === "open" || request.status === "triaged") ? (
                          <Link href={`/bakim/arizalar/${request.id}?convert=1`}>
                            Bakım Emrine Dönüştür
                          </Link>
                        ) : null}
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
          <header className="maintenance-dashboard-section__header">
            <h2>Periyodik Bakım Planları</h2>
            <Link href="/bakim/planlar/yeni">Yeni Plan</Link>
          </header>
          {activePlans.length === 0 ? (
            <p className="maintenance-dashboard-empty">Aktif periyodik bakım planı bulunmuyor.</p>
          ) : (
            <div className="maintenance-table-wrap">
              <table className="maintenance-table maintenance-table--compact">
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
                      className={isOverduePreventivePlan(plan) ? "maintenance-table__row--urgent" : undefined}
                    >
                      <td>
                        <strong>{plan.code}</strong>
                      </td>
                      <td>{plan.name}</td>
                      <td>
                        <span className="maintenance-table__cell">{plan.assetReference}</span>
                        <small>{getMaintenanceAssetTypeLabel(plan.assetType)}</small>
                      </td>
                      <td>
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
                      <td>{getMaintenanceStatusLabel(plan.status)}</td>
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
