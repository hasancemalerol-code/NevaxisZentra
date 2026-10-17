import { MixedProductionLinesView } from "@/features/mixed-production/components/mixed-production-lines-view";
import { loadMixedProductionLinesWorkspace } from "@/features/mixed-production/server/mixed-production-reader";
import { FailureStatePage } from "@/components/ui/failure-state";
import { PageHeader } from "@/components/ui/page";

export default async function MixedProductionLinesPage() {
  const result = await loadMixedProductionLinesWorkspace();

  if (result.status === "failure") {
    return (
      <div className="fact-workspace" data-testid="mixed-production-lines-view">
        <PageHeader eyebrow="ÜRETİM" title="Üretim Hatları" />
        <FailureStatePage
          code={result.code}
          message={result.message}
          failedTitle="Üretim hatları yüklenemedi"
          backHref="/uretim"
          backLabel="Üretim emirlerine dön"
          inline
        />
      </div>
    );
  }

  return <MixedProductionLinesView lines={result.lines} />;
}
