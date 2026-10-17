import { displayReferenceCode } from "@/lib/presentation/display-name";
import {
  CalendarClock,
  PackageSearch,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";

import {
  getProductionPlanDemandDeliveryLabel,
  type ProductionPlanDemandViewModel,
} from "../application/production-plan-demand-view-model";
import { getProductionPlanPriorityLabel } from "../presentation/production-plan-labels";

type ProductionPlanDemandTableProps =
  Readonly<{
    viewModel: ProductionPlanDemandViewModel;
  }>;

function formatQuantity(
  quantity: number,
  unit: string,
): string {
  return (
    new Intl.NumberFormat("tr-TR", {
      maximumFractionDigits: 2,
    }).format(quantity) + ` ${unit}`
  );
}

function formatDeliveryDate(
  value: string | null,
): string {
  const displayValue =
    getProductionPlanDemandDeliveryLabel(
      value,
    );

  if (!value) {
    return displayValue;
  }

  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return displayValue;
  }

  return new Intl.DateTimeFormat("tr-TR", {
    dateStyle: "medium",
  }).format(date);
}

export function ProductionPlanDemandTable({
  viewModel,
}: ProductionPlanDemandTableProps) {
  return (
    <section className="production-plan-demand-table">
      <div className="production-plan-demand-table__heading">
        <div>
          <p>Üretim talepleri</p>

          <h2>
            Sipariş ve ürün gereksinimleri
          </h2>

          <span>
            Plan kapsamındaki doğrulanmış talepler,
            miktarlar, öncelikler ve teslim tarihleri.
          </span>
        </div>

        <div className="production-plan-demand-table__summary">
          <Badge tone="information">
            {viewModel.totalCount} talep
          </Badge>

          <Badge tone="neutral">
            {new Intl.NumberFormat("tr-TR", {
              maximumFractionDigits: 2,
            }).format(viewModel.totalQuantity)}
            {" "}toplam miktar
          </Badge>
        </div>
      </div>

      {!viewModel.hasDemands ? (
        <Card className="production-plan-demand-table__empty">
          <PackageSearch aria-hidden="true" />

          <div>
            <h3>
              Üretim talebi bulunmuyor
            </h3>

            <p>
              Plan henüz doğrulanmış bir sipariş veya
              üretim talebiyle ilişkilendirilmemiş.
            </p>
          </div>
        </Card>
      ) : (
        <Card className="production-plan-demand-table__card">
          <div className="production-plan-demand-table__scroll" tabIndex={0} role="region" aria-label="Talep tablosu (kaydırılabilir)">
            <table aria-label="Üretim talepleri">
              <thead>
                <tr>
                  <th>Sipariş</th>
                  <th>Ürün</th>
                  <th>Miktar</th>
                  <th>Termin</th>
                  <th>Öncelik</th>
                </tr>
              </thead>

              <tbody>
                {viewModel.items.map((item) => (
                  <tr key={item.id}>
                    <td>
                      <strong>
                        {item.orderReference}
                      </strong>

                      <span>{displayReferenceCode(item.id)}</span>
                    </td>

                    <td>
                      <strong>
                        {item.productName}
                      </strong>

                      <span>{displayReferenceCode(item.productId)}</span>
                    </td>

                    <td>
                      <strong>
                        {formatQuantity(
                          item.quantity,
                          item.unit,
                        )}
                      </strong>
                    </td>

                    <td>
                      <div className="production-plan-demand-table__date">
                        <CalendarClock aria-hidden="true" />

                        <span>
                          {formatDeliveryDate(
                            item.requestedDeliveryDate,
                          )}
                        </span>
                      </div>
                    </td>

                    <td>
                      <Badge tone="information">
                        {getProductionPlanPriorityLabel(
                          item.priority,
                        )}
                      </Badge>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </Card>
      )}
    </section>
  );
}