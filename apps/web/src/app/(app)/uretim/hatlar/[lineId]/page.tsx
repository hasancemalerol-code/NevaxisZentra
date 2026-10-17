import { RecordFailureState } from "@/components/ui/state-page";
import { isNotFoundFailureCode } from "@/lib/api/api-failure-classifier";
import { MixedProductionLineDetailView } from "@/features/mixed-production/components/mixed-production-line-detail-view";
import { loadMixedProductionLineWorkspace } from "@/features/mixed-production/server/mixed-production-reader";
import { loadWorkerContextForLine } from "@/features/context-knowledge/server/context-knowledge-reader";
import { loadThroughputOptimizerWorkspaceForLine } from "@/features/throughput-optimizer/server/throughput-optimizer-reader";
import { loadThroughputIntelligenceWorkspace } from "@/features/throughput-intelligence/server/throughput-intelligence-reader";

type MixedProductionLineDetailPageProps = Readonly<{
  params: Promise<{ lineId: string }>;
}>;

export default async function MixedProductionLineDetailPage({ params }: MixedProductionLineDetailPageProps) {
  const { lineId } = await params;
  const result = await loadMixedProductionLineWorkspace(lineId);
  const throughputResult = await loadThroughputIntelligenceWorkspace(lineId);
  const contextResult = await loadWorkerContextForLine(lineId);
  const optimizerResult = await loadThroughputOptimizerWorkspaceForLine(lineId);

  if (result.status === "failure") {
    return (
      <RecordFailureState
        notFound={isNotFoundFailureCode("code" in result ? result.code : undefined)}
        entityLabel="üretim hattı"
        failedTitle="Hat detayı yüklenemedi"
        message={result.message}
        backHref="/uretim/hatlar"
        backLabel="Üretim hatlarına dön"
      />
    );
  }

  return (
    <MixedProductionLineDetailView
      detail={result.detail}
      playback={result.playback}
      throughput={
        throughputResult.status === "success"
          ? { overview: throughputResult.overview, comparison: throughputResult.comparison }
          : null
      }
      contextPackage={contextResult.status === "success" ? contextResult.package : null}
      optimizerWorkspace={optimizerResult.status === "success" ? optimizerResult.workspace : null}
    />
  );
}
