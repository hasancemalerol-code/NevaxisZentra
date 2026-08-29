"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useState, useTransition } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import {
  AlertTriangle,
  Boxes,
  FileWarning,
  LoaderCircle,
  PackageSearch,
  RefreshCw,
  ShieldAlert,
  TriangleAlert,
} from "lucide-react";

import { Card } from "@/components/ui/card";
import { QualityDashboardKpi } from "@/features/quality-control/components/quality-dashboard-kpi";
import {
  formatMaterialGrams,
  getMaterialAssessmentDriverStateLabel,
  getMaterialAssessmentEvidenceFieldLabel,
} from "@/features/materials/presentation/material-labels";
import type {
  MaterialAvailabilityRiskDetail,
  MaterialAvailabilityRiskHistoryEntry,
  MaterialAvailabilityRiskWorkspacePayload,
} from "../domain/material-availability-risk";
import {
  evaluateMaterialAvailabilityRiskAction,
  refreshMaterialAvailabilityRiskWorkspaceAction,
} from "../server/material-availability-risk-actions";
import {
  getMaterialAvailabilityRiskStatusBadgeClass,
  getMaterialAvailabilityRiskStatusLabel,
  MATERIAL_AVAILABILITY_RISK_DISCLAIMER,
} from "../presentation/material-availability-risk-labels";

type MaterialAvailabilityRiskWorkspaceViewProps = Readonly<{
  workspace: MaterialAvailabilityRiskWorkspacePayload;
  initialAssessment?: MaterialAvailabilityRiskDetail;
  assessmentError?: string;
  history?: readonly MaterialAvailabilityRiskHistoryEntry[];
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
  if (unit === "g" || unit === "gram") {
    return formatMaterialGrams(value);
  }
  return `${value.toLocaleString("tr-TR")}${unit ? ` ${unit}` : ""}`;
}

export function MaterialAvailabilityRiskWorkspaceView({
  workspace,
  initialAssessment,
  assessmentError,
  history,
  historyError,
}: MaterialAvailabilityRiskWorkspaceViewProps) {
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
      await refreshMaterialAvailabilityRiskWorkspaceAction();
      router.refresh();
    });
  };

  const handleEvaluate = () => {
    const requirementProfileId = assessment?.requirementProfileId;
    if (!assessment?.materialId || !requirementProfileId) return;
    startTransition(async () => {
      setActionError(null);
      const result = await evaluateMaterialAvailabilityRiskAction(
        assessment.materialId,
        requirementProfileId,
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
    workspace.capabilities.canManage &&
    Boolean(assessment?.materialId && assessment.requirementProfileId);

  if (workspace.assessments.length === 0) {
    return (
      <div className="quality-empty-panel" data-testid="material-availability-risk-empty">
        <FileWarning aria-hidden="true" />
        <h3>Değerlendirilmiş malzeme riski yok</h3>
        <p>Yetkili tesis kapsamında malzeme kullanılabilirliği değerlendirmesi bulunamadı.</p>
      </div>
    );
  }

  return (
    <div data-testid="material-availability-risk-workspace">
      <p className="quality-readonly-note" data-testid="material-availability-risk-readonly-note">
        Bu ekran yalnızca malzeme kullanılabilirliği riskini gösterir; otomatik stok, satın alma veya üretim işlemi yapmaz.
      </p>

      <div className="quality-dashboard-kpi-grid" data-testid="material-availability-risk-kpi-grid">
        <QualityDashboardKpi
          label="Eksik"
          value={workspace.kpis.shortageCount}
          description="Stok yetersizliği"
          tone="paused"
          icon={<TriangleAlert />}
        />
        <QualityDashboardKpi
          label="Yeterli"
          value={workspace.kpis.sufficientCount}
          description="Kapsama yeterli"
          tone="ready"
          icon={<Boxes />}
        />
        <QualityDashboardKpi
          label="Yetersiz veri"
          value={workspace.kpis.insufficientDataCount}
          description="ERP veya ihtiyaç eksik"
          tone="in-progress"
          icon={<FileWarning />}
        />
        <QualityDashboardKpi
          label="Kaynak çelişkisi"
          value={workspace.kpis.sourceConflictCount}
          description="Birim veya kaynak uyumsuz"
          tone="in-progress"
          icon={<PackageSearch />}
        />
      </div>

      <div className="quality-workspace-toolbar">
        <button
          type="button"
          onClick={handleRefresh}
          disabled={isPending}
          data-testid="material-availability-risk-refresh-button"
        >
          <RefreshCw aria-hidden="true" />
          Yenile
        </button>
      </div>

      {actionError ? (
        <div role="alert" className="quality-error-panel" data-testid="material-availability-risk-action-error">
          <AlertTriangle aria-hidden="true" />
          <p>{actionError}</p>
        </div>
      ) : null}

      <section className="quality-panel-grid">
        <Card className="quality-panel">
          <header className="quality-panel__header">
            <h2>Malzeme risk envanteri</h2>
            <p>Kullanılabilirlik değerlendirmeleri</p>
          </header>
          <div className="quality-table-wrap" data-testid="material-availability-risk-table">
            <table>
              <thead>
                <tr>
                  <th>Malzeme</th>
                  <th>Durum</th>
                  <th>Üretim emri</th>
                  <th>Değerlendirme</th>
                </tr>
              </thead>
              <tbody>
                {workspace.assessments.map((row) => (
                  <tr
                    key={row.id}
                    data-testid={`material-availability-risk-row-${row.id}`}
                    aria-selected={row.id === selectedAssessmentId}
                  >
                    <td>
                      <button
                        type="button"
                        className="quality-table-link"
                        onClick={() => selectAssessment(row.id)}
                      >
                        {row.materialCode || "—"}
                      </button>
                      <div className="quality-table-meta">{row.materialName || "—"}</div>
                    </td>
                    <td>
                      <span className={getMaterialAvailabilityRiskStatusBadgeClass(row.status)}>
                        {getMaterialAvailabilityRiskStatusLabel(row.status)}
                      </span>
                    </td>
                    <td>{row.productionOrderReference ?? "—"}</td>
                    <td>{formatDateTime(row.evaluatedAtUtc)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </Card>

        <Card className="quality-panel" data-testid="material-availability-risk-detail-panel">
          <header className="quality-panel__header">
            <h2>Değerlendirme kanıtı</h2>
            <p>{selectedRow ? `${selectedRow.materialCode} — ${selectedRow.materialName}` : "Kayıt seçin"}</p>
          </header>

          {assessmentError ? (
            <p role="alert" data-testid="material-availability-risk-detail-error">{assessmentError}</p>
          ) : !assessment ? (
            <p data-testid="material-availability-risk-detail-empty">Seçili değerlendirme yüklenemedi.</p>
          ) : (
            <div data-testid="material-availability-risk-detail-ready">
              <dl className="quality-detail-grid">
                <div>
                  <dt>Malzeme</dt>
                  <dd>{`${assessment.materialCode} — ${assessment.materialName}`}</dd>
                </div>
                <div>
                  <dt>Durum</dt>
                  <dd>
                    <span className={getMaterialAvailabilityRiskStatusBadgeClass(assessment.status)}>
                      {getMaterialAvailabilityRiskStatusLabel(assessment.status)}
                    </span>
                  </dd>
                </div>
                <div>
                  <dt>Üretim emri</dt>
                  <dd>{assessment.productionOrderReference ?? "—"}</dd>
                </div>
                <div>
                  <dt>Ürün</dt>
                  <dd>{assessment.productReference ?? "—"}</dd>
                </div>
                <div>
                  <dt>Kalıp</dt>
                  <dd>{assessment.moldReference ?? "—"}</dd>
                </div>
                <div>
                  <dt>Gerekli miktar</dt>
                  <dd>{formatMaterialGrams(assessment.requiredQuantityGrams)}</dd>
                </div>
                <div>
                  <dt>ERP kullanılabilir</dt>
                  <dd>{formatQuantity(assessment.erpAvailableQuantity, assessment.erpUnit)}</dd>
                </div>
                <div>
                  <dt>ERP rezerv</dt>
                  <dd>{formatQuantity(assessment.erpReservedQuantity, assessment.erpUnit)}</dd>
                </div>
                <div>
                  <dt>Rezerv sonrası</dt>
                  <dd>{formatQuantity(assessment.availableAfterReservationQuantity, assessment.erpUnit)}</dd>
                </div>
                <div>
                  <dt>Eksik miktar</dt>
                  <dd>{formatQuantity(assessment.shortageQuantity, assessment.erpUnit)}</dd>
                </div>
                <div>
                  <dt>Kaynak tazeliği</dt>
                  <dd>{assessment.sourceFreshness ?? "—"}</dd>
                </div>
                <div>
                  <dt>Değerlendirme</dt>
                  <dd>{formatDateTime(assessment.evaluatedAtUtc)}</dd>
                </div>
                <div>
                  <dt>Kural seti</dt>
                  <dd>{assessment.ruleSetVersion}</dd>
                </div>
              </dl>

              {assessment.drivers.length > 0 ? (
                <section aria-labelledby="material-risk-drivers-heading">
                  <h3 id="material-risk-drivers-heading">Risk sürücüleri</h3>
                  <ul className="quality-audit-list" data-testid="material-availability-risk-drivers">
                    {assessment.drivers.map((driver) => (
                      <li key={`${driver.code}-${driver.label}`}>
                        <strong>{driver.label}</strong>
                        {driver.value ? ` — ${driver.value}` : ""}
                        {" "}
                        ({getMaterialAssessmentDriverStateLabel(driver.state)})
                      </li>
                    ))}
                  </ul>
                </section>
              ) : null}

              {assessment.evidence.length > 0 ? (
                <section aria-labelledby="material-risk-evidence-heading">
                  <h3 id="material-risk-evidence-heading">Kanıt alanları</h3>
                  <ul className="quality-audit-list" data-testid="material-availability-risk-evidence">
                    {assessment.evidence.map((item) => (
                      <li key={`${item.field}-${item.source ?? "unknown"}`}>
                        <strong>{getMaterialAssessmentEvidenceFieldLabel(item.field)}</strong>
                        {": "}
                        {item.isMissing ? "Eksik" : (item.value ?? "—")}
                        {item.source ? ` (${item.source})` : ""}
                      </li>
                    ))}
                  </ul>
                </section>
              ) : null}

              {history && history.length > 0 ? (
                <section aria-labelledby="material-risk-history-heading">
                  <h3 id="material-risk-history-heading">Değerlendirme geçmişi</h3>
                  {historyError ? (
                    <p role="alert" data-testid="material-availability-risk-history-error">{historyError}</p>
                  ) : (
                    <ul className="quality-audit-list" data-testid="material-availability-risk-history">
                      {history.map((entry) => (
                        <li key={entry.id}>
                          <button
                            type="button"
                            className="quality-table-link"
                            onClick={() => selectAssessment(entry.id)}
                          >
                            {getMaterialAvailabilityRiskStatusLabel(entry.status)}
                          </button>
                          {" — "}
                          {formatDateTime(entry.evaluatedAtUtc)}
                          {" "}
                          ({entry.ruleSetVersion})
                        </li>
                      ))}
                    </ul>
                  )}
                </section>
              ) : null}

              <p
                className="quality-automation-disclaimer"
                data-testid="material-availability-risk-automation-disclaimer"
              >
                <ShieldAlert aria-hidden="true" />
                {workspace.automationDisclaimer || MATERIAL_AVAILABILITY_RISK_DISCLAIMER}
              </p>

              <div className="quality-action-row">
                {canEvaluate ? (
                  <button
                    type="button"
                    onClick={handleEvaluate}
                    disabled={isPending}
                    data-testid="material-availability-risk-evaluate-button"
                  >
                    {isPending ? <LoaderCircle aria-hidden="true" className="spin" /> : null}
                    Yeniden değerlendir
                  </button>
                ) : null}

                <Link
                  href={`/malzemeler?material=${assessment.materialId}`}
                  data-testid="material-availability-risk-material-link"
                >
                  Malzeme kaydını aç
                </Link>
              </div>
            </div>
          )}
        </Card>
      </section>
    </div>
  );
}
