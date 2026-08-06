import {
  CalendarClock,
  Clock3,
  Timer,
  UsersRound,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";

import type { ProductionPlanTimelineViewModel } from "../application/production-plan-timeline-view-model";
import {
  getProductionPlanFallbackLabel,
} from "../presentation/production-plan-fallback-labels";

type ProductionPlanTimelineProps =
  Readonly<{
    viewModel: ProductionPlanTimelineViewModel;
  }>;

type TimelineDateFallbackKey =
  | "startTime"
  | "endTime";

function formatDateTime(
  value: string | null,
  fallbackKey: TimelineDateFallbackKey,
): string {
  if (!value) {
    return getProductionPlanFallbackLabel(
      fallbackKey,
    );
  }

  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return new Intl.DateTimeFormat("tr-TR", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(date);
}

export function ProductionPlanTimeline({
  viewModel,
}: ProductionPlanTimelineProps) {
  const shiftLabel =
    viewModel.shiftName ??
    viewModel.shiftId ??
    getProductionPlanFallbackLabel(
      "shift",
    );

  const durationLabel =
    viewModel.estimatedDurationMinutes
      ? viewModel.durationLabel
      : getProductionPlanFallbackLabel(
          "duration",
        );

  return (
    <section className="production-plan-timeline">
      <div className="production-plan-timeline__heading">
        <div>
          <p>Zaman çizelgesi</p>

          <h2>
            Planlanan üretim dönemi
          </h2>

          <span>
            Başlangıç, bitiş, tahmini süre ve
            vardiya bilgileri üretim emrine
            aktarılmadan önce doğrulanmalıdır.
          </span>
        </div>

        <Badge
          tone={
            viewModel.hasValidTimeWindow
              ? "success"
              : "warning"
          }
        >
          <CalendarClock aria-hidden="true" />

          {viewModel.hasValidTimeWindow
            ? "Zaman penceresi geçerli"
            : "Zaman penceresi eksik"}
        </Badge>
      </div>

      <Card className="production-plan-timeline__card">
        <div className="production-plan-timeline__track">
          <div className="production-plan-timeline__point">
            <div className="production-plan-timeline__icon">
              <Clock3 aria-hidden="true" />
            </div>

            <div>
              <span>Planlanan başlangıç</span>

              <strong>
                {formatDateTime(
                  viewModel.plannedStartAt,
                  "startTime",
                )}
              </strong>
            </div>
          </div>

          <div className="production-plan-timeline__line">
            <span />
          </div>

          <div className="production-plan-timeline__point">
            <div className="production-plan-timeline__icon">
              <CalendarClock aria-hidden="true" />
            </div>

            <div>
              <span>Planlanan bitiş</span>

              <strong>
                {formatDateTime(
                  viewModel.plannedEndAt,
                  "endTime",
                )}
              </strong>
            </div>
          </div>
        </div>

        <div className="production-plan-timeline__meta">
          <article>
            <Timer aria-hidden="true" />

            <div>
              <span>Tahmini süre</span>

              <strong>
                {durationLabel}
              </strong>
            </div>
          </article>

          <article>
            <UsersRound aria-hidden="true" />

            <div>
              <span>Vardiya</span>

              <strong>{shiftLabel}</strong>

              {viewModel.shiftName &&
              viewModel.shiftId ? (
                <small>
                  {viewModel.shiftId}
                </small>
              ) : null}
            </div>
          </article>
        </div>
      </Card>
    </section>
  );
}