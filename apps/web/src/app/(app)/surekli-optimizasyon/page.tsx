import { loadContinuousOptimizationControlCenter } from "@/features/continuous-optimization/server/continuous-optimization-reader";

import { ContinuousOptimizationControlCenterView } from "@/features/continuous-optimization/components/continuous-optimization-control-center-view";

export default async function SurekliOptimizasyonPage() {
  const result = await loadContinuousOptimizationControlCenter();

  if (result.status === "failure") {
    return (
      <div className="fact-workspace" data-testid="continuous-optimization-control-center">
        <p>{result.message}</p>
      </div>
    );
  }

  return (
    <div className="fact-workspace">
      <ContinuousOptimizationControlCenterView controlCenter={result.controlCenter} />
    </div>
  );
}
