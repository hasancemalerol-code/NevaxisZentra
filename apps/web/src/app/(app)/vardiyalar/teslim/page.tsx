import { ContinuousImprovementShiftBriefPanel } from "@/features/factory-learning/components/continuous-improvement-shift-brief-panel";
import { ProductionManagerShiftBriefPanel } from "@/features/production-manager/components/production-manager-shift-brief-panel";
import { ShiftOperationsHandoverView } from "@/features/shift-operations/components/shift-operations-handover-view";
import { readShiftOperationsWorkspace } from "@/features/shift-operations/server/shift-operations-reader";

export default async function VardiyalarTeslimPage() {
  const workspace = await readShiftOperationsWorkspace();
  if (workspace.status === "failure") {
    return <p role="alert">{workspace.message}</p>;
  }

  return (
    <>
      <ProductionManagerShiftBriefPanel mode="handover" />
      <ContinuousImprovementShiftBriefPanel mode="handover" />
      <ShiftOperationsHandoverView
        openItems={workspace.openItems}
        preview={workspace.preview}
        shift={workspace.currentShift}
      />
    </>
  );
}
