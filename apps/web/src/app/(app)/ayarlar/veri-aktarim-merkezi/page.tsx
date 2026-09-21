import { BulkDataOnboardingWorkspaceView } from "@/features/bulk-data-onboarding/components/bulk-data-onboarding-workspace-view";
import { readBulkDataOnboardingWorkspace } from "@/features/bulk-data-onboarding/server/bulk-data-onboarding-reader";

type PageProps = Readonly<{
  searchParams?: Promise<{ jobId?: string }>;
}>;

export default async function BulkDataOnboardingPage({ searchParams }: PageProps) {
  const params = await searchParams;
  const result = await readBulkDataOnboardingWorkspace(params?.jobId);
  if (result.status === "failure") {
    return <BulkDataOnboardingWorkspaceView workspace={null} facilityId="" />;
  }

  return (
    <BulkDataOnboardingWorkspaceView
      workspace={result.workspace}
      facilityId={result.facilityId}
    />
  );
}
