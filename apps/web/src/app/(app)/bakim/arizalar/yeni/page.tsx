import Link from "next/link";
import { ArrowLeft } from "lucide-react";

import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { MaintenanceRequestCreateForm } from "@/features/maintenance/components/maintenance-request-create-form";
import { MaintenanceWorkspaceNav } from "@/features/maintenance/components/maintenance-workspace-nav";
import "@/features/maintenance/components/maintenance-workspace.css";
import { createMaintenanceRequestAction } from "@/features/maintenance/server/maintenance-writer";

export default async function NewMaintenanceRequestPage() {
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
          <h1>Yeni Arıza Kaydı</h1>
          <span>Yeni bir arıza veya bakım talebi oluşturun.</span>
        </div>
      </header>
      <MaintenanceWorkspaceNav active="overview" />
      <div className="maintenance-page-content">
        <section aria-label="Yeni arıza kaydı" className="maintenance-section maintenance-section--wide">
          <MaintenanceRequestCreateForm onCreate={createMaintenanceRequestAction} />
        </section>
      </div>
    </main>
  );
}
