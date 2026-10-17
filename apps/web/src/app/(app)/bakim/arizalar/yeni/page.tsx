import Link from "next/link";
import { ArrowLeft } from "lucide-react";

import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { MaintenanceRequestCreateForm } from "@/features/maintenance/components/maintenance-request-create-form";
import "@/features/maintenance/components/maintenance-workspace.css";
import { readMaintenanceAssetOptions } from "@/features/maintenance/server/maintenance-asset-options-reader";
import { createMaintenanceRequestAction } from "@/features/maintenance/server/maintenance-writer";

export default async function NewMaintenanceRequestPage() {
  await forbidUnlessGranted("maintenance.manage");
  const assetOptions = await readMaintenanceAssetOptions();

  return (
    <div className="production-execution-page maintenance-page">
      <header className="maintenance-page-header production-execution-header">
        <Link className="maintenance-back-link" href="/bakim">
          <ArrowLeft aria-hidden="true" />
          Bakım ana sayfası
        </Link>
        <div className="maintenance-page-header__intro">
          <p>BAKIM VE ARIZA</p>
          <h1>Yeni Arıza Kaydı</h1>
          <span>Yeni bir arıza veya bakım talebi oluşturun.</span>
        </div>
      </header>
      <div className="maintenance-page-content">
        <section aria-label="Yeni arıza kaydı" className="maintenance-section maintenance-section--wide">
          <MaintenanceRequestCreateForm assetOptions={assetOptions} onCreate={createMaintenanceRequestAction} />
        </section>
      </div>
    </div>
  );
}
