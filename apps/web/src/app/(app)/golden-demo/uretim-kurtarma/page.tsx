import Link from "next/link";
import { ShieldAlert } from "lucide-react";

import { GoldenDemoP1WorkspaceView } from "@/features/golden-demo-p1/components/golden-demo-p1-workspace-view";
import {
  GOLDEN_DEMO_P1_ROUTE,
  GOLDEN_DEMO_P1_SUBTITLE,
  GOLDEN_DEMO_P1_TITLE,
} from "@/features/golden-demo-p1/domain/golden-demo-p1-labels";
import { readGoldenDemoP1Workspace } from "@/features/golden-demo-p1/server/golden-demo-p1-reader";
import { readGoldenDemoP2PresentationStatus } from "@/features/golden-demo-p2/server/golden-demo-p2-reader";
import { readGoldenDemoP2BDigitalFactoryTeam } from "@/features/golden-demo-p2b/server/golden-demo-p2b-reader";

export default async function GoldenDemoP1Page() {
  const [result, presentationResult, teamResult] = await Promise.all([
    readGoldenDemoP1Workspace(),
    readGoldenDemoP2PresentationStatus(),
    readGoldenDemoP2BDigitalFactoryTeam(),
  ]);

  if (result.status === "failure") {
    return (
      <main className="commint-page">
        <header className="commint-executive">
          <div className="commint-executive__intro">
            <p className="commint-executive__eyebrow">ALTIN DEMO</p>
            <h1>{GOLDEN_DEMO_P1_TITLE}</h1>
            <p className="commint-executive__subtitle">{GOLDEN_DEMO_P1_SUBTITLE}</p>
          </div>
        </header>
        <div role="alert" className="commint-error-banner" data-testid="golden-demo-p1-forbidden">
          <ShieldAlert aria-hidden="true" />
          <div>
            <h3>Erişim reddedildi</h3>
            <p>{result.message}</p>
            <Link href="/operasyon/yonetim-merkezi">Operasyon Yönetim Merkezi</Link>
          </div>
        </div>
      </main>
    );
  }

  return (
    <GoldenDemoP1WorkspaceView
      workspace={result.workspace}
      presentation={presentationResult.status === "success" ? presentationResult.presentation : null}
      digitalFactoryTeam={teamResult.status === "success" ? teamResult.team : null}
    />
  );
}
