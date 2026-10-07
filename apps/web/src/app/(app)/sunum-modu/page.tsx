import { TriangleAlert } from "lucide-react";

import { PresentationModeView } from "@/features/presentation-mode/components/presentation-mode-view";
import { loadPresentationModeDashboard } from "@/features/presentation-mode/server/presentation-mode-reader";
import { forbidUnlessPresentationModeAccess } from "@/features/presentation-mode/server/require-presentation-mode-access";

export default async function PresentationModePage() {
  await forbidUnlessPresentationModeAccess();

  const result = await loadPresentationModeDashboard();
  if (result.status === "failure") {
    return (
      <main className="commint-page">
        <div role="alert" className="commint-error-banner" data-testid="presentation-mode-forbidden">
          <TriangleAlert aria-hidden="true" />
          <div>
            <h3>Veri yüklenemedi</h3>
            <p>{result.message}</p>
          </div>
        </div>
      </main>
    );
  }

  return <PresentationModeView dashboard={result.dashboard} />;
}
