import type { RealFactoryPilotWorkspace } from "../server/real-factory-pilot-api-client";

type Props = {
  workspace: RealFactoryPilotWorkspace;
};

export function RealFactoryPilotPanel({ workspace }: Props) {
  return (
    <section className="state-panel" data-testid="real-factory-pilot-workspace">
      <header className="production-execution-header">
        <p>GERÇEK FABRİKA</p>
        <h2>{workspace.lineCode}</h2>
        <p>
          Faz: <strong>{workspace.currentPhase}</strong> · {workspace.acceptanceKind}
        </p>
      </header>

      <div className="production-execution-grid">
        <article className="state-panel">
          <h3>Bağlantılar</h3>
          <ul>
            {workspace.connectors.map((connector) => (
              <li key={connector.connectorId}>
                {connector.name} — {connector.healthStatus} / {connector.mappingStatus}
                <br />
                <small>{connector.eventsIngested} canonical event</small>
              </li>
            ))}
          </ul>
        </article>

        <article className="state-panel">
          <h3>Veri Kalitesi</h3>
          {workspace.dataQuality ? (
            <>
              <p>{workspace.dataQuality.overallState}</p>
              {workspace.dataQuality.blocksAutonomousAction ? (
                <p role="alert">Otonom aksiyon engelli</p>
              ) : null}
            </>
          ) : (
            <p>Henüz değerlendirilmedi</p>
          )}
        </article>

        <article className="state-panel">
          <h3>Shadow Mode</h3>
          {workspace.shadow ? (
            <>
              <p>{workspace.shadow.mode}</p>
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
          <h3>Throughput Case</h3>
          <p>{workspace.activeCaseId ?? "—"}</p>
          {workspace.evidencePack ? (
            <p>
              Doğrulama: {workspace.evidencePack.verificationOutcome}
              {workspace.evidencePack.knowledgePromoted ? " · Bilgi promote" : ""}
            </p>
          ) : null}
        </article>
      </div>
    </section>
  );
}
