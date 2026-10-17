import { StatePage, StatePageLink } from "@/components/ui/state-page";

import { initializeRealFactoryPilotAction } from "../server/real-factory-pilot-actions";

type Props = Readonly<{
  /** Development/Demo/Presentation deployment AND the caller holds real-factory-pilot.manage. */
  canInitialize: boolean;
  notice: string | null;
}>;

const NOTICES: Readonly<Record<string, string>> = {
  denied: "Sentetik pilot verisi oluşturma yetkiniz yok.",
  unavailable: "Yetki doğrulaması şu anda yapılamıyor; işlem yapılmadı.",
  failed: "Sentetik pilot verisi oluşturulamadı.",
};

/** Truthful empty state: no factory connection exists for this facility, and none is created by viewing the page. */
export function RealFactoryPilotEmptyState({ canInitialize, notice }: Props) {
  const message = notice ? NOTICES[notice] : undefined;

  return (
    <section data-testid="real-factory-pilot-empty">
      <StatePage
        kind="empty"
        inline
        title="Henüz fabrika bağlantısı yok"
        description="Bu tesis için henüz bir fabrika sistemi bağlanmadı. Bağlantılar eklendiğinde durumları burada izlenir."
        actions={
          <StatePageLink href="/ayarlar/veri-aktarim-merkezi" primary>Veri Aktarım Merkezi</StatePageLink>
        }
      />
      {message ? (
        <p role="alert" data-testid="real-factory-pilot-notice">
          {message}
        </p>
      ) : null}
      {canInitialize ? (
        <form action={initializeRealFactoryPilotAction} data-testid="real-factory-pilot-initialize-form">
          <p>
            <strong>Yalnızca geliştirme / demo ortamı:</strong> aşağıdaki işlem SENTETİK pilot verisi (örnek hat ve
            bağlantılar) oluşturur. Gerçek müşteri verisi değildir; işlem denetim kaydına yazılır.
          </p>
          <button type="submit">Sentetik pilot verisini oluştur</button>
        </form>
      ) : null}
    </section>
  );
}
