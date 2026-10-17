import { formatDate } from "@/lib/presentation/format";
import { readShiftOperationsWorkspace } from "@/features/shift-operations/server/shift-operations-reader";
import { SHIFT_STATUS_LABELS } from "@/features/shift-operations/domain/shift-operations-labels";
import Link from "next/link";
import { CalendarClock } from "lucide-react";

import { EmptyPanel } from "@/components/ui/empty-panel";

export default async function VardiyalarPlanPage() {
  const workspace = await readShiftOperationsWorkspace();
  const items = workspace.status === "success" ? workspace.history : [];

  return (
    <>
      {items.length === 0 ? (
        <EmptyPanel
          icon={<CalendarClock />}
          title="Planlanmış vardiya yok"
          description="Vardiya tanımları ve planlanan vardiyalar oluşturulduğunda burada görünür. Ekip ve makine sorumlulukları ayrı bir sayfada atanır."
          action={<Link className="shift-ops-cta" href="/vardiyalar/kaynak">Ekip ve kaynakları yönet</Link>}
        />
      ) : null}
      <ul className="shift-ops-item-list">
        {items.map((shift) => (
          <li className="shift-ops-card" key={shift.id}>
            <strong>{shift.name}</strong>
            <p>
              {formatDate(shift.shiftDate)} · {shift.shiftCode} ·{" "}
              {SHIFT_STATUS_LABELS[shift.status as keyof typeof SHIFT_STATUS_LABELS] ?? shift.status}
            </p>
            {shift.supervisorLabel || shift.teamLabel ? <p>{[shift.supervisorLabel ? `Amir: ${shift.supervisorLabel}` : null, shift.teamLabel ? `Ekip: ${shift.teamLabel}` : null].filter(Boolean).join(" · ")}</p> : null}
          </li>
        ))}
      </ul>
    </>
  );
}
