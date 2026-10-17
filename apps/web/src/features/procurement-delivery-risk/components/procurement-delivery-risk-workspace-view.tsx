"use client";

import { keepCode } from "@/lib/presentation/format";
import Link from "next/link";
import { useCallback, useEffect, useMemo, useState, useTransition } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import {
  AlertTriangle,
  Clock,
  ExternalLink,
  FileWarning,
  LoaderCircle,
  PackageSearch,
  RefreshCw,
  ShieldAlert,
  Truck,
} from "lucide-react";

import { Card } from "@/components/ui/card";
import { QualityDashboardKpi } from "@/features/quality-control/components/quality-dashboard-kpi";
import type {
  ProcurementDeliveryRiskDetail,
  ProcurementDeliveryRiskHistoryEntry,
  ProcurementDeliveryRiskWorkspacePayload,
} from "../domain/procurement-delivery-risk";
import {
  acknowledgeProcurementDeliveryRiskFollowUpAction,
  createProcurementDeliveryRiskTaskDraftAction,
  evaluateProcurementDeliveryRiskAction,
  refreshProcurementDeliveryRiskWorkspaceAction,
} from "../server/procurement-delivery-risk-actions";
import {
  getProcurementDeliveryRiskDriverLabel,
  getProcurementDeliveryRiskEvidenceFieldLabel,
  getProcurementDeliveryRiskFollowUpAuditEventLabel,
  getProcurementDeliveryRiskFollowUpStatusLabel,
  getProcurementDeliveryRiskFreshnessLabel,
  getProcurementDeliveryRiskResultBadgeClass,
  getProcurementDeliveryRiskResultLabel,
} from "../presentation/procurement-delivery-risk-labels";
import { customerMessage } from "@/lib/presentation/customer-message";
import { formatEvidenceValue } from "@/lib/presentation/format";
import { enumLabel } from "@/lib/presentation/enum-labels";

type ProcurementDeliveryRiskWorkspaceViewProps = Readonly<{
  workspace: ProcurementDeliveryRiskWorkspacePayload;
  initialAssessment?: ProcurementDeliveryRiskDetail;
  assessmentError?: string;
  history?: readonly ProcurementDeliveryRiskHistoryEntry[];
  historyError?: string;
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

function formatQuantity(value?: number, unit?: string): string {
  if (value === undefined || value === null) return "—";
  return `${value.toLocaleString("tr-TR")}${unit ? ` ${unit}` : ""}`;
}

export function ProcurementDeliveryRiskWorkspaceView({
  workspace,
  initialAssessment,
  assessmentError,
  history,
  historyError,
}: ProcurementDeliveryRiskWorkspaceViewProps) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [isPending, startTransition] = useTransition();
  const [actionError, setActionError] = useState<string | null>(null);
  const [assessment, setAssessment] = useState(initialAssessment);

  useEffect(() => {
    setAssessment(initialAssessment);
  }, [initialAssessment]);

  const selectedAssessmentId =
    searchParams.get("assessment") ??
    initialAssessment?.id ??
    workspace.assessments[0]?.id ??
    "";

  const _selectedRow = useMemo(
    () => workspace.assessments.find((row) => row.id === selectedAssessmentId),
    [workspace.assessments, selectedAssessmentId],
  );

  const selectAssessment = useCallback(
    (assessmentId: string) => {
      const params = new URLSearchParams(searchParams.toString());
      params.set("assessment", assessmentId);
      router.push(`${pathname}?${params.toString()}`);
    },
    [pathname, router, searchParams],
  );

  const handleRefresh = () => {
    startTransition(async () => {
      setActionError(null);
      await refreshProcurementDeliveryRiskWorkspaceAction();
      router.refresh();
    });
  };

  const handleEvaluate = () => {
    if (!assessment?.purchaseOrderLineId) return;
    startTransition(async () => {
      setActionError(null);
      const result = await evaluateProcurementDeliveryRiskAction(
        assessment.purchaseOrderLineId,
        crypto.randomUUID(),
      );
      if (result.status === "failure") {
        setActionError(result.message);
        return;
      }
      const params = new URLSearchParams(searchParams.toString());
      params.set("assessment", result.assessmentId);
      router.push(`${pathname}?${params.toString()}`);
      router.refresh();
    });
  };

  const handleCreateTask = () => {
    if (!assessment?.id) return;
    startTransition(async () => {
      setActionError(null);
      const result = await createProcurementDeliveryRiskTaskDraftAction(assessment.id);
      if (result.status === "failure") {
        setActionError(result.message);
        return;
      }
      router.push(`/ai-gorevleri/${result.taskId}`);
    });
  };

  const handleAcknowledge = () => {
    if (!assessment?.id) return;
    startTransition(async () => {
      setActionError(null);
      const result = await acknowledgeProcurementDeliveryRiskFollowUpAction(
        assessment.id,
        assessment.version,
      );
      if (result.status === "failure") {
        setActionError(result.message);
        return;
      }
      router.refresh();
    });
  };

  const canEvaluate =
    workspace.capabilities.canManage && Boolean(assessment?.purchaseOrderLineId);

  const canCreateTask =
    workspace.capabilities.canCreateTask &&
    assessment?.canCreateTaskDraft === true &&
    !assessment.linkedAiTaskId;

  const canAcknowledge =
    workspace.capabilities.canManage &&
    assessment?.followUpStatus === "requires-review";

  const taskButtonLabel = assessment?.hasInsufficientEvidence
    ? "Veri inceleme görev taslağı oluştur"
    : "Tedarik inceleme görev taslağı oluştur";

  if (workspace.assessments.length === 0) {
    return (
      <div className="quality-empty-panel" data-testid="procurement-delivery-risk-empty">
        <FileWarning aria-hidden="true" />
        <strong>Değerlendirilmiş tedarik riski yok</strong>
        <p>Yetkili tesis kapsamında tedarik teslimat riski değerlendirmesi yok.</p>
      </div>
    );
  }

  return (
    <div data-testid="procurement-delivery-risk-workspace">
      <p className="quality-readonly-note" data-testid="procurement-delivery-risk-readonly-note">
        {workspace.automationDisclaimer}
      </p>
      <p className="quality-readonly-note" data-testid="procurement-delivery-risk-automation-disclaimer">
        Otomatik satın alma, tedarikçi iletişimi, stok veya üretim kararı uygulanmaz.
      </p>

      <div className="quality-dashboard-kpi-grid" data-testid="procurement-delivery-risk-kpi-grid">
        <QualityDashboardKpi
          label="Gecikmiş"
          value={workspace.kpis.overdueCount}
          description="Taahhüt tarihi geçmiş"
          tone="paused"
          icon={<AlertTriangle />}
        />
        <QualityDashboardKpi
          label="Teslimat riski"
          value={workspace.kpis.deliveryRiskCount}
          description="Kısmi teslim durumu"
          tone="in-progress"
          icon={<Truck />}
        />
        <QualityDashboardKpi
          label="Plan dahilinde"
          value={workspace.kpis.onTrackCount}
          description="Taahhüt içinde açık miktar"
          tone="ready"
          icon={<PackageSearch />}
        />
        <QualityDashboardKpi
          label="Yetersiz veri"
          value={workspace.kpis.insufficientDataCount}
          description="Taahhüt veya miktar eksik"
          tone="in-progress"
          icon={<ShieldAlert />}
        />
        <QualityDashboardKpi
          label="Kaynak tazeliği"
          value={formatDateTime(workspace.kpis.latestEvaluatedAtUtc)}
          description="Son değerlendirme"
          tone="ready"
          icon={<Clock />}
        />
      </div>

      <div className="quality-workspace-toolbar">
        <div className="quality-workspace-toolbar__actions">
          <button
            type="button"
            className="quality-workspace-toolbar__button"
            onClick={handleRefresh}
            disabled={isPending}
            data-testid="procurement-delivery-risk-refresh"
          >
            {isPending ? <LoaderCircle className="animate-spin" aria-hidden="true" /> : <RefreshCw aria-hidden="true" />}
            Yenile
          </button>
          {canEvaluate ? (
            <button
              type="button"
              className="quality-workspace-toolbar__button quality-workspace-toolbar__button--primary"
              onClick={handleEvaluate}
              disabled={isPending}
              data-testid="procurement-delivery-risk-evaluate"
            >
              Değerlendir
            </button>
          ) : null}
        </div>
      </div>

      {actionError ? (
        <div role="alert" className="material-registry-error" data-testid="procurement-delivery-risk-action-error">
          {customerMessage(actionError)}
        </div>
      ) : null}

      <div className="quality-workspace-split">
        <Card className="quality-workspace-table-card" tabIndex={0}>
          <table aria-label="Kaynak tazeliği" className="quality-workspace-table" data-testid="procurement-delivery-risk-table">
            <thead>
              <tr>
                <th>Tedarikçi</th>
                <th>Satın alma</th>
                <th>Taahhüt</th>
                <th>Açık miktar</th>
                <th>Sonuç</th>
                <th>Takip</th>
              </tr>
            </thead>
            <tbody>
              {workspace.assessments.map((row) => (
                <tr
                  key={row.id}
                  className={row.id === selectedAssessmentId ? "is-selected" : undefined}
                  onClick={() => selectAssessment(row.id)}
                  data-testid={`procurement-delivery-risk-row-${row.id}`}
                >
                  <td>{row.supplierName ?? row.externalSupplierCode ?? "—"}</td>
                  <td>{keepCode(row.purchaseOrderNumber)}</td>
                  <td>{formatDateTime(row.commitmentDateUtc)}</td>
                  <td>{formatQuantity(row.openQuantity, row.unit)}</td>
                  <td>
                    <span className={getProcurementDeliveryRiskResultBadgeClass(row.result)}>
                      {getProcurementDeliveryRiskResultLabel(row.result)}
                    </span>
                  </td>
                  <td>{getProcurementDeliveryRiskFollowUpStatusLabel(row.followUpStatus)}</td>
                </tr>
              ))}
            </tbody>
          </table>
        </Card>

        <Card className="quality-workspace-detail-card" data-testid="procurement-delivery-risk-detail">
          {!selectedAssessmentId ? (
            <div className="quality-empty-panel" data-testid="procurement-delivery-risk-no-selection">
              <p>Detay görmek için bir değerlendirme seçin.</p>
            </div>
          ) : assessmentError ? (
            <div role="alert" className="material-registry-error" data-testid="procurement-delivery-risk-detail-error">
              {customerMessage(assessmentError)}
            </div>
          ) : assessment ? (
            <>
              <header className="quality-workspace-detail-header">
                <div>
                  <p className="quality-workspace-detail-eyebrow">Tedarik teslimat riski</p>
                  <h2>{assessment.purchaseOrderNumber}</h2>
                  <p>{assessment.supplierName ?? assessment.externalSupplierCode ?? "Tedarikçi bilinmiyor"}</p>
                </div>
                <span className={getProcurementDeliveryRiskResultBadgeClass(assessment.result)}>
                  {getProcurementDeliveryRiskResultLabel(assessment.result)}
                </span>
              </header>

              <dl className="quality-workspace-detail-grid">
                <div><dt>Sipariş durumu</dt><dd>{enumLabel(assessment.orderStatus)}</dd></div>
                <div><dt>Satır</dt><dd>{assessment.lineNumber}</dd></div>
                <div><dt>Taahhüt tarihi</dt><dd>{formatDateTime(assessment.commitmentDateUtc)}</dd></div>
                <div><dt>Teslim alınan</dt><dd>{formatQuantity(assessment.receivedQuantity, assessment.unit)}</dd></div>
                <div><dt>Açık miktar</dt><dd>{formatQuantity(assessment.openQuantity, assessment.unit)}</dd></div>
                <div><dt>Kaynak tazeliği</dt><dd>{getProcurementDeliveryRiskFreshnessLabel(assessment.sourceFreshness)}</dd></div>
                <div><dt>Değerlendirme</dt><dd>{formatDateTime(assessment.evaluatedAtUtc)}</dd></div>
                <div><dt>Takip durumu</dt><dd>{getProcurementDeliveryRiskFollowUpStatusLabel(assessment.followUpStatus)}</dd></div>
              </dl>

              <div className="quality-workspace-detail-actions">
                {canCreateTask ? (
                  <button
                    type="button"
                    className="quality-workspace-toolbar__button quality-workspace-toolbar__button--primary"
                    onClick={handleCreateTask}
                    disabled={isPending}
                    data-testid="procurement-delivery-risk-create-task-button"
                  >
                    {taskButtonLabel}
                  </button>
                ) : null}
                {assessment.linkedAiTaskId ? (
                  <Link
                    href={`/ai-gorevleri/${assessment.linkedAiTaskId}`}
                    className="quality-workspace-toolbar__button"
                    data-testid="procurement-delivery-risk-linked-task-link"
                  >
                    <ExternalLink aria-hidden="true" />
                    Bağlı görevi aç
                  </Link>
                ) : null}
                {canAcknowledge ? (
                  <button
                    type="button"
                    className="quality-workspace-toolbar__button"
                    onClick={handleAcknowledge}
                    disabled={isPending}
                    data-testid="procurement-delivery-risk-acknowledge-button"
                  >
                    Görüldü olarak işaretle
                  </button>
                ) : null}
              </div>

              {assessment.drivers.length > 0 ? (
                <section>
                  <h3>Kanıt gerekçeleri</h3>
                  <ul className="quality-workspace-driver-list">
                    {assessment.drivers.map((driver) => (
                      <li key={driver.code}>
                        <strong>{getProcurementDeliveryRiskDriverLabel(driver.code)}</strong>
                        <span>{driver.summary}</span>
                      </li>
                    ))}
                  </ul>
                </section>
              ) : null}

              {assessment.evidence.length > 0 ? (
                <section>
                  <h3>Kaynak kanıtları</h3>
                  <ul className="quality-workspace-evidence-list">
                    {assessment.evidence.map((item) => (
                      <li key={`${item.field}-${item.source ?? "unknown"}`}>
                        <span>{getProcurementDeliveryRiskEvidenceFieldLabel(item.field)}</span>
                        <span>{item.isUnknown ? "—" : formatEvidenceValue(item.value)}</span>
                      </li>
                    ))}
                  </ul>
                </section>
              ) : null}

              {assessment.auditEvents.length > 0 ? (
                <section data-testid="procurement-delivery-risk-follow-up-audit">
                  <h3>Takip geçmişi</h3>
                  <ul className="quality-workspace-history-list">
                    {assessment.auditEvents.map((event) => (
                      <li key={`${event.eventType}-${event.occurredAtUtc}`}>
                        <span>{formatDateTime(event.occurredAtUtc)}</span>
                        <span>{getProcurementDeliveryRiskFollowUpAuditEventLabel(event.eventType)}</span>
                        <span>{event.actor}</span>
                      </li>
                    ))}
                  </ul>
                </section>
              ) : null}

              {history && history.length > 0 ? (
                <section data-testid="procurement-delivery-risk-history">
                  <h3>Değerlendirme geçmişi</h3>
                  {historyError ? <p>{customerMessage(historyError)}</p> : null}
                  <ul className="quality-workspace-history-list">
                    {history.map((entry) => (
                      <li key={entry.id}>
                        <span>{formatDateTime(entry.evaluatedAtUtc)}</span>
                        <span>{getProcurementDeliveryRiskResultLabel(entry.result)}</span>
                      </li>
                    ))}
                  </ul>
                </section>
              ) : null}
            </>
          ) : (
            <div className="quality-empty-panel" data-testid="procurement-delivery-risk-detail-loading">
              <LoaderCircle className="animate-spin" aria-hidden="true" />
              <p>Detay yükleniyor…</p>
            </div>
          )}
        </Card>
      </div>
    </div>
  );
}
