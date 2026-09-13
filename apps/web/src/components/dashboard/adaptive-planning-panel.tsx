import Link from "next/link";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";

import {
  ADAPTIVE_PLANNING_TITLE,
  formatAdaptiveDateTime,
  formatAdaptivePercent,
  getAdaptiveLifecycleBadgeTone,
  getAdaptiveLifecycleStatusLabel,
  getChangeTypeLabel,
  getDeadlineRiskLevelLabel,
  getTriggerTypeLabel,
} from "@/features/adaptive-production-planning/domain/adaptive-planning-labels";
import { readAdaptivePlanningDashboardPanel } from "@/features/adaptive-production-planning/server/adaptive-planning-reader";

export async function AdaptivePlanningPanel() {
  const panel = await readAdaptivePlanningDashboardPanel();
  if (panel.status === "failure") {
    return null;
  }

  const capacityLabel =
    panel.capacityOverloads.length > 0
      ? panel.capacityOverloads[0]
      : `${formatAdaptivePercent(panel.metrics.plannedUtilizationPercent)} planlı kullanım`;

  return (
    <section className="dashboard-panel" data-testid="dashboard-adaptive-planning-panel">
      <p className="shift-ops-hero__eyebrow">{ADAPTIVE_PLANNING_TITLE}</p>

      <div className="adaptive-planning-dashboard-grid">
        <Card>
          <span>Plan uyumu</span>
          <strong>{formatAdaptivePercent(panel.metrics.planAdherencePercent)}</strong>
        </Card>
        <Card>
          <span>Riskli sipariş</span>
          <strong>{panel.metrics.ordersAtRisk}</strong>
        </Card>
        <Card>
          <span>Kapasite</span>
          <strong>{capacityLabel}</strong>
        </Card>
      </div>

      <ul>
        <li>
          Sonraki kritik olay:{" "}
          {panel.nextCriticalRisk
            ? `${panel.nextCriticalRisk.orderId} · ${getDeadlineRiskLevelLabel(panel.nextCriticalRisk.riskLevel)} · ${formatAdaptiveDateTime(panel.nextCriticalRisk.deadlineUtc)}`
            : "Kritik olay yok"}
        </li>
        <li>
          Son replan:{" "}
          {panel.lastReplan
            ? `${getChangeTypeLabel(panel.lastReplan.changeType)} · ${getTriggerTypeLabel(panel.lastReplan.triggerType)} · ${formatAdaptiveDateTime(panel.lastReplan.createdAtUtc)}`
            : "Replan kaydı yok"}
        </li>
        {panel.activePlan ? (
          <li>
            Aktif plan: {panel.activePlan.title}
            <Badge tone={getAdaptiveLifecycleBadgeTone(panel.activePlan.adaptiveLifecycleStatus)}>
              {getAdaptiveLifecycleStatusLabel(panel.activePlan.adaptiveLifecycleStatus)}
            </Badge>
          </li>
        ) : null}
      </ul>

      <Link className="shift-ops-cta" href="/planlama">
        Planlama Merkezi
      </Link>
    </section>
  );
}
