import type { ErpWritebackSummary } from "../domain/erp";
import {
  formatErpDateTime,
  getErpWritebackCommandTypeLabel,
  getErpWritebackStatusLabel,
  getErpWritebackStatusTone,
} from "../presentation/erp-labels";
import {
  ErpDataTable,
  ErpEmptyState,
  ErpPageHeader,
  ErpProviderBadge,
  ErpSectionHeader,
  ErpSimulatorNotice,
  ErpStatusBadge,
  ErpTableAction,
  ErpTableActionCell,
  getErpActorDisplayLabel,
} from "./erp-ui-primitives";
import { ErpWorkspaceNav } from "./erp-workspace-nav";
import "./erp-workspace.css";

type ErpWritebacksViewProps = Readonly<{
  writebacks: readonly ErpWritebackSummary[];
}>;

export function ErpWritebacksView({ writebacks }: ErpWritebacksViewProps) {
  return (
    <main className="production-execution-page erp-workspace-page">
      <ErpPageHeader
        title="Onaylı İşlemler"
        description="ERP writeback istekleri — onay ve uygulama akışı. Maker-checker ile kontrollü ERP yazma."
        notice={<ErpSimulatorNotice variant="writeback" />}
      />

      <ErpWorkspaceNav activeHref="/erp-entegrasyonlari/onayli-islemler" />

      <section className="erp-section">
        <ErpSectionHeader
          title="Writeback İstekleri"
          description="Komut türü, durum, müşteri referansı ve onay geçmişi"
        />
        {writebacks.length === 0 ? (
          <ErpEmptyState title="Henüz onaylı işlem kaydı yok." />
        ) : (
          <ErpDataTable className="erp-table--writebacks">
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
                      <ErpStatusBadge
                        label={getErpWritebackStatusLabel(writeback.status)}
                        tone={statusTone}
                      />
                    </td>
                    <td>{getErpActorDisplayLabel(writeback.requestedBySubjectId)}</td>
                    <td>{formatErpDateTime(writeback.requestedAtUtc)}</td>
                    <td>{formatErpDateTime(writeback.latestApprovalAtUtc)}</td>
                    <td>
                      <ErpProviderBadge provider={writeback.provider} />
                    </td>
                    <ErpTableActionCell>
                      <ErpTableAction href={`/erp-entegrasyonlari/onayli-islemler/${writeback.id}`}>
                        Aç
                      </ErpTableAction>
                    </ErpTableActionCell>
                  </tr>
                );
              })}
            </tbody>
          </ErpDataTable>
        )}
      </section>
    </main>
  );
}
