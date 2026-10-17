import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import "@/features/teams/components/teams-inbox.css";
import { AiTaskListPageContent } from "@/features/ai-task-center/components/ai-task-list-page-content";

export const dynamic = "force-dynamic";

type TeamsWorkInboxPageProps = Readonly<{
  searchParams?: Promise<Readonly<Record<string, string | string[] | undefined>>>;
}>;

/** Operasyon iletişimi iş kutusu: görev merkezi içeriği, iletişim sekme çubuğunun içinde. */
export default async function TeamsWorkInboxPage({ searchParams }: TeamsWorkInboxPageProps) {
  await forbidUnlessGranted("teams.read");
  const params = await searchParams;
  const view = typeof params?.gorunum === "string" ? params.gorunum : undefined;
  const origin = typeof params?.kaynak === "string" ? params.kaynak : undefined;

  return (
    <AiTaskListPageContent
      description="Operasyon iletişiminden doğan görevler, onaylar ve vardiya teslimleri tek iş kutusunda."
      eyebrow="Operasyon iletişimi"
      initialOriginFilter={origin}
      initialView={view}
      listPath="/teams/is-kutum"
      showCreateAction
      title="İş kutusu"
    />
  );
}
