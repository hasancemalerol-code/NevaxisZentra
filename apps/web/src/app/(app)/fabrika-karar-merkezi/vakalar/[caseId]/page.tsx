import { FactoryDecisionCaseDetailView } from "@/features/factory-decision-center/components/factory-decision-case-detail-view";
import { loadFactoryDecisionCaseDetail } from "@/features/factory-decision-center/server/factory-decision-center-reader";

type PageProps = Readonly<{
  params: Promise<{ caseId: string }>;
}>;

export default async function FactoryDecisionCaseDetailPage({ params }: PageProps) {
  const { caseId } = await params;
  const result = await loadFactoryDecisionCaseDetail(caseId);

  if (result.status === "failure") {
    return (
      <main className="commint-page">
        <div className="commint-error-banner">{result.message}</div>
      </main>
    );
  }

  return <FactoryDecisionCaseDetailView detail={result.detail} />;
}
