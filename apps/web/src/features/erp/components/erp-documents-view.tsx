import type { ReactNode } from "react";
import Link from "next/link";
import { AlertTriangle, CircleGauge, FileText, Files } from "lucide-react";

import type { CanonicalErpDocumentSummary, ErpDocumentsWorkspaceSnapshot } from "../domain/erp";
import {
  formatErpDateTime,
  getErpDocumentTypeLabel,
  getErpProviderLabel,
  getErpSourceFreshnessLabel,
} from "../presentation/erp-labels";
import { ErpWorkspaceNav } from "./erp-workspace-nav";
import "./erp-workspace.css";

type ErpDocumentsViewProps = Readonly<{
  snapshot: ErpDocumentsWorkspaceSnapshot;
  documents: readonly CanonicalErpDocumentSummary[];
}>;

export function ErpDocumentsView({ snapshot, documents }: ErpDocumentsViewProps) {
  return (
    <main className="production-execution-page erp-workspace-page">
      <header className="erp-workspace-header production-execution-header">
        <div className="erp-workspace-header__intro">
          <p>ERP ENTEGRASYONLARI</p>
          <h1>Belgeler</h1>
          <span>
            Harici ERP belge projeksiyonları — salt okunur görünüm. Belge indirme veya ERP yazma
            işlemi yoktur.
          </span>
          <p className="erp-simulator-note">
            ERP-4L yalnızca ERP Simulator belge projeksiyonlarını içe aktarır.
          </p>
        </div>
      </header>

      <ErpWorkspaceNav activeHref="/erp-entegrasyonlari/belgeler" />

      <section className="erp-kpi-grid erp-kpi-grid--documents" aria-label="Belge özet göstergeleri">
        <ErpKpi
          label="Belgeler"
          value={snapshot.documentCount}
          description="Tanımlı ERP belgesi"
          icon={<Files aria-hidden="true" />}
        />
        <ErpKpi
          label="İçerik Mevcut"
          value={snapshot.contentAvailableCount}
          description="İçerik bayrağı: mevcut"
          icon={<FileText aria-hidden="true" />}
        />
        <ErpKpi
          label="Güncel Veri"
          value={snapshot.freshRecordCount}
          description="Kaynak tazeliği: güncel"
          icon={<CircleGauge aria-hidden="true" />}
        />
        <ErpKpi
          label="Eski Veri"
          value={snapshot.staleRecordCount}
          description="Kaynak tazeliği: eski"
          icon={<AlertTriangle aria-hidden="true" />}
        />
      </section>

      <section className="erp-section">
        <div className="erp-section__header">
          <h2>ERP Belgeleri</h2>
          <p>Belge türü, numara, ilişkili varlık ve tazelik</p>
        </div>
        <div className="erp-table-wrap">
          {documents.length === 0 ? (
            <div className="erp-empty">
              Henüz ERP belgesi yok. Önce belge senkronizasyonu çalıştırın.
            </div>
          ) : (
            <table className="erp-table erp-table--documents">
              <thead>
                <tr>
                  <th>Belge Türü</th>
                  <th>Belge No</th>
                  <th>Belge Tarihi</th>
                  <th>İlişkili Varlık</th>
                  <th>Dosya</th>
                  <th>İçerik</th>
                  <th>Tazelik</th>
                  <th>Kaynak</th>
                  <th>İşlem</th>
                </tr>
              </thead>
              <tbody>
                {documents.map((document) => {
                  const freshnessTone =
                    document.freshness === "fresh"
                      ? "ready"
                      : document.freshness === "stale"
                        ? "paused"
                        : "in-progress";

                  return (
                    <tr key={document.id}>
                      <td>{getErpDocumentTypeLabel(document.documentType)}</td>
                      <td>{document.documentNumber ?? document.externalCode ?? document.externalId}</td>
                      <td>{formatErpDateTime(document.documentDateUtc)}</td>
                      <td>
                        {document.relatedExternalEntityCode ??
                          document.relatedExternalEntityId ??
                          "—"}
                      </td>
                      <td>{document.fileName ?? "—"}</td>
                      <td>{document.contentAvailable ? "Mevcut" : "Yok"}</td>
                      <td>
                        <span className={`erp-status erp-status--${freshnessTone}`}>
                          {getErpSourceFreshnessLabel(document.freshness)}
                        </span>
                      </td>
                      <td>{getErpProviderLabel(document.provider)}</td>
                      <td>
                        <div className="erp-table-actions">
                          <Link href={`/erp-entegrasyonlari/belgeler/${document.id}`}>Aç</Link>
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          )}
        </div>
      </section>
    </main>
  );
}

function ErpKpi({
  label,
  value,
  description,
  icon,
}: Readonly<{
  label: string;
  value: number | string;
  description: string;
  icon: ReactNode;
}>) {
  return (
    <div className="erp-kpi">
      <span className="erp-kpi__icon" aria-hidden="true">
        {icon}
      </span>
      <div>
        <span className="erp-kpi__label">{label}</span>
        <strong className="erp-kpi__value">{value}</strong>
        <small className="erp-kpi__desc">{description}</small>
      </div>
    </div>
  );
}
