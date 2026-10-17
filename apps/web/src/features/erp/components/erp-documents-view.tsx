import { AlertTriangle, FileText, Files } from "lucide-react";

import type { CanonicalErpDocumentSummary, ErpDocumentsWorkspaceSnapshot } from "../domain/erp";
import {
  formatErpDateTime,
  getErpDocumentTypeLabel,
} from "../presentation/erp-labels";
import {
  ErpDataTable,
  ErpEmptyState,
  ErpFreshnessBadge,
  ErpKpiCard,
  ErpKpiGrid,
  ErpPageHeader,
  ErpProviderBadge,
  ErpReadonlyNotice,
  ErpSectionHeader,
  ErpSimulatorNotice,
  ErpTableAction,
  ErpTableActionCell,
} from "./erp-ui-primitives";
import { ErpWorkspaceNav } from "./erp-workspace-nav";
import "./erp-workspace.css";

type ErpDocumentsViewProps = Readonly<{
  snapshot: ErpDocumentsWorkspaceSnapshot;
  documents: readonly CanonicalErpDocumentSummary[];
}>;

export function ErpDocumentsView({ snapshot, documents }: ErpDocumentsViewProps) {
  return (
    <div className="production-execution-page erp-workspace-page erp-workspace-page--dense">
      <ErpPageHeader
        title="Belgeler"
        description="Harici ERP belge özetleri — salt okunur görünüm. Belge indirme veya ERP yazma işlemi yoktur."
        notice={
          <>
            <ErpReadonlyNotice>
              Bu bağlantı ERP belge kayıtlarını salt okunur olarak içe aktarır.
            </ErpReadonlyNotice>
            <ErpSimulatorNotice />
          </>
        }
      />

      <ErpWorkspaceNav activeHref="/erp-entegrasyonlari/belgeler" />

      <ErpKpiGrid>
        <ErpKpiCard
          label="Belgeler"
          value={snapshot.documentCount}
          description="Tanımlı ERP belgesi"
          icon={<Files aria-hidden="true" />}
        />
        <ErpKpiCard
          label="İçerik Mevcut"
          value={snapshot.contentAvailableCount}
          description="İçeriği bulunan belge"
          icon={<FileText aria-hidden="true" />}
          tone="success"
        />
        <ErpKpiCard
          label="Yalnız Metadata"
          value={snapshot.documentCount - snapshot.contentAvailableCount}
          description="İndirilebilir içerik sağlanmıyor"
          icon={<FileText aria-hidden="true" />}
        />
        <ErpKpiCard
          label="Güncel Değil"
          value={snapshot.staleRecordCount}
          description="Kaynak tazeliği"
          icon={<AlertTriangle aria-hidden="true" />}
          tone="warning"
        />
      </ErpKpiGrid>

      <section className="erp-section">
        <ErpSectionHeader
          title="ERP Belgeleri"
          description="Belge türü, numara, ilişkili varlık ve tazelik"
        />
        {documents.length === 0 ? (
          <ErpEmptyState
            title="Henüz ERP belgesi yok."
            description="Önce belge eşitlemesini çalıştırın."
            actionHref="/erp-entegrasyonlari/senkronizasyonlar"
            actionLabel="Eşitleme Merkezi"
          />
        ) : (
          <ErpDataTable label="ERP belgeleri" className="erp-table--documents">
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
              {documents.map((document) => (
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
                  <td>
                    {document.contentAvailable ? (
                      <span className="erp-badge erp-badge--mapped">İçerik Mevcut</span>
                    ) : (
                      <span className="erp-badge erp-badge--ignored">Yalnız Metadata</span>
                    )}
                  </td>
                  <td>
                    <ErpFreshnessBadge freshness={document.freshness} />
                  </td>
                  <td>
                    <ErpProviderBadge provider={document.provider} />
                  </td>
                  <ErpTableActionCell>
                    <ErpTableAction href={`/erp-entegrasyonlari/belgeler/${document.id}`}>
                      Aç
                    </ErpTableAction>
                  </ErpTableActionCell>
                </tr>
              ))}
            </tbody>
          </ErpDataTable>
        )}
      </section>
    </div>
  );
}
