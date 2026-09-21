import { OperationalValueControlCenterView } from "@/features/operational-value/components/operational-value-control-center-view";
import { loadOperationalValueControlCenter } from "@/features/operational-value/server/operational-value-reader";

export default async function OperationalValuePage() {
  const result = await loadOperationalValueControlCenter();

  if (result.status === "failure") {
    return (
      <div className="fact-workspace" data-testid="operational-value-control-center">
        <p>{result.message}</p>
      </div>
    );
  }

  return (
    <div className="fact-workspace">
      <OperationalValueControlCenterView controlCenter={result.controlCenter} />
    </div>
  );
}
