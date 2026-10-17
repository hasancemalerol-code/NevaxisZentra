import { formatDateTime } from "@/lib/presentation/format";
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
import { ProductionQuantityPanel } from "@/features/production-execution/components/production-quantity-panel";
import { ProductionOperatorActions } from "@/features/production-execution/components/production-operator-actions";
import { ProductionEntryHistory } from "@/features/production-execution/components/production-entry-history";
import { ProductionDowntimePanel } from "@/features/production-execution/components/production-downtime-panel";
import { ProductionSetupPanel } from "@/features/production-execution/components/production-setup-panel";
import { ProductionCyclePanel } from "@/features/production-execution/components/production-cycle-panel";
import { ProductionRuntimeAnalytics } from "@/features/production-execution/components/production-runtime-analytics";
import { ProductionHealthPanel } from "@/features/production-execution/components/production-health-panel";
import { ProductionExceptionPanel } from "@/features/production-execution/components/production-exception-panel";
import { ProductionShiftPanel } from "@/features/production-execution/components/production-shift-panel";
import { ProductionOperatorPanel } from "@/features/production-execution/components/production-operator-panel";
import { ProductionHandoverPanel } from "@/features/production-execution/components/production-handover-panel";
import { getProductionOrderStatusLabel } from "@/features/production-execution/domain/production-order";
import {
  readProductionOrderById,
  readProductionEntries,
  readDowntimes,
  readSetupSessions,
  readCycleEntries,
  readRuntimeSummary,
  readExceptions,
  readHealthSummary,
  readShiftAssignments,
  readOperatorAssignments,
  readHandovers,
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
  raiseExceptionAction,
  acknowledgeExceptionAction,
  resolveExceptionAction,
  recoverDowntimeAction,
  recoverSetupSessionAction,
  assignShiftAction,
  reassignShiftAction,
  assignOperatorAction,
  releaseOperatorAction,
  createHandoverAction,
  assignExceptionAction,
  unassignExceptionAction,
} from "@/features/production-execution/server/production-order-writer";
import type { ProductionOrderStatus } from "@/features/production-execution/domain/production-order";
import { FailureStatePage } from "@/components/ui/failure-state";

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
  return formatDateTime(iso);
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

  const [result, entriesResult, downtimesResult, setupsResult, cyclesResult, runtimeResult, exceptionsResult, healthResult, shiftAssignmentsResult, operatorAssignmentsResult, handoversResult] =
    await Promise.all([
      readProductionOrderById(orderId),
      readProductionEntries(orderId, 50),
      readDowntimes(orderId, 20),
      readSetupSessions(orderId, 20),
      readCycleEntries(orderId, 20),
      readRuntimeSummary(orderId),
      readExceptions(orderId, 30),
      readHealthSummary(orderId),
      readShiftAssignments(orderId, 10),
      readOperatorAssignments(orderId, 20),
      readHandovers(orderId, 10),
    ]);

  if (result.status === "failure") {
    if (result.code === "not-found") {
      notFound();
    }

    return (
      <div className="production-order-detail-page">
        <div className="production-order-detail-page__back">
          <Link href="/uretim">
            <ArrowLeft aria-hidden="true" />
            Üretim emirlerine dön
          </Link>
        </div>

        <FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Üretim emri yüklenemedi" inline />
      </div>
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
  const exceptions = exceptionsResult.status === "success" ? exceptionsResult.exceptions : [];
  const healthSummary = healthResult.status === "success" ? healthResult.summary : null;
  const shiftAssignments = shiftAssignmentsResult.status === "success" ? shiftAssignmentsResult.assignments : [];
  const operatorAssignments = operatorAssignmentsResult.status === "success" ? operatorAssignmentsResult.assignments : [];
  const handovers = handoversResult.status === "success" ? handoversResult.handovers : [];

  return (
    <div className="production-order-detail-page">
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
            ÜRETİM
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
            <Badge tone="information">Aktif Kurulum</Badge>
          )}
          {healthSummary?.scheduleOverrun && order.status !== "completed" && (
            <Badge tone="warning">Planlanan bitiş aşıldı</Badge>
          )}
          {healthSummary?.criticalExceptionCount !== undefined && healthSummary.criticalExceptionCount > 0 && (
            <Badge tone="danger">Kritik İstisna</Badge>
          )}
          {order.currentShiftCode && (
            <Badge tone="information">{order.currentShiftCode}</Badge>
          )}
          {order.primaryOperatorSubjectId && (
            <Badge tone="neutral">Ana Operatör Atandı</Badge>
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

      {/* 3 — Operational health summary (4L) */}
      {healthSummary && (
        <section
          className="production-order-detail-page__health"
          aria-label="Operasyon sağlığı"
        >
          <ProductionHealthPanel summary={healthSummary} />
        </section>
      )}

      {/* 3b — 5L: Shift + Operator + Handover panels */}
      <section
        className="production-order-detail-page__resources"
        aria-label="Vardiya ve operatör"
      >
        <ProductionShiftPanel
          orderId={order.id}
          version={order.version}
          status={order.status}
          currentShiftCode={order.currentShiftCode}
          currentShiftStartAtUtc={order.currentShiftStartAtUtc}
          currentShiftEndAtUtc={order.currentShiftEndAtUtc}
          recentAssignments={shiftAssignments}
          onAssignShift={assignShiftAction}
          onReassignShift={reassignShiftAction}
        />

        <ProductionOperatorPanel
          orderId={order.id}
          version={order.version}
          status={order.status}
          primaryOperatorSubjectId={order.primaryOperatorSubjectId}
          activeOperatorCount={order.activeOperatorCount}
          recentAssignments={operatorAssignments}
          onAssignOperator={assignOperatorAction}
          onReleaseOperator={releaseOperatorAction}
        />

        <ProductionHandoverPanel
          orderId={order.id}
          version={order.version}
          status={order.status}
          currentShiftCode={order.currentShiftCode}
          handovers={handovers}
          onCreateHandover={createHandoverAction}
        />
      </section>

      {/* 3c — Runtime analytics */}
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

      {/* 6 — Downtime + Kurulum + Cycle panels */}
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
          downtimeIsStale={healthSummary?.downtimeIsStale}
          onStartDowntime={startDowntimeAction}
          onEndDowntime={endDowntimeAction}
          onRecoverDowntime={recoverDowntimeAction}
        />

        <ProductionSetupPanel
          orderId={order.id}
          status={order.status}
          version={order.version}
          activeSetup={activeSetup}
          recentSessions={setups}
          totalSetupSeconds={runtimeSummary.totalSetupSeconds}
          setupIsStale={healthSummary?.setupIsStale}
          onStartSetup={startSetupSessionAction}
          onCompleteSetup={completeSetupSessionAction}
          onRecoverSetup={recoverSetupSessionAction}
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

      {/* 6b — Exception management panel (4L) */}
      <section
        className="production-order-detail-page__exceptions"
        aria-label="Operasyon istisnaları"
      >
        <ProductionExceptionPanel
          orderId={order.id}
          version={order.version}
          exceptions={exceptions}
          onRaiseException={raiseExceptionAction}
          onAcknowledgeException={acknowledgeExceptionAction}
          onResolveException={resolveExceptionAction}
          onAssignException={assignExceptionAction}
          onUnassignException={unassignExceptionAction}
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

          <Link href={`/planlama/${order.sourceProductionPlanId}`}>Kaynak planı aç</Link>
        </div>

        <div className="production-order-detail-page__context-card">
          <p>KAYIT BİLGİSİ</p>

          <h2>v{order.version} — {formatUtcDate(order.createdAtUtc)}</h2>

          <span>Üretim planından devredildi</span>
        </div>
      </section>
    </div>
  );
}
