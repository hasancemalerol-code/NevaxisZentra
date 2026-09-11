import { readShiftAssignmentContextOptions, readShiftAssignmentPermissions } from "@/features/shift-assignment/server/shift-assignment-queries";
import { readShiftOperationsWorkspace } from "@/features/shift-operations/server/shift-operations-reader";

export default async function VardiyalarKaynakPage() {
  const [workspace, assignmentPermissions, assignmentContext] = await Promise.all([
    readShiftOperationsWorkspace(),
    readShiftAssignmentPermissions(),
    readShiftAssignmentContextOptions(),
  ]);

  const shift = workspace.status === "success" ? workspace.currentShift : null;
  const canManage = assignmentPermissions.status === "success" && assignmentPermissions.capabilities.canManage;
  const machines = assignmentContext.status === "success" ? assignmentContext.options.machines : [];

  return (
    <>
      <header className="shift-ops-hero">
        <p className="shift-ops-hero__eyebrow">EKİP & KAYNAK</p>
        <h1>Ekip & Kaynak Dağılımı</h1>
        <p>{shift?.teamLabel ?? "Ekip ataması"} · Amir: {shift?.supervisorLabel ?? "—"}</p>
      </header>
      <section className="shift-ops-card">
        <h3>Operasyonel atama</h3>
        <p>Hat, makine ve görev sorumlulukları vardiya bağlamında gösterilir.</p>
        <ul>
          {machines.slice(0, 8).map((machine) => (
            <li key={machine.value}>{machine.label}</li>
          ))}
        </ul>
        {canManage ? <p>Yetkinizle operasyonel yeniden atama yapılabilir.</p> : null}
      </section>
    </>
  );
}
