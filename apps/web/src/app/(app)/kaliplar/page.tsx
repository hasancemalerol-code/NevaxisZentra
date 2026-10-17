

import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { MoldRegistryView } from "@/features/molds/components/mold-registry-view";
import "@/features/molds/components/mold-registry.css";
import { readMoldRegistry } from "@/features/molds/server/mold-reader";

import { FailureStatePage } from "@/components/ui/failure-state";

export default async function MoldRegistryPage() {
  await forbidUnlessGranted("mold.read");

  const result = await readMoldRegistry();

  if (result.status === "failure") {
    return (
      <div className="mold-registry-page">
        <header className="mold-registry-header">
          <div className="mold-registry-header__intro">
            <p className="mold-registry-header__eyebrow">KALIP YÖNETİMİ</p>
            <h1>Kalıplar</h1>
            <p className="mold-registry-header__subtitle">
              Kalıp yaşam döngüsü, bakım geçmişi ve makine uyumluluğunu tek operasyon yüzeyinden yönetin.
            </p>
          </div>
        </header>
        <FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Kalıp verisi yüklenemedi" inline />
      </div>
    );
  }

  return (
    <MoldRegistryView
      summary={result.summary}
      molds={result.molds}
      canManage={result.canManage}
    />
  );
}
