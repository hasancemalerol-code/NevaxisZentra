import Link from "next/link";
import { ShieldCheck } from "lucide-react";

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

type ErpWritebackViewProps = Readonly<{
  writebacks: readonly ErpWritebackSummary[];
}>;

export function ErpWritebackView({ writebacks }: ErpWritebackViewProps) {
  return (
    <div className="production-execution-page erp-workspace-page">
      <header className="erp-workspace-header production-execution-header">
        <div className="erp-workspace-header__intro">
          <p>ERP ENTEGRASYONLARI</p>
          <h1>Onaylı İşlemler</h1>
          <span>
            ERP yazma istekleri — onay ve uygulama akışı. Detay ekranından işlem başlatılır.
          </span>
          <p className="erp-simulator-note">
            Örnek ERP bağlantısı — gerçek ERP işlemi değildir.
          </p>
        </div>
      </header>

      <ErpWorkspaceNav activeHref="/erp-entegrasyonlari/onayli-islemler" />

      <section className="erp-section">
        <div className="erp-section__header">
          <h2>Yazma İstekleri</h2>
          <p>Komut türü, durum, müşteri referansı ve onay geçmişi</p>
        </div>
        <div className="erp-table-wrap" tabIndex={0}>
          {writebacks.length === 0 ? (
            <div className="erp-empty">Henüz onaylı işlem kaydı yok.</div>
          ) : (
            <table aria-label="Yazma İstekleri" className="erp-table erp-table--writeback">
              <thead>
                <tr>
                  <th>Komut</th>
                  <th>Müşteri</th>
                  <th>Referans</th>
                  <th>Satır</th>
                  <th>Durum</th>
                  <th>Talep Tarihi</th>
                  <th>Son Onay</th>
                  <th>Kaynak</th>
                  <th>İşlem</th>
                </tr>
              </thead>
              <tbody>
                {writebacks.map((writeback) => (
                  <tr key={writeback.id}>
                    <td>{getErpWritebackCommandTypeLabel(writeback.commandType)}</td>
                    <td>{writeback.customerExternalId ?? "—"}</td>
                    <td>{writeback.orderReference ?? "—"}</td>
                    <td>{writeback.lineCount}</td>
                    <td>
                      <span
                        className={`erp-status erp-status--${getErpWritebackStatusTone(writeback.status)}`}
                      >
                        {getErpWritebackStatusLabel(writeback.status)}
                      </span>
                    </td>
                    <td>{formatErpDateTime(writeback.requestedAtUtc)}</td>
                    <td>
                      {writeback.latestApprovalDecision
                        ? `${writeback.latestApprovalDecision} · ${formatErpDateTime(writeback.latestApprovalAtUtc)}`
                        : "—"}
                    </td>
                    <td>{getErpProviderLabel(writeback.provider)}</td>
                    <td>
                      <div className="erp-table-actions">
                        <Link href={`/erp-entegrasyonlari/onayli-islemler/${writeback.id}`}>
                          Aç
                        </Link>
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>
      </section>

      {writebacks.length === 0 ? (
        <div className="erp-empty" aria-hidden="true">
          <ShieldCheck />
        </div>
      ) : null}
    </div>
  );
}
