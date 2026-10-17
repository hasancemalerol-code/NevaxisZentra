import { Card } from "@/components/ui/card";
import { Skeleton } from "@/components/ui/states";

const LOADING_CARD_COUNT = 3;

export function AiTaskWorkspaceLoading() {
  return (
    <div
      aria-busy="true"
      aria-label="Yapay zekâ görev çalışma alanı yükleniyor"
      className="ai-task-workspace ai-task-workspace--loading"
    >
      <div className="ai-task-filter-presets">
        {Array.from(
          {
            length: 5,
          },
          (_, index) => (
            <Skeleton
              className="ai-task-workspace-loading__preset"
              key={`preset-${index}`}
            />
          ),
        )}
      </div>

      <Card className="ai-task-workspace-loading__filter">
        <div className="ai-task-workspace-loading__filter-heading">
          <div>
            <Skeleton className="ai-task-workspace-loading__label" />
            <Skeleton className="ai-task-workspace-loading__result" />
          </div>

          <Skeleton className="ai-task-workspace-loading__button" />
        </div>

        <div className="ai-task-workspace-loading__controls">
          <Skeleton className="ai-task-workspace-loading__search" />
          <Skeleton className="ai-task-workspace-loading__select" />
          <Skeleton className="ai-task-workspace-loading__select" />
          <Skeleton className="ai-task-workspace-loading__select" />
        </div>

        <div className="ai-task-workspace-loading__toggles">
          <Skeleton className="ai-task-workspace-loading__toggle" />
          <Skeleton className="ai-task-workspace-loading__toggle" />
        </div>
      </Card>

      <div className="ai-task-workspace-loading__summary">
        <Skeleton className="ai-task-workspace-loading__result" />
        <Skeleton className="ai-task-workspace-loading__badge" />
      </div>

      <div className="ai-task-list">
        {Array.from(
          {
            length: LOADING_CARD_COUNT,
          },
          (_, index) => (
            <Card
              className="ai-task-workspace-loading__card"
              key={`task-${index}`}
            >
              <div className="ai-task-workspace-loading__card-heading">
                <Skeleton className="ai-task-workspace-loading__icon" />

                <div>
                  <Skeleton className="ai-task-workspace-loading__category" />
                  <Skeleton className="ai-task-workspace-loading__title" />
                </div>
              </div>

              <Skeleton className="ai-task-workspace-loading__line" />
              <Skeleton className="ai-task-workspace-loading__line ai-task-workspace-loading__line--short" />

              <Skeleton className="ai-task-workspace-loading__recommendation" />

              <div className="ai-task-workspace-loading__metrics">
                {Array.from(
                  {
                    length: 4,
                  },
                  (_, metricIndex) => (
                    <Skeleton
                      className="ai-task-workspace-loading__metric"
                      key={`metric-${metricIndex}`}
                    />
                  ),
                )}
              </div>
            </Card>
          ),
        )}
      </div>
    </div>
  );
}