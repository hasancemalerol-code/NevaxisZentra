import { BarChart3 } from "lucide-react";
import type { ProductionQuantitySummary } from "../domain/production-order";

function formatQty(value: number | undefined): string {
  if (value === undefined) return "—";
  return value.toLocaleString("tr-TR", { maximumFractionDigits: 0 });
}

function formatPercentRaw(value: number | undefined): string {
  if (value === undefined) return "—";
  return value.toLocaleString("tr-TR", { maximumFractionDigits: 1 });
}

type ProductionQuantityPanelProps = Readonly<{
  summary: ProductionQuantitySummary;
}>;

export function ProductionQuantityPanel({ summary }: ProductionQuantityPanelProps) {
  const progressPercentage =
    summary.completionPercentage !== undefined
      ? Math.min(summary.completionPercentage, 100)
      : undefined;

  const actualPercentage = summary.completionPercentage;

  return (
    <div className="production-quantity-panel">
      <div className="production-quantity-panel__header">
        <BarChart3 aria-hidden="true" />

        <div>
          <p className="production-quantity-panel__header-eyebrow">
            MİKTAR VE İLERLEME
          </p>
          <h2>Üretim Miktarı</h2>
        </div>
      </div>

      <div className="production-quantity-panel__grid">
        {summary.plannedQuantity !== undefined && (
          <div className="production-quantity-panel__item">
            <span>Planlanan</span>
            <strong>
              {formatQty(summary.plannedQuantity)}
              <small style={{ fontSize: "0.75rem", fontWeight: 600, color: "#5a697d", marginLeft: 4 }}>
                {summary.plannedQuantityUnit ?? "adet"}
              </small>
            </strong>
          </div>
        )}

        <div className="production-quantity-panel__item production-quantity-panel__item--primary">
          <span>İyi Üretim</span>
          <strong>
            {formatQty(summary.goodQuantity)}
            <small style={{ fontSize: "0.75rem", fontWeight: 600, color: "#5a697d", marginLeft: 4 }}>
              adet
            </small>
          </strong>
        </div>

        <div className="production-quantity-panel__item production-quantity-panel__item--scrap">
          <span>Fire</span>
          <strong>
            {formatQty(summary.scrapQuantity)}
            <small style={{ fontSize: "0.75rem", fontWeight: 600, color: "#a8560a", marginLeft: 4 }}>
              adet
            </small>
          </strong>
        </div>

        <div className="production-quantity-panel__item">
          <span>Toplam Üretim</span>
          <strong>
            {formatQty(summary.grossProducedQuantity)}
            <small style={{ fontSize: "0.75rem", fontWeight: 600, color: "#5a697d", marginLeft: 4 }}>
              adet
            </small>
          </strong>
        </div>

        {summary.remainingQuantity !== undefined && (
          <div className="production-quantity-panel__item production-quantity-panel__item--remaining">
            <span>Kalan</span>
            <strong>
              {formatQty(summary.remainingQuantity)}
              <small style={{ fontSize: "0.75rem", fontWeight: 600, color: "#12673f", marginLeft: 4 }}>
                adet
              </small>
            </strong>
          </div>
        )}

        <div className="production-quantity-panel__item">
          <span>İlerleme</span>
          <strong>
            {actualPercentage !== undefined
              ? `%${formatPercentRaw(actualPercentage)}`
              : "—"}
          </strong>
        </div>
      </div>

      {progressPercentage !== undefined && (
        <div
          className="production-quantity-panel__progress-bar-container"
          role="progressbar"
          aria-valuenow={Math.round(progressPercentage)}
          aria-valuemin={0}
          aria-valuemax={100}
          aria-label={`İlerleme: %${formatPercentRaw(actualPercentage)}`}
        >
          <div
            className="production-quantity-panel__progress-bar"
            style={{ width: `${progressPercentage}%` }}
          />
        </div>
      )}

      {summary.lastEntryAtUtc && (
        <p className="production-quantity-panel__last-entry">
          Son kayıt:{" "}
          {new Date(summary.lastEntryAtUtc).toLocaleString("tr-TR", {
            day: "2-digit",
            month: "short",
            hour: "2-digit",
            minute: "2-digit",
          })}
        </p>
      )}
    </div>
  );
}
