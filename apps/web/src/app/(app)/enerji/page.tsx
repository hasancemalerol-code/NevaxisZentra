

import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { EnergyWorkspaceView } from "@/features/energy/components/energy-workspace-view";
import "@/features/energy/components/energy-workspace.css";
import { readEnergyWorkspace } from "@/features/energy/server/energy-queries";

import { FailureStatePage } from "@/components/ui/failure-state";
import { StatePage, StatePageLink } from "@/components/ui/state-page";

export default async function EnergyPage() {
  await forbidUnlessGranted("energy.read");

  const result = await readEnergyWorkspace();

  if (result.status === "failure") {
    return (
      <div className="energy-page">
        <header className="energy-executive">
          <div className="energy-executive__intro">
            <p className="energy-executive__eyebrow">ENERJİ YÖNETİMİ</p>
            <h1>Enerji</h1>
            <p className="energy-executive__subtitle">
              Enjeksiyon makineleri ve yardımcı ekipman tüketimini izleyin; hedef, baz ve değerlendirme kanıtlarını tek ekranda görün.
            </p>
          </div>
        </header>
        {"code" in result && result.code === "not-found" ? (
          <StatePage
            kind="empty"
            inline
            title="Henüz enerji verisi yok"
            description="Ölçüm noktaları ve tüketim verisi bağlandığında hedef, baz ve değerlendirme kanıtları burada izlenir."
            actions={<StatePageLink href="/ayarlar/veri-aktarim-merkezi" primary>Veri Aktarım Merkezi</StatePageLink>}
          />
        ) : (
          <FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Enerji verisi yüklenemedi" inline />
        )}
      </div>
    );
  }

  return (
    <EnergyWorkspaceView
      workspace={result.workspace}
      canManage={result.canManage}
    />
  );
}
