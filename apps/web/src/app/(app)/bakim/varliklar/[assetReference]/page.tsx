import { isNotFoundFailureCode } from "@/lib/api/api-failure-classifier";
import Link from "next/link";
import { notFound } from "next/navigation";
import { ArrowLeft, Factory, Wrench } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { MaintenanceWorkspaceNav } from "@/features/maintenance/components/maintenance-workspace-nav";
import "@/features/maintenance/components/maintenance-workspace.css";
import {
  formatMaintenanceAge,
  formatMaintenanceCurrency,
  formatMaintenanceDate,
  formatMaintenanceDateTime,
  getMaintenanceAssetTypeLabel,
  getMaintenanceConditionStatusLabel,
  getMaintenanceConditionStatusTone,
  getMaintenancePriorityLabel,
  getMaintenancePriorityTone,
  getMaintenanceStatusLabel,
  getMaintenanceTypeLabel,
  getMaintenanceWorkNoteTypeLabel,
  getMaintenanceWorkOrderStatusTone,
  getPreventiveDueStatusLabel,
  getPreventiveDueStatusTone,
} from "@/features/maintenance/presentation/maintenance-labels";
import { readMaintenanceAssetHistory, readMaintenanceAssetReliability } from "@/features/maintenance/server/maintenance-reader";

type PageProps = Readonly<{
  params: Promise<{ assetReference: string }>;
}>;

export default async function MaintenanceAssetHistoryPage({ params }: PageProps) {
  await forbidUnlessGranted("maintenance.read");

  const { assetReference: rawReference } = await params;
  const assetReference = decodeURIComponent(rawReference);
  const [result, reliabilityResult] = await Promise.all([
    readMaintenanceAssetHistory(assetReference),
    readMaintenanceAssetReliability(assetReference),
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
            <h1>Varlık Bakım Geçmişi</h1>
          </div>
        </header>
        <MaintenanceWorkspaceNav active="overview" />
        <div role="alert" className="maintenance-error-panel">
          <Wrench aria-hidden="true" />
          <div>
            <h3>{isNotFoundFailureCode("code" in result ? result.code : undefined) ? "Kayıt bulunamadı" : "Varlık geçmişi yüklenemedi"}</h3>
            <p>{isNotFoundFailureCode("code" in result ? result.code : undefined) ? "İstenen varlık kaydı bulunamadı." : result.message}</p>
          </div>
        </div>
      </main>
    );
  }

  const history = result.history;
  const reliability = reliabilityResult.status === "success" ? reliabilityResult.reliability : null;
  if (!history.assetReference) {
    notFound();
  }

  return (
    <main className="production-execution-page maintenance-page">
      <header className="maintenance-page-header production-execution-header">
        <Link href="/bakim" className="maintenance-back-link">
          <ArrowLeft aria-hidden="true" />
          Bakım ana sayfası
        </Link>
        <div className="maintenance-page-header__intro">
          <p>BAKIM &amp; ARIZA</p>
          <h1>{history.assetReference}</h1>
          <span>Varlık bakım geçmişi (salt okunur)</span>
        </div>
      </header>

      <MaintenanceWorkspaceNav active="overview" />

      <div className="maintenance-page-content">
        {reliability ? (
          <section className="maintenance-section maintenance-section--wide" aria-label="Güvenilirlik">
            <header className="maintenance-section__header">
              <h2>Güvenilirlik</h2>
            </header>
            <div className="maintenance-detail-grid">
              <dl className="maintenance-report-metrics">
                <div>
                  <dt>Durum</dt>
                  <dd>
                    <Badge tone={getMaintenanceConditionStatusTone(reliability.conditionStatus)}>
                      {getMaintenanceConditionStatusLabel(reliability.conditionStatus)}
                    </Badge>
                  </dd>
                  {reliability.conditionReasons.map((reason) => (
                    <small key={reason}>{reason}</small>
                  ))}
                </div>
                <div>
                  <dt>Arıza Sayısı</dt>
                  <dd>{reliability.totalFaults}</dd>
                  <small>Tekrar: {reliability.repeatFaultCount}</small>
                </div>
                <div>
                  <dt>MTTR</dt>
                  <dd>
                    {reliability.meanRepairMinutes !== undefined
                      ? `${Math.round(reliability.meanRepairMinutes)} dk`
                      : "—"}
                  </dd>
                </div>
                <div>
                  <dt>Periyodik Bakım Uyumu</dt>
                  <dd>
                    {reliability.preventiveComplianceRate !== undefined
                      ? `%${reliability.preventiveComplianceRate.toFixed(1)}`
                      : "—"}
                  </dd>
                </div>
                <div>
                  <dt>Açık Bakım</dt>
                  <dd>{reliability.openWorkOrders}</dd>
                  <small>Kritik: {reliability.criticalOpenWorkOrders}</small>
                </div>
                <div>
                  <dt>Son Arıza</dt>
                  <dd>
                    {reliability.lastFailureAtUtc
                      ? formatMaintenanceDateTime(reliability.lastFailureAtUtc)
                      : "—"}
                  </dd>
                </div>
              </dl>
            </div>
            {reliability.costAnalytics ? (
              <p className="maintenance-report-period">
                Bakım maliyeti: {formatMaintenanceCurrency(reliability.costAnalytics.totalMaintenanceCost)}{" "}
                ({reliability.costAnalytics.dataCompletenessStatus})
              </p>
            ) : null}
          </section>
        ) : null}

        <section className="maintenance-section maintenance-section--wide" aria-label="Bakım Emirleri">
          <header className="maintenance-section__header">
            <h2>Bakım Emirleri</h2>
          </header>
          {history.workOrders.length === 0 ? (
            <p className="maintenance-empty-state">Bakım emri kaydı bulunmuyor.</p>
          ) : (
            <div className="maintenance-table-wrap">
              <table className="maintenance-table maintenance-table--compact">
                <thead>
                  <tr>
                    <th>Emir No</th>
                    <th>Tür</th>
                    <th>Öncelik</th>
                    <th>Durum</th>
                    <th>Güncelleme</th>
                  </tr>
                </thead>
                <tbody>
                  {history.workOrders.map((workOrder) => (
                    <tr key={workOrder.id}>
                      <td>
                        <Link href={`/bakim/emirler/${workOrder.id}`}>
                          <strong>{workOrder.workOrderNumber}</strong>
                        </Link>
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
                      <td>{formatMaintenanceAge(workOrder.updatedAtUtc)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>

        <section className="maintenance-section maintenance-section--wide" aria-label="Arıza Kayıtları">
          <header className="maintenance-section__header">
            <h2>Arıza Kayıtları</h2>
          </header>
          {history.requests.length === 0 ? (
            <p className="maintenance-empty-state">Arıza kaydı bulunmuyor.</p>
          ) : (
            <div className="maintenance-table-wrap">
              <table className="maintenance-table maintenance-table--compact">
                <thead>
                  <tr>
                    <th>Kayıt No</th>
                    <th>Başlık</th>
                    <th>Tür</th>
                    <th>Öncelik</th>
                    <th>Durum</th>
                  </tr>
                </thead>
                <tbody>
                  {history.requests.map((request) => (
                    <tr key={request.id}>
                      <td>
                        <strong>{request.requestNumber}</strong>
                      </td>
                      <td className="maintenance-table__cell--wrap">{request.title}</td>
                      <td>{getMaintenanceTypeLabel(request.type)}</td>
                      <td>
                        <Badge tone={getMaintenancePriorityTone(request.priority)}>
                          {getMaintenancePriorityLabel(request.priority)}
                        </Badge>
                      </td>
                      <td>{getMaintenanceStatusLabel(request.status)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>

        <div className="maintenance-detail-grid">
          <section className="maintenance-section" aria-label="Periyodik Bakım Planları">
            <header className="maintenance-section__header">
              <h2>Periyodik Bakım Planları</h2>
            </header>
            {history.preventivePlans.length === 0 ? (
              <p className="maintenance-empty-state">Plan kaydı bulunmuyor.</p>
            ) : (
              <ul className="maintenance-ca-list">
                {history.preventivePlans.map((plan) => (
                  <li key={plan.id} className="maintenance-ca-item">
                    <div className="maintenance-ca-item__body">
                      <strong>
                        {plan.code} · {plan.name}
                      </strong>
                      <div className="maintenance-ca-item__meta">
                        <span>{getMaintenanceAssetTypeLabel(plan.assetType)}</span>
                        <span>{getMaintenanceStatusLabel(plan.status)}</span>
                        {plan.nextDueAtUtc ? (
                          <span>Sonraki: {formatMaintenanceDate(plan.nextDueAtUtc)}</span>
                        ) : null}
                      </div>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </section>

          <section className="maintenance-section" aria-label="Periyodik Bakım Vadeleri">
            <header className="maintenance-section__header">
              <h2>Periyodik Bakım Vadeleri</h2>
            </header>
            {history.preventiveDues.length === 0 ? (
              <p className="maintenance-empty-state">Vade kaydı bulunmuyor.</p>
            ) : (
              <ul className="maintenance-ca-list">
                {history.preventiveDues.map((due) => (
                  <li key={due.id} className="maintenance-ca-item">
                    <div className="maintenance-ca-item__body">
                      <strong>{due.preventivePlanCode ?? due.preventivePlanId}</strong>
                      <div className="maintenance-ca-item__meta">
                        <span>{formatMaintenanceDate(due.dueAtUtc)}</span>
                        <Badge tone={getPreventiveDueStatusTone(due.status)}>
                          {getPreventiveDueStatusLabel(due.status)}
                        </Badge>
                      </div>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </section>
        </div>

        <section className="maintenance-section maintenance-section--wide" aria-label="Bakım Notları">
          <header className="maintenance-section__header">
            <h2>Bakım Notları</h2>
          </header>
          {history.workNotes.length === 0 ? (
            <p className="maintenance-empty-state">Bakım notu bulunmuyor.</p>
          ) : (
            <ul className="maintenance-ca-list">
              {history.workNotes.map((note) => (
                <li key={note.id} className="maintenance-ca-item">
                  <div className="maintenance-ca-item__body">
                    <strong>{getMaintenanceWorkNoteTypeLabel(note.noteType)}</strong>
                    <p>{note.text}</p>
                    <div className="maintenance-ca-item__meta">
                      <span>{formatMaintenanceDateTime(note.createdAtUtc)}</span>
                      <span>{note.createdBySubjectId}</span>
                    </div>
                  </div>
                </li>
              ))}
            </ul>
          )}
        </section>

        <section className="maintenance-section maintenance-section--wide" aria-label="Parça Kullanımları">
          <header className="maintenance-section__header">
            <h2>Parça Kullanımları</h2>
          </header>
          {history.partUsages.length === 0 ? (
            <p className="maintenance-empty-state">Parça kullanım kaydı bulunmuyor.</p>
          ) : (
            <div className="maintenance-table-wrap">
              <table className="maintenance-table maintenance-table--compact">
                <thead>
                  <tr>
                    <th>Parça</th>
                    <th>Miktar</th>
                    <th>Maliyet</th>
                    <th>Kayıt</th>
                  </tr>
                </thead>
                <tbody>
                  {history.partUsages.map((part) => (
                    <tr key={part.id}>
                      <td>
                        <strong>{part.partReference}</strong>
                        {part.partName ? <small>{part.partName}</small> : null}
                      </td>
                      <td>
                        {part.quantity} {part.unit}
                      </td>
                      <td>
                        {formatMaintenanceCurrency(part.actualLineCost ?? part.estimatedLineCost)}
                      </td>
                      <td>{formatMaintenanceDateTime(part.recordedAtUtc)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>

        <section className="maintenance-section maintenance-section--wide" aria-label="Maliyet Özetleri">
          <header className="maintenance-section__header">
            <h2>Maliyet Özetleri</h2>
          </header>
          {history.costSummaries.length === 0 ? (
            <p className="maintenance-empty-state">Maliyet özeti bulunmuyor.</p>
          ) : (
            <div className="maintenance-table-wrap">
              <table className="maintenance-table maintenance-table--compact">
                <thead>
                  <tr>
                    <th>Emir</th>
                    <th>Parça</th>
                    <th>Dış Hizmet</th>
                    <th>Diğer</th>
                    <th>Toplam</th>
                  </tr>
                </thead>
                <tbody>
                  {history.costSummaries.map((summary) => (
                    <tr key={summary.workOrderId}>
                      <td>
                        <Link href={`/bakim/emirler/${summary.workOrderId}`}>
                          {summary.workOrderId}
                        </Link>
                      </td>
                      <td>{formatMaintenanceCurrency(summary.actualPartsCost)}</td>
                      <td>{formatMaintenanceCurrency(summary.externalServiceCost)}</td>
                      <td>{formatMaintenanceCurrency(summary.otherCost)}</td>
                      <td>{formatMaintenanceCurrency(summary.totalMaintenanceCost)}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>
      </div>

      <footer className="maintenance-footer-note">
        <Factory aria-hidden="true" />
        Varlık bakım geçmişi salt okunurdur; kayıt oluşturma bu ekrandan yapılmaz.
      </footer>
    </main>
  );
}
