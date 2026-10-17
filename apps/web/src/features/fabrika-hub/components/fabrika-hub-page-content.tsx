import Link from "next/link";
import { ArrowUpRight } from "lucide-react";

import { FactoryContextBreadcrumb } from "@/components/workspace/factory-context-breadcrumb";
import { StatusStrip } from "@/components/workspace/status-strip";
import { WorkspaceHeader } from "@/components/workspace/workspace-header";
import { readFabrikaHubWorkspace } from "@/features/fabrika-hub/server/fabrika-hub-queries";

import "../fabrika-hub-workspace.css";

export async function FabrikaHubPageContent() {
  const workspace = await readFabrikaHubWorkspace();

  return (
    <div className="fact-workspace" data-testid="fabrika-hub-page">
      <WorkspaceHeader
        breadcrumb={<FactoryContextBreadcrumb segments={[{ label: "Canlı durum" }]} />}
        context="FABRİKA"
        meta="Fabrika → Alan → Hat → İstasyon → Kaynak → Makine"
        title="Canlı fabrika operasyonları"
      />

      <StatusStrip
        aria-label="Fabrika giriş özet"
        items={workspace.entries.map((entry) => ({
          label: entry.label,
          value: entry.metric,
        }))}
      />

      <section aria-label="Fabrika modülleri" className="fact-section">
        <header className="fact-section__header">
          <h2 className="fact-section__title">Operasyonel giriş noktaları</h2>
        </header>
        <div className="fact-section__body fabrika-hub-grid">
          {workspace.entries.map((entry) => (
            <Link key={entry.href} className="fabrika-hub-card" href={entry.href}>
              <div>
                <h3>{entry.label}</h3>
                <p>{entry.description}</p>
                <span>{entry.metric}</span>
              </div>
              <ArrowUpRight aria-hidden="true" size={16} />
            </Link>
          ))}
        </div>
      </section>
    </div>
  );
}
