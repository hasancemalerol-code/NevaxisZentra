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
  getTelemetryQualityLabel,
  getTelemetryQualityClass,
  getMetricLabel,
  getConnectivityLabel,
  getAlarmSeverityLabel,
  getAlarmSeverityClass,
  getDowntimeReasonCategoryLabel,
  formatDurationMs,
  getEdgeDeliveryStatusLabel,
  getEdgeBufferStatusLabel,
  getEdgeReplayStatusLabel,
  getEdgeBufferErrorLabel,
  formatBufferUtilization,
} from "../presentation/edge-labels";
import type {
  EdgeDeliveryHealthReadModel,
  EdgeBufferedEventReadModel,
  EdgeDeadLetterReadModel,
} from "../presentation/edge-reliability-fixtures";
import { EDGE_RELIABILITY_FIXTURE } from "../presentation/edge-reliability-fixtures";

// ---------------------------------------------------------------------------
// Types (mirrors EdgeModels.cs read models — EDGE-1 + EDGE-2 + EDGE-3)
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

// EDGE-2 types
interface LatestTelemetryReadModel {
  machineId: string;
  metricKey: string;
  numericValue?: number | null;
  textValue?: string | null;
  booleanValue?: boolean | null;
  unit?: string | null;
  quality: string;
  sourceTimestampUtc: string;
  receivedAtUtc: string;
  sourceSequence?: number | null;
  updatedAtUtc: string;
}

interface ObservedCycleReadModel {
  id: string;
  machineId: string;
  cycleIdentity: string;
  cycleDurationMs: number;
  observedCounter?: number | null;
  quality: string;
  sourceTimestampUtc: string;
  receivedAtUtc: string;
  sourceSequence?: number | null;
  idempotencyKey: string;
  isDuplicate: boolean;
  isOutOfOrder: boolean;
}

interface CycleProjectionReadModel {
  machineId: string;
  observedCycleCount: number;
  latestObservedCounter?: number | null;
  lastCycleAtUtc?: string | null;
  lastCycleDurationMs?: number | null;
  averageRecentCycleDurationMs?: number | null;
  updatedAtUtc: string;
}

interface StateTransitionReadModel {
  id: string;
  machineId: string;
  fromState: string;
  toState: string;
  reason?: string | null;
  sourceTimestampUtc: string;
  receivedAtUtc: string;
  sourceSequence?: number | null;
  isOutOfOrder: boolean;
}

interface MachineSignalProjectionReadModel {
  machineId: string;
  connectivity: string;
  observedState: string;
  lastCycleAtUtc?: string | null;
  lastCycleDurationMs?: number | null;
  observedCycleCount: number;
  latestCounter?: number | null;
  quality: string;
  lastTelemetryAtUtc?: string | null;
  // EDGE-3 observational fields (optional for backward compat)
  isInObservedDowntime?: boolean;
  currentDowntimeStartedAtUtc?: string | null;
  observedDowntimeDurationMs?: number | null;
  activeAlarmCount?: number;
  highestAlarmSeverity?: string | null;
  isInObservedSetup?: boolean;
  observedSetupStartedAtUtc?: string | null;
  observedScrapCount?: number;
  observedScrapQuantity?: number;
  lastObservedScrapAtUtc?: string | null;
  updatedAtUtc: string;
}

// EDGE-3 types
interface DowntimeEpisodeReadModel {
  id: string;
  machineId: string;
  externalEpisodeId?: string | null;
  startedAtSourceUtc: string;
  endedAtSourceUtc?: string | null;
  durationMs?: number | null;
  reasonCode?: string | null;
  reasonCategory?: string | null;
  reasonText?: string | null;
  isMicroStop: boolean;
  quality: string;
  isClosed: boolean;
  isOutOfOrder: boolean;
  startSourceSequence?: number | null;
  endSourceSequence?: number | null;
}

interface ActiveAlarmReadModel {
  id: string;
  machineId: string;
  alarmIdentity: string;
  externalAlarmCode: string;
  alarmText?: string | null;
  severity: string;
  raisedAtSourceUtc: string;
  sourceSequence?: number | null;
  quality: string;
}

interface AlarmObservationReadModel {
  id: string;
  machineId: string;
  alarmIdentity: string;
  externalAlarmCode: string;
  alarmText?: string | null;
  severity: string;
  eventKind: string;
  sourceTimestampUtc: string;
  receivedAtUtc: string;
  sourceSequence?: number | null;
  quality: string;
  isDuplicate: boolean;
  isOutOfOrder: boolean;
  isUnmatchedClear: boolean;
}

interface SetupEpisodeReadModel {
  id: string;
  machineId: string;
  externalEpisodeId?: string | null;
  setupType?: string | null;
  fromReference?: string | null;
  toReference?: string | null;
  startedAtSourceUtc: string;
  completedAtSourceUtc?: string | null;
  durationMs?: number | null;
  quality: string;
  isClosed: boolean;
  isOutOfOrder: boolean;
  startSourceSequence?: number | null;
  completeSourceSequence?: number | null;
}

interface ScrapObservationReadModel {
  id: string;
  machineId: string;
  externalObservationId: string;
  quantity: number;
  unit: string;
  reasonCode?: string | null;
  reasonCategory?: string | null;
  reasonText?: string | null;
  quality: string;
  sourceTimestampUtc: string;
  receivedAtUtc: string;
  sourceSequence?: number | null;
  isDuplicate: boolean;
  isOutOfOrder: boolean;
}

interface MachineOperationalSummaryReadModel {
  machineId: string;
  isInObservedDowntime: boolean;
  currentDowntimeStartedAtUtc?: string | null;
  observedDowntimeDurationMs?: number | null;
  observedDowntimeCount: number;
  observedDowntimeTotalDurationMs: number;
  observedMicroStopCount: number;
  activeAlarmCount: number;
  highestAlarmSeverity?: string | null;
  alarmHistoryCount: number;
  criticalAlarmCount: number;
  isInObservedSetup: boolean;
  observedSetupStartedAtUtc?: string | null;
  observedSetupCount: number;
  observedSetupTotalDurationMs: number;
  observedScrapCount: number;
  observedScrapQuantity: number;
  lastObservedScrapAtUtc?: string | null;
  updatedAtUtc?: string | null;
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

function TelemetryQualityBadge({ quality }: { quality: string }) {
  return (
    <span className={`telemetry-quality ${getTelemetryQualityClass(quality)}`}>
      {getTelemetryQualityLabel(quality)}
    </span>
  );
}

function TelemetryCard({ sample }: { sample: LatestTelemetryReadModel }) {
  const displayValue =
    sample.numericValue != null
      ? sample.numericValue.toLocaleString("tr-TR")
      : sample.textValue != null
        ? sample.textValue
        : sample.booleanValue != null
          ? sample.booleanValue
            ? "Evet"
            : "Hayır"
          : "—";

  return (
    <div className="edge-telemetry-card">
      <span className="edge-telemetry-card__metric">
        {getMetricLabel(sample.metricKey)}
      </span>
      <div style={{ display: "flex", alignItems: "baseline", gap: 4 }}>
        <span className="edge-telemetry-card__value">{displayValue}</span>
        {sample.unit && (
          <span className="edge-telemetry-card__unit">{sample.unit}</span>
        )}
      </div>
      <div
        style={{ display: "flex", alignItems: "center", gap: 6, marginTop: 4 }}
      >
        <TelemetryQualityBadge quality={sample.quality} />
      </div>
      <span className="edge-telemetry-card__meta">
        {sample.sourceSequence != null && `#${sample.sourceSequence} · `}
        {new Date(sample.sourceTimestampUtc).toLocaleTimeString("tr-TR")}
      </span>
    </div>
  );
}

function CycleRow({ cycle }: { cycle: ObservedCycleReadModel }) {
  const rowClass = [
    cycle.isDuplicate ? "edge-cycle-table__duplicate" : "",
    cycle.isOutOfOrder ? "edge-cycle-table__oor" : "",
  ]
    .filter(Boolean)
    .join(" ");

  return (
    <tr className={rowClass}>
      <td>
        <span className="edge-cycle-identity">{cycle.cycleIdentity}</span>
      </td>
      <td>{formatDurationMs(cycle.cycleDurationMs)}</td>
      <td>{cycle.observedCounter ?? "—"}</td>
      <td>{cycle.sourceSequence != null ? `#${cycle.sourceSequence}` : "—"}</td>
      <td>
        <TelemetryQualityBadge quality={cycle.quality} />
      </td>
      <td style={{ whiteSpace: "nowrap", fontSize: 11, color: "#94a3b8" }}>
        {new Date(cycle.sourceTimestampUtc).toLocaleTimeString("tr-TR")}
      </td>
    </tr>
  );
}

function StateTransitionRow({ t }: { t: StateTransitionReadModel }) {
  return (
    <div className={`edge-state-row ${t.isOutOfOrder ? "edge-state-row--oor" : ""}`}>
      <span className="edge-state-row__from">
        {t.fromState} →
      </span>
      <span className="edge-state-row__to">{t.toState}</span>
      <span className="edge-state-row__seq">
        {t.sourceSequence != null ? `#${t.sourceSequence}` : "—"}
      </span>
      <span className="edge-state-row__time">
        {new Date(t.sourceTimestampUtc).toLocaleTimeString("tr-TR")}
        {t.reason && ` · ${t.reason}`}
      </span>
    </div>
  );
}

function AlarmSeverityBadge({ severity }: { severity: string }) {
  return (
    <span className={`alarm-severity ${getAlarmSeverityClass(severity)}`}>
      {getAlarmSeverityLabel(severity)}
    </span>
  );
}

function DowntimeEpisodeRow({ episode }: { episode: DowntimeEpisodeReadModel }) {
  const reason =
    episode.reasonText ||
    episode.reasonCode ||
    "—";

  return (
    <tr className={episode.isOutOfOrder ? "edge-ops-table__oor" : undefined}>
      <td style={{ whiteSpace: "nowrap", fontSize: 11 }}>
        {new Date(episode.startedAtSourceUtc).toLocaleTimeString("tr-TR")}
      </td>
      <td style={{ whiteSpace: "nowrap", fontSize: 11 }}>
        {episode.endedAtSourceUtc
          ? new Date(episode.endedAtSourceUtc).toLocaleTimeString("tr-TR")
          : "—"}
      </td>
      <td>{formatDurationMs(episode.durationMs)}</td>
      <td>{reason}</td>
      <td>
        {episode.reasonCategory
          ? getDowntimeReasonCategoryLabel(episode.reasonCategory)
          : "—"}
      </td>
      <td>
        {episode.isMicroStop ? (
          <span className="edge-micro-stop-badge">Gözlenen mikro-duruş</span>
        ) : (
          "—"
        )}
      </td>
      <td>
        <TelemetryQualityBadge quality={episode.quality} />
      </td>
    </tr>
  );
}

function ActiveAlarmRow({ alarm }: { alarm: ActiveAlarmReadModel }) {
  return (
    <tr>
      <td>
        <span className="edge-alarm-code">{alarm.externalAlarmCode}</span>
      </td>
      <td>{alarm.alarmText ?? "—"}</td>
      <td>
        <AlarmSeverityBadge severity={alarm.severity} />
      </td>
      <td style={{ whiteSpace: "nowrap", fontSize: 11 }}>
        {new Date(alarm.raisedAtSourceUtc).toLocaleTimeString("tr-TR")}
      </td>
      <td>
        <span className="edge-alarm-status edge-alarm-status--active">Aktif</span>
      </td>
    </tr>
  );
}

function AlarmHistoryRow({ obs }: { obs: AlarmObservationReadModel }) {
  const isCleared = obs.eventKind === "machine.alarm.cleared";
  return (
    <tr
      className={[
        obs.isDuplicate ? "edge-ops-table__duplicate" : "",
        obs.isOutOfOrder ? "edge-ops-table__oor" : "",
      ]
        .filter(Boolean)
        .join(" ") || undefined}
    >
      <td>
        <span className="edge-alarm-code">{obs.externalAlarmCode}</span>
      </td>
      <td>{obs.alarmText ?? "—"}</td>
      <td>
        <AlarmSeverityBadge severity={obs.severity} />
      </td>
      <td style={{ whiteSpace: "nowrap", fontSize: 11 }}>
        {new Date(obs.sourceTimestampUtc).toLocaleTimeString("tr-TR")}
      </td>
      <td>
        <span
          className={`edge-alarm-status ${
            isCleared ? "edge-alarm-status--cleared" : "edge-alarm-status--raised"
          }`}
        >
          {isCleared ? "Temizlendi" : "Yükseltildi"}
        </span>
      </td>
    </tr>
  );
}

function SetupEpisodeRow({ episode }: { episode: SetupEpisodeReadModel }) {
  return (
    <tr className={episode.isOutOfOrder ? "edge-ops-table__oor" : undefined}>
      <td style={{ whiteSpace: "nowrap", fontSize: 11 }}>
        {new Date(episode.startedAtSourceUtc).toLocaleTimeString("tr-TR")}
      </td>
      <td style={{ whiteSpace: "nowrap", fontSize: 11 }}>
        {episode.completedAtSourceUtc
          ? new Date(episode.completedAtSourceUtc).toLocaleTimeString("tr-TR")
          : "—"}
      </td>
      <td>{formatDurationMs(episode.durationMs)}</td>
      <td>{episode.fromReference ?? episode.setupType ?? "—"}</td>
      <td>{episode.toReference ?? "—"}</td>
      <td>
        <TelemetryQualityBadge quality={episode.quality} />
      </td>
    </tr>
  );
}

function ScrapObservationRow({ scrap }: { scrap: ScrapObservationReadModel }) {
  const reason =
    scrap.reasonText ||
    scrap.reasonCode ||
    "—";

  return (
    <tr
      className={[
        scrap.isDuplicate ? "edge-ops-table__duplicate" : "",
        scrap.isOutOfOrder ? "edge-ops-table__oor" : "",
      ]
        .filter(Boolean)
        .join(" ") || undefined}
    >
      <td className="edge-scrap-qty">
        {scrap.quantity.toLocaleString("tr-TR")}
      </td>
      <td>{scrap.unit}</td>
      <td>{reason}</td>
      <td style={{ whiteSpace: "nowrap", fontSize: 11 }}>
        {new Date(scrap.sourceTimestampUtc).toLocaleTimeString("tr-TR")}
      </td>
      <td>
        <TelemetryQualityBadge quality={scrap.quality} />
      </td>
    </tr>
  );
}

function BufferedEventRow({ event }: { event: EdgeBufferedEventReadModel }) {
  return (
    <tr>
      <td>{getMachineEventKindLabel(event.eventType)}</td>
      <td>{event.machineCode}</td>
      <td style={{ whiteSpace: "nowrap", fontSize: 11 }}>
        {new Date(event.sourceTimestampUtc).toLocaleTimeString("tr-TR")}
      </td>
      <td style={{ whiteSpace: "nowrap", fontSize: 11 }}>
        {new Date(event.queueTimeUtc).toLocaleTimeString("tr-TR")}
      </td>
      <td>{event.attempts}</td>
      <td>
        <span className={`edge-buffer-status edge-buffer-status--${event.status.replace(/[^a-z]/g, "")}`}>
          {getEdgeBufferStatusLabel(event.status)}
        </span>
      </td>
      <td style={{ fontSize: 11, color: "#64748b" }}>
        {getEdgeBufferErrorLabel(event.lastError)}
      </td>
    </tr>
  );
}

function DeadLetterRow({ record }: { record: EdgeDeadLetterReadModel }) {
  return (
    <tr>
      <td>{getMachineEventKindLabel(record.eventType)}</td>
      <td>{record.machineCode}</td>
      <td style={{ fontSize: 11, color: "#64748b" }}>
        {getEdgeBufferErrorLabel(record.reason)}
      </td>
      <td>{record.attempts}</td>
      <td style={{ whiteSpace: "nowrap", fontSize: 11 }}>
        {record.lastFailureAtUtc
          ? new Date(record.lastFailureAtUtc).toLocaleTimeString("tr-TR")
          : "—"}
      </td>
    </tr>
  );
}

// ---------------------------------------------------------------------------
// Main workspace view
// ---------------------------------------------------------------------------

interface EdgeWorkspaceViewProps {
  gateways: EdgeGatewayReadModel[];
  machines: MachineRegistryReadModel[];
  recentEvents?: MachineEventReadModel[];
  latestTelemetry?: LatestTelemetryReadModel[];
  cycleProjection?: CycleProjectionReadModel | null;
  cycleHistory?: ObservedCycleReadModel[];
  signalProjection?: MachineSignalProjectionReadModel | null;
  stateTransitions?: StateTransitionReadModel[];
  // EDGE-3 (optional, default empty)
  downtimeEpisodes?: DowntimeEpisodeReadModel[];
  activeAlarms?: ActiveAlarmReadModel[];
  alarmHistory?: AlarmObservationReadModel[];
  setupEpisodes?: SetupEpisodeReadModel[];
  scrapObservations?: ScrapObservationReadModel[];
  operationalSummary?: MachineOperationalSummaryReadModel | null;
  // EDGE-4 (optional, defaults to fixture for premium UI)
  deliveryHealth?: EdgeDeliveryHealthReadModel | null;
  bufferedEvents?: EdgeBufferedEventReadModel[];
  deadLetters?: EdgeDeadLetterReadModel[];
  offlineMessage?: string | null;
  replayMessage?: string | null;
}

export function EdgeWorkspaceView({
  gateways,
  machines,
  recentEvents = [],
  latestTelemetry = [],
  cycleProjection = null,
  cycleHistory = [],
  signalProjection = null,
  stateTransitions = [],
  downtimeEpisodes = [],
  activeAlarms = [],
  alarmHistory = [],
  setupEpisodes = [],
  scrapObservations = [],
  operationalSummary = null,
  deliveryHealth = EDGE_RELIABILITY_FIXTURE.deliveryHealth,
  bufferedEvents = EDGE_RELIABILITY_FIXTURE.bufferedEvents,
  deadLetters = EDGE_RELIABILITY_FIXTURE.deadLetters,
  offlineMessage = EDGE_RELIABILITY_FIXTURE.offlineMessage,
  replayMessage = EDGE_RELIABILITY_FIXTURE.replayMessage,
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

      {/* EDGE-2: Canlı Telemetri */}
      {latestTelemetry.length > 0 && (
        <section>
          <h2 className="edge-section-title">Canlı Telemetri</h2>
          <p className="edge-simulator-disclaimer" style={{ marginBottom: 8 }}>
            Simülatör verisi — gerçek makine bağlantısı değildir.
          </p>
          <div className="edge-telemetry-grid">
            {latestTelemetry.map((t) => (
              <TelemetryCard key={`${t.machineId}-${t.metricKey}`} sample={t} />
            ))}
          </div>
        </section>
      )}

      {/* EDGE-2: Çevrim Bilgileri */}
      {(cycleProjection || cycleHistory.length > 0) && (
        <section>
          <h2 className="edge-section-title">
            Çevrim Bilgileri
            <span
              style={{
                marginLeft: 8,
                fontSize: 11,
                fontWeight: 400,
                color: "#92400e",
              }}
            >
              Gözlenen — resmi üretim miktarı değildir
            </span>
          </h2>

          {cycleProjection && (
            <div className="edge-cycle-grid" style={{ marginBottom: 12 }}>
              <div className="edge-cycle-kpi edge-cycle-kpi--observed">
                <span className="edge-cycle-kpi__label">
                  Gözlenen Çevrim Sayısı
                </span>
                <span className="edge-cycle-kpi__value">
                  {cycleProjection.observedCycleCount}
                </span>
              </div>
              <div className="edge-cycle-kpi">
                <span className="edge-cycle-kpi__label">Son Çevrim Süresi</span>
                <span className="edge-cycle-kpi__value">
                  {formatDurationMs(cycleProjection.lastCycleDurationMs)}
                </span>
              </div>
              <div className="edge-cycle-kpi">
                <span className="edge-cycle-kpi__label">Ort. Çevrim Süresi</span>
                <span className="edge-cycle-kpi__value">
                  {formatDurationMs(cycleProjection.averageRecentCycleDurationMs)}
                </span>
              </div>
              <div className="edge-cycle-kpi">
                <span className="edge-cycle-kpi__label">Son Sayaç</span>
                <span className="edge-cycle-kpi__value">
                  {cycleProjection.latestObservedCounter ?? "—"}
                </span>
              </div>
            </div>
          )}

          {cycleHistory.length > 0 && (
            <div style={{ overflowX: "auto" }}>
              <table className="edge-cycle-table">
                <thead>
                  <tr>
                    <th>Çevrim Kimliği</th>
                    <th>Süre</th>
                    <th>Sayaç</th>
                    <th>Sıra No.</th>
                    <th>Kalite</th>
                    <th>Kaynak Zamanı</th>
                  </tr>
                </thead>
                <tbody>
                  {cycleHistory.slice(0, 10).map((c) => (
                    <CycleRow key={c.id} cycle={c} />
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>
      )}

      {/* EDGE-2: Makine Sinyalleri */}
      {signalProjection && (
        <section>
          <h2 className="edge-section-title">Makine Sinyalleri</h2>
          <div className="edge-signal-card">
            <div className="edge-signal-grid">
              <div className="edge-signal-item">
                <span className="edge-signal-item__label">Bağlantı</span>
                <span className="edge-signal-item__value">
                  {getConnectivityLabel(signalProjection.connectivity)}
                </span>
              </div>
              <div className="edge-signal-item">
                <span className="edge-signal-item__label">Gözlenen Durum</span>
                <span className="edge-signal-item__value">
                  {signalProjection.observedState}
                </span>
              </div>
              <div className="edge-signal-item">
                <span className="edge-signal-item__label">
                  Makineden Gözlenen Çevrim
                </span>
                <span className="edge-signal-item__value">
                  {signalProjection.observedCycleCount}
                </span>
              </div>
              <div className="edge-signal-item">
                <span className="edge-signal-item__label">Son Çevrim</span>
                <span className="edge-signal-item__value">
                  {formatDurationMs(signalProjection.lastCycleDurationMs)}
                </span>
              </div>
              <div className="edge-signal-item">
                <span className="edge-signal-item__label">Son Sayaç</span>
                <span className="edge-signal-item__value">
                  {signalProjection.latestCounter ?? "—"}
                </span>
              </div>
              <div className="edge-signal-item">
                <span className="edge-signal-item__label">Veri Kalitesi</span>
                <span className="edge-signal-item__value">
                  <TelemetryQualityBadge quality={signalProjection.quality} />
                </span>
              </div>
              {signalProjection.isInObservedDowntime != null && (
                <div className="edge-signal-item">
                  <span className="edge-signal-item__label">
                    Gözlenen Duruşta
                  </span>
                  <span className="edge-signal-item__value">
                    {signalProjection.isInObservedDowntime ? "Evet" : "Hayır"}
                  </span>
                </div>
              )}
              {signalProjection.activeAlarmCount != null && (
                <div className="edge-signal-item">
                  <span className="edge-signal-item__label">Aktif Alarm</span>
                  <span className="edge-signal-item__value">
                    {signalProjection.activeAlarmCount}
                    {signalProjection.highestAlarmSeverity && (
                      <>
                        {" · "}
                        <AlarmSeverityBadge
                          severity={signalProjection.highestAlarmSeverity}
                        />
                      </>
                    )}
                  </span>
                </div>
              )}
              {signalProjection.isInObservedSetup != null && (
                <div className="edge-signal-item">
                  <span className="edge-signal-item__label">
                    Gözlenen Setup&apos;ta
                  </span>
                  <span className="edge-signal-item__value">
                    {signalProjection.isInObservedSetup ? "Evet" : "Hayır"}
                  </span>
                </div>
              )}
              {signalProjection.observedScrapCount != null && (
                <div className="edge-signal-item">
                  <span className="edge-signal-item__label">
                    Makineden Gözlenen Fire
                  </span>
                  <span className="edge-signal-item__value">
                    {signalProjection.observedScrapCount}
                    {signalProjection.observedScrapQuantity != null &&
                      ` · ${signalProjection.observedScrapQuantity.toLocaleString("tr-TR")}`}
                  </span>
                </div>
              )}
            </div>
            <p className="edge-signal-disclaimer">
              Salt okunur sinyal projeksiyonu — Üretim mutasyonu yoktur. Bu
              değerler gözlemdir, resmi üretim miktarı değildir.
            </p>
          </div>
        </section>
      )}

      {/* EDGE-2: Durum Geçmişi */}
      {stateTransitions.length > 0 && (
        <section>
          <h2 className="edge-section-title">Durum Geçmişi</h2>
          <div>
            {stateTransitions.slice(0, 10).map((t) => (
              <StateTransitionRow key={t.id} t={t} />
            ))}
          </div>
        </section>
      )}

      {/* EDGE-3: Operational summary cards */}
      {operationalSummary && (
        <section>
          <h2 className="edge-section-title">Operasyonel Özet (Gözlenen)</h2>
          <p className="edge-simulator-disclaimer" style={{ marginBottom: 8 }}>
            Simülatör verisi — gerçek makine bağlantısı değildir.
          </p>
          <div className="edge-ops-summary-grid">
            <div className="edge-ops-summary-card">
              <span className="edge-ops-summary-card__label">Gözlenen Duruş</span>
              <span className="edge-ops-summary-card__value">
                {operationalSummary.isInObservedDowntime ? "Aktif" : "Yok"}
              </span>
              <span className="edge-ops-summary-card__meta">
                {operationalSummary.observedDowntimeCount} kayıt
                {operationalSummary.observedMicroStopCount > 0 &&
                  ` · ${operationalSummary.observedMicroStopCount} mikro-duruş`}
              </span>
            </div>
            <div className="edge-ops-summary-card">
              <span className="edge-ops-summary-card__label">Aktif Alarm</span>
              <span className="edge-ops-summary-card__value">
                {operationalSummary.activeAlarmCount}
              </span>
              <span className="edge-ops-summary-card__meta">
                {operationalSummary.highestAlarmSeverity
                  ? getAlarmSeverityLabel(operationalSummary.highestAlarmSeverity)
                  : "—"}
              </span>
            </div>
            <div className="edge-ops-summary-card">
              <span className="edge-ops-summary-card__label">Gözlenen Setup</span>
              <span className="edge-ops-summary-card__value">
                {operationalSummary.isInObservedSetup ? "Aktif" : "Yok"}
              </span>
              <span className="edge-ops-summary-card__meta">
                {operationalSummary.observedSetupCount} kayıt
              </span>
            </div>
            <div className="edge-ops-summary-card edge-ops-summary-card--scrap">
              <span className="edge-ops-summary-card__label">
                Makineden Gözlenen Fire
              </span>
              <span className="edge-ops-summary-card__value">
                {operationalSummary.observedScrapQuantity.toLocaleString("tr-TR")}
              </span>
              <span className="edge-ops-summary-card__meta">
                {operationalSummary.observedScrapCount} gözlem
              </span>
            </div>
          </div>
        </section>
      )}

      {/* EDGE-3: Gözlenen Duruşlar */}
      {downtimeEpisodes.length > 0 && (
        <section>
          <h2 className="edge-section-title">Gözlenen Duruşlar</h2>
          <p className="edge-simulator-disclaimer" style={{ marginBottom: 8 }}>
            Simülatör verisi — gerçek makine bağlantısı değildir.
          </p>
          <div style={{ overflowX: "auto" }}>
            <table className="edge-ops-table">
              <thead>
                <tr>
                  <th>Start</th>
                  <th>End</th>
                  <th>Duration</th>
                  <th>Reason</th>
                  <th>Category</th>
                  <th>Micro-stop</th>
                  <th>Quality</th>
                </tr>
              </thead>
              <tbody>
                {downtimeEpisodes.slice(0, 15).map((e) => (
                  <DowntimeEpisodeRow key={e.id} episode={e} />
                ))}
              </tbody>
            </table>
          </div>
        </section>
      )}

      {/* EDGE-3: Aktif Alarmlar */}
      {activeAlarms.length > 0 && (
        <section>
          <h2 className="edge-section-title">Aktif Alarmlar</h2>
          <div style={{ overflowX: "auto" }}>
            <table className="edge-ops-table">
              <thead>
                <tr>
                  <th>Code</th>
                  <th>Text</th>
                  <th>Severity</th>
                  <th>Raised</th>
                  <th>Status</th>
                </tr>
              </thead>
              <tbody>
                {activeAlarms.map((a) => (
                  <ActiveAlarmRow key={a.id} alarm={a} />
                ))}
              </tbody>
            </table>
          </div>
        </section>
      )}

      {/* EDGE-3: Alarm Geçmişi */}
      {alarmHistory.length > 0 && (
        <section>
          <h2 className="edge-section-title">Alarm Geçmişi</h2>
          <div style={{ overflowX: "auto" }}>
            <table className="edge-ops-table">
              <thead>
                <tr>
                  <th>Code</th>
                  <th>Text</th>
                  <th>Severity</th>
                  <th>Raised</th>
                  <th>Cleared/Status</th>
                </tr>
              </thead>
              <tbody>
                {alarmHistory.slice(0, 15).map((o) => (
                  <AlarmHistoryRow key={o.id} obs={o} />
                ))}
              </tbody>
            </table>
          </div>
        </section>
      )}

      {/* EDGE-3: Gözlenen Setup / Değişimler */}
      {setupEpisodes.length > 0 && (
        <section>
          <h2 className="edge-section-title">Gözlenen Setup / Değişimler</h2>
          <div style={{ overflowX: "auto" }}>
            <table className="edge-ops-table">
              <thead>
                <tr>
                  <th>Start</th>
                  <th>Complete</th>
                  <th>Duration</th>
                  <th>From</th>
                  <th>To</th>
                  <th>Quality</th>
                </tr>
              </thead>
              <tbody>
                {setupEpisodes.slice(0, 15).map((e) => (
                  <SetupEpisodeRow key={e.id} episode={e} />
                ))}
              </tbody>
            </table>
          </div>
        </section>
      )}

      {/* EDGE-3: Makineden Gözlenen Fire */}
      {scrapObservations.length > 0 && (
        <section>
          <h2 className="edge-section-title">Makineden Gözlenen Fire</h2>
          <p className="edge-scrap-disclaimer">
            Bu değer makine gözlemidir; resmi üretim/kalite kaydı değildir.
          </p>
          <div style={{ overflowX: "auto" }}>
            <table className="edge-ops-table">
              <thead>
                <tr>
                  <th>Quantity</th>
                  <th>Unit</th>
                  <th>Reason</th>
                  <th>Time</th>
                  <th>Quality</th>
                </tr>
              </thead>
              <tbody>
                {scrapObservations.slice(0, 15).map((s) => (
                  <ScrapObservationRow key={s.id} scrap={s} />
                ))}
              </tbody>
            </table>
          </div>
        </section>
      )}

      {/* EDGE-4: Gateway Delivery / Edge Reliability */}
      {deliveryHealth && (
        <section className="edge-reliability-section">
          <h2 className="edge-section-title">Gateway Delivery / Edge Reliability</h2>
          <p className="edge-simulator-disclaimer" style={{ marginBottom: 8 }}>
            Simülatör verisi — gerçek makine bağlantısı değildir.
          </p>

          {!deliveryHealth.apiConnected && offlineMessage && (
            <div className="edge-reliability-banner edge-reliability-banner--offline">
              {offlineMessage}
            </div>
          )}
          {deliveryHealth.apiConnected &&
            deliveryHealth.replayStatus === "replaying" &&
            replayMessage && (
              <div className="edge-reliability-banner edge-reliability-banner--replay">
                {replayMessage}
              </div>
            )}

          <div className="edge-reliability-kpi-grid">
            <div className="edge-reliability-kpi">
              <span className="edge-reliability-kpi__label">Bekleyen Olay</span>
              <span className="edge-reliability-kpi__value">
                {deliveryHealth.pendingCount}
              </span>
            </div>
            <div className="edge-reliability-kpi">
              <span className="edge-reliability-kpi__label">Yeniden Deneme</span>
              <span className="edge-reliability-kpi__value">
                {deliveryHealth.retryCount}
              </span>
            </div>
            <div className="edge-reliability-kpi">
              <span className="edge-reliability-kpi__label">Dead Letter</span>
              <span className="edge-reliability-kpi__value">
                {deliveryHealth.deadLetterCount}
              </span>
            </div>
            <div className="edge-reliability-kpi">
              <span className="edge-reliability-kpi__label">Son Başarılı Teslim</span>
              <span className="edge-reliability-kpi__value edge-reliability-kpi__value--sm">
                {deliveryHealth.lastSuccessfulDeliveryAtUtc
                  ? new Date(deliveryHealth.lastSuccessfulDeliveryAtUtc).toLocaleTimeString("tr-TR")
                  : "—"}
              </span>
            </div>
            <div className="edge-reliability-kpi">
              <span className="edge-reliability-kpi__label">Kuyruk Kullanımı</span>
              <span className="edge-reliability-kpi__value">
                {formatBufferUtilization(deliveryHealth.bufferUtilization)}
              </span>
            </div>
          </div>

          <div className="edge-reliability-status-card">
            <div className="edge-reliability-status-grid">
              <div className="edge-reliability-status-item">
                <span className="edge-reliability-status-item__label">API Bağlantısı</span>
                <span className={`edge-reliability-status-item__value ${deliveryHealth.apiConnected ? "edge-reliability-status-item__value--ok" : "edge-reliability-status-item__value--warn"}`}>
                  {deliveryHealth.apiConnected ? "Bağlı" : "Kesik"}
                </span>
              </div>
              <div className="edge-reliability-status-item">
                <span className="edge-reliability-status-item__label">Teslimat Durumu</span>
                <span className="edge-reliability-status-item__value">
                  {getEdgeDeliveryStatusLabel(deliveryHealth.deliveryStatus)}
                </span>
              </div>
              <div className="edge-reliability-status-item">
                <span className="edge-reliability-status-item__label">Bekleyen</span>
                <span className="edge-reliability-status-item__value">
                  {deliveryHealth.pendingCount + deliveryHealth.retryCount}
                </span>
              </div>
              <div className="edge-reliability-status-item">
                <span className="edge-reliability-status-item__label">En Eski Bekleyen</span>
                <span className="edge-reliability-status-item__value">
                  {deliveryHealth.oldestPendingAgeMs != null
                    ? formatDurationMs(deliveryHealth.oldestPendingAgeMs)
                    : "—"}
                </span>
              </div>
              <div className="edge-reliability-status-item">
                <span className="edge-reliability-status-item__label">Son Deneme</span>
                <span className="edge-reliability-status-item__value edge-reliability-status-item__value--sm">
                  {deliveryHealth.lastDeliveryAttemptAtUtc
                    ? new Date(deliveryHealth.lastDeliveryAttemptAtUtc).toLocaleTimeString("tr-TR")
                    : "—"}
                </span>
              </div>
              <div className="edge-reliability-status-item">
                <span className="edge-reliability-status-item__label">Son Başarılı Teslim</span>
                <span className="edge-reliability-status-item__value edge-reliability-status-item__value--sm">
                  {deliveryHealth.lastSuccessfulDeliveryAtUtc
                    ? new Date(deliveryHealth.lastSuccessfulDeliveryAtUtc).toLocaleTimeString("tr-TR")
                    : "—"}
                </span>
              </div>
              <div className="edge-reliability-status-item">
                <span className="edge-reliability-status-item__label">Replay</span>
                <span className="edge-reliability-status-item__value">
                  {getEdgeReplayStatusLabel(deliveryHealth.replayStatus)}
                </span>
              </div>
            </div>
          </div>

          {bufferedEvents.length > 0 && (
            <div style={{ marginTop: 12, overflowX: "auto" }}>
              <h3 className="edge-reliability-subtitle">Tamponlanmış Olaylar</h3>
              <table className="edge-reliability-table">
                <thead>
                  <tr>
                    <th>Olay Türü</th>
                    <th>Makine</th>
                    <th>Kaynak Zamanı</th>
                    <th>Kuyruk Zamanı</th>
                    <th>Deneme</th>
                    <th>Durum</th>
                    <th>Son Hata</th>
                  </tr>
                </thead>
                <tbody>
                  {bufferedEvents.slice(0, 20).map((e, i) => (
                    <BufferedEventRow key={`${e.machineCode}-${e.sourceTimestampUtc}-${i}`} event={e} />
                  ))}
                </tbody>
              </table>
            </div>
          )}

          {deadLetters.length > 0 && (
            <div style={{ marginTop: 12, overflowX: "auto" }}>
              <h3 className="edge-reliability-subtitle">Dead Letter (Salt Okunur)</h3>
              <table className="edge-reliability-table edge-reliability-table--dead-letter">
                <thead>
                  <tr>
                    <th>Olay</th>
                    <th>Makine</th>
                    <th>Neden</th>
                    <th>Deneme</th>
                    <th>Son Hata</th>
                  </tr>
                </thead>
                <tbody>
                  {deadLetters.map((d, i) => (
                    <DeadLetterRow key={`${d.machineCode}-${d.eventType}-${i}`} record={d} />
                  ))}
                </tbody>
              </table>
            </div>
          )}
        </section>
      )}

      {/* Capabilities */}
      <section>
        <h2 className="edge-section-title">EDGE-1 + EDGE-2 + EDGE-3 + EDGE-4 Yetenekleri</h2>
        <ul className="edge-capabilities">
          {EDGE_CAPABILITIES.map((cap) => (
            <li key={cap}>{cap}</li>
          ))}
        </ul>
      </section>

      {/* Simulator note */}
      <div className="edge-simulator-disclaimer">
        <strong>Simülatör modu aktif:</strong> Bu görünüm deterministik test
        verilerini kullanır. Gerçek makine bağlantısı (MQTT, OPC UA) EDGE-1/EDGE-2/EDGE-3/EDGE-4
        kapsamı dışındadır. Makine yazma/kontrol komutları gönderilmez.
        Gözlenen Çevrim sayısı resmi Üretim adedi değildir. Gözlenen duruş, alarm,
        setup ve fire değerleri observed-only&apos;dir; resmi üretim/kalite kaydı değildir.
        EDGE-4 yerel store-and-forward tamponu simülatör yerel dosyası ile modellenir — gerçek fabrika ağı doğrulanmamıştır.
      </div>
    </div>
  );
}
