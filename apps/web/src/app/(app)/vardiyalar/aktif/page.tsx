import Link from "next/link";
import { PlayCircle } from "lucide-react";

import { EmptyPanel } from "@/components/ui/empty-panel";
import { ContinuousImprovementShiftBriefPanel } from "@/features/factory-learning/components/continuous-improvement-shift-brief-panel";
import { ProductionManagerShiftBriefPanel } from "@/features/production-manager/components/production-manager-shift-brief-panel";
import { ShiftOperationsActiveShiftView } from "@/features/shift-operations/components/shift-operations-active-shift-view";
import { readShiftOperationsWorkspace } from "@/features/shift-operations/server/shift-operations-reader";

export default async function VardiyalarAktifPage() {
  const workspace = await readShiftOperationsWorkspace();
  if (workspace.status === "failure" || !workspace.currentShift) {
    return (
      <EmptyPanel
        icon={<PlayCircle />}
        title="Şu anda aktif vardiya yok"
        description="Aktif bir vardiya başladığında üretim, ekip ve açık konular burada görünür. Vardiya planından bir vardiya başlatabilirsiniz."
        action={<Link className="shift-ops-cta" href="/vardiyalar/plan">Vardiya planına git</Link>}
      />
    );
  }

  return (
    <>
      <ProductionManagerShiftBriefPanel mode="start" />
      <ContinuousImprovementShiftBriefPanel mode="start" />
      <ShiftOperationsActiveShiftView
        metrics={workspace.metrics}
        openItems={workspace.openItems}
        shift={workspace.currentShift}
      />
    </>
  );
}
