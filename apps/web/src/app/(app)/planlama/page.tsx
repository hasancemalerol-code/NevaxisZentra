import {
  AlertTriangle,
  Bot,
  CalendarRange,
  CheckCircle2,
  Factory,
  Send,
} from "lucide-react";
import Link from "next/link";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";

import {
  calculateProductionPlanSummary,
} from "@/features/production-planning/application/production-plan-query";
import { AdaptivePlanningWorkspacePanel } from "@/features/adaptive-production-planning/components/adaptive-planning-workspace-panel";
import { ProductionPlanFilteredWorkspace } from "@/features/production-planning/components/production-plan-filtered-workspace";
import { ProductionPlanningLogisticsSection } from "@/features/production-planning/components/production-planning-logistics-section";
import { deriveExportReadiness } from "@/features/production-planning/presentation/production-plan-logistics-formatters";
import { readProductionPlanningWorkspace } from "@/features/production-planning/server/production-plan-reader";
import {
  readPlanningLogisticsEvaluation,
  readPlanningLogisticsSummaries,
} from "@/features/production-planning/server/production-plan-logistics-reader";

export default async function ProductionPlanningPage() {
  const workspaceResult =
    await readProductionPlanningWorkspace();

  if (workspaceResult.status === "failure") {
    throw new Error(
      [
        "Production planning list could not be loaded.",
        workspaceResult.code,
        workspaceResult.message,
      ].join(" "),
    );
  }

  const productionPlans =
    workspaceResult.productionPlans;

  const summary =
    calculateProductionPlanSummary(
      productionPlans,
    );

  const logisticsSummaries =
    await readPlanningLogisticsSummaries(
      productionPlans.map((plan) => plan.id),
    );

  const linkedLogisticsEvaluations = (
    await Promise.all(
      productionPlans.slice(0, 4).map(async (plan) => {
        const result = await readPlanningLogisticsEvaluation(
          plan.id,
          { evaluateIfMissing: plan.status === "scheduled" },
        );
        return result.status === "success"
          ? result.evaluation
          : null;
      }),
    )
  ).filter(
    (
      evaluation,
    ): evaluation is NonNullable<typeof evaluation> =>
      evaluation !== null && evaluation.linkageStatus === "linked",
  ).map((evaluation) => ({
    ...evaluation,
    ...deriveExportReadiness(evaluation),
  }));

  return (
    <main className="production-planning-page">
      <header className="production-planning-header">
        <p>Factory Operations Assistant</p>

        <h1>Üretim Planlama</h1>

        <span>
          Onaylı siparişler, ERP üretim talepleri,
          makine, kalıp, malzeme, operatör ve vardiya
          verileriyle açıklanabilir üretim planları
          oluşturun.
        </span>

        <div className="production-planning-header__actions">
          <Link href="/planlama/yeni" className="button button-primary button-medium">
            Yeni Plan Oluştur
          </Link>
        </div>
      </header>

      <section className="production-planning-kpis">
        <Card>
          <Factory aria-hidden="true" />

          <div>
            <span>Toplam plan</span>
            <strong>{summary.total}</strong>
            <small>
              Aktif planlama çalışma alanı
            </small>
          </div>
        </Card>

        <Card>
          <AlertTriangle aria-hidden="true" />

          <div>
            <span>İnceleme gerekiyor</span>
            <strong>
              {summary.needsReview}
            </strong>
            <small>
              Kullanıcı müdahalesi bekliyor
            </small>
          </div>
        </Card>

        <Card>
          <CalendarRange aria-hidden="true" />

          <div>
            <span>Onay bekleyen</span>
            <strong>
              {summary.waitingForApproval}
            </strong>
            <small>
              Planlama yöneticisi kuyruğu
            </small>
          </div>
        </Card>

        <Card>
          <CheckCircle2 aria-hidden="true" />

          <div>
            <span>Onaylanan</span>
            <strong>{summary.approved}</strong>
            <small>
              Planlanabilir üretim planı
            </small>
          </div>
        </Card>

        <Card>
          <Send aria-hidden="true" />

          <div>
            <span>Aktarıma hazır</span>
            <strong>
              {summary.transferable}
            </strong>
            <small>
              MES veya ERP aktarımı bekliyor
            </small>
          </div>
        </Card>

        <Card>
          <Bot aria-hidden="true" />

          <div>
            <span>AI planı</span>
            <strong>
              {summary.aiGenerated}
            </strong>
            <small>
              AI tarafından oluşturulan taslak
            </small>
          </div>
        </Card>
      </section>

      <section className="production-planning-safety">
        <div>
          <Bot aria-hidden="true" />

          <div>
            <p>Human-in-the-Loop</p>

            <h2>
              AI üretim planını önerir, yetkili kullanıcı
              onaylar
            </h2>

            <span>
              Engelleyici kapasite, makine, kalıp,
              malzeme, operatör veya termin riski varken
              plan onaya gönderilemez. Onaylanan planlar
              yalnızca yetkili işlemle bağlı ERP veya MES
              sistemine aktarılır.
            </span>
          </div>
        </div>

        <Badge tone="ai">
          <Bot aria-hidden="true" />
          Açıklanabilir AI planlama
        </Badge>
      </section>

      <ProductionPlanningLogisticsSection
        evaluations={linkedLogisticsEvaluations.map((evaluation) => ({
          productionPlanId: evaluation.productionPlanId,
          exportCaseNumber: evaluation.exportCaseNumber,
          shipmentReference: evaluation.shipmentReference,
          pickupDeadlineUtc: evaluation.pickupDeadlineUtc,
          departureDeadlineUtc: evaluation.departureDeadlineUtc,
          latestProductionCompletionUtc: evaluation.latestProductionCompletionUtc,
          plannedProductionCompletionUtc: evaluation.plannedProductionCompletionUtc,
          slackMinutes: evaluation.slackMinutes,
          riskLevel: evaluation.riskLevel,
          blocking: evaluation.blocking,
          blockingReason: evaluation.blockingReason,
          evaluatedAtUtc: evaluation.evaluatedAtUtc,
          sourceVersion: evaluation.sourceVersion,
          linkageStatus: evaluation.linkageStatus,
        }))}
      />

      <AdaptivePlanningWorkspacePanel />

      <ProductionPlanFilteredWorkspace
        productionPlans={productionPlans}
        logisticsSummaries={logisticsSummaries}
      />
    </main>
  );
}