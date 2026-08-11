"use client";

import { useMemo, useState, useTransition } from "react";
import { Boxes, LoaderCircle } from "lucide-react";

import type { MoldDetail, MoldLifecycleEvent, MoldSummary, MoldWorkspaceSummary } from "../domain/molds";
import {
  formatMoldDateTime,
  formatMoldNumber,
  getMoldCounterSourceLabel,
  getMoldLifecycleStateLabel,
  getMoldLocationLabel,
  getMoldOwnershipLabel,
  getMoldSourceLabel,
  MOLD_LIFECYCLE_FILTER_OPTIONS,
  MOLD_LOCATION_FILTER_OPTIONS,
  MOLD_LOCATION_FORM_OPTIONS,
  MOLD_OWNERSHIP_FILTER_OPTIONS,
  MOLD_OWNERSHIP_FORM_OPTIONS,
  MOLD_SOURCE_FORM_OPTIONS,
  MOLD_TRANSITION_TARGETS,
} from "../presentation/mold-labels";
import {
  createMoldAction,
  recordMoldCounterAction,
  transitionMoldLifecycleAction,
  updateMoldAction,
} from "../server/mold-writer";
import { loadMoldDetailAction } from "../server/mold-actions";
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
  const transitionOptions = activeDetail ? MOLD_TRANSITION_TARGETS[activeDetail.lifecycleState] ?? [] : [];

  function selectMold(mold: MoldSummary) {
    setSelectedId(mold.id);
    setDetail(undefined);
    setHistory([]);
    setMessage(null);
    setError(null);

    startTransition(async () => {
      const result = await loadMoldDetailAction(mold.id);
      if (result.status === "success") {
        setDetail(result.mold);
        setHistory(result.lifecycleHistory);
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
            <>
              <h2>{selectedSummary.code} · {selectedSummary.name}</h2>
              <dl className="mold-registry-meta-grid">
                <div><dt>Durum</dt><dd>{getMoldLifecycleStateLabel(selectedSummary.lifecycleState)}</dd></div>
                <div><dt>Sahiplik</dt><dd>{getMoldOwnershipLabel(selectedSummary.ownershipType)}</dd></div>
                <div><dt>Konum</dt><dd>{getMoldLocationLabel(selectedSummary.locationType)}</dd></div>
                <div><dt>Gözenek</dt><dd>{formatMoldNumber(selectedSummary.cavityCount)}</dd></div>
                <div><dt>Vuruş Sayısı</dt><dd>{formatMoldNumber(selectedSummary.currentShotCount)}</dd></div>
                <div><dt>Sayaç Zamanı</dt><dd>{formatMoldDateTime(selectedSummary.counterRecordedAtUtc)}</dd></div>
                <div><dt>Kaynak</dt><dd>{getMoldSourceLabel(selectedSummary.sourceType)}</dd></div>
                <div><dt>Son Güncelleme</dt><dd>{formatMoldDateTime(selectedSummary.updatedAtUtc)}</dd></div>
              </dl>

              {activeDetail && canManage ? (
                <>
                  <form
                    className="mold-registry-form-grid"
                    action={(formData) => runAction(() => updateMoldAction(formData))}
                  >
                    <input type="hidden" name="moldId" value={activeDetail.id} />
                    <input type="hidden" name="expectedVersion" value={activeDetail.version} />
                    <label className="full-width">Ad<input name="name" defaultValue={activeDetail.name} required /></label>
                    <label>Sahiplik
                      <select name="ownershipType" defaultValue={activeDetail.ownershipType}>
                        {MOLD_OWNERSHIP_FORM_OPTIONS.map((option) => (
                          <option key={option.value} value={option.value}>{option.label}</option>
                        ))}
                      </select>
                    </label>
                    <label>Konum
                      <select name="locationType" defaultValue={activeDetail.locationType}>
                        {MOLD_LOCATION_FORM_OPTIONS.map((option) => (
                          <option key={option.value} value={option.value}>{option.label}</option>
                        ))}
                      </select>
                    </label>
                    <label className="full-width">Notlar<textarea name="notes" rows={2} defaultValue={activeDetail.notes ?? ""} /></label>
                    <div className="mold-registry-actions full-width">
                      <button type="submit" className="mold-registry-button primary">Kayıt Güncelle</button>
                    </div>
                  </form>

                  <form
                    className="mold-registry-form-grid"
                    action={(formData) => runAction(() => recordMoldCounterAction(formData))}
                  >
                    <input type="hidden" name="moldId" value={activeDetail.id} />
                    <input type="hidden" name="expectedVersion" value={activeDetail.version} />
                    <input type="hidden" name="idempotencyKey" value={crypto.randomUUID()} />
                    <label>Vuruş Sayısı<input name="shotCount" type="number" min="0" defaultValue={activeDetail.currentShotCount ?? 0} required /></label>
                    <label>Not<input name="note" /></label>
                    <div className="mold-registry-actions full-width">
                      <button type="submit" className="mold-registry-button">Sayaç Kaydet</button>
                    </div>
                  </form>

                  {transitionOptions.length > 0 ? (
                    <form
                      className="mold-registry-form-grid"
                      action={(formData) => runAction(() => transitionMoldLifecycleAction(formData))}
                    >
                      <input type="hidden" name="moldId" value={activeDetail.id} />
                      <input type="hidden" name="expectedVersion" value={activeDetail.version} />
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
                        <button type="submit" className="mold-registry-button primary">Durum Geçişi</button>
                      </div>
                    </form>
                  ) : null}
                </>
              ) : null}

              {activeDetail ? (
                <section aria-label="Yaşam döngüsü geçmişi">
                  <h3>Yaşam Döngüsü Geçmişi</h3>
                  <div className="mold-registry-history">
                    {history.length === 0 ? (
                      <div className="mold-registry-empty">Henüz geçiş kaydı yok.</div>
                    ) : (
                      history.map((event) => (
                        <article key={event.id} className="mold-registry-history-item">
                          <strong>{getMoldLifecycleStateLabel(event.previousState)} → {getMoldLifecycleStateLabel(event.newState)}</strong>
                          <div>{event.reason}</div>
                          <small>{event.actorDisplayName} · {formatMoldDateTime(event.occurredAtUtc)}</small>
                        </article>
                      ))
                    )}
                  </div>
                  <div><small>Sayaç kaynağı: {getMoldCounterSourceLabel(activeDetail.counterSource)}</small></div>
                </section>
              ) : null}
            </>
          )}
        </aside>
      </div>
    </main>
  );
}
