import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { QualityNcrFollowUpWorkspaceView } from "@/features/quality-control/components/quality-ncr-follow-up-workspace-view";
import { QualityPageShell } from "@/features/quality-control/components/quality-page-shell";
import "@/features/quality-control/components/quality-workspace.css";
import { readQualityNcrFollowUpWorkspace } from "@/features/quality-control/server/quality-ncr-follow-up-reader";

type QualityNcrFollowUpPageProps = Readonly<{
  searchParams: Promise<Readonly<{ finding?: string }>>;
}>;

export default async function QualityNcrFollowUpPage({ searchParams }: QualityNcrFollowUpPageProps) {
  await forbidUnlessGranted("quality.read");

  const params = await searchParams;
  const result = await readQualityNcrFollowUpWorkspace(params.finding);

  if (result.status === "failure") {
    return (
      <QualityPageShell
        backHref="/kalite"
        backLabel="Kalite ana sayfası"
        eyebrow="KALİTE KONTROL"
        title="Takip ve Görevler"
        nav="follow-up"
      >
        <div role="alert" className="quality-error-panel" data-testid="quality-ncr-follow-up-server-error">
          <p>{result.message}</p>
        </div>
      </QualityPageShell>
    );
  }

  return (
    <QualityPageShell
      backHref="/kalite"
      backLabel="Kalite ana sayfası"
      eyebrow="KALİTE KONTROL"
      title="Takip ve Görevler"
      subtitle="Uygunsuzluk kayıtları için insan incelemeli AI görev taslağı akışı."
      nav="follow-up"
    >
      <QualityNcrFollowUpWorkspaceView
        workspace={result.workspace}
        initialFinding={result.finding}
        findingError={result.findingError}
      />
    </QualityPageShell>
  );
}
