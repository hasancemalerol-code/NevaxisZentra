import { FactoryShowcaseView } from "@/features/dashboard-showcase/components/factory-showcase-view";
import "@/features/dashboard-showcase/components/factory-showcase.css";
import { readDashboardShowcaseWorkspace } from "@/features/dashboard-showcase/server/dashboard-showcase-queries";

export const dynamic = "force-dynamic";

export default async function FabrikaVitriniPage() {
  const result = await readDashboardShowcaseWorkspace();

  if (result.status === "failure") {
    return (
      <div className="factory-showcase factory-showcase--error">
        <header className="factory-showcase__header">
          <div className="factory-showcase__brand">
            <span aria-hidden="true" className="factory-showcase__brand-mark">CF</span>
            <strong>CREVAFACTOS</strong>
            <span aria-hidden="true" className="factory-showcase__brand-divider" />
            <span>Fabrika Operasyon Görünümü</span>
          </div>
        </header>
        <p className="factory-showcase-chart__pending">{result.message}</p>
      </div>
    );
  }

  return <FactoryShowcaseView initialViewModel={result.viewModel} />;
}
