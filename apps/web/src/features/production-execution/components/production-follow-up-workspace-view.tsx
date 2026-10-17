"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useState, useTransition } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import {
  AlertTriangle,
  ClipboardList,
  ExternalLink,
  Factory,
  FileWarning,
  LoaderCircle,
  RefreshCw,
  ShieldAlert,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import { QualityDashboardKpi } from "@/features/quality-control/components/quality-dashboard-kpi";
import type {
  ProductionFollowUpDetail,
  ProductionFollowUpWorkspacePayload,
} from "../domain/production-follow-up";
import {
  acknowledgeProductionFollowUpAction,
  createProductionFollowUpTaskDraftAction,
  refreshProductionFollowUpWorkspaceAction,
} from "../server/production-follow-up-actions";
import {
  getProductionExceptionSeverityLabel,
  getProductionExceptionTypeLabel,
  getProductionFollowUpStatusLabel,
  getProductionFollowUpTaskButtonLabel,
  PRODUCTION_FOLLOW_UP_TASK_DISCLAIMER,
} from "../presentation/production-follow-up-labels";
import { customerMessage } from "@/lib/presentation/customer-message";
import { InlineUnavailable } from "@/components/ui/state-page";
import { enumLabel } from "@/lib/presentation/enum-labels";

type ProductionFollowUpWorkspaceViewProps = Readonly<{
  workspace: ProductionFollowUpWorkspacePayload;
  initialException?: ProductionFollowUpDetail;
  exceptionError?: string;
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

export function ProductionFollowUpWorkspaceView({
  workspace,
  initialException,
  exceptionError,
}: ProductionFollowUpWorkspaceViewProps) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [isPending, startTransition] = useTransition();
  const [actionError, setActionError] = useState<string | null>(null);
  const [exception, setException] = useState(initialException);

  useEffect(() => {
    setException(initialException);
  }, [initialException]);

  const selectedExceptionId =
    searchParams.get("exception") ?? initialException?.id ?? workspace.exceptions[0]?.id ?? "";
  const selectedRow = useMemo(
    () => workspace.exceptions.find((row) => row.id === selectedExceptionId),
    [workspace.exceptions, selectedExceptionId],
  );

  const selectException = useCallback(
    (exceptionId: string) => {
      const params = new URLSearchParams(searchParams.toString());
      params.set("exception", exceptionId);
      router.push(`${pathname}?${params.toString()}`);
    },
    [pathname, router, searchParams],
  );

  const handleRefresh = () => {
    startTransition(async () => {
      setActionError(null);
      await refreshProductionFollowUpWorkspaceAction();
      router.refresh();
    });
  };

  const handleCreateTask = () => {
    if (!exception?.id) return;
    startTransition(async () => {
      setActionError(null);
      const result = await createProductionFollowUpTaskDraftAction(exception.id);
      if (result.status === "failure") {
        setActionError(result.message);
        return;
      }
      router.push(`/ai-gorevleri/${result.taskId}`);
    });
  };

  const handleAcknowledge = () => {
    if (!exception?.id) return;
    startTransition(async () => {
      setActionError(null);
      const result = await acknowledgeProductionFollowUpAction(exception.id, exception.version);
      if (result.status === "failure") {
        setActionError(result.message);
        return;
      }
      router.refresh();
    });
  };

  const canCreateTask =
    workspace.capabilities.canCreateTask &&
    exception?.canCreateTaskDraft === true &&
    !exception.linkedAiTaskId;

  const taskButtonLabel = getProductionFollowUpTaskButtonLabel(exception?.hasInsufficientEvidence === true);

  if (workspace.exceptions.length === 0) {
    return (
      <div className="production-execution-empty" role="status" data-testid="production-follow-up-empty">
        <div className="production-execution-empty__icon">
          <FileWarning aria-hidden="true" />
        </div>
        <div>
          <p>TAKİP KAYDI YOK</p>
          <h2>Takip edilecek üretim istisnası yok</h2>
        </div>
        <p className="production-execution-empty__desc">Yetkili tesis kapsamında açık üretim istisnası bulunamadı.</p>
      </div>
    );
  }

  return (
    <div data-testid="production-follow-up-workspace">
      <p className="quality-readonly-note" data-testid="production-follow-up-readonly-note">
        Bu ekran yalnızca insan incelemeli görev taslağı oluşturur; otomatik üretim, makine, stok veya kalite kararı uygulanmaz.
      </p>

      <div className="quality-dashboard-kpi-grid" data-testid="production-follow-up-kpi-grid">
        <QualityDashboardKpi
          label="İnceleme gerekli"
          value={workspace.kpis.requiresReviewCount}
          description="Açık takip"
          tone="in-progress"
          icon={<AlertTriangle />}
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
          description="Yapay zekâ görev taslağı"
          tone="ready"
          icon={<ExternalLink />}
        />
        <QualityDashboardKpi
          label="Blokaj / gecikme"
          value={workspace.kpis.blockedOrDelayedCount}
          description="Kritik istisna sinyali"
          tone="paused"
          icon={<Factory />}
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
          data-testid="production-follow-up-refresh-button"
        >
          <RefreshCw aria-hidden="true" />
          Yenile
        </button>
      </div>

      {actionError ? (
        <div role="alert" className="quality-error-panel">
          <AlertTriangle aria-hidden="true" />
          <p>{customerMessage(actionError)}</p>
        </div>
      ) : null}

      <section className="quality-panel-grid">
        <Card className="quality-panel">
          <header className="quality-panel__header">
            <h2>Üretim istisna envanteri</h2>
            <p>Takip gerektiren üretim istisnaları</p>
          </header>
          <div className="quality-table-wrap" tabIndex={0} data-testid="production-follow-up-table">
            <table aria-label="Üretim istisna envanteri">
              <thead>
                <tr>
                  <th>Emir</th>
                  <th>İstisna türü</th>
                  <th>Takip durumu</th>
                  <th>Oluşturulma</th>
                </tr>
              </thead>
              <tbody>
                {workspace.exceptions.map((row) => (
                  <tr
                    key={row.id}
                    data-testid={`production-follow-up-row-${row.id}`}
                    aria-selected={row.id === selectedExceptionId}
                  >
                    <td>
                      <button
                        type="button"
                        className="quality-table-link"
                        onClick={() => selectException(row.id)}
                      >
                        {row.productionOrderReference || "—"}
                      </button>
                      <div className="quality-table-meta">{getProductionExceptionTypeLabel(row.exceptionType)}</div>
                    </td>
                    <td>{getProductionExceptionSeverityLabel(row.severity)}</td>
                    <td>{getProductionFollowUpStatusLabel(row.followUpStatus)}</td>
                    <td>{formatDateTime(row.raisedAtUtc)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </Card>

        <Card className="quality-panel" data-testid="production-follow-up-detail-panel">
          <header className="quality-panel__header">
            <h2>Üretim kanıtı ve takip</h2>
            <p>{selectedRow ? selectedRow.productionOrderReference : "Kayıt seçin"}</p>
          </header>

          {exceptionError ? (
            <p role="alert" data-testid="production-follow-up-detail-error">{customerMessage(exceptionError)}</p>
          ) : !exception ? (
            <InlineUnavailable subject="Seçili üretim istisnası" testId="production-follow-up-detail-empty" />
          ) : (
            <div data-testid="production-follow-up-detail-ready">
              <dl className="quality-detail-grid">
                <div><dt>Üretim emri</dt><dd>{exception.productionOrderReference}</dd></div>
                <div><dt>Emir başlığı</dt><dd>{exception.productionOrderTitle}</dd></div>
                <div><dt>Emir durumu</dt><dd>{enumLabel(exception.productionOrderStatus)}</dd></div>
                <div><dt>İstisna türü</dt><dd>{getProductionExceptionTypeLabel(exception.exceptionType)}</dd></div>
                <div><dt>Kritiklik</dt><dd>{getProductionExceptionSeverityLabel(exception.severity)}</dd></div>
                <div><dt>Takip durumu</dt><dd>{getProductionFollowUpStatusLabel(exception.followUpStatus)}</dd></div>
                <div><dt>Makine</dt><dd>{exception.machineReference ?? "—"}</dd></div>
                <div><dt>Kalıp</dt><dd>{exception.moldReference ?? "—"}</dd></div>
                <div><dt>Ürün</dt><dd>{exception.productReference ?? "—"}</dd></div>
                <div><dt>Oluşturulma</dt><dd>{formatDateTime(exception.raisedAtUtc)}</dd></div>
              </dl>

              <section aria-labelledby="production-evidence-heading">
                <h3 id="production-evidence-heading">Kanıt özeti</h3>
                {exception.hasInsufficientEvidence ? (
                  <p data-testid="production-follow-up-insufficient-evidence">
                    Kanıt yetersiz veya çelişkili — yalnızca veri inceleme görev taslağı oluşturulabilir.
                  </p>
                ) : (
                  <p>{exception.evidenceSummary || exception.message || "—"}</p>
                )}
              </section>

              {exception.auditEvents.length > 0 ? (
                <section aria-labelledby="production-audit-heading">
                  <h3 id="production-audit-heading">Takip denetim kaydı</h3>
                  <ul className="quality-audit-list">
                    {exception.auditEvents.map((event) => (
                      <li key={`${event.eventType}-${event.occurredAtUtc}`}>
                        <strong>{enumLabel(event.eventType)}</strong> — {event.actor} ({formatDateTime(event.occurredAtUtc)})
                      </li>
                    ))}
                  </ul>
                </section>
              ) : null}

              <p
                className="quality-automation-disclaimer"
                data-testid="production-follow-up-automation-disclaimer"
              >
                <ShieldAlert aria-hidden="true" />
                {workspace.automationDisclaimer || PRODUCTION_FOLLOW_UP_TASK_DISCLAIMER}
              </p>

              <div className="quality-action-row">
                {canCreateTask ? (
                  <button
                    type="button"
                    onClick={handleCreateTask}
                    disabled={isPending}
                    data-testid="production-follow-up-create-task-button"
                  >
                    {isPending ? <LoaderCircle aria-hidden="true" className="spin" /> : null}
                    {taskButtonLabel}
                  </button>
                ) : null}

                {exception.linkedAiTaskId ? (
                  <Link
                    href={`/ai-gorevleri/${exception.linkedAiTaskId}`}
                    data-testid="production-follow-up-open-linked-task"
                  >
                    <ExternalLink aria-hidden="true" />
                    Bağlı görevi aç
                  </Link>
                ) : null}

                {workspace.capabilities.canManage &&
                exception.followUpStatus === "requires-review" ? (
                  <button
                    type="button"
                    onClick={handleAcknowledge}
                    disabled={isPending}
                    data-testid="production-follow-up-acknowledge-button"
                  >
                    Görüldü olarak işaretle
                  </button>
                ) : null}

                <Link
                  href={`/uretim/${exception.productionOrderId}`}
                  data-testid="production-follow-up-order-link"
                >
                  Üretim emrini aç
                </Link>
              </div>

              {exception.linkedAiTaskId ? (
                <div data-testid="production-follow-up-linked-task-state">
                  <Badge tone="neutral">Bağlı görev oluşturuldu</Badge>
                </div>
              ) : null}
            </div>
          )}
        </Card>
      </section>
    </div>
  );
}
