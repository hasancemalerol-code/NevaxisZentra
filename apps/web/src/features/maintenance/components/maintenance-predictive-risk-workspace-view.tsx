"use client";

import Link from "next/link";
import { useCallback, useEffect, useMemo, useState, useTransition } from "react";
import { usePathname, useRouter, useSearchParams } from "next/navigation";
import {
  Activity,
  AlertTriangle,
  CircleOff,
  ClipboardList,
  ExternalLink,
  Gauge,
  LoaderCircle,
  RefreshCw,
  ShieldAlert,
  ShieldCheck,
  Siren,
  Eye,
} from "lucide-react";

import { DataTable } from "@/components/ui/data-table";
import { DetailBlock, DetailList } from "@/components/ui/detail-list";
import { EmptyPanel } from "@/components/ui/empty-panel";
import { MetricCard, MetricGrid } from "@/components/ui/metric";
import { Notice } from "@/components/ui/notice";
import { PageHeader } from "@/components/ui/page";
import { Panel } from "@/components/ui/panel";
import { SeverityBadge, type SeverityLevel } from "@/components/ui/severity-badge";
import type {
  MaintenancePredictiveRiskAssessment,
  MaintenancePredictiveRiskAssetRow,
  MaintenancePredictiveRiskWorkspacePayload,
} from "../domain/maintenance-predictive-risk";
import {
  createMaintenancePredictiveRiskTaskDraftAction,
  refreshMaintenancePredictiveRiskWorkspaceAction,
  transitionMaintenancePredictiveRiskFollowUpAction,
} from "../server/maintenance-predictive-risk-actions";
import {
  getMaintenanceDataFreshnessLabel,
  getMaintenanceDataSufficiencyLabel,
} from "../presentation/maintenance-condition-labels";
import {
  formatMaintenancePredictiveDriverLabel,
  formatMaintenancePredictiveEvidenceTypeLabel,
  getMaintenancePredictiveRecommendationCategoryLabel,
  getMaintenancePredictiveReviewPriorityLabel,
  getMaintenancePredictiveReviewPriorityTone,
  getMaintenancePredictiveRiskBandLabel,
  getMaintenancePredictiveRiskFollowUpStatusLabel,
  MAINTENANCE_PREDICTIVE_RISK_SAFETY_COPY,
  MAINTENANCE_PREDICTIVE_RISK_TASK_DISCLAIMER,
} from "../presentation/maintenance-predictive-risk-labels";
import { MaintenanceEvaluateControl } from "./maintenance-evaluate-control";
import { MAINTENANCE_RISK_BAND_ORDER, MaintenanceRiskBandBadge } from "./maintenance-risk-badge";
import { MaintenanceWorkspaceNav } from "./maintenance-workspace-nav";
import "./maintenance-workspace.css";
import { customerMessage } from "@/lib/presentation/customer-message";

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

const PRIORITY_LEVEL: Record<ReturnType<typeof getMaintenancePredictiveReviewPriorityTone>, SeverityLevel> = {
  danger: "critical",
  warning: "high",
  paused: "medium",
  ready: "low",
  neutral: "neutral",
};

function ReviewPriorityBadge({ priority }: Readonly<{ priority: MaintenancePredictiveRiskAssetRow["reviewPriority"] }>) {
  return (
    <SeverityBadge level={PRIORITY_LEVEL[getMaintenancePredictiveReviewPriorityTone(priority)]}>
      {getMaintenancePredictiveReviewPriorityLabel(priority)}
    </SeverityBadge>
  );
}

function groupRowsByBand(rows: readonly MaintenancePredictiveRiskAssetRow[]) {
  return MAINTENANCE_RISK_BAND_ORDER.map((band) => ({
    band,
    rows: rows
      .filter((row) => row.riskBand === band)
      .slice()
      .sort((left, right) => (right.assessedAtUtc ?? "").localeCompare(left.assessedAtUtc ?? "")),
  })).filter((group) => group.rows.length > 0);
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
  const groups = useMemo(() => groupRowsByBand(workspace.assets), [workspace.assets]);

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

  const overview = workspace.overview;

  return (
    <div className="zx-page" data-testid="maintenance-predictive-risk-workspace" aria-busy={isPending}>
      <PageHeader
        eyebrow="BAKIM VE ARIZA"
        title="Kestirimci Riskler"
        description="Makine durumu ve kural tabanlı risk değerlendirmeleri için insan incelemeli görev akışı."
        actions={
          <button
            type="button"
            className="zx-button"
            onClick={handleRefresh}
            disabled={isPending}
            data-testid="maintenance-predictive-risk-refresh-button"
          >
            {isPending ? <LoaderCircle aria-hidden="true" className="zx-spin" /> : <RefreshCw aria-hidden="true" />}
            Yenile
          </button>
        }
      />

      <MaintenanceWorkspaceNav active="predictive-risks" />

      <Notice tone="info" title="Karar desteği — otomatik işlem yok" data-testid="maintenance-predictive-risk-readonly-note">
        {MAINTENANCE_PREDICTIVE_RISK_SAFETY_COPY}
      </Notice>

      <section className="zx-stack maintenance-risk-metrics" aria-labelledby="risk-distribution-heading">
        <div className="zx-section-head">
          <h2 id="risk-distribution-heading" className="zx-section-title">Risk dağılımı</h2>
          <span className="zx-count-chip" data-testid="maintenance-predictive-risk-monitored-count">
            {overview.monitoredAssetCount} varlık izleniyor
          </span>
        </div>
        <MetricGrid label="Risk bandı dağılımı" data-testid="maintenance-predictive-risk-distribution">
          <MetricCard label="Kritik risk" value={overview.criticalRiskCount} hint="Acil inceleme" tone={overview.criticalRiskCount > 0 ? "critical" : "neutral"} icon={<Siren />} />
          <MetricCard label="Yüksek risk" value={overview.highRiskCount} hint="Planlı inceleme" tone={overview.highRiskCount > 0 ? "high" : "neutral"} icon={<AlertTriangle />} />
          <MetricCard label="İzlenmeli" value={overview.watchCount} hint="Yakın izleme" tone={overview.watchCount > 0 ? "medium" : "neutral"} icon={<Eye />} />
          <MetricCard label="Düşük risk" value={overview.lowRiskCount} hint="Rutin izleme" tone={overview.lowRiskCount > 0 ? "low" : "neutral"} icon={<ShieldCheck />} />
          <MetricCard label="Yetersiz veri" value={overview.insufficientDataCount} hint="Veri gerekli" icon={<CircleOff />} />
        </MetricGrid>
      </section>

      <section className="zx-stack maintenance-risk-metrics" aria-labelledby="risk-followup-heading">
        <h2 id="risk-followup-heading" className="zx-section-title">Takip akışı</h2>
        <MetricGrid label="Takip özeti" data-testid="maintenance-predictive-risk-kpi-grid">
          <MetricCard label="Aktif risk" value={workspace.kpis.activeRiskCount} hint="Açık takip durumu" tone={workspace.kpis.activeRiskCount > 0 ? "high" : "neutral"} icon={<Activity />} />
          <MetricCard label="Görev bağlantısı" value={workspace.kpis.taskLinkedCount} hint="Görev taslağı bağlı" tone="info" icon={<ClipboardList />} />
          <MetricCard label="İnsan incelemesi" value={workspace.kpis.humanReviewedCount} hint="İnceleme kaydı" tone="low" icon={<ShieldAlert />} />
        </MetricGrid>
      </section>

      <MaintenanceEvaluateControl
        lastEvaluatedAtUtc={workspace.overview.lastAssessedAtUtc}
        canManage={workspace.capabilities.canManage}
      />

      {actionError ? (
        <Notice tone="danger" alert>
          {customerMessage(actionError)}
        </Notice>
      ) : null}

      <div className="zx-split maintenance-risk-split">
        <Panel
          flush
          headingId="risk-inventory-heading"
          title="Risk envanteri"
          subtitle="Yetkili kestirimci bakım riskleri — en yüksek risk üstte"
        >
          {workspace.assets.length === 0 ? (
            <div className="zx-panel__body">
              <EmptyPanel
                icon={<ShieldCheck />}
                title="Henüz risk değerlendirmesi yok"
                description="Makine durum verisi geldikten sonra Değerlendirmeyi güncelle düğmesiyle ilk değerlendirmeyi çalıştırın."
                data-testid="maintenance-predictive-risk-empty"
              />
            </div>
          ) : (
            <DataTable label="Kestirimci bakım risk envanteri" data-testid="maintenance-predictive-risk-table" minWidth={620}>
              <thead>
                <tr>
                  <th scope="col">Varlık</th>
                  <th scope="col">Risk bandı</th>
                  <th scope="col">Takip durumu</th>
                  <th scope="col">Değerlendirme</th>
                </tr>
              </thead>
              {groups.map((group) => (
                <tbody key={group.band} aria-label={`${getMaintenancePredictiveRiskBandLabel(group.band)} grubu`}>
                  <tr className="zx-table-group">
                    <th scope="colgroup" colSpan={4}>
                      {getMaintenancePredictiveRiskBandLabel(group.band)} · {group.rows.length} varlık
                    </th>
                  </tr>
                  {group.rows.map((row) => (
                    <tr
                      key={row.assetReference}
                      data-testid={`maintenance-predictive-risk-row-${row.assetReference}`}
                      aria-selected={row.assetReference === selectedAsset}
                    >
                      <td>
                        <div className="zx-table-primary">
                          <button
                            type="button"
                            className="zx-table-link"
                            aria-pressed={row.assetReference === selectedAsset}
                            onClick={() => selectAsset(row.assetReference)}
                          >
                            {row.machineName ?? row.assetName ?? row.assetReference}
                          </button>
                          <span className="zx-table-meta">{row.machineCode ?? row.assetReference}</span>
                        </div>
                      </td>
                      <td><MaintenanceRiskBandBadge band={row.riskBand} /></td>
                      <td>
                        {row.followUpStatus
                          ? getMaintenancePredictiveRiskFollowUpStatusLabel(row.followUpStatus)
                          : "—"}
                      </td>
                      <td>{formatDateTime(row.assessedAtUtc)}</td>
                    </tr>
                  ))}
                </tbody>
              ))}
            </DataTable>
          )}
        </Panel>

        <Panel
          className="zx-sticky"
          headingId="risk-detail-heading"
          title="Risk kanıtı ve öneri"
          subtitle={selectedRow ? (selectedRow.machineName ?? selectedRow.assetName ?? selectedRow.assetReference) : "Varlık seçin"}
          data-testid="maintenance-predictive-risk-detail-panel"
        >
          {assessmentError ? (
            <Notice tone="danger" alert>
              {customerMessage(assessmentError)}
            </Notice>
          ) : !assessment ? (
            <EmptyPanel
              compact
              icon={<Gauge />}
              title="Değerlendirme seçilmedi"
              description="Seçili varlık için risk değerlendirmesi yok."
              data-testid="maintenance-predictive-risk-detail-empty"
            />
          ) : (
            <div className="zx-stack" data-testid="maintenance-predictive-risk-detail-ready">
              <div className="zx-detail-head">
                <MaintenanceRiskBandBadge band={assessment.riskBand} />
                <ReviewPriorityBadge priority={assessment.reviewPriority} />
              </div>

              <DetailList
                items={[
                  { label: "Makine", value: assessment.machineName ?? assessment.assetReference },
                  { label: "Veri yeterliliği", value: getMaintenanceDataSufficiencyLabel(assessment.dataSufficiency as Parameters<typeof getMaintenanceDataSufficiencyLabel>[0]) },
                  { label: "Veri tazeliği", value: getMaintenanceDataFreshnessLabel(assessment.dataFreshness as Parameters<typeof getMaintenanceDataFreshnessLabel>[0]) },
                  { label: "Değerlendirme zamanı", value: formatDateTime(assessment.assessedAtUtc) },
                  { label: "Takip durumu", value: assessment.followUpStatus ? getMaintenancePredictiveRiskFollowUpStatusLabel(assessment.followUpStatus) : "—" },
                ]}
              />

              <DetailBlock title="Açıklama" headingId="risk-explanation-heading">
                <p>{assessment.explanation}</p>
              </DetailBlock>

              {assessment.recommendation ? (
                <DetailBlock title="Öneri" headingId="risk-recommendation-heading">
                  <p>
                    <strong>{getMaintenancePredictiveRecommendationCategoryLabel(assessment.recommendation.category)}</strong>
                    {" — "}
                    {assessment.recommendation.explanation}
                  </p>
                </DetailBlock>
              ) : null}

              {assessment.riskDrivers.length > 0 ? (
                <DetailBlock title="Risk sürücüleri" headingId="risk-drivers-heading">
                  <ul className="zx-bullet-list">
                    {assessment.riskDrivers.map((driver) => (
                      <li key={driver.driverCode}>
                        <strong>{formatMaintenancePredictiveDriverLabel(driver.driverCode)}</strong>
                        <span>{driver.summary}</span>
                      </li>
                    ))}
                  </ul>
                </DetailBlock>
              ) : null}

              {assessment.evidence.length > 0 ? (
                <DetailBlock title="Kanıt" headingId="risk-evidence-heading">
                  <ul className="zx-bullet-list">
                    {assessment.evidence.map((item) => (
                      <li key={`${item.evidenceType}-${item.evidenceReference}`}>
                        <strong>{formatMaintenancePredictiveEvidenceTypeLabel(item.evidenceType)}</strong>
                        <span>{item.summary}</span>
                      </li>
                    ))}
                  </ul>
                </DetailBlock>
              ) : null}

              <Notice
                tone="neutral"
                icon={<ShieldAlert aria-hidden="true" />}
                data-testid="maintenance-predictive-risk-automation-disclaimer"
              >
                {workspace.automationDisclaimer || MAINTENANCE_PREDICTIVE_RISK_TASK_DISCLAIMER}
              </Notice>

              <div className="zx-action-bar zx-action-bar--bordered">
                {canCreateTask ? (
                  <button
                    type="button"
                    className="zx-button zx-button--primary"
                    onClick={handleCreateTask}
                    disabled={isPending}
                    data-testid="maintenance-predictive-risk-create-task-button"
                  >
                    {isPending ? <LoaderCircle aria-hidden="true" className="zx-spin" /> : <ClipboardList aria-hidden="true" />}
                    {taskButtonLabel}
                  </button>
                ) : null}

                {assessment.linkedAiTaskId ? (
                  <Link
                    href={`/ai-gorevleri/${assessment.linkedAiTaskId}`}
                    className="zx-button"
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
                    className="zx-button"
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
        </Panel>
      </div>
    </div>
  );
}
