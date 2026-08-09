import Link from "next/link";

import type { ErpWritebackSummary } from "../domain/erp";
import {
  formatErpDateTime,
  getErpProviderLabel,
  getErpWritebackCommandTypeLabel,
  getErpWritebackStatusLabel,
  getErpWritebackStatusTone,
} from "../presentation/erp-labels";
import { ErpWorkspaceNav } from "./erp-workspace-nav";
import "./erp-workspace.css";

type ErpWritebacksViewProps = Readonly<{
  writebacks: readonly ErpWritebackSummary[];
}>;

export function ErpWritebacksView({ writebacks }: ErpWritebacksViewProps) {
  return (
    <main className="production-execution-page erp-workspace-page">
      <header className="erp-workspace-header production-execution-header">
        <div className="erp-workspace-header__intro">
          <p>ERP ENTEGRASYONLARI</p>
          <h1>Onaylı İşlemler</h1>
          <span>
            ERP writeback istekleri — onay ve uygulama akışı. Maker-checker ile kontrollü ERP
            yazma.
          </span>
          <p className="erp-simulator-note erp-simulator-note--warning">
            ERP Simulator — gerçek ERP işlemi değildir.
          </p>
        </div>
      </header>

      <ErpWorkspaceNav activeHref="/erp-entegrasyonlari/onayli-islemler" />

      <section className="erp-section">
        <div className="erp-section__header">
          <h2>Writeback İstekleri</h2>
          <p>Komut türü, durum, müşteri referansı ve onay geçmişi</p>
        </div>
        <div className="erp-table-wrap">
          {writebacks.length === 0 ? (
            <div className="erp-empty">Henüz onaylı işlem kaydı yok.</div>
          ) : (
            <table className="erp-table erp-table--writebacks">
              <thead>
                <tr>
                  <th>Komut</th>
                  <th>Müşteri</th>
                  <th>Referans</th>
                  <th>Satır</th>
                  <th>Durum</th>
                  <th>Talep Eden</th>
                  <th>Talep Tarihi</th>
                  <th>Son Onay</th>
                  <th>Kaynak</th>
                  <th>İşlem</th>
                </tr>
              </thead>
              <tbody>
                {writebacks.map((writeback) => {
                  const statusTone = getErpWritebackStatusTone(writeback.status);

                  return (
                    <tr key={writeback.id}>
                      <td>{getErpWritebackCommandTypeLabel(writeback.commandType)}</td>
                      <td>{writeback.customerExternalId ?? "—"}</td>
                      <td>{writeback.orderReference ?? "—"}</td>
                      <td>{writeback.lineCount}</td>
                      <td>
                        <span className={`erp-status erp-status--${statusTone}`}>
                          {getErpWritebackStatusLabel(writeback.status)}
                        </span>
                      </td>
                      <td>{writeback.requestedBySubjectId}</td>
                      <td>{formatErpDateTime(writeback.requestedAtUtc)}</td>
                      <td>{formatErpDateTime(writeback.latestApprovalAtUtc)}</td>
                      <td>{getErpProviderLabel(writeback.provider)}</td>
                      <td>
                        <div className="erp-table-actions">
                          <Link href={`/erp-entegrasyonlari/onayli-islemler/${writeback.id}`}>
                            Aç
                          </Link>
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
