import Link from "next/link";
import { ArrowUpRight } from "lucide-react";

import { WorkspaceHeader } from "@/components/workspace/workspace-header";
import { resolveNavigationIcon } from "@/lib/navigation/navigation-icon-registry";
import { resolvePermissionAwareAppsCatalog } from "@/lib/navigation/navigation-visibility";

import "../apps-catalog-workspace.css";

export async function AppsCatalogPageContent() {
  const catalog = await resolvePermissionAwareAppsCatalog();

  return (
    <main className="fact-workspace" data-testid="apps-catalog-page">
      <WorkspaceHeader
        context="UYGULAMALAR"
        meta="Uzman modüller, entegrasyonlar ve yönetim"
        title="Uygulamalar ve daha fazla"
      />

      {catalog.map((group) => (
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
                    <p>{item.href}</p>
                  </div>
                  <ArrowUpRight aria-hidden="true" size={16} />
                </Link>
              );
            })}
          </div>
        </section>
      ))}
    </main>
  );
}
