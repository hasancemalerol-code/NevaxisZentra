import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";


import { MaterialRegistryView } from "@/features/materials/components/material-registry-view";
import "@/features/materials/components/material-registry.css";
import { readMaterialRegistry } from "@/features/materials/server/material-reader";

import { FailureStatePage } from "@/components/ui/failure-state";

export default async function MaterialRegistryPage() {
  await forbidUnlessGranted("material.read");

  const result = await readMaterialRegistry();

  if (result.status === "failure") {
    return (
      <div className="material-registry-page">
        <header className="material-registry-header">
          <div className="material-registry-header__intro">
            <p className="material-registry-header__eyebrow">MALZEME YÖNETİMİ</p>
            <h1>Malzemeler</h1>
            <p className="material-registry-header__subtitle">
              Polimer malzeme kayıtlarını, teknik özelliklerini ve yaşam döngüsü durumlarını yönetin.
            </p>
          </div>
        </header>
        <FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Malzeme verisi yüklenemedi" inline />
      </div>
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
