"use client";

import { useMemo, useState } from "react";

import type {
  MoldCompatibilityOverview,
  MoldDetail,
  MoldLifecycleEvent,
  MoldMaintenanceRecord,
  MoldRevisionRecord,
  MoldSummary,
  MoldTimelineEntry,
  MoldTrialRecord,
} from "../domain/molds";
import {
  formatMoldDateTime,
  formatMoldNumber,
  getMoldCounterSourceLabel,
  getMoldLifecycleStateLabel,
  getMoldLocationLabel,
  getMoldOwnershipLabel,
  getMoldSourceLabel,
  MOLD_LOCATION_FORM_OPTIONS,
  MOLD_OWNERSHIP_FORM_OPTIONS,
  MOLD_TRANSITION_TARGETS,
} from "../presentation/mold-labels";
import {
  getMoldMaintenanceStatusLabel,
  getMoldMaintenanceTypeLabel,
  getMoldRevisionTypeLabel,
  getMoldTimelineKindLabel,
  getMoldTrialResultLabel,
  MOLD_MAINTENANCE_STATUS_OPTIONS,
  MOLD_MAINTENANCE_TYPE_OPTIONS,
  MOLD_REVISION_TYPE_OPTIONS,
  MOLD_TRIAL_RESULT_OPTIONS,
} from "../presentation/mold-history-labels";
import {
  appendMoldRevisionAction,
  appendMoldTrialAction,
  completeMoldMaintenanceAction,
  createMoldMaintenanceAction,
} from "../server/mold-history-writer";
import { transitionMoldLifecycleAction, updateMoldAction } from "../server/mold-writer";
import { MoldCompatibilityDetailPanel } from "./mold-compatibility-detail-panel";

type DetailTab = "overview" | "maintenance" | "revisions" | "trials" | "timeline" | "compatibility";

type MoldHistoryDetailPanelProps = Readonly<{
  selectedSummary: MoldSummary;
  detail?: MoldDetail;
  lifecycleHistory: readonly MoldLifecycleEvent[];
  maintenanceHistory: readonly MoldMaintenanceRecord[];
  revisionHistory: readonly MoldRevisionRecord[];
  trialHistory: readonly MoldTrialRecord[];
  timeline: readonly MoldTimelineEntry[];
  compatibilityOverview?: MoldCompatibilityOverview;
  compatibilityError?: string | null;
  canManage: boolean;
  isPending: boolean;
  onAction: (action: () => Promise<{ status: string; message?: string; compatibilityOverview?: MoldCompatibilityOverview }>) => void;
}>;

export function MoldHistoryDetailPanel({
  selectedSummary,
  detail,
  lifecycleHistory,
  maintenanceHistory,
  revisionHistory,
  trialHistory,
  timeline,
  compatibilityOverview,
  compatibilityError,
  canManage,
  isPending,
  onAction,
}: MoldHistoryDetailPanelProps) {
  const [tab, setTab] = useState<DetailTab>("overview");

  const historyKpis = useMemo(
    () => ({
      maintenanceCount: maintenanceHistory.length,
      openMaintenanceCount: maintenanceHistory.filter(
        (item) => item.status === "planned" || item.status === "in-progress",
      ).length,
      revisionCount: revisionHistory.length,
      trialCount: trialHistory.length,
      successfulTrialCount: trialHistory.filter(
        (item) => item.result === "successful" || item.result === "conditionally-approved",
      ).length,
    }),
    [maintenanceHistory, revisionHistory, trialHistory],
  );

  const transitionOptions = detail ? MOLD_TRANSITION_TARGETS[detail.lifecycleState] ?? [] : [];
  const openMaintenance = maintenanceHistory.find(
    (item) => item.status === "planned" || item.status === "in-progress",
  );

  return (
    <>
      <h2>{selectedSummary.code} · {selectedSummary.name}</h2>

      <section className="mold-registry-kpis" aria-label="Kalıp geçmişi özeti">
        <article className="mold-registry-kpi"><strong>{historyKpis.maintenanceCount}</strong><span>Bakım</span></article>
        <article className="mold-registry-kpi"><strong>{historyKpis.openMaintenanceCount}</strong><span>Açık Bakım</span></article>
        <article className="mold-registry-kpi"><strong>{historyKpis.revisionCount}</strong><span>Revizyon</span></article>
        <article className="mold-registry-kpi"><strong>{historyKpis.trialCount}</strong><span>Deneme</span></article>
      </section>

      <nav className="mold-registry-toolbar" aria-label="Kalıp detay sekmeleri">
        {([
          ["overview", "Genel Bakış"],
          ["maintenance", "Bakım Geçmişi"],
          ["revisions", "Revizyonlar"],
          ["trials", "Denemeler"],
          ["timeline", "Zaman Çizelgesi"],
          ["compatibility", "Makine Uyumluluğu"],
        ] as const).map(([value, label]) => (
          <button
            key={value}
            type="button"
            className={`mold-registry-button${tab === value ? " primary" : ""}`}
            onClick={() => setTab(value)}
          >
            {label}
          </button>
        ))}
      </nav>

      {!detail && isPending ? (
        <div className="mold-registry-empty" role="status">Detay yükleniyor…</div>
      ) : null}

      {!detail && !isPending ? (
        <div className="mold-registry-empty">Detay yüklenemedi veya erişim yok.</div>
      ) : null}

      {detail && tab === "overview" ? (
        <>
          <dl className="mold-registry-meta-grid">
            <div><dt>Durum</dt><dd>{getMoldLifecycleStateLabel(selectedSummary.lifecycleState)}</dd></div>
            <div><dt>Sahiplik</dt><dd>{getMoldOwnershipLabel(selectedSummary.ownershipType)}</dd></div>
            <div><dt>Konum</dt><dd>{getMoldLocationLabel(selectedSummary.locationType)}</dd></div>
            <div><dt>Gözenek</dt><dd>{formatMoldNumber(selectedSummary.cavityCount)}</dd></div>
            <div><dt>Vuruş Sayısı</dt><dd>{formatMoldNumber(selectedSummary.currentShotCount)}</dd></div>
            <div><dt>Kaynak</dt><dd>{getMoldSourceLabel(selectedSummary.sourceType)}</dd></div>
            <div><dt>Son Güncelleme</dt><dd>{formatMoldDateTime(selectedSummary.updatedAtUtc)}</dd></div>
            <div><dt>Sayaç Kaynağı</dt><dd>{getMoldCounterSourceLabel(detail.counterSource)}</dd></div>
          </dl>

          {canManage ? (
            <>
              <form className="mold-registry-form-grid" action={(formData) => onAction(() => updateMoldAction(formData))}>
                <input type="hidden" name="moldId" value={detail.id} />
                <input type="hidden" name="expectedVersion" value={detail.version} />
                <label className="full-width">Ad<input name="name" defaultValue={detail.name} required /></label>
                <label>Sahiplik
                  <select name="ownershipType" defaultValue={detail.ownershipType}>
                    {MOLD_OWNERSHIP_FORM_OPTIONS.map((option) => (
                      <option key={option.value} value={option.value}>{option.label}</option>
                    ))}
                  </select>
                </label>
                <label>Konum
                  <select name="locationType" defaultValue={detail.locationType}>
                    {MOLD_LOCATION_FORM_OPTIONS.map((option) => (
                      <option key={option.value} value={option.value}>{option.label}</option>
                    ))}
                  </select>
                </label>
                <div className="mold-registry-actions full-width">
                  <button type="submit" className="mold-registry-button primary">Kayıt Güncelle</button>
                </div>
              </form>

              {transitionOptions.length > 0 ? (
                <form className="mold-registry-form-grid" action={(formData) => onAction(() => transitionMoldLifecycleAction(formData))}>
                  <input type="hidden" name="moldId" value={detail.id} />
                  <input type="hidden" name="expectedVersion" value={detail.version} />
                  <input type="hidden" name="idempotencyKey" value={crypto.randomUUID()} />
                  <label>Hedef Durum
                    <select name="targetState" required defaultValue={transitionOptions[0]?.value}>
                      {transitionOptions.map((option) => (
                        <option key={option.value} value={option.value}>{option.label}</option>
                      ))}
                    </select>
                  </label>
                  <label className="full-width">Geçiş Gerekçesi<input name="reason" required /></label>
                  <div className="mold-registry-actions full-width">
                    <button type="submit" className="mold-registry-button">Durum Geçişi</button>
                  </div>
                </form>
              ) : null}
            </>
          ) : (
            <div className="mold-registry-empty">Bu kalıp için yalnızca görüntüleme yetkiniz var.</div>
          )}

          <section aria-label="Yaşam döngüsü geçmişi">
            <h3>Son Yaşam Döngüsü Olayları</h3>
            <div className="mold-registry-history">
              {lifecycleHistory.length === 0 ? (
                <div className="mold-registry-empty">Henüz geçiş kaydı yok.</div>
              ) : (
                lifecycleHistory.slice(0, 5).map((event) => (
                  <article key={event.id} className="mold-registry-history-item">
                    <strong>{getMoldLifecycleStateLabel(event.previousState)} → {getMoldLifecycleStateLabel(event.newState)}</strong>
                    <div>{event.reason}</div>
                    <small>{event.actorDisplayName} · {formatMoldDateTime(event.occurredAtUtc)}</small>
                  </article>
                ))
              )}
            </div>
          </section>
        </>
      ) : null}

      {detail && tab === "maintenance" ? (
        <>
          {canManage ? (
            <form className="mold-registry-form-grid" action={(formData) => onAction(() => createMoldMaintenanceAction(formData))}>
              <input type="hidden" name="moldId" value={detail.id} />
              <input type="hidden" name="idempotencyKey" value={crypto.randomUUID()} />
              <label>Bakım Türü
                <select name="maintenanceType" defaultValue="preventive">
                  {MOLD_MAINTENANCE_TYPE_OPTIONS.map((option) => (
                    <option key={option.value} value={option.value}>{option.label}</option>
                  ))}
                </select>
              </label>
              <label>Durum
                <select name="status" defaultValue="planned">
                  {MOLD_MAINTENANCE_STATUS_OPTIONS.map((option) => (
                    <option key={option.value} value={option.value}>{option.label}</option>
                  ))}
                </select>
              </label>
              <label className="full-width">Özet<input name="summary" required placeholder="Örn. Planlı yağlama ve temizlik" /></label>
              <label className="full-width">Bulgular<textarea name="findings" rows={2} /></label>
              <label>Uygulayan<input name="performedByDisplayName" placeholder="Örn. Bakım Ekibi" /></label>
              <label>Dış Bakım Talebi Ref.<input name="externalMaintenanceRequestReference" placeholder="İsteğe bağlı referans" /></label>
              <div className="mold-registry-actions full-width">
                <button type="submit" className="mold-registry-button primary">Bakım Kaydı Aç</button>
              </div>
            </form>
          ) : null}

          {canManage && openMaintenance ? (
            <form className="mold-registry-form-grid" action={(formData) => onAction(() => completeMoldMaintenanceAction(formData))}>
              <input type="hidden" name="moldId" value={detail.id} />
              <input type="hidden" name="maintenanceId" value={openMaintenance.id} />
              <input type="hidden" name="expectedVersion" value={openMaintenance.version} />
              <label className="full-width">Tamamlama Notu<textarea name="actionsTaken" rows={2} defaultValue="Bakım tamamlandı." /></label>
              <div className="mold-registry-actions full-width">
                <button type="submit" className="mold-registry-button primary">Açık Bakımı Tamamla</button>
              </div>
            </form>
          ) : null}

          <table className="mold-registry-table">
            <thead>
              <tr>
                <th>Tür</th>
                <th>Durum</th>
                <th>Özet</th>
                <th>Açılış</th>
              </tr>
            </thead>
            <tbody>
              {maintenanceHistory.length === 0 ? (
                <tr><td colSpan={4}>Bakım kaydı yok.</td></tr>
              ) : maintenanceHistory.map((item) => (
                <tr key={item.id}>
                  <td><span className="mold-registry-badge">{getMoldMaintenanceTypeLabel(item.maintenanceType)}</span></td>
                  <td><span className="mold-registry-badge">{getMoldMaintenanceStatusLabel(item.status)}</span></td>
                  <td>{item.summary}</td>
                  <td>{formatMoldDateTime(item.openedAtUtc)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </>
      ) : null}

      {detail && tab === "revisions" ? (
        <>
          {canManage ? (
            <form className="mold-registry-form-grid" action={(formData) => onAction(() => appendMoldRevisionAction(formData))}>
              <input type="hidden" name="moldId" value={detail.id} />
              <input type="hidden" name="idempotencyKey" value={crypto.randomUUID()} />
              <label>Revizyon No<input name="revisionNumber" required placeholder="R-01" /></label>
              <label>Tür
                <select name="revisionType" defaultValue="cavity-change">
                  {MOLD_REVISION_TYPE_OPTIONS.map((option) => (
                    <option key={option.value} value={option.value}>{option.label}</option>
                  ))}
                </select>
              </label>
              <label>Tarih<input name="revisionDate" type="date" defaultValue={new Date().toISOString().slice(0, 10)} /></label>
              <label className="full-width">Özet<input name="summary" required placeholder="Örn. Gözenek sayısı güncellendi" /></label>
              <label>Önceki Gözenek<input name="previousCavityCount" type="number" min="1" /></label>
              <label>Yeni Gözenek<input name="newCavityCount" type="number" min="1" /></label>
              <div className="mold-registry-actions full-width">
                <button type="submit" className="mold-registry-button primary">Revizyon Ekle</button>
              </div>
            </form>
          ) : null}

          <table className="mold-registry-table">
            <thead>
              <tr><th>No</th><th>Tür</th><th>Özet</th><th>Tarih</th></tr>
            </thead>
            <tbody>
              {revisionHistory.length === 0 ? (
                <tr><td colSpan={4}>Revizyon kaydı yok.</td></tr>
              ) : revisionHistory.map((item) => (
                <tr key={item.id}>
                  <td>{item.revisionNumber}</td>
                  <td><span className="mold-registry-badge">{getMoldRevisionTypeLabel(item.revisionType)}</span></td>
                  <td>{item.summary}</td>
                  <td>{item.revisionDate}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </>
      ) : null}

      {detail && tab === "trials" ? (
        <>
          {canManage ? (
            <form className="mold-registry-form-grid" action={(formData) => onAction(() => appendMoldTrialAction(formData))}>
              <input type="hidden" name="moldId" value={detail.id} />
              <input type="hidden" name="idempotencyKey" value={crypto.randomUUID()} />
              <label>Deneme No<input name="trialNumber" required placeholder="T-01" /></label>
              <label>Sonuç
                <select name="result" defaultValue="successful">
                  {MOLD_TRIAL_RESULT_OPTIONS.map((option) => (
                    <option key={option.value} value={option.value}>{option.label}</option>
                  ))}
                </select>
              </label>
              <label>Tarih<input name="trialDate" type="date" defaultValue={new Date().toISOString().slice(0, 10)} /></label>
              <label>Makine Ref.<input name="machineReference" placeholder="IM-120" /></label>
              <label>Vuruş<input name="shotCount" type="number" min="0" /></label>
              <label>İyi Parça<input name="goodPartCount" type="number" min="0" /></label>
              <label>Hurda<input name="scrapPartCount" type="number" min="0" /></label>
              <label className="full-width">Bulgular<textarea name="findings" rows={2} placeholder="Deneme sonucu notları" /></label>
              <div className="mold-registry-actions full-width">
                <button type="submit" className="mold-registry-button primary">Deneme Ekle</button>
              </div>
            </form>
          ) : null}

          <table className="mold-registry-table">
            <thead>
              <tr><th>No</th><th>Sonuç</th><th>Makine</th><th>Tarih</th></tr>
            </thead>
            <tbody>
              {trialHistory.length === 0 ? (
                <tr><td colSpan={4}>Deneme kaydı yok.</td></tr>
              ) : trialHistory.map((item) => (
                <tr key={item.id}>
                  <td>{item.trialNumber}</td>
                  <td><span className="mold-registry-badge">{getMoldTrialResultLabel(item.result)}</span></td>
                  <td>{item.machineReference ?? "—"}</td>
                  <td>{item.trialDate}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </>
      ) : null}

      {detail && tab === "timeline" ? (
        <div className="mold-registry-history">
          {timeline.length === 0 ? (
            <div className="mold-registry-empty">Zaman çizelgesi boş.</div>
          ) : timeline.map((entry) => (
            <article key={`${entry.eventKind}-${entry.id}`} className="mold-registry-history-item">
              <strong>{entry.title}</strong>
              <div>{entry.summary}</div>
              <small>
                {getMoldTimelineKindLabel(entry.eventKind)}
                {entry.statusOrResult ? ` · ${entry.statusOrResult.startsWith("draft") || entry.statusOrResult.includes("-")
                  ? getMoldLifecycleStateLabel(entry.statusOrResult)
                  : getMoldMaintenanceStatusLabel(entry.statusOrResult) || getMoldTrialResultLabel(entry.statusOrResult) || entry.statusOrResult}` : ""}
                {entry.reference ? ` · ${entry.reference}` : ""}
                {entry.actorDisplayName ? ` · ${entry.actorDisplayName}` : ""}
                {" · "}{formatMoldDateTime(entry.eventDateUtc)}
              </small>
            </article>
          ))}
        </div>
      ) : null}

      {detail && tab === "compatibility" ? (
        <MoldCompatibilityDetailPanel
          key={detail.id}
          moldId={detail.id}
          overview={compatibilityOverview}
          canManage={canManage}
          isPending={isPending}
          loadError={compatibilityError}
          onAction={onAction}
        />
      ) : null}
    </>
  );
}
