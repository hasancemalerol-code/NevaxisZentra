import { Boxes } from "lucide-react";

import { MoldRegistryView } from "@/features/molds/components/mold-registry-view";
import { readMoldRegistry } from "@/features/molds/server/mold-reader";

export default async function MoldRegistryPage() {
  const result = await readMoldRegistry();

  if (result.status === "failure") {
    const isUnauthorized = result.code === "permission-denied" || result.code === "unauthenticated";

    return (
      <main className="mold-registry-page">
        <header className="mold-registry-header">
          <div>
            <p>KALIP YÖNETİMİ</p>
            <h1>Kalıplar</h1>
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
