"use client";

import { useMemo, useState } from "react";
import "./edge-workspace.css";
import { EdgeExecutiveKpis } from "./edge-executive-kpis";
import { EdgeGatewayWorkspace } from "./edge-gateway-workspace";
import { EdgeMachineDetailPanel } from "./edge-machine-detail-panel";
import { EdgeEventTimeline, EdgeMachineTable } from "./edge-machine-table";
import { EdgeOperationalAttention } from "./edge-operational-attention";
import {
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
  getAlarmSeverityLabel,
  getAlarmSeverityClass,
  getDowntimeReasonCategoryLabel,
  formatDurationMs,
  getEdgeBufferStatusLabel,
  getEdgeBufferErrorLabel,
} from "../presentation/edge-labels";
import type {
  EdgeDeliveryHealthReadModel,
  EdgeBufferedEventReadModel,
  EdgeDeadLetterReadModel,
} from "../presentation/edge-reliability-fixtures";
import { EDGE_RELIABILITY_FIXTURE } from "../presentation/edge-reliability-fixtures";
import type { EdgeMachineDetailBundle } from "../domain/edge-types";

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
// Sub-components (retained for EDGE capability / regression; not on landing)
// ---------------------------------------------------------------------------

/* eslint-disable @typescript-eslint/no-unused-vars -- legacy EDGE presentation components */

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

/**
 * EDGE capability anchors (landing duplicates removed — capability in machine detail):
 * Edge Gateway · Makine Kayıt Defteri · Canlı Telemetri · Gözlenen Duruşlar · Son Olaylar
 * Gateway Delivery / Edge Reliability · Çevrim Bilgileri · Makine Sinyalleri · Durum Geçmişi
 */
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

/* eslint-enable @typescript-eslint/no-unused-vars */

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
  deliveryHealth?: EdgeDeliveryHealthReadModel;
  bufferedEvents?: EdgeBufferedEventReadModel[];
  deadLetters?: EdgeDeadLetterReadModel[];
  offlineMessage?: string | null;
  replayMessage?: string | null;
  // EDGE-UI-1 workspace bundle
  machineDetails?: Record<string, EdgeMachineDetailBundle>;
  isSimulatorFixture?: boolean;
  loadError?: string | null;
}

export function EdgeWorkspaceView({
  gateways,
  machines,
  recentEvents = [],
  deliveryHealth = EDGE_RELIABILITY_FIXTURE.deliveryHealth,
  bufferedEvents = EDGE_RELIABILITY_FIXTURE.bufferedEvents,
  deadLetters = EDGE_RELIABILITY_FIXTURE.deadLetters,
  offlineMessage = EDGE_RELIABILITY_FIXTURE.offlineMessage,
  replayMessage = EDGE_RELIABILITY_FIXTURE.replayMessage,
  machineDetails = {},
  isSimulatorFixture = true,
  loadError = null,
}: EdgeWorkspaceViewProps) {
  const [selectedMachineId, setSelectedMachineId] = useState<string | null>(null);

  const activeDetail = selectedMachineId ? machineDetails[selectedMachineId] : undefined;
  const activeMachine = selectedMachineId
    ? machines.find((m) => m.id === selectedMachineId)
    : undefined;

  const machineCodeById = useMemo(
    () => Object.fromEntries(machines.map((m) => [m.id, m.machineCode])),
    [machines],
  );

  const resolvedDeliveryHealth =
    deliveryHealth ?? EDGE_RELIABILITY_FIXTURE.deliveryHealth;

  return (
    <div className="edge-workspace-page">
      {/* Executive header */}
      <div className="edge-workspace-header edge-workspace-header--executive">
        <div className="edge-header__intro">
          <div className="edge-header__title-row">
            <h1>Makine & Edge</h1>
            {isSimulatorFixture && (
              <span className="edge-sim-badge edge-sim-badge--header">Simülatör</span>
            )}
          </div>
          <p className="edge-header__subtitle">
            Makine bağlantıları, operasyonel sinyaller ve Edge veri akışının merkezi.
          </p>
          {isSimulatorFixture && (
            <p className="edge-simulator-notice edge-simulator-notice--inline" data-testid="edge-simulator-page-notice">
              Simülatör verisi — gerçek makine bağlantısı değildir.
            </p>
          )}
        </div>
      </div>

      {loadError && (
        <div className="edge-load-error" role="alert">
          {loadError}
        </div>
      )}

      {/* Primary KPI strip */}
      <EdgeExecutiveKpis
        machines={machines}
        machineDetails={machineDetails}
        deliveryHealth={resolvedDeliveryHealth}
      />

      {/* Operational attention */}
      <EdgeOperationalAttention
        machines={machines}
        machineDetails={machineDetails}
        deliveryHealth={resolvedDeliveryHealth}
      />

      {/* Primary machine table */}
      <section className="edge-landing-section">
        <h2 className="edge-section-title edge-section-title--compact">Makine Operasyonları</h2>
        <EdgeMachineTable
          machines={machines}
          machineDetails={machineDetails}
          deliveryHealth={resolvedDeliveryHealth}
          selectedMachineId={selectedMachineId}
          onSelectMachine={setSelectedMachineId}
          isSimulatorFixture={isSimulatorFixture}
        />
      </section>

      {selectedMachineId && activeMachine && activeDetail && (
        <EdgeMachineDetailPanel
          machine={activeMachine}
          detail={activeDetail}
          gateway={gateways.find((g) => g.id === activeMachine.activeGatewayId) ?? gateways[0]}
          deliveryHealth={resolvedDeliveryHealth}
          bufferedEvents={bufferedEvents}
          deadLetters={deadLetters}
          offlineMessage={offlineMessage}
          replayMessage={replayMessage}
          isSimulatorFixture={isSimulatorFixture}
          onClose={() => setSelectedMachineId(null)}
        />
      )}

      <div className="edge-landing-secondary">
        <section className="edge-landing-section">
          <h2 className="edge-section-title edge-section-title--compact">Edge Gateway Durumu</h2>
          <EdgeGatewayWorkspace gateways={gateways} deliveryHealth={resolvedDeliveryHealth} />
        </section>

        <section className="edge-landing-section edge-landing-section--timeline">
          <h2 className="edge-section-title edge-section-title--compact">Son Operasyonel Olaylar</h2>
          <EdgeEventTimeline events={recentEvents} machineCodeById={machineCodeById} />
        </section>
      </div>
    </div>
  );
}
