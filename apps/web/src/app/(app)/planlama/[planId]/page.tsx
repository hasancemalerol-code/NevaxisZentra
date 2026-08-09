import {
  ArrowLeft,
  Bot,
  CalendarRange,
  Factory,
  ShieldCheck,
} from "lucide-react";
import Link from "next/link";
import { notFound } from "next/navigation";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";

import { readProductionPlanningDetail } from "@/features/production-planning/server/production-plan-reader";
import { createProductionPlanDetailViewModel } from "@/features/production-planning/application/production-plan-detail-view-model";
import { createProductionPlanRiskViewModel } from "@/features/production-planning/application/production-plan-risk-view-model";
import { createProductionPlanApprovalTransferViewModel } from "@/features/production-planning/application/production-plan-approval-transfer-view-model";
import { createProductionPlanDemandViewModel } from "@/features/production-planning/application/production-plan-demand-view-model";
import { createProductionPlanTimelineViewModel } from "@/features/production-planning/application/production-plan-timeline-view-model";
import { createProductionPlanHistoryViewModel } from "@/features/production-planning/application/production-plan-history-view-model";
import { ProductionPlanResourceCards } from "@/features/production-planning/components/production-plan-resource-cards";
import { ProductionPlanRiskPanel } from "@/features/production-planning/components/production-plan-risk-panel";
import { ProductionPlanApprovalTransferPanel } from "@/features/production-planning/components/production-plan-approval-transfer-panel";
import { ProductionPlanDemandTable } from "@/features/production-planning/components/production-plan-demand-table";
import { ProductionPlanTimeline } from "@/features/production-planning/components/production-plan-timeline";
import { ProductionPlanHistoryPanel } from "@/features/production-planning/components/production-plan-history-panel";
import { ProductionPlanWorkflowActions } from "@/features/production-planning/components/production-plan-workflow-actions";
import { ProductionPlanSchedulingActions } from "@/features/production-planning/components/production-plan-scheduling-actions";
import { ProductionPlanTransferPanel } from "@/features/production-planning/components/production-plan-transfer-panel";
import { getProductionPlanFallbackLabel } from "@/features/production-planning/presentation/production-plan-fallback-labels";
import {
  getProductionPlanConfidenceLabel,
  getProductionPlanOriginLabel,
  getProductionPlanPriorityLabel,
  getProductionPlanStatusLabel,
  getProductionPlanTargetSystemLabel,
} from "@/features/production-planning/presentation/production-plan-labels";
import {
  approveAction,
  rejectAction,
  requestRevisionAction,
  submitForReviewAction,
} from "@/features/production-planning/server/production-plan-workflow-actions";
import { scheduleAction } from "@/features/production-planning/server/production-plan-scheduling-actions";
import {
  createTransferRequestAction,
  executeTransferAction,
} from "@/features/production-planning/server/production-plan-transfer-actions";
import { ProductionPlanHandoffPanel } from "@/features/production-planning/components/production-plan-handoff-panel";
import { ProductionPlanLogisticsPanel } from "@/features/production-planning/components/production-plan-logistics-panel";
import { deriveExportReadiness } from "@/features/production-planning/presentation/production-plan-logistics-formatters";
import { prepareHandoffAction } from "@/features/production-planning/server/production-plan-handoff-actions";
import { evaluatePlanningLogisticsAction } from "@/features/production-planning/server/production-plan-logistics-actions";
import { readPlanningLogisticsEvaluation } from "@/features/production-planning/server/production-plan-logistics-reader";

type ProductionPlanDetailPageProps =
  Readonly<{
    params: Promise<{
      planId: string;
    }>;
  }>;

export default async function ProductionPlanDetailPage({
  params,
}: ProductionPlanDetailPageProps) {
  const { planId } = await params;

  const result =
    await readProductionPlanningDetail(
      planId,
    );

  if (result.status === "failure") {
    throw new Error(
      [
        "Production plan detail could not be loaded.",
        result.code,
        result.message,
      ].join(" "),
    );
  }

  if (result.status === "not-found") {
    notFound();
  }

  const { productionPlan } = result;

  const viewModel =
    createProductionPlanDetailViewModel(
      productionPlan,
    );

  const riskViewModel =
    createProductionPlanRiskViewModel(
      productionPlan,
    );

  const approvalTransferViewModel =
    createProductionPlanApprovalTransferViewModel(
      productionPlan,
    );

  const demandViewModel =
    createProductionPlanDemandViewModel(
      productionPlan,
    );

  const timelineViewModel =
    createProductionPlanTimelineViewModel(
      productionPlan,
    );

  const historyViewModel =
    createProductionPlanHistoryViewModel(
      productionPlan,
    );

  const isEditable =
    productionPlan.status === "draft" ||
    productionPlan.status === "needs-review";

  const logisticsResult =
    await readPlanningLogisticsEvaluation(
      productionPlan.id,
      { evaluateIfMissing: productionPlan.status === "scheduled" },
    );

  return (
    <main className="production-plan-detail-page">
      <div className="production-plan-detail-page__back">
        <Link href="/planlama">
          <ArrowLeft aria-hidden="true" />
          Planlama çalışma alanına dön
        </Link>
      </div>

      <header className="production-plan-detail-page__header">
        <div>
          <p>Üretim Planı Detayı</p>

          <h1>{productionPlan.title}</h1>

          <span>
            {productionPlan.id}
          </span>
        </div>

        <div className="production-plan-detail-page__badges">
          <Badge tone="information">
            {getProductionPlanStatusLabel(
              productionPlan.status,
            )}
          </Badge>

          {isEditable ? (
            <Link
              href={`/planlama/${productionPlan.id}/duzenle`}
              className="button button-secondary button-small"
            >
              Düzenle
            </Link>
          ) : null}

          <Badge tone="neutral">
            {getProductionPlanPriorityLabel(
              productionPlan.demands[0]?.priority ??
                "normal",
            )}
          </Badge>

          <Badge tone="ai">
            <Bot aria-hidden="true" />
            {getProductionPlanConfidenceLabel(
              productionPlan.confidence,
            )}
          </Badge>
        </div>
      </header>

      <section className="production-plan-detail-page__summary">
        <Card>
          <CalendarRange aria-hidden="true" />

          <div>
            <span>Plan dönemi</span>
            <strong>
              {productionPlan.timeWindow.plannedStartAt}
            </strong>
            <small>
              {productionPlan.timeWindow.plannedEndAt}
            </small>
          </div>
        </Card>

        <Card>
          <Factory aria-hidden="true" />

          <div>
            <span>Kaynak</span>
            <strong>
              {getProductionPlanOriginLabel(
                productionPlan.origin,
              )}
            </strong>
            <small>
              {productionPlan.facilityId ?? getProductionPlanFallbackLabel("facility")}
            </small>
          </div>
        </Card>

        <Card>
          <ShieldCheck aria-hidden="true" />

          <div>
            <span>Hedef sistem</span>
            <strong>
              {productionPlan.transfer.targetSystem
                ? getProductionPlanTargetSystemLabel(
                    productionPlan.transfer.targetSystem,
                  )
                : getProductionPlanFallbackLabel(
                    "targetSystem",
                  )}
            </strong>
            <small>
              Yetkili aktarım bekleniyor
            </small>
          </div>
        </Card>
      </section>

      <section className="production-plan-detail-page__content">
        <Card>
          <p>Plan özeti</p>

          <h2>
            {productionPlan.notes ?? getProductionPlanFallbackLabel("notes")}
          </h2>
        </Card>

        <Card>
          <p>Human-in-the-Loop</p>

          <h2>
            Plan yetkili onayı olmadan üretime
            aktarılmaz
          </h2>

          <span>
            Makine, kalıp, malzeme, operatör,
            kapasite ve termin riskleri plan
            onayından önce kontrol edilir.
          </span>
        </Card>
      </section>

      <ProductionPlanTimeline
        viewModel={timelineViewModel}
      />

      <ProductionPlanDemandTable
        viewModel={demandViewModel}
      />

      <ProductionPlanResourceCards
        viewModel={viewModel}
      />

      <ProductionPlanRiskPanel
        viewModel={riskViewModel}
      />

      {logisticsResult.status === "success" ? (
        <ProductionPlanLogisticsPanel
          planId={productionPlan.id}
          evaluation={{
            productionPlanId: logisticsResult.evaluation.productionPlanId,
            exportCaseNumber: logisticsResult.evaluation.exportCaseNumber,
            shipmentReference: logisticsResult.evaluation.shipmentReference,
            pickupDeadlineUtc: logisticsResult.evaluation.pickupDeadlineUtc,
            departureDeadlineUtc: logisticsResult.evaluation.departureDeadlineUtc,
            latestProductionCompletionUtc:
              logisticsResult.evaluation.latestProductionCompletionUtc,
            plannedProductionCompletionUtc:
              logisticsResult.evaluation.plannedProductionCompletionUtc,
            slackMinutes: logisticsResult.evaluation.slackMinutes,
            riskLevel: logisticsResult.evaluation.riskLevel,
            blocking: logisticsResult.evaluation.blocking,
            blockingReason: logisticsResult.evaluation.blockingReason,
            evaluatedAtUtc: logisticsResult.evaluation.evaluatedAtUtc,
            sourceVersion: logisticsResult.evaluation.sourceVersion,
            linkageStatus: logisticsResult.evaluation.linkageStatus,
            ...deriveExportReadiness(logisticsResult.evaluation),
          }}
          onEvaluate={evaluatePlanningLogisticsAction}
        />
      ) : null}

      <ProductionPlanWorkflowActions
        planId={productionPlan.id}
        planStatus={productionPlan.status}
        version={productionPlan.version}
        planTitle={productionPlan.title}
        onSubmitForReview={submitForReviewAction}
        onRequestRevision={requestRevisionAction}
        onApprove={approveAction}
        onReject={rejectAction}
      />

      <ProductionPlanSchedulingActions
        planId={productionPlan.id}
        planStatus={productionPlan.status}
        version={productionPlan.version}
        planTitle={productionPlan.title}
        onSchedule={scheduleAction}
      />

      <ProductionPlanTransferPanel
        planId={productionPlan.id}
        planStatus={productionPlan.status}
        version={productionPlan.version}
        planTitle={productionPlan.title}
        transferStatus={productionPlan.transfer.status}
        onCreateTransferRequest={createTransferRequestAction}
        onExecuteTransfer={executeTransferAction}
      />

      <ProductionPlanHandoffPanel
        planId={productionPlan.id}
        planStatus={productionPlan.status}
        version={productionPlan.version}
        planTitle={productionPlan.title}
        transferStatus={productionPlan.transfer.status}
        onPrepareHandoff={prepareHandoffAction}
      />

      <ProductionPlanApprovalTransferPanel
        viewModel={approvalTransferViewModel}
      />

      <ProductionPlanHistoryPanel
        viewModel={historyViewModel}
      />
    </main>
  );
}