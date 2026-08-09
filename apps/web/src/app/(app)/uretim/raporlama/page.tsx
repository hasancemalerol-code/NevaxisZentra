import Link from "next/link";
import {
  AlertTriangle,
  BarChart3,
  Clock3,
  Factory,
  Flame,
  Package,
  Play,
  ShieldCheck,
  Truck,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import { readProductionReportingWorkspace } from "@/features/production-reporting/server/production-reporting-reader";

function formatMinutes(minutes: number): string {
  if (minutes < 60) {
    return `${minutes} dk`;
  }

  const hours = Math.floor(minutes / 60);
  const remainder = minutes % 60;
  return remainder === 0 ? `${hours} sa` : `${hours} sa ${remainder} dk`;
}

function logisticsTone(riskLevel: string): "neutral" | "information" | "success" | "warning" | "danger" {
  switch (riskLevel) {
    case "critical":
      return "danger";
    case "high":
      return "warning";
    case "medium":
      return "information";
    case "low":
      return "success";
    default:
      return "neutral";
  }
}

export default async function ProductionReportingPage() {
  const result = await readProductionReportingWorkspace();

  if (result.status === "failure") {
    return (
      <main className="production-execution-page">
        <header className="production-execution-header">
          <p>ÜRETİM RAPORLAMA</p>
          <h1>Operasyonel Raporlama</h1>
        </header>

        <div role="alert" className="state-panel error-state">
          <ShieldCheck aria-hidden="true" />
          <div>
            <h3>Raporlama verisi yüklenemedi</h3>
            <p>{result.message}</p>
          </div>
        </div>
      </main>
    );
  }

  const { overview, orders, shifts, downtime, exceptions } = result;

  return (
    <main className="production-execution-page">
      <header className="production-execution-header">
        <p>ÜRETİM RAPORLAMA</p>
        <h1>Operasyonel Raporlama</h1>
        <span>
          Üretim emirleri, miktar, fire, duruş, vardiya ve istisna özetlerini
          tek operasyonel görünümde izleyin.
        </span>
      </header>

      <section className="production-execution-kpis" aria-label="Raporlama özeti">
        <Card className="production-execution-kpis__item--in-progress">
          <Play aria-hidden="true" />
          <div>
            <span>Aktif İş Emirleri</span>
            <strong>{overview.activeOrderCount}</strong>
          </div>
        </Card>

        <Card className="production-execution-kpis__item--completed">
          <Factory aria-hidden="true" />
          <div>
            <span>Tamamlanan</span>
            <strong>{overview.completedOrderCount}</strong>
          </div>
        </Card>

        <Card className="production-execution-kpis__item--total">
          <Package aria-hidden="true" />
          <div>
            <span>Üretim Miktarı</span>
            <strong>{overview.goodQuantity}</strong>
          </div>
        </Card>

        <Card className="production-execution-kpis__item--paused">
          <Flame aria-hidden="true" />
          <div>
            <span>Fire</span>
            <strong>{overview.scrapQuantity}</strong>
          </div>
        </Card>

        <Card className="production-execution-kpis__item--paused">
          <Clock3 aria-hidden="true" />
          <div>
            <span>Duruş Süresi</span>
            <strong>{formatMinutes(Number(overview.totalDowntimeMinutes))}</strong>
          </div>
        </Card>

        <Card className="production-execution-kpis__item--ready">
          <AlertTriangle aria-hidden="true" />
          <div>
            <span>Açık İstisnalar</span>
            <strong>{overview.openExceptionCount}</strong>
          </div>
        </Card>

        <Card className="production-execution-kpis__item--total">
          <BarChart3 aria-hidden="true" />
          <div>
            <span>Vardiya Durumu</span>
            <strong>{overview.shiftCoverageCount}</strong>
            <small>Aktif vardiya kapsamı</small>
          </div>
        </Card>

        <Card className="production-execution-kpis__item--in-progress">
          <Truck aria-hidden="true" />
          <div>
            <span>Termin / Sevkiyat Riski</span>
            <strong>
              {overview.logisticsRisk.available
                ? overview.logisticsRisk.riskLevel
                : "—"}
            </strong>
            <small>
              {overview.logisticsRisk.available
                ? `${overview.logisticsRisk.slackMinutes} dk slack`
                : "Planlama lojistiği yok"}
            </small>
          </div>
        </Card>
      </section>

      {overview.riskSignals.length > 0 ? (
        <section className="production-execution-safety" aria-label="Operasyonel risk sinyalleri">
          <div>
            <AlertTriangle aria-hidden="true" />
            <div>
              <p>OPERASYONEL RİSK</p>
              <h2>Deterministik üretim risk sinyalleri</h2>
              <div className="production-execution-workspace__list">
                {overview.riskSignals.map((signal) => (
                  <Badge key={`${signal.code}-${signal.relatedProductionOrderId ?? "global"}`} tone="warning">
                    {signal.label}
                  </Badge>
                ))}
              </div>
            </div>
          </div>
          {overview.logisticsRisk.available ? (
            <Badge tone={logisticsTone(overview.logisticsRisk.riskLevel)}>
              Planlama lojistiği: {overview.logisticsRisk.riskLevel}
            </Badge>
          ) : null}
        </section>
      ) : null}

      <section className="production-execution-workspace" aria-label="Emir performansı">
        <div className="production-execution-workspace__heading">
          <div>
            <p>PLAN VS ACTUAL</p>
            <h2>Emir performansı</h2>
          </div>
        </div>

        <div className="production-execution-workspace__list">
          {orders.map((order) => (
            <Card key={order.productionOrderId} className="production-order-card">
              <div className="production-order-card__header">
                <div>
                  <p>{order.referenceCode}</p>
                  <h3>{order.title}</h3>
                </div>
                <Badge tone={order.hasActiveException ? "warning" : "neutral"}>
                  {order.status}
                </Badge>
              </div>
              <div className="production-order-card__meta">
                <span>Plan: {order.planActual.plannedQuantity ?? "—"}</span>
                <span>İyi: {order.planActual.goodQuantity}</span>
                <span>Fire: {order.planActual.scrapQuantity}</span>
                <span>Tamamlanma: {order.planActual.completionPercent ?? "—"}%</span>
              </div>
              <Link href={`/uretim/${order.productionOrderId}`}>Emir detayına git</Link>
            </Card>
          ))}
        </div>
      </section>

      <section className="production-execution-workspace" aria-label="Vardiya özeti">
        <div className="production-execution-workspace__heading">
          <div>
            <p>VARDIYA</p>
            <h2>Vardiya özeti</h2>
          </div>
        </div>
        <div className="production-execution-workspace__list">
          {shifts.map((shift) => (
            <Card key={shift.shiftId}>
              <strong>{shift.shiftCode}</strong> — {shift.shiftName}
              <div>Emir: {shift.productionOrderCount}</div>
              <div>İyi/Fire: {shift.goodQuantity}/{shift.scrapQuantity}</div>
              <div>Duruş: {formatMinutes(Number(shift.downtimeMinutes))}</div>
              <div>Devir: {shift.handoverState}</div>
            </Card>
          ))}
        </div>
      </section>

      <section className="production-execution-workspace" aria-label="Duruş özeti">
        <div className="production-execution-workspace__heading">
          <div>
            <p>DURUŞ</p>
            <h2>Duruş analizi</h2>
          </div>
        </div>
        <p>Toplam duruş: {formatMinutes(Number(downtime.totalDowntimeMinutes))}</p>
        <div className="production-execution-workspace__list">
          {downtime.events.slice(0, 8).map((event) => (
            <Card key={event.downtimeId}>
              <strong>{event.orderReferenceCode}</strong> — {event.reasonCode}
              <div>{event.durationMinutes ? formatMinutes(event.durationMinutes) : "Aktif"}</div>
            </Card>
          ))}
        </div>
      </section>

      <section className="production-execution-workspace" aria-label="İstisna özeti">
        <div className="production-execution-workspace__heading">
          <div>
            <p>İSTİSNA</p>
            <h2>İstisna analizi</h2>
          </div>
        </div>
        <p>Açık: {exceptions.openCount} · Atanmamış: {exceptions.unassignedCount}</p>
        <div className="production-execution-workspace__list">
          {exceptions.items.slice(0, 8).map((item) => (
            <Card key={item.exceptionId}>
              <strong>{item.orderReferenceCode}</strong> — {item.exceptionType}
              <div>{item.severity} · {item.status} · {item.ageMinutes} dk</div>
            </Card>
          ))}
        </div>
      </section>
    </main>
  );
}
