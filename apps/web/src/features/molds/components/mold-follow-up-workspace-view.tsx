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
  Wrench,
} from "lucide-react";

import { Card } from "@/components/ui/card";
import { QualityDashboardKpi } from "@/features/quality-control/components/quality-dashboard-kpi";
import type {
  MoldFollowUpDetail,
  MoldFollowUpWorkspacePayload,
} from "../domain/mold-follow-up";
import {
  acknowledgeMoldFollowUpAction,
  createMoldFollowUpTaskDraftAction,
  refreshMoldFollowUpWorkspaceAction,
} from "../server/mold-follow-up-actions";
import { getMoldLifecycleStateLabel } from "../presentation/mold-labels";
import {
  getMoldFollowUpStatusLabel,
  getMoldFollowUpTaskButtonLabel,
  MOLD_FOLLOW_UP_TASK_DISCLAIMER,
} from "../presentation/mold-follow-up-labels";
import { customerMessage } from "@/lib/presentation/customer-message";
import { InlineUnavailable } from "@/components/ui/state-page";

type MoldFollowUpWorkspaceViewProps = Readonly<{
  workspace: MoldFollowUpWorkspacePayload;
  initialMold?: MoldFollowUpDetail;
  moldError?: string;
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

function formatCounter(current?: number, nominal?: number): string {
  const currentLabel = current?.toLocaleString("tr-TR") ?? "—";
  const nominalLabel = nominal?.toLocaleString("tr-TR") ?? "—";
  return `${currentLabel} / ${nominalLabel}`;
}

export function MoldFollowUpWorkspaceView({
  workspace,
  initialMold,
  moldError,
}: MoldFollowUpWorkspaceViewProps) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [isPending, startTransition] = useTransition();
  const [actionError, setActionError] = useState<string | null>(null);
  const [mold, setMold] = useState(initialMold);

  useEffect(() => {
    setMold(initialMold);
  }, [initialMold]);

  const selectedMoldId =
    searchParams.get("mold") ?? initialMold?.id ?? workspace.molds[0]?.id ?? "";
  const selectedRow = useMemo(
    () => workspace.molds.find((row) => row.id === selectedMoldId),
    [workspace.molds, selectedMoldId],
  );

  const selectMold = useCallback(
    (moldId: string) => {
      const params = new URLSearchParams(searchParams.toString());
      params.set("mold", moldId);
      router.push(`${pathname}?${params.toString()}`);
    },
    [pathname, router, searchParams],
  );

  const handleRefresh = () => {
    startTransition(async () => {
      setActionError(null);
      await refreshMoldFollowUpWorkspaceAction();
      router.refresh();
    });
  };

  const handleCreateTask = () => {
    if (!mold?.id) return;
    startTransition(async () => {
      setActionError(null);
      const result = await createMoldFollowUpTaskDraftAction(mold.id);
      if (result.status === "failure") {
        setActionError(result.message);
        return;
      }
      router.push(`/ai-gorevleri/${result.taskId}`);
    });
  };

  const handleAcknowledge = () => {
    if (!mold?.id) return;
    startTransition(async () => {
      setActionError(null);
      const result = await acknowledgeMoldFollowUpAction(mold.id, mold.version);
      if (result.status === "failure") {
        setActionError(result.message);
        return;
      }
      router.refresh();
    });
  };

  const canCreateTask =
    workspace.capabilities.canCreateTask &&
    mold?.canCreateTaskDraft === true &&
    !mold.linkedAiTaskId;

  const taskButtonLabel = getMoldFollowUpTaskButtonLabel(mold?.hasInsufficientEvidence === true);

  if (workspace.molds.length === 0) {
    return (
      <div className="quality-empty-panel" data-testid="mold-follow-up-empty">
        <FileWarning aria-hidden="true" />
        <strong>Takip edilecek kalıp kaydı yok</strong>
        <p>Yetkili tesis kapsamında bakım veya sayaç eşiği takibi gereken kalıp bulunamadı.</p>
      </div>
    );
  }

  return (
    <div data-testid="mold-follow-up-workspace">
      <p className="quality-readonly-note" data-testid="mold-follow-up-readonly-note">
        Bu ekran yalnızca insan incelemeli görev taslağı oluşturur; otomatik bakım veya yaşam döngüsü kararı uygulanmaz.
      </p>

      <div className="quality-dashboard-kpi-grid" data-testid="mold-follow-up-kpi-grid">
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
          label="Bakımda"
          value={workspace.kpis.maintenanceStateCount}
          description="Yaşam döngüsü"
          tone="paused"
          icon={<Wrench />}
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
          data-testid="mold-follow-up-refresh-button"
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
            <h2>Kalıp envanteri</h2>
            <p>Takip gerektiren kalıp kayıtları</p>
          </header>
          <div className="quality-table-wrap" tabIndex={0} data-testid="mold-follow-up-table">
            <table aria-label="Kalıp envanteri">
              <thead>
                <tr>
                  <th>Kalıp</th>
                  <th>Yaşam döngüsü</th>
                  <th>Takip durumu</th>
                  <th>Güncelleme</th>
                </tr>
              </thead>
              <tbody>
                {workspace.molds.map((row) => (
                  <tr
                    key={row.id}
                    data-testid={`mold-follow-up-row-${row.id}`}
                    aria-selected={row.id === selectedMoldId}
                  >
                    <td>
                      <button
                        type="button"
                        className="quality-table-link"
                        onClick={() => selectMold(row.id)}
                      >
                        {row.code.trim() || "—"}
                      </button>
                      <div className="quality-table-meta">{row.name}</div>
                    </td>
                    <td>{getMoldLifecycleStateLabel(row.lifecycleState)}</td>
                    <td>{getMoldFollowUpStatusLabel(row.followUpStatus)}</td>
                    <td>{formatDateTime(row.updatedAtUtc)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </Card>

        <Card className="quality-panel" data-testid="mold-follow-up-detail-panel">
          <header className="quality-panel__header">
            <h2>Kalıp kanıtı ve takip</h2>
            <p>{selectedRow ? (selectedRow.code.trim() || selectedRow.name) : "Kayıt seçin"}</p>
          </header>

          {moldError ? (
            <p role="alert" data-testid="mold-follow-up-detail-error">{customerMessage(moldError)}</p>
          ) : !mold ? (
            <InlineUnavailable subject="Seçili kalıp kaydı" testId="mold-follow-up-detail-empty" />
          ) : (
            <div data-testid="mold-follow-up-detail-ready">
              <dl className="quality-detail-grid">
                <div><dt>Kalıp kodu</dt><dd>{mold.code.trim() || "—"}</dd></div>
                <div><dt>Ad</dt><dd>{mold.name}</dd></div>
                <div><dt>Yaşam döngüsü</dt><dd>{getMoldLifecycleStateLabel(mold.lifecycleState)}</dd></div>
                <div><dt>Takip durumu</dt><dd>{getMoldFollowUpStatusLabel(mold.followUpStatus)}</dd></div>
                <div><dt>Vuruş sayacı</dt><dd>{formatCounter(mold.currentShotCount, mold.nominalShotLife)}</dd></div>
                <div><dt>Sayaç kaydı</dt><dd>{formatDateTime(mold.counterRecordedAtUtc)}</dd></div>
                <div><dt>Ürün referansı</dt><dd>{mold.primaryProductReference ?? "—"}</dd></div>
                <div><dt>Malzeme</dt><dd>{mold.materialFamilyReference ?? "—"}</dd></div>
              </dl>

              {mold.lifecycleEvents.length > 0 ? (
                <section aria-labelledby="mold-lifecycle-heading">
                  <h3 id="mold-lifecycle-heading">Yaşam döngüsü geçmişi</h3>
                  <ul className="quality-audit-list">
                    {mold.lifecycleEvents.map((event) => (
                      <li key={event.id}>
                        {getMoldLifecycleStateLabel(event.previousState)} → {getMoldLifecycleStateLabel(event.newState)}
                        {" — "}
                        {event.reason} ({event.actorDisplayName}, {formatDateTime(event.occurredAtUtc)})
                      </li>
                    ))}
                  </ul>
                </section>
              ) : null}

              <section aria-labelledby="mold-evidence-heading">
                <h3 id="mold-evidence-heading">Kanıt özeti</h3>
                {mold.hasInsufficientEvidence ? (
                  <p data-testid="mold-follow-up-insufficient-evidence">
                    Kanıt yetersiz veya çelişkili — yalnızca veri inceleme görev taslağı oluşturulabilir.
                  </p>
                ) : (
                  <p>{mold.evidenceSummary || "—"}</p>
                )}
              </section>

              {mold.auditEvents.length > 0 ? (
                <section aria-labelledby="mold-audit-heading">
                  <h3 id="mold-audit-heading">Takip denetim kaydı</h3>
                  <ul className="quality-audit-list">
                    {mold.auditEvents.map((event) => (
                      <li key={`${event.eventType}-${event.occurredAtUtc}`}>
                        <strong>{event.eventType}</strong> — {event.actor} ({formatDateTime(event.occurredAtUtc)})
                      </li>
                    ))}
                  </ul>
                </section>
              ) : null}

              <p
                className="quality-automation-disclaimer"
                data-testid="mold-follow-up-automation-disclaimer"
              >
                <ShieldAlert aria-hidden="true" />
                {workspace.automationDisclaimer || MOLD_FOLLOW_UP_TASK_DISCLAIMER}
              </p>

              <div className="quality-action-row">
                {canCreateTask ? (
                  <button
                    type="button"
                    onClick={handleCreateTask}
                    disabled={isPending}
                    data-testid="mold-follow-up-create-task-button"
                  >
                    {isPending ? (
                      <LoaderCircle aria-hidden="true" className="spin" />
                    ) : (
                      <ClipboardList aria-hidden="true" />
                    )}
                    {taskButtonLabel}
                  </button>
                ) : null}

                {mold.linkedAiTaskId ? (
                  <Link
                    href={`/ai-gorevleri/${mold.linkedAiTaskId}`}
                    data-testid="mold-follow-up-linked-task-link"
                  >
                    <ExternalLink aria-hidden="true" />
                    Bağlı görevi aç
                  </Link>
                ) : null}

                {workspace.capabilities.canManage &&
                mold.followUpStatus === "requires-review" ? (
                  <button
                    type="button"
                    onClick={handleAcknowledge}
                    disabled={isPending}
                    data-testid="mold-follow-up-acknowledge-button"
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
