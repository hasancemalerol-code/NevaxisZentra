import Link from "next/link";
import { ArrowLeft } from "lucide-react";

import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { MaintenanceWorkOrderCreateForm } from "@/features/maintenance/components/maintenance-work-order-create-form";
import { MaintenanceWorkspaceNav } from "@/features/maintenance/components/maintenance-workspace-nav";
import "@/features/maintenance/components/maintenance-workspace.css";
import { createMaintenanceWorkOrderAction } from "@/features/maintenance/server/maintenance-writer";

export default async function NewMaintenanceWorkOrderPage() {
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
          <h1>Yeni Bakım Emri</h1>
          <span>Yeni bir bakım emri taslağı oluşturun.</span>
        </div>
      </header>
      <MaintenanceWorkspaceNav active="work-orders" />
      <div className="maintenance-page-content">
        <section aria-label="Yeni bakım emri" className="maintenance-section maintenance-section--wide">
          <MaintenanceWorkOrderCreateForm onCreate={createMaintenanceWorkOrderAction} />
        </section>
      </div>
    </main>
  );
}
