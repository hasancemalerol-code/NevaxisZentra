import Link from "next/link";
import {
  ArrowRight,
  CalendarClock,
  Factory,
  Users,
  Wrench,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";

import type {
  ProductionOrderStatus,
  ProductionOrderSummary,
} from "../domain/production-order";
import { getProductionOrderStatusLabel } from "../domain/production-order";

function formatQuantity(value: number): string {
  return value.toLocaleString("tr-TR", { maximumFractionDigits: 0 });
}

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
  const hasQuantityData =
    order.goodQuantity > 0 ||
    order.plannedQuantity !== undefined ||
    order.scrapQuantity > 0;

  const completionPct =
    order.plannedQuantity !== undefined && order.plannedQuantity > 0
      ? (order.goodQuantity / order.plannedQuantity) * 100
      : undefined;

  const progressBarWidth =
    completionPct !== undefined ? Math.min(completionPct, 100) : undefined;

  return (
    <Card className="production-order-card">
      {/* Heading: status badge + title + reference */}
      <div className="production-order-card__heading">
        <div className="production-order-card__badges">
          <Badge tone={getStatusTone(order.status)}>
            {getProductionOrderStatusLabel(order.status)}
          </Badge>
        </div>

        <h2>{order.title}</h2>

        <p>{order.referenceCode}</p>
      </div>

      {/* Metadata grid */}
      <div className="production-order-card__summary">
        <div>
          <CalendarClock aria-hidden="true" />

          <div>
            <span>Planlanan başlangıç</span>
            <strong>{formatUtcDate(order.plannedStartAtUtc)}</strong>
            <small>Bitiş: {formatUtcDate(order.plannedEndAtUtc)}</small>
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
          <Users aria-hidden="true" />

          <div>
            <span>Vardiya</span>
            <strong>{order.shiftName ?? "Vardiya belirlenmedi"}</strong>
          </div>
        </div>
      </div>

      {/* Quantity / progress */}
      {hasQuantityData && (
        <div className="production-order-card__quantity">
          <div className="production-order-card__quantity-row">
            <div>
              <div className="production-order-card__quantity-label">
                İyi Üretim
              </div>
              <div className="production-order-card__quantity-values">
                <span className="production-order-card__quantity-good">
                  {formatQuantity(order.goodQuantity)}
                </span>
                {order.plannedQuantity !== undefined && (
                  <span className="production-order-card__quantity-planned">
                    {" "}/ {formatQuantity(order.plannedQuantity)} adet
                  </span>
                )}
                {order.plannedQuantity === undefined && (
                  <span className="production-order-card__quantity-planned">
                    {" "}adet
                  </span>
                )}
              </div>
            </div>

            <div className="production-order-card__quantity-meta">
              {order.scrapQuantity > 0 && (
                <span className="production-order-card__scrap">
                  <Wrench aria-hidden="true" style={{ width: 12, height: 12 }} />
                  Fire: {formatQuantity(order.scrapQuantity)} adet
                </span>
              )}
              {completionPct !== undefined && (
                <span className="production-order-card__progress">
                  %{Math.round(completionPct).toLocaleString("tr-TR")}
                </span>
              )}
            </div>
          </div>

          {progressBarWidth !== undefined && (
            <div
              className="production-order-card__progress-bar-container"
              role="progressbar"
              aria-valuenow={Math.round(progressBarWidth)}
              aria-valuemin={0}
              aria-valuemax={100}
              aria-label={`İlerleme: %${Math.round(progressBarWidth)}`}
            >
              <div
                className="production-order-card__progress-bar"
                style={{ width: `${progressBarWidth}%` }}
              />
            </div>
          )}
        </div>
      )}

      {/* Footer */}
      <div className="production-order-card__footer">
        <Link
          className="production-order-card__link"
          href={`/uretim/${order.id}`}
        >
          Üretim emrini aç
          <ArrowRight aria-hidden="true" style={{ width: 14, height: 14 }} />
        </Link>
      </div>
    </Card>
  );
}
