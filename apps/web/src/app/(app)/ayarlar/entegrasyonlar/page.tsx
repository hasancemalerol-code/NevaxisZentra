import Link from "next/link";

import { readIntegrationHub } from "@/features/integration/server/integration-reader";

import { FailureStatePage } from "@/components/ui/failure-state";
import { PageHeader } from "@/components/ui/page";
import { StatePage, StatePageLink } from "@/components/ui/state-page";
import { enumLabel } from "@/lib/presentation/enum-labels";

export const dynamic = "force-dynamic";

const HUB_DESCRIPTION = "Fabrika sistemlerinizin bağlantı durumunu, olay akışını ve karantina kayıtlarını izleyin.";

export default async function EntegrasyonlarPage() {
  const result = await readIntegrationHub();

  if (result.status === "failure") {
    return (
      <div className="production-execution-page">
        <PageHeader eyebrow="Ayarlar" title="Entegrasyonlar" description={HUB_DESCRIPTION} />
        <FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Bu bilgi şu anda alınamıyor" inline />
      </div>
    );
  }

  return (
    <div className="production-execution-page">
      <PageHeader
        eyebrow="Ayarlar"
        title="Entegrasyonlar"
        description={HUB_DESCRIPTION}
        actions={
          result.connectors.length === 0 ? undefined : (
            <>
              <StatePageLink href="/ayarlar/entegrasyonlar/merkez">Entegrasyon Merkezi</StatePageLink>
              <StatePageLink href="/ayarlar/entegrasyonlar/gercek-fabrika">Fabrika Bağlantıları</StatePageLink>
            </>
          )
        }
      />

      {result.connectors.length === 0 ? (
        <StatePage
          kind="empty"
          inline
          title="Henüz entegrasyon bağlantısı yok"
          description="Fabrika verilerini Veri Aktarım Merkezi üzerinden içeri alabilirsiniz; bağlantılar eklendiğinde burada izlenir."
          actions={<StatePageLink href="/ayarlar/veri-aktarim-merkezi" primary>Veri Aktarım Merkezi</StatePageLink>}
        />
      ) : (
        <section className="state-panel">
          <h2>Bağlantılar</h2>
          <ul>
            {result.connectors.map((connector) => (
              <li key={connector.id}>
                <strong>{connector.name}</strong> — {enumLabel(connector.connectorKind)}
                {" "}
                <span>
                  ({enumLabel(connector.healthStatus)} / {enumLabel(connector.freshnessState)})
                </span>
                <div>
                  {(connector.connectorKind === "file-import" || connector.connectorKind === "folder-watch") && (
                    <Link href={`/ayarlar/entegrasyonlar/${connector.id}/dosya-aktarimi`}>
                      Dosya aktarımı
                    </Link>
                  )}
                  {connector.connectorKind !== "file-import" && connector.connectorKind !== "folder-watch" && (
                    <span>ERP, veritabanı ve servis bağlantıları buradan yönetilir.</span>
                  )}
                </div>
              </li>
            ))}
          </ul>
        </section>
      )}
    </div>
  );
}
