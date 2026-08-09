import Link from "next/link";

import { createQualityInspectionAction } from "@/features/quality-control/server/quality-control-writer";
import { readProductionOrders } from "@/features/production-execution/server/production-order-reader";
import { readQualityWorkspace } from "@/features/quality-control/server/quality-control-reader";
import { NewQualityInspectionForm } from "@/features/quality-control/components/new-quality-inspection-form";

export default async function NewQualityInspectionPage() {
  const [ordersResult, qualityResult] = await Promise.all([
    readProductionOrders(),
    readQualityWorkspace(),
  ]);

  if (ordersResult.status === "failure") {
    return (
      <main className="production-execution-page">
        <p>Üretim emirleri yüklenemedi: {ordersResult.message}</p>
      </main>
    );
  }

  if (qualityResult.status === "failure") {
    return (
      <main className="production-execution-page">
        <p>Kalite planları yüklenemedi: {qualityResult.message}</p>
      </main>
    );
  }

  const activePlans = qualityResult.plans.filter((plan) => plan.status === "active");

  return (
    <main className="production-execution-page">
      <header className="production-execution-header">
        <p>KALİTE KONTROL</p>
        <h1>Yeni Kalite Kontrolü</h1>
        <Link href="/kalite" className="production-execution-header__link">
          Kalite ana sayfasına dön
        </Link>
      </header>
      <NewQualityInspectionForm
        orders={ordersResult.orders}
        plans={activePlans}
        onCreate={createQualityInspectionAction}
      />
    </main>
  );
}
