import Link from "next/link";
import { ArrowUpRight } from "lucide-react";

import { StatusStrip } from "@/components/workspace/status-strip";
import { WorkspaceHeader } from "@/components/workspace/workspace-header";
import { readIslerHubWorkspace } from "@/features/isler-hub/server/isler-hub-queries";

import "../isler-hub-workspace.css";

export async function IslerHubPageContent() {
  const workspace = await readIslerHubWorkspace();

  return (
    <div className="fact-workspace" data-testid="isler-hub-page">
      <WorkspaceHeader
        context="İŞLER"
        meta="İş · Sorun · Aksiyon · Karar · Sonuç · Doğrulama"
        title="Operasyonel iş akışı"
      />

      <StatusStrip
        aria-label="İş özet"
        items={workspace.entries.slice(0, 4).map((entry) => ({
          label: entry.label,
          value: entry.metric,
        }))}
      />

      <section aria-label="İş giriş noktaları" className="fact-section">
        <header className="fact-section__header">
          <h2 className="fact-section__title">Ne üzerinde çalışıyorum?</h2>
        </header>
        <div className="fact-section__body isler-hub-grid">
          {workspace.entries.map((entry) => (
            <Link key={entry.href} className="isler-hub-card" href={entry.href}>
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
