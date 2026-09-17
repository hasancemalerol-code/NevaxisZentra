import Link from "next/link";

import { RealFactoryPilotPanel } from "@/features/real-factory-pilot/components/real-factory-pilot-panel";
import { readRealFactoryPilotWorkspaceState } from "@/features/real-factory-pilot/server/real-factory-pilot-reader";

export const dynamic = "force-dynamic";

export default async function GercekFabrikaPilotPage() {
  const result = await readRealFactoryPilotWorkspaceState();

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
      <RealFactoryPilotPanel workspace={result.workspace} />
    </main>
  );
}
