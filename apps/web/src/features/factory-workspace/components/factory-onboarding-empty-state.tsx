import Link from "next/link";

import { BULK_DATA_ONBOARDING_ROUTE } from "@/features/bulk-data-onboarding/domain/bulk-data-onboarding-labels";

type FactoryOnboardingEmptyStateProps = Readonly<{
  resourceCount: number;
  lineCount: number;
}>;

export function FactoryOnboardingEmptyState({
  resourceCount,
  lineCount,
}: FactoryOnboardingEmptyStateProps) {
  if (resourceCount > 0 || lineCount > 0) {
    return null;
  }

  return (
    <section className="commint-panel factory-onboarding-empty" data-testid="factory-onboarding-empty">
      <h2>Fabrika verisi henüz tanımlanmadı</h2>
      <p>
        Yeni müşteri kurulumunda makine ve üretim kaynaklarını toplu yükleyebilir veya tek tek tanımlayabilirsiniz.
      </p>
      <div className="factory-onboarding-empty__actions">
        <Link href={BULK_DATA_ONBOARDING_ROUTE}>Veri Aktarım Merkezi&apos;nden toplu yükle</Link>
        <Link href="/uretim/kaynaklar">Makineler &amp; Kaynaklar</Link>
      </div>
    </section>
  );
}
