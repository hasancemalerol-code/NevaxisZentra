import Link from "next/link";

import { readIntegrationOperationsCenter } from "@/features/integration/server/integration-operations-reader";

export const dynamic = "force-dynamic";

export default async function EntegrasyonMerkeziPage() {
  const result = await readIntegrationOperationsCenter();

  if (result.status === "failure") {
    return (
      <main className="production-execution-page">
        <header className="production-execution-header">
          <p>AYARLAR</p>
          <h1>Entegrasyon Merkezi</h1>
        </header>
        <div role="alert" className="state-panel error-state">{result.message}</div>
      </main>
    );
  }

  return (
    <main className="production-execution-page">
      <header className="production-execution-header">
        <p>AYARLAR</p>
        <h1>Entegrasyon Merkezi</h1>
        <p>Canlı fabrika verisi, bağlantı sağlığı ve olay akışı.</p>
        <Link href="/ayarlar/entegrasyonlar">← Entegrasyonlar</Link>
      </header>

      <section className="state-panel">
        <h2>Bağlantı Sağlığı</h2>
        {result.operations.length === 0 ? (
          <p>Operasyon verisi henüz yok.</p>
        ) : (
          <table>
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
                  <td>{op.name} ({op.connectorKind})</td>
                  <td>{op.healthState}</td>
                  <td>{op.lastEventReceivedAtUtc ?? "—"}</td>
                  <td>{op.processedToday}</td>
                  <td>{op.quarantinedToday}</td>
                  <td>{op.failedToday}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>

      <section className="state-panel">
        <h2>Canlı Olay Monitörü</h2>
        {result.events.length === 0 ? (
          <p>Henüz işlenmiş fabrika olayı yok.</p>
        ) : (
          <table>
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
                  <td>{event.eventType}</td>
                  <td>{event.machineId ?? "—"}</td>
                  <td>{event.orderId ?? "—"}</td>
                  <td>{event.occurredAtUtc}</td>
                  <td>{event.receivedAtUtc}</td>
                  <td>{event.processingState}</td>
                  <td>{event.routedCaseId ?? "—"}</td>
                </tr>
              ))}
            </tbody>
          </table>
        )}
      </section>
    </main>
  );
}
