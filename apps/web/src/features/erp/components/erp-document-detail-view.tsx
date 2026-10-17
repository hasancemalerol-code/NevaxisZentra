import Link from "next/link";

import type { CanonicalErpDocumentDetail } from "../domain/erp";
import {
  formatErpDateTime,
  getErpDocumentTypeLabel,
} from "../presentation/erp-labels";
import {
  ErpDetailSection,
  ErpEmptyState,
  ErpFreshnessBadge,
  ErpMetadataGrid,
  ErpMetadataItem,
  ErpPageHeader,
  ErpProviderBadge,
  ErpReadonlyNotice,
} from "./erp-ui-primitives";
import { ErpWorkspaceNav } from "./erp-workspace-nav";
import "./erp-workspace.css";

type ErpDocumentDetailViewProps = Readonly<{
  document: CanonicalErpDocumentDetail;
}>;

export function ErpDocumentDetailView({ document }: ErpDocumentDetailViewProps) {
  const title = document.documentNumber ?? document.externalCode ?? document.externalId;

  return (
    <div className="production-execution-page erp-workspace-page erp-workspace-page--dense">
      <ErpPageHeader
        title={title}
        description="ERP belge özeti — salt okunur."
        notice={
          <ErpReadonlyNotice>
            Belge meta verisi ERP kaynağından görüntülenir; içerik indirme veya düzenleme bu ekranda
            yapılamaz.
          </ErpReadonlyNotice>
        }
      />

      <ErpWorkspaceNav activeHref="/erp-entegrasyonlari/belgeler" />

      <ErpDetailSection title="Belge Özeti">
        <ErpMetadataGrid>
          <ErpMetadataItem label="Belge Türü">
            {getErpDocumentTypeLabel(document.documentType)}
          </ErpMetadataItem>
          <ErpMetadataItem label="Belge No">{document.documentNumber ?? "—"}</ErpMetadataItem>
          <ErpMetadataItem label="Belge Tarihi">
            {formatErpDateTime(document.documentDateUtc)}
          </ErpMetadataItem>
          <ErpMetadataItem label="Durum">{document.status ?? "—"}</ErpMetadataItem>
          <ErpMetadataItem label="İlişkili Varlık">
            {document.relatedExternalEntityCode ?? document.relatedExternalEntityId ?? "—"}
          </ErpMetadataItem>
          <ErpMetadataItem label="Varlık Türü">{document.relatedEntityType ?? "—"}</ErpMetadataItem>
          <ErpMetadataItem label="Dosya Adı">{document.fileName ?? "—"}</ErpMetadataItem>
          <ErpMetadataItem label="MIME Türü">{document.mimeType ?? "—"}</ErpMetadataItem>
        </ErpMetadataGrid>
      </ErpDetailSection>

      <ErpDetailSection title="ERP Kaynağı">
        <ErpMetadataGrid>
          <ErpMetadataItem label="Sağlayıcı">
            <ErpProviderBadge provider={document.provider} />
          </ErpMetadataItem>
          <ErpMetadataItem label="Güncellik">
            <ErpFreshnessBadge freshness={document.freshness} />
          </ErpMetadataItem>
          <ErpMetadataItem label="Harici Referans">
            {document.externalReference ?? document.externalCode ?? document.externalId}
          </ErpMetadataItem>
          <ErpMetadataItem label="Son Güncelleme">
            {formatErpDateTime(document.updatedAtUtc)}
          </ErpMetadataItem>
        </ErpMetadataGrid>
      </ErpDetailSection>

      <ErpDetailSection title="Belge İçeriği">
        {document.contentAvailable ? (
          <ErpMetadataGrid>
            <ErpMetadataItem label="Durum">İçerik mevcut</ErpMetadataItem>
            {document.fileName ? (
              <ErpMetadataItem label="Dosya">{document.fileName}</ErpMetadataItem>
            ) : null}
            {document.mimeType ? (
              <ErpMetadataItem label="Biçim">{document.mimeType}</ErpMetadataItem>
            ) : null}
          </ErpMetadataGrid>
        ) : (
          <ErpEmptyState
            title="Belge içeriği mevcut değil"
            description="ERP kaynağı yalnızca belge meta verisini sağladı. Tam dosya içeriği senkronize edilmedi veya erişilebilir değil."
          />
        )}
      </ErpDetailSection>

      <p>
        <Link href="/erp-entegrasyonlari/belgeler">← Belge listesine dön</Link>
        {" · "}
        <Link href="/erp-entegrasyonlari/uretim">Üretim</Link>
      </p>
    </div>
  );
}
