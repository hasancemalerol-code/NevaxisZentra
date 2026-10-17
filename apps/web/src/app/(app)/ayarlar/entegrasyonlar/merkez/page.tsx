import { readIntegrationOperationsCenter } from "@/features/integration/server/integration-operations-reader";

import { FailureStatePage } from "@/components/ui/failure-state";
import { PageHeader } from "@/components/ui/page";
import { StatePage, StatePageLink } from "@/components/ui/state-page";
import { enumLabel } from "@/lib/presentation/enum-labels";
import { formatDateTime } from "@/lib/presentation/format";

export const dynamic = "force-dynamic";

const CENTER_DESCRIPTION = "Canlı fabrika verisi, bağlantı sağlığı ve olay akışı.";

export default async function EntegrasyonMerkeziPage() {
  const result = await readIntegrationOperationsCenter();

  if (result.status === "failure") {
    return (
      <div className="production-execution-page">
        <PageHeader eyebrow="Ayarlar" title="Entegrasyon Merkezi" description={CENTER_DESCRIPTION} />
        <FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Bu bilgi şu anda alınamıyor" inline />
      </div>
    );
  }

  const nothingToShow = result.operations.length === 0 && result.events.length === 0;

  return (
    <div className="production-execution-page">
      <PageHeader
        eyebrow="Ayarlar"
        title="Entegrasyon Merkezi"
        description={CENTER_DESCRIPTION}
        actions={<StatePageLink href="/ayarlar/entegrasyonlar">Entegrasyonlar</StatePageLink>}
      />

      {nothingToShow ? (
        <StatePage
          kind="empty"
          inline
          title="Henüz bağlantı verisi yok"
          description="Bağlantı sağlığı ve işlenen fabrika olayları, bir bağlantı veri göndermeye başladığında burada görünür."
          actions={<StatePageLink href="/ayarlar/veri-aktarim-merkezi" primary>Veri Aktarım Merkezi</StatePageLink>}
        />
      ) : null}

      {result.operations.length > 0 ? (
        <section className="state-panel">
          <h2>Bağlantı Sağlığı</h2>
          <table aria-label="Bağlantı Sağlığı">
            <thead>
              <tr>
                <th>Bağlantı</th>
                <th>Sağlık</th>
                <th>Son veri</th>
                <th>Bugün işlenen</th>
                <th>Karantina</th>
                <th>Hata</th>
              </tr>
            </thead>
            <tbody>
              {result.operations.map((op) => (
                <tr key={op.connectorId}>
                  <td>{op.name} ({enumLabel(op.connectorKind)})</td>
                  <td>{enumLabel(op.healthState)}</td>
                  <td>{formatDateTime(op.lastEventReceivedAtUtc)}</td>
                  <td>{op.processedToday}</td>
                  <td>{op.quarantinedToday}</td>
                  <td>{op.failedToday}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>
      ) : null}

      {result.events.length > 0 ? (
        <section className="state-panel">
          <h2>Canlı Olay Monitörü</h2>
          <table aria-label="Canlı Olay Monitörü">
            <thead>
              <tr>
                <th>Kaynak</th>
                <th>Olay</th>
                <th>Makine</th>
                <th>Sipariş</th>
                <th>Gerçekleşme</th>
                <th>Alınma</th>
                <th>Durum</th>
                <th>Vaka</th>
              </tr>
            </thead>
            <tbody>
              {result.events.map((event) => (
                <tr key={event.canonicalEventId}>
                  <td>{event.sourceSystem}</td>
                  <td>{enumLabel(event.eventType)}</td>
                  <td>{event.machineId ?? "—"}</td>
                  <td>{event.orderId ?? "—"}</td>
                  <td>{formatDateTime(event.occurredAtUtc)}</td>
                  <td>{formatDateTime(event.receivedAtUtc)}</td>
                  <td>{enumLabel(event.processingState)}</td>
                  <td>{event.routedCaseId ?? "—"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </section>
      ) : null}
    </div>
  );
}
