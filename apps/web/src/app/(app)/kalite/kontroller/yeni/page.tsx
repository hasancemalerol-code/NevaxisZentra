

import { NewQualityInspectionForm } from "@/features/quality-control/components/new-quality-inspection-form";
import { QualityPageShell } from "@/features/quality-control/components/quality-page-shell";
import "@/features/quality-control/components/quality-workspace.css";
import { readProductionOrders } from "@/features/production-execution/server/production-order-reader";
import { readQualityWorkspace } from "@/features/quality-control/server/quality-control-reader";
import { createQualityInspectionAction } from "@/features/quality-control/server/quality-control-writer";
import { FailureStatePage } from "@/components/ui/failure-state";

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
        <FailureStatePage code={"code" in ordersResult ? ordersResult.code : undefined} message={ordersResult.message} failedTitle="Üretim emirleri yüklenemedi" inline />
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
        <FailureStatePage code={"code" in qualityResult ? qualityResult.code : undefined} message={qualityResult.message} failedTitle="Kalite planları yüklenemedi" inline />
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
