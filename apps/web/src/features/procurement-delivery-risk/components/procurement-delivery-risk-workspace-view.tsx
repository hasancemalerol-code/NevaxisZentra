"use client";

import { useCallback, useEffect, useMemo, useState, useTransition } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import {
  AlertTriangle,
  Clock,
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
  evaluateProcurementDeliveryRiskAction,
  refreshProcurementDeliveryRiskWorkspaceAction,
} from "../server/procurement-delivery-risk-actions";
import {
  getProcurementDeliveryRiskDriverLabel,
  getProcurementDeliveryRiskEvidenceFieldLabel,
  getProcurementDeliveryRiskFreshnessLabel,
  getProcurementDeliveryRiskResultBadgeClass,
  getProcurementDeliveryRiskResultLabel,
  PROCUREMENT_DELIVERY_RISK_DISCLAIMER,
} from "../presentation/procurement-delivery-risk-labels";

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

  const selectedRow = useMemo(
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

  const canEvaluate =
    workspace.capabilities.canManage && Boolean(assessment?.purchaseOrderLineId);

  if (workspace.assessments.length === 0) {
    return (
      <div className="quality-empty-panel" data-testid="procurement-delivery-risk-empty">
        <FileWarning aria-hidden="true" />
        <h3>Değerlendirilmiş tedarik riski yok</h3>
        <p>Yetkili tesis kapsamında tedarik teslimat riski değerlendirmesi bulunamadı.</p>
      </div>
    );
  }

  return (
    <div data-testid="procurement-delivery-risk-workspace">
      <p className="quality-readonly-note" data-testid="procurement-delivery-risk-readonly-note">
        {PROCUREMENT_DELIVERY_RISK_DISCLAIMER}
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
        <span className="quality-workspace-toolbar__meta">Kural seti: {workspace.ruleSetVersion}</span>
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
          {actionError}
        </div>
      ) : null}

      <div className="quality-workspace-split">
        <Card className="quality-workspace-table-card">
          <table className="quality-workspace-table" data-testid="procurement-delivery-risk-table">
            <thead>
              <tr>
                <th>Tedarikçi</th>
                <th>Satın alma</th>
                <th>Taahhüt</th>
                <th>Açık miktar</th>
                <th>Sonuç</th>
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
                  <td>{row.purchaseOrderNumber}</td>
                  <td>{formatDateTime(row.commitmentDateUtc)}</td>
                  <td>{formatQuantity(row.openQuantity, row.unit)}</td>
                  <td>
                    <span className={getProcurementDeliveryRiskResultBadgeClass(row.result)}>
                      {getProcurementDeliveryRiskResultLabel(row.result)}
                    </span>
                  </td>
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
              {assessmentError}
            </div>
          ) : assessment ? (
            <>
              <header className="quality-workspace-detail-header">
                <div>
                  <p className="quality-workspace-detail-eyebrow">Tedarik teslimat riski</p>
                  <h3>{assessment.purchaseOrderNumber}</h3>
                  <p>{assessment.supplierName ?? assessment.externalSupplierCode ?? "Tedarikçi bilinmiyor"}</p>
                </div>
                <span className={getProcurementDeliveryRiskResultBadgeClass(assessment.result)}>
                  {getProcurementDeliveryRiskResultLabel(assessment.result)}
                </span>
              </header>

              <dl className="quality-workspace-detail-grid">
                <div><dt>Sipariş durumu</dt><dd>{assessment.orderStatus}</dd></div>
                <div><dt>Satır</dt><dd>{assessment.lineNumber}</dd></div>
                <div><dt>Taahhüt tarihi</dt><dd>{formatDateTime(assessment.commitmentDateUtc)}</dd></div>
                <div><dt>Teslim alınan</dt><dd>{formatQuantity(assessment.receivedQuantity, assessment.unit)}</dd></div>
                <div><dt>Açık miktar</dt><dd>{formatQuantity(assessment.openQuantity, assessment.unit)}</dd></div>
                <div><dt>Kaynak tazeliği</dt><dd>{getProcurementDeliveryRiskFreshnessLabel(assessment.sourceFreshness)}</dd></div>
                <div><dt>Değerlendirme</dt><dd>{formatDateTime(assessment.evaluatedAtUtc)}</dd></div>
              </dl>

              {assessment.drivers.length > 0 ? (
                <section>
                  <h4>Kanıt gerekçeleri</h4>
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
                  <h4>Kaynak kanıtları</h4>
                  <ul className="quality-workspace-evidence-list">
                    {assessment.evidence.map((item) => (
                      <li key={`${item.field}-${item.source ?? "unknown"}`}>
                        <span>{getProcurementDeliveryRiskEvidenceFieldLabel(item.field)}</span>
                        <span>{item.isUnknown ? "—" : (item.value ?? "—")}</span>
                      </li>
                    ))}
                  </ul>
                </section>
              ) : null}

              {history && history.length > 0 ? (
                <section data-testid="procurement-delivery-risk-history">
                  <h4>Değerlendirme geçmişi</h4>
                  {historyError ? <p>{historyError}</p> : null}
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
