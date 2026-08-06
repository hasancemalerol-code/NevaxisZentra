import Link from "next/link";
import {
  Bot,
  CalendarClock,
  Factory,
  PackageCheck,
  ShieldAlert,
  Sparkles,
  Wrench,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";

import type {
  ProductionPlan,
  ProductionPlanPriority,
  ProductionPlanRiskSeverity,
  ProductionPlanStatus,
} from "../domain/production-plan";
import {
  hasBlockingProductionPlanRisks,
  resolveProductionPlanHighestPriority,
} from "../domain/production-plan";
import {
  formatProductionPlanConfidence,
  formatProductionPlanQuantity,
  formatProductionPlanRiskCount,
  formatProductionPlanTimeWindow,
} from "../presentation/production-plan-formatters";
import {
  getProductionPlanConfidenceLabel,
  getProductionPlanOriginLabel,
  getProductionPlanPriorityLabel,
  getProductionPlanStatusLabel,
} from "../presentation/production-plan-labels";

type ProductionPlanCardProps = Readonly<{
  productionPlan: ProductionPlan;
}>;

type BadgeTone =
  | "neutral"
  | "information"
  | "success"
  | "warning"
  | "danger"
  | "ai";

function getStatusTone(
  status: ProductionPlanStatus,
): BadgeTone {
  switch (status) {
    case "approved":
    case "scheduled":
    case "completed":
      return "success";

    case "ready-for-approval":
    case "in-progress":
      return "information";

    case "needs-review":
      return "warning";

    case "rejected":
    case "cancelled":
      return "danger";

    case "draft":
    default:
      return "neutral";
  }
}

function getPriorityTone(
  priority: ProductionPlanPriority,
): BadgeTone {
  switch (priority) {
    case "urgent":
      return "danger";

    case "high":
      return "warning";

    case "normal":
      return "information";

    case "low":
    default:
      return "neutral";
  }
}

function getRiskTone(
  severity: ProductionPlanRiskSeverity,
): BadgeTone {
  switch (severity) {
    case "critical":
      return "danger";

    case "warning":
      return "warning";

    case "information":
    default:
      return "information";
  }
}

function getHighestRiskSeverity(
  productionPlan: ProductionPlan,
): ProductionPlanRiskSeverity | null {
  const weight = {
    information: 0,
    warning: 1,
    critical: 2,
  } satisfies Record<
    ProductionPlanRiskSeverity,
    number
  >;

  const highestFromRisks =
    productionPlan.risks.reduce<
      ProductionPlanRiskSeverity | null
    >(
      (highest, risk) => {
        if (!highest) {
          return risk.severity;
        }

        return weight[risk.severity] >
          weight[highest]
          ? risk.severity
          : highest;
      },
      null,
    );

  if (highestFromRisks) {
    return highestFromRisks;
  }

  const listSummary =
    productionPlan.listSummary;

  if (
    (listSummary?.blockingRiskCount ?? 0) >
      0 ||
    (listSummary?.criticalRiskCount ?? 0) > 0
  ) {
    return "critical";
  }

  if ((listSummary?.riskCount ?? 0) > 0) {
    return "warning";
  }

  return null;
}

export function ProductionPlanCard({
  productionPlan,
}: ProductionPlanCardProps) {
  const highestPriority =
    resolveProductionPlanHighestPriority(
      productionPlan,
    );

  const highestRiskSeverity =
    getHighestRiskSeverity(productionPlan);

  const primaryDemand =
    productionPlan.demands[0];

  const listSummary =
    productionPlan.listSummary;

  const blockingRiskCount =
    productionPlan.risks.length > 0
      ? productionPlan.risks.filter(
          (risk) => risk.blocking,
        ).length
      : (listSummary?.blockingRiskCount ??
        0);

  const hasBlockingRisk =
    hasBlockingProductionPlanRisks(
      productionPlan,
    );

  return (
    <Card className="production-plan-card">
      <div className="production-plan-card__heading">
        <div>
          <div className="production-plan-card__badges">
            <Badge
              tone={getStatusTone(
                productionPlan.status,
              )}
            >
              {getProductionPlanStatusLabel(
                productionPlan.status,
              )}
            </Badge>

            <Badge
              tone={getPriorityTone(
                highestPriority,
              )}
            >
              {getProductionPlanPriorityLabel(
                highestPriority,
              )}
            </Badge>

            {productionPlan.aiGenerated ? (
              <Badge tone="ai">
                <Sparkles aria-hidden="true" />
                AI planı
              </Badge>
            ) : null}
          </div>

          <h2>{productionPlan.title}</h2>

          <p>
            {getProductionPlanOriginLabel(
              productionPlan.origin,
            )}
          </p>
        </div>

        <div className="production-plan-card__confidence">
          <Bot aria-hidden="true" />

          <div>
            <span>
              {getProductionPlanConfidenceLabel(
                productionPlan.confidence,
              )}
            </span>

            <strong>
              {formatProductionPlanConfidence(
                productionPlan.confidence,
              )}
            </strong>
          </div>
        </div>
      </div>

      <div className="production-plan-card__summary">
        <div>
          <PackageCheck aria-hidden="true" />

          <div>
            <span>Üretim talebi</span>

            <strong>
              {primaryDemand
                ? formatProductionPlanQuantity(
                    primaryDemand.requiredQuantity,
                    primaryDemand.unit,
                  )
                : listSummary?.primaryRequiredQuantity !==
                    undefined
                  ? formatProductionPlanQuantity(
                      listSummary.primaryRequiredQuantity,
                      listSummary.primaryUnit,
                    )
                  : (listSummary?.demandCount ??
                        0) > 0
                    ? `${listSummary?.demandCount ?? 0} talep`
                    : "Talep bulunmuyor"}
            </strong>

            <small>
              {primaryDemand?.productCode ??
                primaryDemand?.productName ??
                listSummary?.primaryProductCode ??
                listSummary?.primaryProductName ??
                "Ürün belirlenmedi"}
            </small>
          </div>
        </div>

        <div>
          <CalendarClock aria-hidden="true" />

          <div>
            <span>Plan aralığı</span>

            <strong>
              {formatProductionPlanTimeWindow(
                productionPlan.timeWindow,
              )}
            </strong>

            <small>
              {productionPlan.timeWindow
                .shiftName ??
                listSummary?.shiftName ??
                "Vardiya belirlenmedi"}
            </small>
          </div>
        </div>

        <div>
          <Factory aria-hidden="true" />

          <div>
            <span>Makine ve kalıp</span>

            <strong>
              {productionPlan
                .resourceAssignment
                .machineName ??
                "Makine atanmadı"}
            </strong>

            <small>
              {productionPlan
                .resourceAssignment
                .moldName ??
                "Kalıp atanmadı"}
            </small>
          </div>
        </div>

        <div>
          <Wrench aria-hidden="true" />

          <div>
            <span>Malzeme</span>

            <strong>
              {productionPlan
                .resourceAssignment
                .materialName ??
                "Malzeme atanmadı"}
            </strong>

            <small>
              {productionPlan
                .resourceAssignment
                .operatorGroupName ??
                "Operatör grubu belirlenmedi"}
            </small>
          </div>
        </div>
      </div>

      <div className="production-plan-card__footer">
        <div className="production-plan-card__risk">
          <ShieldAlert aria-hidden="true" />

          <div>
            <span>Risk durumu</span>

            <strong>
              {formatProductionPlanRiskCount(
                productionPlan,
              )}
            </strong>
          </div>

          {highestRiskSeverity ||
          hasBlockingRisk ? (
            <Badge
              tone={getRiskTone(
                highestRiskSeverity ??
                  "critical",
              )}
            >
              {blockingRiskCount > 0
                ? `${blockingRiskCount} engelleyici`
                : highestRiskSeverity}
            </Badge>
          ) : null}
        </div>

        <Link
          className="production-plan-card__link"
          href={`/planlama/${productionPlan.id}`}
        >
          Planı incele
        </Link>
      </div>
    </Card>
  );
}