import Link from "next/link";

import {
  DIGITAL_WORKFORCE_KPIS,
  DIGITAL_WORKFORCE_LIVE_STRIP,
} from "@/features/digital-workforce/data/digital-workforce-demo-catalog";
import { DIGITAL_WORKFORCE_SUBTITLE } from "@/features/digital-workforce/domain/digital-workforce-labels";
import { resolveWorkerUiId } from "@/features/digital-workforce/domain/digital-workforce-api-ids";
import { readDigitalWorkforceControlCenter } from "@/features/digital-workforce/server/digital-workforce-reader";
import "@/features/digital-workforce/components/digital-workforce-workspace.css";

export async function DigitalWorkforceDashboardPanel() {
  const hub = await readDigitalWorkforceControlCenter();
  const snapshot = hub.status === "success" ? hub.snapshot : null;
  const useDemo = !snapshot;

  const activeWorkers = snapshot
    ? snapshot.workers.filter((worker) => worker.activeCaseCount > 0 && !worker.isPaused && !worker.isKilled).length
    : DIGITAL_WORKFORCE_KPIS.activeWorkers;
  const pendingApprovals = snapshot?.pendingApprovals ?? DIGITAL_WORKFORCE_KPIS.pendingApprovals;
  const observing = snapshot
    ? snapshot.workers.filter((worker) => worker.autonomyMode === "observe").length
    : 1;
  const completedToday = snapshot
    ? snapshot.workers.reduce((sum, worker) => sum + worker.completedToday, 0)
    : DIGITAL_WORKFORCE_KPIS.completedToday;
  const verifiedClosures = snapshot
    ? snapshot.workers.reduce((sum, worker) => sum + worker.verifiedClosures, 0)
    : DIGITAL_WORKFORCE_KPIS.verifiedClosures;
  const activeCases = snapshot?.openCases ?? DIGITAL_WORKFORCE_KPIS.activeCases;
  const liveStrip = snapshot
    ? snapshot.workers.map((worker) => ({
        id: resolveWorkerUiId(worker.id) ?? worker.code.toLowerCase(),
        name: worker.name.replace(" Dijital Çalışanı", ""),
        statusLabel: worker.isPaused || worker.isKilled
          ? "Duraklatıldı"
          : worker.activeCaseCount > 0
            ? "Çalışıyor"
            : worker.autonomyMode === "observe"
              ? "İzliyor"
              : "Hazır",
      }))
    : DIGITAL_WORKFORCE_LIVE_STRIP;

  return (
    <section className="dashboard-panel dashboard-panel--digital-workforce dwf-dashboard-card">
      <div className="dwf-dashboard-card__copy">
        <p className="dwf-hero__eyebrow">DİJİTAL İŞ GÜCÜ</p>
        <h2>Dijital İş Gücü</h2>
        <p>{DIGITAL_WORKFORCE_SUBTITLE}</p>
        {useDemo ? <p className="dwf-meta">Demo / tahmini — API bağlantısı yok</p> : null}
        <p className="dwf-dashboard-card__count">
          <strong>{snapshot?.workers.length ?? 7}</strong> Dijital Çalışan
        </p>
        <div className="dwf-dashboard__stats">
          <div className="dwf-dashboard__stat">
            <strong>{activeWorkers} Aktif</strong>
            <span>Çalışıyor / uygulama</span>
          </div>
          <div className="dwf-dashboard__stat">
            <strong>{pendingApprovals} Onay Bekliyor</strong>
            <span>İnsan kararı gerekli</span>
          </div>
          <div className="dwf-dashboard__stat">
            <strong>{observing} İzliyor</strong>
            <span>Gözlem modu</span>
          </div>
        </div>
        <p className="dwf-dashboard-card__today">
          Bugün: <strong>{completedToday} iş tamamlandı</strong>
          {" · "}
          <strong>{verifiedClosures} doğrulandı</strong>
        </p>
        <p className="dwf-meta">
          Current activity: {activeCases} aktif vaka
          {useDemo ? " · Demo / tahmini" : " · Canlı"}
        </p>
        <div className="dwf-dashboard-card__actions">
          <Link className="dwf-dashboard__cta" href="/dijital-is-gucu">
            Dijital İş Gücünü Yönet
          </Link>
          <Link className="dwf-btn dwf-btn--ghost" href="/dijital-is-gucu">
            Dijital İş Gücü Kontrol Merkezi&apos;ni Aç
          </Link>
        </div>
      </div>
      <div className="dwf-dashboard__live">
        <strong>Şu an ne yapıyorlar?</strong>
        <span>{activeCases} aktif vaka</span>
        <div className="dwf-dashboard__strip" aria-label="Canlı dijital çalışan şeridi">
          {liveStrip.map((item) => (
            <Link className="dwf-dashboard__chip" href={`/dijital-is-gucu/${item.id}`} key={item.id}>
              <span>{item.name}</span>
              <strong>{item.statusLabel}</strong>
            </Link>
          ))}
        </div>
      </div>
    </section>
  );
}
