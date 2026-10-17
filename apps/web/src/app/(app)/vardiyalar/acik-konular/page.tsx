import { readShiftOperationsWorkspace } from "@/features/shift-operations/server/shift-operations-reader";
import { HANDOVER_PRIORITY_LABELS, OPEN_ITEM_CATEGORY_LABELS } from "@/features/shift-operations/domain/shift-operations-labels";
import Link from "next/link";
import { ListChecks } from "lucide-react";

import { EmptyPanel } from "@/components/ui/empty-panel";

export default async function VardiyalarAcikKonularPage() {
  const workspace = await readShiftOperationsWorkspace();
  const items = workspace.status === "success" ? workspace.openItems : [];

  return (
    <>
      {items.length === 0 ? (
        <EmptyPanel
          icon={<ListChecks />}
          title="Açık konu yok"
          description="Açılan vardiya konuları önceliğiyle burada izlenir."
          action={<Link className="shift-ops-cta" href="/vardiyalar/aktif">Aktif vardiyayı gör</Link>}
        />
      ) : null}
      <ul className="shift-ops-item-list">
        {items.map((item) => (
          <li className="shift-ops-item" data-priority={item.handoverPriority} key={item.id}>
            <strong>{item.title}</strong>
            <p>
              {OPEN_ITEM_CATEGORY_LABELS[item.category] ?? item.category}
              {" · "}
              {HANDOVER_PRIORITY_LABELS[item.handoverPriority] ?? item.handoverPriority}
              {item.carryoverCount >= 2 ? ` · ${item.carryoverCount} vardiyadır devrediliyor` : ""}
            </p>
          </li>
        ))}
      </ul>
    </>
  );
}
