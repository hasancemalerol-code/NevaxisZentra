import Link from "next/link";

import "@/features/shift-handover/components/shift-brief.css";

import {
  getProductionManagerConfidenceLabel,
  getProductionManagerStatusLabel,
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
  const title = mode === "start" ? "Üretim yöneticisi önerileri" : "Açık kararlar";
  const hint = mode === "start"
    ? "Vardiyaya başlarken dikkate alınacak öneriler."
    : "Vardiya teslimi öncesi onay bekleyen üretim kararları.";
  const predictions = workspace.predictions.slice(0, 3);
  const bottlenecks = workspace.bottlenecks.slice(0, 2);
  const decisions = (mode === "handover" ? workspace.pendingDecisions : workspace.recentDecisions).slice(0, 3);
  const isEmpty = predictions.length === 0 && bottlenecks.length === 0 && decisions.length === 0;

  return (
    <section className="shift-brief" data-testid={`production-manager-shift-brief-${mode}`}>
      <div className="shift-brief__heading">
        <div className="shift-brief__heading-text">
          <h2 className="shift-brief__title">{title}</h2>
          {isEmpty ? null : <p className="shift-brief__hint">{hint}</p>}
        </div>
        <Link className="button button-secondary button-small" href="/operasyon/yonetim-merkezi">
          Yönetim merkezine git
        </Link>
      </div>
      {isEmpty ? (
        <div className="shift-brief__empty" data-testid={`production-manager-shift-brief-${mode}-empty`}>
          <strong>{mode === "handover" ? "Henüz açık karar yok" : "Henüz öneri yok"}</strong>
          <p>
            {mode === "handover"
              ? "Bu vardiya için onay bekleyen bir üretim kararı bulunmuyor."
              : "Bu vardiya için üretim yöneticisi önerisi bulunmuyor."}
          </p>
        </div>
      ) : (
        <ul className="shift-brief__list">
          {predictions.map((prediction) => (
            <li className="shift-brief__item" key={prediction.id}>
              <strong>{prediction.title}</strong>
              <p>{prediction.summary}</p>
              <span className="shift-brief__meta">{getProductionManagerConfidenceLabel(prediction.confidenceBand)}</span>
            </li>
          ))}
          {bottlenecks.map((bottleneck) => (
            <li className="shift-brief__item" key={`${bottleneck.domain}-${bottleneck.summary}`}>
              <strong>Darboğaz</strong>
              <p>{bottleneck.summary}</p>
            </li>
          ))}
          {decisions.map((decision) => (
            <li className="shift-brief__item" key={decision.id}>
              <Link href={`/operasyon/yonetim-merkezi/kararlar/${decision.id}`}>
                {decision.title}
              </Link>
              <span className="shift-brief__meta">{getProductionManagerStatusLabel(decision.status)}</span>
            </li>
          ))}
        </ul>
      )}
    </section>
  );
}
