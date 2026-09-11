import { readShiftOperationsWorkspace } from "@/features/shift-operations/server/shift-operations-reader";
import { HANDOVER_PRIORITY_LABELS, OPEN_ITEM_CATEGORY_LABELS } from "@/features/shift-operations/domain/shift-operations-labels";

export default async function VardiyalarAcikKonularPage() {
  const workspace = await readShiftOperationsWorkspace();
  const items = workspace.status === "success" ? workspace.openItems : [];

  return (
    <>
      <header className="shift-ops-hero">
        <p className="shift-ops-hero__eyebrow">AÇIK KONULAR</p>
        <h1>Açık Konular</h1>
      </header>
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
