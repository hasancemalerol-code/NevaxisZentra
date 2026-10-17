import { FACILITY_TIME_ZONE } from "@/lib/presentation/format";
import Link from "next/link";
import { notFound } from "next/navigation";
import { ArrowLeft, Factory } from "lucide-react";

import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { isMaintenanceRecordId } from "@/features/maintenance/lib/maintenance-record-id";

import { Badge } from "@/components/ui/badge";
import {
  MaintenanceWorkOrderCostSummaryPanel,
  MaintenanceWorkOrderNotesPanel,
  MaintenanceWorkOrderPartsPanel,
  MaintenanceWorkOrderPreventiveSourcePanel,
  MaintenanceWorkOrderTimelinePanel,
} from "@/features/maintenance/components/maintenance-work-order-execution-panels";
import "@/features/maintenance/components/maintenance-workspace.css";
import {
  formatMaintenanceAge,
  getMaintenanceAssetTypeLabel,
  getMaintenancePriorityLabel,
  getMaintenancePriorityTone,
  getMaintenanceResolutionCodeLabel,
  getMaintenanceStatusLabel,
  getMaintenanceTypeLabel,
  getMaintenanceWorkOrderStatusTone,
  describeMaintenanceActivity,
} from "@/features/maintenance/presentation/maintenance-labels";
import {
  readMaintenanceWorkOrderDetail,
  readMaintenanceWorkOrderExecution,
} from "@/features/maintenance/server/maintenance-reader";
import {
  createMaintenancePartUsageAction,
  createMaintenanceWorkNoteAction,
} from "@/features/maintenance/server/maintenance-writer";

import { displayActor } from "@/lib/presentation/display-label";
import { FailureStatePage } from "@/components/ui/failure-state";
import { enumLabel } from "@/lib/presentation/enum-labels";

type PageProps = Readonly<{
  params: Promise<{ id: string }>;
}>;

function formatDateTime(iso?: string, emptyLabel = "—"): string {
  if (!iso) {
    return emptyLabel;
  }

  return new Date(iso).toLocaleString("tr-TR", {
    day: "2-digit",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
    timeZone: FACILITY_TIME_ZONE,
  });
}

function withAgo(age: string): string {
  return age === "Az önce" || age === "—" ? age : `${age} önce`;
}

export default async function MaintenanceWorkOrderDetailPage({ params }: PageProps) {
  await forbidUnlessGranted("maintenance.read");

  const { id } = await params;
  if (id === "yeni" || !isMaintenanceRecordId(id)) {
    notFound();
  }

  const [result, executionResult] = await Promise.all([
    readMaintenanceWorkOrderDetail(id),
    readMaintenanceWorkOrderExecution(id),
  ]);

  if (result.status === "failure") {
    if (result.code === "not-found" || result.code === "permission-denied") {
      notFound();
    }

    return (
      <div className="production-execution-page maintenance-page">
        <header className="maintenance-page-header production-execution-header">
          <Link href="/bakim" className="maintenance-back-link">
            <ArrowLeft aria-hidden="true" />
            Bakım ana sayfası
          </Link>
          <div className="maintenance-page-header__intro">
            <p>BAKIM VE ARIZA</p>
            <h1>Bakım Emri Detayı</h1>
          </div>
        </header>
        <FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Bakım emri yüklenemedi" inline />
      </div>
    );
  }

  const workOrder = result.workOrder;
  if (!workOrder.id) {
    notFound();
  }

  const execution =
    executionResult.status === "success"
      ? executionResult
      : {
          notes: [],
          parts: [],
          timeline: [],
          costSummary: null,
        };

  const hasPlanningData = Boolean(
    workOrder.assignedTechnicianSubjectId ||
      workOrder.plannedStartUtc ||
      workOrder.plannedEndUtc ||
      workOrder.startedAtUtc ||
      workOrder.completedAtUtc ||
      workOrder.resolutionCode ||
      workOrder.completionNote,
  );

  return (
    <div className="production-execution-page maintenance-page">
      <header className="maintenance-page-header production-execution-header">
        <Link href="/bakim" className="maintenance-back-link">
          <ArrowLeft aria-hidden="true" />
          Bakım ana sayfası
        </Link>
        <div className="maintenance-page-header__intro">
          <p>BAKIM VE ARIZA</p>
          <h1>{workOrder.workOrderNumber}</h1>
          <span>{workOrder.title}</span>
        </div>
      </header>

      <div className="maintenance-page-content">
        <section className="maintenance-section" aria-label="Bakım Emri Özeti">
          <header className="maintenance-section__header">
            <h2>Bakım Emri Özeti</h2>
          </header>
          <dl className="maintenance-detail-dl maintenance-detail-dl--summary">
            <div>
              <dt>Durum</dt>
              <dd>
                <Badge tone={getMaintenanceWorkOrderStatusTone(workOrder.status)}>
                  {getMaintenanceStatusLabel(workOrder.status)}
                </Badge>
              </dd>
            </div>
            <div>
              <dt>Öncelik</dt>
              <dd>
                <Badge tone={getMaintenancePriorityTone(workOrder.priority)}>
                  {getMaintenancePriorityLabel(workOrder.priority)}
                </Badge>
              </dd>
            </div>
            <div>
              <dt>Tür</dt>
              <dd>
                <Badge tone="neutral">{getMaintenanceTypeLabel(workOrder.type)}</Badge>
              </dd>
            </div>
            <div>
              <dt>Emir No</dt>
              <dd>{workOrder.workOrderNumber}</dd>
            </div>
            <div>
              <dt>Oluşturan</dt>
              <dd>{displayActor(workOrder.createdBySubjectId)}</dd>
            </div>
            <div>
              <dt>Oluşturulma</dt>
              <dd>{formatDateTime(workOrder.createdAtUtc)}</dd>
            </div>
            <div className="maintenance-detail-dl__wide">
              <dt>Açıklama</dt>
              <dd>{workOrder.description || "Açıklama girilmedi"}</dd>
            </div>
          </dl>
          <p className="maintenance-detail-note">Son Güncelleme: {withAgo(formatMaintenanceAge(workOrder.updatedAtUtc))}</p>
        </section>

        <div className="maintenance-detail-grid">
          <section className="maintenance-section maintenance-section--wide" aria-label="Varlık ve Bağlam">
            <header className="maintenance-section__header">
              <h2>Varlık ve Bağlam</h2>
              <Link href={`/bakim/varliklar/${encodeURIComponent(workOrder.assetReference)}`}>
                Varlık Geçmişi
              </Link>
            </header>
            <dl className="maintenance-detail-dl">
              <div>
                <dt>Varlık Türü</dt>
                <dd>{getMaintenanceAssetTypeLabel(workOrder.assetType)}</dd>
              </div>
              <div>
                <dt>Referans</dt>
                <dd>{workOrder.assetReference}</dd>
              </div>
              <div>
                <dt>Varlık Adı</dt>
                <dd>{workOrder.assetName ?? "Adı girilmedi"}</dd>
              </div>
              <div>
                <dt>Talep No</dt>
                <dd>
                  {workOrder.maintenanceRequestId ? (
                    <Link href={`/bakim/arizalar/${workOrder.maintenanceRequestId}`}>
                      {workOrder.maintenanceRequestNumber ?? "Arıza talebini aç"}
                    </Link>
                  ) : (
                    workOrder.maintenanceRequestNumber ?? "Talepten oluşturulmadı"
                  )}
                </dd>
              </div>
              <div>
                <dt>Üretim Emri</dt>
                <dd>
                  {workOrder.productionOrderReference
                    ? `${workOrder.productionOrderReference}${workOrder.productionOrderStatus ? ` · ${enumLabel(workOrder.productionOrderStatus)}` : ""}`
                    : "Bağlı üretim emri yok"}
                </dd>
              </div>
            </dl>
          </section>

          <section className="maintenance-section maintenance-section--wide" aria-label="Planlama ve Yürütme">
            <header className="maintenance-section__header">
              <h2>Planlama ve Yürütme</h2>
            </header>
            {hasPlanningData ? (
            <dl className="maintenance-detail-dl">
              <div>
                <dt>Atanan Teknisyen</dt>
                <dd>{workOrder.assignedTechnicianSubjectId ? displayActor(workOrder.assignedTechnicianSubjectId, "Atanan teknisyen") : "Atanmadı"}</dd>
              </div>
              <div>
                <dt>Planlanan Başlangıç</dt>
                <dd>{formatDateTime(workOrder.plannedStartUtc, "Planlanmadı")}</dd>
              </div>
              <div>
                <dt>Planlanan Bitiş</dt>
                <dd>{formatDateTime(workOrder.plannedEndUtc, "Planlanmadı")}</dd>
              </div>
              <div>
                <dt>Fiili Başlangıç</dt>
                <dd>{formatDateTime(workOrder.startedAtUtc, "Henüz başlamadı")}</dd>
              </div>
              <div>
                <dt>Tamamlanma</dt>
                <dd>{formatDateTime(workOrder.completedAtUtc, "Henüz tamamlanmadı")}</dd>
              </div>
              {workOrder.resolutionCode ? (
                <div>
                  <dt>Çözüm Kodu</dt>
                  <dd>{getMaintenanceResolutionCodeLabel(workOrder.resolutionCode)}</dd>
                </div>
              ) : null}
              {workOrder.completionNote ? (
                <div className="maintenance-detail-dl__wide">
                  <dt>Tamamlama Notu</dt>
                  <dd>{workOrder.completionNote}</dd>
                </div>
              ) : null}
            </dl>
            ) : (
              <p className="maintenance-empty-state">
                Teknisyen ataması ve planlama bekleniyor; henüz başlangıç, bitiş veya tamamlanma kaydı yok.
              </p>
            )}
          </section>

          <MaintenanceWorkOrderPreventiveSourcePanel workOrder={workOrder} />

          <MaintenanceWorkOrderCostSummaryPanel
            costSummary={execution.costSummary}
            workOrder={workOrder}
          />

          <MaintenanceWorkOrderNotesPanel
            workOrderId={workOrder.id}
            notes={execution.notes}
            onCreateNote={createMaintenanceWorkNoteAction}
          />

          <MaintenanceWorkOrderPartsPanel
            workOrderId={workOrder.id}
            parts={execution.parts}
            onCreatePart={createMaintenancePartUsageAction}
          />

          <MaintenanceWorkOrderTimelinePanel timeline={execution.timeline} />

          {execution.timeline.length === 0 ? (
          <section className="maintenance-section maintenance-section--wide" aria-label="Aktivite">
            <header className="maintenance-section__header">
              <h2>Aktivite</h2>
            </header>
            {workOrder.activity.length === 0 ? (
              <p className="maintenance-empty-state">Aktivite kaydı bulunmuyor.</p>
            ) : (
              <div className="maintenance-table-wrap" tabIndex={0} role="region" aria-label="Aktivite (kaydırılabilir)">
                <table aria-label="Aktivite" className="maintenance-table maintenance-table--compact maintenance-table--timeline">
                  <thead>
                    <tr>
                      <th>Zaman</th>
                      <th>Olay</th>
                      <th>Aktör</th>
                    </tr>
                  </thead>
                  <tbody>
                    {workOrder.activity.map((entry, index) => {
                      const activity = describeMaintenanceActivity(entry);
                      return (
                        <tr key={`${entry.occurredAtUtc}-${index}`}>
                          <td data-label="Zaman">{formatDateTime(entry.occurredAtUtc)}</td>
                          <td data-label="Olay" className="maintenance-table__cell--wrap">
                            {activity.label}
                            {activity.detail ? <small>{activity.detail}</small> : null}
                          </td>
                          <td data-label="Aktör">{entry.actorSubjectId ? displayActor(entry.actorSubjectId) : "—"}</td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            )}
          </section>
          ) : null}
        </div>
      </div>

      <footer className="maintenance-footer-note">
        <Factory aria-hidden="true" />
        Üretim emri verileri salt okunurdur; bakım emri durumu üretim akışından bağımsız izlenir.
      </footer>
    </div>
  );
}
