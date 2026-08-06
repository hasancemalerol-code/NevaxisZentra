import {
  Boxes,
  Factory,
  PackageSearch,
  Settings2,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";

import type { ProductionPlanDetailViewModel } from "../application/production-plan-detail-view-model";
import { getProductionPlanResourceDisplayValue } from "../application/production-plan-detail-view-model";
import { getProductionPlanPriorityLabel } from "../presentation/production-plan-labels";

type ProductionPlanResourceCardsProps =
  Readonly<{
    viewModel: ProductionPlanDetailViewModel;
  }>;

type ProductionPlanResourceType =
  | "machine"
  | "mold"
  | "material";

type ResourceCardProps =
  Readonly<{
    icon: React.ReactNode;
    label: string;
    resourceType: ProductionPlanResourceType;
    value: string | null;
  }>;

function ResourceCard({
  icon,
  label,
  resourceType,
  value,
}: ResourceCardProps) {
  const isAssigned = Boolean(value);

  return (
    <Card className="production-plan-resource-card">
      <div className="production-plan-resource-card__icon">
        {icon}
      </div>

      <div className="production-plan-resource-card__content">
        <span>{label}</span>

        <strong>
          {getProductionPlanResourceDisplayValue(
            resourceType,
            value,
          )}
        </strong>

        <Badge
          tone={
            isAssigned
              ? "success"
              : "warning"
          }
        >
          {isAssigned
            ? "Atama tamamlandı"
            : "Atama gerekiyor"}
        </Badge>
      </div>
    </Card>
  );
}

export function ProductionPlanResourceCards({
  viewModel,
}: ProductionPlanResourceCardsProps) {
  return (
    <section className="production-plan-resources">
      <div className="production-plan-resources__heading">
        <div>
          <p>Kaynak planı</p>

          <h2>
            Üretim kaynakları ve talep kapsamı
          </h2>

          <span>
            Planın makine, kalıp, malzeme ve talep
            atamaları yetkili onayından önce eksiksiz
            olmalıdır.
          </span>
        </div>

        <Badge
          tone={
            viewModel.hasCompleteResourceAssignment
              ? "success"
              : "warning"
          }
        >
          {viewModel.hasCompleteResourceAssignment
            ? "Kaynak atamaları tamam"
            : `${viewModel.missingResourceCount} eksik kaynak`}
        </Badge>
      </div>

      <div className="production-plan-resources__grid">
        <ResourceCard
          icon={<Factory aria-hidden="true" />}
          label="Makine"
          resourceType="machine"
          value={viewModel.machineId}
        />

        <ResourceCard
          icon={<Settings2 aria-hidden="true" />}
          label="Kalıp"
          resourceType="mold"
          value={viewModel.moldId}
        />

        <ResourceCard
          icon={<Boxes aria-hidden="true" />}
          label="Malzeme"
          resourceType="material"
          value={viewModel.materialId}
        />

        <Card className="production-plan-resource-card">
          <div className="production-plan-resource-card__icon">
            <PackageSearch aria-hidden="true" />
          </div>

          <div className="production-plan-resource-card__content">
            <span>Üretim talepleri</span>

            <strong>
              {viewModel.demandCount} talep
            </strong>

            <Badge tone="information">
              {getProductionPlanPriorityLabel(
                viewModel.primaryPriority,
              )}
            </Badge>
          </div>
        </Card>
      </div>
    </section>
  );
}