import Link from "next/link";

import { readIntegrationHub } from "@/features/integration/server/integration-reader";

export const dynamic = "force-dynamic";

export default async function EntegrasyonlarPage() {
  const result = await readIntegrationHub();

  if (result.status === "failure") {
    return (
      <main className="production-execution-page">
        <header className="production-execution-header">
          <p>AYARLAR</p>
          <h1>Entegrasyonlar</h1>
        </header>
        <div role="alert" className="state-panel error-state">{result.message}</div>
      </main>
    );
  }

  return (
    <main className="production-execution-page">
      <header className="production-execution-header">
        <p>AYARLAR</p>
        <h1>Entegrasyonlar</h1>
        <p>
          <Link href="/ayarlar/entegrasyonlar/merkez">Entegrasyon Merkezi</Link>
          {" — canlı olay akışı, sağlık ve karantina görünümü"}
          {" · "}
          <Link href="/ayarlar/entegrasyonlar/gercek-fabrika">Gerçek Fabrika Pilot</Link>
        </p>
      </header>

      <section className="state-panel">
        <h2>Bağlantılar</h2>
        {result.connectors.length === 0 ? (
          <p>Henüz entegrasyon bağlantısı yok.</p>
        ) : (
          <ul>
            {result.connectors.map((connector) => (
              <li key={connector.id}>
                <strong>{connector.name}</strong> — {connector.connectorKind}
                {" "}
                <span>
                  ({connector.healthStatus} / {connector.freshnessState})
                </span>
                <div>
                  {(connector.connectorKind === "file-import" || connector.connectorKind === "folder-watch") && (
                    <Link href={`/ayarlar/entegrasyonlar/${connector.id}/dosya-aktarimi`}>
                      Dosya aktarımı
                    </Link>
                  )}
                  {connector.connectorKind !== "file-import" && connector.connectorKind !== "folder-watch" && (
                    <span>REST / Webhook / DB / ERP bağlantıları API üzerinden yönetilir.</span>
                  )}
                </div>
              </li>
            ))}
          </ul>
        )}
      </section>
    </main>
  );
}
