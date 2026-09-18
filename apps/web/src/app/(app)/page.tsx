import { BugunPageContent } from "@/features/bugun/components/bugun-page-content";

import "@/features/bugun/components/bugun-workspace.css";

type HomePageProps = Readonly<{
  searchParams?: Promise<Readonly<Record<string, string | string[] | undefined>>>;
}>;

export default async function HomePage({ searchParams }: HomePageProps) {
  const params = await searchParams;
  const initialView = typeof params?.gorunum === "string" ? params.gorunum : undefined;
  const initialOriginFilter = typeof params?.kaynak === "string" ? params.kaynak : undefined;

  return (
    <BugunPageContent
      initialOriginFilter={initialOriginFilter}
      initialView={initialView}
    />
  );
}
