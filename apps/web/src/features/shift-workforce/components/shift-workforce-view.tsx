"use client";

import { useCallback, useEffect, useMemo, useState, useTransition } from "react";
import { LoaderCircle, Search, Users, X } from "lucide-react";

import {
  formatShiftWorkforceCount,
  formatShiftWorkforceDateTime,
  getEmploymentStatusLabel,
  employmentBadgeClass,
  availabilityBadgeClass,
  freshnessBadgeClass,
  verificationBadgeClass,
  LOCAL_PILOT_DISCLAIMER,
  SHIFT_WORKFORCE_ACTIVE_FILTER_OPTIONS,
  SHIFT_WORKFORCE_AVAILABILITY_FILTER_OPTIONS,
  SHIFT_WORKFORCE_COMPETENCY_LEVEL_OPTIONS,
  SHIFT_WORKFORCE_FRESHNESS_FILTER_OPTIONS,
  SHIFT_WORKFORCE_VERIFICATION_STATUS_OPTIONS,
} from "../domain/shift-workforce-labels";
import type {
  CreateShiftWorkforceCompetencyCommand,
  ShiftWorkforceCompetencyAssignment,
  ShiftWorkforceCompetencyDefinition,
  ShiftWorkforceOverview,
  ShiftWorkforcePersonDetail,
  ShiftWorkforcePersonListResult,
  ShiftWorkforcePersonSummary,
  UpdateShiftWorkforceCompetencyCommand,
} from "../domain/shift-workforce-types";
import {
  createShiftWorkforceCompetencyAction,
  loadShiftWorkforcePersonDetailAction,
  loadShiftWorkforcePersonsAction,
  synchronizeShiftWorkforceAction,
  updateShiftWorkforceCompetencyAction,
} from "../server/shift-workforce-actions";
import "./shift-workforce.css";

type ShiftWorkforceViewProps = Readonly<{
  overview: ShiftWorkforceOverview;
  persons: ShiftWorkforcePersonListResult;
  competencyDefinitions: readonly ShiftWorkforceCompetencyDefinition[];
  canManage: boolean;
}>;

type CompetencyModalState =
  | Readonly<{ mode: "create" }>
  | Readonly<{ mode: "edit"; assignment: ShiftWorkforceCompetencyAssignment }>
  | null;

const PAGE_SIZE = 20;

const WORKSPACE_TABS = [
  { id: "personnel", label: "Personel ve Yetkinlikler" },
] as const;

function competencyScopeText(assignment: ShiftWorkforceCompetencyAssignment): string {
  const parts = [
    assignment.machineFamilyScope,
    assignment.processScope,
    assignment.materialScope,
    assignment.equipmentScope,
  ].filter(Boolean);
  return parts.length > 0 ? parts.join(" · ") : "Genel kapsam";
}

export function ShiftWorkforceView({
  overview,
  persons: initialPersons,
  competencyDefinitions,
  canManage,
}: ShiftWorkforceViewProps) {
  const [persons, setPersons] = useState(initialPersons);
  const [search, setSearch] = useState("");
  const [activeFilter, setActiveFilter] = useState("");
  const [availabilityFilter, setAvailabilityFilter] = useState("");
  const [freshnessFilter, setFreshnessFilter] = useState("");
  const [competencyFilter, setCompetencyFilter] = useState("");
  const [page, setPage] = useState(1);
  const [selectedId, setSelectedId] = useState("");
  const [detail, setDetail] = useState<ShiftWorkforcePersonDetail | undefined>();
  const [detailCanManage, setDetailCanManage] = useState(canManage);
  const [competencyModal, setCompetencyModal] = useState<CompetencyModalState>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [isPending, startTransition] = useTransition();

  const filtersActive = Boolean(
    search.trim() || activeFilter || availabilityFilter || freshnessFilter || competencyFilter,
  );

  const competencyFilterOptions = useMemo(
    () => [
      { value: "", label: "Tüm yetkinlikler" },
      ...competencyDefinitions
        .filter((item) => item.isActive)
        .sort((a, b) => a.sortOrder - b.sortOrder)
        .map((item) => ({ value: item.code, label: item.displayLabelTr })),
    ],
    [competencyDefinitions],
  );

  const selectedSummary =
    persons.items.find((item) => item.id === selectedId) ??
    initialPersons.items.find((item) => item.id === selectedId);
  const activeDetail = detail && detail.id === selectedId ? detail : undefined;
  const detailOpen = Boolean(selectedSummary);

  const totalPages = Math.max(1, Math.ceil(persons.totalCount / PAGE_SIZE));

  const reloadPersons = useCallback(() => {
    startTransition(async () => {
      setError(null);
      const result = await loadShiftWorkforcePersonsAction({
        search: search.trim() || undefined,
        isActive: activeFilter || undefined,
        availability: availabilityFilter || undefined,
        freshness: freshnessFilter || undefined,
        competencyCode: competencyFilter || undefined,
        page,
        pageSize: PAGE_SIZE,
      });

      if (result.status === "failure") {
        setError(result.message);
        return;
      }

      setPersons(result.persons);
      setSelectedId((currentId) => {
        if (currentId && !result.persons.items.some((item) => item.id === currentId)) {
          setDetail(undefined);
          return "";
        }
        return currentId;
      });
    });
  }, [activeFilter, availabilityFilter, competencyFilter, freshnessFilter, page, search]);

  useEffect(() => {
    reloadPersons();
  }, [reloadPersons]);

  function resetFilters() {
    setSearch("");
    setActiveFilter("");
    setAvailabilityFilter("");
    setFreshnessFilter("");
    setCompetencyFilter("");
    setPage(1);
  }

  function clearSelection() {
    setSelectedId("");
    setDetail(undefined);
    setCompetencyModal(null);
    setMessage(null);
    setError(null);
  }

  function selectPerson(item: ShiftWorkforcePersonSummary) {
    setSelectedId(item.id);
    setDetail(undefined);
    setCompetencyModal(null);
    setMessage(null);
    setError(null);

    startTransition(async () => {
      const result = await loadShiftWorkforcePersonDetailAction(item.id);
      if (result.status === "success") {
        setDetail(result.person);
        setDetailCanManage(result.canManage);
      } else {
        setError(result.message);
      }
    });
  }

  function runSynchronize() {
    startTransition(async () => {
      setMessage(null);
      setError(null);
      const result = await synchronizeShiftWorkforceAction();
      if (result.status === "failure") {
        setError(result.message);
        return;
      }

      setMessage("Kaynak senkronizasyonu tamamlandı.");
      reloadPersons();
      if (selectedId) {
        const refreshed = await loadShiftWorkforcePersonDetailAction(selectedId);
        if (refreshed.status === "success") {
          setDetail(refreshed.person);
        }
      }
    });
  }

  function handleCreateCompetency(formData: FormData) {
    if (!selectedId) {
      return;
    }

    const command: CreateShiftWorkforceCompetencyCommand = {
      competencyDefinitionCode: String(formData.get("competencyDefinitionCode") ?? ""),
      machineFamilyScope: String(formData.get("machineFamilyScope") ?? "") || undefined,
      processScope: String(formData.get("processScope") ?? "") || undefined,
      materialScope: String(formData.get("materialScope") ?? "") || undefined,
      equipmentScope: String(formData.get("equipmentScope") ?? "") || undefined,
      competencyLevel: String(formData.get("competencyLevel") ?? "competent"),
      verificationStatus: String(formData.get("verificationStatus") ?? "pending-verification"),
      validFromUtc: String(formData.get("validFromUtc") ?? "") || undefined,
      validUntilUtc: String(formData.get("validUntilUtc") ?? "") || undefined,
      operationalNote: String(formData.get("operationalNote") ?? "") || undefined,
      idempotencyKey: crypto.randomUUID(),
    };

    startTransition(async () => {
      setMessage(null);
      setError(null);
      const result = await createShiftWorkforceCompetencyAction(selectedId, command);
      if (result.status === "failure") {
        setError(result.message);
        return;
      }

      setCompetencyModal(null);
      setMessage("Yetkinlik kaydı oluşturuldu.");
      const refreshed = await loadShiftWorkforcePersonDetailAction(selectedId);
      if (refreshed.status === "success") {
        setDetail(refreshed.person);
      }
      reloadPersons();
    });
  }

  function handleUpdateCompetency(formData: FormData) {
    if (!selectedId || competencyModal?.mode !== "edit") {
      return;
    }

    const command: UpdateShiftWorkforceCompetencyCommand = {
      competencyLevel: String(formData.get("competencyLevel") ?? "") || undefined,
      verificationStatus: String(formData.get("verificationStatus") ?? "") || undefined,
      validFromUtc: String(formData.get("validFromUtc") ?? "") || undefined,
      validUntilUtc: String(formData.get("validUntilUtc") ?? "") || undefined,
      operationalNote: String(formData.get("operationalNote") ?? "") || undefined,
      expectedVersion: competencyModal.assignment.version,
    };

    startTransition(async () => {
      setMessage(null);
      setError(null);
      const result = await updateShiftWorkforceCompetencyAction(
        selectedId,
        competencyModal.assignment.id,
        command,
      );
      if (result.status === "failure") {
        setError(result.message);
        return;
      }

      setCompetencyModal(null);
      setMessage("Yetkinlik kaydı güncellendi.");
      const refreshed = await loadShiftWorkforcePersonDetailAction(selectedId);
      if (refreshed.status === "success") {
        setDetail(refreshed.person);
      }
      reloadPersons();
    });
  }

  function renderCompetencyModal() {
    if (!competencyModal) {
      return null;
    }

    const isEdit = competencyModal.mode === "edit";
    const assignment = isEdit ? competencyModal.assignment : undefined;

    return (
      <div className="shift-workforce-modal-backdrop" role="presentation">
        <div
          className="shift-workforce-modal"
          role="dialog"
          aria-modal="true"
          aria-labelledby="shift-workforce-competency-modal-title"
        >
          <h3 id="shift-workforce-competency-modal-title">
            {isEdit ? "Yetkinliği Güncelle" : "Yetkinlik Ekle"}
          </h3>
          <p className="shift-workforce-empty-inline">
            ERP/HR alanları salt okunurdur; yalnızca operasyonel yetkinlik zenginleştirmesi yapılabilir.
          </p>
          <form
            className="shift-workforce-form-grid"
            onSubmit={(event) => {
              event.preventDefault();
              const formData = new FormData(event.currentTarget);
              if (isEdit) {
                handleUpdateCompetency(formData);
              } else {
                handleCreateCompetency(formData);
              }
            }}
          >
            {!isEdit ? (
              <label>
                Yetkinlik tanımı
                <select name="competencyDefinitionCode" required defaultValue="">
                  <option value="" disabled>
                    Seçin
                  </option>
                  {competencyDefinitions
                    .filter((item) => item.isActive)
                    .map((item) => (
                      <option key={item.code} value={item.code}>
                        {item.displayLabelTr}
                      </option>
                    ))}
                </select>
              </label>
            ) : null}
            <label>
              Yetkinlik seviyesi
              <select
                name="competencyLevel"
                defaultValue={assignment?.competencyLevel ?? "competent"}
              >
                {SHIFT_WORKFORCE_COMPETENCY_LEVEL_OPTIONS.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>
            <label>
              Doğrulama durumu
              <select
                name="verificationStatus"
                defaultValue={assignment?.verificationStatus ?? "pending-verification"}
              >
                {SHIFT_WORKFORCE_VERIFICATION_STATUS_OPTIONS.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </label>
            {!isEdit ? (
              <>
                <label>
                  Makine ailesi kapsamı
                  <input name="machineFamilyScope" type="text" placeholder="İsteğe bağlı" />
                </label>
                <label>
                  Proses kapsamı
                  <input name="processScope" type="text" placeholder="İsteğe bağlı" />
                </label>
              </>
            ) : null}
            <label>
              Geçerlilik başlangıcı
              <input
                name="validFromUtc"
                type="datetime-local"
                defaultValue={
                  assignment?.validFromUtc
                    ? new Date(assignment.validFromUtc).toISOString().slice(0, 16)
                    : ""
                }
              />
            </label>
            <label>
              Geçerlilik bitişi
              <input
                name="validUntilUtc"
                type="datetime-local"
                defaultValue={
                  assignment?.validUntilUtc
                    ? new Date(assignment.validUntilUtc).toISOString().slice(0, 16)
                    : ""
                }
              />
            </label>
            <label>
              Operasyonel not
              <textarea
                name="operationalNote"
                rows={3}
                defaultValue={assignment?.operationalNote ?? ""}
              />
            </label>
            <div className="shift-workforce-form-actions">
              <button
                type="button"
                className="shift-workforce-btn"
                onClick={() => setCompetencyModal(null)}
              >
                İptal
              </button>
              <button
                type="submit"
                className="shift-workforce-btn shift-workforce-btn--primary"
                disabled={isPending}
              >
                {isPending ? <LoaderCircle aria-hidden="true" className="animate-spin" size={14} /> : null}
                {isEdit ? "Güncelle" : "Kaydet"}
              </button>
            </div>
          </form>
        </div>
      </div>
    );
  }

  function renderDetailPanel() {
    if (!selectedSummary) {
      return null;
    }

    const detailItem = activeDetail;
    const showStaleWarning = selectedSummary.freshnessState === "stale";
    const showInactiveWarning = !selectedSummary.isActive;
    const showUnknownAvailability = selectedSummary.availabilityState === "unknown";

    return (
      <aside className="shift-workforce-detail-workspace" aria-label="Personel detayı">
        <header className="shift-workforce-detail-header">
          <div className="shift-workforce-detail-header__row">
            <div className="shift-workforce-detail-header__identity">
              <p className="shift-workforce-detail-breadcrumb">Vardiyalar · Personel ve Yetkinlikler</p>
              <h2>{selectedSummary.displayName}</h2>
              <p className="shift-workforce-detail-header__meta">
                {selectedSummary.employeeCode}
                {selectedSummary.jobTitle ? ` · ${selectedSummary.jobTitle}` : ""}
              </p>
              <div className="shift-workforce-detail-header__chips">
                <span className={employmentBadgeClass(selectedSummary.isActive)}>
                  {getEmploymentStatusLabel(selectedSummary.isActive)}
                </span>
                <span className={availabilityBadgeClass(selectedSummary.availabilityState)}>
                  {selectedSummary.availabilityLabel}
                </span>
                <span className={freshnessBadgeClass(selectedSummary.freshnessState)}>
                  {selectedSummary.freshnessLabel}
                </span>
              </div>
            </div>
            <button
              type="button"
              className="shift-workforce-detail-close"
              aria-label="Personel detayını kapat"
              onClick={clearSelection}
            >
              <X aria-hidden="true" size={16} />
            </button>
          </div>
        </header>

        {detailItem?.isPilotData ? (
          <div className="shift-workforce-pilot-banner" role="status">
            {LOCAL_PILOT_DISCLAIMER}
          </div>
        ) : null}

        {showStaleWarning ? (
          <div className="shift-workforce-alert shift-workforce-alert--warning" role="status">
            <div>
              <h3>Bayat kaynak verisi</h3>
              <p>Personel bilgileri güncel değil; ERP/HR senkronizasyonu önerilir.</p>
            </div>
          </div>
        ) : null}

        {showInactiveWarning ? (
          <div className="shift-workforce-alert shift-workforce-alert--warning" role="status">
            <div>
              <h3>Pasif personel</h3>
              <p>Bu personel operasyonel uygunluk için kullanılamaz.</p>
            </div>
          </div>
        ) : null}

        {showUnknownAvailability ? (
          <div className="shift-workforce-alert shift-workforce-alert--info" role="status">
            <div>
              <h3>Müsaitlik bilinmiyor</h3>
              <p>Kaynak sistem müsaitlik durumu sağlamadı; varsayılan değer üretilmedi.</p>
            </div>
          </div>
        ) : null}

        {isPending && !detailItem ? (
          <div className="shift-workforce-loading">
            <LoaderCircle aria-hidden="true" className="animate-spin" size={16} />
            Detay yükleniyor…
          </div>
        ) : detailItem ? (
          <>
            <section className="shift-workforce-detail-section" aria-label="ERP/HR bilgileri">
              <h3>ERP/HR bilgileri (salt okunur)</h3>
              <dl className="shift-workforce-detail-grid shift-workforce-detail-grid--readonly">
                <div>
                  <dt>Personel kodu</dt>
                  <dd>{detailItem.employeeCode}</dd>
                </div>
                <div>
                  <dt>Ad soyad</dt>
                  <dd>{detailItem.displayName}</dd>
                </div>
                <div>
                  <dt>Departman</dt>
                  <dd>{detailItem.department ?? "—"}</dd>
                </div>
                <div>
                  <dt>Ünvan</dt>
                  <dd>{detailItem.jobTitle ?? "—"}</dd>
                </div>
                <div>
                  <dt>Vardiya referansı</dt>
                  <dd>{detailItem.sourceShiftReference ?? detailItem.shiftLabel ?? "—"}</dd>
                </div>
                <div>
                  <dt>Takvim referansı</dt>
                  <dd>{detailItem.sourceCalendarReference ?? "—"}</dd>
                </div>
              </dl>
            </section>

            <section className="shift-workforce-detail-section" aria-label="Kaynak ve tazelik">
              <h3>Kaynak ve tazelik</h3>
              <dl className="shift-workforce-detail-grid">
                <div>
                  <dt>Kaynak</dt>
                  <dd>{detailItem.sourceLabel}</dd>
                </div>
                <div>
                  <dt>Kaynak güncelleme</dt>
                  <dd>{formatShiftWorkforceDateTime(detailItem.sourceUpdatedAtUtc)}</dd>
                </div>
                <div>
                  <dt>Senkronizasyon</dt>
                  <dd>{formatShiftWorkforceDateTime(detailItem.synchronizedAtUtc)}</dd>
                </div>
                <div>
                  <dt>Tazelik</dt>
                  <dd>
                    <span className={freshnessBadgeClass(detailItem.freshnessState)}>
                      {detailItem.freshnessLabel}
                    </span>
                  </dd>
                </div>
              </dl>
              {detailItem.sourceLimitations.length > 0 ? (
                <ul className="shift-workforce-limitations">
                  {detailItem.sourceLimitations.map((limitation) => (
                    <li key={limitation}>{limitation}</li>
                  ))}
                </ul>
              ) : null}
            </section>

            <section className="shift-workforce-detail-section" aria-label="Operasyonel yetkinlikler">
              <div className="shift-workforce-registry__header">
                <h3>Operasyonel yetkinlikler</h3>
                {detailCanManage ? (
                  <button
                    type="button"
                    className="shift-workforce-btn shift-workforce-btn--sm shift-workforce-btn--primary"
                    onClick={() => setCompetencyModal({ mode: "create" })}
                  >
                    Yetkinlik Ekle
                  </button>
                ) : null}
              </div>

              {detailItem.competencies.length === 0 ? (
                <p className="shift-workforce-empty-inline">Kayıtlı yetkinlik bulunmuyor.</p>
              ) : (
                <ul className="shift-workforce-competency-list">
                  {detailItem.competencies.map((assignment) => (
                    <li
                      key={assignment.id}
                      className="shift-workforce-competency-item"
                      data-attention={assignment.requiresAttention ? "true" : "false"}
                    >
                      <div className="shift-workforce-competency-item__title">
                        {assignment.competencyLabel}
                      </div>
                      <div className="shift-workforce-competency-item__meta">
                        {assignment.competencyLevelLabel}
                        {" · "}
                        <span className={verificationBadgeClass(assignment.verificationStatus)}>
                          {assignment.verificationStatusLabel}
                        </span>
                        {" · "}
                        {competencyScopeText(assignment)}
                        {" · Geçerlilik: "}
                        {formatShiftWorkforceDateTime(assignment.validFromUtc)}
                        {" – "}
                        {formatShiftWorkforceDateTime(assignment.validUntilUtc)}
                        {assignment.operationalNote ? ` · Not: ${assignment.operationalNote}` : ""}
                      </div>
                      {detailCanManage ? (
                        <button
                          type="button"
                          className="shift-workforce-btn shift-workforce-btn--sm"
                          style={{ marginTop: "0.35rem" }}
                          onClick={() => setCompetencyModal({ mode: "edit", assignment })}
                        >
                          Güncelle
                        </button>
                      ) : null}
                    </li>
                  ))}
                </ul>
              )}
            </section>
          </>
        ) : (
          <div className="shift-workforce-empty">
            <p>Detay yüklenemedi.</p>
          </div>
        )}
      </aside>
    );
  }

  return (
    <div className="shift-workforce-page">
      <section className="shift-workforce-executive" aria-label="Vardiya personel komuta yüzeyi">
        <div className="shift-workforce-executive__top">
          <div className="shift-workforce-executive__intro">
            <p className="shift-workforce-executive__eyebrow">VARDİYA YÖNETİMİ</p>
            <h1>Vardiyalar</h1>
            <p className="shift-workforce-executive__subtitle">
              ERP/HR kaynaklı personel verilerini ve CrevaFactOS operasyonel yetkinlik zenginleştirmesini
              vardiya planlama bağlamında izleyin.
            </p>
          </div>
          {canManage ? (
            <div className="shift-workforce-executive__actions">
              <button
                type="button"
                className="shift-workforce-btn shift-workforce-btn--primary"
                disabled={isPending}
                onClick={runSynchronize}
              >
                {isPending ? <LoaderCircle aria-hidden="true" className="animate-spin" size={16} /> : null}
                Kaynağı Senkronize Et
              </button>
            </div>
          ) : null}
        </div>

        <div className="shift-workforce-workspace-tabs" role="tablist" aria-label="Vardiya çalışma alanı">
          {WORKSPACE_TABS.map((tab) => (
            <span
              key={tab.id}
              role="tab"
              aria-selected="true"
              className="shift-workforce-workspace-tab shift-workforce-workspace-tab--active"
            >
              {tab.label}
            </span>
          ))}
        </div>

        <div className="shift-workforce-executive__kpis" aria-label="Personel göstergeleri">
          <article className="shift-workforce-kpi">
            <span className="shift-workforce-kpi__label">Toplam Personel</span>
            <strong className="shift-workforce-kpi__value">
              {formatShiftWorkforceCount(overview.totalWorkforce)}
            </strong>
          </article>
          <article className="shift-workforce-kpi">
            <span className="shift-workforce-kpi__label">Aktif</span>
            <strong className="shift-workforce-kpi__value">
              {formatShiftWorkforceCount(overview.activeWorkforce)}
            </strong>
          </article>
          <article className="shift-workforce-kpi">
            <span className="shift-workforce-kpi__label">Müsait</span>
            <strong className="shift-workforce-kpi__value">
              {formatShiftWorkforceCount(overview.currentlyAvailable)}
            </strong>
          </article>
          <article className="shift-workforce-kpi">
            <span className="shift-workforce-kpi__label">İzinli / Müsait Değil</span>
            <strong className="shift-workforce-kpi__value">
              {formatShiftWorkforceCount(overview.unavailableOrLeave)}
            </strong>
          </article>
          <article className="shift-workforce-kpi">
            <span className="shift-workforce-kpi__label">Bayat Kayıt</span>
            <strong className="shift-workforce-kpi__value">
              {formatShiftWorkforceCount(overview.staleSourceRecords)}
            </strong>
          </article>
          <article className="shift-workforce-kpi">
            <span className="shift-workforce-kpi__label">Yetkinlik Dikkati</span>
            <strong className="shift-workforce-kpi__value">
              {formatShiftWorkforceCount(overview.competencyAttentionCount)}
            </strong>
          </article>
        </div>
      </section>

      {!canManage ? (
        <div className="shift-workforce-alert shift-workforce-alert--info" role="status">
          <div>
            <h3>Salt okunur erişim</h3>
            <p>Bu modülde yalnızca görüntüleme yetkiniz var; yetkinlik zenginleştirmesi yapılamaz.</p>
          </div>
        </div>
      ) : null}

      {message ? <div className="shift-workforce-message" role="status">{message}</div> : null}
      {error ? (
        <div className="shift-workforce-alert shift-workforce-alert--error" role="alert">
          <div>
            <h3>İşlem hatası</h3>
            <p>{error}</p>
          </div>
        </div>
      ) : null}

      <div
        className={`shift-workforce-workspace${detailOpen ? " shift-workforce-workspace--detail-open" : ""}`}
      >
        <section className="shift-workforce-registry" aria-label="Personel kaydı">
          <div className="shift-workforce-registry__header">
            <h2 className="shift-workforce-registry__title">Personel Kaydı</h2>
            <span className="shift-workforce-registry__count">
              {isPending ? "Yükleniyor…" : `${persons.totalCount} sonuç`}
            </span>
          </div>

          <div className="shift-workforce-registry__toolbar">
            <div className="shift-workforce-registry__search-row">
              <div className="shift-workforce-search">
                <Search aria-hidden="true" className="shift-workforce-search__icon" size={14} />
                <input
                  id="shift-workforce-search"
                  type="search"
                  placeholder="Personel kodu veya ad ara"
                  value={search}
                  onChange={(event) => {
                    setSearch(event.target.value);
                    setPage(1);
                  }}
                />
              </div>
              {filtersActive ? (
                <button
                  type="button"
                  className="shift-workforce-btn shift-workforce-btn--ghost"
                  onClick={resetFilters}
                >
                  <X aria-hidden="true" size={14} />
                  Temizle
                </button>
              ) : null}
            </div>
            <div className="shift-workforce-registry__filters">
              <div className="shift-workforce-filter">
                <label htmlFor="shift-workforce-active">Durum</label>
                <select
                  id="shift-workforce-active"
                  value={activeFilter}
                  onChange={(event) => {
                    setActiveFilter(event.target.value);
                    setPage(1);
                  }}
                >
                  {SHIFT_WORKFORCE_ACTIVE_FILTER_OPTIONS.map((option) => (
                    <option key={option.value || "all"} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
              </div>
              <div className="shift-workforce-filter">
                <label htmlFor="shift-workforce-availability">Müsaitlik</label>
                <select
                  id="shift-workforce-availability"
                  value={availabilityFilter}
                  onChange={(event) => {
                    setAvailabilityFilter(event.target.value);
                    setPage(1);
                  }}
                >
                  {SHIFT_WORKFORCE_AVAILABILITY_FILTER_OPTIONS.map((option) => (
                    <option key={option.value || "all"} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
              </div>
              <div className="shift-workforce-filter">
                <label htmlFor="shift-workforce-freshness">Tazelik</label>
                <select
                  id="shift-workforce-freshness"
                  value={freshnessFilter}
                  onChange={(event) => {
                    setFreshnessFilter(event.target.value);
                    setPage(1);
                  }}
                >
                  {SHIFT_WORKFORCE_FRESHNESS_FILTER_OPTIONS.map((option) => (
                    <option key={option.value || "all"} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
              </div>
              <div className="shift-workforce-filter">
                <label htmlFor="shift-workforce-competency">Yetkinlik</label>
                <select
                  id="shift-workforce-competency"
                  value={competencyFilter}
                  onChange={(event) => {
                    setCompetencyFilter(event.target.value);
                    setPage(1);
                  }}
                >
                  {competencyFilterOptions.map((option) => (
                    <option key={option.value || "all"} value={option.value}>
                      {option.label}
                    </option>
                  ))}
                </select>
              </div>
            </div>
          </div>

          <div className="shift-workforce-registry__table-wrap">
            {persons.items.length === 0 && !isPending ? (
              <div className="shift-workforce-empty">
                <Users aria-hidden="true" size={28} />
                <h3>Personel bulunamadı</h3>
                <p>
                  {filtersActive
                    ? "Seçili filtrelere uygun kayıt yok. Filtreleri genişletmeyi deneyin."
                    : "Henüz personel kaydı bulunmuyor."}
                </p>
              </div>
            ) : (
              <table className="shift-workforce-table">
                <thead>
                  <tr>
                    <th>Personel</th>
                    <th>Rol</th>
                    <th>Vardiya</th>
                    <th>Kaynak</th>
                    <th>Tazelik</th>
                    <th>Yetkinlik</th>
                    <th>İşlem</th>
                  </tr>
                </thead>
                <tbody>
                  {persons.items.map((item) => (
                    <tr
                      key={item.id}
                      data-person-id={item.id}
                      data-selected={item.id === selectedId ? "true" : "false"}
                      tabIndex={0}
                      onClick={() => selectPerson(item)}
                      onKeyDown={(event) => {
                        if (event.key === "Enter" || event.key === " ") {
                          event.preventDefault();
                          selectPerson(item);
                        }
                      }}
                    >
                      <td>
                        <span className="shift-workforce-cell-primary">{item.displayName}</span>
                        <span className="shift-workforce-cell-secondary">{item.employeeCode}</span>
                      </td>
                      <td>
                        <span className="shift-workforce-cell-primary">
                          {item.jobTitle ?? "—"}
                        </span>
                        <span className="shift-workforce-cell-secondary">
                          {getEmploymentStatusLabel(item.isActive)}
                          {" · "}
                          {item.availabilityLabel}
                        </span>
                      </td>
                      <td>{item.shiftLabel ?? "—"}</td>
                      <td>{item.sourceLabel}</td>
                      <td>
                        <span className={freshnessBadgeClass(item.freshnessState)}>
                          {item.freshnessLabel}
                        </span>
                      </td>
                      <td>
                        <span
                          className="shift-workforce-cell-primary"
                          data-attention={item.competencyAttention ? "true" : "false"}
                        >
                          {item.competencySummary || "—"}
                        </span>
                      </td>
                      <td>
                        <button
                          type="button"
                          className="shift-workforce-btn shift-workforce-btn--sm"
                          onClick={(event) => {
                            event.stopPropagation();
                            selectPerson(item);
                          }}
                        >
                          Detay
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </div>

          {persons.totalCount > PAGE_SIZE ? (
            <div className="shift-workforce-pagination">
              <span className="shift-workforce-pagination__info">
                Sayfa {page} / {totalPages}
              </span>
              <div className="shift-workforce-pagination__actions">
                <button
                  type="button"
                  className="shift-workforce-btn shift-workforce-btn--sm"
                  disabled={page <= 1 || isPending}
                  onClick={() => setPage((current) => Math.max(1, current - 1))}
                >
                  Önceki
                </button>
                <button
                  type="button"
                  className="shift-workforce-btn shift-workforce-btn--sm"
                  disabled={page >= totalPages || isPending}
                  onClick={() => setPage((current) => Math.min(totalPages, current + 1))}
                >
                  Sonraki
                </button>
              </div>
            </div>
          ) : null}
        </section>

        {renderDetailPanel()}
      </div>

      {renderCompetencyModal()}
    </div>
  );
}
