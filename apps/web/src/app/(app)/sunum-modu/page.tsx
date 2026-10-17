

import { PresentationModeView } from "@/features/presentation-mode/components/presentation-mode-view";
import { loadPresentationModeDashboard } from "@/features/presentation-mode/server/presentation-mode-reader";
import { forbidUnlessPresentationModeAccess } from "@/features/presentation-mode/server/require-presentation-mode-access";

import { FailureStatePage } from "@/components/ui/failure-state";

export default async function PresentationModePage() {
  await forbidUnlessPresentationModeAccess();

  const result = await loadPresentationModeDashboard();
  if (result.status === "failure") {
    return (
      <div className="commint-page">
        <div data-testid="presentation-mode-forbidden"><FailureStatePage code={"code" in result ? result.code : undefined} message={result.message} failedTitle="Veri yüklenemedi" inline /></div>
      </div>
    );
  }

  return <PresentationModeView dashboard={result.dashboard} />;
}
