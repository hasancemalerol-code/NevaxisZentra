import Link from "next/link";

import { BULK_DATA_ONBOARDING_ROUTE } from "@/features/bulk-data-onboarding/domain/bulk-data-onboarding-labels";
import { displayTenantName } from "@/lib/presentation/display-name";
import type { DashboardShowcaseViewModel } from "@/features/dashboard-showcase/domain/dashboard-showcase-types";
import { FactoryOperationsAnalyticsGrid } from "@/features/factory-operations/components/factory-operations-analytics-grid";

type AppsFactoryOperationsSectionProps = Readonly<{
  viewModel: DashboardShowcaseViewModel;
}>;

function hasNoOperationalData(viewModel: DashboardShowcaseViewModel): boolean {
  // When no KPI has a value the whole dashboard is one calm empty state instead of a wall of "Veri yok" tiles.
  return viewModel.kpiCards.every((card) => card.pending)
    && viewModel.oeeComponentsPending
    && viewModel.distributionPending;
}

function pendingKpiLabels(viewModel: DashboardShowcaseViewModel): string {
  const labels = viewModel.kpiCards.filter((card) => card.pending).map((card) => card.label);
  return labels.length > 0 ? `Veri bekleyen göstergeler: ${labels.join(", ")}.` : "";
}

function hasPendingParts(viewModel: DashboardShowcaseViewModel): boolean {
  return viewModel.kpiCards.some((card) => card.pending)
    || viewModel.hourlyPending
    || viewModel.oeeComponentsPending
    || viewModel.distributionPending;
}

export function AppsFactoryOperationsSection({ viewModel }: AppsFactoryOperationsSectionProps) {
  // Only KPI tiles hold data: the tile and the explanatory note sit side by side instead of leaving a half-empty panel.
  const kpiOnly = viewModel.hourlyPending && viewModel.oeeComponentsPending && viewModel.distributionPending;
  return (
    <section className="fact-section apps-factory-operations-section" data-testid="apps-factory-operations-section">
      <header className="fact-section__header">
        <div>
          <p className="fact-workspace__context">OPERASYONEL GENEL BAKIŞ</p>
          <h2 className="fact-section__title">Fabrika operasyon görünümü</h2>
          <p className="fact-workspace__meta">{displayTenantName(viewModel.facilityDisplayName)}</p>
        </div>
      </header>
      <div className="fact-section__body">
        {hasNoOperationalData(viewModel) ? (
          <div className="apps-factory-ops-empty" data-testid="apps-factory-operations-empty">
            <p className="apps-factory-ops-empty__title">Henüz üretim verisi yok</p>
            <p className="apps-factory-ops-empty__body">
              OEE, fire oranı, çevrim süresi ve saatlik üretim göstergeleri, üretim kayıtları geldiğinde burada görünür.
            </p>
            <div className="apps-operational-overview__actions">
              <Link href="/fabrika" className="commint-button commint-button--ghost">
                Fabrika yapısını aç
              </Link>
              <Link href={BULK_DATA_ONBOARDING_ROUTE} className="commint-button commint-button--primary">
                Veri Aktarım Merkezi&apos;ni aç
              </Link>
            </div>
          </div>
        ) : (
          <div className={kpiOnly ? "apps-factory-ops-row apps-factory-ops-row--side" : "apps-factory-ops-row"}>
            <FactoryOperationsAnalyticsGrid
              pendingLabel="Veri yok"
              viewModel={{ ...viewModel, kpiCards: viewModel.kpiCards.filter((card) => !card.pending) }}
            />
            {hasPendingParts(viewModel) ? (
              <p className="apps-factory-ops-note" data-testid="apps-factory-operations-pending-note">
                {pendingKpiLabels(viewModel)} Üretim kayıtları geldikçe bu göstergeler, saatlik üretim ve dağılım grafikleri burada dolacak.
              </p>
            ) : null}
          </div>
        )}
      </div>
    </section>
  );
}
