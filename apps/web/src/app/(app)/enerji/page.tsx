import { Zap } from "lucide-react";

import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { EnergyWorkspaceView } from "@/features/energy/components/energy-workspace-view";
import "@/features/energy/components/energy-workspace.css";
import { readEnergyWorkspace } from "@/features/energy/server/energy-queries";

export default async function EnergyPage() {
  await forbidUnlessGranted("energy.read");

  const result = await readEnergyWorkspace();

  if (result.status === "failure") {
    const isUnauthorized =
      result.code === "permission-denied" || result.code === "unauthenticated";

    return (
      <main className="energy-page">
        <header className="energy-executive">
          <div className="energy-executive__intro">
            <p className="energy-executive__eyebrow">ENERJİ YÖNETİMİ</p>
            <h1>Enerji</h1>
            <p className="energy-executive__subtitle">
              Enjeksiyon makineleri ve yardımcı ekipman tüketimini izleyin; hedef, baz ve değerlendirme kanıtlarını tek workspace&apos;te görün.
            </p>
          </div>
        </header>
        <div
          role="alert"
          className={isUnauthorized ? "energy-error-banner" : "energy-partial-error"}
        >
          <Zap aria-hidden="true" />
          <div>
            <h3>{isUnauthorized ? "Bu modüle erişim yetkiniz yok" : "Enerji verisi yüklenemedi"}</h3>
            <p>{result.message}</p>
          </div>
        </div>
      </main>
    );
  }

  return (
    <EnergyWorkspaceView
      workspace={result.workspace}
      canManage={result.canManage}
    />
  );
}
