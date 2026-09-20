import { WorksPageContent } from "@/features/works/components/works-page-content";

type IslerPageProps = Readonly<{
  searchParams: Promise<Readonly<Record<string, string | string[] | undefined>>>;
}>;

export default function IslerPage({ searchParams }: IslerPageProps) {
  return <WorksPageContent searchParams={searchParams} />;
}
