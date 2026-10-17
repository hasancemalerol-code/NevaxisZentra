import Link from "next/link";
import { ArrowLeft } from "lucide-react";

import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { MaintenanceWorkOrderCreateForm } from "@/features/maintenance/components/maintenance-work-order-create-form";
import "@/features/maintenance/components/maintenance-workspace.css";
import { readMaintenanceAssetOptions } from "@/features/maintenance/server/maintenance-asset-options-reader";
import { createMaintenanceWorkOrderAction } from "@/features/maintenance/server/maintenance-writer";

export default async function NewMaintenanceWorkOrderPage() {
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
          <h1>Yeni Bakım Emri</h1>
          <span>Yeni bir bakım emri taslağı oluşturun.</span>
        </div>
      </header>
      <div className="maintenance-page-content">
        <section aria-label="Yeni bakım emri" className="maintenance-section maintenance-section--wide">
          <MaintenanceWorkOrderCreateForm assetOptions={assetOptions} onCreate={createMaintenanceWorkOrderAction} />
        </section>
      </div>
    </div>
  );
}
