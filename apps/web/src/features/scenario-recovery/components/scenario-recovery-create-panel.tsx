"use client";

import { useActionState } from "react";

import { createScenarioRecoveryRunAction, type CreateScenarioRecoveryActionState } from "../server/scenario-recovery-actions";
import type { ScenarioRecoveryHistoryItemApi } from "../server/scenario-recovery-api-client";

type ScenarioRecoveryCreatePanelProps = Readonly<{
  lineId: string;
  entryPoint?: string;
  orderReference?: string;
  constraintStationCode?: string;
  canSimulate: boolean;
  history: readonly ScenarioRecoveryHistoryItemApi[];
}>;

const initialState: CreateScenarioRecoveryActionState = { status: "idle" };

export function ScenarioRecoveryCreatePanel(props: ScenarioRecoveryCreatePanelProps) {
  const [state, formAction, pending] = useActionState(createScenarioRecoveryRunAction, initialState);

  return (
    <section className="commint-page" data-testid="scenario-recovery-ready">
      <h1>Senaryo Kurtarma</h1>
      <p>Bu hat için henüz açık bir senaryo çalıştırması seçilmedi. Sayfa görüntülenirken senaryo oluşturulmaz.</p>
      {props.history.length > 0 ? (
        <ul aria-label="Önceki senaryo çalıştırmaları">
          {props.history.map((item) => (
            <li key={item.runId}>
              <a href={`/senaryo-kurtarma?runId=${encodeURIComponent(item.runId)}`}>{item.problemSummary}</a>
            </li>
          ))}
        </ul>
      ) : (
        <p>Bu hat için kayıtlı senaryo yok.</p>
      )}
      {props.canSimulate ? (
        <form action={formAction}>
          <input type="hidden" name="lineId" value={props.lineId} />
          {props.entryPoint ? <input type="hidden" name="entryPoint" value={props.entryPoint} /> : null}
          {props.orderReference ? <input type="hidden" name="orderReference" value={props.orderReference} /> : null}
          {props.constraintStationCode ? (
            <input type="hidden" name="constraintStationCode" value={props.constraintStationCode} />
          ) : null}
          <button type="submit" disabled={pending} data-testid="scenario-recovery-create">
            {pending ? "Senaryo oluşturuluyor…" : "Senaryo oluştur"}
          </button>
          {state.status === "failure" ? (
            <p role="alert" data-testid="scenario-recovery-create-error">
              {state.message}
            </p>
          ) : null}
        </form>
      ) : (
        <p data-testid="scenario-recovery-simulate-denied">Senaryo oluşturmak için simülasyon izni gerekli.</p>
      )}
    </section>
  );
}
