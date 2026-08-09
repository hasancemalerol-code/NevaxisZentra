import Link from "next/link";

import type { ErpWritebackDetail, ErpWritebackPermissions } from "../domain/erp";
import {
  formatErpDateTime,
  formatErpQuantity,
  getErpEntityTypeLabel,
  getErpWritebackCommandTypeLabel,
  getErpWritebackDecisionLabel,
  getErpWritebackStatusLabel,
  getErpWritebackStatusTone,
} from "../presentation/erp-labels";
import { ErpWritebackActionPanel } from "./erp-writeback-action-panel";
import {
  ErpDataTable,
  ErpDetailSection,
  ErpEmptyState,
  ErpMetadataGrid,
  ErpMetadataItem,
  ErpPageHeader,
  ErpProviderBadge,
  ErpSimulatorNotice,
  ErpStatusBadge,
  getErpActorDisplayLabel,
} from "./erp-ui-primitives";
import { ErpWorkspaceNav } from "./erp-workspace-nav";
import "./erp-workspace.css";

type ErpWritebackDetailViewProps = Readonly<{
  writeback: ErpWritebackDetail;
  permissions: ErpWritebackPermissions;
}>;

export function ErpWritebackDetailView({ writeback, permissions }: ErpWritebackDetailViewProps) {
  const statusTone = getErpWritebackStatusTone(writeback.status);
  const isSimulator = writeback.provider === "simulator";

  return (
    <main className="production-execution-page erp-workspace-page erp-workspace-page--dense">
      <ErpPageHeader
        title={getErpWritebackCommandTypeLabel(writeback.commandType)}
        description="Onaylı ERP writeback isteği detayı."
        notice={isSimulator ? <ErpSimulatorNotice variant="writeback" /> : null}
      />

      <ErpWorkspaceNav activeHref="/erp-entegrasyonlari/onayli-islemler" />

      <ErpDetailSection title="İşlem Özeti">
        <ErpMetadataGrid>
          <ErpMetadataItem label="Komut">
            {getErpWritebackCommandTypeLabel(writeback.commandType)}
          </ErpMetadataItem>
          <ErpMetadataItem label="Durum">
            <ErpStatusBadge
              label={getErpWritebackStatusLabel(writeback.status)}
              tone={statusTone}
            />
          </ErpMetadataItem>
          <ErpMetadataItem label="Sürüm">{writeback.version}</ErpMetadataItem>
        </ErpMetadataGrid>
      </ErpDetailSection>

      <ErpDetailSection title="Hedef ERP">
        <ErpMetadataGrid>
          <ErpMetadataItem label="Sağlayıcı">
            <ErpProviderBadge provider={writeback.provider} />
          </ErpMetadataItem>
          <ErpMetadataItem label="Hedef Varlık Türü">
            {writeback.targetEntityType
              ? getErpEntityTypeLabel(writeback.targetEntityType)
              : "—"}
          </ErpMetadataItem>
          <ErpMetadataItem label="Hedef Harici Kayıt">
            {writeback.targetExternalId ?? "—"}
          </ErpMetadataItem>
        </ErpMetadataGrid>
      </ErpDetailSection>

      <ErpDetailSection title="Etkilenecek Kayıtlar">
        <ErpMetadataGrid>
          <ErpMetadataItem label="Müşteri">
            {writeback.payloadSummary.customerExternalId ?? "—"}
          </ErpMetadataItem>
          <ErpMetadataItem label="Sipariş Referansı">
            {writeback.payloadSummary.orderReference ?? "—"}
          </ErpMetadataItem>
          <ErpMetadataItem label="İstenen Teslim">
            {formatErpDateTime(writeback.payloadSummary.requestedDeliveryDateUtc)}
          </ErpMetadataItem>
        </ErpMetadataGrid>
      </ErpDetailSection>

      <ErpDetailSection title="Veri Özeti">
        <p>{writeback.payloadSummary.lineCount} satır</p>
        {writeback.payloadSummary.lines.length === 0 ? (
          <ErpEmptyState title="Payload satırı yok." />
        ) : (
          <ErpDataTable>
            <thead>
              <tr>
                <th>Ürün</th>
                <th>Miktar</th>
              </tr>
            </thead>
            <tbody>
              {writeback.payloadSummary.lines.map((line) => (
                <tr key={`${line.productExternalId}-${line.unit}`}>
                  <td>{line.productExternalId}</td>
                  <td>{formatErpQuantity(line.quantity, line.unit)}</td>
                </tr>
              ))}
            </tbody>
          </ErpDataTable>
        )}
      </ErpDetailSection>

      <ErpDetailSection title="Talep Bilgisi">
        <ErpMetadataGrid>
          <ErpMetadataItem label="Talep Eden">
            {getErpActorDisplayLabel(writeback.requestedBySubjectId)}
          </ErpMetadataItem>
          <ErpMetadataItem label="Talep Tarihi">
            {formatErpDateTime(writeback.requestedAtUtc)}
          </ErpMetadataItem>
          <ErpMetadataItem label="Onaya Gönderim">
            {formatErpDateTime(writeback.submittedAtUtc)}
          </ErpMetadataItem>
        </ErpMetadataGrid>
      </ErpDetailSection>

      <ErpDetailSection title="Onay">
        <ErpWritebackActionPanel
          writebackId={writeback.id}
          status={writeback.status}
          version={writeback.version}
          permissions={permissions}
        />

        {writeback.approvals.length === 0 ? (
          <ErpEmptyState title="Henüz onay kaydı yok." />
        ) : (
          <ErpDataTable className="erp-table--writeback-approvals">
            <thead>
              <tr>
                <th>Karar</th>
                <th>Onaylayan</th>
                <th>Tarih</th>
                <th>Sürüm</th>
                <th>Yorum</th>
              </tr>
            </thead>
            <tbody>
              {writeback.approvals.map((approval) => (
                <tr key={approval.id}>
                  <td>{getErpWritebackDecisionLabel(approval.decision)}</td>
                  <td>{getErpActorDisplayLabel(approval.decidedBySubjectId)}</td>
                  <td>{formatErpDateTime(approval.decidedAtUtc)}</td>
                  <td>{approval.requestVersion}</td>
                  <td>{approval.comment ?? "—"}</td>
                </tr>
              ))}
            </tbody>
          </ErpDataTable>
        )}
      </ErpDetailSection>

      <ErpDetailSection title="Uygulama Sonucu">
        <ErpMetadataGrid>
          <ErpMetadataItem label="Uygulama Tarihi">
            {formatErpDateTime(writeback.executedAtUtc)}
          </ErpMetadataItem>
          <ErpMetadataItem label="Harici Referans">
            {writeback.externalExecutionReference ?? "—"}
          </ErpMetadataItem>
        </ErpMetadataGrid>

        {writeback.lastErrorMessage ? (
          <div className="erp-writeback-actions__error" role="alert">
            {writeback.lastErrorCode ? `${writeback.lastErrorCode}: ` : ""}
            {writeback.lastErrorMessage}
          </div>
        ) : null}
      </ErpDetailSection>

      <p>
        <Link href="/erp-entegrasyonlari/onayli-islemler">← Onaylı işlemler listesine dön</Link>
      </p>
    </main>
  );
}
