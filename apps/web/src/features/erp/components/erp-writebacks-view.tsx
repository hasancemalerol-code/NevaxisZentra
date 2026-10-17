import { CircleCheck, Clock, ShieldCheck } from "lucide-react";

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
  ErpKpiCard,
  ErpKpiGrid,
  ErpPageHeader,
  ErpProviderBadge,
  ErpReadonlyNotice,
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
    <div className="production-execution-page erp-workspace-page erp-workspace-page--dense">
      <ErpPageHeader
        title="Onaylı İşlemler"
        description="ERP’ye yazılacak işlem istekleri — iki aşamalı onay ve uygulama akışı."
        notice={
          <>
            <ErpReadonlyNotice>
              ERP’ye yazma yalnızca insan onayıyla gerçekleşir; onaysız işlem uygulanmaz.
            </ErpReadonlyNotice>
            <ErpSimulatorNotice variant="writeback" />
          </>
        }
      />

      <ErpWorkspaceNav activeHref="/erp-entegrasyonlari/onayli-islemler" />

      <ErpKpiGrid columns={3}>
        <ErpKpiCard
          label="İşlem İstekleri"
          value={writebacks.length}
          description="Kayıtlı ERP işlem isteği"
          icon={<ShieldCheck aria-hidden="true" />}
        />
        <ErpKpiCard
          label="Onay Bekliyor"
          value={writebacks.filter((writeback) => writeback.status === "pending-approval").length}
          description="İnsan onayı bekleyen"
          icon={<Clock aria-hidden="true" />}
          tone="warning"
        />
        <ErpKpiCard
          label="Tamamlandı"
          value={writebacks.filter((writeback) => writeback.status === "succeeded").length}
          description="ERP’ye başarıyla yazılan"
          icon={<CircleCheck aria-hidden="true" />}
          tone="success"
        />
      </ErpKpiGrid>

      <section className="erp-section">
        <ErpSectionHeader
          title="Onaylı İşlem İstekleri"
          description="Komut türü, durum, müşteri referansı ve onay geçmişi"
        />
        {writebacks.length === 0 ? (
          <ErpEmptyState
            title="Henüz onaylı işlem kaydı yok."
            description="ERP işlem istekleri oluşturulduğunda onay geçmişiyle birlikte burada listelenir."
            actionHref="/erp-entegrasyonlari/senkronizasyonlar"
            actionLabel="Eşitleme Merkezi"
          />
        ) : (
          <ErpDataTable label="Onaylı işlemler" className="erp-table--writebacks">
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
    </div>
  );
}
