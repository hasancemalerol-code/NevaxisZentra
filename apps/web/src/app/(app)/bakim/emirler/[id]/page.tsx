import Link from "next/link";
import { notFound } from "next/navigation";
import { ArrowLeft, Factory, Wrench } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import {
  MaintenanceWorkOrderCostSummaryPanel,
  MaintenanceWorkOrderNotesPanel,
  MaintenanceWorkOrderPartsPanel,
  MaintenanceWorkOrderPreventiveSourcePanel,
  MaintenanceWorkOrderTimelinePanel,
} from "@/features/maintenance/components/maintenance-work-order-execution-panels";
import { MaintenanceWorkspaceNav } from "@/features/maintenance/components/maintenance-workspace-nav";
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
} from "@/features/maintenance/presentation/maintenance-labels";
import {
  readMaintenanceWorkOrderDetail,
  readMaintenanceWorkOrderExecution,
} from "@/features/maintenance/server/maintenance-reader";
import {
  createMaintenancePartUsageAction,
  createMaintenanceWorkNoteAction,
} from "@/features/maintenance/server/maintenance-writer";

type PageProps = Readonly<{
  params: Promise<{ id: string }>;
}>;

function formatDateTime(iso?: string): string {
  if (!iso) {
    return "—";
  }

  return new Date(iso).toLocaleString("tr-TR", {
    day: "2-digit",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}

export default async function MaintenanceWorkOrderDetailPage({ params }: PageProps) {
  const { id } = await params;
  const [result, executionResult] = await Promise.all([
    readMaintenanceWorkOrderDetail(id),
    readMaintenanceWorkOrderExecution(id),
  ]);

  if (result.status === "failure") {
    return (
      <main className="production-execution-page maintenance-page">
        <header className="maintenance-page-header production-execution-header">
          <Link href="/bakim" className="maintenance-back-link">
            <ArrowLeft aria-hidden="true" />
            Bakım ana sayfası
          </Link>
          <div className="maintenance-page-header__intro">
            <p>BAKIM &amp; ARIZA</p>
            <h1>Bakım Emri Detayı</h1>
          </div>
        </header>
        <MaintenanceWorkspaceNav active="work-orders" />
        <div role="alert" className="maintenance-error-panel">
          <Wrench aria-hidden="true" />
          <div>
            <h3>Bakım emri yüklenemedi</h3>
            <p>{result.message}</p>
          </div>
        </div>
      </main>
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

  return (
    <main className="production-execution-page maintenance-page">
      <header className="maintenance-page-header production-execution-header">
        <Link href="/bakim" className="maintenance-back-link">
          <ArrowLeft aria-hidden="true" />
          Bakım ana sayfası
        </Link>
        <div className="maintenance-page-header__intro">
          <p>BAKIM &amp; ARIZA</p>
          <h1>{workOrder.workOrderNumber}</h1>
          <span>{workOrder.title}</span>
        </div>
      </header>

      <MaintenanceWorkspaceNav active="work-orders" />

      <div className="maintenance-page-content">
        <section className="maintenance-section" aria-label="Bakım Emri Özeti">
          <header className="maintenance-section__header">
            <h2>Bakım Emri Özeti</h2>
          </header>
          <div className="maintenance-badge-row">
            <Badge tone={getMaintenanceWorkOrderStatusTone(workOrder.status)}>
              {getMaintenanceStatusLabel(workOrder.status)}
            </Badge>
            <Badge tone={getMaintenancePriorityTone(workOrder.priority)}>
              {getMaintenancePriorityLabel(workOrder.priority)}
            </Badge>
            <Badge tone="neutral">{getMaintenanceTypeLabel(workOrder.type)}</Badge>
          </div>
          <dl className="maintenance-detail-dl">
            <div>
              <dt>Emir No</dt>
              <dd>{workOrder.workOrderNumber}</dd>
            </div>
            <div>
              <dt>Başlık</dt>
              <dd>{workOrder.title}</dd>
            </div>
            <div>
              <dt>Açıklama</dt>
              <dd>{workOrder.description || "—"}</dd>
            </div>
            <div>
              <dt>Oluşturan</dt>
              <dd>{workOrder.createdBySubjectId}</dd>
            </div>
            <div>
              <dt>Oluşturulma</dt>
              <dd>{formatDateTime(workOrder.createdAtUtc)}</dd>
            </div>
            <div>
              <dt>Son Güncelleme</dt>
              <dd>{formatMaintenanceAge(workOrder.updatedAtUtc)}</dd>
            </div>
          </dl>
        </section>

        <div className="maintenance-detail-grid">
          <section className="maintenance-section" aria-label="Varlık">
            <header className="maintenance-section__header">
              <h2>Varlık</h2>
              <Link href={`/bakim/varliklar/${encodeURIComponent(workOrder.assetReference)}`}>
                Geçmişi Gör
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
                <dt>Ad</dt>
                <dd>{workOrder.assetName ?? "—"}</dd>
              </div>
              <div>
                <dt>Varlık Kimliği</dt>
                <dd>{workOrder.assetId ?? "—"}</dd>
              </div>
            </dl>
          </section>

          <section className="maintenance-section" aria-label="Bağlı Üretim">
            <header className="maintenance-section__header">
              <h2>Bağlı Üretim</h2>
            </header>
            <dl className="maintenance-detail-dl">
              <div>
                <dt>Üretim Emri</dt>
                <dd>{workOrder.productionOrderReference ?? workOrder.productionOrderId ?? "—"}</dd>
              </div>
              <div>
                <dt>Üretim Durumu</dt>
                <dd>{workOrder.productionOrderStatus ?? "—"}</dd>
              </div>
            </dl>
          </section>

          <section className="maintenance-section" aria-label="Arıza/Talep">
            <header className="maintenance-section__header">
              <h2>Arıza/Talep</h2>
            </header>
            <dl className="maintenance-detail-dl">
              <div>
                <dt>Talep No</dt>
                <dd>{workOrder.maintenanceRequestNumber ?? "—"}</dd>
              </div>
              <div>
                <dt>Talep Kimliği</dt>
                <dd>{workOrder.maintenanceRequestId ?? "—"}</dd>
              </div>
            </dl>
          </section>

          <section className="maintenance-section" aria-label="Atanan Teknisyen">
            <header className="maintenance-section__header">
              <h2>Atanan Teknisyen</h2>
            </header>
            <dl className="maintenance-detail-dl">
              <div>
                <dt>Teknisyen</dt>
                <dd>{workOrder.assignedTechnicianSubjectId ?? "Atanmadı"}</dd>
              </div>
            </dl>
          </section>

          <section className="maintenance-section" aria-label="Planlama">
            <header className="maintenance-section__header">
              <h2>Planlama</h2>
            </header>
            <dl className="maintenance-detail-dl">
              <div>
                <dt>Planlanan Başlangıç</dt>
                <dd>{formatDateTime(workOrder.plannedStartUtc)}</dd>
              </div>
              <div>
                <dt>Planlanan Bitiş</dt>
                <dd>{formatDateTime(workOrder.plannedEndUtc)}</dd>
              </div>
            </dl>
          </section>

          <section className="maintenance-section" aria-label="Çalışma Durumu">
            <header className="maintenance-section__header">
              <h2>Çalışma Durumu</h2>
            </header>
            <dl className="maintenance-detail-dl">
              <div>
                <dt>Durum</dt>
                <dd>{getMaintenanceStatusLabel(workOrder.status)}</dd>
              </div>
              <div>
                <dt>Başlangıç</dt>
                <dd>{formatDateTime(workOrder.startedAtUtc)}</dd>
              </div>
            </dl>
          </section>

          <MaintenanceWorkOrderPreventiveSourcePanel workOrder={workOrder} />

          <section className="maintenance-section maintenance-section--wide" aria-label="Çözüm/Tamamlama">
            <header className="maintenance-section__header">
              <h2>Çözüm/Tamamlama</h2>
            </header>
            <dl className="maintenance-detail-dl">
              <div>
                <dt>Tamamlanma</dt>
                <dd>{formatDateTime(workOrder.completedAtUtc)}</dd>
              </div>
              <div>
                <dt>Çözüm Kodu</dt>
                <dd>
                  {workOrder.resolutionCode
                    ? getMaintenanceResolutionCodeLabel(workOrder.resolutionCode)
                    : "—"}
                </dd>
              </div>
              <div>
                <dt>Tamamlama Notu</dt>
                <dd>{workOrder.completionNote ?? "—"}</dd>
              </div>
            </dl>
          </section>

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

          <section className="maintenance-section maintenance-section--wide" aria-label="Aktivite">
            <header className="maintenance-section__header">
              <h2>Aktivite</h2>
            </header>
            {workOrder.activity.length === 0 ? (
              <p className="maintenance-empty-state">Aktivite kaydı bulunmuyor.</p>
            ) : (
              <div className="maintenance-table-wrap">
                <table className="maintenance-table maintenance-table--compact">
                  <thead>
                    <tr>
                      <th>Zaman</th>
                      <th>Olay</th>
                      <th>Özet</th>
                      <th>Aktör</th>
                    </tr>
                  </thead>
                  <tbody>
                    {workOrder.activity.map((entry, index) => (
                      <tr key={`${entry.occurredAtUtc}-${index}`}>
                        <td>{formatDateTime(entry.occurredAtUtc)}</td>
                        <td>{entry.eventType}</td>
                        <td className="maintenance-table__cell--wrap">{entry.summary}</td>
                        <td>{entry.actorSubjectId ?? "—"}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </section>
        </div>
      </div>

      <footer className="maintenance-footer-note">
        <Factory aria-hidden="true" />
        Üretim emri verileri salt okunurdur; bakım emri durumu üretim akışından bağımsız izlenir.
      </footer>
    </main>
  );
}
