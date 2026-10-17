"use client";

import { useCallback, useEffect, useMemo, useState, useTransition } from "react";
import {
  AlertTriangle,
  CheckCircle2,
  ClipboardList,
  LoaderCircle,
  UserCheck,
  Users,
  X,
} from "lucide-react";

import {
  assignmentStatusBadgeClass,
  canCreateDraftCandidate,
  formatShiftAssignmentCount,
  formatShiftAssignmentDateTime,
  getCompetencyCodeLabel,
  LOCAL_PILOT_DISCLAIMER,
  outcomeBadgeClass,
  severityBadgeClass,
  SHIFT_ASSIGNMENT_STATUS_FILTER_OPTIONS,
} from "../domain/shift-assignment-labels";
import type {
  ShiftAssignmentCandidateDetail,
  ShiftAssignmentCandidateSummary,
  ShiftAssignmentCandidatesResult,
  ShiftAssignmentContextOptions,
  ShiftAssignmentRecord,
  ShiftAssignmentSelectionContext,
} from "../domain/shift-assignment-types";
import {
  approveShiftAssignmentAction,
  cancelShiftAssignmentAction,
  createShiftAssignmentDraftAction,
  loadShiftAssignmentCandidateDetailAction,
  loadShiftAssignmentCandidatesAction,
  loadShiftAssignmentRegistryAction,
} from "../server/shift-assignment-actions";
import {
  availabilityBadgeClass,
  freshnessBadgeClass,
} from "../../shift-workforce/domain/shift-workforce-labels";
import { customerMessage } from "@/lib/presentation/customer-message";
import { InlineUnavailable } from "@/components/ui/state-page";

type ShiftAssignmentPanelProps = Readonly<{
  contextOptions: ShiftAssignmentContextOptions;
  initialSelection: ShiftAssignmentSelectionContext;
  canManage: boolean;
}>;

type ApprovalModalState =
  | Readonly<{
      mode: "create-draft";
      candidate: ShiftAssignmentCandidateSummary;
      assessmentId: string;
    }>
  | Readonly<{
      mode: "approve";
      assignment: ShiftAssignmentRecord;
    }>
  | null;

const CANDIDATE_PAGE_SIZE = 20;
const ASSIGNMENT_PAGE_SIZE = 10;

function buildPlannedWindow(shiftDate: string): { start: string; end: string } {
  const start = new Date(`${shiftDate}T08:00:00`);
  const end = new Date(`${shiftDate}T16:00:00`);
  return {
    start: start.toISOString(),
    end: end.toISOString(),
  };
}

export function ShiftAssignmentPanel({
  contextOptions,
  initialSelection,
  canManage,
}: ShiftAssignmentPanelProps) {
  const [selection, setSelection] = useState<ShiftAssignmentSelectionContext>(initialSelection);
  const [candidatesResult, setCandidatesResult] = useState<ShiftAssignmentCandidatesResult | null>(
    null,
  );
  const [assignments, setAssignments] = useState<readonly ShiftAssignmentRecord[]>([]);
  const [assignmentTotalCount, setAssignmentTotalCount] = useState(0);
  const [assignmentPage, setAssignmentPage] = useState(1);
  const [assignmentStatusFilter, setAssignmentStatusFilter] = useState("");
  const [selectedPersonId, setSelectedPersonId] = useState("");
  const [candidateDetail, setCandidateDetail] = useState<ShiftAssignmentCandidateDetail | undefined>();
  const [approvalModal, setApprovalModal] = useState<ApprovalModalState>(null);
  const [message, setMessage] = useState<string | null>(null);
  const [error, setError] = useState<string | null>(null);
  const [hasLoadedCandidates, setHasLoadedCandidates] = useState(false);
  const [isPending, startTransition] = useTransition();

  const selectedCandidate =
    candidatesResult?.candidates.find((item) => item.personId === selectedPersonId) ?? undefined;
  const detailOpen = Boolean(selectedCandidate);
  const activeDetail =
    candidateDetail && candidateDetail.personId === selectedPersonId ? candidateDetail : undefined;

  const assignmentTotalPages = Math.max(
    1,
    Math.ceil(assignmentTotalCount / ASSIGNMENT_PAGE_SIZE),
  );

  const competencyLabel = useMemo(
    () =>
      getCompetencyCodeLabel(
        selection.requiredCompetencyCode,
        contextOptions.competencyCodes.find(
          (item) => item.value === selection.requiredCompetencyCode,
        )?.label,
      ),
    [contextOptions.competencyCodes, selection.requiredCompetencyCode],
  );

  const reloadAssignments = useCallback(() => {
    startTransition(async () => {
      const result = await loadShiftAssignmentRegistryAction({
        productionOrderReference: selection.productionOrderReference,
        status: assignmentStatusFilter || undefined,
        page: assignmentPage,
        pageSize: ASSIGNMENT_PAGE_SIZE,
      });

      if (result.status === "failure") {
        setError(result.message);
        return;
      }

      setAssignments(result.assignments.items);
      setAssignmentTotalCount(result.assignments.totalCount);
    });
  }, [assignmentPage, assignmentStatusFilter, selection.productionOrderReference]);

  const reloadCandidates = useCallback(() => {
    startTransition(async () => {
      setError(null);
      setMessage(null);
      const result = await loadShiftAssignmentCandidatesAction(selection);
      if (result.status === "failure") {
        setCandidatesResult(null);
        setHasLoadedCandidates(true);
        setError(result.message);
        return;
      }

      setCandidatesResult(result.result);
      setHasLoadedCandidates(true);
      setSelectedPersonId((currentId) => {
        if (
          currentId &&
          !result.result.candidates.some((item) => item.personId === currentId)
        ) {
          setCandidateDetail(undefined);
          return "";
        }
        return currentId;
      });
    });
  }, [selection]);

  useEffect(() => {
    reloadAssignments();
  }, [reloadAssignments]);

  function clearCandidateSelection() {
    setSelectedPersonId("");
    setCandidateDetail(undefined);
    setApprovalModal(null);
  }

  function selectCandidate(candidate: ShiftAssignmentCandidateSummary) {
    setSelectedPersonId(candidate.personId);
    setCandidateDetail(undefined);
    setApprovalModal(null);
    setMessage(null);
    setError(null);

    startTransition(async () => {
      const result = await loadShiftAssignmentCandidateDetailAction(
        candidate.personId,
        selection,
      );
      if (result.status === "success") {
        setCandidateDetail(result.candidate);
      } else {
        setError(result.message);
      }
    });
  }

  function openCreateDraftModal(candidate: ShiftAssignmentCandidateSummary) {
    if (!candidatesResult) {
      return;
    }

    setApprovalModal({
      mode: "create-draft",
      candidate,
      assessmentId: candidatesResult.assessmentId,
    });
  }

  function openApproveModal(assignment: ShiftAssignmentRecord) {
    setApprovalModal({ mode: "approve", assignment });
  }

  function handleCreateDraft() {
    if (!approvalModal || approvalModal.mode !== "create-draft" || !candidatesResult) {
      return;
    }

    const plannedWindow = buildPlannedWindow(selection.shiftDate);
    startTransition(async () => {
      setMessage(null);
      setError(null);
      const result = await createShiftAssignmentDraftAction({
        productionOrderReference: selection.productionOrderReference,
        machineAssetId: selection.machineAssetId,
        productionShiftId: selection.productionShiftId,
        shiftDate: selection.shiftDate,
        workforcePersonId: approvalModal.candidate.personId,
        plannedStartUtc: plannedWindow.start,
        plannedEndUtc: plannedWindow.end,
        eligibilityAssessmentId: approvalModal.assessmentId,
        eligibilityResultId: approvalModal.candidate.eligibilityResultId,
        requiredCompetencyCode: selection.requiredCompetencyCode,
        machineFamilyScope: selection.machineFamilyScope,
        processScope: selection.processScope,
        idempotencyKey: crypto.randomUUID(),
      });

      if (result.status === "failure") {
        setError(result.message);
        return;
      }

      setApprovalModal(null);
      setMessage("Taslak atama oluşturuldu. Onay için atama kaydını kullanın.");
      reloadAssignments();
    });
  }

  function handleApproveDraft() {
    if (!approvalModal || approvalModal.mode !== "approve") {
      return;
    }

    startTransition(async () => {
      setMessage(null);
      setError(null);
      const result = await approveShiftAssignmentAction(approvalModal.assignment.id, {
        expectedVersion: approvalModal.assignment.version,
        idempotencyKey: crypto.randomUUID(),
      });

      if (result.status === "failure") {
        setError(result.message);
        return;
      }

      setApprovalModal(null);
      setMessage("Operatör ataması onaylandı.");
      reloadAssignments();
      reloadCandidates();
    });
  }

  function handleCancelAssignment(assignment: ShiftAssignmentRecord) {
    startTransition(async () => {
      setMessage(null);
      setError(null);
      const result = await cancelShiftAssignmentAction(assignment.id, {
        expectedVersion: assignment.version,
        cancellationReason: "Kullanıcı tarafından iptal edildi",
      });

      if (result.status === "failure") {
        setError(result.message);
        return;
      }

      setMessage("Atama iptal edildi.");
      reloadAssignments();
      reloadCandidates();
    });
  }

  function renderApprovalModal() {
    if (!approvalModal) {
      return null;
    }

    const isDraft = approvalModal.mode === "create-draft";
    const candidateName = isDraft
      ? approvalModal.candidate.displayName
      : approvalModal.assignment.displayName;

    return (
      <div className="shift-workforce-modal-backdrop" role="presentation">
        <div
          className="shift-workforce-modal"
          role="dialog"
          aria-modal="true"
          aria-labelledby="shift-assignment-approval-modal-title"
        >
          <h3 id="shift-assignment-approval-modal-title">
            {isDraft ? "Taslak Atama Onayı" : "Atamayı Onayla"}
          </h3>
          <p className="shift-workforce-empty-inline">
            {isDraft
              ? "Seçili aday için taslak atama oluşturulacak. Otomatik atama yapılmaz; onay ayrı bir adımdır."
              : "Bu atamayı insan onayıyla kesinleştirmek üzeresiniz. Uygunluk değerlendirmesi kayıt altına alınmıştır."}
          </p>
          <dl className="shift-workforce-detail-grid">
            <div>
              <dt>Operatör</dt>
              <dd>{candidateName}</dd>
            </div>
            <div>
              <dt>Üretim emri</dt>
              <dd>{selection.productionOrderReference}</dd>
            </div>
            <div>
              <dt>Vardiya tarihi</dt>
              <dd>{selection.shiftDate}</dd>
            </div>
            <div>
              <dt>Gerekli yetkinlik</dt>
              <dd>{competencyLabel}</dd>
            </div>
          </dl>
          <div className="shift-workforce-form-actions">
            <button
              type="button"
              className="shift-workforce-btn"
              onClick={() => setApprovalModal(null)}
            >
              Vazgeç
            </button>
            <button
              type="button"
              className="shift-workforce-btn shift-workforce-btn--primary"
              disabled={isPending}
              onClick={isDraft ? handleCreateDraft : handleApproveDraft}
            >
              {isPending ? (
                <LoaderCircle aria-hidden="true" className="animate-spin" size={14} />
              ) : null}
              {isDraft ? "Taslak Oluştur" : "Onayla"}
            </button>
          </div>
        </div>
      </div>
    );
  }

  function renderCandidateDetail() {
    if (!selectedCandidate) {
      return null;
    }

    const detail = activeDetail;
    const canSelect =
      canManage && canCreateDraftCandidate(selectedCandidate.outcome);

    return (
      <aside className="shift-workforce-detail-workspace" aria-label="Aday detayı">
        <header className="shift-workforce-detail-header">
          <div className="shift-workforce-detail-header__row">
            <div className="shift-workforce-detail-header__identity">
              <p className="shift-workforce-detail-breadcrumb">Vardiyalar · Operatör Atama</p>
              <h2>{selectedCandidate.displayName}</h2>
              <p className="shift-workforce-detail-header__meta">
                {selectedCandidate.employeeCode}
                {selectedCandidate.jobTitle ? ` · ${selectedCandidate.jobTitle}` : ""}
              </p>
              <div className="shift-workforce-detail-header__chips">
                <span className={outcomeBadgeClass(selectedCandidate.outcome)}>
                  {selectedCandidate.outcomeLabel}
                </span>
                <span className={availabilityBadgeClass(selectedCandidate.availabilityState)}>
                  {selectedCandidate.availabilityLabel}
                </span>
                <span className={freshnessBadgeClass(selectedCandidate.freshnessState)}>
                  {selectedCandidate.freshnessLabel}
                </span>
              </div>
            </div>
            <button
              type="button"
              className="shift-workforce-detail-close"
              aria-label="Aday detayını kapat"
              onClick={clearCandidateSelection}
            >
              <X aria-hidden="true" size={16} />
            </button>
          </div>
        </header>

        {selectedCandidate.isPilotData ? (
          <div className="shift-workforce-pilot-banner" role="status">
            {LOCAL_PILOT_DISCLAIMER}
          </div>
        ) : null}

        {selectedCandidate.hasOverlapWarning ? (
          <div className="shift-workforce-alert shift-workforce-alert--warning" role="status">
            <div>
              <h3>Çakışan atama uyarısı</h3>
              <p>Bu personelin planlanan pencerede onaylı bir ataması olabilir.</p>
            </div>
          </div>
        ) : null}

        {selectedCandidate.outcome === "insufficient-data" ? (
          <div className="shift-workforce-alert shift-workforce-alert--warning" role="status">
            <div>
              <h3>Yetersiz veri</h3>
              <p>
                Uygunluk değerlendirmesi için gerekli kaynak verisi eksik; onay engellenir.
              </p>
            </div>
          </div>
        ) : null}

        {isPending && !detail ? (
          <div className="shift-workforce-loading">
            <LoaderCircle aria-hidden="true" className="animate-spin" size={16} />
            Aday detayı yükleniyor…
          </div>
        ) : detail ? (
          <>
            <section className="shift-workforce-detail-section" aria-label="ERP/HR bilgileri">
              <h3>ERP/HR bilgileri (salt okunur)</h3>
              <dl className="shift-workforce-detail-grid shift-workforce-detail-grid--readonly">
                <div>
                  <dt>Personel kodu</dt>
                  <dd>{detail.employeeCode}</dd>
                </div>
                <div>
                  <dt>Ad soyad</dt>
                  <dd>{detail.displayName}</dd>
                </div>
                <div>
                  <dt>Departman</dt>
                  <dd>{detail.department ?? "—"}</dd>
                </div>
                <div>
                  <dt>Ünvan</dt>
                  <dd>{detail.jobTitle ?? "—"}</dd>
                </div>
                <div>
                  <dt>Vardiya referansı</dt>
                  <dd>{detail.shiftLabel ?? "—"}</dd>
                </div>
                <div>
                  <dt>Kaynak güncelleme</dt>
                  <dd>{formatShiftAssignmentDateTime(detail.sourceUpdatedAtUtc)}</dd>
                </div>
              </dl>
            </section>

            <section className="shift-workforce-detail-section" aria-label="Uygunluk değerlendirmesi">
              <h3>Uygunluk değerlendirmesi</h3>
              <dl className="shift-workforce-detail-grid">
                <div>
                  <dt>Kaynak</dt>
                  <dd>{detail.sourceLabel}</dd>
                </div>
                <div>
                  <dt>Birincil neden</dt>
                  <dd>{detail.primaryReasonLabel ?? "—"}</dd>
                </div>
                <div>
                  <dt>Yetkinlik özeti</dt>
                  <dd>{detail.competencySummary}</dd>
                </div>
              </dl>
            </section>

            <section className="shift-workforce-detail-section" aria-label="Yapılandırılmış nedenler">
              <h3>Yapılandırılmış nedenler</h3>
              {detail.reasons.length === 0 ? (
                <p className="shift-workforce-empty-inline">Yapılandırılmış neden bulunmuyor.</p>
              ) : (
                <ul className="shift-assignment-reason-list">
                  {detail.reasons.map((reason) => (
                    <li key={`${reason.reasonCode}-${reason.labelTr}`} className="shift-assignment-reason-item">
                      <div className="shift-assignment-reason-item__header">
                        <span className="shift-assignment-reason-item__title">{reason.labelTr}</span>
                        <span className={severityBadgeClass(reason.severity)}>
                          {reason.severityLabel}
                        </span>
                      </div>
                      <div className="shift-assignment-reason-item__meta">
                        {reason.requiredValue ? `Beklenen: ${reason.requiredValue}` : null}
                        {reason.requiredValue && reason.observedValue ? " · " : null}
                        {reason.observedValue ? `Gözlemlenen: ${reason.observedValue}` : null}
                        {(reason.requiredValue || reason.observedValue) && reason.evidenceSourceLabel
                          ? " · "
                          : null}
                        {reason.evidenceSourceLabel
                          ? `Kaynak: ${reason.evidenceSourceLabel}`
                          : null}
                        {reason.evidenceTimestampUtc
                          ? ` · ${formatShiftAssignmentDateTime(reason.evidenceTimestampUtc)}`
                          : null}
                      </div>
                    </li>
                  ))}
                </ul>
              )}
            </section>

            {canManage ? (
              <div className="shift-assignment-approval-actions">
                <button
                  type="button"
                  className="shift-workforce-btn shift-workforce-btn--primary"
                  disabled={!canSelect || isPending}
                  onClick={() => openCreateDraftModal(selectedCandidate)}
                >
                  <UserCheck aria-hidden="true" size={14} />
                  Taslak Atama Oluştur
                </button>
                {!canSelect ? (
                  <p className="shift-workforce-empty-inline">
                    Bu aday uygunluk sonucu nedeniyle taslak oluşturulamaz.
                  </p>
                ) : null}
              </div>
            ) : null}
          </>
        ) : (
          <div className="shift-workforce-empty">
            <InlineUnavailable subject="Aday detayı" />
          </div>
        )}
      </aside>
    );
  }

  const summary = candidatesResult?.summary;
  const hasEligibleCandidate = (summary?.eligibleCount ?? 0) > 0;

  return (
    <div className="shift-assignment-panel">
      {!canManage ? (
        <div className="shift-workforce-alert shift-workforce-alert--info" role="status">
          <div>
            <h3>Salt okunur erişim</h3>
            <p>
              Operatör atama adaylarını görüntüleyebilirsiniz; taslak oluşturma ve onay işlemleri
              yönetici yetkisi gerektirir.
            </p>
          </div>
        </div>
      ) : null}

      {message ? <div className="shift-workforce-message" role="status">{customerMessage(message)}</div> : null}
      {error ? (
        <div className="shift-workforce-alert shift-workforce-alert--error" role="alert">
          <div>
            <h3>İşlem hatası</h3>
            <p>{customerMessage(error)}</p>
          </div>
        </div>
      ) : null}

      <section className="shift-assignment-context" aria-label="Seçim bağlamı">
        <div className="shift-workforce-registry__header">
          <h2 className="shift-workforce-registry__title">Seçim Bağlamı</h2>
        </div>
        <div className="shift-assignment-context__grid">
          <label>
            Üretim emri
            <select
              value={selection.productionOrderReference}
              onChange={(event) =>
                setSelection((current) => ({
                  ...current,
                  productionOrderReference: event.target.value,
                }))
              }
            >
              {contextOptions.productionOrders.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>
          <label>
            Makine
            <select
              value={selection.machineAssetId}
              onChange={(event) =>
                setSelection((current) => ({
                  ...current,
                  machineAssetId: event.target.value,
                }))
              }
            >
              {contextOptions.machines.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>
          <label>
            Vardiya
            <select
              value={selection.productionShiftId}
              onChange={(event) =>
                setSelection((current) => ({
                  ...current,
                  productionShiftId: event.target.value,
                }))
              }
            >
              {contextOptions.shifts.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>
          <label>
            Vardiya tarihi
            <input
              type="date"
              value={selection.shiftDate}
              onChange={(event) =>
                setSelection((current) => ({
                  ...current,
                  shiftDate: event.target.value,
                }))
              }
            />
          </label>
          <label>
            Gerekli yetkinlik
            <select
              value={selection.requiredCompetencyCode}
              onChange={(event) =>
                setSelection((current) => ({
                  ...current,
                  requiredCompetencyCode: event.target.value,
                }))
              }
            >
              {contextOptions.competencyCodes.map((option) => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </label>
        </div>
        <div className="shift-assignment-context__actions">
          <button
            type="button"
            className="shift-workforce-btn shift-workforce-btn--primary"
            disabled={isPending}
            onClick={reloadCandidates}
          >
            {isPending ? (
              <LoaderCircle aria-hidden="true" className="animate-spin" size={14} />
            ) : null}
            Uygunluk Değerlendir
          </button>
        </div>
      </section>

      {hasLoadedCandidates ? (
        <section className="shift-assignment-kpis" aria-label="Uygunluk göstergeleri">
          <article className="shift-workforce-kpi">
            <span className="shift-workforce-kpi__label">Uygun</span>
            <strong className="shift-workforce-kpi__value shift-assignment-kpi__value--eligible">
              {formatShiftAssignmentCount(summary?.eligibleCount)}
            </strong>
          </article>
          <article className="shift-workforce-kpi">
            <span className="shift-workforce-kpi__label">Koşullu</span>
            <strong className="shift-workforce-kpi__value shift-assignment-kpi__value--conditional">
              {formatShiftAssignmentCount(summary?.conditionalCount)}
            </strong>
          </article>
          <article className="shift-workforce-kpi">
            <span className="shift-workforce-kpi__label">Uygun Değil</span>
            <strong className="shift-workforce-kpi__value shift-assignment-kpi__value--ineligible">
              {formatShiftAssignmentCount(summary?.ineligibleCount)}
            </strong>
          </article>
          <article className="shift-workforce-kpi">
            <span className="shift-workforce-kpi__label">Yetersiz Veri</span>
            <strong className="shift-workforce-kpi__value shift-assignment-kpi__value--insufficient">
              {formatShiftAssignmentCount(summary?.insufficientDataCount)}
            </strong>
          </article>
        </section>
      ) : null}

      <div
        className={`shift-workforce-workspace${detailOpen ? " shift-workforce-workspace--detail-open" : ""}`}
      >
        <section className="shift-workforce-registry" aria-label="Aday kaydı">
          <div className="shift-workforce-registry__header">
            <h2 className="shift-workforce-registry__title">Aday Kaydı</h2>
            <span className="shift-workforce-registry__count">
              {isPending && !candidatesResult
                ? "Yükleniyor…"
                : `${candidatesResult?.candidates.length ?? 0} aday`}
            </span>
          </div>

          <div className="shift-workforce-registry__table-wrap" tabIndex={0} role="region" aria-label="Tablo (kaydırılabilir)">
            {!hasLoadedCandidates ? (
              <div className="shift-workforce-empty">
                <Users aria-hidden="true" size={28} />
                <h3>Değerlendirme bekleniyor</h3>
                <p>Seçim bağlamını ayarlayıp uygunluk değerlendirmesini başlatın.</p>
              </div>
            ) : candidatesResult && candidatesResult.candidates.length === 0 ? (
              <div className="shift-workforce-empty">
                <AlertTriangle aria-hidden="true" size={28} />
                <h3>Aday bulunamadı</h3>
                <p>Seçili bağlam için değerlendirilecek personel bulunmuyor.</p>
              </div>
            ) : !hasEligibleCandidate && candidatesResult ? (
              <div className="shift-workforce-alert shift-workforce-alert--warning" role="status">
                <div>
                  <h3>Uygun aday yok</h3>
                  <p>
                    Değerlendirme tamamlandı ancak tam uygun aday bulunmuyor. Koşullu adaylar
                    yönetici onayıyla ilerleyebilir.
                  </p>
                </div>
              </div>
            ) : null}

            {candidatesResult && candidatesResult.candidates.length > 0 ? (
              <table aria-label="Uygun adaylar" className="shift-workforce-table">
                <thead>
                  <tr>
                    <th>Personel</th>
                    <th>Rol</th>
                    <th>Müsaitlik</th>
                    <th>Yetkinlik</th>
                    <th>Tazelik</th>
                    <th>Uygunluk</th>
                    <th>Birincil neden</th>
                    <th>İşlem</th>
                  </tr>
                </thead>
                <tbody>
                  {candidatesResult.candidates.slice(0, CANDIDATE_PAGE_SIZE).map((item) => (
                    <tr
                      key={item.personId}
                      data-selected={item.personId === selectedPersonId ? "true" : "false"}
                      tabIndex={0}
                      onClick={() => selectCandidate(item)}
                      onKeyDown={(event) => {
                        if (event.key === "Enter" || event.key === " ") {
                          event.preventDefault();
                          selectCandidate(item);
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
                          {item.department ?? "—"}
                        </span>
                      </td>
                      <td>
                        <span className={availabilityBadgeClass(item.availabilityState)}>
                          {item.availabilityLabel}
                        </span>
                      </td>
                      <td>{item.competencySummary}</td>
                      <td>
                        <span className={freshnessBadgeClass(item.freshnessState)}>
                          {item.freshnessLabel}
                        </span>
                      </td>
                      <td>
                        <span className={outcomeBadgeClass(item.outcome)}>
                          {item.outcomeLabel}
                        </span>
                      </td>
                      <td>{item.primaryReasonLabel ?? "—"}</td>
                      <td>
                        <button
                          type="button"
                          className="shift-workforce-btn shift-workforce-btn--sm"
                          onClick={(event) => {
                            event.stopPropagation();
                            selectCandidate(item);
                          }}
                        >
                          Detay
                        </button>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
            ) : null}
          </div>
        </section>

        {renderCandidateDetail()}
      </div>

      <section className="shift-workforce-registry shift-assignment-registry" aria-label="Atama kaydı">
        <div className="shift-workforce-registry__header">
          <h2 className="shift-workforce-registry__title">
            <ClipboardList aria-hidden="true" size={16} />
            Atama Kaydı
          </h2>
          <span className="shift-workforce-registry__count">
            {isPending ? "Yükleniyor…" : `${assignmentTotalCount} kayıt`}
          </span>
        </div>

        <div className="shift-workforce-registry__filters">
          <div className="shift-workforce-filter">
            <label htmlFor="shift-assignment-status">Durum</label>
            <select
              id="shift-assignment-status"
              value={assignmentStatusFilter}
              onChange={(event) => {
                setAssignmentStatusFilter(event.target.value);
                setAssignmentPage(1);
              }}
            >
              {SHIFT_ASSIGNMENT_STATUS_FILTER_OPTIONS.map((option) => (
                <option key={option.value || "all"} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </div>
        </div>

        <div className="shift-workforce-registry__table-wrap" tabIndex={0} role="region" aria-label="Atama kayıtları (kaydırılabilir)">
          {assignments.length === 0 && !isPending ? (
            <div className="shift-workforce-empty">
              <CheckCircle2 aria-hidden="true" size={28} />
              <h3>Atama kaydı yok</h3>
              <p>Henüz taslak veya onaylı operatör ataması bulunmuyor.</p>
            </div>
          ) : (
            <table aria-label="Atama kayıtları" className="shift-workforce-table">
              <thead>
                <tr>
                  <th>Operatör</th>
                  <th>Üretim emri</th>
                  <th>Makine</th>
                  <th>Vardiya</th>
                  <th>Durum</th>
                  <th>Onay</th>
                  <th>İşlem</th>
                </tr>
              </thead>
              <tbody>
                {assignments.map((item) => (
                  <tr key={item.id}>
                    <td>
                      <span className="shift-workforce-cell-primary">{item.displayName}</span>
                      <span className="shift-workforce-cell-secondary">{item.employeeCode}</span>
                    </td>
                    <td>{item.productionOrderLabel ?? item.productionOrderReference}</td>
                    <td>{item.machineAssetLabel}</td>
                    <td>
                      <span className="shift-workforce-cell-primary">{item.shiftLabel}</span>
                      <span className="shift-workforce-cell-secondary">{item.shiftDate}</span>
                    </td>
                    <td>
                      <span className={assignmentStatusBadgeClass(item.status)}>
                        {item.statusLabel}
                      </span>
                    </td>
                    <td>
                      {item.approvedByDisplayName
                        ? `${item.approvedByDisplayName} · ${formatShiftAssignmentDateTime(item.approvedAtUtc)}`
                        : "—"}
                    </td>
                    <td>
                      <div className="shift-assignment-row-actions">
                        {canManage && item.status === "draft" ? (
                          <button
                            type="button"
                            className="shift-workforce-btn shift-workforce-btn--sm shift-workforce-btn--primary"
                            disabled={isPending}
                            onClick={() => openApproveModal(item)}
                          >
                            Onayla
                          </button>
                        ) : null}
                        {canManage &&
                        (item.status === "draft" || item.status === "approved") ? (
                          <button
                            type="button"
                            className="shift-workforce-btn shift-workforce-btn--sm"
                            disabled={isPending}
                            onClick={() => handleCancelAssignment(item)}
                          >
                            İptal
                          </button>
                        ) : null}
                      </div>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          )}
        </div>

        {assignmentTotalCount > ASSIGNMENT_PAGE_SIZE ? (
          <div className="shift-workforce-pagination">
            <span className="shift-workforce-pagination__info">
              Sayfa {assignmentPage} / {assignmentTotalPages}
            </span>
            <div className="shift-workforce-pagination__actions">
              <button
                type="button"
                className="shift-workforce-btn shift-workforce-btn--sm"
                disabled={assignmentPage <= 1 || isPending}
                onClick={() => setAssignmentPage((current) => Math.max(1, current - 1))}
              >
                Önceki
              </button>
              <button
                type="button"
                className="shift-workforce-btn shift-workforce-btn--sm"
                disabled={assignmentPage >= assignmentTotalPages || isPending}
                onClick={() =>
                  setAssignmentPage((current) => Math.min(assignmentTotalPages, current + 1))
                }
              >
                Sonraki
              </button>
            </div>
          </div>
        ) : null}
      </section>

      {renderApprovalModal()}
    </div>
  );
}
