import { LivePlanWorkspacePageContent } from "@/features/live-plan-workspace/components/live-plan-workspace-page-content";

type PlanlamaPageProps = Readonly<{
  searchParams?: Promise<Readonly<Record<string, string | string[] | undefined>>>;
}>;

export default function ProductionPlanningPage({ searchParams }: PlanlamaPageProps) {
  return <LivePlanWorkspacePageContent searchParams={searchParams} />;
}
