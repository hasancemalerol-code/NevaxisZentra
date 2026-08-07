import {
  ArrowLeft,
  CalendarClock,
  CalendarRange,
  Factory,
  ShieldCheck,
  Wrench,
} from "lucide-react";
import Link from "next/link";
import { notFound } from "next/navigation";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import { ProductionOrderLifecycleActions } from "@/features/production-execution/components/production-order-lifecycle-actions";
import { ProductionQuantityPanel } from "@/features/production-execution/components/production-quantity-panel";
import { ProductionOperatorActions } from "@/features/production-execution/components/production-operator-actions";
import { ProductionEntryHistory } from "@/features/production-execution/components/production-entry-history";
import { ProductionDowntimePanel } from "@/features/production-execution/components/production-downtime-panel";
import { ProductionSetupPanel } from "@/features/production-execution/components/production-setup-panel";
import { ProductionCyclePanel } from "@/features/production-execution/components/production-cycle-panel";
import { ProductionRuntimeAnalytics } from "@/features/production-execution/components/production-runtime-analytics";
import { getProductionOrderStatusLabel } from "@/features/production-execution/domain/production-order";
import {
  readProductionOrderById,
  readProductionEntries,
  readDowntimes,
  readSetupSessions,
  readCycleEntries,
  readRuntimeSummary,
} from "@/features/production-execution/server/production-order-reader";
import {
  completeProductionOrderAction,
  pauseProductionOrderAction,
  resumeProductionOrderAction,
  startProductionOrderAction,
  recordGoodProductionAction,
  recordScrapAction,
  startDowntimeAction,
  endDowntimeAction,
  startSetupSessionAction,
  completeSetupSessionAction,
  recordManualCycleAction,
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

function shortId(id: string): string {
  if (id.length > 12) return `…${id.slice(-8)}`;
  return id;
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

  const [result, entriesResult, downtimesResult, setupsResult, cyclesResult, runtimeResult] =
    await Promise.all([
      readProductionOrderById(orderId),
      readProductionEntries(orderId, 50),
      readDowntimes(orderId, 20),
      readSetupSessions(orderId, 20),
      readCycleEntries(orderId, 20),
      readRuntimeSummary(orderId),
    ]);

  if (result.status === "failure") {
    if (result.code === "not-found") {
      notFound();
    }

    return (
      <main className="production-order-detail-page">
        <div className="production-order-detail-page__back">
          <Link href="/uretim">
            <ArrowLeft aria-hidden="true" />
            Üretim emirlerine dön
          </Link>
        </div>

        <div role="alert" className="state-panel error-state">
          <div className="state-icon">
            <ShieldCheck aria-hidden="true" />
          </div>
          <div>
            <h3>Üretim emri yüklenemedi</h3>
            <p>Veriler alınırken bir hata oluştu. Lütfen sayfayı yenileyin.</p>
          </div>
        </div>
      </main>
    );
  }

  const { order } = result;
  const entries = entriesResult.status === "success" ? entriesResult.entries : [];
  const downtimes = downtimesResult.status === "success" ? downtimesResult.downtimes : [];
  const setups = setupsResult.status === "success" ? setupsResult.sessions : [];
  const cycles = cyclesResult.status === "success" ? cyclesResult.cycles : [];
  const runtimeSummary =
    runtimeResult.status === "success"
      ? runtimeResult.summary
      : {
          currentElapsedSeconds: 0,
          totalDowntimeSeconds: order.totalDowntimeSeconds ?? 0,
          totalSetupSeconds: order.totalSetupSeconds ?? 0,
          netRunningSeconds: 0,
          manualCycleCount: order.manualCycleCount ?? 0,
          averageManualCycleMilliseconds:
            (order.manualCycleCount ?? 0) > 0 && (order.totalManualCycleDurationMilliseconds ?? 0) > 0
              ? (order.totalManualCycleDurationMilliseconds ?? 0) / (order.manualCycleCount ?? 1)
              : undefined,
          lastCycleAtUtc: order.lastCycleAtUtc,
          activeDowntime: undefined,
          activeSetup: undefined,
        };

  const activeDowntime = runtimeSummary.activeDowntime;
  const activeSetup = runtimeSummary.activeSetup;

  return (
    <main className="production-order-detail-page">
      {/* Back navigation */}
      <div className="production-order-detail-page__back">
        <Link href="/uretim">
          <ArrowLeft aria-hidden="true" />
          Üretim emirlerine dön
        </Link>
      </div>

      {/* 1 — Page header */}
      <header className="production-order-detail-page__header">
        <div>
          <p className="production-order-detail-page__header-eyebrow">
            ÜRETİM EMRİ
          </p>

          <h1>{order.title}</h1>

          <span className="production-order-detail-page__header-ref">
            {order.referenceCode}
          </span>
        </div>

        <div className="production-order-detail-page__badges">
          <Badge tone={getStatusTone(order.status)}>
            {getProductionOrderStatusLabel(order.status)}
          </Badge>
          {activeDowntime && (
            <Badge tone="warning">Aktif Duruş</Badge>
          )}
          {activeSetup && (
            <Badge tone="information">Aktif Setup</Badge>
          )}
        </div>
      </header>

      {/* 2 — Key operational summary */}
      <section
        className="production-order-detail-page__summary"
        aria-label="Operasyonel özet"
      >
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

      {/* 3 — Runtime analytics */}
      <section
        className="production-order-detail-page__runtime"
        aria-label="Çalışma analitiği"
      >
        <ProductionRuntimeAnalytics summary={runtimeSummary} />
      </section>

      {/* 4 — Quantity / progress */}
      <section
        className="production-order-detail-page__quantity"
        aria-label="Üretim miktarı"
      >
        <ProductionQuantityPanel summary={order.quantitySummary} />
      </section>

      {/* 5 — Lifecycle controls */}
      <ProductionOrderLifecycleActions
        orderId={order.id}
        orderTitle={order.title}
        status={order.status}
        version={order.version}
        quantitySummary={order.quantitySummary}
        onStart={startProductionOrderAction}
        onPause={pauseProductionOrderAction}
        onResume={resumeProductionOrderAction}
        onComplete={completeProductionOrderAction}
      />

      {/* 6 — Downtime + Setup + Cycle panels */}
      <section
        className="production-order-detail-page__ops-panels"
        aria-label="Operasyonel yönetim"
      >
        <ProductionDowntimePanel
          orderId={order.id}
          status={order.status}
          version={order.version}
          activeDowntime={activeDowntime}
          recentDowntimes={downtimes}
          totalDowntimeSeconds={runtimeSummary.totalDowntimeSeconds}
          onStartDowntime={startDowntimeAction}
          onEndDowntime={endDowntimeAction}
        />

        <ProductionSetupPanel
          orderId={order.id}
          status={order.status}
          version={order.version}
          activeSetup={activeSetup}
          recentSessions={setups}
          totalSetupSeconds={runtimeSummary.totalSetupSeconds}
          onStartSetup={startSetupSessionAction}
          onCompleteSetup={completeSetupSessionAction}
        />

        <ProductionCyclePanel
          orderId={order.id}
          status={order.status}
          version={order.version}
          manualCycleCount={runtimeSummary.manualCycleCount}
          averageManualCycleMilliseconds={runtimeSummary.averageManualCycleMilliseconds}
          lastCycleAtUtc={runtimeSummary.lastCycleAtUtc}
          recentCycles={cycles}
          onRecordCycle={recordManualCycleAction}
        />
      </section>

      {/* 7 — Operator quantity actions */}
      <section
        className="production-order-detail-page__actions"
        aria-label="Operatör miktarı girişi"
      >
        <ProductionOperatorActions
          orderId={order.id}
          status={order.status}
          version={order.version}
          onRecordGood={recordGoodProductionAction}
          onRecordScrap={recordScrapAction}
          plannedQuantity={order.quantitySummary.plannedQuantity}
          goodQuantity={order.quantitySummary.goodQuantity}
        />
      </section>

      {/* 8 — Execution history */}
      <section
        className="production-order-detail-page__history"
        aria-label="Üretim geçmişi"
      >
        <ProductionEntryHistory entries={entries} />
      </section>

      {/* 9 — Source planning context */}
      <section
        className="production-order-detail-page__content"
        aria-label="Kaynak planlama bağlamı"
      >
        <div className="production-order-detail-page__context-card">
          <p>KAYNAK PLANLAMA</p>

          <h2>Versiyon {order.sourceProductionPlanVersion}</h2>

          <span>
            Plan:{" "}
            <span className="production-order-detail-page__context-card__guid">
              {shortId(order.sourceProductionPlanId)}
            </span>
          </span>
        </div>

        <div className="production-order-detail-page__context-card">
          <p>KAYIT BİLGİSİ</p>

          <h2>v{order.version} — {formatUtcDate(order.createdAtUtc)}</h2>

          <span>
            El değişimi:{" "}
            <span className="production-order-detail-page__context-card__guid">
              {shortId(order.productionPlanHandoffId)}
            </span>
          </span>
        </div>
      </section>
    </main>
  );
}
