import Link from "next/link";

import type { CanonicalErpDocumentDetail } from "../domain/erp";
import {
  formatErpDateTime,
  getErpDocumentTypeLabel,
  getErpProviderLabel,
  getErpSourceFreshnessLabel,
} from "../presentation/erp-labels";
import { ErpWorkspaceNav } from "./erp-workspace-nav";
import "./erp-workspace.css";

type ErpDocumentDetailViewProps = Readonly<{
  document: CanonicalErpDocumentDetail;
}>;

export function ErpDocumentDetailView({ document }: ErpDocumentDetailViewProps) {
  return (
    <main className="production-execution-page erp-workspace-page">
      <header className="erp-workspace-header production-execution-header">
        <div className="erp-workspace-header__intro">
          <p>ERP ENTEGRASYONLARI</p>
          <h1>{document.documentNumber ?? document.externalCode ?? document.externalId}</h1>
          <span>ERP belge özeti — salt okunur.</span>
        </div>
      </header>

      <ErpWorkspaceNav activeHref="/erp-entegrasyonlari/belgeler" />

      <section className="erp-section">
        <div className="erp-section__header">
          <h2>Belge Özeti</h2>
        </div>
        <dl className="erp-detail-grid">
          <div>
            <dt>Belge Türü</dt>
            <dd>{getErpDocumentTypeLabel(document.documentType)}</dd>
          </div>
          <div>
            <dt>Belge No</dt>
            <dd>{document.documentNumber ?? "—"}</dd>
          </div>
          <div>
            <dt>Belge Tarihi</dt>
            <dd>{formatErpDateTime(document.documentDateUtc)}</dd>
          </div>
          <div>
            <dt>Durum</dt>
            <dd>{document.status ?? "—"}</dd>
          </div>
          <div>
            <dt>İlişkili Varlık</dt>
            <dd>
              {document.relatedExternalEntityCode ??
                document.relatedExternalEntityId ??
                "—"}
            </dd>
          </div>
          <div>
            <dt>Varlık Türü</dt>
            <dd>{document.relatedEntityType ?? "—"}</dd>
          </div>
          <div>
            <dt>Dosya Adı</dt>
            <dd>{document.fileName ?? "—"}</dd>
          </div>
          <div>
            <dt>MIME Türü</dt>
            <dd>{document.mimeType ?? "—"}</dd>
          </div>
          <div>
            <dt>İçerik</dt>
            <dd>{document.contentAvailable ? "Mevcut" : "Yok"}</dd>
          </div>
          <div>
            <dt>Harici Referans</dt>
            <dd>{document.externalReference ?? document.externalCode ?? document.externalId}</dd>
          </div>
          <div>
            <dt>Kaynak</dt>
            <dd>
              {getErpProviderLabel(document.provider)} ·{" "}
              {getErpSourceFreshnessLabel(document.freshness)}
            </dd>
          </div>
          <div>
            <dt>Son Güncelleme</dt>
            <dd>{formatErpDateTime(document.updatedAtUtc)}</dd>
          </div>
        </dl>
      </section>

      <p>
        <Link href="/erp-entegrasyonlari/belgeler">← Belge listesine dön</Link>
        {" · "}
        <Link href="/erp-entegrasyonlari/uretim">Üretim</Link>
      </p>
    </main>
  );
}
