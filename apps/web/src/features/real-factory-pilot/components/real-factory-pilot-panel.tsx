import { enumLabel } from "@/lib/presentation/enum-labels";

import type { RealFactoryPilotWorkspace } from "../server/real-factory-pilot-api-client";

type Props = {
  workspace: RealFactoryPilotWorkspace;
};

export function RealFactoryPilotPanel({ workspace }: Props) {
  return (
    <section className="state-panel" data-testid="real-factory-pilot-workspace">
      <header className="production-execution-header">
        <p>Fabrika Bağlantıları</p>
        <h2>{workspace.lineCode}</h2>
        <p>
          Aşama: <strong>{enumLabel(workspace.currentPhase)}</strong> · {enumLabel(workspace.acceptanceKind)}
        </p>
      </header>

      <div className="production-execution-grid">
        <article className="state-panel">
          <h3>Bağlantılar</h3>
          <ul>
            {workspace.connectors.map((connector) => (
              <li key={connector.connectorId}>
                {connector.name} — {enumLabel(connector.healthStatus)} / {enumLabel(connector.mappingStatus)}
                <br />
                <small>{connector.eventsIngested} alınan olay</small>
              </li>
            ))}
          </ul>
        </article>

        <article className="state-panel">
          <h3>Veri Kalitesi</h3>
          {workspace.dataQuality ? (
            <>
              <p>{enumLabel(workspace.dataQuality.overallState)}</p>
              {workspace.dataQuality.blocksAutonomousAction ? (
                <p role="alert">Otonom aksiyon engelli</p>
              ) : null}
            </>
          ) : (
            <p>Henüz değerlendirilmedi</p>
          )}
        </article>

        <article className="state-panel">
          <h3>Gölge Modu</h3>
          {workspace.shadow ? (
            <>
              <p>{enumLabel(workspace.shadow.mode)}</p>
              <p>
                Fiziksel aksiyon:{" "}
                {workspace.shadow.blocksPhysicalActions ? "ENGELLİ" : "İZİNLİ"}
              </p>
            </>
          ) : (
            <p>Pasif</p>
          )}
        </article>

        <article className="state-panel">
          <h3>Verim Vakası</h3>
          <p>{workspace.activeCaseId ?? "—"}</p>
          {workspace.evidencePack ? (
            <p>
              Doğrulama: {enumLabel(workspace.evidencePack.verificationOutcome)}
              {workspace.evidencePack.knowledgePromoted ? " · Bilgi kalıcılaştırıldı" : ""}
            </p>
          ) : null}
        </article>
      </div>
    </section>
  );
}
