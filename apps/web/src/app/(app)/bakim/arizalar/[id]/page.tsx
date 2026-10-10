import Link from "next/link";
import { notFound } from "next/navigation";
import { ArrowLeft, Wrench } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import {
  fetchEffectiveAccessCapabilities,
  hasGrantedPermission,
} from "@/features/admin-access/server/admin-access-effective-access";
import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { MaintenanceRequestConvertForm } from "@/features/maintenance/components/maintenance-request-convert-form";
import { MaintenanceWorkspaceNav } from "@/features/maintenance/components/maintenance-workspace-nav";
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
import { readMaintenanceRequestDetail } from "@/features/maintenance/server/maintenance-reader";
import { convertMaintenanceRequestAction } from "@/features/maintenance/server/maintenance-writer";

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
  });
}

function displayActor(value?: string): string {
  if (!value || looksLikeRawUuid(value)) {
    return "Kayıtlı kullanıcı";
  }

  return value;
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
      <main className="production-execution-page maintenance-page">
        <header className="maintenance-page-header production-execution-header">
          <Link className="maintenance-back-link" href="/bakim">
            <ArrowLeft aria-hidden="true" />
            Bakım ana sayfası
          </Link>
          <div className="maintenance-page-header__intro">
            <p>BAKIM &amp; ARIZA</p>
            <h1>Arıza Kaydı</h1>
          </div>
        </header>
        <MaintenanceWorkspaceNav active="overview" />
        <div className="maintenance-error-panel" role="alert">
          <Wrench aria-hidden="true" />
          <div>
            <h3>Arıza kaydı yüklenemedi</h3>
            <p>Kayıt şu anda görüntülenemiyor. Lütfen daha sonra tekrar deneyin.</p>
          </div>
        </div>
      </main>
    );
  }

  const request = result.request;
  const access = await fetchEffectiveAccessCapabilities();
  const canManage = hasGrantedPermission(access, "maintenance.manage");
  const canConvert =
    canManage && (request.status === "open" || request.status === "triaged");

  return (
    <main className="production-execution-page maintenance-page">
      <header className="maintenance-page-header production-execution-header">
        <Link className="maintenance-back-link" href="/bakim">
          <ArrowLeft aria-hidden="true" />
          Bakım ana sayfası
        </Link>
        <div className="maintenance-page-header__intro">
          <p>BAKIM &amp; ARIZA</p>
          <h1>{request.requestNumber}</h1>
          <span>{request.title}</span>
        </div>
      </header>

      <MaintenanceWorkspaceNav active="overview" />

      <div className="maintenance-page-content">
        <section aria-label="Arıza kaydı özeti" className="maintenance-section">
          <header className="maintenance-section__header">
            <h2>Arıza Kaydı Özeti</h2>
          </header>
          <div className="maintenance-badge-row">
            <Badge tone="neutral">{getMaintenanceStatusLabel(request.status)}</Badge>
            <Badge tone={getMaintenancePriorityTone(request.priority)}>
              {getMaintenancePriorityLabel(request.priority)}
            </Badge>
            <Badge tone="neutral">{getMaintenanceTypeLabel(request.type)}</Badge>
          </div>
          <dl className="maintenance-detail-dl">
            <div>
              <dt>Kayıt No</dt>
              <dd>{request.requestNumber}</dd>
            </div>
            <div>
              <dt>Başlık</dt>
              <dd>{request.title}</dd>
            </div>
            <div>
              <dt>Açıklama</dt>
              <dd>{request.description || "—"}</dd>
            </div>
            <div>
              <dt>Kaynak</dt>
              <dd>{request.source === "manual" ? "Manuel" : request.source}</dd>
            </div>
            <div>
              <dt>Bildiren</dt>
              <dd>{displayActor(request.reportedBySubjectId)}</dd>
            </div>
            <div>
              <dt>Bildirilme</dt>
              <dd>{formatDateTime(request.reportedAtUtc)}</dd>
            </div>
            <div>
              <dt>Son güncelleme</dt>
              <dd>{formatMaintenanceAge(request.updatedAtUtc)}</dd>
            </div>
          </dl>
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
              <dt>Referans</dt>
              <dd>{request.assetReference}</dd>
            </div>
            <div>
              <dt>Ad</dt>
              <dd>{request.assetName ?? "—"}</dd>
            </div>
          </dl>
        </section>

        {canConvert ? (
          <section aria-label="Bakım emrine dönüştür" className="maintenance-section">
            <header className="maintenance-section__header">
              <h2>Bakım Emrine Dönüştür</h2>
            </header>
            <MaintenanceRequestConvertForm
              expectedVersion={request.version}
              onConvert={convertMaintenanceRequestAction}
              requestId={request.id}
            />
          </section>
        ) : null}
      </div>
    </main>
  );
}
