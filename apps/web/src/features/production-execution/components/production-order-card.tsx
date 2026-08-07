import Link from "next/link";
import { CalendarClock, Factory, Wrench } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";

import type { ProductionOrderStatus, ProductionOrderSummary } from "../domain/production-order";
import { getProductionOrderStatusLabel } from "../domain/production-order";

type BadgeTone =
  | "neutral"
  | "information"
  | "success"
  | "warning"
  | "danger"
  | "ai";

function getStatusTone(status: ProductionOrderStatus): BadgeTone {
  switch (status) {
    case "in-progress":
      return "information";
    case "paused":
      return "warning";
    case "completed":
      return "success";
    case "cancelled":
      return "danger";
    case "ready":
    default:
      return "neutral";
  }
}

function formatUtcDate(iso: string): string {
  try {
    return new Date(iso).toLocaleDateString("tr-TR", {
      day: "2-digit",
      month: "short",
      year: "numeric",
    });
  } catch {
    return iso;
  }
}

type ProductionOrderCardProps = Readonly<{
  order: ProductionOrderSummary;
}>;

export function ProductionOrderCard({ order }: ProductionOrderCardProps) {
  return (
    <Card className="production-order-card">
      <div className="production-order-card__heading">
        <div>
          <div className="production-order-card__badges">
            <Badge tone={getStatusTone(order.status)}>
              {getProductionOrderStatusLabel(order.status)}
            </Badge>
          </div>

          <h2>{order.title}</h2>

          <p>{order.referenceCode}</p>
        </div>
      </div>

      <div className="production-order-card__summary">
        <div>
          <CalendarClock aria-hidden="true" />

          <div>
            <span>Planlanan başlangıç</span>
            <strong>{formatUtcDate(order.plannedStartAtUtc)}</strong>
            <small>{formatUtcDate(order.plannedEndAtUtc)}</small>
          </div>
        </div>

        {order.actualStartAtUtc ? (
          <div>
            <CalendarClock aria-hidden="true" />

            <div>
              <span>Gerçek başlangıç</span>
              <strong>{formatUtcDate(order.actualStartAtUtc)}</strong>
            </div>
          </div>
        ) : null}

        <div>
          <Factory aria-hidden="true" />

          <div>
            <span>Makine</span>
            <strong>{order.machineReference ?? "Makine atanmadı"}</strong>
            <small>{order.moldReference ?? "Kalıp atanmadı"}</small>
          </div>
        </div>

        <div>
          <Wrench aria-hidden="true" />

          <div>
            <span>Vardiya</span>
            <strong>{order.shiftName ?? "Vardiya belirlenmedi"}</strong>
          </div>
        </div>
      </div>

      <div className="production-order-card__footer">
        <Link
          className="production-order-card__link"
          href={`/uretim/${order.id}`}
        >
          Siparişi incele
        </Link>
      </div>
    </Card>
  );
}
