import Link from "next/link";

import {
  fetchCurrentEffectiveAccessResult,
  hasGrantedPermission,
} from "@/features/admin-access/server/admin-access-effective-access";
import { RealFactoryPilotEmptyState } from "@/features/real-factory-pilot/components/real-factory-pilot-empty-state";
import { RealFactoryPilotPanel } from "@/features/real-factory-pilot/components/real-factory-pilot-panel";
import { readRealFactoryPilotWorkspaceState } from "@/features/real-factory-pilot/server/real-factory-pilot-reader";

export const dynamic = "force-dynamic";

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
      <main className="production-execution-page">
        <header className="production-execution-header">
          <p>AYARLAR · ENTEGRASYONLAR</p>
          <h1>Gerçek Fabrika Bağlantıları</h1>
          <p>
            <Link href="/ayarlar/entegrasyonlar">← Entegrasyonlar</Link>
          </p>
        </header>
        <div role="alert" className="state-panel error-state">{result.message}</div>
      </main>
    );
  }

  const access = await fetchCurrentEffectiveAccessResult();
  const canInitialize =
    result.syntheticInitializationAvailable &&
    access.status === "ok" &&
    hasGrantedPermission(access.access, "real-factory-pilot.manage");

  return (
    <main className="production-execution-page">
      <header className="production-execution-header">
        <p>AYARLAR · ENTEGRASYONLAR</p>
        <h1>Gerçek Fabrika Bağlantıları</h1>
        <p>
          <Link href="/ayarlar/entegrasyonlar">← Entegrasyonlar</Link>
          {" · "}
          <Link href="/ayarlar/entegrasyonlar/merkez">Entegrasyon Merkezi</Link>
        </p>
      </header>
      {result.workspace ? (
        <RealFactoryPilotPanel workspace={result.workspace} />
      ) : (
        <RealFactoryPilotEmptyState canInitialize={canInitialize} notice={notice} />
      )}
    </main>
  );
}
