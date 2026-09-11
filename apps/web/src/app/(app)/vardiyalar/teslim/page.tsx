import { ShiftOperationsHandoverView } from "@/features/shift-operations/components/shift-operations-handover-view";
import { readShiftOperationsWorkspace } from "@/features/shift-operations/server/shift-operations-reader";

export default async function VardiyalarTeslimPage() {
  const workspace = await readShiftOperationsWorkspace();
  if (workspace.status === "failure") {
    return <p role="alert">{workspace.message}</p>;
  }

  return (
    <ShiftOperationsHandoverView
      openItems={workspace.openItems}
      preview={workspace.preview}
      shift={workspace.currentShift}
    />
  );
}
