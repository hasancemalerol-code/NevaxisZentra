"use client";

import Link from "next/link";
import {
  useCallback,
  useEffect,
  useMemo,
  useState,
  useTransition,
  type FormEvent,
} from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import { LoaderCircle, RefreshCw } from "lucide-react";

import { ContextPanel } from "@/components/workspace/context-panel";
import { StatusStrip } from "@/components/workspace/status-strip";
import { WorkspaceHeader } from "@/components/workspace/workspace-header";

import {
  getWorksQueueCount,
  getWorksQueueLabel,
  resolveWorksQueueId,
  WORKS_QUEUE_OPTIONS,
  type WorksQueueId,
} from "../domain/works-queues";
import {
  formatWorksApprovalStatus,
  formatWorksBlockerStatus,
  resolveWorksDisplayBadgeClass,
  resolveWorksPriorityBadgeClass,
  resolveWorksRowToneClass,
} from "../domain/works-state-projection";
import type {
  WorksCaseDetailSnapshot,
  WorksListItem,
  WorksWorkspacePagePayload,
} from "../domain/works-types";
import { decideWorkApprovalAction, loadCaseDetailAction } from "../server/works-actions";

type WorksWorkspaceViewProps = Readonly<{
  page: WorksWorkspacePagePayload;
}>;

function formatDateTime(value: string | null | undefined): string {
  if (!value) return "—";
  return new Date(value).toLocaleString("tr-TR");
}

function formatNumber(value: number | null | undefined, digits = 0): string {
  if (value == null || Number.isNaN(value)) return "—";
  return value.toFixed(digits);
}

export function WorksWorkspaceView({ page: initialPage }: WorksWorkspaceViewProps) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [page, setPage] = useState(initialPage);
  const [caseDetail, setCaseDetail] = useState<WorksCaseDetailSnapshot | null>(initialPage.caseDetail);
  const [caseDetailStatus, setCaseDetailStatus] = useState<"idle" | "loading" | "ready" | "error">(
    initialPage.caseDetail ? "ready" : "idle",
  );
  const [caseDetailError, setCaseDetailError] = useState<string | null>(null);
  const [searchDraft, setSearchDraft] = useState(initialPage.search);
  const [feedback, setFeedback] = useState<string | null>(null);
  const [feedbackTone, setFeedbackTone] = useState<"success" | "error">("success");
  const [isPending, startTransition] = useTransition();

  const activeQueue = useMemo(() => {
    const queue = searchParams.get("kuyruk");
    return resolveWorksQueueId(queue);
  }, [searchParams]);

  const selectedCaseId = searchParams.get("is") ?? page.selectedCaseId ?? null;
  const searchQuery = searchParams.get("ara") ?? "";

  useEffect(() => {
    setPage(initialPage);
    setCaseDetail(initialPage.caseDetail);
    setCaseDetailStatus(initialPage.caseDetail ? "ready" : "idle");
    setSearchDraft(initialPage.search);
  }, [initialPage]);

  const replaceParams = useCallback((mutate: (params: URLSearchParams) => void) => {
    const params = new URLSearchParams(searchParams.toString());
    mutate(params);
    const query = params.toString();
    router.replace(query ? `${pathname}?${query}` : pathname, { scroll: false });
  }, [pathname, router, searchParams]);

  const selectQueue = useCallback((queueId: WorksQueueId) => {
    replaceParams((params) => {
      params.set("kuyruk", queueId);
    });
  }, [replaceParams]);

  const selectCase = useCallback((caseId: string) => {
    replaceParams((params) => {
      params.set("is", caseId);
    });
  }, [replaceParams]);

  const clearCaseSelection = useCallback(() => {
    replaceParams((params) => {
      params.delete("is");
    });
  }, [replaceParams]);

  const submitSearch = useCallback((event: FormEvent<HTMLFormElement>) => {
    event.preventDefault();
    replaceParams((params) => {
      const trimmed = searchDraft.trim();
      if (trimmed) {
        params.set("ara", trimmed);
      } else {
        params.delete("ara");
      }
    });
  }, [replaceParams, searchDraft]);

  const refreshCaseDetail = useCallback((caseId: string) => {
    setCaseDetailStatus("loading");
    setCaseDetailError(null);
    void (async () => {
      const result = await loadCaseDetailAction(caseId);
      if (result.status === "failure") {
        setCaseDetailStatus("error");
        setCaseDetailError(result.message);
        return;
      }
      setCaseDetail(result.detail);
      setCaseDetailStatus("ready");
    })();
  }, []);

  useEffect(() => {
    if (!selectedCaseId) {
      setCaseDetail(null);
      setCaseDetailStatus("idle");
      setCaseDetailError(null);
      return;
    }

    if (initialPage.selectedCaseId === selectedCaseId && initialPage.caseDetail) {
      setCaseDetail(initialPage.caseDetail);
      setCaseDetailStatus("ready");
      return;
    }

    refreshCaseDetail(selectedCaseId);
  }, [initialPage.caseDetail, initialPage.selectedCaseId, refreshCaseDetail, selectedCaseId]);

  const handleApprovalDecision = useCallback((
    approvalId: string,
    decision: "approved" | "rejected" | "modified",
    concurrencyToken: number,
  ) => {
    startTransition(async () => {
      const result = await decideWorkApprovalAction(approvalId, decision, concurrencyToken);
      if (result.status === "failure") {
        setFeedback(result.message);
        setFeedbackTone("error");
        return;
      }

      setFeedback(decision === "approved" ? "Onay kaydedildi." : "Karar kaydedildi.");
      setFeedbackTone("success");
      router.refresh();
      if (selectedCaseId) {
        refreshCaseDetail(selectedCaseId);
      }
    });
  }, [refreshCaseDetail, router, selectedCaseId]);

  const selectedItem = useMemo(() => {
    if (!selectedCaseId) return null;
    return page.workspace.items.find((item) => item.id === selectedCaseId) ?? caseDetail?.summary ?? null;
  }, [caseDetail?.summary, page.workspace.items, selectedCaseId]);

  const queueLabel = getWorksQueueLabel(activeQueue);

  return (
    <div className="works-workspace" data-testid="works-workspace">
      <WorkspaceHeader
        context="İŞLER"
        title="Operasyonel iş akışı"
        meta="İş · Sorun · Onay · Engel · Sonuç · Doğrulama"
        actions={(
          <button
            type="button"
            className="works-action-button"
            data-testid="works-refresh"
            disabled={isPending}
            onClick={() => router.refresh()}
          >
            {isPending ? <LoaderCircle aria-hidden="true" className="spin" size={14} /> : <RefreshCw aria-hidden="true" size={14} />}
            {" "}
            Yenile
          </button>
        )}
      />

      <StatusStrip
        aria-label="İş özeti"
        items={[
          { label: "Kuyruk", value: queueLabel },
          { label: "Kayıt", value: page.workspace.items.length },
          { label: "Onay", value: page.workspace.queueCounts.pendingApproval },
          { label: "Engel", value: page.workspace.queueCounts.blocked },
          { label: "Doğrulama", value: page.workspace.queueCounts.verificationPending },
        ]}
      />

      {feedback ? (
        <p className="works-feedback" data-testid="works-feedback" data-tone={feedbackTone}>
          {feedback}
        </p>
      ) : null}

      <div className="works-workspace__grid">
        <aside className="works-workspace__left" data-testid="works-queue-nav">
          <section className="fact-section">
            <header className="fact-section__header">
              <h2 className="fact-section__title">Kuyruklar</h2>
            </header>
            <div className="fact-section__body works-queue-nav">
              {WORKS_QUEUE_OPTIONS.map((option) => {
                const count = getWorksQueueCount(page.workspace.queueCounts, option.id);
                return (
                  <button
                    key={option.id}
                    type="button"
                    className="works-queue-nav__item"
                    data-testid={`works-queue-${option.id}`}
                    data-active={activeQueue === option.id ? "true" : "false"}
                    onClick={() => selectQueue(option.id)}
                  >
                    <span className="works-queue-nav__label">
                      <strong>{option.label}</strong>
                      <span>{option.description}</span>
                    </span>
                    <span className="works-queue-nav__count">{count}</span>
                  </button>
                );
              })}
            </div>
          </section>
        </aside>

        <main className="works-workspace__center" data-testid="works-list-panel">
          <section className="fact-section">
            <header className="fact-section__header">
              <h2 className="fact-section__title">{queueLabel}</h2>
            </header>
            <div className="fact-section__body">
              <div className="works-toolbar">
                <form onSubmit={submitSearch} data-testid="works-search-form">
                  <input
                    type="search"
                    className="works-search"
                    data-testid="works-search-input"
                    placeholder="İş, sorun veya korelasyon ara…"
                    value={searchDraft}
                    onChange={(event) => setSearchDraft(event.target.value)}
                    aria-label="İş ara"
                  />
                </form>
                {searchQuery ? (
                  <span data-testid="works-search-active">
                    Arama:
                    {" "}
                    <strong>{searchQuery}</strong>
                  </span>
                ) : null}
              </div>

              <div className="works-table-wrap">
                <table className="works-table" data-testid="works-table">
                  <thead>
                    <tr>
                      <th>İş / Sorun</th>
                      <th>Durum</th>
                      <th>Öncelik</th>
                      <th>Fabrika</th>
                      <th>Sahip</th>
                      <th>Sistem</th>
                      <th>Onay</th>
                      <th>Engel</th>
                      <th>Güncelleme</th>
                    </tr>
                  </thead>
                  <tbody>
                    {page.workspace.items.length === 0 ? (
                      <tr data-testid="works-table-empty">
                        <td colSpan={9}>Bu kuyrukta görüntülenecek iş yok.</td>
                      </tr>
                    ) : (
                      page.workspace.items.map((item) => (
                        <WorksTableRow
                          key={item.id}
                          item={item}
                          selected={selectedCaseId === item.id}
                          onSelect={() => selectCase(item.id)}
                        />
                      ))
                    )}
                  </tbody>
                </table>
              </div>
            </div>
          </section>
        </main>

        <aside className="works-workspace__right" data-testid="works-inspector">
          {!selectedCaseId ? (
            <div className="works-inspector-empty" data-testid="works-inspector-empty">
              Detay görmek için listeden bir iş seçin.
            </div>
          ) : caseDetailStatus === "loading" ? (
            <div className="works-inspector-empty" data-testid="works-inspector-loading">
              <LoaderCircle aria-hidden="true" className="spin" size={16} />
              {" "}
              İş detayı yükleniyor…
            </div>
          ) : caseDetailStatus === "error" ? (
            <div className="works-inspector-empty" data-testid="works-inspector-error" role="alert">
              {caseDetailError ?? "İş detayı yüklenemedi."}
            </div>
          ) : caseDetail ? (
            <WorksInspectorPanel
              detail={caseDetail}
              canDecideApproval={page.canDecideApproval}
              isPending={isPending}
              onApprovalDecision={handleApprovalDecision}
              onClose={clearCaseSelection}
            />
          ) : selectedItem ? (
            <WorksInspectorSummary item={selectedItem} onClose={clearCaseSelection} />
          ) : (
            <div className="works-inspector-empty" data-testid="works-inspector-not-found">
              Seçilen iş bulunamadı.
            </div>
          )}
        </aside>
      </div>
    </div>
  );
}

type WorksTableRowProps = Readonly<{
  item: WorksListItem;
  selected: boolean;
  onSelect: () => void;
}>;

function WorksTableRow({ item, selected, onSelect }: WorksTableRowProps) {
  return (
    <tr
      className={resolveWorksRowToneClass(item)}
      data-testid={`works-row-${item.id}`}
      data-selected={selected ? "true" : "false"}
      onClick={onSelect}
    >
      <td>
        <strong>{item.title}</strong>
        <div>{item.problemSummary}</div>
      </td>
      <td>
        <span className={resolveWorksDisplayBadgeClass(item.displayState)}>
          {item.displayState}
        </span>
      </td>
      <td>
        <span className={resolveWorksPriorityBadgeClass(item.priorityLabel)}>
          {item.priorityLabel}
        </span>
      </td>
      <td>{item.factoryContextLabel}</td>
      <td>{item.ownerLabel}</td>
      <td>{item.actingSystemLabel}</td>
      <td>{formatWorksApprovalStatus(item.hasPendingApproval)}</td>
      <td>{formatWorksBlockerStatus(item)}</td>
      <td>{item.lastUpdateLabel}</td>
    </tr>
  );
}

type WorksInspectorSummaryProps = Readonly<{
  item: WorksListItem;
  onClose: () => void;
}>;

function WorksInspectorSummary({ item, onClose }: WorksInspectorSummaryProps) {
  return (
    <div className="works-inspector" data-testid="works-inspector-summary">
      <article className="works-problem-hero">
        <p className="works-problem-hero__eyebrow">Sorun</p>
        <h2>{item.outcomeLabel || item.problemSummary}</h2>
        <p>{item.title}</p>
      </article>
      <StatusStrip
        aria-label="İş durumu"
        items={[
          { label: "Durum", value: item.displayState },
          { label: "Öncelik", value: item.priorityLabel },
          { label: "Sonuç", value: item.outcomeLabel || "—" },
          { label: "Doğrulama", value: item.verificationState || "—" },
        ]}
      />
      <button type="button" className="works-action-button" onClick={onClose}>
        Seçimi temizle
      </button>
    </div>
  );
}

type WorksInspectorPanelProps = Readonly<{
  detail: WorksCaseDetailSnapshot;
  canDecideApproval: boolean;
  isPending: boolean;
  onApprovalDecision: (
    approvalId: string,
    decision: "approved" | "rejected" | "modified",
    concurrencyToken: number,
  ) => void;
  onClose: () => void;
}>;

function WorksInspectorPanel({
  detail,
  canDecideApproval,
  isPending,
  onApprovalDecision,
  onClose,
}: WorksInspectorPanelProps) {
  const summary = detail.summary;
  const approval = detail.pendingApproval;

  return (
    <div className="works-inspector" data-testid="works-inspector-detail">
      <article className="works-problem-hero" data-testid="works-problem-hero">
        <p className="works-problem-hero__eyebrow">Sorun / Sonuç</p>
        <h2>{summary.outcomeLabel || summary.problemSummary}</h2>
        <p>{summary.title}</p>
      </article>

      <StatusStrip
        aria-label="İş durumu"
        items={[
          { label: "Durum", value: summary.displayState },
          { label: "Öncelik", value: summary.priorityLabel },
          { label: "Sonraki adım", value: detail.nextStepLabel },
          { label: "Doğrulama", value: summary.verificationState || "—" },
        ]}
      />

      <section className="fact-section">
        <header className="fact-section__header">
          <h2 className="fact-section__title">Etki</h2>
        </header>
        <div className="fact-section__body">
          <ContextPanel
            title="Operasyonel etki"
            rows={[
              { label: "Özet", value: detail.impactSummary },
              { label: "Beklenen etki", value: summary.expectedImpact },
              { label: "Tür", value: summary.workTypeLabel },
              { label: "Alan", value: summary.primaryDomain },
            ]}
          />
        </div>
      </section>

      <section className="fact-section">
        <header className="fact-section__header">
          <h2 className="fact-section__title">Sonraki adım</h2>
        </header>
        <div className="fact-section__body">
          <ContextPanel
            title="Aksiyon"
            rows={[
              { label: "Adım", value: detail.nextStepLabel },
              { label: "Aktif aksiyon", value: detail.activeActionLabel },
            ]}
          />
        </div>
      </section>

      {approval ? (
        <section className="fact-section" data-testid="works-approval-panel">
          <header className="fact-section__header">
            <h2 className="fact-section__title">Onay</h2>
          </header>
          <div className="fact-section__body">
            <ContextPanel
              title="Bekleyen onay"
              rows={[
                { label: "Aksiyon", value: approval.action },
                { label: "Gerekçe", value: approval.businessReason },
                { label: "Etki", value: approval.impact },
                { label: "Risk", value: approval.riskLevel },
              ]}
            />
            {canDecideApproval && detail.permittedActions.includes("approve") ? (
              <div className="works-actions">
                <button
                  type="button"
                  className="works-action-button"
                  data-variant="primary"
                  data-testid="works-approve"
                  disabled={isPending}
                  onClick={() => onApprovalDecision(approval.id, "approved", approval.concurrencyToken)}
                >
                  Onayla
                </button>
                <button
                  type="button"
                  className="works-action-button"
                  data-testid="works-reject"
                  disabled={isPending}
                  onClick={() => onApprovalDecision(approval.id, "rejected", approval.concurrencyToken)}
                >
                  Reddet
                </button>
                <button
                  type="button"
                  className="works-action-button"
                  data-testid="works-request-change"
                  disabled={isPending}
                  onClick={() => onApprovalDecision(approval.id, "modified", approval.concurrencyToken)}
                >
                  Değişiklik iste
                </button>
              </div>
            ) : (
              <p>Onay kararı için yetkiniz yok.</p>
            )}
          </div>
        </section>
      ) : null}

      {detail.blocker ? (
        <section className="fact-section" data-testid="works-blocker-panel">
          <header className="fact-section__header">
            <h2 className="fact-section__title">Engel</h2>
          </header>
          <div className="fact-section__body">
            <ContextPanel
              title="Aktif engel"
              rows={[
                { label: "Kod", value: detail.blocker.code },
                { label: "Etiket", value: detail.blocker.label },
                { label: "Başlangıç", value: formatDateTime(detail.blocker.sinceUtc) },
                { label: "Detay", value: detail.blocker.detail },
              ]}
            />
          </div>
        </section>
      ) : null}

      {detail.dependencies.length > 0 ? (
        <section className="fact-section" data-testid="works-dependencies">
          <header className="fact-section__header">
            <h2 className="fact-section__title">Bağımlılıklar</h2>
          </header>
          <div className="fact-section__body">
            {detail.dependencies.map((dependency) => (
              <ContextPanel
                key={`${dependency.label}-${dependency.relation}`}
                title={dependency.label}
                rows={[
                  { label: "İlişki", value: dependency.relation },
                  { label: "Detay", value: dependency.detail },
                ]}
              />
            ))}
          </div>
        </section>
      ) : null}

      {detail.timeline.length > 0 ? (
        <section className="fact-section" data-testid="works-timeline">
          <header className="fact-section__header">
            <h2 className="fact-section__title">Zaman çizelgesi</h2>
          </header>
          <div className="fact-section__body works-timeline">
            {detail.timeline.map((entry) => (
              <article key={`${entry.code}-${entry.occurredAtUtc}`} className="works-timeline__item">
                <strong>{entry.label}</strong>
                <time dateTime={entry.occurredAtUtc}>{formatDateTime(entry.occurredAtUtc)}</time>
                <span>{entry.detail}</span>
              </article>
            ))}
          </div>
        </section>
      ) : null}

      {detail.verification ? (
        <section className="fact-section" data-testid="works-verification">
          <header className="fact-section__header">
            <h2 className="fact-section__title">Doğrulama</h2>
          </header>
          <div className="fact-section__body">
            <ContextPanel
              title="Önce / Sonra"
              rows={[
                { label: "Sonuç", value: detail.verification.outcomeLabel },
                { label: "Önce", value: formatNumber(detail.verification.beforeValue, 1) },
                { label: "Sonra", value: formatNumber(detail.verification.afterValue, 1) },
                { label: "Durum", value: detail.verification.verificationState },
                { label: "Doğrulayan", value: detail.verification.verifiedByLabel },
                { label: "Tarih", value: formatDateTime(detail.verification.verifiedAtUtc) },
              ]}
            />
          </div>
        </section>
      ) : null}

      {detail.evidence.length > 0 ? (
        <section className="fact-section" data-testid="works-evidence">
          <header className="fact-section__header">
            <h2 className="fact-section__title">Kanıt</h2>
          </header>
          <div className="fact-section__body">
            {detail.evidence.map((item) => (
              <ContextPanel
                key={`${item.kind}-${item.label}-${item.occurredAtUtc ?? "na"}`}
                title={item.label}
                rows={[
                  { label: "Tür", value: item.kind },
                  { label: "Detay", value: item.detail },
                  { label: "Zaman", value: formatDateTime(item.occurredAtUtc) },
                ]}
              />
            ))}
          </div>
        </section>
      ) : null}

      {detail.relatedWork.length > 0 ? (
        <section className="fact-section" data-testid="works-related-work">
          <header className="fact-section__header">
            <h2 className="fact-section__title">İlgili işler</h2>
          </header>
          <div className="fact-section__body works-related-list">
            {detail.relatedWork.map((item) => (
              <Link
                key={item.id}
                href={`/isler?is=${item.id}`}
                data-testid={`works-related-${item.id}`}
              >
                <strong>{item.title}</strong>
                <span>{item.relation}</span>
                <span className={resolveWorksDisplayBadgeClass(item.displayState)}>{item.displayState}</span>
              </Link>
            ))}
          </div>
        </section>
      ) : null}

      {detail.scenarioContext ? (
        <section className="fact-section" data-testid="works-scenario-context">
          <header className="fact-section__header">
            <h2 className="fact-section__title">Senaryo bağlamı</h2>
          </header>
          <div className="fact-section__body">
            <ContextPanel
              title="Kurtarma senaryosu"
              rows={[
                { label: "Hat", value: detail.scenarioContext.lineCode },
                { label: "Faz", value: detail.scenarioContext.currentPhase },
                { label: "Seçim", value: detail.scenarioContext.selectedOptionLabel },
                { label: "Doğrulama", value: detail.scenarioContext.verificationOutcome },
                { label: "Baseline", value: formatNumber(detail.scenarioContext.baselineThroughput, 1) },
                { label: "Tahmin", value: formatNumber(detail.scenarioContext.predictedThroughput, 1) },
                { label: "Gerçek", value: formatNumber(detail.scenarioContext.actualThroughput, 1) },
              ]}
            />
            <Link href={detail.scenarioContext.scenarioHref} data-testid="works-scenario-link">
              Senaryo kurtarma çalışma alanı
            </Link>
          </div>
        </section>
      ) : null}

      <section className="fact-section" data-testid="works-links">
        <header className="fact-section__header">
          <h2 className="fact-section__title">Bağlantılar</h2>
        </header>
        <div className="fact-section__body works-actions">
          {detail.factoryHref ? (
            <Link href={detail.factoryHref} className="works-action-button" data-testid="works-factory-link">
              Fabrika
            </Link>
          ) : null}
          {detail.planHref ? (
            <Link href={detail.planHref} className="works-action-button" data-testid="works-plan-link">
              Plan
            </Link>
          ) : null}
          {detail.scenarioHref ? (
            <Link href={detail.scenarioHref} className="works-action-button" data-testid="works-scenario-href">
              Senaryo
            </Link>
          ) : null}
          {detail.digitalWorkforceHref ? (
            <Link href={detail.digitalWorkforceHref} className="works-action-button" data-testid="works-digital-workforce-link">
              Dijital iş gücü
            </Link>
          ) : null}
          <button type="button" className="works-action-button" onClick={onClose}>
            Seçimi temizle
          </button>
        </div>
      </section>

      {detail.permittedActions.length > 0 ? (
        <section className="fact-section" data-testid="works-permitted-actions">
          <header className="fact-section__header">
            <h2 className="fact-section__title">İzinli aksiyonlar</h2>
          </header>
          <div className="fact-section__body">
            <ContextPanel
              title="Sistem aksiyonları"
              rows={detail.permittedActions.map((action) => ({
                label: action,
                value: "İzinli",
              }))}
            />
          </div>
        </section>
      ) : null}
    </div>
  );
}
