import Link from "next/link";

import { QualityPlanForm } from "@/features/quality-control/components/quality-plan-form";
import { createQualityControlPlanAction } from "@/features/quality-control/server/quality-control-writer";

export default function NewQualityPlanPage() {
  return (
    <main className="production-execution-page">
      <header className="production-execution-header">
        <p>KALİTE KONTROL</p>
        <h1>Yeni Kontrol Planı</h1>
        <Link href="/kalite" className="production-execution-header__link">
          Kalite ana sayfasına dön
        </Link>
      </header>
      <QualityPlanForm onCreate={createQualityControlPlanAction} />
    </main>
  );
}
