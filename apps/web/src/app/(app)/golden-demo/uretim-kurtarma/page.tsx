


import { GoldenDemoP1WorkspaceView } from "@/features/golden-demo-p1/components/golden-demo-p1-workspace-view";
import {
  GOLDEN_DEMO_P1_SUBTITLE,
  GOLDEN_DEMO_P1_TITLE,
} from "@/features/golden-demo-p1/domain/golden-demo-p1-labels";
import { readGoldenDemoP1Workspace } from "@/features/golden-demo-p1/server/golden-demo-p1-reader";
import { readGoldenDemoP2PresentationStatus } from "@/features/golden-demo-p2/server/golden-demo-p2-reader";
import { readGoldenDemoP2BDigitalFactoryTeam } from "@/features/golden-demo-p2b/server/golden-demo-p2b-reader";
import { readGoldenDemoP3Outcome } from "@/features/golden-demo-p3/server/golden-demo-p3-reader";
import { forbidUnlessPresentationModeAccess } from "@/features/presentation-mode/server/require-presentation-mode-access";
import { FailureStatePage } from "@/components/ui/failure-state";

export default async function GoldenDemoP1Page() {
  await forbidUnlessPresentationModeAccess();
  const [result, presentationResult, teamResult, outcomeResult] = await Promise.all([
    readGoldenDemoP1Workspace(),
    readGoldenDemoP2PresentationStatus(),
    readGoldenDemoP2BDigitalFactoryTeam(),
    readGoldenDemoP3Outcome(),
  ]);

  if (result.status === "failure") {
    return (
      <div className="commint-page">
        <header className="commint-executive">
          <div className="commint-executive__intro">
            <p className="commint-executive__eyebrow">ALTIN DEMO</p>
            <h1>{GOLDEN_DEMO_P1_TITLE}</h1>
            <p className="commint-executive__subtitle">{GOLDEN_DEMO_P1_SUBTITLE}</p>
          </div>
        </header>
        <div data-testid="golden-demo-p1-forbidden"><FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Veri yüklenemedi" inline /></div>
      </div>
    );
  }

  return (
    <GoldenDemoP1WorkspaceView
      workspace={result.workspace}
      presentation={presentationResult.status === "success" ? presentationResult.presentation : null}
      digitalFactoryTeam={teamResult.status === "success" ? teamResult.team : null}
      outcome={outcomeResult.status === "success" ? outcomeResult.outcome : null}
    />
  );
}
