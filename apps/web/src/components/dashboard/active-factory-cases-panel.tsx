import Link from "next/link";

import { getFactoryBlockerLabel } from "@/features/factory-orchestration/domain/factory-orchestration-labels";
import { readActiveFactoryCasesPanel } from "@/features/factory-orchestration/server/factory-orchestration-reader";

export async function ActiveFactoryCasesDashboardPanel() {
  const panel = await readActiveFactoryCasesPanel();
  if (panel.status === "failure") {
    return null;
  }

  return (
    <section className="dashboard-panel" data-testid="dashboard-factory-orchestration-panel">
      <p className="shift-ops-hero__eyebrow">AKTİF OPERASYON VAKALARI</p>
      <h2>{panel.activeCount} açık vaka</h2>
      <p>Kritik: {panel.criticalCases.length}</p>
      <p>Bugün doğrulanan kapanış: {panel.verifiedTodayCount}</p>
      <ul>
        {panel.cases.map((caseRow) => (
          <li key={caseRow.id}>
            <Link href={`/operasyon/yonetim-merkezi/vakalar/${caseRow.id}`}>
              {caseRow.title}
            </Link>
            {" · "}
            {getFactoryBlockerLabel(caseRow.currentBlockerCode)}
          </li>
        ))}
      </ul>
      <Link className="shift-ops-cta" href="/operasyon/yonetim-merkezi">
        Operasyon Yönetim Merkezi
      </Link>
    </section>
  );
}
