"use client";

import {
  useCallback,
  useMemo,
  useState,
  useTransition,
  type KeyboardEvent as ReactKeyboardEvent,
} from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import {
  AlertTriangle,
  LoaderCircle,
  RefreshCw,
  ShieldAlert,
  ClipboardList,
} from "lucide-react";

import {
  applyOperationalRiskCoverageToKpis,
  buildOperationalRiskDonutArcs,
  resolveOperationalRiskKpiToneClass,
} from "./operational-risk-presentation";
import {
  formatOperationalRiskCount,
  formatOperationalRiskPercent,
} from "../domain/operational-risk-formatters";
import {
  getOperationalRiskDetectionOutcomeLabel,
  getOperationalRiskEvidenceLabel,
  getOperationalRiskLifecycleLabel,
  getOperationalRiskProfileStatusLabel,
  getOperationalRiskSeverityLabel,
  getOperationalRiskTransitionLabel,
  OPERATIONAL_RISK_CURRENT_EVIDENCE_SCOPE,
  OPERATIONAL_RISK_DEVELOPMENT_BANNER,
  OPERATIONAL_RISK_READONLY_NOTE,
  OPERATIONAL_RISK_TAB_OPTIONS,
} from "../domain/operational-risk-labels";
import type {
  OperationalRiskCaseDetail,
  OperationalRiskCaseSummary,
  OperationalRiskDetectionProfileSummary,
  OperationalRiskDetectionRunDetail,
  OperationalRiskTabId,
  OperationalRiskWorkspacePayload,
} from "../types/operational-risk-types";
import {
  executeOperationalRiskDetectionRunAction,
  loadOperationalRiskCaseDetailAction,
  loadOperationalRiskRunDetailAction,
  refreshOperationalRiskWorkspaceAction,
  transitionOperationalRiskCaseAction,
  updateOperationalRiskDetectionProfileAction,
} from "../server/operational-risk-actions";
import "./operational-risk-workspace.css";

type OperationalRiskWorkspaceViewProps = Readonly<{
  workspace: OperationalRiskWorkspacePayload;
  canManage: boolean;
}>;

const VALID_TAB_IDS = new Set<OperationalRiskTabId>(
  OPERATIONAL_RISK_TAB_OPTIONS.map((tab) => tab.id),
);

const KPI_ICONS: Record<string, typeof ShieldAlert> = {
  "open-cases": ShieldAlert,
  "critical-high": AlertTriangle,
  "new-detections": ShieldAlert,
  "under-review": ShieldAlert,
  "insufficient-data": AlertTriangle,
  "source-conflict": AlertTriangle,
};

function severityBadgeClass(band: string | null): string {
  if (band === "critical") return "oprisk-badge oprisk-badge--critical";
  if (band === "high") return "oprisk-badge oprisk-badge--high";
  if (band === "moderate") return "oprisk-badge oprisk-badge--moderate";
  if (band === "low") return "oprisk-badge oprisk-badge--low";
  return "oprisk-badge";
}

function sourceStateClass(state: string): string {
  return `oprisk-source-card oprisk-source-card--${state}`;
}

function isMeaningfulLabel(value: string | null | undefined): value is string {
  const trimmed = value?.trim();
  return Boolean(trimmed) && trimmed !== "—" && trimmed !== "-";
}

export function OperationalRiskWorkspaceView({
  workspace: initialWorkspace,
  canManage,
}: OperationalRiskWorkspaceViewProps) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [workspace, setWorkspace] = useState(initialWorkspace);
  const [isPending, startTransition] = useTransition();
  const [feedback, setFeedback] = useState<string | null>(null);
  const [feedbackTone, setFeedbackTone] = useState<"success" | "error">("success");
  const [selectedCaseId, setSelectedCaseId] = useState<string | null>(null);
  const [caseDetail, setCaseDetail] = useState<OperationalRiskCaseDetail | null>(null);
  const [caseDetailStatus, setCaseDetailStatus] = useState<"idle" | "loading" | "ready" | "error" | "not-found">("idle");
  const [caseDetailError, setCaseDetailError] = useState<string | null>(null);
  const [selectedRunId, setSelectedRunId] = useState<string | null>(null);
  const [runDetail, setRunDetail] = useState<OperationalRiskDetectionRunDetail | null>(null);
  const [selectedProfileId, setSelectedProfileId] = useState<string | null>(null);
  const [statusFilter, setStatusFilter] = useState("");
  const [severityFilter, setSeverityFilter] = useState("");
  const [categoryFilter, setCategoryFilter] = useState("");
  const [evidenceFilter, setEvidenceFilter] = useState("");
  const [searchQuery, setSearchQuery] = useState("");
  const [transitionTarget, setTransitionTarget] = useState<string | null>(null);
  const [transitionNote, setTransitionNote] = useState("");
  const [profileEditId, setProfileEditId] = useState<string | null>(null);
  const [profileDisplayName, setProfileDisplayName] = useState("");
  const [profileStatus, setProfileStatus] = useState("approved");
  const [runProfileId, setRunProfileId] = useState("");
  const [runWindowStart, setRunWindowStart] = useState("");
  const [runWindowEnd, setRunWindowEnd] = useState("");

  const approvedProfiles = useMemo(
    () => workspace.profiles.filter((profile) => profile.isExecutable),
    [workspace.profiles],
  );

  const selectedProfile = useMemo(() => {
    if (selectedProfileId) {
      return workspace.profiles.find((profile) => profile.id === selectedProfileId)
        ?? workspace.profiles[0]
        ?? null;
    }
    return workspace.profiles[0] ?? null;
  }, [workspace.profiles, selectedProfileId]);

  const defaultRunProfileId = approvedProfiles.length === 1 ? approvedProfiles[0].id : "";
  const effectiveRunProfileId = runProfileId || defaultRunProfileId;

  const activeTab = useMemo<OperationalRiskTabId>(() => {
    const tab = searchParams.get("tab");
    return tab && VALID_TAB_IDS.has(tab as OperationalRiskTabId)
      ? (tab as OperationalRiskTabId)
      : "overview";
  }, [searchParams]);

  const setActiveTab = useCallback((tabId: OperationalRiskTabId) => {
    const params = new URLSearchParams(searchParams.toString());
    params.set("tab", tabId);
    router.replace(`${pathname}?${params.toString()}`, { scroll: false });
  }, [pathname, router, searchParams]);

  const filteredCases = useMemo(() => {
    const query = searchQuery.trim().toLowerCase();
    return workspace.cases.filter((item) => {
      if (statusFilter && item.lifecycleStatus !== statusFilter) return false;
      if (severityFilter && item.severityBand !== severityFilter) return false;
      if (categoryFilter && item.category !== categoryFilter) return false;
      if (evidenceFilter && item.evidenceState !== evidenceFilter) return false;
      if (!query) return true;
      return item.title.toLowerCase().includes(query)
        || item.caseReference.toLowerCase().includes(query);
    });
  }, [workspace.cases, statusFilter, severityFilter, categoryFilter, evidenceFilter, searchQuery]);

  const donutArcs = useMemo(
    () => buildOperationalRiskDonutArcs(workspace.distribution.slices),
    [workspace.distribution.slices],
  );

  const kpis = useMemo(
    () => applyOperationalRiskCoverageToKpis(workspace.kpis, workspace.sourceCoverage),
    [workspace.kpis, workspace.sourceCoverage],
  );

  const refreshWorkspace = useCallback(() => {
    startTransition(async () => {
      const result = await refreshOperationalRiskWorkspaceAction();
      if (result.status === "failure") {
        setFeedback(result.message);
        setFeedbackTone("error");
        return;
      }
      setWorkspace(result.workspace);
      setFeedback("Çalışma alanı yenilendi.");
      setFeedbackTone("success");
    });
  }, []);

  const openCaseDetail = useCallback((caseId: string) => {
    setSelectedCaseId(caseId);
    setCaseDetail(null);
    setCaseDetailError(null);
    setCaseDetailStatus("loading");
    void (async () => {
      const result = await loadOperationalRiskCaseDetailAction(caseId);
      if (result.status === "failure") {
        if ("code" in result && result.code === "not-found") {
          setCaseDetailStatus("not-found");
          return;
        }
        setCaseDetailStatus("error");
        setCaseDetailError(result.message);
        return;
      }
      setCaseDetail(result.detail);
      setCaseDetailStatus("ready");
    })();
  }, []);

  const retryCaseDetail = useCallback(() => {
    if (selectedCaseId) openCaseDetail(selectedCaseId);
  }, [openCaseDetail, selectedCaseId]);

  const closeCaseDetail = useCallback(() => {
    setSelectedCaseId(null);
    setCaseDetail(null);
    setCaseDetailStatus("idle");
    setCaseDetailError(null);
  }, []);

  const openRunDetail = useCallback((runId: string) => {
    setSelectedRunId(runId);
    setRunDetail(null);
    startTransition(async () => {
      const result = await loadOperationalRiskRunDetailAction(runId);
      if (result.status === "failure") {
        setFeedback(result.message);
        setFeedbackTone("error");
        return;
      }
      setRunDetail(result.detail);
    });
  }, []);

  const handleTransition = useCallback(() => {
    if (!caseDetail || !transitionTarget) return;
    startTransition(async () => {
      const result = await transitionOperationalRiskCaseAction({
        caseId: caseDetail.summary.id,
        expectedVersion: caseDetail.summary.version,
        targetStatus: transitionTarget,
        note: transitionNote.trim() || undefined,
        sourceProvenance: caseDetail.sourceProvenance,
      });
      if (result.status === "failure") {
        setFeedback(result.message);
        setFeedbackTone("error");
        return;
      }
      setTransitionTarget(null);
      setTransitionNote("");
      setFeedback(result.idempotentReplay
        ? "Yaşam döngüsü geçişi zaten uygulanmıştı."
        : "Yaşam döngüsü geçişi kaydedildi.");
      setFeedbackTone("success");
      refreshWorkspace();
      openCaseDetail(caseDetail.summary.id);
    });
  }, [caseDetail, transitionTarget, transitionNote, refreshWorkspace, openCaseDetail]);

  const handleProfileSave = useCallback(() => {
    const profile = workspace.profiles.find((item) => item.id === profileEditId);
    if (!profile) return;
    startTransition(async () => {
      const result = await updateOperationalRiskDetectionProfileAction({
        profileId: profile.id,
        expectedVersion: profile.version,
        displayName: profileDisplayName,
        status: profileStatus,
        sourceProvenance: profile.sourceProvenance,
      });
      if (result.status === "failure") {
        setFeedback(result.message);
        setFeedbackTone("error");
        return;
      }
      setProfileEditId(null);
      setFeedback(result.idempotentReplay ? "Profil güncellemesi zaten uygulanmıştı." : "Profil güncellendi.");
      setFeedbackTone("success");
      refreshWorkspace();
    });
  }, [workspace.profiles, profileEditId, profileDisplayName, profileStatus, refreshWorkspace]);

  const handleRunExecute = useCallback(() => {
    if (!effectiveRunProfileId || !runWindowStart || !runWindowEnd) return;
    const idempotencyKey = `OPRISKUI1-CLOSURE-${Date.now()}`;
    startTransition(async () => {
      const result = await executeOperationalRiskDetectionRunAction({
        profileId: effectiveRunProfileId,
        windowStartUtc: new Date(runWindowStart).toISOString(),
        windowEndUtc: new Date(runWindowEnd).toISOString(),
        sourceProvenance: workspace.provenanceDisclaimer,
        idempotencyKey,
      });
      if (result.status === "failure") {
        setFeedback(result.message);
        setFeedbackTone("error");
        return;
      }
      setFeedback(result.idempotentReplay
        ? "Kontrol çalışması zaten kayıtlıydı."
        : "Kontrol çalışması tamamlandı.");
      setFeedbackTone("success");
      refreshWorkspace();
    });
  }, [effectiveRunProfileId, runWindowStart, runWindowEnd, workspace.provenanceDisclaimer, refreshWorkspace]);

  const onTabKeyDown = (event: ReactKeyboardEvent<HTMLButtonElement>, index: number) => {
    if (event.key !== "ArrowRight" && event.key !== "ArrowLeft") return;
    event.preventDefault();
    const delta = event.key === "ArrowRight" ? 1 : -1;
    const next = (index + delta + OPERATIONAL_RISK_TAB_OPTIONS.length) % OPERATIONAL_RISK_TAB_OPTIONS.length;
    setActiveTab(OPERATIONAL_RISK_TAB_OPTIONS[next].id);
  };

  return (
    <div className="oprisk-page" data-testid="operational-risk-workspace">
      <header className="oprisk-executive">
        <div className="oprisk-executive__top">
          <div className="oprisk-executive__intro">
            <p className="oprisk-executive__eyebrow">OPERASYONEL RİSK YÖNETİMİ</p>
            <h1>Operasyonel Riskler</h1>
            <p className="oprisk-executive__subtitle">
              Operasyonel risk vakalarını, kanıt zaman çizelgesini ve kontrol çalışmalarını tek çalışma alanında izleyin.
            </p>
            <ul className="oprisk-executive__meta">
              <li>{workspace.facilityLabel}</li>
              {isMeaningfulLabel(workspace.reportingPeriodLabel) ? <li>Dönem: {workspace.reportingPeriodLabel}</li> : null}
              {isMeaningfulLabel(workspace.freshnessLabel) ? <li>Güncellik: {workspace.freshnessLabel}</li> : null}
              {isMeaningfulLabel(workspace.evidenceLabel) ? <li>Kanıt: {workspace.evidenceLabel}</li> : null}
              <li>{OPERATIONAL_RISK_CURRENT_EVIDENCE_SCOPE}</li>
            </ul>
          </div>
          <div className="oprisk-executive__actions">
            <button
              type="button"
              className="oprisk-button"
              onClick={refreshWorkspace}
              disabled={isPending}
            >
              {isPending ? <LoaderCircle className="animate-spin" size={14} /> : <RefreshCw size={14} />}
              Yenile
            </button>
          </div>
        </div>
        {workspace.hasDevelopmentPilotData ? (
          <p className="oprisk-provenance-strip" data-testid="operational-risk-provenance-strip">
            <span className="oprisk-provenance-strip__badge">Örnek veri</span>
            {OPERATIONAL_RISK_DEVELOPMENT_BANNER}
          </p>
        ) : null}
        {!canManage ? (
          <p className="oprisk-readonly-note" data-testid="operational-risk-readonly-note">
            {OPERATIONAL_RISK_READONLY_NOTE}
          </p>
        ) : null}
        {feedback ? (
          <p className={`oprisk-feedback oprisk-feedback--${feedbackTone}`}>{feedback}</p>
        ) : null}
      </header>

      <section className="oprisk-kpi-grid" data-testid="operational-risk-kpi-grid" aria-label="Operasyonel risk göstergeleri">
        {kpis.map((kpi) => {
          const Icon = KPI_ICONS[kpi.id] ?? ShieldAlert;
          return (
            <article
              key={kpi.id}
              className={`oprisk-kpi-card ${resolveOperationalRiskKpiToneClass(kpi.tone)}`}
            >
              <div className="oprisk-kpi-card__header">
                <p className="oprisk-kpi-card__label">{kpi.label}</p>
                <span className="oprisk-kpi-card__icon" aria-hidden="true">
                  <Icon size={16} />
                </span>
              </div>
              <p className="oprisk-kpi-card__value">{kpi.value}</p>
              <p className="oprisk-kpi-card__detail">{kpi.detail}</p>
              <div className="oprisk-kpi-card__footer">
                <span>{kpi.scopeLabel}</span>
                <span className="oprisk-kpi-card__badge">{kpi.stateBadge}</span>
                <span>{kpi.evidenceLabel}</span>
              </div>
            </article>
          );
        })}
      </section>

      <div
        className="oprisk-tab-rail"
        role="tablist"
        aria-label="Operasyonel risk sekmeleri"
        data-testid="operational-risk-tab-rail"
      >
        {OPERATIONAL_RISK_TAB_OPTIONS.map((tab, index) => (
          <button
            key={tab.id}
            type="button"
            role="tab"
            aria-selected={activeTab === tab.id}
            className={`oprisk-tab${activeTab === tab.id ? " oprisk-tab--active" : ""}`}
            onClick={() => setActiveTab(tab.id)}
            onKeyDown={(event) => onTabKeyDown(event, index)}
          >
            {tab.label}
          </button>
        ))}
      </div>

      {activeTab === "overview" ? (
        <div className="oprisk-panel-grid oprisk-panel-grid--overview">
          <section className="oprisk-panel oprisk-panel--distribution">
            <h2 className="oprisk-panel__title">Açık vaka dağılımı</h2>
            {workspace.distribution.total > 0 ? (
              <div className="oprisk-donut" data-testid="operational-risk-distribution-donut">
                <svg viewBox="0 0 160 160" role="img" aria-label="Kritiklik dağılımı">
                  {donutArcs.map((arc) => (
                    <path key={arc.key} d={arc.path} fill={arc.color} />
                  ))}
                  <text x="80" y="74" textAnchor="middle" fontSize="12" fill="#64748b">
                    Açık vaka
                  </text>
                  <text x="80" y="92" textAnchor="middle" fontSize="20" fontWeight="700" fill="#12263a">
                    {workspace.distribution.centerLabel}
                  </text>
                </svg>
                <div className="oprisk-donut__legend">
                  {workspace.distribution.slices.map((slice) => (
                    <div key={slice.key} className="oprisk-donut__legend-item">
                      <span style={{ display: "inline-flex", alignItems: "center", gap: "0.35rem" }}>
                        <span className="oprisk-donut__swatch" style={{ background: slice.color }} />
                        {slice.label}
                      </span>
                      <span>{formatOperationalRiskCount(slice.count)} ({formatOperationalRiskPercent(slice.ratio)})</span>
                    </div>
                  ))}
                </div>
              </div>
            ) : (
              <p className="oprisk-empty">{workspace.distribution.unavailableReason}</p>
            )}
          </section>

          <section className="oprisk-panel oprisk-panel--source-coverage">
            <h2 className="oprisk-panel__title">Kaynak modül kapsamı</h2>
            <div className="oprisk-source-grid" data-testid="operational-risk-source-coverage">
              {workspace.sourceCoverage.map((source) => (
                <div key={source.module} className={sourceStateClass(source.state)}>
                  <div className="oprisk-source-card__top">
                    <p className="oprisk-source-card__title">{source.label}</p>
                    <span className={`oprisk-source-card__badge oprisk-source-card__badge--${source.state}`}>
                      {source.evidenceLabel}
                    </span>
                  </div>
                  <p className="oprisk-source-card__detail">{source.detail}</p>
                </div>
              ))}
            </div>
          </section>

          <section className="oprisk-panel oprisk-panel--priority">
            <h2 className="oprisk-panel__title">Öncelikli vakalar</h2>
            <div className="oprisk-priority-list" data-testid="operational-risk-priority-list">
              {workspace.priorityCases.length > 0 ? workspace.priorityCases.map((item) => (
                <PriorityRow key={item.id} item={item} onDetail={() => openCaseDetail(item.id)} />
              )) : <p className="oprisk-empty">Öncelikli açık vaka yok</p>}
            </div>
          </section>

          <section className="oprisk-panel oprisk-panel--detection-summary">
            <h2 className="oprisk-panel__title">Tespit özeti</h2>
            {workspace.detectionSummary.hasRuns ? (
              <div className="oprisk-outcome-grid" data-testid="operational-risk-detection-summary">
                {Object.entries(workspace.detectionSummary.outcomeTotals).map(([key, value]) => (
                  <div key={key} className="oprisk-outcome-card">
                    <p className="oprisk-outcome-card__value">{formatOperationalRiskCount(value)}</p>
                    <p className="oprisk-outcome-card__label">
                      {getOperationalRiskDetectionOutcomeLabel(key)}
                    </p>
                  </div>
                ))}
              </div>
            ) : (
              <div className="oprisk-detection-empty" data-testid="operational-risk-detection-summary-empty">
                <p className="oprisk-empty-state__eyebrow">TESPİT DURUMU</p>
                <div className="oprisk-empty-state__icon" aria-hidden="true">
                  <ClipboardList size={20} />
                </div>
                <h3>Bu dönemde kontrol çalışması yok</h3>
                <p>Sonuçlar, yetkili bir kontrol çalışması tamamlandığında burada gösterilir.</p>
                {canManage ? (
                  <button type="button" className="oprisk-button" onClick={() => setActiveTab("runs")}>
                    Kontrol Çalışmaları
                  </button>
                ) : null}
              </div>
            )}
          </section>
        </div>
      ) : null}

      {activeTab === "cases" ? (
        <section className="oprisk-panel">
          <h2 className="oprisk-panel__title">Risk vakaları</h2>
          <div className="oprisk-toolbar">
            <input
              type="search"
              placeholder="Referans veya başlık ara"
              value={searchQuery}
              onChange={(event) => setSearchQuery(event.target.value)}
              aria-label="Vaka ara"
            />
            <select value={statusFilter} onChange={(event) => setStatusFilter(event.target.value)} aria-label="Durum filtresi">
              <option value="">Tüm durumlar</option>
              {[...new Set(workspace.cases.map((item) => item.lifecycleStatus))].map((status) => (
                <option key={status} value={status}>{getOperationalRiskLifecycleLabel(status)}</option>
              ))}
            </select>
            <select value={severityFilter} onChange={(event) => setSeverityFilter(event.target.value)} aria-label="Kritiklik filtresi">
              <option value="">Tüm şiddetler</option>
              {[...new Set(workspace.cases.map((item) => item.severityBand).filter(Boolean))].map((band) => (
                <option key={band} value={band ?? ""}>{getOperationalRiskSeverityLabel(band)}</option>
              ))}
            </select>
            <select value={categoryFilter} onChange={(event) => setCategoryFilter(event.target.value)} aria-label="Kategori filtresi">
              <option value="">Tüm kategoriler</option>
              {[...new Set(workspace.cases.map((item) => item.category))].map((category) => {
                const label = workspace.cases.find((item) => item.category === category)?.categoryLabel ?? category;
                return <option key={category} value={category}>{label}</option>;
              })}
            </select>
            <select value={evidenceFilter} onChange={(event) => setEvidenceFilter(event.target.value)} aria-label="Kanıt durumu filtresi">
              <option value="">Tüm kanıt durumları</option>
              {[...new Set(workspace.cases.map((item) => item.evidenceState))].map((state) => (
                <option key={state} value={state}>{getOperationalRiskEvidenceLabel(state)}</option>
              ))}
            </select>
          </div>
          <div className="oprisk-table-wrap" tabIndex={0} role="region" aria-label="Risk vakaları (kaydırılabilir)">
            <table aria-label="Risk vakaları" className="oprisk-table">
              <thead>
                <tr>
                  <th>Başlık</th>
                  <th>Referans</th>
                  <th>Kritiklik</th>
                  <th>Durum</th>
                  <th>Kategori</th>
                  <th>Kapsam</th>
                  <th>Kaynak</th>
                  <th>Son kanıt</th>
                  <th />
                </tr>
              </thead>
              <tbody>
                {filteredCases.map((item) => (
                  <tr key={item.id}>
                    <td>{item.title}</td>
                    <td>{item.caseReference}</td>
                    <td><span className={severityBadgeClass(item.severityBand)}>{item.severityLabel}</span></td>
                    <td>{item.lifecycleLabel}</td>
                    <td>{item.categoryLabel}</td>
                    <td>{item.scopeLabel}</td>
                    <td>{item.sourceModuleLabel}</td>
                    <td>{item.lastObservedLabel}</td>
                    <td>
                      <button type="button" className="oprisk-button" onClick={() => openCaseDetail(item.id)}>
                        Detay
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      ) : null}

      {activeTab === "profiles" ? (
        <section className="oprisk-panel oprisk-profile-workspace" data-testid="operational-risk-profile-workspace">
          <h2 className="oprisk-panel__title">Tespit profilleri</h2>
          <div className="oprisk-profile-workspace__layout">
            <div className="oprisk-profile-rail" role="listbox" aria-label="Tespit profilleri">
              {workspace.profiles.map((profile) => (
                <button
                  key={profile.id}
                  type="button"
                  role="option"
                  aria-selected={selectedProfile?.id === profile.id}
                  className={`oprisk-profile-rail__item${selectedProfile?.id === profile.id ? " oprisk-profile-rail__item--active" : ""}`}
                  onClick={() => setSelectedProfileId(profile.id)}
                >
                  <span className="oprisk-profile-rail__name">{profile.displayName}</span>
                  <span className="oprisk-profile-rail__meta">{profile.profileCode}</span>
                  <span className="oprisk-badge">{profile.statusLabel}</span>
                </button>
              ))}
            </div>
            {selectedProfile ? (
              <ProfileHero
                profile={selectedProfile}
                canManage={canManage}
                onEdit={() => {
                  setProfileEditId(selectedProfile.id);
                  setProfileDisplayName(selectedProfile.displayName);
                  setProfileStatus(selectedProfile.status);
                }}
              />
            ) : (
              <p className="oprisk-empty">Profil bulunamadı</p>
            )}
          </div>
        </section>
      ) : null}

      {activeTab === "runs" ? (
        <div className="oprisk-runs-workspace" data-testid="operational-risk-runs-workspace">
          <section className="oprisk-panel oprisk-runs-composer">
            <h2 className="oprisk-panel__title">Yeni kontrol çalışması</h2>
            <p className="oprisk-panel__hint">
              Yalnızca onaylı profiller çalıştırılabilir. Değerlendirme penceresi seçilen kaynak kanıtı aralığını temsil eder.
            </p>
            {canManage ? (
              <div className="oprisk-toolbar oprisk-toolbar--composer">
                <select
                  value={effectiveRunProfileId}
                  onChange={(event) => setRunProfileId(event.target.value)}
                  aria-label="Onaylı profil seç"
                >
                  <option value="">Onaylı profil seçin</option>
                  {approvedProfiles.map((profile) => (
                    <option key={profile.id} value={profile.id}>{profile.displayName}</option>
                  ))}
                </select>
                <input type="datetime-local" value={runWindowStart} onChange={(event) => setRunWindowStart(event.target.value)} aria-label="Pencere başlangıcı" />
                <input type="datetime-local" value={runWindowEnd} onChange={(event) => setRunWindowEnd(event.target.value)} aria-label="Pencere bitişi" />
                <button
                  type="button"
                  className="oprisk-button oprisk-button--primary"
                  onClick={handleRunExecute}
                  disabled={isPending || !effectiveRunProfileId || !runWindowStart || !runWindowEnd}
                >
                  Kontrolü çalıştır
                </button>
              </div>
            ) : (
              <p className="oprisk-readonly-note">{OPERATIONAL_RISK_READONLY_NOTE}</p>
            )}
          </section>
          <section className="oprisk-panel oprisk-runs-history" data-testid="operational-risk-runs-history">
            <h2 className="oprisk-panel__title">Son kontrol çalışmaları</h2>
            {workspace.runs.length > 0 ? (
              <div className="oprisk-table-wrap" tabIndex={0} role="region" aria-label="Son kontrol çalışmaları (kaydırılabilir)">
                <table aria-label="Son kontrol çalışmaları" className="oprisk-table oprisk-table--runs">
                  <thead>
                    <tr>
                      <th>Referans</th>
                      <th>Profil</th>
                      <th>Pencere</th>
                      <th>Durum</th>
                      <th>Risk</th>
                      <th>Eşik içi</th>
                      <th>Yetersiz</th>
                      <th>Çelişki</th>
                      <th>Kanıt</th>
                      <th />
                    </tr>
                  </thead>
                  <tbody>
                    {workspace.runs.map((run) => (
                      <tr key={run.id}>
                        <td>{run.runReference}</td>
                        <td>{run.profileCode}</td>
                        <td>{run.windowLabel}</td>
                        <td>{run.statusLabel}</td>
                        <td>{formatOperationalRiskCount(run.outcomeCounts["risk-detected"] ?? 0)}</td>
                        <td>{formatOperationalRiskCount(run.outcomeCounts["within-threshold"] ?? 0)}</td>
                        <td>{formatOperationalRiskCount(run.outcomeCounts["insufficient-data"] ?? 0)}</td>
                        <td>{formatOperationalRiskCount(run.outcomeCounts["source-conflict"] ?? 0)}</td>
                        <td>{run.isPilotData ? "Örnek veri" : "Kayıt"}</td>
                        <td>
                          <button type="button" className="oprisk-button" onClick={() => openRunDetail(run.id)}>
                            Detay
                          </button>
                        </td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            ) : (
              <div className="oprisk-empty-state oprisk-empty-state--runs" data-testid="operational-risk-runs-empty">
                <div className="oprisk-empty-state__icon" aria-hidden="true">
                  <ShieldAlert size={22} />
                </div>
                <h3>Henüz kontrol çalışması yok</h3>
                <p>Onaylı bir profille çalıştırma tamamlandığında dönem, sonuç ve kanıt özeti burada görünür.</p>
              </div>
            )}
          </section>
        </div>
      ) : null}

      {selectedCaseId ? (
        <>
          <button type="button" className="oprisk-drawer-backdrop" aria-label="Detayı kapat" onClick={closeCaseDetail} />
          <aside className="oprisk-drawer" aria-label="Vaka detayı">
            <div className="oprisk-drawer__header">
              <h2>{caseDetail?.summary.title ?? "Vaka detayı"}</h2>
              <p className="oprisk-priority-row__meta">{caseDetail?.summary.caseReference ?? "—"}</p>
            </div>
            <div className="oprisk-drawer__body">
              {caseDetailStatus === "loading" ? (
                <div className="oprisk-case-detail-skeleton" data-testid="operational-risk-case-detail-loading">
                  <div className="oprisk-skeleton-line oprisk-skeleton-line--title" />
                  <div className="oprisk-skeleton-line" />
                  <div className="oprisk-skeleton-line" />
                  <div className="oprisk-skeleton-line oprisk-skeleton-line--short" />
                </div>
              ) : null}
              {caseDetailStatus === "error" ? (
                <div className="oprisk-error-banner" data-testid="operational-risk-case-detail-error" role="alert">
                  <p>{caseDetailError ?? "Vaka detayı yüklenemedi."}</p>
                  <button type="button" className="oprisk-button" onClick={retryCaseDetail}>Tekrar dene</button>
                </div>
              ) : null}
              {caseDetailStatus === "not-found" ? (
                <div className="oprisk-partial-error" data-testid="operational-risk-case-detail-error" role="alert">
                  <p>Vaka kaydı bulunamadı.</p>
                  <button type="button" className="oprisk-button" onClick={closeCaseDetail}>Kapat</button>
                </div>
              ) : null}
              {caseDetailStatus === "ready" && caseDetail ? (
                <div data-testid="operational-risk-case-detail-ready">
                  <DetailSection title="Özet">
                    <div className="oprisk-detail-grid">
                      <DetailField label="Kritiklik" value={caseDetail.summary.severityLabel} />
                      <DetailField label="Durum" value={caseDetail.summary.lifecycleLabel} />
                      <DetailField label="Kategori" value={caseDetail.summary.categoryLabel} />
                      <DetailField label="Kapsam" value={caseDetail.summary.scopeLabel} />
                      <DetailField label="Kaynak modül" value={caseDetail.summary.sourceModuleLabel} />
                      <DetailField label="Son kanıt" value={caseDetail.summary.lastObservedLabel} />
                      <DetailField label="Kanıt durumu" value={caseDetail.summary.evidenceLabel} />
                      <DetailField label="Sürüm" value={String(caseDetail.summary.version)} />
                    </div>
                    {caseDetail.description ? <p className="oprisk-detail-copy">{caseDetail.description}</p> : null}
                    <p className="oprisk-priority-row__meta">Kanıt kaynağı: {caseDetail.sourceProvenance}</p>
                  </DetailSection>
                  <DetailSection title="Belirleyici sürücüler">
                    <p className="oprisk-priority-row__title">
                      {caseDetail.leadingDriverLabel ?? "Belirtilmedi"}
                    </p>
                    {caseDetail.supportingDriverLabels.length > 0 ? (
                      <ul className="oprisk-driver-list">
                        {caseDetail.supportingDriverLabels.map((label) => (
                          <li key={label}>{label}</li>
                        ))}
                      </ul>
                    ) : (
                      <p className="oprisk-priority-row__meta">Destekleyici sürücü kaydı yok</p>
                    )}
                  </DetailSection>
                  <DetailSection title="Kanıt zaman çizelgesi">
                    <div className="oprisk-timeline">
                      {caseDetail.evidence.length > 0 ? caseDetail.evidence.map((entry) => (
                        <div key={entry.id} className="oprisk-timeline__item">
                          <p className="oprisk-priority-row__title">{entry.summary}</p>
                          <p className="oprisk-priority-row__meta">
                            {entry.observedLabel} · {entry.evidenceLabel} · {entry.sourceModuleLabel}
                          </p>
                          {entry.technicalAuditTokens.length > 0 ? (
                            <details className="oprisk-technical-details">
                              <summary>Teknik ayrıntılar</summary>
                              <ul className="oprisk-technical-details__list">
                                {entry.technicalAuditTokens.map((token) => (
                                  <li key={token}><code>{token}</code></li>
                                ))}
                              </ul>
                            </details>
                          ) : null}
                        </div>
                      )) : (
                        <p className="oprisk-priority-row__meta">Kanıt kaydı bulunamadı</p>
                      )}
                    </div>
                  </DetailSection>
                  <DetailSection title="Yaşam döngüsü geçmişi">
                    <div className="oprisk-timeline">
                      {caseDetail.statusHistory.map((entry) => (
                        <div key={entry.id} className="oprisk-timeline__item">
                          <p className="oprisk-priority-row__title">
                            {entry.previousLabel} → {entry.newLabel}
                          </p>
                          <p className="oprisk-priority-row__meta">{entry.occurredLabel}</p>
                          {entry.reasonNote ? <p className="oprisk-priority-row__meta">{entry.reasonNote}</p> : null}
                        </div>
                      ))}
                    </div>
                  </DetailSection>
                  {canManage && caseDetail.allowedTransitions.length > 0 ? (
                    <DetailSection title="Yaşam döngüsü işlemleri">
                      <div className="oprisk-toolbar">
                        <select
                          value={transitionTarget ?? ""}
                          onChange={(event) => setTransitionTarget(event.target.value || null)}
                          aria-label="Hedef durum"
                        >
                          <option value="">Durum seçin</option>
                          {caseDetail.allowedTransitions.map((status) => (
                            <option key={status} value={status}>{getOperationalRiskTransitionLabel(status)}</option>
                          ))}
                        </select>
                        <textarea
                          value={transitionNote}
                          onChange={(event) => setTransitionNote(event.target.value)}
                          placeholder="Gerekli not"
                          aria-label="Geçiş notu"
                        />
                        <button type="button" className="oprisk-button oprisk-button--primary" onClick={handleTransition} disabled={!transitionTarget || isPending}>
                          Onayla
                        </button>
                      </div>
                    </DetailSection>
                  ) : null}
                </div>
              ) : null}
            </div>
            <div className="oprisk-drawer__footer">
              <button type="button" className="oprisk-button" onClick={closeCaseDetail}>Kapat</button>
            </div>
          </aside>
        </>
      ) : null}

      {selectedRunId ? (
        <>
          <button type="button" className="oprisk-drawer-backdrop" aria-label="Çalıştırma detayını kapat" onClick={() => setSelectedRunId(null)} />
          <aside className="oprisk-drawer" aria-label="Kontrol çalışması detayı">
            <div className="oprisk-drawer__header">
              <h2>{runDetail?.summary.runReference ?? "Kontrol çalışması"}</h2>
              <p className="oprisk-priority-row__meta">{runDetail?.summary.profileCode}</p>
            </div>
            <div className="oprisk-drawer__body">
              {runDetail ? (
                Object.entries(runDetail.groupedOutcomes).map(([outcome, results]) => (
                  <DetailSection key={outcome} title={
                    outcome === "risk-detected" ? "Risk tespit edildi"
                      : outcome === "within-threshold" ? "Eşik içinde"
                        : outcome === "insufficient-data" ? "Veri yetersiz"
                          : "Kaynak uyuşmazlığı"
                  }>
                    {results.length > 0 ? results.map((result) => (
                      <div key={result.id} className="oprisk-timeline__item">
                        <p className="oprisk-priority-row__title">{result.categoryLabel}</p>
                        <p className="oprisk-priority-row__meta">
                          {result.sourceModuleLabel} · {result.leadingDriverLabel}
                        </p>
                        <p className="oprisk-priority-row__meta">{result.sourceReference}</p>
                      </div>
                    )) : <p className="oprisk-empty">Sonuç yok</p>}
                  </DetailSection>
                ))
              ) : <p className="oprisk-empty">Yükleniyor…</p>}
            </div>
            <div className="oprisk-drawer__footer">
              <button type="button" className="oprisk-button" onClick={() => setSelectedRunId(null)}>Kapat</button>
            </div>
          </aside>
        </>
      ) : null}

      {profileEditId ? (
        <>
          <button type="button" className="oprisk-drawer-backdrop" aria-label="Profil düzenlemeyi kapat" onClick={() => setProfileEditId(null)} />
          <aside className="oprisk-drawer" aria-label="Profil düzenleme">
            <div className="oprisk-drawer__header">
              <h2>Profil düzenle</h2>
            </div>
            <div className="oprisk-drawer__body">
              <label>
                Görünen ad
                <input value={profileDisplayName} onChange={(event) => setProfileDisplayName(event.target.value)} />
              </label>
              <label>
                Durum
                <select value={profileStatus} onChange={(event) => setProfileStatus(event.target.value)}>
                  {["draft", "approved", "inactive"].map((status) => (
                    <option key={status} value={status}>{getOperationalRiskProfileStatusLabel(status)}</option>
                  ))}
                </select>
              </label>
            </div>
            <div className="oprisk-drawer__footer">
              <button type="button" className="oprisk-button" onClick={() => setProfileEditId(null)}>İptal</button>
              <button type="button" className="oprisk-button oprisk-button--primary" onClick={handleProfileSave} disabled={isPending}>
                Kaydet
              </button>
            </div>
          </aside>
        </>
      ) : null}
    </div>
  );
}

function PriorityRow({
  item,
  onDetail,
}: Readonly<{ item: OperationalRiskCaseSummary; onDetail: () => void }>) {
  return (
    <div className="oprisk-priority-row">
      <div className="oprisk-priority-row__top">
        <p className="oprisk-priority-row__title">{item.title}</p>
        <span className={severityBadgeClass(item.severityBand)}>{item.severityLabel}</span>
      </div>
      <p className="oprisk-priority-row__meta">{item.caseReference}</p>
      <p className="oprisk-priority-row__meta">
        {item.lifecycleLabel} · {item.categoryLabel} · {item.sourceModuleLabel}
      </p>
      <p className="oprisk-priority-row__meta">Son kanıt: {item.lastObservedLabel}</p>
      <button type="button" className="oprisk-button" onClick={onDetail}>Detay</button>
    </div>
  );
}

function ProfileHero({
  profile,
  canManage,
  onEdit,
}: Readonly<{
  profile: OperationalRiskDetectionProfileSummary;
  canManage: boolean;
  onEdit: () => void;
}>) {
  return (
    <div className="oprisk-profile-hero" data-testid="operational-risk-profile-hero">
      <div className="oprisk-profile-hero__header">
        <div>
          <h3>{profile.displayName}</h3>
          <p className="oprisk-priority-row__meta">Kod: {profile.profileCode}</p>
        </div>
        <span className="oprisk-badge">{profile.statusLabel}</span>
      </div>
      <div className="oprisk-detail-grid oprisk-detail-grid--profile">
        <DetailField label="Sürüm" value={String(profile.version)} />
        <DetailField label="Aktif kurallar" value={`${formatOperationalRiskCount(profile.activeRuleCount)} / ${formatOperationalRiskCount(profile.ruleCount)}`} />
        <DetailField label="Kaynak modüller" value={profile.sourceModuleLabels.join(" · ")} />
        <DetailField label="Güncellik" value={profile.updatedLabel} />
        <DetailField label="Kanıt" value={profile.isPilotData ? "Örnek veri" : "Kayıt"} />
      </div>
      {canManage ? (
        <div className="oprisk-profile-hero__actions">
          <button type="button" className="oprisk-button" onClick={onEdit}>Düzenle</button>
        </div>
      ) : null}
      {profile.rules.length > 0 ? (
        <div className="oprisk-rule-grid" data-testid="operational-risk-profile-rule-grid">
          {profile.rules.map((rule) => (
            <article key={rule.id} className="oprisk-rule-card">
              <div className="oprisk-rule-card__top">
                <p className="oprisk-rule-card__title">{rule.categoryLabel}</p>
                <span className={`oprisk-badge${rule.isActive ? "" : " oprisk-badge--insufficient"}`}>
                  {rule.isActive ? "Aktif" : "Pasif"}
                </span>
              </div>
              <p className="oprisk-priority-row__meta">{rule.sourceModuleLabel}</p>
              <p className="oprisk-priority-row__meta">Kod: {rule.ruleCode}</p>
              <p className="oprisk-priority-row__meta">
                {rule.thresholdVarianceBand !== null
                  ? `Eşik bandı: ${formatOperationalRiskCount(rule.thresholdVarianceBand)}`
                  : "Eşik bilgisi yok"}
              </p>
              <p className="oprisk-priority-row__meta">{rule.driverLabel}</p>
            </article>
          ))}
        </div>
      ) : (
        <p className="oprisk-empty" data-testid="operational-risk-profile-rules-unavailable">
          Kural detayı API yanıtında bulunmuyor; yalnızca özet görüntüleniyor.
        </p>
      )}
    </div>
  );
}

function DetailField({
  label,
  value,
}: Readonly<{ label: string; value: string }>) {
  return (
    <div className="oprisk-detail-field">
      <p className="oprisk-detail-field__label">{label}</p>
      <p className="oprisk-detail-field__value">{value}</p>
    </div>
  );
}

function DetailSection({
  title,
  children,
}: Readonly<{ title: string; children: React.ReactNode }>) {
  return (
    <section className="oprisk-detail-section">
      <h3>{title}</h3>
      {children}
    </section>
  );
}
