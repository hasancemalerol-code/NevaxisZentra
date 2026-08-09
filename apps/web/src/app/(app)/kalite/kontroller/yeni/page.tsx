import { ShieldCheck } from "lucide-react";

import { NewQualityInspectionForm } from "@/features/quality-control/components/new-quality-inspection-form";
import { QualityPageShell } from "@/features/quality-control/components/quality-page-shell";
import "@/features/quality-control/components/quality-workspace.css";
import { readProductionOrders } from "@/features/production-execution/server/production-order-reader";
import { readQualityWorkspace } from "@/features/quality-control/server/quality-control-reader";
import { createQualityInspectionAction } from "@/features/quality-control/server/quality-control-writer";

export default async function NewQualityInspectionPage() {
  const [ordersResult, qualityResult] = await Promise.all([
    readProductionOrders(),
    readQualityWorkspace(),
  ]);

  if (ordersResult.status === "failure") {
    return (
      <QualityPageShell
        backHref="/kalite"
        backLabel="Kalite ana sayfası"
        eyebrow="KALİTE KONTROL"
        title="Yeni Kalite Kontrolü"
      >
        <div role="alert" className="quality-error-panel">
          <ShieldCheck aria-hidden="true" />
          <div>
            <h3>Üretim emirleri yüklenemedi</h3>
            <p>{ordersResult.message}</p>
          </div>
        </div>
      </QualityPageShell>
    );
  }

  if (qualityResult.status === "failure") {
    return (
      <QualityPageShell
        backHref="/kalite"
        backLabel="Kalite ana sayfası"
        eyebrow="KALİTE KONTROL"
        title="Yeni Kalite Kontrolü"
      >
        <div role="alert" className="quality-error-panel">
          <ShieldCheck aria-hidden="true" />
          <div>
            <h3>Kalite planları yüklenemedi</h3>
            <p>{qualityResult.message}</p>
          </div>
        </div>
      </QualityPageShell>
    );
  }

  const activePlans = qualityResult.plans.filter((plan) => plan.status === "active");

  return (
    <QualityPageShell
      backHref="/kalite"
      backLabel="Kalite ana sayfası"
      eyebrow="KALİTE KONTROL"
      title="Yeni Kalite Kontrolü"
      subtitle="Üretim emrine bağlı yeni kalite kontrol kaydı oluşturun."
    >
      <NewQualityInspectionForm
        orders={ordersResult.orders}
        plans={activePlans}
        onCreate={createQualityInspectionAction}
      />
    </QualityPageShell>
  );
}
