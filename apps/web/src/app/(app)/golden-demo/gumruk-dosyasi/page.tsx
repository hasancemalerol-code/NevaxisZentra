


import { GoldenDemoCustomsFileWorkspaceView } from "@/features/golden-demo-customs-file/components/golden-demo-customs-file-workspace-view";
import {
  GOLDEN_DEMO_CUSTOMS_FILE_SUBTITLE,
  GOLDEN_DEMO_CUSTOMS_FILE_TITLE,
} from "@/features/golden-demo-customs-file/domain/golden-demo-customs-file-labels";
import { readGoldenDemoCustomsFileWorkspace } from "@/features/golden-demo-customs-file/server/golden-demo-customs-file-reader";
import { forbidUnlessPresentationModeAccess } from "@/features/presentation-mode/server/require-presentation-mode-access";
import { FailureStatePage } from "@/components/ui/failure-state";

export default async function GoldenDemoCustomsFilePage() {
  await forbidUnlessPresentationModeAccess();

  const result = await readGoldenDemoCustomsFileWorkspace();

  if (result.status === "failure") {
    return (
      <div className="commint-page">
        <header className="commint-executive">
          <div className="commint-executive__intro">
            <p className="commint-executive__eyebrow">ALTIN DEMO</p>
            <h1>{GOLDEN_DEMO_CUSTOMS_FILE_TITLE}</h1>
            <p className="commint-executive__subtitle">{GOLDEN_DEMO_CUSTOMS_FILE_SUBTITLE}</p>
          </div>
        </header>
        <div data-testid="golden-demo-customs-file-forbidden"><FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Veri yüklenemedi" inline /></div>
      </div>
    );
  }

  return <GoldenDemoCustomsFileWorkspaceView workspace={result.workspace} />;
}
