import { FACILITY_TIME_ZONE } from "@/lib/presentation/format";
import Link from "next/link";
import { notFound } from "next/navigation";
import { ArrowLeft } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import {
  fetchEffectiveAccessCapabilities,
  hasGrantedPermission,
} from "@/features/admin-access/server/admin-access-effective-access";
import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { MaintenanceRequestConvertForm } from "@/features/maintenance/components/maintenance-request-convert-form";
import "@/features/maintenance/components/maintenance-workspace.css";
import {
  looksLikeRawUuid,
  isMaintenanceRecordId,
} from "@/features/maintenance/lib/maintenance-record-id";
import {
  formatMaintenanceAge,
  getMaintenanceAssetTypeLabel,
  getMaintenancePriorityLabel,
  getMaintenancePriorityTone,
  getMaintenanceStatusLabel,
  getMaintenanceTypeLabel,
} from "@/features/maintenance/presentation/maintenance-labels";
import { readMaintenanceAssetHistory, readMaintenanceRequestDetail } from "@/features/maintenance/server/maintenance-reader";
import { convertMaintenanceRequestAction } from "@/features/maintenance/server/maintenance-writer";
import { FailureStatePage } from "@/components/ui/failure-state";
import { enumLabel } from "@/lib/presentation/enum-labels";

type PageProps = Readonly<{
  params: Promise<{ id: string }>;
  searchParams: Promise<Readonly<{ convert?: string }>>;
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
    timeZone: FACILITY_TIME_ZONE,
  });
}

function displayActor(value?: string): string {
  if (!value || looksLikeRawUuid(value)) {
    return "Kayıtlı kullanıcı";
  }

  return value;
}

function withAgo(age: string): string {
  return age === "Az önce" || age === "—" ? age : `${age} önce`;
}

export default async function MaintenanceRequestDetailPage({
  params,
  searchParams,
}: PageProps) {
  await forbidUnlessGranted("maintenance.read");

  const { id } = await params;
  await searchParams;

  if (id === "yeni" || !isMaintenanceRecordId(id)) {
    notFound();
  }

  const result = await readMaintenanceRequestDetail(id);
  if (result.status === "failure") {
    if (result.code === "not-found" || result.code === "permission-denied") {
      notFound();
    }

    return (
      <div className="production-execution-page maintenance-page">
        <header className="maintenance-page-header production-execution-header">
          <Link className="maintenance-back-link" href="/bakim">
            <ArrowLeft aria-hidden="true" />
            Bakım ana sayfası
          </Link>
          <div className="maintenance-page-header__intro">
            <p>BAKIM VE ARIZA</p>
            <h1>Arıza Kaydı</h1>
          </div>
        </header>
        <FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Arıza kaydı yüklenemedi" inline />
      </div>
    );
  }

  const request = result.request;
  const access = await fetchEffectiveAccessCapabilities();
  const canManage = hasGrantedPermission(access, "maintenance.manage");
  const historyResult = request.status === "converted-to-work-order" ? await readMaintenanceAssetHistory(request.assetReference) : null;
  const linkedWorkOrder = historyResult && historyResult.status === "success"
    ? historyResult.history.workOrders.find((item) => item.maintenanceRequestId === request.id)
    : undefined;
  const canConvert =
    canManage && (request.status === "open" || request.status === "triaged");

  return (
    <div className="production-execution-page maintenance-page">
      <header className="maintenance-page-header production-execution-header">
        <Link className="maintenance-back-link" href="/bakim">
          <ArrowLeft aria-hidden="true" />
          Bakım ana sayfası
        </Link>
        <div className="maintenance-page-header__row">
          <div className="maintenance-page-header__intro">
            <p>BAKIM VE ARIZA</p>
            <h1>{request.requestNumber}</h1>
            <span>{request.title}</span>
          </div>
          {linkedWorkOrder ? (
            <div className="maintenance-page-header__actions">
              <Link className="button button-secondary button-medium" href={`/bakim/emirler/${linkedWorkOrder.id}`}>
                <span>Bakım Emrini Gör</span>
              </Link>
            </div>
          ) : null}
          {canConvert ? (
            <div className="maintenance-page-header__actions">
              <MaintenanceRequestConvertForm
                expectedVersion={request.version}
                onConvert={convertMaintenanceRequestAction}
                requestId={request.id}
              />
              <small className="maintenance-page-header__hint">Bu arıza kaydından yeni bir bakım emri açılır.</small>
            </div>
          ) : null}
        </div>
      </header>

      <div className="maintenance-page-content">
        <section aria-label="Arıza kaydı özeti" className="maintenance-section">
          <header className="maintenance-section__header">
            <h2>Arıza Kaydı Özeti</h2>
          </header>
          <dl className="maintenance-detail-dl maintenance-detail-dl--summary">
            <div>
              <dt>Durum</dt>
              <dd>
                <Badge tone="neutral">{getMaintenanceStatusLabel(request.status)}</Badge>
              </dd>
            </div>
            <div>
              <dt>Öncelik</dt>
              <dd>
                <Badge tone={getMaintenancePriorityTone(request.priority)}>
                  {getMaintenancePriorityLabel(request.priority)}
                </Badge>
              </dd>
            </div>
            <div>
              <dt>Tür</dt>
              <dd>
                <Badge tone="neutral">{getMaintenanceTypeLabel(request.type)}</Badge>
              </dd>
            </div>
            <div>
              <dt>Kaynak</dt>
              <dd>{request.source === "manual" ? "Manuel" : enumLabel(request.source)}</dd>
            </div>
            <div>
              <dt>Bildiren</dt>
              <dd>{displayActor(request.reportedBySubjectId)}</dd>
            </div>
            <div>
              <dt>Bildirilme</dt>
              <dd>{formatDateTime(request.reportedAtUtc)}</dd>
            </div>
            <div className="maintenance-detail-dl__wide">
              <dt>Açıklama</dt>
              <dd>{request.description || "Açıklama girilmedi"}</dd>
            </div>
          </dl>
          <p className="maintenance-detail-note">Son güncelleme: {withAgo(formatMaintenanceAge(request.updatedAtUtc))}</p>
        </section>

        <section aria-label="Varlık" className="maintenance-section">
          <header className="maintenance-section__header">
            <h2>Varlık</h2>
            <Link href={`/bakim/varliklar/${encodeURIComponent(request.assetReference)}`}>
              Geçmişi Gör
            </Link>
          </header>
          <dl className="maintenance-detail-dl">
            <div>
              <dt>Varlık türü</dt>
              <dd>{getMaintenanceAssetTypeLabel(request.assetType)}</dd>
            </div>
            <div>
              <dt>Varlık referansı</dt>
              <dd>{request.assetReference}</dd>
            </div>
            <div>
              <dt>Varlık adı</dt>
              <dd>{request.assetName ?? "Adı girilmedi"}</dd>
            </div>
          </dl>
        </section>

      </div>
    </div>
  );
}
