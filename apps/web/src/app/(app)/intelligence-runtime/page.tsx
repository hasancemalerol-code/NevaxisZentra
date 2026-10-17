import { IntelligenceRuntimeControlCenterView } from "@/features/intelligence-runtime/components/intelligence-runtime-control-center-view";
import { loadIntelligenceRuntimeControlCenter } from "@/features/intelligence-runtime/server/intelligence-runtime-reader";
import { customerMessage } from "@/lib/presentation/customer-message";

export default async function IntelligenceRuntimePage() {
  const result = await loadIntelligenceRuntimeControlCenter();

  if (result.status === "failure") {
    return (
      <div className="fact-workspace" data-testid="intelligence-runtime-control-center">
        <p>{customerMessage(result.message)}</p>
      </div>
    );
  }

  return (
    <div className="fact-workspace">
      <IntelligenceRuntimeControlCenterView controlCenter={result.controlCenter} />
    </div>
  );
}
