"use client";

import { useMemo, useState, useTransition } from "react";
import { Boxes, LoaderCircle, Search, X } from "lucide-react";

import type {
  MoldCompatibilityOverview,
  MoldDetail,
  MoldLifecycleEvent,
  MoldMaintenanceRecord,
  MoldRevisionRecord,
  MoldSummary,
  MoldTimelineEntry,
  MoldTrialRecord,
  MoldWorkspaceSummary,
} from "../domain/molds";
import {
  formatMoldDateTime,
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

type AttentionItem = Readonly<{
  id: string;
  moldId: string;
  label: string;
  meta: string;
  tone: "critical" | "warning" | "info";
}>;

function lifecycleBadgeClass(state: string): string {
  switch (state) {
    case "active":
      return "mold-registry-badge mold-registry-badge--active";
    case "maintenance":
      return "mold-registry-badge mold-registry-badge--maintenance";
    case "trial":
      return "mold-registry-badge mold-registry-badge--trial";
    case "quarantined":
    case "scrapped":
      return "mold-registry-badge mold-registry-badge--quarantined";
    case "draft":
    case "retired":
      return "mold-registry-badge mold-registry-badge--draft";
    default:
      return "mold-registry-badge mold-registry-badge--neutral";
  }
}

function buildAttentionItems(molds: readonly MoldSummary[]): AttentionItem[] {
  const items: AttentionItem[] = [];

  for (const mold of molds) {
    if (mold.lifecycleState === "quarantined") {
      items.push({
        id: `quarantine-${mold.id}`,
        moldId: mold.id,
        label: "Karantinada",
        meta: `${mold.code} · ${mold.name}`,
        tone: "critical",
      });
    } else if (mold.lifecycleState === "maintenance") {
      items.push({
        id: `maintenance-${mold.id}`,
        moldId: mold.id,
        label: "Bakımda",
        meta: `${mold.code} · ${mold.name}`,
        tone: "warning",
      });
    } else if (mold.lifecycleState === "trial") {
      items.push({
        id: `trial-${mold.id}`,
        moldId: mold.id,
        label: "Denemede",
        meta: `${mold.code} · ${mold.name}`,
        tone: "info",
      });
    }
  }

  return items.slice(0, 8);
}

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
  const [selectedId, setSelectedId] = useState(initialSelectedId ?? "");
  const [detail, setDetail] = useState<MoldDetail | undefined>(initialDetail);
  const [history, setHistory] = useState<readonly MoldLifecycleEvent[]>(initialHistory);
  const [maintenanceHistory, setMaintenanceHistory] = useState<readonly MoldMaintenanceRecord[]>([]);
  const [revisionHistory, setRevisionHistory] = useState<readonly MoldRevisionRecord[]>([]);
  const [trialHistory, setTrialHistory] = useState<readonly MoldTrialRecord[]>([]);
  const [timeline, setTimeline] = useState<readonly MoldTimelineEntry[]>([]);
  const [compatibilityOverview, setCompatibilityOverview] = useState<MoldCompatibilityOverview | undefined>();
  const [compatibilityError, setCompatibilityError] = useState<string | null>(null);
  const [detailCanManage, setDetailCanManage] = useState(canManage);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [showCreate, setShowCreate] = useState(false);
  const [isPending, startTransition] = useTransition();

  const filtersActive = Boolean(search.trim() || lifecycleFilter || ownershipFilter || locationFilter);

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

  const attentionItems = useMemo(() => buildAttentionItems(molds), [molds]);
  const selectedSummary = filteredMolds.find((mold) => mold.id === selectedId) ?? molds.find((m) => m.id === selectedId);
  const activeDetail = detail && detail.id === selectedId ? detail : undefined;

  function resetFilters() {
    setSearch("");
    setLifecycleFilter("");
    setOwnershipFilter("");
    setLocationFilter("");
  }

  function selectMold(mold: MoldSummary) {
    setSelectedId(mold.id);
    setDetail(undefined);
    setHistory([]);
    setMaintenanceHistory([]);
    setRevisionHistory([]);
    setTrialHistory([]);
    setTimeline([]);
    setCompatibilityOverview(undefined);
    setCompatibilityError(null);
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
        setCompatibilityOverview(result.compatibilityOverview);
        setCompatibilityError(null);
        setDetailCanManage(result.canManage);
      } else {
        setError(result.message);
      }
    });
  }

  function selectMoldById(moldId: string) {
    const mold = molds.find((item) => item.id === moldId);
    if (mold) {
      selectMold(mold);
    }
  }

  function runAction(action: () => Promise<{ status: string; message?: string; compatibilityOverview?: MoldCompatibilityOverview }>) {
    startTransition(async () => {
      setMessage(null);
      setError(null);
      const result = await action();
      if (result.status === "failure") {
        setError(result.message ?? "İşlem başarısız.");
        return;
      }

      if (result.compatibilityOverview) {
        setCompatibilityOverview(result.compatibilityOverview);
        setCompatibilityError(null);
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
          setCompatibilityOverview(refreshed.compatibilityOverview);
          setCompatibilityError(null);
          setDetailCanManage(refreshed.canManage);
        }
      }
    });
  }

  return (
    <main className="mold-registry-page">
      <header className="mold-registry-header">
        <div className="mold-registry-header__intro">
          <p className="mold-registry-header__eyebrow">KALIP YÖNETİMİ</p>
          <h1>Kalıplar</h1>
          <p className="mold-registry-header__subtitle">
            Kalıp yaşam döngüsü, bakım geçmişi ve makine uyumluluğunu tek operasyon yüzeyinden yönetin.
          </p>
        </div>
        <div className="mold-registry-header__actions">
          {canManage ? (
            <button
              type="button"
              className="mold-registry-button primary"
              onClick={() => setShowCreate((current) => !current)}
              aria-expanded={showCreate}
            >
              Yeni Kalıp
            </button>
          ) : null}
        </div>
      </header>

      <section className="mold-registry-kpis" aria-label="Kalıp özet sayıları">
        <article className="mold-registry-kpi mold-registry-kpi--neutral">
          <strong>{summary.totalCount}</strong>
          <span>Toplam Kalıp</span>
        </article>
        <article className="mold-registry-kpi mold-registry-kpi--success">
          <strong>{summary.activeCount}</strong>
          <span>Aktif</span>
        </article>
        <article className="mold-registry-kpi mold-registry-kpi--warning">
          <strong>{summary.maintenanceCount}</strong>
          <span>Bakımda</span>
        </article>
        <article className="mold-registry-kpi mold-registry-kpi--info">
          <strong>{summary.trialCount}</strong>
          <span>Denemede</span>
        </article>
        <article className="mold-registry-kpi mold-registry-kpi--critical">
          <strong>{summary.quarantinedCount}</strong>
          <span>Kritik Dikkat</span>
          <small>Karantinada</small>
        </article>
      </section>

      <section className="mold-attention" aria-label="Operasyonel Dikkat">
        <div className="mold-attention__header">
          <h2>Operasyonel Dikkat</h2>
        </div>
        {attentionItems.length === 0 ? (
          <p className="mold-attention__empty">Şu an takip gerektiren operasyonel kalıp kaydı yok.</p>
        ) : (
          <ul className="mold-attention__list">
            {attentionItems.map((item) => (
              <li key={item.id}>
                <button
                  type="button"
                  className={`mold-attention__item mold-attention__item--${item.tone}`}
                  onClick={() => selectMoldById(item.moldId)}
                >
                  <span className="mold-attention__item-label">{item.label}</span>
                  <span className="mold-attention__item-meta">{item.meta}</span>
                </button>
              </li>
            ))}
          </ul>
        )}
      </section>

      {error ? (
        <div className="mold-registry-error" role="alert">
          {error}
        </div>
      ) : null}
      {message ? (
        <div className="mold-registry-empty" role="status">
          {message}
        </div>
      ) : null}
      {isPending ? (
        <div className="mold-registry-status" role="status" aria-live="polite">
          <LoaderCircle aria-hidden="true" size={16} />
          İşleniyor…
        </div>
      ) : null}

      {showCreate && canManage ? (
        <section className="mold-registry-panel" aria-label="Yeni kalıp formu">
          <div className="mold-form-section">
            <p className="mold-form-section__label">Yeni Kalıp</p>
            <h2>Kalıp kaydı oluştur</h2>
            <p className="mold-form-hint">Zorunlu alanlar işaretlidir. Kayıt sonrası yaşam döngüsü taslak olarak başlar.</p>
          </div>
          <form
            className="mold-registry-form-grid"
            action={(formData) => runAction(() => createMoldAction(formData))}
          >
            <label className="mold-required">
              Kalıp Kodu
              <input name="code" required aria-required="true" />
            </label>
            <label className="mold-required">
              Kalıp Adı
              <input name="name" required aria-required="true" />
            </label>
            <label>
              Sahiplik
              <select name="ownershipType" defaultValue="company-owned">
                {MOLD_OWNERSHIP_FORM_OPTIONS.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Sahip Referansı
              <input name="ownerReference" />
            </label>
            <label>
              Konum
              <select name="locationType" defaultValue="warehouse">
                {MOLD_LOCATION_FORM_OPTIONS.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Konum Etiketi
              <input name="locationLabel" />
            </label>
            <label>
              Kaynak
              <select name="sourceType" defaultValue="manual">
                {MOLD_SOURCE_FORM_OPTIONS.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Göz Sayısı
              <input name="cavityCount" type="number" min="1" />
            </label>
            <label className="full-width">
              Açıklama
              <textarea name="description" rows={2} />
            </label>
            <input type="hidden" name="idempotencyKey" value={crypto.randomUUID()} />
            <div className="mold-registry-actions full-width">
              <button type="button" className="mold-registry-button ghost" onClick={() => setShowCreate(false)}>
                İptal
              </button>
              <button type="submit" className="mold-registry-button primary">
                Kaydet
              </button>
            </div>
          </form>
        </section>
      ) : null}

      <div className="mold-registry-layout">
        <section className="mold-registry-surface" aria-label="Kalıp listesi">
          <div className="mold-registry-surface__header">
            <h2 className="mold-registry-surface__title">Kalıp Sicili</h2>
            <span className="mold-registry-surface__count">
              {filteredMolds.length} / {molds.length} kayıt
            </span>
          </div>

          <div className="mold-registry-toolbar" aria-label="Kalıp arama ve filtreler">
            <label>
              Ara
              <span className="mold-registry-search">
                <Search aria-hidden="true" size={14} className="mold-registry-search__icon" />
                <input
                  value={search}
                  onChange={(event) => setSearch(event.target.value)}
                  placeholder="Kalıp kodu veya adı"
                />
              </span>
            </label>
            <label>
              Durum
              <select value={lifecycleFilter} onChange={(event) => setLifecycleFilter(event.target.value)}>
                {MOLD_LIFECYCLE_FILTER_OPTIONS.map((option) => (
                  <option key={option.value || "all"} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Sahiplik
              <select value={ownershipFilter} onChange={(event) => setOwnershipFilter(event.target.value)}>
                {MOLD_OWNERSHIP_FILTER_OPTIONS.map((option) => (
                  <option key={option.value || "all"} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Lokasyon
              <select value={locationFilter} onChange={(event) => setLocationFilter(event.target.value)}>
                {MOLD_LOCATION_FILTER_OPTIONS.map((option) => (
                  <option key={option.value || "all"} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>
            {filtersActive ? (
              <button
                type="button"
                className="mold-registry-button compact mold-registry-toolbar__reset"
                onClick={resetFilters}
              >
                <X aria-hidden="true" size={14} />
                Filtreleri Temizle
              </button>
            ) : (
              <span className="mold-registry-toolbar__reset" aria-hidden="true" />
            )}
          </div>

          <div className="mold-registry-table-wrap">
            {filteredMolds.length === 0 ? (
              <div className="mold-registry-empty">
                <Boxes aria-hidden="true" />
                <p>Kayıtlı kalıp bulunamadı.</p>
              </div>
            ) : (
              <table className="mold-registry-table">
                <thead>
                  <tr>
                    <th>Kalıp</th>
                    <th>Kod</th>
                    <th>Durum</th>
                    <th>Sahiplik</th>
                    <th>Lokasyon</th>
                    <th>Göz Sayısı</th>
                    <th>Güncel Sayaç</th>
                    <th>Son Güncelleme</th>
                    <th>İşlem</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredMolds.map((mold) => {
                    const locationText = `${getMoldLocationLabel(mold.locationType)}${
                      mold.locationLabel ? ` · ${mold.locationLabel}` : ""
                    }`;
                    return (
                      <tr
                        key={mold.id}
                        data-selected={mold.id === selectedId}
                        tabIndex={0}
                        onClick={() => selectMold(mold)}
                        onKeyDown={(event) => {
                          if (event.key === "Enter" || event.key === " ") {
                            event.preventDefault();
                            selectMold(mold);
                          }
                        }}
                      >
                        <td>
                          <div className="mold-identity">
                            <span className="mold-identity__name" title={mold.name}>
                              {mold.name}
                            </span>
                          </div>
                        </td>
                        <td>
                          <span className="mold-identity__code">{mold.code}</span>
                        </td>
                        <td>
                          <span className={lifecycleBadgeClass(mold.lifecycleState)}>
                            {getMoldLifecycleStateLabel(mold.lifecycleState)}
                          </span>
                        </td>
                        <td>
                          <span className="mold-cell-truncate" title={getMoldOwnershipLabel(mold.ownershipType)}>
                            {getMoldOwnershipLabel(mold.ownershipType)}
                          </span>
                        </td>
                        <td>
                          <span className="mold-cell-truncate" title={locationText}>
                            {locationText}
                          </span>
                        </td>
                        <td>{formatMoldNumber(mold.cavityCount)}</td>
                        <td>{formatMoldNumber(mold.currentShotCount)}</td>
                        <td>
                          <span className="mold-cell-truncate" title={formatMoldDateTime(mold.updatedAtUtc)}>
                            {formatMoldDateTime(mold.updatedAtUtc)}
                          </span>
                        </td>
                        <td className="mold-registry-row-action">
                          <button
                            type="button"
                            className="mold-registry-button compact"
                            onClick={(event) => {
                              event.stopPropagation();
                              selectMold(mold);
                            }}
                          >
                            Detay
                          </button>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            )}
          </div>
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
              compatibilityOverview={compatibilityOverview}
              compatibilityError={compatibilityError}
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
