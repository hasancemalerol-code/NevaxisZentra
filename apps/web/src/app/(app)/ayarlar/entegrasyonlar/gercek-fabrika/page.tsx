import {
  fetchCurrentEffectiveAccessResult,
  hasGrantedPermission,
} from "@/features/admin-access/server/admin-access-effective-access";
import { RealFactoryPilotEmptyState } from "@/features/real-factory-pilot/components/real-factory-pilot-empty-state";
import { RealFactoryPilotPanel } from "@/features/real-factory-pilot/components/real-factory-pilot-panel";
import { readRealFactoryPilotWorkspaceState } from "@/features/real-factory-pilot/server/real-factory-pilot-reader";

import { FailureStatePage } from "@/components/ui/failure-state";
import { PageHeader } from "@/components/ui/page";
import { StatePageLink } from "@/components/ui/state-page";

export const dynamic = "force-dynamic";

const DESCRIPTION = "Fabrika sistemlerinizin bağlantı durumunu ve veri kalitesini izleyin.";

type GercekFabrikaPilotPageProps = Readonly<{
  searchParams?: Promise<Readonly<Record<string, string | string[] | undefined>>>;
}>;

export default async function GercekFabrikaPilotPage({ searchParams }: GercekFabrikaPilotPageProps) {
  // Viewing this page is read-only. It never creates connectors, runs or synthetic data.
  const result = await readRealFactoryPilotWorkspaceState();
  const params = await searchParams;
  const notice = typeof params?.pilot === "string" ? params.pilot : null;

  if (result.status === "failure") {
    return (
      <div className="production-execution-page">
        <PageHeader eyebrow="Ayarlar" title="Fabrika Bağlantıları" description={DESCRIPTION} actions={<StatePageLink href="/ayarlar/entegrasyonlar">Entegrasyonlar</StatePageLink>} />
        <FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Bu bilgi şu anda alınamıyor" inline />
      </div>
    );
  }

  const access = await fetchCurrentEffectiveAccessResult();
  const canInitialize =
    result.syntheticInitializationAvailable &&
    access.status === "ok" &&
    hasGrantedPermission(access.access, "real-factory-pilot.manage");

  return (
    <div className="production-execution-page">
      <PageHeader
        eyebrow="Ayarlar"
        title="Fabrika Bağlantıları"
        description={DESCRIPTION}
        actions={<StatePageLink href="/ayarlar/entegrasyonlar">Entegrasyonlar</StatePageLink>}
      />
      {result.workspace ? (
        <RealFactoryPilotPanel workspace={result.workspace} />
      ) : (
        <RealFactoryPilotEmptyState canInitialize={canInitialize} notice={notice} />
      )}
    </div>
  );
}
