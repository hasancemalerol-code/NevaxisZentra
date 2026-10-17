import { FailureStatePage } from "@/components/ui/failure-state";
import { ContinuousImprovementShiftBriefPanel } from "@/features/factory-learning/components/continuous-improvement-shift-brief-panel";
import { ProductionManagerShiftBriefPanel } from "@/features/production-manager/components/production-manager-shift-brief-panel";
import { ShiftHandoverListView } from "@/features/shift-handover/components/shift-handover-list-view";
import { ShiftHandoverPageShell } from "@/features/shift-handover/components/shift-handover-page-shell";
import { readShiftHandoverList } from "@/features/shift-handover/server/shift-handover-queries";

export const dynamic = "force-dynamic";

export default async function VardiyaDevirPage() {
  const result = await readShiftHandoverList();

  return (
    <ShiftHandoverPageShell>
      {result.status === "failure" ? (
        <FailureStatePage code={result.code} message={result.message} failedTitle="Vardiya devirleri yüklenemedi" inline />
      ) : (
        <ShiftHandoverListView list={result.value} />
      )}
      <ProductionManagerShiftBriefPanel mode="handover" />
      <ContinuousImprovementShiftBriefPanel mode="handover" />
    </ShiftHandoverPageShell>
  );
}
