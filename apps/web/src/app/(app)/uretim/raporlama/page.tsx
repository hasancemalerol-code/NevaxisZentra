import Link from "next/link";
import {
  AlertTriangle,
  BarChart3,
  Clock3,
  Factory,
  Flame,
  Inbox,
  Package,
  Play,
  Truck,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import { readProductionReportingWorkspace } from "@/features/production-reporting/server/production-reporting-reader";

import { enumLabel } from "@/lib/presentation/enum-labels";
import { FailureStatePage } from "@/components/ui/failure-state";
import { formatPercent } from "@/lib/presentation/format";
import { localizeDataText } from "@/lib/presentation/display-name";
const formatNumber = (value: number) => value.toLocaleString("tr-TR", { maximumFractionDigits: 0 });

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

function ReportEmpty() {
  return (
    <div className="production-report-empty" role="status">
      <Inbox aria-hidden="true" />
      <p>Bu dönemde veri yok</p>
      <span>Kayıtlar oluştuğunda bu bölümde özetlenir.</span>
    </div>
  );
}

export default async function ProductionReportingPage() {
  const result = await readProductionReportingWorkspace();

  if (result.status === "failure") {
    return (
      <div className="production-execution-page">
        <header className="production-execution-header">
          <p>ÜRETİM</p>
          <h1>Operasyonel Raporlama</h1>
        </header>

        <FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Raporlama verisi yüklenemedi" inline />
      </div>
    );
  }

  const { overview, orders, shifts, downtime, exceptions } = result;
  const allSectionsEmpty =
    orders.length === 0 && shifts.length === 0 && downtime.events.length === 0 && exceptions.items.length === 0;

  return (
    <div className="production-execution-page">
      <header className="production-execution-header">
        <p>ÜRETİM</p>
        <h1>Operasyonel Raporlama</h1>
        <span>
          Üretim emirleri, miktar, fire, duruş, vardiya ve istisna özetlerini
          tek operasyonel görünümde izleyin.
        </span>
      </header>

      <section className="production-execution-kpis production-report-kpis" aria-label="Raporlama özeti">
        <Card className="production-execution-kpis__item--in-progress">
          <Play aria-hidden="true" />
          <div>
            <span>Aktif İş Emirleri</span>
            <strong>{overview.activeOrderCount}</strong>
            <small>Yürütülen emirler</small>
          </div>
        </Card>

        <Card className="production-execution-kpis__item--completed">
          <Factory aria-hidden="true" />
          <div>
            <span>Tamamlanan</span>
            <strong>{overview.completedOrderCount}</strong>
            <small>Kapanan emirler</small>
          </div>
        </Card>

        <Card className="production-execution-kpis__item--total">
          <Package aria-hidden="true" />
          <div>
            <span>Üretim Miktarı</span>
            <strong>{formatNumber(overview.goodQuantity)}</strong>
            <small>İyi adet</small>
          </div>
        </Card>

        <Card className="production-execution-kpis__item--paused">
          <Flame aria-hidden="true" />
          <div>
            <span>Fire</span>
            <strong>{formatNumber(overview.scrapQuantity)}</strong>
            <small>Hatalı adet</small>
          </div>
        </Card>

        <Card className="production-execution-kpis__item--paused">
          <Clock3 aria-hidden="true" />
          <div>
            <span>Duruş Süresi</span>
            <strong>{formatMinutes(Number(overview.totalDowntimeMinutes))}</strong>
            <small>Toplam süre</small>
          </div>
        </Card>

        <Card className="production-execution-kpis__item--ready">
          <AlertTriangle aria-hidden="true" />
          <div>
            <span>Açık İstisnalar</span>
            <strong>{overview.openExceptionCount}</strong>
            <small>Çözüm bekleyen</small>
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
            <span>Sevkiyat Riski</span>
            <strong>
              {overview.logisticsRisk.available
                ? enumLabel(overview.logisticsRisk.riskLevel)
                : "—"}
            </strong>
            <small>
              {overview.logisticsRisk.available
                ? overview.logisticsRisk.slackMinutes < 0 ? `${formatMinutes(Math.abs(overview.logisticsRisk.slackMinutes))} gecikme` : `${formatMinutes(overview.logisticsRisk.slackMinutes)} termin payı`
                : "Veri yok"}
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
              <h2>Üretim risk sinyalleri</h2>
              <div className="production-execution-workspace__list">
                {overview.riskSignals.map((signal) => (
                  <Badge key={`${signal.code}-${signal.relatedProductionOrderId ?? "global"}`} tone="warning">
                    {localizeDataText(signal.label)}
                  </Badge>
                ))}
              </div>
            </div>
          </div>
          {overview.logisticsRisk.available ? (
            <Badge tone={logisticsTone(overview.logisticsRisk.riskLevel)}>
              Planlama lojistiği: {enumLabel(overview.logisticsRisk.riskLevel)}
            </Badge>
          ) : null}
        </section>
      ) : null}

      <div className={`production-report-sections${allSectionsEmpty ? " production-report-sections--empty" : ""}`}>
        <section className="production-execution-workspace production-report-section" aria-label="Emir performansı">
          <div className="production-execution-workspace__heading">
            <div>
              <p>Plan ve gerçekleşen</p>
              <h2>Emir performansı</h2>
            </div>
          </div>

          {orders.length === 0 ? <ReportEmpty /> : (
          <div className="production-execution-workspace__list">
            {orders.map((order) => (
              <Card key={order.productionOrderId} className="production-order-card">
                <div className="production-order-card__header">
                  <div>
                    <p>{order.referenceCode}</p>
                    <h2>{order.title}</h2>
                  </div>
                  <Badge tone={order.hasActiveException ? "warning" : "neutral"}>
                    {enumLabel(order.status)}
                  </Badge>
                </div>
                <div className="production-order-card__meta">
                  <span>Plan: {order.planActual.plannedQuantity != null ? formatNumber(order.planActual.plannedQuantity) : "—"}</span>
                  <span>İyi: {formatNumber(order.planActual.goodQuantity)}</span>
                  <span>Fire: {formatNumber(order.planActual.scrapQuantity)}</span>
                  <span>Tamamlanma: {formatPercent(order.planActual.completionPercent, 0)}</span>
                </div>
                <Link href={`/uretim/${order.productionOrderId}`}>Emir detayına git</Link>
              </Card>
            ))}
          </div>
          )}
        </section>

        <section className="production-execution-workspace production-report-section" aria-label="Vardiya özeti">
          <div className="production-execution-workspace__heading">
            <div>
              <p>Vardiya</p>
              <h2>Vardiya özeti</h2>
            </div>
          </div>
          {shifts.length === 0 ? <ReportEmpty /> : (
          <div className="production-execution-workspace__list">
            {shifts.map((shift) => (
              <Card key={shift.shiftId} className="production-report-card">
                <div className="production-order-card__header">
                  <div>
                    <p>{shift.shiftCode}</p>
                    <h3>{shift.shiftName}</h3>
                  </div>
                  <Badge tone="neutral">{enumLabel(shift.handoverState)}</Badge>
                </div>
                <dl className="production-report-card__facts">
                  <div><dt>Emir</dt><dd>{shift.productionOrderCount}</dd></div>
                  <div><dt>İyi adet</dt><dd>{formatNumber(shift.goodQuantity)}</dd></div>
                  <div><dt>Fire</dt><dd>{formatNumber(shift.scrapQuantity)}</dd></div>
                  <div><dt>Duruş</dt><dd>{formatMinutes(Number(shift.downtimeMinutes))}</dd></div>
                </dl>
              </Card>
            ))}
          </div>
          )}
        </section>

        <section className="production-execution-workspace production-report-section" aria-label="Duruş özeti">
          <div className="production-execution-workspace__heading">
            <div>
              <p>Duruş</p>
              <h2>Duruş analizi</h2>
              <span>Toplam duruş: {formatMinutes(Number(downtime.totalDowntimeMinutes))}</span>
            </div>
          </div>
          {downtime.events.length === 0 ? <ReportEmpty /> : (
          <div className="production-execution-workspace__list">
            {downtime.events.slice(0, 8).map((event) => (
              <Card key={event.downtimeId} className="production-report-card">
                <div className="production-order-card__header">
                  <div>
                    <p>{event.orderReferenceCode}</p>
                    <h3>{localizeDataText(enumLabel(event.reasonCode))}</h3>
                  </div>
                  <Badge tone={event.durationMinutes ? "neutral" : "warning"}>
                    {event.durationMinutes ? formatMinutes(event.durationMinutes) : "Aktif"}
                  </Badge>
                </div>
              </Card>
            ))}
          </div>
          )}
        </section>

        <section className="production-execution-workspace production-report-section" aria-label="İstisna özeti">
          <div className="production-execution-workspace__heading">
            <div>
              <p>İstisna</p>
              <h2>İstisna analizi</h2>
              <span>Açık: {exceptions.openCount} · Atanmamış: {exceptions.unassignedCount}</span>
            </div>
          </div>
          {exceptions.items.length === 0 ? <ReportEmpty /> : (
          <div className="production-execution-workspace__list">
            {exceptions.items.slice(0, 8).map((item) => (
              <Card key={item.exceptionId} className="production-report-card">
                <div className="production-order-card__header">
                  <div>
                    <p>{item.orderReferenceCode}</p>
                    <h3>{localizeDataText(enumLabel(item.exceptionType))}</h3>
                  </div>
                  <Badge tone={item.severity === "critical" || item.severity === "high" ? "warning" : "neutral"}>
                    {enumLabel(item.severity)}
                  </Badge>
                </div>
                <div className="production-order-card__meta">
                  <span>{enumLabel(item.status)}</span>
                  <span>{formatMinutes(Number(item.ageMinutes))} önce açıldı</span>
                </div>
              </Card>
            ))}
          </div>
          )}
        </section>
      </div>
    </div>
  );
}
