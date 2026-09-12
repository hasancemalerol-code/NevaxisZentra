"use client";

import { useEffect, useMemo, useRef, useState, useTransition } from "react";
import { LoaderCircle, Package, Search, X } from "lucide-react";

import type {
  MaterialDetail,
  MaterialLifecycleEvent,
  MaterialSummary,
  MaterialWorkspaceSummary,
} from "../domain/materials";
import {
  formatMaterialDateTime,
  formatMaterialNumber,
  formatMaterialRange,
  getMaterialCategoryLabel,
  getMaterialLifecycleStatusLabel,
  getMaterialSourceLabel,
  getPhysicalFormLabel,
  getPolymerFamilyLabel,
  getSpecificationSourceLabel,
  getTechnicalDataLabel,
  MATERIAL_CATEGORY_FILTER_OPTIONS,
  MATERIAL_CATEGORY_FORM_OPTIONS,
  MATERIAL_LIFECYCLE_FILTER_OPTIONS,
  MATERIAL_PHYSICAL_FORM_OPTIONS,
  MATERIAL_POLYMER_FILTER_OPTIONS,
  MATERIAL_POLYMER_FORM_OPTIONS,
  MATERIAL_SOURCE_FORM_OPTIONS,
  MATERIAL_SPECIFICATION_SOURCE_FORM_OPTIONS,
  MATERIAL_TRANSITION_TARGETS,
} from "../presentation/material-labels";
import { loadMaterialDetailAction } from "../server/material-actions";
import {
  createMaterialAction,
  transitionMaterialLifecycleAction,
  updateMaterialAction,
  updateMaterialTechnicalAction,
} from "../server/material-writer";
import { MaterialLotPanel } from "./material-lot-panel";
import { MaterialRequirementPanel } from "./material-requirement-panel";
import "./material-registry.css";

type MaterialRegistryViewProps = Readonly<{
  summary: MaterialWorkspaceSummary;
  materials: readonly MaterialSummary[];
  canManage: boolean;
  initialSelectedId?: string;
  initialDetail?: MaterialDetail;
  initialHistory?: readonly MaterialLifecycleEvent[];
}>;

type DetailTab =
  | "overview"
  | "technical"
  | "source"
  | "history"
  | "lots"
  | "traceability"
  | "requirement"
  | "regrind-policy";
type ActionPanel = "edit" | "technical" | "transition" | null;

function lifecycleBadgeClass(status: string): string {
  switch (status) {
    case "active":
      return "material-registry-badge material-registry-badge--active";
    case "blocked":
      return "material-registry-badge material-registry-badge--quarantined";
    case "obsolete":
      return "material-registry-badge material-registry-badge--draft";
    case "draft":
      return "material-registry-badge material-registry-badge--neutral";
    default:
      return "material-registry-badge material-registry-badge--neutral";
  }
}

function technicalDataBadgeClass(hasTechnicalData: boolean): string {
  return hasTechnicalData
    ? "material-registry-badge material-registry-badge--success"
    : "material-registry-badge material-registry-badge--warning";
}

function manufacturerGradeText(material: MaterialSummary | MaterialDetail): string {
  const parts = [material.manufacturer, material.tradeNameOrGrade].filter(Boolean);
  return parts.length > 0 ? parts.join(" · ") : "—";
}

const DETAIL_TABS: readonly [DetailTab, string][] = [
  ["overview", "Genel Bakış"],
  ["technical", "Teknik Özellikler"],
  ["source", "Kaynak ve ERP Referansı"],
  ["history", "Durum Geçmişi"],
  ["lots", "Lotlar"],
  ["traceability", "İzlenebilirlik"],
  ["requirement", "İhtiyaç ve Tüketim"],
  ["regrind-policy", "Regrind Politikası"],
];

export function MaterialRegistryView({
  summary,
  materials,
  canManage,
  initialSelectedId,
  initialDetail,
  initialHistory = [],
}: MaterialRegistryViewProps) {
  const [search, setSearch] = useState("");
  const [lifecycleFilter, setLifecycleFilter] = useState("");
  const [categoryFilter, setCategoryFilter] = useState("");
  const [polymerFilter, setPolymerFilter] = useState("");
  const [selectedId, setSelectedId] = useState(initialSelectedId ?? "");
  const [detail, setDetail] = useState<MaterialDetail | undefined>(initialDetail);
  const [history, setHistory] = useState<readonly MaterialLifecycleEvent[]>(initialHistory);
  const [detailCanManage, setDetailCanManage] = useState(canManage);
  const [tab, setTab] = useState<DetailTab>("overview");
  const [actionPanel, setActionPanel] = useState<ActionPanel>(null);
  const [transitionTarget, setTransitionTarget] = useState("");
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [showCreate, setShowCreate] = useState(false);
  const [stickyContextVisible, setStickyContextVisible] = useState(false);
  const [isPending, startTransition] = useTransition();
  const heroRef = useRef<HTMLElement | null>(null);

  const filtersActive = Boolean(search.trim() || lifecycleFilter || categoryFilter || polymerFilter);
  const activeTabLabel = DETAIL_TABS.find(([value]) => value === tab)?.[1] ?? "Genel Bakış";

  const filteredMaterials = useMemo(() => {
    return materials.filter((material) => {
      const matchesSearch =
        !search.trim() ||
        material.code.toLowerCase().includes(search.toLowerCase()) ||
        material.name.toLowerCase().includes(search.toLowerCase()) ||
        (material.manufacturer?.toLowerCase().includes(search.toLowerCase()) ?? false) ||
        (material.tradeNameOrGrade?.toLowerCase().includes(search.toLowerCase()) ?? false);
      const matchesLifecycle = !lifecycleFilter || material.lifecycleStatus === lifecycleFilter;
      const matchesCategory = !categoryFilter || material.materialCategory === categoryFilter;
      const matchesPolymer = !polymerFilter || material.polymerFamily === polymerFilter;
      return matchesSearch && matchesLifecycle && matchesCategory && matchesPolymer;
    });
  }, [categoryFilter, lifecycleFilter, materials, polymerFilter, search]);

  const selectedSummary =
    filteredMaterials.find((material) => material.id === selectedId) ??
    materials.find((material) => material.id === selectedId);
  const activeDetail = detail && detail.id === selectedId ? detail : undefined;
  const detailOpen = Boolean(selectedSummary);

  function resetFilters() {
    setSearch("");
    setLifecycleFilter("");
    setCategoryFilter("");
    setPolymerFilter("");
  }

  function clearSelection() {
    const restoreId = selectedId;
    setSelectedId("");
    setDetail(undefined);
    setHistory([]);
    setTab("overview");
    setActionPanel(null);
    setMessage(null);
    setError(null);
    setStickyContextVisible(false);

    requestAnimationFrame(() => {
      const row = document.querySelector(
        `tr[data-material-id="${CSS.escape(restoreId)}"]`,
      ) as HTMLElement | null;
      row?.focus({ preventScroll: true });

      const heading = document.querySelector(".material-executive__intro h1") as HTMLElement | null;
      if (!heading) return;
      const topBarRaw = getComputedStyle(document.documentElement).getPropertyValue("--top-bar-height").trim();
      const topBarPx = Number.parseFloat(topBarRaw) || 72;
      if (heading.getBoundingClientRect().top < topBarPx) {
        heading.scrollIntoView({ block: "start", behavior: "smooth" });
      }
    });
  }

  function selectMaterial(material: MaterialSummary) {
    setSelectedId(material.id);
    setDetail(undefined);
    setHistory([]);
    setTab("overview");
    setActionPanel(null);
    setMessage(null);
    setError(null);

    startTransition(async () => {
      const result = await loadMaterialDetailAction(material.id);
      if (result.status === "success") {
        setDetail(result.material);
        setHistory(result.lifecycleHistory);
        setDetailCanManage(result.canManage);
        focusSelectedDetail();
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
      setActionPanel(null);

      if (selectedId) {
        const refreshed = await loadMaterialDetailAction(selectedId);
        if (refreshed.status === "success") {
          setDetail(refreshed.material);
          setHistory(refreshed.lifecycleHistory);
          setDetailCanManage(refreshed.canManage);
        }
      }
    });
  }

  const transitionOptions =
    activeDetail
      ? MATERIAL_TRANSITION_TARGETS[activeDetail.lifecycleStatus] ?? []
      : selectedSummary
        ? MATERIAL_TRANSITION_TARGETS[selectedSummary.lifecycleStatus] ?? []
        : [];

  useEffect(() => {
    const hero = heroRef.current;
    if (!hero || !detailOpen) {
      setStickyContextVisible(false);
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
  }, [detailOpen, selectedId, tab]);

  function focusSelectedDetail() {
    requestAnimationFrame(() => {
      const target =
        (document.getElementById("material-detail-focus-target") as HTMLElement | null) ??
        (document.getElementById("material-detail-heading") as HTMLElement | null);
      if (!target) return;

      target.focus({ preventScroll: true });

      const topBarRaw = getComputedStyle(document.documentElement).getPropertyValue("--top-bar-height").trim();
      const topBarPx = Number.parseFloat(topBarRaw) || 72;
      if (target.getBoundingClientRect().top < topBarPx) {
        target.scrollIntoView({ block: "start", behavior: "smooth" });
      }
    });
  }

  function selectTab(next: DetailTab) {
    setTab(next);
    requestAnimationFrame(() => {
      const panel = document.getElementById(`material-tab-panel-${next}`);
      if (!panel) return;
      const topBarRaw = getComputedStyle(document.documentElement).getPropertyValue("--top-bar-height").trim();
      const topBarPx = Number.parseFloat(topBarRaw) || 72;
      const sticky = document.querySelector(".material-sticky-context.is-visible") as HTMLElement | null;
      const stickyH = sticky?.getBoundingClientRect().height || 0;
      const offset = topBarPx + stickyH + 8;
      const nextTop = window.scrollY + panel.getBoundingClientRect().top - offset;
      window.scrollTo({ top: Math.max(0, nextTop), behavior: "auto" });
    });
  }

  return (
    <main className="material-registry-page">
      <section className="material-executive" aria-label="Malzeme yönetim komuta yüzeyi">
        <div className="material-executive__top">
          <div className="material-executive__intro">
            <p className="material-executive__eyebrow">MALZEME YÖNETİMİ</p>
            <h1>Malzemeler</h1>
            <p className="material-executive__subtitle">
              Enjeksiyon kalıplama operasyonları için polimer malzeme sicili, teknik veri, lot izlenebilirliği ve
              ihtiyaç zekâsını tek çalışma yüzeyinde yönetin.
            </p>
          </div>
          <div className="material-executive__actions">
            {canManage ? (
              <button
                type="button"
                className="material-registry-button primary"
                onClick={() => setShowCreate((current) => !current)}
                aria-expanded={showCreate}
              >
                Yeni Malzeme
              </button>
            ) : null}
          </div>
        </div>

        <div className="material-executive__kpis" aria-label="Malzeme özet sayıları">
          <article className="material-executive__kpi material-executive__kpi--neutral">
            <strong>{summary.totalCount}</strong>
            <span>Toplam Malzeme</span>
          </article>
          <article className="material-executive__kpi material-executive__kpi--success">
            <strong>{summary.activeCount}</strong>
            <span>Aktif</span>
          </article>
          <article className="material-executive__kpi material-executive__kpi--critical">
            <strong>{summary.blockedCount}</strong>
            <span>Bloke</span>
          </article>
          <article className="material-executive__kpi material-executive__kpi--info">
            <strong>{summary.obsoleteCount}</strong>
            <span>Kullanımdan Kalkmış</span>
          </article>
          <article className="material-executive__kpi material-executive__kpi--warning">
            <strong>{summary.missingTechnicalDataCount}</strong>
            <span>Teknik Verisi Eksik</span>
          </article>
        </div>
      </section>

      {error ? (
        <div className="material-registry-error" role="alert">
          {error}
        </div>
      ) : null}
      {message ? (
        <div className="material-registry-empty" role="status">
          {message}
        </div>
      ) : null}
      {isPending ? (
        <div className="material-registry-status" role="status" aria-live="polite">
          <LoaderCircle aria-hidden="true" size={16} />
          İşleniyor…
        </div>
      ) : null}

      {showCreate && canManage ? (
        <section className="material-registry-panel" aria-label="Yeni malzeme formu">
          <div className="material-form-section">
            <p className="material-form-section__label">Yeni Malzeme</p>
            <h2>Malzeme kaydı oluştur</h2>
            <p className="material-form-hint">Zorunlu alanlar işaretlidir. Kayıt sonrası durum taslak olarak başlar.</p>
          </div>
          <form
            className="material-registry-form-grid"
            action={(formData) => runAction(() => createMaterialAction(formData))}
          >
            <label className="material-required">
              Malzeme Kodu
              <input name="code" required aria-required="true" />
            </label>
            <label className="material-required">
              Malzeme Adı
              <input name="name" required aria-required="true" />
            </label>
            <label>
              Kategori
              <select name="materialCategory" defaultValue="virgin-polymer">
                {MATERIAL_CATEGORY_FORM_OPTIONS.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Polimer
              <select name="polymerFamily" defaultValue="unknown">
                {MATERIAL_POLYMER_FORM_OPTIONS.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Üretici
              <input name="manufacturer" />
            </label>
            <label>
              Grade / Ticari Ad
              <input name="tradeNameOrGrade" />
            </label>
            <label>
              Renk
              <input name="colorName" />
            </label>
            <label>
              Fiziksel Form
              <select name="physicalForm" defaultValue="granule">
                {MATERIAL_PHYSICAL_FORM_OPTIONS.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Kaynak
              <select name="sourceType" defaultValue="manual">
                {MATERIAL_SOURCE_FORM_OPTIONS.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>
            <label className="full-width">
              Açıklama
              <textarea name="description" rows={2} />
            </label>
            <input type="hidden" name="idempotencyKey" value={crypto.randomUUID()} />
            <div className="material-registry-actions full-width">
              <button type="button" className="material-registry-button ghost" onClick={() => setShowCreate(false)}>
                İptal
              </button>
              <button type="submit" className="material-registry-button primary">
                Kaydet
              </button>
            </div>
          </form>
        </section>
      ) : null}

      <div
        className={`material-registry-layout ${detailOpen ? "material-registry-layout--detail" : "material-registry-layout--solo"}`}
        data-registry-mode={detailOpen ? "detail" : "full"}
        data-detail-open={detailOpen ? "true" : "false"}
      >
        <section className="material-registry-surface" aria-label="Malzeme listesi">
          <div className="material-registry-surface__header">
            <h2 className="material-registry-surface__title">Malzeme Sicili</h2>
            <span className="material-registry-surface__count">
              {filteredMaterials.length} / {materials.length} kayıt
            </span>
          </div>

          <div className="material-registry-toolbar" aria-label="Malzeme arama ve filtreler">
            <label className="material-registry-toolbar__search">
              Ara
              <span className="material-registry-search">
                <Search aria-hidden="true" size={14} className="material-registry-search__icon" />
                <input
                  value={search}
                  onChange={(event) => setSearch(event.target.value)}
                  placeholder="Kod, ad, üretici veya grade"
                />
              </span>
            </label>
            <div className="material-registry-toolbar__filters">
              <label className="material-registry-toolbar__filter">
                Kategori
                <select value={categoryFilter} onChange={(event) => setCategoryFilter(event.target.value)}>
                  {MATERIAL_CATEGORY_FILTER_OPTIONS.map((option) => (
                    <option key={option.value || "all"} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
              </label>
              <label className="material-registry-toolbar__filter">
                Polimer
                <select value={polymerFilter} onChange={(event) => setPolymerFilter(event.target.value)}>
                  {MATERIAL_POLYMER_FILTER_OPTIONS.map((option) => (
                    <option key={option.value || "all"} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
              </label>
              {filtersActive ? (
                <button
                  type="button"
                  className="material-registry-button compact material-registry-toolbar__reset"
                  onClick={resetFilters}
                >
                  <X aria-hidden="true" size={14} />
                  Filtreleri Temizle
                </button>
              ) : null}
            </div>
          </div>

          <div className="material-lifecycle-pills" aria-label="Durum filtreleri">
            {MATERIAL_LIFECYCLE_FILTER_OPTIONS.map((option) => (
              <button
                key={option.value || "all"}
                type="button"
                className="material-lifecycle-pill"
                aria-pressed={lifecycleFilter === option.value}
                onClick={() => setLifecycleFilter(option.value)}
              >
                {option.label}
              </button>
            ))}
          </div>

          <div className="material-registry-table-wrap">
            {filteredMaterials.length === 0 ? (
              <div className="material-registry-empty">
                <Package aria-hidden="true" />
                <p>Kayıtlı malzeme bulunamadı.</p>
              </div>
            ) : (
              <table className={`material-registry-table${detailOpen ? " material-registry-table--compact" : ""}`}>
                <thead>
                  <tr>
                    <th>Malzeme</th>
                    {!detailOpen ? <th>Kod</th> : null}
                    <th>Polimer</th>
                    {!detailOpen ? <th>Kategori</th> : null}
                    {!detailOpen ? <th>Üretici / Grade</th> : null}
                    {!detailOpen ? <th>Renk</th> : null}
                    <th>Durum</th>
                    <th>Teknik Veri</th>
                    <th>İşlem</th>
                  </tr>
                </thead>
                <tbody>
                  {filteredMaterials.map((material) => {
                    const gradeText = manufacturerGradeText(material);
                    return (
                      <tr
                        key={material.id}
                        data-material-id={material.id}
                        data-selected={material.id === selectedId}
                        tabIndex={0}
                        onClick={() => selectMaterial(material)}
                        onKeyDown={(event) => {
                          if (event.key === "Enter" || event.key === " ") {
                            event.preventDefault();
                            selectMaterial(material);
                          }
                        }}
                      >
                        <td>
                          <div className="material-identity">
                            <span className="material-identity__name" title={material.name}>
                              {material.name}
                            </span>
                            {detailOpen ? (
                              <span className="material-identity__code" title={material.code}>
                                {material.code}
                              </span>
                            ) : null}
                          </div>
                        </td>
                        {!detailOpen ? (
                          <td>
                            <span className="material-cell-truncate" title={material.code}>
                              {material.code}
                            </span>
                          </td>
                        ) : null}
                        <td>{getPolymerFamilyLabel(material.polymerFamily)}</td>
                        {!detailOpen ? (
                          <td>{getMaterialCategoryLabel(material.materialCategory)}</td>
                        ) : null}
                        {!detailOpen ? (
                          <td>
                            <span className="material-cell-truncate" title={gradeText}>
                              {gradeText}
                            </span>
                          </td>
                        ) : null}
                        {!detailOpen ? (
                          <td>
                            <span className="material-cell-truncate" title={material.colorName ?? "—"}>
                              {material.colorName ?? "—"}
                            </span>
                          </td>
                        ) : null}
                        <td>
                          <span
                            className={lifecycleBadgeClass(material.lifecycleStatus)}
                            title={getMaterialLifecycleStatusLabel(material.lifecycleStatus)}
                          >
                            {getMaterialLifecycleStatusLabel(material.lifecycleStatus)}
                          </span>
                        </td>
                        <td>
                          <span className={technicalDataBadgeClass(material.hasTechnicalData)}>
                            {getTechnicalDataLabel(material.hasTechnicalData)}
                          </span>
                        </td>
                        <td className="material-registry-row-action">
                          <button
                            type="button"
                            className="material-registry-link-action"
                            onClick={(event) => {
                              event.stopPropagation();
                              selectMaterial(material);
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

        {detailOpen && selectedSummary ? (
          <aside className="material-registry-panel" aria-label="Malzeme detayı">
            <header
              ref={heroRef}
              className="material-detail-header"
              id="material-detail-focus-target"
              tabIndex={-1}
            >
              <div className="material-detail-header__context">
                <p className="material-detail-breadcrumb">Malzemeler / {selectedSummary.code}</p>
                <button
                  type="button"
                  className="material-detail-close"
                  aria-label="Malzeme detayını kapat"
                  title="Malzeme detayını kapat"
                  onClick={clearSelection}
                >
                  <X aria-hidden="true" size={18} strokeWidth={2.25} />
                </button>
              </div>
              <div className="material-detail-header__identity">
                <div className="material-detail-header__title-row">
                  <h2
                    id="material-detail-heading"
                    className="material-detail-header__name"
                    title={selectedSummary.name}
                  >
                    {selectedSummary.name}
                  </h2>
                  <span className={lifecycleBadgeClass(selectedSummary.lifecycleStatus)}>
                    {getMaterialLifecycleStatusLabel(selectedSummary.lifecycleStatus)}
                  </span>
                </div>
                <span className="material-detail-header__code" title={selectedSummary.code}>
                  {selectedSummary.code}
                </span>
              </div>

              <div className="material-detail-header__meta" aria-label="Malzeme özet bilgileri">
                <div className="material-detail-meta-item">
                  <span className="material-detail-meta-item__label">Polimer</span>
                  <strong className="material-detail-meta-item__value">
                    {getPolymerFamilyLabel(selectedSummary.polymerFamily)}
                  </strong>
                </div>
                <div className="material-detail-meta-item">
                  <span className="material-detail-meta-item__label">Kategori</span>
                  <strong className="material-detail-meta-item__value">
                    {getMaterialCategoryLabel(selectedSummary.materialCategory)}
                  </strong>
                </div>
                <div className="material-detail-meta-item">
                  <span className="material-detail-meta-item__label">Üretici / Grade</span>
                  <strong className="material-detail-meta-item__value" title={manufacturerGradeText(selectedSummary)}>
                    {manufacturerGradeText(selectedSummary)}
                  </strong>
                </div>
                <div className="material-detail-meta-item">
                  <span className="material-detail-meta-item__label">Son Güncelleme</span>
                  <strong className="material-detail-meta-item__value">
                    {formatMaterialDateTime(selectedSummary.updatedAtUtc)}
                  </strong>
                </div>
              </div>

              {detailCanManage ? (
                <div className="material-detail-header__actions">
                  <button
                    type="button"
                    className="material-registry-button compact"
                    onClick={() => setActionPanel(actionPanel === "edit" ? null : "edit")}
                  >
                    Düzenle
                  </button>
                  <button
                    type="button"
                    className="material-registry-button compact"
                    onClick={() => setActionPanel(actionPanel === "technical" ? null : "technical")}
                  >
                    Teknik Veriyi Güncelle
                  </button>
                  {transitionOptions.length > 0 ? (
                    <button
                      type="button"
                      className="material-registry-button compact"
                      onClick={() => setActionPanel(actionPanel === "transition" ? null : "transition")}
                    >
                      Durum Değiştir
                    </button>
                  ) : null}
                </div>
              ) : (
                <p className="material-readonly-note">Bu kayıt salt okunurdur. Düzenleme yetkiniz bulunmuyor.</p>
              )}
            </header>

            <div
              className={`material-sticky-context${stickyContextVisible ? " is-visible" : ""}`}
              data-material-sticky-context={stickyContextVisible ? "true" : "false"}
              aria-hidden={!stickyContextVisible}
              role={stickyContextVisible ? "region" : undefined}
              aria-label={stickyContextVisible ? "Seçili malzeme bağlamı" : undefined}
            >
              <div className="material-sticky-context__identity">
                <p className="material-sticky-context__name" title={selectedSummary.name}>
                  {selectedSummary.name}
                </p>
                <span className="material-sticky-context__code" title={selectedSummary.code}>
                  {selectedSummary.code}
                </span>
                <span className={lifecycleBadgeClass(selectedSummary.lifecycleStatus)}>
                  {getMaterialLifecycleStatusLabel(selectedSummary.lifecycleStatus)}
                </span>
                <span className="material-sticky-context__tab">{activeTabLabel}</span>
              </div>
              <button
                type="button"
                className="material-detail-close"
                aria-label="Malzeme detayını kapat"
                title="Malzeme detayını kapat"
                tabIndex={stickyContextVisible ? 0 : -1}
                onClick={clearSelection}
              >
                <X aria-hidden="true" size={18} strokeWidth={2.25} />
              </button>
            </div>

            {activeDetail ? (
              <section
                className="material-registry-kpis material-registry-kpis--compact"
                aria-label="Malzeme detay özeti"
              >
                <article className="material-executive__kpi material-executive__kpi--info">
                  <strong>{getTechnicalDataLabel(selectedSummary.hasTechnicalData)}</strong>
                  <span>Teknik Veri</span>
                </article>
                <article className="material-executive__kpi material-executive__kpi--neutral">
                  <strong>{getMaterialSourceLabel(activeDetail.sourceType)}</strong>
                  <span>Kaynak</span>
                </article>
                <article className="material-executive__kpi material-executive__kpi--neutral">
                  <strong>{formatMaterialDateTime(activeDetail.dataRecordedAtUtc)}</strong>
                  <span>Veri Yaşı</span>
                </article>
                <article className="material-executive__kpi material-executive__kpi--neutral">
                  <strong>{formatMaterialDateTime(activeDetail.updatedAtUtc)}</strong>
                  <span>Güncellik</span>
                </article>
              </section>
            ) : null}

            {!activeDetail && isPending ? (
              <div className="material-registry-status">
                <LoaderCircle aria-hidden="true" size={16} />
                Detay yükleniyor…
              </div>
            ) : null}

            {actionPanel === "edit" && activeDetail && detailCanManage ? (
              <section className="material-detail-action-panel" aria-label="Malzeme düzenleme">
                <h3 className="material-detail-action-panel__title">Malzeme bilgilerini düzenle</h3>
                <form
                  className="material-registry-form-grid"
                  action={(formData) => runAction(() => updateMaterialAction(formData))}
                >
                  <input type="hidden" name="materialId" value={activeDetail.id} />
                  <input type="hidden" name="expectedVersion" value={activeDetail.version} />
                  <label className="material-required">
                    Malzeme Adı
                    <input name="name" defaultValue={activeDetail.name} required />
                  </label>
                  <label>
                    Kategori
                    <select name="materialCategory" defaultValue={activeDetail.materialCategory}>
                      {MATERIAL_CATEGORY_FORM_OPTIONS.map((option) => (
                        <option key={option.value} value={option.value}>
                          {option.label}
                        </option>
                      ))}
                    </select>
                  </label>
                  <label>
                    Polimer
                    <select name="polymerFamily" defaultValue={activeDetail.polymerFamily}>
                      {MATERIAL_POLYMER_FORM_OPTIONS.map((option) => (
                        <option key={option.value} value={option.value}>
                          {option.label}
                        </option>
                      ))}
                    </select>
                  </label>
                  <label>
                    Üretici
                    <input name="manufacturer" defaultValue={activeDetail.manufacturer ?? ""} />
                  </label>
                  <label>
                    Grade / Ticari Ad
                    <input name="tradeNameOrGrade" defaultValue={activeDetail.tradeNameOrGrade ?? ""} />
                  </label>
                  <label>
                    Renk
                    <input name="colorName" defaultValue={activeDetail.colorName ?? ""} />
                  </label>
                  <label>
                    Fiziksel Form
                    <select name="physicalForm" defaultValue={activeDetail.physicalForm}>
                      {MATERIAL_PHYSICAL_FORM_OPTIONS.map((option) => (
                        <option key={option.value} value={option.value}>
                          {option.label}
                        </option>
                      ))}
                    </select>
                  </label>
                  <label className="full-width">
                    Açıklama
                    <textarea name="description" rows={2} defaultValue={activeDetail.description ?? ""} />
                  </label>
                  <div className="material-registry-actions full-width">
                    <button type="button" className="material-registry-button ghost" onClick={() => setActionPanel(null)}>
                      İptal
                    </button>
                    <button type="submit" className="material-registry-button primary">
                      Kaydet
                    </button>
                  </div>
                </form>
              </section>
            ) : null}

            {actionPanel === "technical" && activeDetail && detailCanManage ? (
              <section className="material-detail-action-panel" aria-label="Teknik veri güncelleme">
                <h3 className="material-detail-action-panel__title">Teknik veriyi güncelle</h3>
                <form
                  className="material-registry-form-grid"
                  action={(formData) => runAction(() => updateMaterialTechnicalAction(formData))}
                >
                  <input type="hidden" name="materialId" value={activeDetail.id} />
                  <input
                    type="hidden"
                    name="expectedVersion"
                    value={activeDetail.technicalSpecification?.version ?? activeDetail.version}
                  />
                  <label>
                    Yoğunluk (g/cm³)
                    <input
                      name="densityGPerCm3"
                      type="number"
                      step="0.001"
                      defaultValue={activeDetail.technicalSpecification?.densityGPerCm3 ?? ""}
                    />
                  </label>
                  <label>
                    Erime Akış İndeksi
                    <input
                      name="meltFlowRate"
                      type="number"
                      step="0.01"
                      defaultValue={activeDetail.technicalSpecification?.meltFlowRate ?? ""}
                    />
                  </label>
                  <label>
                    Erime Sıcaklığı Min (°C)
                    <input
                      name="meltTemperatureMinC"
                      type="number"
                      defaultValue={activeDetail.technicalSpecification?.meltTemperatureMinC ?? ""}
                    />
                  </label>
                  <label>
                    Erime Sıcaklığı Max (°C)
                    <input
                      name="meltTemperatureMaxC"
                      type="number"
                      defaultValue={activeDetail.technicalSpecification?.meltTemperatureMaxC ?? ""}
                    />
                  </label>
                  <label>
                    Kalıp Sıcaklığı Min (°C)
                    <input
                      name="moldTemperatureMinC"
                      type="number"
                      defaultValue={activeDetail.technicalSpecification?.moldTemperatureMinC ?? ""}
                    />
                  </label>
                  <label>
                    Kalıp Sıcaklığı Max (°C)
                    <input
                      name="moldTemperatureMaxC"
                      type="number"
                      defaultValue={activeDetail.technicalSpecification?.moldTemperatureMaxC ?? ""}
                    />
                  </label>
                  <label>
                    Kurutma Gerekli
                    <select
                      name="dryingRequired"
                      defaultValue={
                        activeDetail.technicalSpecification?.dryingRequired === true
                          ? "true"
                          : activeDetail.technicalSpecification?.dryingRequired === false
                            ? "false"
                            : ""
                      }
                    >
                      <option value="">Belirtilmedi</option>
                      <option value="true">Evet</option>
                      <option value="false">Hayır</option>
                    </select>
                  </label>
                  <label>
                    Spesifikasyon Kaynağı
                    <select
                      name="specificationSource"
                      defaultValue={activeDetail.technicalSpecification?.specificationSource ?? "manual"}
                    >
                      {MATERIAL_SPECIFICATION_SOURCE_FORM_OPTIONS.map((option) => (
                        <option key={option.value} value={option.value}>
                          {option.label}
                        </option>
                      ))}
                    </select>
                  </label>
                  <label className="full-width">
                    Teknik Notlar
                    <textarea
                      name="technicalNotes"
                      rows={2}
                      defaultValue={activeDetail.technicalSpecification?.technicalNotes ?? ""}
                    />
                  </label>
                  <div className="material-registry-actions full-width">
                    <button type="button" className="material-registry-button ghost" onClick={() => setActionPanel(null)}>
                      İptal
                    </button>
                    <button type="submit" className="material-registry-button primary">
                      Kaydet
                    </button>
                  </div>
                </form>
              </section>
            ) : null}

            {actionPanel === "transition" && activeDetail && detailCanManage ? (
              <section className="material-detail-action-panel" aria-label="Durum değiştirme">
                <h3 className="material-detail-action-panel__title">Durum değiştir</h3>
                <form
                  className="material-registry-form-grid"
                  action={(formData) => runAction(() => transitionMaterialLifecycleAction(formData))}
                >
                  <input type="hidden" name="materialId" value={activeDetail.id} />
                  <input type="hidden" name="expectedVersion" value={activeDetail.version} />
                  <input type="hidden" name="idempotencyKey" value={crypto.randomUUID()} />
                  <label className="material-required">
                    Hedef Durum
                    <select
                      name="targetStatus"
                      required
                      value={transitionTarget}
                      onChange={(event) => setTransitionTarget(event.target.value)}
                    >
                      <option value="">Seçiniz</option>
                      {transitionOptions.map((option) => (
                        <option key={option.value} value={option.value}>
                          {option.label}
                        </option>
                      ))}
                    </select>
                  </label>
                  <label className="material-required full-width">
                    Gerekçe
                    <textarea name="reason" rows={2} required placeholder="Durum değişikliği gerekçesi" />
                  </label>
                  <div className="material-registry-actions full-width">
                    <button type="button" className="material-registry-button ghost" onClick={() => setActionPanel(null)}>
                      İptal
                    </button>
                    <button type="submit" className="material-registry-button primary">
                      Durumu Güncelle
                    </button>
                  </div>
                </form>
              </section>
            ) : null}

            <div className="material-detail-tabs-rail">
              <div className="material-detail-tabs" role="tablist">
                {DETAIL_TABS.map(([value, label]) => (
                  <button
                    key={value}
                    type="button"
                    role="tab"
                    className="material-detail-tabs__button"
                    aria-selected={tab === value}
                    onClick={() => selectTab(value)}
                  >
                    {label}
                  </button>
                ))}
              </div>
            </div>

            {activeDetail ? (
              <>
                {tab === "overview" ? (
                  <div className="material-overview-grid" role="tabpanel" id="material-tab-panel-overview">
                    <article className="material-overview-card material-overview-card--wide">
                      <h3 className="material-overview-card__title">Genel Bilgiler</h3>
                      <dl className="material-registry-meta-grid">
                        <div>
                          <dt>Kategori</dt>
                          <dd>{getMaterialCategoryLabel(activeDetail.materialCategory)}</dd>
                        </div>
                        <div>
                          <dt>Polimer</dt>
                          <dd>{getPolymerFamilyLabel(activeDetail.polymerFamily)}</dd>
                        </div>
                        <div>
                          <dt>Üretici / Grade</dt>
                          <dd>{manufacturerGradeText(activeDetail)}</dd>
                        </div>
                        <div>
                          <dt>Renk</dt>
                          <dd>{activeDetail.colorName ?? "—"}</dd>
                        </div>
                        <div>
                          <dt>Fiziksel Form</dt>
                          <dd>{getPhysicalFormLabel(activeDetail.physicalForm)}</dd>
                        </div>
                        <div>
                          <dt>Ölçü Birimi</dt>
                          <dd>{activeDetail.unitOfMeasure}</dd>
                        </div>
                        <div>
                          <dt>Teknik Veri</dt>
                          <dd>
                            {getTechnicalDataLabel(
                              activeDetail.technicalSpecification?.hasTechnicalData ??
                                selectedSummary.hasTechnicalData,
                            )}
                          </dd>
                        </div>
                        <div>
                          <dt>Son Güncelleme</dt>
                          <dd>{formatMaterialDateTime(activeDetail.updatedAtUtc)}</dd>
                        </div>
                        {activeDetail.description ? (
                          <div className="full-width">
                            <dt>Açıklama</dt>
                            <dd>{activeDetail.description}</dd>
                          </div>
                        ) : null}
                      </dl>
                    </article>
                  </div>
                ) : null}

                {tab === "technical" ? (
                  <div className="material-overview-grid" role="tabpanel" id="material-tab-panel-technical">
                    <article className="material-overview-card material-overview-card--wide">
                      <h3 className="material-overview-card__title">Teknik Özellikler</h3>
                      {activeDetail.technicalSpecification?.hasTechnicalData ? (
                        <div className="material-tech-groups">
                          <section className="material-form-section">
                            <p className="material-form-section__label">Fiziksel Özellikler</p>
                            <dl className="material-registry-meta-grid">
                              <div>
                                <dt>Yoğunluk</dt>
                                <dd>
                                  {activeDetail.technicalSpecification.densityGPerCm3 !== undefined
                                    ? `${formatMaterialNumber(activeDetail.technicalSpecification.densityGPerCm3)} g/cm³`
                                    : "—"}
                                </dd>
                              </div>
                            </dl>
                          </section>
                          <section className="material-form-section">
                            <p className="material-form-section__label">Akış ve Nem</p>
                            <dl className="material-registry-meta-grid">
                              <div>
                                <dt>Erime Akış İndeksi</dt>
                                <dd>
                                  {activeDetail.technicalSpecification.meltFlowRate !== undefined
                                    ? `${formatMaterialNumber(activeDetail.technicalSpecification.meltFlowRate)}${
                                        activeDetail.technicalSpecification.meltFlowUnit
                                          ? ` ${activeDetail.technicalSpecification.meltFlowUnit}`
                                          : ""
                                      }`
                                    : "—"}
                                </dd>
                              </div>
                              <div>
                                <dt>Nem Limiti</dt>
                                <dd>
                                  {activeDetail.technicalSpecification.moistureLimitPercent !== undefined
                                    ? `${formatMaterialNumber(activeDetail.technicalSpecification.moistureLimitPercent)} %`
                                    : "—"}
                                </dd>
                              </div>
                            </dl>
                          </section>
                          <section className="material-form-section">
                            <p className="material-form-section__label">Kurutma</p>
                            <dl className="material-registry-meta-grid">
                              <div>
                                <dt>Kurutma Gerekli</dt>
                                <dd>
                                  {activeDetail.technicalSpecification.dryingRequired === true
                                    ? "Gerekli"
                                    : activeDetail.technicalSpecification.dryingRequired === false
                                      ? "Gerekli değil"
                                      : "—"}
                                </dd>
                              </div>
                              <div>
                                <dt>Kurutma Sıcaklığı</dt>
                                <dd>
                                  {formatMaterialRange(
                                    activeDetail.technicalSpecification.dryingTemperatureMinC,
                                    activeDetail.technicalSpecification.dryingTemperatureMaxC,
                                    "°C",
                                  )}
                                </dd>
                              </div>
                              <div>
                                <dt>Kurutma Süresi</dt>
                                <dd>
                                  {formatMaterialRange(
                                    activeDetail.technicalSpecification.dryingTimeMinHours,
                                    activeDetail.technicalSpecification.dryingTimeMaxHours,
                                    "sa",
                                  )}
                                </dd>
                              </div>
                            </dl>
                          </section>
                          <section className="material-form-section">
                            <p className="material-form-section__label">Eriyik ve Kalıp Sıcaklıkları</p>
                            <dl className="material-registry-meta-grid">
                              <div>
                                <dt>Erime Sıcaklığı</dt>
                                <dd>
                                  {formatMaterialRange(
                                    activeDetail.technicalSpecification.meltTemperatureMinC,
                                    activeDetail.technicalSpecification.meltTemperatureMaxC,
                                    "°C",
                                  )}
                                </dd>
                              </div>
                              <div>
                                <dt>Kalıp Sıcaklığı</dt>
                                <dd>
                                  {formatMaterialRange(
                                    activeDetail.technicalSpecification.moldTemperatureMinC,
                                    activeDetail.technicalSpecification.moldTemperatureMaxC,
                                    "°C",
                                  )}
                                </dd>
                              </div>
                            </dl>
                          </section>
                          <section className="material-form-section">
                            <p className="material-form-section__label">Depolama</p>
                            <dl className="material-registry-meta-grid">
                              <div>
                                <dt>Depolama Sıcaklığı</dt>
                                <dd>
                                  {formatMaterialRange(
                                    activeDetail.technicalSpecification.storageTemperatureMinC,
                                    activeDetail.technicalSpecification.storageTemperatureMaxC,
                                    "°C",
                                  )}
                                </dd>
                              </div>
                            </dl>
                          </section>
                          <section className="material-form-section">
                            <p className="material-form-section__label">Regrind Sınırı</p>
                            <dl className="material-registry-meta-grid">
                              <div>
                                <dt>Maksimum Regrind</dt>
                                <dd>
                                  {activeDetail.technicalSpecification.maximumRegrindPercent !== undefined
                                    ? `${formatMaterialNumber(activeDetail.technicalSpecification.maximumRegrindPercent)} %`
                                    : "—"}
                                </dd>
                              </div>
                            </dl>
                          </section>
                          <section className="material-form-section">
                            <p className="material-form-section__label">Teknik Dokümanlar ve Notlar</p>
                            <dl className="material-registry-meta-grid">
                              <div>
                                <dt>Spesifikasyon Kaynağı</dt>
                                <dd>
                                  {getSpecificationSourceLabel(activeDetail.technicalSpecification.specificationSource)}
                                </dd>
                              </div>
                              <div>
                                <dt>Kayıt Tarihi</dt>
                                <dd>
                                  {formatMaterialDateTime(
                                    activeDetail.technicalSpecification.specificationRecordedAtUtc,
                                  )}
                                </dd>
                              </div>
                              <div>
                                <dt>SDS Referansı</dt>
                                <dd>{activeDetail.technicalSpecification.safetyDataSheetReference ?? "—"}</dd>
                              </div>
                              <div>
                                <dt>TDS Referansı</dt>
                                <dd>{activeDetail.technicalSpecification.technicalDataSheetReference ?? "—"}</dd>
                              </div>
                              {activeDetail.technicalSpecification.technicalNotes ? (
                                <div className="full-width">
                                  <dt>Teknik Notlar</dt>
                                  <dd>{activeDetail.technicalSpecification.technicalNotes}</dd>
                                </div>
                              ) : null}
                            </dl>
                          </section>
                        </div>
                      ) : (
                        <p className="material-form-hint">Bu malzeme için teknik veri kaydı bulunmuyor.</p>
                      )}
                    </article>
                  </div>
                ) : null}

                {tab === "source" ? (
                  <div className="material-overview-grid" role="tabpanel" id="material-tab-panel-source">
                    <article className="material-overview-card material-overview-card--wide">
                      <h3 className="material-overview-card__title">Kaynak ve ERP Referansı</h3>
                      <p className="material-form-hint">
                        ERP kaynaklı kayıtlarda sistem-of-record ERP&apos;dir; bu ekran FactOS malzeme kaydı
                        bağlamını gösterir.
                      </p>
                      <dl className="material-registry-meta-grid">
                        <div>
                          <dt>Kaynak</dt>
                          <dd>{getMaterialSourceLabel(activeDetail.sourceType)}</dd>
                        </div>
                        <div>
                          <dt>Kaynak Sistemi</dt>
                          <dd>{activeDetail.sourceSystem ?? "—"}</dd>
                        </div>
                        <div>
                          <dt>Kaynak Referansı</dt>
                          <dd>{activeDetail.sourceReference ?? "—"}</dd>
                        </div>
                        <div>
                          <dt>ERP Malzeme Kodu</dt>
                          <dd>{activeDetail.externalMaterialCode ?? "—"}</dd>
                        </div>
                        <div>
                          <dt>Veri Kayıt Tarihi</dt>
                          <dd>{formatMaterialDateTime(activeDetail.dataRecordedAtUtc)}</dd>
                        </div>
                        <div>
                          <dt>Oluşturulma</dt>
                          <dd>{formatMaterialDateTime(activeDetail.createdAtUtc)}</dd>
                        </div>
                      </dl>
                    </article>
                  </div>
                ) : null}

                {tab === "history" ? (
                  <div role="tabpanel" id="material-tab-panel-history">
                    <h3 className="material-overview-card__title">Durum Geçmişi</h3>
                    {history.length === 0 ? (
                      <p className="material-form-hint">Durum geçmişi kaydı bulunmuyor.</p>
                    ) : (
                      <div className="material-registry-history">
                        {history.map((event) => (
                          <article key={event.id} className="material-registry-history-item">
                            <strong>
                              {getMaterialLifecycleStatusLabel(event.previousStatus)} →{" "}
                              {getMaterialLifecycleStatusLabel(event.newStatus)}
                            </strong>
                            <p>{event.reason}</p>
                            <small>
                              {event.actorDisplayName} · {formatMaterialDateTime(event.occurredAtUtc)}
                            </small>
                          </article>
                        ))}
                      </div>
                    )}
                  </div>
                ) : null}

                {tab === "lots" && activeDetail ? (
                  <div id="material-tab-panel-lots" role="tabpanel">
                    <MaterialLotPanel
                      materialId={activeDetail.id}
                      materialCode={activeDetail.code}
                      materialName={activeDetail.name}
                      unitOfMeasure={activeDetail.unitOfMeasure}
                      canManage={detailCanManage}
                    />
                  </div>
                ) : null}

                {tab === "traceability" && activeDetail ? (
                  <div id="material-tab-panel-traceability" role="tabpanel">
                    <MaterialLotPanel
                      materialId={activeDetail.id}
                      materialCode={activeDetail.code}
                      materialName={activeDetail.name}
                      unitOfMeasure={activeDetail.unitOfMeasure}
                      canManage={detailCanManage}
                      traceabilityMode
                    />
                  </div>
                ) : null}

                {tab === "requirement" && activeDetail ? (
                  <div id="material-tab-panel-requirement" role="tabpanel">
                    <MaterialRequirementPanel
                      materialId={activeDetail.id}
                      materialCode={activeDetail.code}
                      materialName={activeDetail.name}
                      unitOfMeasure={activeDetail.unitOfMeasure}
                      canManage={detailCanManage}
                      section="consumption"
                    />
                  </div>
                ) : null}

                {tab === "regrind-policy" && activeDetail ? (
                  <div id="material-tab-panel-regrind-policy" role="tabpanel">
                    <MaterialRequirementPanel
                      materialId={activeDetail.id}
                      materialCode={activeDetail.code}
                      materialName={activeDetail.name}
                      unitOfMeasure={activeDetail.unitOfMeasure}
                      canManage={detailCanManage}
                      section="regrind-policy"
                    />
                  </div>
                ) : null}
              </>
            ) : !isPending ? (
              <div className="material-registry-error" role="alert">
                Malzeme detayı yüklenemedi.
              </div>
            ) : null}
          </aside>
        ) : null}
      </div>
    </main>
  );
}
