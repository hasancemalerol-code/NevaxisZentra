import { FailureStatePage } from "@/components/ui/failure-state";
import { ShiftHandoverDetailView } from "@/features/shift-handover/components/shift-handover-detail-view";
import { ShiftHandoverPageShell } from "@/features/shift-handover/components/shift-handover-page-shell";
import { isShiftHandoverId, shiftHandoverDepartmentLabel } from "@/features/shift-handover/domain/shift-handover-constants";
import { readShiftHandoverDetail } from "@/features/shift-handover/server/shift-handover-queries";

export const dynamic = "force-dynamic";

const BACK = { backHref: "/vardiya-devir", backLabel: "Vardiya devir listesine dön" } as const;

/** A missing or unreadable record keeps the list page's header position, so the page does not jump on navigation. */
function DetailFailure({ code, message }: Readonly<{ code: string; message?: string }>) {
  return (
    <ShiftHandoverPageShell showModuleTabs={false}>
      <FailureStatePage
        code={code}
        message={message}
        entityLabel="vardiya devri"
        failedTitle="Vardiya devri açılamadı"
        inline
        {...BACK}
      />
    </ShiftHandoverPageShell>
  );
}

export default async function VardiyaDevirDetailPage({ params }: Readonly<{ params: Promise<{ id: string }> }>) {
  const { id } = await params;

  if (!isShiftHandoverId(id)) {
    return <DetailFailure code="not-found" />;
  }

  const result = await readShiftHandoverDetail(id);
  if (result.status === "failure") {
    return <DetailFailure code={result.code} message={result.message} />;
  }

  const detail = result.value;
  return (
    <ShiftHandoverPageShell
      title={detail.handoverReference}
      description={`${shiftHandoverDepartmentLabel(detail.department)} · ${detail.facilityName}`}
      showModuleTabs={false}
      backHref="/vardiya-devir"
      backLabel="Vardiya devirleri"
    >
      <ShiftHandoverDetailView detail={detail} />
    </ShiftHandoverPageShell>
  );
}
