import { AlertTriangle, Truck } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";

import {
  formatPlanningLogisticsIndicatorLabel,
  formatPlanningLogisticsRiskLabel,
  formatPlanningLogisticsSlackLabel,
  type PlanningLogisticsEvaluationView,
} from "../presentation/production-plan-logistics-formatters";

type ProductionPlanningLogisticsSectionProps = Readonly<{
  evaluations: readonly PlanningLogisticsEvaluationView[];
}>;

function getRiskTone(riskLevel: string) {
  switch (riskLevel) {
    case "critical":
    case "high":
      return "danger" as const;
    case "medium":
      return "warning" as const;
    case "low":
      return "information" as const;
    default:
      return "neutral" as const;
  }
}

export function ProductionPlanningLogisticsSection({
  evaluations,
}: ProductionPlanningLogisticsSectionProps) {
  if (evaluations.length === 0) {
    return null;
  }

  return (
    <section className="production-planning-safety">
      <div>
        <Truck aria-hidden="true" />

        <div>
          <p>Lojistik Kısıtları</p>

          <h2>İhracat / Sevkiyat deadline tüketimi</h2>

          <span>
            Planning yalnızca Export/Shipment kaynak projeksiyonunu okur;
            sevkiyat alanları bu ekrandan düzenlenmez.
          </span>
        </div>
      </div>

      <div className="production-plan-filtered-workspace">
        {evaluations.map((evaluation) => (
          <Card key={evaluation.productionPlanId} className="production-plan-card">
            <div className="production-plan-card__heading">
              <div>
                <div className="production-plan-card__badges">
                  <Badge tone={getRiskTone(evaluation.riskLevel)}>
                    {formatPlanningLogisticsRiskLabel(evaluation.riskLevel)}
                  </Badge>
                </div>

                <h2>
                  {evaluation.exportCaseNumber ?? "EXP"} ·{" "}
                  {evaluation.shipmentReference ?? "SHP"}
                </h2>

                <p>{formatPlanningLogisticsIndicatorLabel(evaluation)}</p>
              </div>
            </div>

            <div className="production-plan-card__footer">
              <div className="production-plan-card__risk">
                <AlertTriangle aria-hidden="true" />
                <div>
                  <span>Slack</span>
                  <strong>
                    {formatPlanningLogisticsSlackLabel(evaluation.slackMinutes)}
                  </strong>
                </div>
              </div>
            </div>
          </Card>
        ))}
      </div>
    </section>
  );
}
