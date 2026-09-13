import Link from "next/link";

import {
  ADAPTIVE_PLANNING_TITLE,
  formatAdaptiveDateTime,
  formatAdaptivePercent,
  getDeadlineRiskLevelLabel,
} from "../domain/adaptive-planning-labels";
import { readAdaptivePlanningShiftBrief } from "../server/adaptive-planning-reader";

type AdaptivePlanningShiftBriefPanelProps = Readonly<{
  mode: "start" | "handover";
}>;

export async function AdaptivePlanningShiftBriefPanel({ mode }: AdaptivePlanningShiftBriefPanelProps) {
  const result = await readAdaptivePlanningShiftBrief();
  if (result.status === "failure") {
    return null;
  }

  const { workspace, priorityOrders, machineAssignments, criticalMaterials, bottlenecks } = result;
  const title =
    mode === "start"
      ? "Vardiyaya Başlarken — Uyarlanabilir Planlama"
      : "Vardiya Teslimi — Plan Durumu ve Riskler";

  return (
    <section className="shift-ops-card" data-testid={`adaptive-planning-shift-brief-${mode}`}>
      <h3>{title}</h3>
      <p>{ADAPTIVE_PLANNING_TITLE}</p>

      <ul className="shift-ops-item-list">
        <li data-testid="adaptive-planning-shift-today-plan">
          <strong>
            Bugünün planı: {workspace.activePlan?.title ?? "Aktif plan yok"}
          </strong>
          <p>
            Uyum {formatAdaptivePercent(workspace.metrics.planAdherencePercent)} · Riskli sipariş{" "}
            {workspace.metrics.ordersAtRisk}
          </p>
        </li>

        <li data-testid="adaptive-planning-shift-priority-orders">
          <strong>Öncelikli siparişler</strong>
          {priorityOrders.length === 0 ? (
            <p>Öncelikli sipariş riski yok.</p>
          ) : (
            <ul>
              {priorityOrders.map((order) => (
                <li key={order.orderId}>
                  {order.orderId} · {getDeadlineRiskLevelLabel(order.riskLevel)} · Termin{" "}
                  {formatAdaptiveDateTime(order.deadlineUtc)}
                </li>
              ))}
            </ul>
          )}
        </li>

        <li data-testid="adaptive-planning-shift-machine-assignments">
          <strong>Makine atamaları</strong>
          {machineAssignments.length === 0 ? (
            <p>Planlanmış makine ataması yok.</p>
          ) : (
            <p>{machineAssignments.join(" · ")}</p>
          )}
        </li>

        <li data-testid="adaptive-planning-shift-critical-materials">
          <strong>Kritik malzemeler</strong>
          {criticalMaterials.length === 0 ? (
            <p>Kritik malzeme riski yok.</p>
          ) : (
            <p>{criticalMaterials.join(" · ")}</p>
          )}
        </li>

        <li data-testid="adaptive-planning-shift-bottlenecks">
          <strong>Tahmini darboğazlar</strong>
          {bottlenecks.length === 0 ? (
            <p>Kapasite darboğazı tespit edilmedi.</p>
          ) : (
            <p>{bottlenecks.join(" · ")}</p>
          )}
        </li>
      </ul>

      <Link className="shift-ops-cta" href="/planlama">
        Planlama Merkezi
      </Link>
    </section>
  );
}
