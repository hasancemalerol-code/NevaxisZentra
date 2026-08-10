"use client";

import "./edge-workspace.css";
import {
  EDGE_CAPABILITIES,
  getEdgeGatewayStatusClass,
  getEdgeGatewayStatusLabel,
  getEdgeGatewayVendorLabel,
  getMachineConnectivityLabel,
  getMachineEventKindLabel,
  getMachineReadinessLabel,
  getMachineStatusClass,
  getMachineStatusLabel,
  getEdgeSimulatorDisclaimer,
} from "../presentation/edge-labels";

// ---------------------------------------------------------------------------
// Types (mirrors EdgeModels.cs read models)
// ---------------------------------------------------------------------------

interface EdgeGatewayReadModel {
  id: string;
  facilityId: string;
  gatewayCode: string;
  name: string;
  description?: string | null;
  vendorKey: string;
  status: string;
  lastSeenAtUtc?: string | null;
  lastHeartbeatAtUtc?: string | null;
  lastHealthMessage?: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
  machineCount: number;
}

interface MachineRegistryReadModel {
  id: string;
  facilityId: string;
  machineCode: string;
  name: string;
  description?: string | null;
  assetClass?: string | null;
  assetReference?: string | null;
  status: string;
  isReadyForProduction: boolean;
  lastSeenAtUtc?: string | null;
  lastEventAtUtc?: string | null;
  lastKnownStatus?: string | null;
  lastKnownStatusAtUtc?: string | null;
  createdAtUtc: string;
  updatedAtUtc: string;
  activeGatewayId?: string | null;
  activeGatewayCode?: string | null;
}

interface MachineEventReadModel {
  id: string;
  machineId: string;
  gatewayId: string;
  eventKind: string;
  sourceTimestampUtc: string;
  receivedAtUtc: string;
  sourceSequence: number;
  idempotencyKey: string;
  payload?: string | null;
  isDuplicate: boolean;
  duplicateOfIdempotencyKey?: string | null;
}

// ---------------------------------------------------------------------------
// Sub-components
// ---------------------------------------------------------------------------

function EdgeStatusBadge({ status }: { status: string }) {
  return (
    <span className={`edge-status ${getEdgeGatewayStatusClass(status)}`}>
      {getEdgeGatewayStatusLabel(status)}
    </span>
  );
}

function MachineStatusBadge({ status }: { status: string }) {
  return (
    <span className={`edge-status ${getMachineStatusClass(status)}`}>
      {getMachineStatusLabel(status)}
    </span>
  );
}

function GatewayCard({ gateway }: { gateway: EdgeGatewayReadModel }) {
  return (
    <div className="edge-card">
      <div className="edge-card__header">
        <span className="edge-card__code">{gateway.gatewayCode}</span>
        <EdgeStatusBadge status={gateway.status} />
      </div>
      <p className="edge-card__name">{gateway.name}</p>
      <div className="edge-card__meta">
        <span>{getEdgeGatewayVendorLabel(gateway.vendorKey)}</span>
        <span>·</span>
        <span>{gateway.machineCount} makine</span>
        {gateway.lastSeenAtUtc && (
          <>
            <span>·</span>
            <span>{getMachineConnectivityLabel(gateway.lastSeenAtUtc)}</span>
          </>
        )}
      </div>
      {gateway.lastHealthMessage && (
        <p className="edge-card__meta">{gateway.lastHealthMessage}</p>
      )}
      {gateway.vendorKey === "simulator" && (
        <p className="edge-simulator-disclaimer" style={{ marginTop: 4 }}>
          {getEdgeSimulatorDisclaimer()}
        </p>
      )}
    </div>
  );
}

function MachineCard({ machine }: { machine: MachineRegistryReadModel }) {
  return (
    <div className="edge-card">
      <div className="edge-card__header">
        <span className="edge-card__code">{machine.machineCode}</span>
        <MachineStatusBadge status={machine.status} />
      </div>
      <p className="edge-card__name">{machine.name}</p>
      <div className="edge-card__meta">
        {machine.assetClass && <span>{machine.assetClass}</span>}
        {machine.activeGatewayCode && (
          <>
            <span>·</span>
            <span>GW: {machine.activeGatewayCode}</span>
          </>
        )}
        <span>·</span>
        <span
          style={{
            color: machine.isReadyForProduction ? "#15803d" : "#b91c1c",
            fontWeight: 600,
          }}
        >
          {getMachineReadinessLabel(machine.isReadyForProduction)}
        </span>
      </div>
      {machine.lastSeenAtUtc && (
        <div className="edge-card__meta">
          {getMachineConnectivityLabel(machine.lastSeenAtUtc)}
        </div>
      )}
    </div>
  );
}

function MachineEventRow({ event }: { event: MachineEventReadModel }) {
  const rowClass = [
    "edge-event-row",
    event.isDuplicate ? "edge-event-row--duplicate" : "",
  ]
    .filter(Boolean)
    .join(" ");

  return (
    <div className={rowClass}>
      <span className="edge-event__seq">#{event.sourceSequence}</span>
      <span className="edge-event__kind">
        {getMachineEventKindLabel(event.eventKind)}
      </span>
      <span className="edge-card__meta" style={{ overflow: "hidden", textOverflow: "ellipsis", whiteSpace: "nowrap" }}>
        {event.idempotencyKey}
        {event.isDuplicate && " (yinelenen)"}
      </span>
      <span className="edge-event__time">
        {new Date(event.sourceTimestampUtc).toLocaleTimeString("tr-TR")}
      </span>
    </div>
  );
}

// ---------------------------------------------------------------------------
// Main workspace view
// ---------------------------------------------------------------------------

interface EdgeWorkspaceViewProps {
  gateways: EdgeGatewayReadModel[];
  machines: MachineRegistryReadModel[];
  recentEvents?: MachineEventReadModel[];
}

export function EdgeWorkspaceView({
  gateways,
  machines,
  recentEvents = [],
}: EdgeWorkspaceViewProps) {
  const onlineMachines = machines.filter((m) => m.isReadyForProduction).length;
  const totalEvents = recentEvents.length;
  const duplicateEvents = recentEvents.filter((e) => e.isDuplicate).length;

  return (
    <div className="edge-workspace-page">
      {/* Header */}
      <div className="edge-workspace-header">
        <div className="edge-header__intro">
          <h1>Makineler & Edge Gateway</h1>
          <p className="edge-header__subtitle">
            Tesis makineleri, gateway kayıtları ve telemetri izleme. Gerçek
            PLC/makine bağlantısı yok (EDGE-1 foundation).
          </p>
        </div>
      </div>

      {/* KPI strip */}
      <div className="edge-kpi-grid">
        <div className="edge-kpi">
          <div className="edge-kpi__content">
            <span className="edge-kpi__label">Kayıtlı Makine</span>
            <span className="edge-kpi__value">{machines.length}</span>
          </div>
        </div>
        <div className="edge-kpi">
          <div className="edge-kpi__content">
            <span className="edge-kpi__label">Üretime Hazır</span>
            <span className="edge-kpi__value">{onlineMachines}</span>
          </div>
        </div>
        <div className="edge-kpi">
          <div className="edge-kpi__content">
            <span className="edge-kpi__label">Edge Gateway</span>
            <span className="edge-kpi__value">{gateways.length}</span>
          </div>
        </div>
        <div className="edge-kpi">
          <div className="edge-kpi__content">
            <span className="edge-kpi__label">Son Olaylar</span>
            <span className="edge-kpi__value">{totalEvents}</span>
          </div>
        </div>
      </div>

      {/* Gateways */}
      {gateways.length > 0 && (
        <section>
          <h2 className="edge-section-title">Edge Gateway&apos;ler</h2>
          <div className="edge-card-grid">
            {gateways.map((g) => (
              <GatewayCard key={g.id} gateway={g} />
            ))}
          </div>
        </section>
      )}

      {/* Machines */}
      {machines.length > 0 && (
        <section>
          <h2 className="edge-section-title">Makine Kayıt Defteri</h2>
          <div className="edge-card-grid">
            {machines.map((m) => (
              <MachineCard key={m.id} machine={m} />
            ))}
          </div>
        </section>
      )}

      {/* Recent events */}
      {recentEvents.length > 0 && (
        <section>
          <h2 className="edge-section-title">
            Son Olaylar
            {duplicateEvents > 0 && (
              <span
                style={{
                  marginLeft: 8,
                  fontSize: 12,
                  fontWeight: 400,
                  color: "#b91c1c",
                }}
              >
                ({duplicateEvents} yinelenen reddedildi)
              </span>
            )}
          </h2>
          <div className="edge-events-list">
            {recentEvents.map((e) => (
              <MachineEventRow key={e.id} event={e} />
            ))}
          </div>
        </section>
      )}

      {/* Capabilities */}
      <section>
        <h2 className="edge-section-title">EDGE-1 Yetenekleri</h2>
        <ul className="edge-capabilities">
          {EDGE_CAPABILITIES.map((cap) => (
            <li key={cap}>{cap}</li>
          ))}
        </ul>
      </section>

      {/* Simulator note */}
      <div className="edge-simulator-disclaimer">
        <strong>Simülatör modu aktif:</strong> Bu görünüm deterministik test
        verilerini kullanır. Gerçek makine bağlantısı (MQTT, OPC UA) EDGE-1
        kapsamı dışındadır. Makine yazma/kontrol komutları gönderilmez.
      </div>
    </div>
  );
}
