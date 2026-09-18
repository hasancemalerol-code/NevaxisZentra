import Link from "next/link";
import { redirect } from "next/navigation";

import { WorkspaceHeader } from "@/components/workspace/workspace-header";

type DashboardPageProps = Readonly<{
  searchParams?: Promise<Readonly<Record<string, string | string[] | undefined>>>;
}>;

export default async function DashboardPage({ searchParams }: DashboardPageProps) {
  const params = await searchParams;
  const legacyView = params?.legacy === "1";

  if (!legacyView) {
    redirect("/");
  }

  return (
    <main className="fact-workspace" data-testid="dashboard-legacy-banner">
      <WorkspaceHeader
        context="LEGACY"
        meta="Bu görünüm artık operasyonel ana sayfa değildir"
        title="Operasyon Merkezi (Eski)"
      />

      <section className="commint-error-banner" role="status">
        <p>
          <strong>Önizleme verileri kaldırıldı.</strong> Güvenilir operasyonel ana sayfa artık{" "}
          <Link href="/">Bugün</Link> görünümüdür. Eski mock KPI paneli operasyonel gerçeklik
          olarak kullanılmamalıdır.
        </p>
      </section>
    </main>
  );
}
