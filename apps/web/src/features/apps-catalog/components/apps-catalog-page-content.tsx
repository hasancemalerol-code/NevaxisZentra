import Link from "next/link";
import { ArrowUpRight } from "lucide-react";

import { WorkspaceHeader } from "@/components/workspace/workspace-header";
import { resolveNavigationIcon } from "@/lib/navigation/navigation-icon-registry";
import { resolvePermissionAwareAppsCatalog } from "@/lib/navigation/navigation-visibility";

import { readAppsFactoryOperationsDashboard } from "../server/apps-factory-operations-reader";
import { AppsFactoryOperationsSection } from "./apps-factory-operations-section";
import { AppsKaynaklarCatalogSection } from "./apps-kaynaklar-catalog-section";
import { mapDashboardShowcaseViewModel } from "@/features/dashboard-showcase/server/dashboard-showcase-mapper";

import { balanceCatalogColumns } from "../domain/catalog-columns";

import "../apps-catalog-workspace.css";

function buildEmptyOperationsViewModel() {
  return mapDashboardShowcaseViewModel(
    {
      facilityDisplayName: "—",
      developmentPilotBanner: false,
      overview: null,
      orders: [],
      shifts: [],
      scrapRatePercent: null,
      cycleDurationMs: null,
      latestOeePercent: null,
      previousOeePercent: null,
      latestAssessment: null,
      qualityOverview: null,
      assessments: [],
    },
    new Date().toISOString(),
  );
}

export async function AppsCatalogPageContent() {
  const [catalog, operationsViewModel] = await Promise.all([
    resolvePermissionAwareAppsCatalog(),
    readAppsFactoryOperationsDashboard(),
  ]);

  const viewModel = operationsViewModel ?? buildEmptyOperationsViewModel();
  const columns = balanceCatalogColumns(catalog.filter((group) => group.label !== "Kaynaklar"), 2, 3);

  return (
    <div className="fact-workspace apps-catalog-workspace" data-testid="apps-catalog-page">
      <WorkspaceHeader
        context="UYGULAMALAR"
        meta="Fabrika operasyon görünümü ve uzman modül kataloğu"
        title="Uygulamalar"
      />

      <AppsFactoryOperationsSection viewModel={viewModel} />

      <header className="apps-catalog-specialist-header">
        <p className="fact-workspace__context">UZMAN UYGULAMALAR</p>
        <h2 className="fact-section__title">Modül kataloğu</h2>
      </header>

      <div className="apps-catalog-sections">
        {columns.map((column, index) => (
          <div className="apps-catalog-sections__column" key={index}>
            {index === 0 ? <AppsKaynaklarCatalogSection /> : null}
            {column.map((group) => (
              <section aria-label={group.label} className="fact-section" key={group.label}>
                <header className="fact-section__header">
                  <h2 className="fact-section__title">{group.label}</h2>
                </header>
                <div className="fact-section__body apps-catalog-grid">
                  {group.items.map((item) => {
                    const Icon = resolveNavigationIcon(item.iconKey);

                    return (
                      <Link key={item.href} className="apps-catalog-card" href={item.href}>
                        <Icon aria-hidden="true" size={16} />
                        <div>
                          <h3>{item.label}</h3>
                        </div>
                        <ArrowUpRight aria-hidden="true" size={16} />
                      </Link>
                    );
                  })}
                </div>
              </section>
            ))}
          </div>
        ))}
      </div>
    </div>
  );
}
