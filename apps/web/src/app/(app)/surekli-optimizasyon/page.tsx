import { loadContinuousOptimizationControlCenter } from "@/features/continuous-optimization/server/continuous-optimization-reader";

import { ContinuousOptimizationControlCenterView } from "@/features/continuous-optimization/components/continuous-optimization-control-center-view";
import { customerMessage } from "@/lib/presentation/customer-message";

export default async function SurekliOptimizasyonPage() {
  const result = await loadContinuousOptimizationControlCenter();

  if (result.status === "failure") {
    return (
      <div className="fact-workspace continuous-optimization-page" data-testid="continuous-optimization-control-center">
        <p>{customerMessage(result.message)}</p>
      </div>
    );
  }

  return (
    <div className="fact-workspace continuous-optimization-page">
      <ContinuousOptimizationControlCenterView controlCenter={result.controlCenter} />
    </div>
  );
}
