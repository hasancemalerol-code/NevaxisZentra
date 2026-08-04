import { Card } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/states";

const KPI_CARD_COUNT = 4;

export function AiTaskKpiLoading() {
  return (
    <div
      aria-busy="true"
      aria-label="AI görev merkezi özet göstergeleri yükleniyor"
      className="kpi-grid ai-task-kpi-links ai-task-kpi-loading"
    >
      {Array.from(
        {
          length: KPI_CARD_COUNT,
        },
        (_, index) => (
          <Card
            className="ai-task-kpi-loading__card"
            key={`ai-task-kpi-loading-${index}`}
          >
            <div className="ai-task-kpi-loading__content">
              <Skeleton className="ai-task-kpi-loading__label" />
              <Skeleton className="ai-task-kpi-loading__value" />
              <Skeleton className="ai-task-kpi-loading__detail" />
            </div>

            <Skeleton className="ai-task-kpi-loading__icon" />
          </Card>
        ),
      )}
    </div>
  );
}