import { FactosBrandLockup } from "@/components/brand/factos-brand-lockup";
import { FactoryShowcaseView } from "@/features/dashboard-showcase/components/factory-showcase-view";
import "@/features/dashboard-showcase/components/factory-showcase.css";
import { readDashboardShowcaseWorkspace } from "@/features/dashboard-showcase/server/dashboard-showcase-queries";
import { customerMessage } from "@/lib/presentation/customer-message";

export const dynamic = "force-dynamic";

export default async function FabrikaVitriniPage() {
  const result = await readDashboardShowcaseWorkspace();

  if (result.status === "failure") {
    return (
      <div className="factory-showcase factory-showcase--error">
        <header className="factory-showcase__header">
          <div className="factory-showcase__brand">
            <FactosBrandLockup className="factory-showcase__brand-lockup" />
            <span aria-hidden="true" className="factory-showcase__brand-divider" />
            <h1 className="factory-showcase__title">Fabrika Operasyon Görünümü</h1>
          </div>
        </header>
        <p className="factory-showcase-chart__pending">{customerMessage(result.message)}</p>
      </div>
    );
  }

  return <FactoryShowcaseView initialViewModel={result.viewModel} />;
}
