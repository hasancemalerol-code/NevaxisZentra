import { readShiftOperationsWorkspace } from "@/features/shift-operations/server/shift-operations-reader";
import { SHIFT_STATUS_LABELS } from "@/features/shift-operations/domain/shift-operations-labels";

export default async function VardiyalarGecmisPage() {
  const workspace = await readShiftOperationsWorkspace();
  const items = workspace.status === "success" ? workspace.history : [];

  return (
    <>
      <header className="shift-ops-hero">
        <p className="shift-ops-hero__eyebrow">VARDİYA GEÇMİŞİ</p>
        <h1>Geçmiş</h1>
      </header>
      <ul className="shift-ops-item-list">
        {items.map((shift) => (
          <li className="shift-ops-card" key={shift.id}>
            <strong>{shift.shiftDate} · {shift.name}</strong>
            <p>
              {SHIFT_STATUS_LABELS[shift.status as keyof typeof SHIFT_STATUS_LABELS] ?? shift.status}
              {" · "}
              Teslim skoru: {shift.handoverReadinessScore}%
            </p>
          </li>
        ))}
      </ul>
    </>
  );
}
