import Link from "next/link";

import {
  getProductionManagerConfidenceLabel,
  getProductionManagerStatusLabel,
  PRODUCTION_MANAGER_TITLE,
} from "../domain/production-manager-labels";
import { readProductionManagerWorkspace } from "../server/production-manager-reader";

type ProductionManagerShiftBriefPanelProps = Readonly<{
  mode: "start" | "handover";
}>;

export async function ProductionManagerShiftBriefPanel({ mode }: ProductionManagerShiftBriefPanelProps) {
  const result = await readProductionManagerWorkspace();
  if (result.status === "failure") {
    return null;
  }

  const { workspace } = result;
  const title = mode === "start" ? "Vardiyaya Başlarken — AI Üretim Yöneticisi" : "Vardiya Teslimi — Açık Kararlar";

  return (
    <section className="shift-ops-card" data-testid={`production-manager-shift-brief-${mode}`}>
      <h3>{title}</h3>
      <p>{PRODUCTION_MANAGER_TITLE}</p>
      <ul className="shift-ops-item-list">
        {workspace.predictions.slice(0, 3).map((prediction) => (
          <li key={prediction.id}>
            <strong>{prediction.title}</strong>
            <p>{prediction.summary}</p>
            <span>{getProductionManagerConfidenceLabel(prediction.confidenceBand)}</span>
          </li>
        ))}
        {workspace.bottlenecks.slice(0, 2).map((bottleneck) => (
          <li key={`${bottleneck.domain}-${bottleneck.summary}`}>
            Darboğaz: {bottleneck.summary}
          </li>
        ))}
        {(mode === "handover" ? workspace.pendingDecisions : workspace.recentDecisions)
          .slice(0, 3)
          .map((decision) => (
            <li key={decision.id}>
              <Link href={`/operasyon/yonetim-merkezi/kararlar/${decision.id}`}>
                {decision.title}
              </Link>
              <p>{getProductionManagerStatusLabel(decision.status)}</p>
            </li>
          ))}
      </ul>
      <Link className="shift-ops-cta" href="/operasyon/yonetim-merkezi">
        Operasyon Yönetim Merkezi
      </Link>
    </section>
  );
}
