"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useState, useTransition } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import {
  AlertTriangle,
  ClipboardList,
  ExternalLink,
  FileWarning,
  LoaderCircle,
  RefreshCw,
  ShieldAlert,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import type {
  QualityNcrFollowUpFinding,
  QualityNcrFollowUpWorkspacePayload,
} from "../domain/quality-ncr-follow-up";
import {
  acknowledgeQualityNcrFollowUpAction,
  createQualityNcrFollowUpTaskDraftAction,
  refreshQualityNcrFollowUpWorkspaceAction,
} from "../server/quality-ncr-follow-up-actions";
import {
  getQualityNcrFollowUpSeverityTone,
  getQualityNcrFollowUpStatusLabel,
  QUALITY_NCR_FOLLOW_UP_TASK_DISCLAIMER,
} from "../presentation/quality-ncr-follow-up-labels";
import { QualityDashboardKpi } from "./quality-dashboard-kpi";

type QualityNcrFollowUpWorkspaceViewProps = Readonly<{
  workspace: QualityNcrFollowUpWorkspacePayload;
  initialFinding?: QualityNcrFollowUpFinding;
  findingError?: string;
}>;

function formatDateTime(iso?: string): string {
  if (!iso) return "—";
  return new Date(iso).toLocaleString("tr-TR", {
    day: "2-digit",
    month: "short",
    year: "numeric",
    hour: "2-digit",
    minute: "2-digit",
  });
}

function SeverityBadge({ severity }: Readonly<{ severity: string }>) {
  const tone = getQualityNcrFollowUpSeverityTone(severity);
  const badgeTone = tone === "danger" ? "danger" : tone === "warning" ? "warning" : "neutral";
  return <Badge tone={badgeTone}>{severity}</Badge>;
}

export function QualityNcrFollowUpWorkspaceView({
  workspace,
  initialFinding,
  findingError,
}: QualityNcrFollowUpWorkspaceViewProps) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [isPending, startTransition] = useTransition();
  const [actionError, setActionError] = useState<string | null>(null);
  const [finding, setFinding] = useState(initialFinding);

  useEffect(() => {
    setFinding(initialFinding);
  }, [initialFinding]);

  const selectedFindingId =
    searchParams.get("finding") ?? initialFinding?.id ?? workspace.findings[0]?.id ?? "";
  const selectedRow = useMemo(
    () => workspace.findings.find((row) => row.id === selectedFindingId),
    [workspace.findings, selectedFindingId],
  );

  const selectFinding = useCallback(
    (findingId: string) => {
      const params = new URLSearchParams(searchParams.toString());
      params.set("finding", findingId);
      router.push(`${pathname}?${params.toString()}`);
    },
    [pathname, router, searchParams],
  );

  const handleRefresh = () => {
    startTransition(async () => {
      setActionError(null);
      await refreshQualityNcrFollowUpWorkspaceAction();
      router.refresh();
    });
  };

  const handleCreateTask = () => {
    if (!finding?.id) return;
    startTransition(async () => {
      setActionError(null);
      const result = await createQualityNcrFollowUpTaskDraftAction(finding.id);
      if (result.status === "failure") {
        setActionError(result.message);
        return;
      }
      router.push(`/ai-gorevleri/${result.taskId}`);
    });
  };

  const handleAcknowledge = () => {
    if (!finding?.id) return;
    startTransition(async () => {
      setActionError(null);
      const result = await acknowledgeQualityNcrFollowUpAction(finding.id, finding.version);
      if (result.status === "failure") {
        setActionError(result.message);
        return;
      }
      router.refresh();
    });
  };

  const canCreateTask =
    workspace.capabilities.canCreateTask &&
    finding?.canCreateTaskDraft === true &&
    !finding.linkedAiTaskId;

  const taskButtonLabel = finding?.hasInsufficientEvidence
    ? "Veri inceleme görev taslağı oluştur"
    : "Kalite inceleme görev taslağı oluştur";

  if (workspace.findings.length === 0) {
    return (
      <div className="quality-empty-panel" data-testid="quality-ncr-follow-up-empty">
        <FileWarning aria-hidden="true" />
        <h3>Takip edilecek uygunsuzluk kaydı yok</h3>
        <p>Yetkili tesis kapsamında açık NCR bulunamadı.</p>
      </div>
    );
  }

  return (
    <div data-testid="quality-ncr-follow-up-workspace">
      <p className="quality-readonly-note" data-testid="quality-ncr-follow-up-readonly-note">
        Bu ekran yalnızca insan incelemeli görev taslağı oluşturur; otomatik kalite kararı uygulanmaz.
      </p>

      <div className="quality-dashboard-kpi-grid" data-testid="quality-ncr-follow-up-kpi-grid">
        <QualityDashboardKpi
          label="İnceleme gerekli"
          value={workspace.kpis.requiresReviewCount}
          description="Açık takip"
          tone="in-progress"
          icon={<AlertTriangle />}
        />
        <QualityDashboardKpi
          label="Yüksek öncelik"
          value={workspace.kpis.highPriorityCount}
          description="Major/kritik NCR"
          tone="paused"
          icon={<ShieldAlert />}
        />
        <QualityDashboardKpi
          label="Görüldü"
          value={workspace.kpis.acknowledgedCount}
          description="Onaylanmış takip"
          tone="ready"
          icon={<ClipboardList />}
        />
        <QualityDashboardKpi
          label="Görev bağlantısı"
          value={workspace.kpis.taskLinkedCount}
          description="AI görev taslağı"
          tone="ready"
          icon={<ExternalLink />}
        />
        <QualityDashboardKpi
          label="Veri incelemesi"
          value={workspace.kpis.dataReviewCount}
          description="Yetersiz kanıt"
          tone="in-progress"
          icon={<FileWarning />}
        />
      </div>

      <div className="quality-workspace-toolbar">
        <button
          type="button"
          onClick={handleRefresh}
          disabled={isPending}
          data-testid="quality-ncr-follow-up-refresh-button"
        >
          <RefreshCw aria-hidden="true" />
          Yenile
        </button>
      </div>

      {actionError ? (
        <div role="alert" className="quality-error-panel">
          <AlertTriangle aria-hidden="true" />
          <p>{actionError}</p>
        </div>
      ) : null}

      <section className="quality-panel-grid">
        <Card className="quality-panel">
          <header className="quality-panel__header">
            <h2>Uygunsuzluk envanteri</h2>
            <p>Yetkili NCR takip kayıtları</p>
          </header>
          <div className="quality-table-wrap" data-testid="quality-ncr-follow-up-table">
            <table>
              <thead>
                <tr>
                  <th>NCR</th>
                  <th>Şiddet</th>
                  <th>Takip durumu</th>
                  <th>Tespit</th>
                </tr>
              </thead>
              <tbody>
                {workspace.findings.map((row) => (
                  <tr
                    key={row.id}
                    data-testid={`quality-ncr-follow-up-row-${row.id}`}
                    aria-selected={row.id === selectedFindingId}
                  >
                    <td>
                      <button
                        type="button"
                        className="quality-table-link"
                        onClick={() => selectFinding(row.id)}
                      >
                        {row.ncrNumber}
                      </button>
                      <div className="quality-table-meta">{row.title}</div>
                    </td>
                    <td><SeverityBadge severity={row.severity} /></td>
                    <td>{getQualityNcrFollowUpStatusLabel(row.followUpStatus)}</td>
                    <td>{formatDateTime(row.detectedAtUtc)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </Card>

        <Card className="quality-panel" data-testid="quality-ncr-follow-up-detail-panel">
          <header className="quality-panel__header">
            <h2>Uygunsuzluk kanıtı ve takip</h2>
            <p>{selectedRow ? selectedRow.ncrNumber : "Kayıt seçin"}</p>
          </header>

          {findingError ? (
            <p role="alert" data-testid="quality-ncr-follow-up-detail-error">{findingError}</p>
          ) : !finding ? (
            <p data-testid="quality-ncr-follow-up-detail-empty">Seçili uygunsuzluk kaydı yüklenemedi.</p>
          ) : (
            <div data-testid="quality-ncr-follow-up-detail-ready">
              <dl className="quality-detail-grid">
                <div><dt>NCR numarası</dt><dd>{finding.ncrNumber}</dd></div>
                <div><dt>Başlık</dt><dd>{finding.title}</dd></div>
                <div><dt>Şiddet</dt><dd><SeverityBadge severity={finding.severity} /></dd></div>
                <div><dt>NCR durumu</dt><dd>{finding.status}</dd></div>
                <div><dt>Takip durumu</dt><dd>{getQualityNcrFollowUpStatusLabel(finding.followUpStatus)}</dd></div>
                <div><dt>Ürün referansı</dt><dd>{finding.productReference ?? "—"}</dd></div>
                <div><dt>Operasyon</dt><dd>{finding.operationReference ?? "—"}</dd></div>
                <div><dt>Tespit zamanı</dt><dd>{formatDateTime(finding.detectedAtUtc)}</dd></div>
              </dl>

              <section aria-labelledby="ncr-description-heading">
                <h3 id="ncr-description-heading">Açıklama</h3>
                <p>{finding.description}</p>
              </section>

              <section aria-labelledby="ncr-evidence-heading">
                <h3 id="ncr-evidence-heading">Kanıt özeti</h3>
                {finding.hasInsufficientEvidence ? (
                  <p data-testid="quality-ncr-follow-up-insufficient-evidence">
                    Kanıt yetersiz veya çelişkili — yalnızca veri inceleme görev taslağı oluşturulabilir.
                  </p>
                ) : (
                  <p>{finding.evidenceSummary || "—"}</p>
                )}
              </section>

              {finding.auditEvents.length > 0 ? (
                <section aria-labelledby="ncr-audit-heading">
                  <h3 id="ncr-audit-heading">Takip denetim kaydı</h3>
                  <ul className="quality-audit-list">
                    {finding.auditEvents.map((event) => (
                      <li key={`${event.eventType}-${event.occurredAtUtc}`}>
                        <strong>{event.eventType}</strong> — {event.actor} ({formatDateTime(event.occurredAtUtc)})
                      </li>
                    ))}
                  </ul>
                </section>
              ) : null}

              <p
                className="quality-automation-disclaimer"
                data-testid="quality-ncr-follow-up-automation-disclaimer"
              >
                <ShieldAlert aria-hidden="true" />
                {workspace.automationDisclaimer || QUALITY_NCR_FOLLOW_UP_TASK_DISCLAIMER}
              </p>

              <div className="quality-action-row">
                {canCreateTask ? (
                  <button
                    type="button"
                    onClick={handleCreateTask}
                    disabled={isPending}
                    data-testid="quality-ncr-follow-up-create-task-button"
                  >
                    {isPending ? (
                      <LoaderCircle aria-hidden="true" className="spin" />
                    ) : (
                      <ClipboardList aria-hidden="true" />
                    )}
                    {taskButtonLabel}
                  </button>
                ) : null}

                {finding.linkedAiTaskId ? (
                  <Link
                    href={`/ai-gorevleri/${finding.linkedAiTaskId}`}
                    data-testid="quality-ncr-follow-up-linked-task-link"
                  >
                    <ExternalLink aria-hidden="true" />
                    Bağlı görevi aç
                  </Link>
                ) : null}

                {workspace.capabilities.canManage &&
                finding.followUpStatus === "requires-review" ? (
                  <button
                    type="button"
                    onClick={handleAcknowledge}
                    disabled={isPending}
                    data-testid="quality-ncr-follow-up-acknowledge-button"
                  >
                    Görüldü olarak işaretle
                  </button>
                ) : null}
              </div>
            </div>
          )}
        </Card>
      </section>
    </div>
  );
}
