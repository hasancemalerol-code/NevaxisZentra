import {
  ArrowRightLeft,
  Bot,
  CheckCircle2,
  Clock3,
  FileClock,
  History,
  PencilLine,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";

import type {
  ProductionPlanHistoryItemType,
  ProductionPlanHistoryViewItem,
  ProductionPlanHistoryViewModel,
} from "../application/production-plan-history-view-model";
import {
  getProductionPlanDisplayValue,
  getProductionPlanFallbackLabel,
} from "../presentation/production-plan-fallback-labels";
import { getProductionPlanOriginLabel } from "../presentation/production-plan-labels";

type ProductionPlanHistoryPanelProps =
  Readonly<{
    viewModel: ProductionPlanHistoryViewModel;
  }>;

function formatDateTime(
  value: string | null,
): string {
  if (!value) {
    return getProductionPlanFallbackLabel(
      "auditDate",
    );
  }

  const date = new Date(value);

  if (Number.isNaN(date.getTime())) {
    return value;
  }

  return new Intl.DateTimeFormat("tr-TR", {
    dateStyle: "medium",
    timeStyle: "short",
  }).format(date);
}

function getHistoryItemLabel(
  type: ProductionPlanHistoryItemType,
): string {
  const labels: Record<
    ProductionPlanHistoryItemType,
    string
  > = {
    created: "Oluşturma",
    updated: "Güncelleme",
    approval: "Onay",
    transfer: "Aktarım",
  };

  return labels[type];
}

function getHistoryItemIcon(
  type: ProductionPlanHistoryItemType,
) {
  if (type === "created") {
    return <FileClock aria-hidden="true" />;
  }

  if (type === "updated") {
    return <PencilLine aria-hidden="true" />;
  }

  if (type === "approval") {
    return <CheckCircle2 aria-hidden="true" />;
  }

  return <ArrowRightLeft aria-hidden="true" />;
}

function ProductionPlanHistoryItem({
  item,
}: Readonly<{
  item: ProductionPlanHistoryViewItem;
}>) {
  return (
    <article className="production-plan-history-item">
      <div className="production-plan-history-item__icon">
        {getHistoryItemIcon(item.type)}
      </div>

      <div className="production-plan-history-item__content">
        <div className="production-plan-history-item__heading">
          <div>
            <p>{getHistoryItemLabel(item.type)}</p>
            <h3>{item.title}</h3>
          </div>

          <span>
            <Clock3 aria-hidden="true" />
            {formatDateTime(item.occurredAt)}
          </span>
        </div>

        <p>{item.description}</p>
      </div>
    </article>
  );
}

export function ProductionPlanHistoryPanel({
  viewModel,
}: ProductionPlanHistoryPanelProps) {
  const sourceReference =
    getProductionPlanDisplayValue(
      viewModel.sourceReference,
      "sourceReference",
    );

  return (
    <section className="production-plan-history-panel">
      <div className="production-plan-history-panel__heading">
        <div>
          <p>Kaynak ve audit geçmişi</p>

          <h2>
            Planın oluşum ve işlem geçmişi
          </h2>

          <span>
            Kaynak tipi, referans bilgisi, oluşturma,
            güncelleme, onay ve aktarım olayları izlenebilir
            biçimde gösterilir.
          </span>
        </div>

        <Badge
          tone={
            viewModel.isAiGenerated
              ? "ai"
              : "neutral"
          }
        >
          {viewModel.isAiGenerated ? (
            <Bot aria-hidden="true" />
          ) : (
            <History aria-hidden="true" />
          )}

          {viewModel.isAiGenerated
            ? "AI tarafından oluşturuldu"
            : "İşlem geçmişi"}
        </Badge>
      </div>

      <div className="production-plan-history-panel__meta">
        <Card>
          <span>Kaynak tipi</span>

          <strong>
            {getProductionPlanOriginLabel(
              viewModel.sourceType,
            )}
          </strong>
        </Card>

        <Card>
          <span>Kaynak referansı</span>

          <strong>{sourceReference}</strong>
        </Card>

        <Card>
          <span>Oluşturulma</span>

          <strong>
            {formatDateTime(viewModel.createdAt)}
          </strong>
        </Card>

        <Card>
          <span>Son güncelleme</span>

          <strong>
            {formatDateTime(viewModel.updatedAt)}
          </strong>
        </Card>
      </div>

      {!viewModel.hasHistory ? (
        <Card className="production-plan-history-panel__empty">
          <History aria-hidden="true" />

          <div>
            <h3>
              Audit geçmişi bulunmuyor
            </h3>

            <p>
              Bu üretim planı için gösterilebilir bir
              oluşturma, güncelleme, onay veya aktarım
              kaydı bulunmuyor.
            </p>
          </div>
        </Card>
      ) : (
        <div className="production-plan-history-panel__list">
          {viewModel.items.map((item) => (
            <ProductionPlanHistoryItem
              item={item}
              key={item.id}
            />
          ))}
        </div>
      )}
    </section>
  );
}