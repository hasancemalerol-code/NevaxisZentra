import { Package } from "lucide-react";

import { MaterialRegistryView } from "@/features/materials/components/material-registry-view";
import "@/features/materials/components/material-registry.css";
import { readMaterialRegistry } from "@/features/materials/server/material-reader";

export default async function MaterialRegistryPage() {
  const result = await readMaterialRegistry();

  if (result.status === "failure") {
    const isUnauthorized = result.code === "permission-denied" || result.code === "unauthenticated";

    return (
      <main className="material-registry-page">
        <header className="material-registry-header">
          <div className="material-registry-header__intro">
            <p className="material-registry-header__eyebrow">MALZEME YÖNETİMİ</p>
            <h1>Malzemeler</h1>
            <p className="material-registry-header__subtitle">
              Polimer malzeme kayıtlarını, teknik özelliklerini ve yaşam döngüsü durumlarını yönetin.
            </p>
          </div>
        </header>
        <div
          role="alert"
          className={isUnauthorized ? "material-registry-unauthorized" : "material-registry-error"}
        >
          <Package aria-hidden="true" />
          <div>
            <h3>{isUnauthorized ? "Bu modüle erişim yetkiniz yok" : "Malzeme verisi yüklenemedi"}</h3>
            <p>{result.message}</p>
          </div>
        </div>
      </main>
    );
  }

  return (
    <MaterialRegistryView
      summary={result.summary}
      materials={result.materials}
      canManage={result.canManage}
    />
  );
}
