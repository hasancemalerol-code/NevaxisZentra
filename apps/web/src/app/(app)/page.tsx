import { AiTaskListPageContent } from "@/features/ai-task-center/components/ai-task-list-page-content";

type HomePageProps = Readonly<{
  searchParams?: Promise<Readonly<Record<string, string | string[] | undefined>>>;
}>;

export default async function HomePage({ searchParams }: HomePageProps) {
  const params = await searchParams;
  const initialView = typeof params?.gorunum === "string" ? params.gorunum : undefined;
  const initialOriginFilter = typeof params?.kaynak === "string" ? params.kaynak : undefined;

  return (
    <AiTaskListPageContent
      initialOriginFilter={initialOriginFilter}
      initialView={initialView}
      listPath="/"
      showCreateAction
    />
  );
}
