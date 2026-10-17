import Link from "next/link";
import { Users } from "lucide-react";

import { EmptyPanel } from "@/components/ui/empty-panel";
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
      <p className="shift-ops-meta" aria-label="Atama durumu">
        <span className="shift-ops-chip" data-tone={shift?.teamLabel ? "ok" : "neutral"}>{shift?.teamLabel ? `Ekip: ${shift.teamLabel}` : "Ekip: atanmadı"}</span>
        <span className="shift-ops-chip" data-tone={shift?.supervisorLabel ? "ok" : "neutral"}>
          {shift?.supervisorLabel ? `Amir: ${shift.supervisorLabel}` : "Amir: atanmadı"}
        </span>
        {canManage ? <span className="shift-ops-chip" data-tone="neutral">Yetki: yeniden atama yapabilirsiniz</span> : null}
      </p>
      <section className="shift-ops-card" aria-labelledby="shift-ops-assignment-title">
        <div className="shift-ops-card__heading">
          <h2 id="shift-ops-assignment-title">Operasyonel atama</h2>
          {machines.length > 0 ? <span className="shift-ops-count">{machines.length} kaynak</span> : null}
        </div>
        {machines.length === 0 ? (
          <EmptyPanel
            compact
            icon={<Users />}
            title="Atanabilir makine yok"
            description="Hat, makine ve görev sorumlulukları tanımlandığında vardiya bağlamında burada listelenir."
            action={<Link className="shift-ops-cta" href="/vardiyalar/plan">Vardiya planına git</Link>}
          />
        ) : (
          <div className="shift-ops-table-wrap" tabIndex={0} role="region" aria-label="Atanabilir kaynaklar (kaydırılabilir)">
            <table className="shift-ops-table shift-ops-table--resources" aria-label="Atanabilir kaynaklar">
              <thead>
                <tr>
                  <th scope="col">Kaynak</th>
                  <th scope="col">Tür</th>
                </tr>
              </thead>
              <tbody>
                {machines.slice(0, 8).map((machine) => (
                  <tr key={machine.value}>
                    <td className="shift-ops-row__label">{machine.label}</td>
                    <td><span className="shift-ops-chip" data-tone="neutral">Makine</span></td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
    </>
  );
}
