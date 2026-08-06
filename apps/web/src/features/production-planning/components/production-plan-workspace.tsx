import {
  CircleAlert,
  Factory,
  SearchX,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";

import type { ProductionPlan } from "../domain/production-plan";
import { hasBlockingProductionPlanRisks } from "../domain/production-plan";
import { ProductionPlanCard } from "./production-plan-card";

type ProductionPlanWorkspaceProps = Readonly<{
  productionPlans: readonly ProductionPlan[];
  totalCount?: number;
}>;

export function ProductionPlanWorkspace({
  productionPlans,
  totalCount = productionPlans.length,
}: ProductionPlanWorkspaceProps) {
  const blockingPlanCount =
    productionPlans.filter(
      hasBlockingProductionPlanRisks,
    ).length;

  if (productionPlans.length === 0) {
    return (
      <Card className="production-plan-workspace production-plan-workspace--empty">
        <SearchX aria-hidden="true" />

        <div>
          <h2>Üretim planı bulunamadı</h2>

          <p>
            Seçilen filtrelerle eşleşen bir üretim planı
            bulunmuyor. Filtreleri temizleyerek tüm
            planları tekrar görüntüleyebilirsiniz.
          </p>
        </div>
      </Card>
    );
  }

  return (
    <section className="production-plan-workspace">
      <div className="production-plan-workspace__heading">
        <div>
          <p>Planlama çalışma alanı</p>

          <h2>Üretim planları</h2>

          <span>
            Toplam {totalCount} plan içinden{" "}
            {productionPlans.length} plan gösteriliyor.
          </span>
        </div>

        <div className="production-plan-workspace__summary">
          <Badge tone="information">
            <Factory aria-hidden="true" />
            {productionPlans.length} plan
          </Badge>

          {blockingPlanCount > 0 ? (
            <Badge tone="danger">
              <CircleAlert aria-hidden="true" />
              {blockingPlanCount} engelli
            </Badge>
          ) : (
            <Badge tone="success">
              Engelleyici risk yok
            </Badge>
          )}
        </div>
      </div>

      <div className="production-plan-workspace__list">
        {productionPlans.map(
          (productionPlan) => (
            <ProductionPlanCard
              key={productionPlan.id}
              productionPlan={productionPlan}
            />
          ),
        )}
      </div>
    </section>
  );
}