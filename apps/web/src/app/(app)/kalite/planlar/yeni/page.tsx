import { QualityPageShell } from "@/features/quality-control/components/quality-page-shell";
import { QualityPlanForm } from "@/features/quality-control/components/quality-plan-form";
import "@/features/quality-control/components/quality-workspace.css";
import { createQualityControlPlanAction } from "@/features/quality-control/server/quality-control-writer";

export default function NewQualityPlanPage() {
  return (
    <QualityPageShell
      backHref="/kalite"
      backLabel="Kalite ana sayfası"
      eyebrow="KALİTE KONTROL"
      title="Yeni Kontrol Planı"
      subtitle="Ürün ve operasyon için kalite kontrol planı tanımlayın."
    >
      <QualityPlanForm onCreate={createQualityControlPlanAction} />
    </QualityPageShell>
  );
}
