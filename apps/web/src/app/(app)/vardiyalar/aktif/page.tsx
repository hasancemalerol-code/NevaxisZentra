import { ShiftOperationsActiveShiftView } from "@/features/shift-operations/components/shift-operations-active-shift-view";
import { readShiftOperationsWorkspace } from "@/features/shift-operations/server/shift-operations-reader";

export default async function VardiyalarAktifPage() {
  const workspace = await readShiftOperationsWorkspace();
  if (workspace.status === "failure" || !workspace.currentShift) {
    return <p role="alert">Aktif vardiya bulunamadı.</p>;
  }

  return (
    <ShiftOperationsActiveShiftView
      metrics={workspace.metrics}
      openItems={workspace.openItems}
      shift={workspace.currentShift}
    />
  );
}
