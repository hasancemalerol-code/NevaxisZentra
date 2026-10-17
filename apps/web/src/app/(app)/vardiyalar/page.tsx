

import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { ShiftOperationsOverviewView } from "@/features/shift-operations/components/shift-operations-overview-view";
import { readShiftOperationsWorkspace } from "@/features/shift-operations/server/shift-operations-reader";

import { FailureStatePage } from "@/components/ui/failure-state";

export default async function VardiyalarPage() {
  await forbidUnlessGranted("shift-workforce.read");

  const workspace = await readShiftOperationsWorkspace();

  if (workspace.status === "failure") {
    return (
      <div>
        <FailureStatePage code={"code" in workspace ? workspace.code : undefined} message={workspace.message} failedTitle="Vardiya verisi yüklenemedi" inline />
      </div>
    );
  }

  return (
    <ShiftOperationsOverviewView
      currentShift={workspace.currentShift}
      metrics={workspace.metrics}
      openItems={workspace.openItems}
    />
  );
}
