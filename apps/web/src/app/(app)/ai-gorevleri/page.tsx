import { redirect } from "next/navigation";

import { buildCanonicalTaskCenterRedirect } from "@/lib/navigation/legacy-task-routes";

type AiTaskLegacyListPageProps = Readonly<{
  searchParams?: Promise<Readonly<Record<string, string | string[] | undefined>>>;
}>;

export default async function AiTaskLegacyListPage({ searchParams }: AiTaskLegacyListPageProps) {
  const params = await searchParams;
  redirect(buildCanonicalTaskCenterRedirect(params));
}
