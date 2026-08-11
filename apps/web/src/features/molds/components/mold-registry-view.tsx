"use client";

import { useMemo, useState, useTransition } from "react";
import { Boxes, LoaderCircle } from "lucide-react";

import type { MoldDetail, MoldLifecycleEvent, MoldMaintenanceRecord, MoldRevisionRecord, MoldSummary, MoldTimelineEntry, MoldTrialRecord, MoldWorkspaceSummary } from "../domain/molds";
import {
  formatMoldNumber,
  getMoldLifecycleStateLabel,
  getMoldLocationLabel,
  getMoldOwnershipLabel,
  MOLD_LIFECYCLE_FILTER_OPTIONS,
  MOLD_LOCATION_FILTER_OPTIONS,
  MOLD_LOCATION_FORM_OPTIONS,
  MOLD_OWNERSHIP_FILTER_OPTIONS,
  MOLD_OWNERSHIP_FORM_OPTIONS,
  MOLD_SOURCE_FORM_OPTIONS,
} from "../presentation/mold-labels";
import { createMoldAction } from "../server/mold-writer";
import { loadMoldDetailAction } from "../server/mold-actions";
import { MoldHistoryDetailPanel } from "./mold-history-detail-panel";
import "./mold-registry.css";

type MoldRegistryViewProps = Readonly<{
  summary: MoldWorkspaceSummary;
  molds: readonly MoldSummary[];
  canManage: boolean;
  initialSelectedId?: string;
  initialDetail?: MoldDetail;
  initialHistory?: readonly MoldLifecycleEvent[];
}>;

export function MoldRegistryView({
  summary,
  molds,
  canManage,
  initialSelectedId,
  initialDetail,
  initialHistory = [],
}: MoldRegistryViewProps) {
  const [search, setSearch] = useState("");
  const [lifecycleFilter, setLifecycleFilter] = useState("");
  const [ownershipFilter, setOwnershipFilter] = useState("");
  const [locationFilter, setLocationFilter] = useState("");
  const [selectedId, setSelectedId] = useState(initialSelectedId ?? molds[0]?.id ?? "");
  const [detail, setDetail] = useState<MoldDetail | undefined>(initialDetail);
  const [history, setHistory] = useState<readonly MoldLifecycleEvent[]>(initialHistory);
  const [maintenanceHistory, setMaintenanceHistory] = useState<readonly MoldMaintenanceRecord[]>([]);
  const [revisionHistory, setRevisionHistory] = useState<readonly MoldRevisionRecord[]>([]);
  const [trialHistory, setTrialHistory] = useState<readonly MoldTrialRecord[]>([]);
  const [timeline, setTimeline] = useState<readonly MoldTimelineEntry[]>([]);
  const [detailCanManage, setDetailCanManage] = useState(canManage);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [showCreate, setShowCreate] = useState(false);
  const [isPending, startTransition] = useTransition();

  const filteredMolds = useMemo(() => {
    return molds.filter((mold) => {
      const matchesSearch =
        !search.trim() ||
        mold.code.toLowerCase().includes(search.toLowerCase()) ||
        mold.name.toLowerCase().includes(search.toLowerCase());
      const matchesLifecycle = !lifecycleFilter || mold.lifecycleState === lifecycleFilter;
      const matchesOwnership = !ownershipFilter || mold.ownershipType === ownershipFilter;
      const matchesLocation = !locationFilter || mold.locationType === locationFilter;
      return matchesSearch && matchesLifecycle && matchesOwnership && matchesLocation;
    });
  }, [lifecycleFilter, locationFilter, molds, ownershipFilter, search]);

  const selectedSummary = filteredMolds.find((mold) => mold.id === selectedId) ?? molds.find((m) => m.id === selectedId);
  const activeDetail = detail && detail.id === selectedId ? detail : undefined;

  function selectMold(mold: MoldSummary) {
    setSelectedId(mold.id);
    setDetail(undefined);
    setHistory([]);
    setMaintenanceHistory([]);
    setRevisionHistory([]);
    setTrialHistory([]);
    setTimeline([]);
    setMessage(null);
    setError(null);

    startTransition(async () => {
      const result = await loadMoldDetailAction(mold.id);
      if (result.status === "success") {
        setDetail(result.mold);
        setHistory(result.lifecycleHistory);
        setMaintenanceHistory(result.maintenanceHistory);
        setRevisionHistory(result.revisionHistory);
        setTrialHistory(result.trialHistory);
        setTimeline(result.timeline);
        setDetailCanManage(result.canManage);
      } else {
        setError(result.message);
      }
    });
  }

  function runAction(action: () => Promise<{ status: string; message?: string }>) {
    startTransition(async () => {
      setMessage(null);
      setError(null);
      const result = await action();
      if (result.status === "failure") {
        setError(result.message ?? "İşlem başarısız.");
        return;
      }

      setMessage("Kayıt güncellendi.");
      setShowCreate(false);

      if (selectedId) {
        const refreshed = await loadMoldDetailAction(selectedId);
        if (refreshed.status === "success") {
          setDetail(refreshed.mold);
          setHistory(refreshed.lifecycleHistory);
          setMaintenanceHistory(refreshed.maintenanceHistory);
          setRevisionHistory(refreshed.revisionHistory);
          setTrialHistory(refreshed.trialHistory);
          setTimeline(refreshed.timeline);
          setDetailCanManage(refreshed.canManage);
        }
      }
    });
  }

  return (
    <main className="mold-registry-page">
      <header className="mold-registry-header">
        <div>
          <p>KALIP YÖNETİMİ</p>
          <h1>Kalıplar</h1>
        </div>
        {canManage ? (
          <button
            type="button"
            className="mold-registry-button primary"
            onClick={() => setShowCreate((current) => !current)}
          >
            Kalıp Oluştur
          </button>
        ) : null}
      </header>

      <section className="mold-registry-kpis" aria-label="Kalıp özet sayıları">
        <article className="mold-registry-kpi"><strong>{summary.totalCount}</strong><span>Toplam</span></article>
        <article className="mold-registry-kpi"><strong>{summary.activeCount}</strong><span>Aktif</span></article>
        <article className="mold-registry-kpi"><strong>{summary.maintenanceCount}</strong><span>Bakımda</span></article>
        <article className="mold-registry-kpi"><strong>{summary.draftCount}</strong><span>Taslak</span></article>
      </section>

      <section className="mold-registry-toolbar" aria-label="Kalıp arama ve filtreler">
        <label>
          Ara
          <input value={search} onChange={(event) => setSearch(event.target.value)} placeholder="Kalıp kodu veya adı" />
        </label>
        <label>
          Yaşam döngüsü
          <select value={lifecycleFilter} onChange={(event) => setLifecycleFilter(event.target.value)}>
            {MOLD_LIFECYCLE_FILTER_OPTIONS.map((option) => (
              <option key={option.value || "all"} value={option.value}>{option.label}</option>
            ))}
          </select>
        </label>
        <label>
          Sahiplik
          <select value={ownershipFilter} onChange={(event) => setOwnershipFilter(event.target.value)}>
            {MOLD_OWNERSHIP_FILTER_OPTIONS.map((option) => (
              <option key={option.value || "all"} value={option.value}>{option.label}</option>
            ))}
          </select>
        </label>
        <label>
          Konum
          <select value={locationFilter} onChange={(event) => setLocationFilter(event.target.value)}>
            {MOLD_LOCATION_FILTER_OPTIONS.map((option) => (
              <option key={option.value || "all"} value={option.value}>{option.label}</option>
            ))}
          </select>
        </label>
      </section>

      {error ? <div className="mold-registry-error" role="alert">{error}</div> : null}
      {message ? <div className="mold-registry-empty" role="status">{message}</div> : null}
      {isPending ? (
        <div className="mold-registry-empty" role="status">
          <LoaderCircle aria-hidden="true" /> İşleniyor…
        </div>
      ) : null}

      {showCreate && canManage ? (
        <section className="mold-registry-panel" aria-label="Yeni kalıp formu">
          <h2>Yeni Kalıp</h2>
          <form
            className="mold-registry-form-grid"
            action={(formData) => runAction(() => createMoldAction(formData))}
          >
            <label>Kalıp Kodu<input name="code" required /></label>
            <label>Kalıp Adı<input name="name" required /></label>
            <label>Sahiplik
              <select name="ownershipType" defaultValue="company-owned">
                {MOLD_OWNERSHIP_FORM_OPTIONS.map((option) => (
                  <option key={option.value} value={option.value}>{option.label}</option>
                ))}
              </select>
            </label>
            <label>Sahip Referansı<input name="ownerReference" /></label>
            <label>Konum
              <select name="locationType" defaultValue="warehouse">
                {MOLD_LOCATION_FORM_OPTIONS.map((option) => (
                  <option key={option.value} value={option.value}>{option.label}</option>
                ))}
              </select>
            </label>
            <label>Konum Etiketi<input name="locationLabel" /></label>
            <label>Kaynak
              <select name="sourceType" defaultValue="manual">
                {MOLD_SOURCE_FORM_OPTIONS.map((option) => (
                  <option key={option.value} value={option.value}>{option.label}</option>
                ))}
              </select>
            </label>
            <label>Gözenek Sayısı<input name="cavityCount" type="number" min="1" /></label>
            <label className="full-width">Açıklama<textarea name="description" rows={2} /></label>
            <input type="hidden" name="idempotencyKey" value={crypto.randomUUID()} />
            <div className="mold-registry-actions full-width">
              <button type="submit" className="mold-registry-button primary">Kaydet</button>
            </div>
          </form>
        </section>
      ) : null}

      <div className="mold-registry-layout">
        <section className="mold-registry-table-wrap" aria-label="Kalıp listesi">
          {filteredMolds.length === 0 ? (
            <div className="mold-registry-empty">
              <Boxes aria-hidden="true" />
              <p>Kayıtlı kalıp bulunamadı.</p>
            </div>
          ) : (
            <table className="mold-registry-table">
              <thead>
                <tr>
                  <th>Kod</th>
                  <th>Ad</th>
                  <th>Durum</th>
                  <th>Sahiplik</th>
                  <th>Konum</th>
                  <th>Vuruş</th>
                </tr>
              </thead>
              <tbody>
                {filteredMolds.map((mold) => (
                  <tr
                    key={mold.id}
                    data-selected={mold.id === selectedId}
                    onClick={() => selectMold(mold)}
                  >
                    <td>{mold.code}</td>
                    <td>{mold.name}</td>
                    <td><span className="mold-registry-badge">{getMoldLifecycleStateLabel(mold.lifecycleState)}</span></td>
                    <td>{getMoldOwnershipLabel(mold.ownershipType)}</td>
                    <td>{getMoldLocationLabel(mold.locationType)}{mold.locationLabel ? ` · ${mold.locationLabel}` : ""}</td>
                    <td>{formatMoldNumber(mold.currentShotCount)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </section>

        <aside className="mold-registry-panel" aria-label="Kalıp detayı">
          {!selectedSummary ? (
            <div className="mold-registry-empty">Detay görmek için bir kalıp seçin.</div>
          ) : (
            <MoldHistoryDetailPanel
              selectedSummary={selectedSummary}
              detail={activeDetail}
              lifecycleHistory={history}
              maintenanceHistory={maintenanceHistory}
              revisionHistory={revisionHistory}
              trialHistory={trialHistory}
              timeline={timeline}
              canManage={detailCanManage}
              isPending={isPending}
              onAction={runAction}
            />
          )}
        </aside>
      </div>
    </main>
  );
}
