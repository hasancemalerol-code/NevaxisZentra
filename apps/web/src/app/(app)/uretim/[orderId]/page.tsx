import {
  ArrowLeft,
  CalendarClock,
  CalendarRange,
  Factory,
  Wrench,
} from "lucide-react";
import Link from "next/link";
import { notFound } from "next/navigation";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import { ProductionOrderLifecycleActions } from "@/features/production-execution/components/production-order-lifecycle-actions";
import { getProductionOrderStatusLabel } from "@/features/production-execution/domain/production-order";
import { readProductionOrderById } from "@/features/production-execution/server/production-order-reader";
import {
  completeProductionOrderAction,
  pauseProductionOrderAction,
  resumeProductionOrderAction,
  startProductionOrderAction,
} from "@/features/production-execution/server/production-order-writer";
import type { ProductionOrderStatus } from "@/features/production-execution/domain/production-order";

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

function formatUtcDate(iso: string | undefined): string {
  if (!iso) {
    return "—";
  }

  try {
    return new Date(iso).toLocaleDateString("tr-TR", {
      day: "2-digit",
      month: "long",
      year: "numeric",
      hour: "2-digit",
      minute: "2-digit",
    });
  } catch {
    return iso;
  }
}

type ProductionOrderDetailPageProps = Readonly<{
  params: Promise<{
    orderId: string;
  }>;
}>;

export default async function ProductionOrderDetailPage({
  params,
}: ProductionOrderDetailPageProps) {
  const { orderId } = await params;

  const result = await readProductionOrderById(orderId);

  if (result.status === "failure") {
    if (result.code === "not-found") {
      notFound();
    }

    throw new Error(
      [
        "Üretim emri detayı yüklenemedi.",
        result.code,
        result.message,
      ].join(" "),
    );
  }

  const { order } = result;

  return (
    <main className="production-order-detail-page">
      <div className="production-order-detail-page__back">
        <Link href="/uretim">
          <ArrowLeft aria-hidden="true" />
          Üretim emirlerine dön
        </Link>
      </div>

      <header className="production-order-detail-page__header">
        <div>
          <p>Üretim Emri Detayı</p>

          <h1>{order.title}</h1>

          <span>{order.referenceCode}</span>
        </div>

        <div className="production-order-detail-page__badges">
          <Badge tone={getStatusTone(order.status)}>
            {getProductionOrderStatusLabel(order.status)}
          </Badge>
        </div>
      </header>

      <section className="production-order-detail-page__summary">
        <Card>
          <CalendarRange aria-hidden="true" />

          <div>
            <span>Planlanan başlangıç</span>
            <strong>{formatUtcDate(order.plannedStartAtUtc)}</strong>
            <small>Bitiş: {formatUtcDate(order.plannedEndAtUtc)}</small>
          </div>
        </Card>

        {order.actualStartAtUtc ? (
          <Card>
            <CalendarClock aria-hidden="true" />

            <div>
              <span>Gerçek başlangıç</span>
              <strong>{formatUtcDate(order.actualStartAtUtc)}</strong>
              {order.actualEndAtUtc ? (
                <small>Bitiş: {formatUtcDate(order.actualEndAtUtc)}</small>
              ) : (
                <small>Henüz tamamlanmadı</small>
              )}
            </div>
          </Card>
        ) : null}

        <Card>
          <Factory aria-hidden="true" />

          <div>
            <span>Makine ve kalıp</span>
            <strong>{order.machineReference ?? "Makine atanmadı"}</strong>
            <small>{order.moldReference ?? "Kalıp atanmadı"}</small>
          </div>
        </Card>

        <Card>
          <Wrench aria-hidden="true" />

          <div>
            <span>Ürün ve vardiya</span>
            <strong>{order.productReference ?? "Ürün belirlenmedi"}</strong>
            <small>{order.shiftName ?? "Vardiya belirlenmedi"}</small>
          </div>
        </Card>
      </section>

      <section className="production-order-detail-page__content">
        <Card>
          <p>Kaynak planlama bilgisi</p>

          <h2>Plan: {order.sourceProductionPlanId}</h2>

          <span>
            El değişimi: {order.productionPlanHandoffId} — Versiyon:{" "}
            {order.sourceProductionPlanVersion}
          </span>
        </Card>

        <Card>
          <p>Versiyon ve değişiklik bilgisi</p>

          <h2>Versiyon {order.version}</h2>

          <span>
            Oluşturan: {order.createdBySubjectId} —{" "}
            {formatUtcDate(order.createdAtUtc)}
          </span>
        </Card>
      </section>

      <ProductionOrderLifecycleActions
        orderId={order.id}
        orderTitle={order.title}
        status={order.status}
        version={order.version}
        onStart={startProductionOrderAction}
        onPause={pauseProductionOrderAction}
        onResume={resumeProductionOrderAction}
        onComplete={completeProductionOrderAction}
      />
    </main>
  );
}
