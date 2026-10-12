"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useState, useTransition } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import {
  AlertTriangle,
  ClipboardList,
  ExternalLink,
  LoaderCircle,
  RefreshCw,
  ShieldAlert,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import type {
  MaintenancePredictiveRiskAssessment,
  MaintenancePredictiveRiskWorkspacePayload,
} from "../domain/maintenance-predictive-risk";
import {
  createMaintenancePredictiveRiskTaskDraftAction,
  refreshMaintenancePredictiveRiskWorkspaceAction,
  transitionMaintenancePredictiveRiskFollowUpAction,
} from "../server/maintenance-predictive-risk-actions";
import {
  formatMaintenancePredictiveDriverLabel,
  formatMaintenancePredictiveEvidenceTypeLabel,
  getMaintenancePredictiveRecommendationCategoryLabel,
  getMaintenancePredictiveReviewPriorityLabel,
  getMaintenancePredictiveRiskBandLabel,
  getMaintenancePredictiveRiskBandTone,
  getMaintenancePredictiveRiskFollowUpStatusLabel,
  MAINTENANCE_PREDICTIVE_RISK_SAFETY_COPY,
  MAINTENANCE_PREDICTIVE_RISK_TASK_DISCLAIMER,
} from "../presentation/maintenance-predictive-risk-labels";
import { MaintenanceDashboardKpi } from "./maintenance-dashboard-kpi";
import { MaintenanceEvaluateControl } from "./maintenance-evaluate-control";
import { MaintenanceSectionHeader } from "./maintenance-table-primitives";
import { MaintenanceWorkspaceNav } from "./maintenance-workspace-nav";

type MaintenancePredictiveRiskWorkspaceViewProps = Readonly<{
  workspace: MaintenancePredictiveRiskWorkspacePayload;
  initialAssessment?: MaintenancePredictiveRiskAssessment;
  initialAssetReference?: string;
  assessmentError?: string;
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

function RiskBandBadge({ band }: Readonly<{ band: MaintenancePredictiveRiskAssessment["riskBand"] }>) {
  const tone = getMaintenancePredictiveRiskBandTone(band);
  const badgeTone =
    tone === "danger" ? "danger" : tone === "warning" ? "warning" : tone === "ready" ? "success" : "neutral";
  return <Badge tone={badgeTone}>{getMaintenancePredictiveRiskBandLabel(band)}</Badge>;
}

export function MaintenancePredictiveRiskWorkspaceView({
  workspace,
  initialAssessment,
  initialAssetReference,
  assessmentError,
}: MaintenancePredictiveRiskWorkspaceViewProps) {
  const router = useRouter();
  const pathname = usePathname();
  const searchParams = useSearchParams();
  const [isPending, startTransition] = useTransition();
  const [actionError, setActionError] = useState<string | null>(null);
  const [assessment, setAssessment] = useState(initialAssessment);

  useEffect(() => {
    setAssessment(initialAssessment);
  }, [initialAssessment]);

  const selectedAsset = searchParams.get("asset") ?? initialAssetReference ?? workspace.assets[0]?.assetReference ?? "";
  const selectedRow = useMemo(
    () => workspace.assets.find((row) => row.assetReference === selectedAsset),
    [workspace.assets, selectedAsset],
  );

  const selectAsset = useCallback(
    (assetReference: string) => {
      const params = new URLSearchParams(searchParams.toString());
      params.set("asset", assetReference);
      router.push(`${pathname}?${params.toString()}`);
    },
    [pathname, router, searchParams],
  );

  const handleRefresh = () => {
    startTransition(async () => {
      setActionError(null);
      await refreshMaintenancePredictiveRiskWorkspaceAction();
      router.refresh();
    });
  };

  const handleCreateTask = () => {
    if (!assessment?.id) return;
    startTransition(async () => {
      setActionError(null);
      const result = await createMaintenancePredictiveRiskTaskDraftAction(assessment.id!);
      if (result.status === "failure") {
        setActionError(result.message);
        return;
      }
      router.push(`/ai-gorevleri/${result.taskId}`);
    });
  };

  const handleMarkReviewed = () => {
    if (!assessment?.id) return;
    startTransition(async () => {
      setActionError(null);
      const result = await transitionMaintenancePredictiveRiskFollowUpAction(
        assessment.id!,
        "human-reviewed",
      );
      if (result.status === "failure") {
        setActionError(result.message);
        return;
      }
      router.refresh();
    });
  };

  const canCreateTask =
    workspace.capabilities.canCreateTask &&
    assessment?.canCreateTaskDraft === true &&
    !assessment.linkedAiTaskId;

  const taskButtonLabel =
    assessment?.riskBand === "insufficient-data"
      ? "Veri inceleme görev taslağı oluştur"
      : "Bakım görev taslağı oluştur";

  return (
    <main className="production-execution-page" data-testid="maintenance-predictive-risk-workspace">
      <header className="production-execution-header">
        <p>BAKIM &amp; ARIZA</p>
        <h1>Kestirimci Bakım Riskleri</h1>
        <p className="production-execution-subtitle">
          Makine durumu ve deterministik risk değerlendirmeleri için insan incelemeli görev akışı.
        </p>
      </header>

      <MaintenanceWorkspaceNav active="predictive-risks" />

      <p className="maintenance-readonly-note" data-testid="maintenance-predictive-risk-readonly-note">
        {MAINTENANCE_PREDICTIVE_RISK_SAFETY_COPY}
      </p>

      <div className="maintenance-kpi-grid" data-testid="maintenance-predictive-risk-kpi-grid">
        <MaintenanceDashboardKpi label="Aktif risk" value={workspace.kpis.activeRiskCount} description="Açık takip durumu" tone="in-progress" icon={<AlertTriangle />} />
        <MaintenanceDashboardKpi label="Görev bağlantısı" value={workspace.kpis.taskLinkedCount} description="AI görev taslağı bağlı" tone="ready" icon={<ClipboardList />} />
        <MaintenanceDashboardKpi label="İnsan incelemesi" value={workspace.kpis.humanReviewedCount} description="İnceleme kaydı" tone="paused" icon={<ShieldAlert />} />
      </div>

      <MaintenanceEvaluateControl
        lastEvaluatedAtUtc={workspace.overview.lastAssessedAtUtc}
        canManage={workspace.capabilities.canManage}
      />

      <div className="maintenance-workspace-toolbar">
        <button type="button" onClick={handleRefresh} disabled={isPending} data-testid="maintenance-predictive-risk-refresh-button">
          <RefreshCw aria-hidden="true" />
          Yenile
        </button>
      </div>

      {actionError ? (
        <div role="alert" className="state-panel error-state">
          <AlertTriangle aria-hidden="true" />
          <p>{actionError}</p>
        </div>
      ) : null}

      <section className="maintenance-panel-grid">
        <Card className="maintenance-panel">
          <MaintenanceSectionHeader title="Risk envanteri" subtitle="Yetkili kestirimci bakım riskleri" />
          <div className="maintenance-table-wrap" data-testid="maintenance-predictive-risk-table">
            <table>
              <thead>
                <tr>
                  <th>Varlık</th>
                  <th>Risk bandı</th>
                  <th>Takip durumu</th>
                  <th>Değerlendirme</th>
                </tr>
              </thead>
              <tbody>
                {workspace.assets.map((row) => (
                  <tr
                    key={row.assetReference}
                    data-testid={`maintenance-predictive-risk-row-${row.assetReference}`}
                    aria-selected={row.assetReference === selectedAsset}
                  >
                    <td>
                      <button type="button" className="maintenance-table-link" onClick={() => selectAsset(row.assetReference)}>
                        {row.machineName ?? row.assetName ?? row.assetReference}
                      </button>
                      <div className="maintenance-table-meta">{row.machineCode ?? row.assetReference}</div>
                    </td>
                    <td><RiskBandBadge band={row.riskBand} /></td>
                    <td>
                      {row.followUpStatus
                        ? getMaintenancePredictiveRiskFollowUpStatusLabel(row.followUpStatus)
                        : "—"}
                    </td>
                    <td>{formatDateTime(row.assessedAtUtc)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </Card>

        <Card className="maintenance-panel" data-testid="maintenance-predictive-risk-detail-panel">
          <MaintenanceSectionHeader
            title="Risk kanıtı ve öneri"
            subtitle={selectedRow ? selectedRow.assetReference : "Varlık seçin"}
          />

          {assessmentError ? (
            <p role="alert">{assessmentError}</p>
          ) : !assessment ? (
            <p data-testid="maintenance-predictive-risk-detail-empty">Seçili varlık için risk değerlendirmesi yok.</p>
          ) : (
            <div data-testid="maintenance-predictive-risk-detail-ready">
              <dl className="maintenance-detail-grid">
                <div><dt>Makine</dt><dd>{assessment.machineName ?? assessment.assetReference}</dd></div>
                <div><dt>Risk bandı</dt><dd><RiskBandBadge band={assessment.riskBand} /></dd></div>
                <div><dt>İnceleme önceliği</dt><dd>{getMaintenancePredictiveReviewPriorityLabel(assessment.reviewPriority)}</dd></div>
                <div><dt>Veri yeterliliği</dt><dd>{assessment.dataSufficiency}</dd></div>
                <div><dt>Veri tazeliği</dt><dd>{assessment.dataFreshness}</dd></div>
                <div><dt>Değerlendirme zamanı</dt><dd>{formatDateTime(assessment.assessedAtUtc)}</dd></div>
                <div><dt>Takip durumu</dt><dd>{assessment.followUpStatus ? getMaintenancePredictiveRiskFollowUpStatusLabel(assessment.followUpStatus) : "—"}</dd></div>
              </dl>

              <section aria-labelledby="risk-explanation-heading">
                <h3 id="risk-explanation-heading">Açıklama</h3>
                <p>{assessment.explanation}</p>
              </section>

              {assessment.recommendation ? (
                <section aria-labelledby="risk-recommendation-heading">
                  <h3 id="risk-recommendation-heading">Öneri</h3>
                  <p>
                    <strong>{getMaintenancePredictiveRecommendationCategoryLabel(assessment.recommendation.category)}</strong>
                    {" — "}
                    {assessment.recommendation.explanation}
                  </p>
                </section>
              ) : null}

              {assessment.riskDrivers.length > 0 ? (
                <section aria-labelledby="risk-drivers-heading">
                  <h3 id="risk-drivers-heading">Risk sürücüleri</h3>
                  <ul>
                    {assessment.riskDrivers.map((driver) => (
                      <li key={driver.driverCode}>
                        <strong>{formatMaintenancePredictiveDriverLabel(driver.driverCode)}</strong>: {driver.summary}
                      </li>
                    ))}
                  </ul>
                </section>
              ) : null}

              {assessment.evidence.length > 0 ? (
                <section aria-labelledby="risk-evidence-heading">
                  <h3 id="risk-evidence-heading">Kanıt</h3>
                  <ul>
                    {assessment.evidence.map((item) => (
                      <li key={`${item.evidenceType}-${item.evidenceReference}`}>
                        <strong>{formatMaintenancePredictiveEvidenceTypeLabel(item.evidenceType)}</strong>: {item.summary}
                      </li>
                    ))}
                  </ul>
                </section>
              ) : null}

              <p className="maintenance-automation-disclaimer" data-testid="maintenance-predictive-risk-automation-disclaimer">
                <ShieldAlert aria-hidden="true" />
                {workspace.automationDisclaimer || MAINTENANCE_PREDICTIVE_RISK_TASK_DISCLAIMER}
              </p>

              <div className="maintenance-action-row">
                {canCreateTask ? (
                  <button
                    type="button"
                    onClick={handleCreateTask}
                    disabled={isPending}
                    data-testid="maintenance-predictive-risk-create-task-button"
                  >
                    {isPending ? <LoaderCircle aria-hidden="true" className="spin" /> : <ClipboardList aria-hidden="true" />}
                    {taskButtonLabel}
                  </button>
                ) : null}

                {assessment.linkedAiTaskId ? (
                  <Link
                    href={`/ai-gorevleri/${assessment.linkedAiTaskId}`}
                    data-testid="maintenance-predictive-risk-linked-task-link"
                  >
                    <ExternalLink aria-hidden="true" />
                    Bağlı görevi aç
                  </Link>
                ) : null}

                {workspace.capabilities.canManage &&
                assessment.followUpStatus !== "human-reviewed" &&
                assessment.followUpStatus !== "resolved" ? (
                  <button
                    type="button"
                    onClick={handleMarkReviewed}
                    disabled={isPending}
                    data-testid="maintenance-predictive-risk-mark-reviewed-button"
                  >
                    İnsan incelemesi kaydet
                  </button>
                ) : null}
              </div>
            </div>
          )}
        </Card>
      </section>
    </main>
  );
}
