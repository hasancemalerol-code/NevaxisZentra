import Link from "next/link";
import { ArrowLeft } from "lucide-react";

import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { MaintenancePlanCreateForm } from "@/features/maintenance/components/maintenance-plan-create-form";
import { MaintenanceWorkspaceNav } from "@/features/maintenance/components/maintenance-workspace-nav";
import "@/features/maintenance/components/maintenance-workspace.css";
import { createPreventiveMaintenancePlanAction } from "@/features/maintenance/server/maintenance-writer";

export default async function NewPreventivePlanPage() {
  await forbidUnlessGranted("maintenance.manage");

  return (
    <main className="production-execution-page maintenance-page">
      <header className="maintenance-page-header production-execution-header">
        <Link className="maintenance-back-link" href="/bakim">
          <ArrowLeft aria-hidden="true" />
          Bakım ana sayfası
        </Link>
        <div className="maintenance-page-header__intro">
          <p>BAKIM &amp; ARIZA</p>
          <h1>Yeni Periyodik Bakım Planı</h1>
          <span>Varlık için yeni bir periyodik bakım planı oluşturun.</span>
        </div>
      </header>
      <MaintenanceWorkspaceNav active="planning" />
      <div className="maintenance-page-content">
        <section aria-label="Yeni periyodik plan" className="maintenance-section maintenance-section--wide">
          <MaintenancePlanCreateForm onCreate={createPreventiveMaintenancePlanAction} />
        </section>
      </div>
    </main>
  );
}
