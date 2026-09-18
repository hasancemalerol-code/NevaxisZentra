import { FactoryWorkspacePageContent } from "@/features/factory-workspace/components/factory-workspace-page-content";

type FabrikaPageProps = Readonly<{
  searchParams?: Promise<Readonly<Record<string, string | string[] | undefined>>>;
}>;

export default function FabrikaPage({ searchParams }: FabrikaPageProps) {
  return <FactoryWorkspacePageContent searchParams={searchParams} />;
}
