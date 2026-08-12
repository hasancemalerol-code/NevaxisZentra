"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import { X } from "lucide-react";

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
import {
  recordMoldCounterAction,
  transitionMoldLifecycleAction,
  updateMoldAction,
} from "../server/mold-writer";
import { MoldCompatibilityDetailPanel } from "./mold-compatibility-detail-panel";

type DetailTab = "overview" | "maintenance" | "revisions" | "trials" | "timeline" | "compatibility";
type ActionPanel = "edit" | "counter" | "transition" | null;

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
  onClose?: () => void;
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

function timelineDotClass(kind: string): string {
  switch (kind) {
    case "maintenance":
      return "mold-timeline__dot mold-timeline__dot--maintenance";
    case "revision":
      return "mold-timeline__dot mold-timeline__dot--revision";
    case "trial":
      return "mold-timeline__dot mold-timeline__dot--trial";
    case "counter":
      return "mold-timeline__dot mold-timeline__dot--counter";
    default:
      return "mold-timeline__dot";
  }
}

function localizeTimelineStatus(value?: string): string | undefined {
  if (!value) {
    return undefined;
  }

  const lifecycle = getMoldLifecycleStateLabel(value);
  if (lifecycle !== "Bilinmiyor") {
    return lifecycle;
  }

  const maintenance = getMoldMaintenanceStatusLabel(value);
  if (maintenance !== "Bilinmiyor") {
    return maintenance;
  }

  const trial = getMoldTrialResultLabel(value);
  if (trial !== "Bilinmiyor") {
    return trial;
  }

  return value.includes("-") ? "Bilinmiyor" : value;
}

const DETAIL_TABS = [
  ["overview", "Genel Bakış"],
  ["maintenance", "Bakım Geçmişi"],
  ["revisions", "Revizyonlar"],
  ["trials", "Denemeler"],
  ["timeline", "Zaman Çizelgesi"],
  ["compatibility", "Makine Uyumluluğu"],
] as const;

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
  onClose,
}: MoldHistoryDetailPanelProps) {
  const [tab, setTab] = useState<DetailTab>("overview");
  const [actionPanel, setActionPanel] = useState<ActionPanel>(null);
  const [transitionTarget, setTransitionTarget] = useState("");
  const [stickyContextVisible, setStickyContextVisible] = useState(false);
  const heroRef = useRef<HTMLElement | null>(null);

  const activeTabLabel = DETAIL_TABS.find(([value]) => value === tab)?.[1] ?? "Genel Bakış";

  useEffect(() => {
    const hero = heroRef.current;
    if (!hero) {
      return;
    }

    const updateStickyVisibility = () => {
      const topBarRaw = getComputedStyle(document.documentElement).getPropertyValue("--top-bar-height").trim();
      const topBarPx = Number.parseFloat(topBarRaw) || 72;
      const rect = hero.getBoundingClientRect();
      setStickyContextVisible(rect.bottom <= topBarPx + 1);
    };

    updateStickyVisibility();
    window.addEventListener("scroll", updateStickyVisibility, { passive: true });
    window.addEventListener("resize", updateStickyVisibility);
    return () => {
      window.removeEventListener("scroll", updateStickyVisibility);
      window.removeEventListener("resize", updateStickyVisibility);
    };
  }, [selectedSummary.id]);

  function selectTab(next: DetailTab) {
    setTab(next);
    requestAnimationFrame(() => {
      const panel = document.getElementById(`mold-tab-panel-${next}`);
      if (!panel) return;
      const topBarRaw = getComputedStyle(document.documentElement).getPropertyValue("--top-bar-height").trim();
      const topBarPx = Number.parseFloat(topBarRaw) || 72;
      const sticky = document.querySelector(".mold-sticky-context.is-visible") as HTMLElement | null;
      const stickyH = sticky?.getBoundingClientRect().height || 0;
      const offset = topBarPx + stickyH + 8;
      const nextTop = window.scrollY + panel.getBoundingClientRect().top - offset;
      window.scrollTo({ top: Math.max(0, nextTop), behavior: "auto" });
    });
  }

  const historyKpis = useMemo(
    () => ({
      maintenanceCount: maintenanceHistory.length,
      openMaintenanceCount: maintenanceHistory.filter(
        (item) => item.status === "planned" || item.status === "in-progress",
      ).length,
      revisionCount: revisionHistory.length,
      trialCount: trialHistory.length,
    }),
    [maintenanceHistory, revisionHistory, trialHistory],
  );

  const transitionOptions = detail ? MOLD_TRANSITION_TARGETS[detail.lifecycleState] ?? [] : [];
  const openMaintenance = maintenanceHistory.find(
    (item) => item.status === "planned" || item.status === "in-progress",
  );
  const selectedTarget = transitionTarget || transitionOptions[0]?.value || "";
  const isTerminalTransition = selectedTarget === "scrapped" || selectedTarget === "quarantined";

  const locationSummary = `${getMoldLocationLabel(selectedSummary.locationType)}${
    selectedSummary.locationLabel ? ` · ${selectedSummary.locationLabel}` : ""
  }`;

  function toggleAction(panel: ActionPanel) {
    setActionPanel((current) => (current === panel ? null : panel));
  }

  return (
    <>
      <header
        ref={heroRef}
        className="mold-detail-header"
        id="mold-detail-focus-target"
        tabIndex={-1}
      >
        <div className="mold-detail-header__context">
          <p className="mold-detail-breadcrumb">Kalıplar / {selectedSummary.code}</p>
          {onClose ? (
            <button
              type="button"
              className="mold-detail-close"
              aria-label="Kalıp detayını kapat"
              title="Kalıp detayını kapat"
              onClick={onClose}
            >
              <X aria-hidden="true" size={18} strokeWidth={2.25} />
            </button>
          ) : null}
        </div>

        <div className="mold-detail-header__identity">
          <div className="mold-detail-header__title-row">
            <h2
              className="mold-detail-header__name"
              id="mold-detail-heading"
              title={selectedSummary.name}
            >
              {selectedSummary.name}
            </h2>
            <span className={lifecycleBadgeClass(selectedSummary.lifecycleState)}>
              {getMoldLifecycleStateLabel(selectedSummary.lifecycleState)}
            </span>
          </div>
          <span className="mold-detail-header__code" title={selectedSummary.code}>
            {selectedSummary.code}
          </span>
        </div>

        <div className="mold-detail-header__meta" aria-label="Kalıp özet bilgileri">
          <div className="mold-detail-meta-item">
            <span className="mold-detail-meta-item__label">Sahiplik</span>
            <strong className="mold-detail-meta-item__value">{getMoldOwnershipLabel(selectedSummary.ownershipType)}</strong>
          </div>
          <div className="mold-detail-meta-item">
            <span className="mold-detail-meta-item__label">Lokasyon</span>
            <strong className="mold-detail-meta-item__value" title={locationSummary}>
              {locationSummary}
            </strong>
          </div>
          <div className="mold-detail-meta-item">
            <span className="mold-detail-meta-item__label">Güncel Sayaç</span>
            <strong className="mold-detail-meta-item__value">{formatMoldNumber(selectedSummary.currentShotCount)}</strong>
          </div>
          <div className="mold-detail-meta-item">
            <span className="mold-detail-meta-item__label">Son Güncelleme</span>
            <strong className="mold-detail-meta-item__value">{formatMoldDateTime(selectedSummary.updatedAtUtc)}</strong>
          </div>
        </div>

        {canManage ? (
          <div className="mold-detail-header__actions">
            <button
              type="button"
              className="mold-registry-button compact"
              aria-pressed={actionPanel === "edit"}
              onClick={() => toggleAction("edit")}
            >
              Düzenle
            </button>
            <button
              type="button"
              className="mold-registry-button compact"
              aria-pressed={actionPanel === "counter"}
              onClick={() => toggleAction("counter")}
            >
              Sayaç Kaydet
            </button>
            {transitionOptions.length > 0 ? (
              <button
                type="button"
                className="mold-registry-button compact"
                aria-pressed={actionPanel === "transition"}
                onClick={() => toggleAction("transition")}
              >
                Durum Değiştir
              </button>
            ) : null}
          </div>
        ) : null}
      </header>

      <div
        className={`mold-sticky-context${stickyContextVisible ? " is-visible" : ""}`}
        data-mold-sticky-context={stickyContextVisible ? "true" : "false"}
        aria-hidden={!stickyContextVisible}
        role={stickyContextVisible ? "region" : undefined}
        aria-label={stickyContextVisible ? "Seçili kalıp bağlamı" : undefined}
      >
        <div className="mold-sticky-context__identity">
          <p className="mold-sticky-context__name" title={selectedSummary.name}>
            {selectedSummary.name}
          </p>
          <span className="mold-sticky-context__code" title={selectedSummary.code}>
            {selectedSummary.code}
          </span>
          <span className={lifecycleBadgeClass(selectedSummary.lifecycleState)}>
            {getMoldLifecycleStateLabel(selectedSummary.lifecycleState)}
          </span>
          <span className="mold-sticky-context__tab">{activeTabLabel}</span>
        </div>
        {onClose ? (
          <button
            type="button"
            className="mold-detail-close"
            aria-label="Kalıp detayını kapat"
            title="Kalıp detayını kapat"
            tabIndex={stickyContextVisible ? 0 : -1}
            onClick={onClose}
          >
            <X aria-hidden="true" size={18} strokeWidth={2.25} />
          </button>
        ) : null}
      </div>

      {canManage && actionPanel === "edit" && detail ? (
        <section className="mold-detail-action-panel" aria-label="Kalıp düzenle">
          <h3 className="mold-detail-action-panel__title">Kalıp Düzenle</h3>
          <p className="mold-form-hint">Kimlik alanları korunur; sahiplik ve konum güncellenebilir.</p>
          <form className="mold-registry-form-grid" action={(formData) => onAction(() => updateMoldAction(formData))}>
            <input type="hidden" name="moldId" value={detail.id} />
            <input type="hidden" name="expectedVersion" value={detail.version} />
            <label className="full-width mold-required">
              Ad
              <input name="name" defaultValue={detail.name} required aria-required="true" />
            </label>
            <label>
              Sahiplik
              <select name="ownershipType" defaultValue={detail.ownershipType}>
                {MOLD_OWNERSHIP_FORM_OPTIONS.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Konum
              <select name="locationType" defaultValue={detail.locationType}>
                {MOLD_LOCATION_FORM_OPTIONS.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>
            <div className="mold-registry-actions full-width">
              <button type="button" className="mold-registry-button ghost" onClick={() => setActionPanel(null)}>
                Kapat
              </button>
              <button type="submit" className="mold-registry-button primary">
                Kaydı Güncelle
              </button>
            </div>
          </form>
        </section>
      ) : null}

      {canManage && actionPanel === "counter" && detail ? (
        <section className="mold-detail-action-panel" aria-label="Sayaç kaydet">
          <h3 className="mold-detail-action-panel__title">Sayaç Kaydet</h3>
          <p className="mold-form-hint">Manuel sayaç kaydı oluşturur; mevcut API sözleşmesi korunur.</p>
          <form
            className="mold-registry-form-grid"
            action={(formData) => onAction(() => recordMoldCounterAction(formData))}
          >
            <input type="hidden" name="moldId" value={detail.id} />
            <input type="hidden" name="expectedVersion" value={detail.version} />
            <input type="hidden" name="idempotencyKey" value={crypto.randomUUID()} />
            <label className="mold-required">
              Vuruş Sayısı
              <input
                name="shotCount"
                type="number"
                min="0"
                required
                aria-required="true"
                defaultValue={detail.currentShotCount ?? 0}
              />
            </label>
            <label className="full-width">
              Not
              <input name="note" placeholder="İsteğe bağlı not" />
            </label>
            <div className="mold-registry-actions full-width">
              <button type="button" className="mold-registry-button ghost" onClick={() => setActionPanel(null)}>
                Kapat
              </button>
              <button type="submit" className="mold-registry-button primary">
                Sayaç Kaydet
              </button>
            </div>
          </form>
        </section>
      ) : null}

      {canManage && actionPanel === "transition" && detail && transitionOptions.length > 0 ? (
        <section className="mold-detail-action-panel" aria-label="Durum değiştir">
          <h3 className="mold-detail-action-panel__title">Durum Değiştir</h3>
          <p className="mold-form-hint">Geçiş gerekçesi zorunludur. Terminal durumlar ek onay gerektirir.</p>
          {isTerminalTransition ? (
            <p className="mold-form-warning" role="status">
              Seçilen hedef durum kritik bir geçiştir. İşlemi onaylamadan önce gerekçeyi doğrulayın.
            </p>
          ) : null}
          <form
            className="mold-registry-form-grid"
            action={(formData) => onAction(() => transitionMoldLifecycleAction(formData))}
          >
            <input type="hidden" name="moldId" value={detail.id} />
            <input type="hidden" name="expectedVersion" value={detail.version} />
            <input type="hidden" name="idempotencyKey" value={crypto.randomUUID()} />
            <label className="mold-required">
              Hedef Durum
              <select
                name="targetState"
                required
                aria-required="true"
                value={selectedTarget}
                onChange={(event) => setTransitionTarget(event.target.value)}
              >
                {transitionOptions.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>
            <label className="full-width mold-required">
              Geçiş Gerekçesi
              <input name="reason" required aria-required="true" placeholder="Operasyonel gerekçe" />
            </label>
            <div className="mold-registry-actions full-width">
              <button type="button" className="mold-registry-button ghost" onClick={() => setActionPanel(null)}>
                Kapat
              </button>
              <button type="submit" className="mold-registry-button primary">
                Durum Geçişi Uygula
              </button>
            </div>
          </form>
        </section>
      ) : null}

      <section className="mold-registry-kpis mold-registry-kpis--compact" aria-label="Kalıp geçmişi özeti">
        <article className="mold-registry-kpi mold-registry-kpi--neutral">
          <strong>{historyKpis.maintenanceCount}</strong>
          <span>Bakım</span>
        </article>
        <article className="mold-registry-kpi mold-registry-kpi--warning">
          <strong>{historyKpis.openMaintenanceCount}</strong>
          <span>Açık Bakım</span>
        </article>
        <article className="mold-registry-kpi mold-registry-kpi--info">
          <strong>{historyKpis.revisionCount}</strong>
          <span>Revizyon</span>
        </article>
        <article className="mold-registry-kpi mold-registry-kpi--success">
          <strong>{historyKpis.trialCount}</strong>
          <span>Deneme</span>
        </article>
      </section>

      <div className="mold-detail-tabs-rail">
        <nav className="mold-detail-tabs" aria-label="Kalıp detay sekmeleri" role="tablist">
          {DETAIL_TABS.map(([value, label]) => (
            <button
              key={value}
              type="button"
              role="tab"
              id={`mold-tab-${value}`}
              aria-selected={tab === value}
              aria-controls={`mold-tab-panel-${value}`}
              className="mold-detail-tabs__button"
              onClick={() => selectTab(value)}
            >
              {label}
            </button>
          ))}
        </nav>
      </div>

      {!detail && isPending ? (
        <div className="mold-registry-empty" role="status" aria-live="polite">
          Detay yükleniyor…
        </div>
      ) : null}

      {!detail && !isPending ? (
        <div className="mold-registry-empty">Detay yüklenemedi veya erişim yok.</div>
      ) : null}

      {detail && tab === "overview" ? (
        <div
          id="mold-tab-panel-overview"
          role="tabpanel"
          aria-labelledby="mold-tab-overview"
          className="mold-overview-grid"
        >
          <section className="mold-overview-card">
            <h3 className="mold-overview-card__title">Teknik Özellikler</h3>
            <dl className="mold-registry-meta-grid">
              <div>
                <dt>Göz Sayısı</dt>
                <dd>{formatMoldNumber(detail.cavityCount ?? selectedSummary.cavityCount)}</dd>
              </div>
              <div>
                <dt>Kalıp Tipi</dt>
                <dd>{detail.moldType ?? "—"}</dd>
              </div>
              <div>
                <dt>Üretici</dt>
                <dd>{detail.manufacturer ?? "—"}</dd>
              </div>
              <div>
                <dt>Seri No</dt>
                <dd>{detail.serialNumber ?? "—"}</dd>
              </div>
              <div>
                <dt>Ağırlık (kg)</dt>
                <dd>{formatMoldNumber(detail.moldWeightKg)}</dd>
              </div>
              <div>
                <dt>Boyutlar (mm)</dt>
                <dd>
                  {formatMoldNumber(detail.dimensionLengthMm)} × {formatMoldNumber(detail.dimensionWidthMm)} ×{" "}
                  {formatMoldNumber(detail.dimensionHeightMm)}
                </dd>
              </div>
            </dl>
          </section>

          <section className="mold-overview-card">
            <h3 className="mold-overview-card__title">Yaşam Döngüsü ve Lokasyon</h3>
            <dl className="mold-registry-meta-grid">
              <div>
                <dt>Durum</dt>
                <dd>{getMoldLifecycleStateLabel(selectedSummary.lifecycleState)}</dd>
              </div>
              <div>
                <dt>Lokasyon</dt>
                <dd>{locationSummary}</dd>
              </div>
              <div>
                <dt>Kaynak</dt>
                <dd>{getMoldSourceLabel(selectedSummary.sourceType)}</dd>
              </div>
              <div>
                <dt>Son Güncelleme</dt>
                <dd>{formatMoldDateTime(selectedSummary.updatedAtUtc)}</dd>
              </div>
            </dl>
          </section>

          <section className="mold-overview-card">
            <h3 className="mold-overview-card__title">Sahiplik ve Müşteri</h3>
            <dl className="mold-registry-meta-grid">
              <div>
                <dt>Sahiplik</dt>
                <dd>{getMoldOwnershipLabel(selectedSummary.ownershipType)}</dd>
              </div>
              <div>
                <dt>Sahip Referansı</dt>
                <dd>{detail.ownerReference ?? "—"}</dd>
              </div>
              <div>
                <dt>Ürün Referansı</dt>
                <dd>{detail.primaryProductReference ?? "—"}</dd>
              </div>
              <div>
                <dt>Malzeme Grubu</dt>
                <dd>{detail.materialFamilyReference ?? "—"}</dd>
              </div>
            </dl>
          </section>

          <section className="mold-overview-card">
            <h3 className="mold-overview-card__title">Sayaç ve Operasyon</h3>
            <dl className="mold-registry-meta-grid">
              <div>
                <dt>Güncel Sayaç</dt>
                <dd>{formatMoldNumber(selectedSummary.currentShotCount)}</dd>
              </div>
              <div>
                <dt>Sayaç Kaynağı</dt>
                <dd>{getMoldCounterSourceLabel(detail.counterSource)}</dd>
              </div>
              <div>
                <dt>Sayaç Zamanı</dt>
                <dd>{formatMoldDateTime(detail.counterRecordedAtUtc)}</dd>
              </div>
              <div>
                <dt>Nominal Ömür</dt>
                <dd>{formatMoldNumber(detail.nominalShotLife)}</dd>
              </div>
            </dl>
          </section>

          {!canManage ? (
            <div className="mold-overview-card mold-overview-card--wide">
              <div className="mold-readonly-note">Bu kalıp için yalnızca görüntüleme yetkiniz var.</div>
            </div>
          ) : null}

          <section className="mold-overview-card mold-overview-card--wide" aria-label="Yaşam döngüsü geçmişi">
            <h3 className="mold-overview-card__title">Son Yaşam Döngüsü Olayları</h3>
            <div className="mold-registry-history">
              {lifecycleHistory.length === 0 ? (
                <div className="mold-registry-empty">Henüz geçiş kaydı yok.</div>
              ) : (
                lifecycleHistory.slice(0, 5).map((event) => (
                  <article key={event.id} className="mold-registry-history-item">
                    <strong>
                      {getMoldLifecycleStateLabel(event.previousState)} → {getMoldLifecycleStateLabel(event.newState)}
                    </strong>
                    <div>{event.reason}</div>
                    <small>
                      {event.actorDisplayName} · {formatMoldDateTime(event.occurredAtUtc)}
                    </small>
                  </article>
                ))
              )}
            </div>
          </section>
        </div>
      ) : null}

      {detail && tab === "maintenance" ? (
        <div id="mold-tab-panel-maintenance" role="tabpanel" aria-labelledby="mold-tab-maintenance">
          {canManage ? (
            <section className="mold-form-section" aria-label="Bakım kaydı">
              <p className="mold-form-section__label">Bakım Kaydı</p>
              <form
                className="mold-registry-form-grid"
                action={(formData) => onAction(() => createMoldMaintenanceAction(formData))}
              >
                <input type="hidden" name="moldId" value={detail.id} />
                <input type="hidden" name="idempotencyKey" value={crypto.randomUUID()} />
                <label>
                  Bakım Türü
                  <select name="maintenanceType" defaultValue="preventive">
                    {MOLD_MAINTENANCE_TYPE_OPTIONS.map((option) => (
                      <option key={option.value} value={option.value}>
                        {option.label}
                      </option>
                    ))}
                  </select>
                </label>
                <label>
                  Durum
                  <select name="status" defaultValue="planned">
                    {MOLD_MAINTENANCE_STATUS_OPTIONS.map((option) => (
                      <option key={option.value} value={option.value}>
                        {option.label}
                      </option>
                    ))}
                  </select>
                </label>
                <label className="full-width mold-required">
                  Özet
                  <input name="summary" required aria-required="true" placeholder="Örn. Planlı yağlama ve temizlik" />
                </label>
                <label className="full-width">
                  Bulgular
                  <textarea name="findings" rows={2} />
                </label>
                <label>
                  Uygulayan
                  <input name="performedByDisplayName" placeholder="Örn. Bakım Ekibi" />
                </label>
                <label>
                  Dış Bakım Talebi Ref.
                  <input name="externalMaintenanceRequestReference" placeholder="İsteğe bağlı referans" />
                </label>
                <div className="mold-registry-actions full-width">
                  <button type="submit" className="mold-registry-button primary">
                    Bakım Kaydı Aç
                  </button>
                </div>
              </form>
            </section>
          ) : null}

          {canManage && openMaintenance ? (
            <section className="mold-form-section" aria-label="Açık bakım tamamlama">
              <p className="mold-form-section__label">Açık Bakımı Tamamla</p>
              <form
                className="mold-registry-form-grid"
                action={(formData) => onAction(() => completeMoldMaintenanceAction(formData))}
              >
                <input type="hidden" name="moldId" value={detail.id} />
                <input type="hidden" name="maintenanceId" value={openMaintenance.id} />
                <input type="hidden" name="expectedVersion" value={openMaintenance.version} />
                <label className="full-width">
                  Tamamlama Notu
                  <textarea name="actionsTaken" rows={2} defaultValue="Bakım tamamlandı." />
                </label>
                <div className="mold-registry-actions full-width">
                  <button type="submit" className="mold-registry-button primary">
                    Açık Bakımı Tamamla
                  </button>
                </div>
              </form>
            </section>
          ) : null}

          <div className="mold-compat-table-wrap">
            <table className="mold-registry-table">
              <thead>
                <tr>
                  <th>Durum</th>
                  <th>Tür</th>
                  <th>Açılış</th>
                  <th>Tamamlanma</th>
                  <th>Bulgular / Aksiyon</th>
                  <th>Kaynak / Referans</th>
                </tr>
              </thead>
              <tbody>
                {maintenanceHistory.length === 0 ? (
                  <tr>
                    <td colSpan={6}>Bakım kaydı yok.</td>
                  </tr>
                ) : (
                  maintenanceHistory.map((item) => (
                    <tr key={item.id}>
                      <td>
                        <span className="mold-registry-badge">{getMoldMaintenanceStatusLabel(item.status)}</span>
                      </td>
                      <td>
                        <span className="mold-registry-badge mold-registry-badge--neutral">
                          {getMoldMaintenanceTypeLabel(item.maintenanceType)}
                        </span>
                      </td>
                      <td>{formatMoldDateTime(item.openedAtUtc)}</td>
                      <td>{formatMoldDateTime(item.completedAtUtc)}</td>
                      <td>
                        <strong>{item.summary}</strong>
                        {item.findings ? <div>{item.findings}</div> : null}
                        {item.actionsTaken ? <div>{item.actionsTaken}</div> : null}
                      </td>
                      <td>
                        {item.performedByDisplayName ?? "—"}
                        {item.externalMaintenanceRequestReference
                          ? ` · ${item.externalMaintenanceRequestReference}`
                          : ""}
                        {item.externalWorkOrderReference ? ` · ${item.externalWorkOrderReference}` : ""}
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>
      ) : null}

      {detail && tab === "revisions" ? (
        <div id="mold-tab-panel-revisions" role="tabpanel" aria-labelledby="mold-tab-revisions">
          {canManage ? (
            <section className="mold-form-section" aria-label="Revizyon kaydı">
              <p className="mold-form-section__label">Revizyon Kaydı</p>
              <form
                className="mold-registry-form-grid"
                action={(formData) => onAction(() => appendMoldRevisionAction(formData))}
              >
                <input type="hidden" name="moldId" value={detail.id} />
                <input type="hidden" name="idempotencyKey" value={crypto.randomUUID()} />
                <label className="mold-required">
                  Revizyon No
                  <input name="revisionNumber" required aria-required="true" placeholder="R-01" />
                </label>
                <label>
                  Tür
                  <select name="revisionType" defaultValue="cavity-change">
                    {MOLD_REVISION_TYPE_OPTIONS.map((option) => (
                      <option key={option.value} value={option.value}>
                        {option.label}
                      </option>
                    ))}
                  </select>
                </label>
                <label>
                  Tarih
                  <input name="revisionDate" type="date" defaultValue={new Date().toISOString().slice(0, 10)} />
                </label>
                <label className="full-width mold-required">
                  Özet
                  <input name="summary" required aria-required="true" placeholder="Örn. Gözenek sayısı güncellendi" />
                </label>
                <label>
                  Önceki Göz
                  <input name="previousCavityCount" type="number" min="1" />
                </label>
                <label>
                  Yeni Göz
                  <input name="newCavityCount" type="number" min="1" />
                </label>
                <div className="mold-registry-actions full-width">
                  <button type="submit" className="mold-registry-button primary">
                    Revizyon Ekle
                  </button>
                </div>
              </form>
            </section>
          ) : null}

          <div className="mold-compat-table-wrap">
            <table className="mold-registry-table">
              <thead>
                <tr>
                  <th>Revizyon No</th>
                  <th>Tür</th>
                  <th>Tarih</th>
                  <th>Teknik Değişiklik</th>
                  <th>Önceki / Yeni</th>
                </tr>
              </thead>
              <tbody>
                {revisionHistory.length === 0 ? (
                  <tr>
                    <td colSpan={5}>Revizyon kaydı yok.</td>
                  </tr>
                ) : (
                  revisionHistory.map((item) => (
                    <tr key={item.id}>
                      <td>
                        <strong>{item.revisionNumber}</strong>
                      </td>
                      <td>
                        <span className="mold-registry-badge">{getMoldRevisionTypeLabel(item.revisionType)}</span>
                      </td>
                      <td>{item.revisionDate}</td>
                      <td>
                        <strong>{item.summary}</strong>
                        {item.technicalChange ? <div>{item.technicalChange}</div> : null}
                      </td>
                      <td>
                        {item.previousCavityCount !== undefined || item.newCavityCount !== undefined
                          ? `${formatMoldNumber(item.previousCavityCount)} → ${formatMoldNumber(item.newCavityCount)}`
                          : "—"}
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>
      ) : null}

      {detail && tab === "trials" ? (
        <div id="mold-tab-panel-trials" role="tabpanel" aria-labelledby="mold-tab-trials">
          {canManage ? (
            <section className="mold-form-section" aria-label="Deneme kaydı">
              <p className="mold-form-section__label">Deneme Kaydı</p>
              <form
                className="mold-registry-form-grid"
                action={(formData) => onAction(() => appendMoldTrialAction(formData))}
              >
                <input type="hidden" name="moldId" value={detail.id} />
                <input type="hidden" name="idempotencyKey" value={crypto.randomUUID()} />
                <label className="mold-required">
                  Deneme No
                  <input name="trialNumber" required aria-required="true" placeholder="T-01" />
                </label>
                <label>
                  Sonuç
                  <select name="result" defaultValue="successful">
                    {MOLD_TRIAL_RESULT_OPTIONS.map((option) => (
                      <option key={option.value} value={option.value}>
                        {option.label}
                      </option>
                    ))}
                  </select>
                </label>
                <label>
                  Tarih
                  <input name="trialDate" type="date" defaultValue={new Date().toISOString().slice(0, 10)} />
                </label>
                <label>
                  Makine Ref.
                  <input name="machineReference" placeholder="IM-120" />
                </label>
                <label>
                  Vuruş
                  <input name="shotCount" type="number" min="0" />
                </label>
                <label>
                  İyi Parça
                  <input name="goodPartCount" type="number" min="0" />
                </label>
                <label>
                  Hurda
                  <input name="scrapPartCount" type="number" min="0" />
                </label>
                <label className="full-width">
                  Bulgular
                  <textarea name="findings" rows={2} placeholder="Deneme sonucu notları" />
                </label>
                <div className="mold-registry-actions full-width">
                  <button type="submit" className="mold-registry-button primary">
                    Deneme Ekle
                  </button>
                </div>
              </form>
            </section>
          ) : null}

          <div className="mold-compat-table-wrap">
            <table className="mold-registry-table">
              <thead>
                <tr>
                  <th>Deneme No</th>
                  <th>Sonuç</th>
                  <th>Tarih</th>
                  <th>Makine / Ürün / Malzeme</th>
                  <th>Çevrim / Parça</th>
                  <th>Bulgular / Karar</th>
                </tr>
              </thead>
              <tbody>
                {trialHistory.length === 0 ? (
                  <tr>
                    <td colSpan={6}>Deneme kaydı yok.</td>
                  </tr>
                ) : (
                  trialHistory.map((item) => (
                    <tr key={item.id}>
                      <td>
                        <strong>{item.trialNumber}</strong>
                      </td>
                      <td>
                        <span className="mold-registry-badge">{getMoldTrialResultLabel(item.result)}</span>
                      </td>
                      <td>{item.trialDate}</td>
                      <td>
                        {item.machineReference ?? "—"}
                        {item.productReference ? ` · ${item.productReference}` : ""}
                        {item.materialReference ? ` · ${item.materialReference}` : ""}
                      </td>
                      <td>
                        {item.cycleTimeSeconds !== undefined ? `${formatMoldNumber(item.cycleTimeSeconds)} sn` : "—"}
                        {" · "}
                        {formatMoldNumber(item.shotCount)} vuruş
                        {" · "}
                        {formatMoldNumber(item.goodPartCount)} iyi / {formatMoldNumber(item.scrapPartCount)} hurda
                      </td>
                      <td>
                        {item.findings ?? "—"}
                        {item.decision ? <div>{item.decision}</div> : null}
                      </td>
                    </tr>
                  ))
                )}
              </tbody>
            </table>
          </div>
        </div>
      ) : null}

      {detail && tab === "timeline" ? (
        <div id="mold-tab-panel-timeline" role="tabpanel" aria-labelledby="mold-tab-timeline">
          {timeline.length === 0 ? (
            <div className="mold-registry-empty">Zaman çizelgesi boş.</div>
          ) : (
            <div className="mold-timeline" aria-label="Zaman çizelgesi">
              {timeline.map((entry, index) => {
                const statusLabel = localizeTimelineStatus(entry.statusOrResult);
                return (
                  <article key={`${entry.eventKind}-${entry.id}-${index}`} className="mold-timeline__item">
                    <div className="mold-timeline__rail" aria-hidden="true">
                      <span className={timelineDotClass(entry.eventKind)} />
                      <span className="mold-timeline__line" />
                    </div>
                    <div className="mold-timeline__body">
                      <h3 className="mold-timeline__title">{entry.title}</h3>
                      <p className="mold-timeline__summary">{entry.summary}</p>
                      <div className="mold-timeline__meta">
                        <span className="mold-registry-badge mold-registry-badge--neutral">
                          {getMoldTimelineKindLabel(entry.eventKind)}
                        </span>
                        {statusLabel ? <span>{statusLabel}</span> : null}
                        {entry.reference ? <span>{entry.reference}</span> : null}
                        {entry.actorDisplayName ? <span>{entry.actorDisplayName}</span> : null}
                        <span>{formatMoldDateTime(entry.eventDateUtc)}</span>
                      </div>
                    </div>
                  </article>
                );
              })}
            </div>
          )}
        </div>
      ) : null}

      {detail && tab === "compatibility" ? (
        <div id="mold-tab-panel-compatibility" role="tabpanel" aria-labelledby="mold-tab-compatibility">
          <MoldCompatibilityDetailPanel
            key={detail.id}
            moldId={detail.id}
            overview={compatibilityOverview}
            canManage={canManage}
            isPending={isPending}
            loadError={compatibilityError}
            onAction={onAction}
          />
        </div>
      ) : null}
    </>
  );
}
