import { formatDate } from "@/lib/presentation/format";
import { readShiftOperationsWorkspace } from "@/features/shift-operations/server/shift-operations-reader";
import { SHIFT_STATUS_LABELS } from "@/features/shift-operations/domain/shift-operations-labels";
import Link from "next/link";
import { History } from "lucide-react";

import { EmptyPanel } from "@/components/ui/empty-panel";
import { formatPercent } from "@/lib/presentation/format";

export default async function VardiyalarGecmisPage() {
  const workspace = await readShiftOperationsWorkspace();
  const items = workspace.status === "success" ? workspace.history : [];

  return (
    <>
      {items.length === 0 ? (
        <EmptyPanel
          icon={<History />}
          title="Henüz vardiya geçmişi yok"
          description="Tamamlanan vardiyalar teslim skoruyla birlikte burada listelenir."
          action={<Link className="shift-ops-cta" href="/vardiyalar/aktif">Aktif vardiyayı gör</Link>}
        />
      ) : null}
      <ul className="shift-ops-item-list">
        {items.map((shift) => (
          <li className="shift-ops-card" key={shift.id}>
            <strong>{formatDate(shift.shiftDate)} · {shift.name}</strong>
            <p>
              {SHIFT_STATUS_LABELS[shift.status as keyof typeof SHIFT_STATUS_LABELS] ?? shift.status}
              {shift.handoverReadinessScore ? ` · Teslim skoru: ${formatPercent(shift.handoverReadinessScore, 0)}` : ""}
            </p>
          </li>
        ))}
      </ul>
    </>
  );
}
