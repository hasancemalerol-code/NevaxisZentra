import Link from "next/link";
import { ClipboardList } from "lucide-react";

import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { MoldFollowUpWorkspaceView } from "@/features/molds/components/mold-follow-up-workspace-view";
import "@/features/molds/components/mold-registry.css";
import "@/features/quality-control/components/quality-workspace.css";
import { readMoldFollowUpWorkspace } from "@/features/molds/server/mold-follow-up-reader";

type MoldFollowUpPageProps = Readonly<{
  searchParams: Promise<Readonly<{ mold?: string }>>;
}>;

export default async function MoldFollowUpPage({ searchParams }: MoldFollowUpPageProps) {
  await forbidUnlessGranted("mold.read");

  const params = await searchParams;
  const result = await readMoldFollowUpWorkspace(params.mold);

  if (result.status === "failure") {
    return (
      <main className="mold-registry-page">
        <header className="mold-registry-header">
          <div className="mold-registry-header__intro">
            <p className="mold-registry-header__eyebrow">KALIP YÖNETİMİ</p>
            <h1>Takip ve Görevler</h1>
          </div>
        </header>
        <div role="alert" className="mold-registry-error" data-testid="mold-follow-up-server-error">
          <p>{result.message}</p>
        </div>
      </main>
    );
  }

  return (
    <main className="mold-registry-page">
      <header className="mold-registry-header">
        <div className="mold-registry-header__intro">
          <p className="mold-registry-header__eyebrow">KALIP YÖNETİMİ</p>
          <h1>Takip ve Görevler</h1>
          <p className="mold-registry-header__subtitle">
            Kalıp yaşam döngüsü ve sayaç kanıtı için insan incelemeli AI görev taslağı akışı.
          </p>
        </div>
        <div className="mold-registry-header__actions">
          <Link href="/kaliplar" className="mold-registry-header__link">
            <ClipboardList aria-hidden="true" />
            Kalıp kayıtları
          </Link>
        </div>
      </header>

      <MoldFollowUpWorkspaceView
        workspace={result.workspace}
        initialMold={result.mold}
        moldError={result.moldError}
      />
    </main>
  );
}
