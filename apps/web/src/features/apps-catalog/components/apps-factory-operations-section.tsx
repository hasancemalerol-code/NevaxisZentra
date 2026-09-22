import type { DashboardShowcaseViewModel } from "@/features/dashboard-showcase/domain/dashboard-showcase-types";
import { FactoryOperationsAnalyticsGrid } from "@/features/factory-operations/components/factory-operations-analytics-grid";

type AppsFactoryOperationsSectionProps = Readonly<{
  viewModel: DashboardShowcaseViewModel;
}>;

export function AppsFactoryOperationsSection({ viewModel }: AppsFactoryOperationsSectionProps) {
  return (
    <section className="fact-section apps-factory-operations-section" data-testid="apps-factory-operations-section">
      <header className="fact-section__header">
        <div>
          <p className="fact-workspace__context">OPERASYONEL GENEL BAKIŞ</p>
          <h2 className="fact-section__title">Fabrika operasyon görünümü</h2>
          <p className="fact-workspace__meta">{viewModel.facilityDisplayName}</p>
        </div>
      </header>
      <div className="fact-section__body">
        <FactoryOperationsAnalyticsGrid pendingLabel="Veri yok" viewModel={viewModel} />
      </div>
    </section>
  );
}
