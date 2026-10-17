import { FactoryDecisionCaseDetailView } from "@/features/factory-decision-center/components/factory-decision-case-detail-view";
import { loadWorkerContextForCase } from "@/features/context-knowledge/server/context-knowledge-reader";
import { loadFactoryDecisionCaseDetail } from "@/features/factory-decision-center/server/factory-decision-center-reader";
import { RecordFailureState } from "@/components/ui/state-page";
import { deferredSurfacesEnabled } from "@/lib/runtime/deferred-surfaces";
import { isNotFoundFailureCode } from "@/lib/api/api-failure-classifier";

type PageProps = Readonly<{
  params: Promise<{ caseId: string }>;
}>;

export default async function FactoryDecisionCaseDetailPage({ params }: PageProps) {
  const { caseId } = await params;
  const result = await loadFactoryDecisionCaseDetail(caseId);
  const contextResult = await loadWorkerContextForCase(caseId);

  if (result.status === "failure") {
    return (
      <RecordFailureState
        notFound={isNotFoundFailureCode("code" in result ? result.code : undefined)}
        entityLabel="karar vakası"
        failedTitle="Karar vakası yüklenemedi"
        message={result.message}
        backHref="/fabrika-karar-merkezi"
        backLabel="Karar Merkezi’ne dön"
      />
    );
  }

  return (
    <FactoryDecisionCaseDetailView
      detail={result.detail}
      showWorkforce={deferredSurfacesEnabled()}
      contextPackage={contextResult.status === "success" ? contextResult.package : null}
    />
  );
}
