import {
  CircleDollarSign,
  Clock3,
  Factory,
  Gauge,
  TrendingUp,
  Wrench,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";

import type { AiTask } from "../domain/ai-task";
import {
  formatAiTaskDurationMinutes,
  formatAiTaskMoney,
  formatAiTaskQuantity,
} from "../presentation/ai-task-formatters";

type AiTaskImpactPanelProps = Readonly<{
  impact: AiTask["impact"];
}>;

type ImpactMetric = Readonly<{
  id: string;
  label: string;
  value: string;
  icon: React.ReactNode;
}>;

export function AiTaskImpactPanel({
  impact,
}: AiTaskImpactPanelProps) {
  const metrics: readonly ImpactMetric[] = [
    {
      id: "financial-impact",
      label: "Finansal etki",
      value: formatAiTaskMoney(
        impact.financialAmount,
        impact.currency,
      ),
      icon: <CircleDollarSign aria-hidden="true" />,
    },
    {
      id: "estimated-savings",
      label: "Tahmini tasarruf",
      value: formatAiTaskMoney(
        impact.estimatedSavingsAmount,
        impact.currency,
      ),
      icon: <TrendingUp aria-hidden="true" />,
    },
    {
      id: "production-quantity",
      label: "Etkilenen üretim",
      value: formatAiTaskQuantity(
        impact.productionQuantity,
        impact.productionUnit,
      ),
      icon: <Factory aria-hidden="true" />,
    },
    {
      id: "estimated-delay",
      label: "Tahmini gecikme",
      value: formatAiTaskDurationMinutes(
        impact.estimatedDelayMinutes,
      ),
      icon: <Clock3 aria-hidden="true" />,
    },
    {
      id: "estimated-downtime",
      label: "Tahmini duruş",
      value: formatAiTaskDurationMinutes(
        impact.estimatedDowntimeMinutes,
      ),
      icon: <Wrench aria-hidden="true" />,
    },
  ];

  const visibleMetrics = metrics.filter(
    (metric) => metric.value !== "Tanımlanmadı",
  );

  return (
    <Card className="ai-task-impact-panel">
      <div className="ai-task-impact-panel__heading">
        <div>
          <p>
            Karar etkisi
          </p>

          <h2>
            Operasyonel etki
          </h2>
        </div>

        <Badge tone="information">
          <Gauge aria-hidden="true" />
          {visibleMetrics.length} gösterge
        </Badge>
      </div>

      <p className="ai-task-impact-panel__summary">
        {impact.summary}
      </p>

      {visibleMetrics.length > 0 ? (
        <dl className="ai-task-impact-panel__metrics">
          {visibleMetrics.map((metric) => (
            <div key={metric.id}>
              <dt>
                {metric.icon}
                {metric.label}
              </dt>

              <dd>
                {metric.value}
              </dd>
            </div>
          ))}
        </dl>
      ) : (
        <p className="ai-task-impact-panel__empty">
          Bu görev için sayısal etki göstergesi
          tanımlanmadı.
        </p>
      )}
    </Card>
  );
}