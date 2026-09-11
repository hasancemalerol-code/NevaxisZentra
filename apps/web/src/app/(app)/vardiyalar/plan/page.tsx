import { readShiftOperationsWorkspace } from "@/features/shift-operations/server/shift-operations-reader";
import { SHIFT_STATUS_LABELS } from "@/features/shift-operations/domain/shift-operations-labels";

export default async function VardiyalarPlanPage() {
  const workspace = await readShiftOperationsWorkspace();
  const items = workspace.status === "success" ? workspace.history : [];

  return (
    <>
      <header className="shift-ops-hero">
        <p className="shift-ops-hero__eyebrow">VARDİYA PLANI</p>
        <h1>Vardiya Planı</h1>
      </header>
      <ul className="shift-ops-item-list">
        {items.map((shift) => (
          <li className="shift-ops-card" key={shift.id}>
            <strong>{shift.name}</strong>
            <p>
              {shift.shiftDate} · {shift.shiftCode} ·{" "}
              {SHIFT_STATUS_LABELS[shift.status as keyof typeof SHIFT_STATUS_LABELS] ?? shift.status}
            </p>
            <p>Amir: {shift.supervisorLabel ?? "—"} · Ekip: {shift.teamLabel ?? "—"}</p>
          </li>
        ))}
      </ul>
    </>
  );
}
