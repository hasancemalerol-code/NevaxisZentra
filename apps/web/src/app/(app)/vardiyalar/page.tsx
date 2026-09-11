import { CalendarClock } from "lucide-react";

import { forbidUnlessGranted } from "@/features/admin-access/server/require-current-permission";
import { ShiftOperationsOverviewView } from "@/features/shift-operations/components/shift-operations-overview-view";
import { readShiftOperationsWorkspace } from "@/features/shift-operations/server/shift-operations-reader";

export default async function VardiyalarPage() {
  await forbidUnlessGranted("shift-workforce.read");

  const workspace = await readShiftOperationsWorkspace();

  if (workspace.status === "failure") {
    const isUnauthorized =
      workspace.code === "permission-denied" || workspace.code === "unauthenticated";

    return (
      <main>
        <div role="alert">
          <CalendarClock aria-hidden="true" />
          <h3>{isUnauthorized ? "Bu modüle erişim yetkiniz yok" : "Vardiya verisi yüklenemedi"}</h3>
          <p>{workspace.message}</p>
        </div>
      </main>
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
