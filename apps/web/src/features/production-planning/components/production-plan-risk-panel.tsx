import {
  AlertTriangle,
  CheckCircle2,
  CircleAlert,
  Info,
  ShieldAlert,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";

import type {
  ProductionPlanRiskViewItem,
  ProductionPlanRiskViewModel,
} from "../application/production-plan-risk-view-model";
import type {
  ProductionPlanRiskSeverity,
  ProductionPlanRiskType,
} from "../domain/production-plan";

type ProductionPlanRiskPanelProps =
  Readonly<{
    viewModel: ProductionPlanRiskViewModel;
  }>;

function getRiskSeverityLabel(
  severity: ProductionPlanRiskSeverity,
): string {
  const labels: Record<
    ProductionPlanRiskSeverity,
    string
  > = {
    information: "Bilgi",
    warning: "Uyarı",
    critical: "Kritik",
  };

  return labels[severity];
}

function getRiskSeverityTone(
  severity: ProductionPlanRiskSeverity,
): "neutral" | "warning" | "danger" {
  if (severity === "critical") {
    return "danger";
  }

  if (severity === "warning") {
    return "warning";
  }

  return "neutral";
}

function getRiskTypeLabel(
  type: ProductionPlanRiskType,
): string {
  const labels: Record<
    ProductionPlanRiskType,
    string
  > = {
    "capacity-shortage": "Kapasite yetersizliği",
    "machine-conflict": "Makine çakışması",
    "mold-conflict": "Kalıp çakışması",
    "material-shortage": "Malzeme yetersizliği",
    "operator-shortage": "Operatör yetersizliği",
    "delivery-risk": "Termin riski",
    "quality-risk": "Kalite riski",
    "maintenance-risk": "Bakım riski",
    "data-missing": "Eksik veri",
    "data-conflict": "Veri çelişkisi",
  };

  return labels[type];
}

function ProductionPlanRiskItem({
  item,
}: Readonly<{
  item: ProductionPlanRiskViewItem;
}>) {
  return (
    <article
      className={[
        "production-plan-risk-item",
        item.isBlocking
          ? "production-plan-risk-item--blocking"
          : "production-plan-risk-item--advisory",
      ].join(" ")}
    >
      <div className="production-plan-risk-item__icon">
        {item.isBlocking ? (
          <ShieldAlert aria-hidden="true" />
        ) : (
          <CircleAlert aria-hidden="true" />
        )}
      </div>

      <div className="production-plan-risk-item__content">
        <div className="production-plan-risk-item__heading">
          <div>
            <p>
              {getRiskTypeLabel(item.type)}
            </p>

            <h3>{item.title}</h3>
          </div>

          <div className="production-plan-risk-item__badges">
            <Badge
              tone={getRiskSeverityTone(
                item.severity,
              )}
            >
              {getRiskSeverityLabel(
                item.severity,
              )}
            </Badge>

            <Badge
              tone={
                item.isBlocking
                  ? "danger"
                  : "information"
              }
            >
              {item.isBlocking
                ? "Engelleyici"
                : "Bilgilendirici"}
            </Badge>
          </div>
        </div>

        <span>{item.description}</span>
      </div>
    </article>
  );
}

export function ProductionPlanRiskPanel({
  viewModel,
}: ProductionPlanRiskPanelProps) {
  return (
    <section className="production-plan-risk-panel">
      <div className="production-plan-risk-panel__heading">
        <div>
          <p>Risk ve güvenlik kontrolleri</p>

          <h2>
            Plan engelleri ve operasyonel uyarılar
          </h2>

          <span>
            Engelleyici riskler çözülmeden üretim
            planı onaya gönderilemez veya bağlı
            sisteme aktarılamaz.
          </span>
        </div>

        <div className="production-plan-risk-panel__summary">
          <Badge
            tone={
              viewModel.hasBlockingRisks
                ? "danger"
                : "success"
            }
          >
            {viewModel.hasBlockingRisks ? (
              <AlertTriangle aria-hidden="true" />
            ) : (
              <CheckCircle2 aria-hidden="true" />
            )}

            {viewModel.blockingCount} engelleyici
          </Badge>

          <Badge tone="information">
            <Info aria-hidden="true" />
            {viewModel.advisoryCount} bilgilendirici
          </Badge>
        </div>
      </div>

      {!viewModel.hasRisks ? (
        <Card className="production-plan-risk-panel__empty">
          <CheckCircle2 aria-hidden="true" />

          <div>
            <h3>
              Aktif plan riski bulunmuyor
            </h3>

            <p>
              Plan için tanımlanmış engelleyici veya
              bilgilendirici bir operasyon riski yok.
            </p>
          </div>
        </Card>
      ) : (
        <div className="production-plan-risk-panel__list">
          {viewModel.blockingItems.map(
            (item) => (
              <ProductionPlanRiskItem
                item={item}
                key={item.id}
              />
            ),
          )}

          {viewModel.advisoryItems.map(
            (item) => (
              <ProductionPlanRiskItem
                item={item}
                key={item.id}
              />
            ),
          )}
        </div>
      )}
    </section>
  );
}