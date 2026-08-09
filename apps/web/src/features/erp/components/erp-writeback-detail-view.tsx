import Link from "next/link";

import type { ErpWritebackDetail, ErpWritebackPermissions } from "../domain/erp";
import {
  formatErpDateTime,
  formatErpQuantity,
  getErpProviderLabel,
  getErpWritebackCommandTypeLabel,
  getErpWritebackDecisionLabel,
  getErpWritebackStatusLabel,
  getErpWritebackStatusTone,
} from "../presentation/erp-labels";
import { ErpWritebackActionPanel } from "./erp-writeback-action-panel";
import { ErpWorkspaceNav } from "./erp-workspace-nav";
import "./erp-workspace.css";

type ErpWritebackDetailViewProps = Readonly<{
  writeback: ErpWritebackDetail;
  permissions: ErpWritebackPermissions;
}>;

export function ErpWritebackDetailView({ writeback, permissions }: ErpWritebackDetailViewProps) {
  const statusTone = getErpWritebackStatusTone(writeback.status);

  return (
    <main className="production-execution-page erp-workspace-page">
      <header className="erp-workspace-header production-execution-header">
        <div className="erp-workspace-header__intro">
          <p>ERP ENTEGRASYONLARI</p>
          <h1>{getErpWritebackCommandTypeLabel(writeback.commandType)}</h1>
          <span>Onaylı ERP writeback isteği detayı.</span>
          <p className="erp-simulator-note erp-simulator-note--warning">
            ERP Simulator — gerçek ERP işlemi değildir.
          </p>
        </div>
      </header>

      <ErpWorkspaceNav activeHref="/erp-entegrasyonlari/onayli-islemler" />

      <section className="erp-section">
        <div className="erp-section__header">
          <h2>İstek Özeti</h2>
        </div>
        <dl className="erp-detail-grid">
          <div>
            <dt>Komut</dt>
            <dd>{getErpWritebackCommandTypeLabel(writeback.commandType)}</dd>
          </div>
          <div>
            <dt>Durum</dt>
            <dd>
              <span className={`erp-status erp-status--${statusTone}`}>
                {getErpWritebackStatusLabel(writeback.status)}
              </span>
            </dd>
          </div>
          <div>
            <dt>Müşteri</dt>
            <dd>{writeback.payloadSummary.customerExternalId ?? "—"}</dd>
          </div>
          <div>
            <dt>Sipariş Referansı</dt>
            <dd>{writeback.payloadSummary.orderReference ?? "—"}</dd>
          </div>
          <div>
            <dt>İstenen Teslim</dt>
            <dd>{formatErpDateTime(writeback.payloadSummary.requestedDeliveryDateUtc)}</dd>
          </div>
          <div>
            <dt>Talep Eden</dt>
            <dd>{writeback.requestedBySubjectId}</dd>
          </div>
          <div>
            <dt>Talep Tarihi</dt>
            <dd>{formatErpDateTime(writeback.requestedAtUtc)}</dd>
          </div>
          <div>
            <dt>Onaya Gönderim</dt>
            <dd>{formatErpDateTime(writeback.submittedAtUtc)}</dd>
          </div>
          <div>
            <dt>Uygulama</dt>
            <dd>{formatErpDateTime(writeback.executedAtUtc)}</dd>
          </div>
          <div>
            <dt>Harici Referans</dt>
            <dd>{writeback.externalExecutionReference ?? "—"}</dd>
          </div>
          <div>
            <dt>Kaynak</dt>
            <dd>{getErpProviderLabel(writeback.provider)}</dd>
          </div>
          <div>
            <dt>Sürüm</dt>
            <dd>{writeback.version}</dd>
          </div>
        </dl>

        {writeback.lastErrorMessage ? (
          <p className="erp-writeback-actions__error" role="alert">
            {writeback.lastErrorCode ? `${writeback.lastErrorCode}: ` : ""}
            {writeback.lastErrorMessage}
          </p>
        ) : null}
      </section>

      <ErpWritebackActionPanel
        writebackId={writeback.id}
        status={writeback.status}
        version={writeback.version}
        permissions={permissions}
      />

      <section className="erp-section">
        <div className="erp-section__header">
          <h2>Payload Satırları</h2>
          <p>{writeback.payloadSummary.lineCount} satır</p>
        </div>
        <div className="erp-table-wrap">
          {writeback.payloadSummary.lines.length === 0 ? (
            <div className="erp-empty">Payload satırı yok.</div>
          ) : (
            <table className="erp-table">
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
            </table>
          )}
        </div>
      </section>

      <section className="erp-section">
        <div className="erp-section__header">
          <h2>Onay Geçmişi</h2>
        </div>
        <div className="erp-table-wrap">
          {writeback.approvals.length === 0 ? (
            <div className="erp-empty">Henüz onay kaydı yok.</div>
          ) : (
            <table className="erp-table erp-table--writeback-approvals">
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
                    <td>{approval.decidedBySubjectId}</td>
                    <td>{formatErpDateTime(approval.decidedAtUtc)}</td>
                    <td>{approval.requestVersion}</td>
                    <td>{approval.comment ?? "—"}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </section>

      <p>
        <Link href="/erp-entegrasyonlari/onayli-islemler">← Onaylı işlemler listesine dön</Link>
      </p>
    </main>
  );
}
