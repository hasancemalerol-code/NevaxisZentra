
import { enumLabel } from "@/lib/presentation/enum-labels";
import Link from "next/link";
import { notFound } from "next/navigation";
import { ArrowLeft, Factory } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import "@/features/maintenance/components/maintenance-workspace.css";
import {
  formatMaintenanceAge,
  formatMaintenanceCurrency,
  formatMaintenanceDate,
  formatMaintenanceDateTime,
  getMaintenanceAssetTypeLabel,
  getMaintenanceConditionStatusLabel,
  getMaintenanceConditionStatusTone,
  getMaintenanceConditionReasonLabel,
  getMaintenancePriorityLabel,
  getMaintenancePriorityTone,
  describeMeanRepairUnavailable,
  isActiveMaintenanceWorkOrder,
  isOpenMaintenanceRequest,
  isOverduePreventiveDue,
  getMaintenanceStatusLabel,
  getMaintenanceTypeLabel,
  getMaintenanceWorkNoteTypeLabel,
  getMaintenanceWorkOrderStatusTone,
  getPreventiveDueStatusLabel,
  getPreventiveDueStatusTone,
} from "@/features/maintenance/presentation/maintenance-labels";
import { readMaintenanceAssetHistory, readMaintenanceAssetReliability } from "@/features/maintenance/server/maintenance-reader";
import { displayActor } from "@/lib/presentation/display-label";
import { FailureStatePage } from "@/components/ui/failure-state";

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
            <h1>Varlık Bakım Geçmişi</h1>
          </div>
        </header>
        <FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Varlık geçmişi yüklenemedi" inline />
      </div>
    );
  }

  const history = result.history;
  const reliability = reliabilityResult.status === "success" ? reliabilityResult.reliability : null;
  if (!history.assetReference) {
    notFound();
  }

  const assetName =
    reliability?.assetName
    ?? history.requests.find((item) => item.assetName)?.assetName
    ?? history.workOrders.find((item) => item.assetName)?.assetName;
  const assetTitle = assetName?.trim() || history.assetReference;
  // "Son arıza" is derived from fault and corrective records only; the source record is named under the date.
  const lastFaultRecord = [...history.requests]
    .filter((item) => item.type === "fault" || item.type === "corrective")
    .sort((a, b) => b.reportedAtUtc.localeCompare(a.reportedAtUtc))[0];
  const openRequestCount = history.requests.filter((item) => isOpenMaintenanceRequest(item.status)).length;
  const activeWorkOrders = history.workOrders.filter((item) => isActiveMaintenanceWorkOrder(item.status));
  const criticalActiveWorkOrderCount = activeWorkOrders.filter((item) => item.priority === "critical").length;
  const planNameById = new Map(history.preventivePlans.map((plan) => [plan.id, plan.name]));
  const hasCostValues = history.costSummaries.some((summary) =>
    [summary.actualPartsCost, summary.externalServiceCost, summary.otherCost, summary.totalMaintenanceCost].some(
      (value) => value !== undefined && value !== null,
    ),
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
          <h1>{assetTitle}</h1>
          <span>{assetTitle === history.assetReference ? "Varlık bakım geçmişi (salt okunur)" : `${history.assetReference} · Varlık bakım geçmişi (salt okunur)`}</span>
        </div>
      </header>

      <div className="maintenance-page-content">
        {reliability ? (
          <section className="maintenance-section maintenance-section--wide" aria-label="Güvenilirlik">
            <header className="maintenance-section__header">
              <h2>Güvenilirlik</h2>
            </header>
            <dl className="maintenance-detail-dl maintenance-detail-dl--metrics">
              <div className={reliability.costAnalytics ? undefined : "maintenance-detail-dl__span2"}>
                <dt>Durum</dt>
                <dd>
                  <Badge tone={getMaintenanceConditionStatusTone(reliability.conditionStatus)}>
                    {getMaintenanceConditionStatusLabel(reliability.conditionStatus)}
                  </Badge>
                </dd>
                {reliability.conditionReasons.map((reason) => (
                  <dd key={reason}><small>{getMaintenanceConditionReasonLabel(reason)}</small></dd>
                ))}
              </div>
              <div>
                <dt>Açık arıza kaydı</dt>
                <dd>{openRequestCount}</dd>
                <dd><small>Toplam arıza kaydı: {history.requests.length}</small></dd>
              </div>
              <div>
                <dt>Açık bakım emri</dt>
                <dd>{activeWorkOrders.length}</dd>
                <dd><small>Kritik: {criticalActiveWorkOrderCount}</small></dd>
              </div>
              <div>
                <dt title="Güvenilirlik hesabına giren kayıtlı arıza olayları">Arıza olayı</dt>
                <dd>{reliability.totalFaults}</dd>
                <dd><small>Tekrarlayan: {reliability.repeatFaultCount}</small></dd>
              </div>
              <div>
                <dt>Ort. onarım süresi</dt>
                <dd>
                  {reliability.meanRepairMinutes !== undefined
                    ? `${Math.round(reliability.meanRepairMinutes)} dk`
                    : <span className="maintenance-detail-dl__empty">Hesaplanamadı</span>}
                </dd>
                {reliability.meanRepairMinutes === undefined ? <dd><small>{describeMeanRepairUnavailable(history.workOrders)}</small></dd> : null}
              </div>
              <div>
                <dt>Periyodik uyum</dt>
                <dd>
                  {reliability.preventiveComplianceRate !== undefined
                    ? `%${reliability.preventiveComplianceRate.toLocaleString("tr-TR", { minimumFractionDigits: 1, maximumFractionDigits: 1 })}`
                    : <span className="maintenance-detail-dl__empty">Hesaplanamadı</span>}
                </dd>
                <dd><small>Tamamlanan / vadesi gelen</small></dd>
              </div>
              <div>
                <dt>Son arıza</dt>
                <dd>
                  {reliability.lastFailureAtUtc
                    ? formatMaintenanceDateTime(reliability.lastFailureAtUtc)
                    : "Kayıtlı arıza yok"}
                </dd>
                {lastFaultRecord ? (
                  <dd><small>{lastFaultRecord.requestNumber} · {getMaintenanceTypeLabel(lastFaultRecord.type)}</small></dd>
                ) : null}
              </div>
              {reliability.costAnalytics ? (
                <div>
                  <dt>Bakım maliyeti</dt>
                  <dd>
                    {reliability.costAnalytics.totalMaintenanceCost === undefined || reliability.costAnalytics.totalMaintenanceCost === null
                      ? <span className="maintenance-detail-dl__empty">Girilmedi</span>
                      : formatMaintenanceCurrency(reliability.costAnalytics.totalMaintenanceCost)}
                  </dd>
                  <dd><small>{enumLabel(reliability.costAnalytics.dataCompletenessStatus)}</small></dd>
                </div>
              ) : null}
            </dl>
          </section>
        ) : null}

        <section className="maintenance-section maintenance-section--wide" aria-label="Bakım Emirleri">
          <header className="maintenance-section__header">
            <h2>Bakım Emirleri</h2>
          </header>
          {history.workOrders.length === 0 ? (
            <p className="maintenance-empty-state">Bakım emri kaydı bulunmuyor.</p>
          ) : (
            <div className="maintenance-table-wrap" tabIndex={0} role="region" aria-label="Bakım Emirleri (kaydırılabilir)">
              <table aria-label="Bakım Emirleri" className="maintenance-table maintenance-table--compact">
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
            <div className="maintenance-table-wrap" tabIndex={0} role="region" aria-label="Arıza Kayıtları (kaydırılabilir)">
              <table aria-label="Arıza Kayıtları" className="maintenance-table maintenance-table--compact">
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
                      <td>
                        <Badge tone="neutral">{getMaintenanceStatusLabel(request.status)}</Badge>
                      </td>
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
                          <span title="Bu planın, açık vadeden sonra üretilecek planlı vadesi">Sonraki planlı vade: {formatMaintenanceDate(plan.nextDueAtUtc)}</span>
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
                      <strong>
                        {due.preventivePlanName ?? planNameById.get(due.preventivePlanId) ?? due.preventivePlanCode ?? "Periyodik bakım planı"}
                      </strong>
                      <div className="maintenance-ca-item__meta">
                        {due.preventivePlanCode ? <span>{due.preventivePlanCode}</span> : null}
                        <span title="Plandan üretilmiş vade kaydı">{isOverduePreventiveDue(due) ? "Gecikmiş vade" : "Vade tarihi"}: {formatMaintenanceDate(due.dueAtUtc)}</span>
                        {isOverduePreventiveDue(due) ? (
                          <Badge tone="danger">Vadesi geçmiş</Badge>
                        ) : (
                          <Badge tone={getPreventiveDueStatusTone(due.status)}>
                            {getPreventiveDueStatusLabel(due.status)}
                          </Badge>
                        )}
                      </div>
                    </div>
                  </li>
                ))}
              </ul>
            )}
          </section>
        </div>

        {history.workNotes.length === 0 && history.partUsages.length === 0 ? (
          <section className="maintenance-section maintenance-section--wide" aria-label="Notlar ve Parçalar">
            <header className="maintenance-section__header">
              <h2>Notlar ve Parçalar</h2>
            </header>
            <p className="maintenance-empty-state">Bu varlık için bakım notu veya parça kullanım kaydı bulunmuyor.</p>
          </section>
        ) : null}

        {history.workNotes.length > 0 ? (
        <section className="maintenance-section maintenance-section--wide" aria-label="Bakım Notları">
          <header className="maintenance-section__header">
            <h2>Bakım Notları</h2>
          </header>
          <ul className="maintenance-ca-list">
            {history.workNotes.map((note) => (
              <li key={note.id} className="maintenance-ca-item">
                <div className="maintenance-ca-item__body">
                  <strong>{getMaintenanceWorkNoteTypeLabel(note.noteType)}</strong>
                  <p>{note.text}</p>
                  <div className="maintenance-ca-item__meta">
                    <span>{formatMaintenanceDateTime(note.createdAtUtc)}</span>
                    <span>{displayActor(note.createdBySubjectId)}</span>
                  </div>
                </div>
              </li>
            ))}
          </ul>
        </section>
        ) : null}

        {history.partUsages.length > 0 ? (
        <section className="maintenance-section maintenance-section--wide" aria-label="Parça Kullanımları">
          <header className="maintenance-section__header">
            <h2>Parça Kullanımları</h2>
          </header>
          {(
            <div className="maintenance-table-wrap" tabIndex={0} role="region" aria-label="Parça Kullanımları (kaydırılabilir)">
              <table aria-label="Parça Kullanımları" className="maintenance-table maintenance-table--compact">
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
        ) : null}

        <section className="maintenance-section maintenance-section--wide" aria-label="Maliyet Özetleri">
          <header className="maintenance-section__header">
            <h2>Maliyet Özetleri</h2>
          </header>
          {!hasCostValues ? (
            <p className="maintenance-empty-state">Henüz maliyet girilmedi.</p>
          ) : (
            <div className="maintenance-table-wrap" tabIndex={0} role="region" aria-label="Maliyet Özetleri (kaydırılabilir)">
              <table aria-label="Maliyet Özetleri" className="maintenance-table maintenance-table--compact">
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
                          {history.workOrders.find((item) => item.id === summary.workOrderId)?.workOrderNumber ?? "Bakım emri"}
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
    </div>
  );
}
