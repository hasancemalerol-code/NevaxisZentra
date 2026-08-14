import { Boxes } from "lucide-react";

import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { MoldRegistryView } from "@/features/molds/components/mold-registry-view";
import "@/features/molds/components/mold-registry.css";
import { readMoldRegistry } from "@/features/molds/server/mold-reader";

export default async function MoldRegistryPage() {
  await forbidUnlessGranted("mold.read");

  const result = await readMoldRegistry();

  if (result.status === "failure") {
    const isUnauthorized = result.code === "permission-denied" || result.code === "unauthenticated";

    return (
      <main className="mold-registry-page">
        <header className="mold-registry-header">
          <div className="mold-registry-header__intro">
            <p className="mold-registry-header__eyebrow">KALIP YÖNETİMİ</p>
            <h1>Kalıplar</h1>
            <p className="mold-registry-header__subtitle">
              Kalıp yaşam döngüsü, bakım geçmişi ve makine uyumluluğunu tek operasyon yüzeyinden yönetin.
            </p>
          </div>
        </header>
        <div
          role="alert"
          className={isUnauthorized ? "mold-registry-unauthorized" : "mold-registry-error"}
        >
          <Boxes aria-hidden="true" />
          <div>
            <h3>{isUnauthorized ? "Bu modüle erişim yetkiniz yok" : "Kalıp verisi yüklenemedi"}</h3>
            <p>{result.message}</p>
          </div>
        </div>
      </main>
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
