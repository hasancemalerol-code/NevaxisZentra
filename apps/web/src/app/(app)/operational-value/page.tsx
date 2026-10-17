import { PageFrame } from "@/components/ui/page";
import { OperationalValueControlCenterView } from "@/features/operational-value/components/operational-value-control-center-view";
import { loadOperationalValueControlCenter } from "@/features/operational-value/server/operational-value-reader";
import { customerMessage } from "@/lib/presentation/customer-message";

export default async function OperationalValuePage() {
  const result = await loadOperationalValueControlCenter();

  if (result.status === "failure") {
    return (
      <PageFrame data-testid="operational-value-control-center">
        <p>{customerMessage(result.message)}</p>
      </PageFrame>
    );
  }

  return (
    <PageFrame>
      <OperationalValueControlCenterView controlCenter={result.controlCenter} />
    </PageFrame>
  );
}
