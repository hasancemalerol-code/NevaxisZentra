"use client";

import Link from "next/link";
import { useState } from "react";

import { useDigitalWorkforceRuntime } from "../application/digital-workforce-runtime";
import { DIGITAL_WORKERS, getDigitalWorker } from "../data/digital-workforce-demo-catalog";
import { AUTONOMY_LABELS, DIGITAL_WORKFORCE_FULL_NOTE, STAGE_LABELS } from "../domain/digital-workforce-labels";
import type { DigitalWorkerId } from "../domain/digital-workforce-types";
import { DigitalWorkforcePermissionDrawer } from "./digital-workforce-management-table";
import { AutonomyChips, DemoBadge, StatusChip } from "./digital-workforce-primitives";

export function DigitalWorkforceWorkersView() {
  const runtime = useDigitalWorkforceRuntime();
  const [permissionId, setPermissionId] = useState<DigitalWorkerId | null>(null);
  const permissionWorker = permissionId ? getDigitalWorker(permissionId) : undefined;

  return (
    <>
      <header className="dwf-hero">
        <div>
          <p className="dwf-hero__eyebrow">DİJİTAL İŞ GÜCÜ</p>
          <h1>Dijital Çalışanlar</h1>
          <p>7 dijital çalışan. Her birinin çalışma seviyesi, yetkisi ve canlı işi buradan yönetilir.</p>
        </div>
        <div className="dwf-hero__count">
          <strong>7</strong>
          <span>Dijital Çalışan</span>
        </div>
      </header>

      <div className="dwf-card-grid">
        {DIGITAL_WORKERS.map((worker) => {
          const mode = runtime.getWorkerMode(worker.id);
          const status = runtime.getWorkerStatus(worker.id, worker.defaultStatus);
          return (
            <article className="dwf-card" key={worker.id}>
              <div className="dwf-card__top">
                <div>
                  <span className="dwf-meta">{worker.code}</span>
                  <h3>{worker.name}</h3>
                </div>
                <StatusChip status={status} />
              </div>
              <p>{worker.mission}</p>
              <p><strong>Şu An Ne Yapıyor?</strong> {runtime.isPaused(worker.id) ? "Duraklatıldı" : worker.currentAssignment ?? "Boşta"}</p>
              <p className="dwf-meta">
                {worker.currentStage ? `Aşama: ${STAGE_LABELS[worker.currentStage]} · ` : ""}
                Yetki: {AUTONOMY_LABELS[mode]}
              </p>
              <AutonomyChips value={mode} onChange={(next) => runtime.setWorkerMode(worker.id, next)} />
              {mode === "full" ? <p className="dwf-note">{DIGITAL_WORKFORCE_FULL_NOTE}</p> : null}
              <div className="dwf-stats">
                <div><span>Bugün</span><strong>{worker.kpis.completedToday}</strong></div>
                <div><span>Doğrulanan</span><strong>{worker.kpis.verifiedClosed}</strong></div>
                <div><span>Onay</span><strong>{worker.kpis.pendingApprovals}</strong></div>
              </div>
              <DemoBadge />
              <div className="dwf-actions" style={{ marginTop: "0.65rem" }}>
                <Link className="dwf-btn dwf-btn--ghost" href={`/dijital-is-gucu/${worker.id}`}>Detay</Link>
                <button className="dwf-btn dwf-btn--ghost" onClick={() => setPermissionId(worker.id)} type="button">
                  Yetkileri Yönet
                </button>
                <button className="dwf-btn dwf-btn--ghost" onClick={() => runtime.toggleWorkerPause(worker.id)} type="button">
                  {runtime.isPaused(worker.id) ? "Başlat" : "Duraklat"}
                </button>
              </div>
            </article>
          );
        })}
      </div>

      {permissionWorker ? (
        <DigitalWorkforcePermissionDrawer
          onClose={() => setPermissionId(null)}
          worker={permissionWorker}
        />
      ) : null}
    </>
  );
}
