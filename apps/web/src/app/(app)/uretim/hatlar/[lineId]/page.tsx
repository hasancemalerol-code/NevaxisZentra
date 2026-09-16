import { MixedProductionLineDetailView } from "@/features/mixed-production/components/mixed-production-line-detail-view";
import { loadMixedProductionLineWorkspace } from "@/features/mixed-production/server/mixed-production-reader";
import { loadWorkerContextForLine } from "@/features/context-knowledge/server/context-knowledge-reader";
import { loadThroughputIntelligenceWorkspace } from "@/features/throughput-intelligence/server/throughput-intelligence-reader";

type MixedProductionLineDetailPageProps = Readonly<{
  params: Promise<{ lineId: string }>;
}>;

export default async function MixedProductionLineDetailPage({ params }: MixedProductionLineDetailPageProps) {
  const { lineId } = await params;
  const result = await loadMixedProductionLineWorkspace(lineId);
  const throughputResult = await loadThroughputIntelligenceWorkspace(lineId);
  const contextResult = await loadWorkerContextForLine(lineId);

  if (result.status === "failure") {
    return (
      <main className="production-execution-page">
        <div role="alert" className="state-panel error-state">
          <h3>Hat detayı yüklenemedi</h3>
          <p>{result.message}</p>
        </div>
      </main>
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
    />
  );
}
