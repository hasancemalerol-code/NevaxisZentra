"use client";

import { useState } from "react";
import {
  formatDurationMs,
  getAlarmSeverityClass,
  getAlarmSeverityLabel,
  getConnectivityLabel,
  getDowntimeReasonCategoryLabel,
  getEdgeBufferErrorLabel,
  getEdgeBufferStatusLabel,
  getEdgeDeliveryPresentationLabel,
  getEdgeDeliveryStatusLabel,
  getEdgeGatewayVendorLabel,
  getEdgeReplayStatusLabel,
  getMachineEventKindLabel,
  getObservedMachineStateLabel,
  getMetricLabel,
  getTelemetryQualityClass,
  getTelemetryQualityLabel,
  formatBufferUtilization,
} from "../presentation/edge-labels";
import {
  computeDataFreshness,
  formatRelativeTime,
} from "../presentation/edge-freshness";
import type {
  EdgeDeliveryHealthReadModel,
  EdgeBufferedEventReadModel,
  EdgeDeadLetterReadModel,
} from "../presentation/edge-reliability-fixtures";
import type {
  EdgeGatewayReadModel,
  EdgeMachineDetailBundle,
  MachineRegistryReadModel,
} from "../domain/edge-types";

const TABS = [
  { id: "summary", label: "Özet" },
  { id: "telemetry", label: "Telemetri" },
  { id: "cycles", label: "Çevrimler" },
  { id: "ops", label: "Duruş & Alarm" },
  { id: "setupScrap", label: "Kurulum ve fire" },
  { id: "delivery", label: "Saha veri sağlığı" },
  { id: "timeline", label: "Olaylar" },
] as const;

type TabId = (typeof TABS)[number]["id"];

/** EDGE-UI-1 regression anchors: Makine Özeti · Canlı Durum · Olay Geçmişi */

interface EdgeMachineDetailPanelProps {
  machine: MachineRegistryReadModel;
  detail: EdgeMachineDetailBundle;
  gateway?: EdgeGatewayReadModel | null;
  deliveryHealth: EdgeDeliveryHealthReadModel;
  bufferedEvents: EdgeBufferedEventReadModel[];
  deadLetters: EdgeDeadLetterReadModel[];
  offlineMessage?: string | null;
  replayMessage?: string | null;
  isSimulatorFixture: boolean;
  onClose: () => void;
}

function QualityBadge({ quality }: { quality: string }) {
  return (
    <span className={`telemetry-quality ${getTelemetryQualityClass(quality)}`}>
      {getTelemetryQualityLabel(quality)}
    </span>
  );
}

function SeverityBadge({ severity }: { severity: string }) {
  const cls = getAlarmSeverityClass(severity);
  return (
    <span className={`alarm-severity ${cls}${severity === "critical" ? " alarm-severity--prominent" : ""}`}>
      {getAlarmSeverityLabel(severity)}
    </span>
  );
}

export function EdgeMachineDetailPanel({
  machine,
  detail,
  gateway,
  deliveryHealth,
  bufferedEvents,
  deadLetters,
  offlineMessage,
  replayMessage,
  isSimulatorFixture,
  onClose,
}: EdgeMachineDetailPanelProps) {
  const [tab, setTab] = useState<TabId>("summary");
  const signal = detail.signalProjection;
  const freshness = computeDataFreshness(signal?.lastTelemetryAtUtc ?? machine.lastSeenAtUtc);
  const machineBuffered = bufferedEvents.filter((e) => e.machineCode === machine.machineCode);
  const machineDeadLetters = deadLetters.filter((d) => d.machineCode === machine.machineCode);

  return (
    <section className="edge-detail-panel" data-testid="edge-machine-detail">
      <header className="edge-detail-panel__header">
        <div>
          <h2 className="edge-detail-panel__title">
            {machine.name}
            <span className="edge-detail-panel__code">{machine.machineCode}</span>
            {isSimulatorFixture && <span className="edge-sim-badge">Örnek veri</span>}
          </h2>
          <p className="edge-detail-panel__subtitle">
            Makine operasyonel gözlemleri — resmi üretim/kalite kaydı değildir.
          </p>
        </div>
        <button type="button" className="edge-detail-panel__close" onClick={onClose} aria-label="Detayı kapat">
          Kapat
        </button>
      </header>

      {isSimulatorFixture && (
        <p className="edge-simulator-notice">
          Örnek veri — gerçek makine bağlantısı değildir.
        </p>
      )}

      <div className="edge-detail-tabs" role="tablist">
        {TABS.map((t) => (
          <button
            key={t.id}
            type="button"
            role="tab"
            aria-selected={tab === t.id}
            className={`edge-detail-tab${tab === t.id ? " edge-detail-tab--active" : ""}`}
            onClick={() => setTab(t.id)}
          >
            {t.label}
          </button>
        ))}
      </div>

      <div className="edge-detail-panel__body">
        {tab === "summary" && (
          <>
            <div className="edge-detail-summary-grid edge-detail-summary-grid--primary">
              <div className="edge-detail-summary-item">
                <span className="edge-detail-summary-item__label">Bağlantı</span>
                <span className={`edge-status-pill edge-status-pill--conn-${signal?.connectivity ?? "unknown"}`}>
                  {getConnectivityLabel(signal?.connectivity ?? "unknown")}
                </span>
              </div>
              <div className="edge-detail-summary-item">
                <span className="edge-detail-summary-item__label">Makine Durumu</span>
                <span>{getObservedMachineStateLabel(signal?.observedState ?? machine.status)}</span>
              </div>
              <div className="edge-detail-summary-item">
                <span className="edge-detail-summary-item__label">Güncellik</span>
                <span className={`edge-status-pill edge-status-pill--fresh-${freshness.tone}`}>{freshness.label}</span>
              </div>
              <div className="edge-detail-summary-item">
                <span className="edge-detail-summary-item__label">Aktif Alarm</span>
                <span>{detail.activeAlarms.length}</span>
              </div>
              <div className="edge-detail-summary-item">
                <span className="edge-detail-summary-item__label">Son Çevrim</span>
                <span>{formatRelativeTime(signal?.lastCycleAtUtc ?? detail.cycleProjection?.lastCycleAtUtc)}</span>
              </div>
              <div className="edge-detail-summary-item">
                <span className="edge-detail-summary-item__label">Saha verisi teslimi</span>
                <span>{getEdgeDeliveryPresentationLabel(deliveryHealth)}</span>
              </div>
            </div>
            <div className="edge-detail-summary-grid edge-detail-summary-grid--secondary">
              <div className="edge-detail-summary-item">
                <span className="edge-detail-summary-item__label">Ağ geçidi</span>
                <span>{machine.activeGatewayCode ?? "—"}</span>
              </div>
              <div className="edge-detail-summary-item">
                <span className="edge-detail-summary-item__label">Aktarım yöntemi</span>
                <span>{gateway ? getEdgeGatewayVendorLabel(gateway.vendorKey) : "—"}</span>
              </div>
              <div className="edge-detail-summary-item">
                <span className="edge-detail-summary-item__label">Son Görülme</span>
                <span>{formatRelativeTime(machine.lastSeenAtUtc)}</span>
              </div>
              <div className="edge-detail-summary-item">
                <span className="edge-detail-summary-item__label">Ortam</span>
                <span>{isSimulatorFixture ? "Örnek veri" : "Saha ağ geçidi"}</span>
              </div>
            </div>
            {signal?.isInObservedDowntime && (
              <div className="edge-live-status-banner edge-live-status-banner--warn">
                Gözlenen duruş aktif
              </div>
            )}
            {detail.activeAlarms.some((a) => a.severity === "critical") && (
              <div className="edge-alarm-attention-panel">
                {detail.activeAlarms
                  .filter((a) => a.severity === "critical")
                  .map((a) => (
                    <div key={a.id} className="edge-alarm-attention-item">
                      <SeverityBadge severity={a.severity} />
                      <span className="edge-alarm-code">{a.externalAlarmCode}</span>
                      <span>{a.alarmText ?? "—"}</span>
                    </div>
                  ))}
              </div>
            )}
          </>
        )}

        {tab === "telemetry" && (
          <>
            {detail.latestTelemetry.length === 0 ? (
              <div className="edge-empty-state edge-empty-state--compact">
                <p className="edge-empty-state__title">Telemetri yok</p>
              </div>
            ) : (
              <div className="edge-telemetry-table-wrap" tabIndex={0} role="region" aria-label="Canlı telemetri (kaydırılabilir)">
                <table aria-label="Canlı telemetri" className="edge-telemetry-table">
                  <thead>
                    <tr>
                      <th>Metrik</th>
                      <th>Değer</th>
                      <th>Birim</th>
                      <th>Kalite</th>
                      <th>Son Veri</th>
                    </tr>
                  </thead>
                  <tbody>
                    {detail.latestTelemetry.map((t) => {
                      const val =
                        t.numericValue != null
                          ? t.numericValue.toLocaleString("tr-TR")
                          : t.textValue ?? (t.booleanValue != null ? (t.booleanValue ? "Evet" : "Hayır") : "—");
                      return (
                        <tr key={t.metricKey}>
                          <td>{getMetricLabel(t.metricKey)}</td>
                          <td>{val}</td>
                          <td>{t.unit ?? "—"}</td>
                          <td><QualityBadge quality={t.quality} /></td>
                          <td>{formatRelativeTime(t.sourceTimestampUtc)}</td>
                        </tr>
                      );
                    })}
                  </tbody>
                </table>
              </div>
            )}
          </>
        )}

        {tab === "cycles" && (
          <>
            <div className="edge-cycle-summary-grid">
              <div className="edge-cycle-kpi">
                <span className="edge-cycle-kpi__label">Son Çevrim</span>
                <span className="edge-cycle-kpi__value">
                  {detail.cycleProjection?.lastCycleAtUtc
                    ? formatRelativeTime(detail.cycleProjection.lastCycleAtUtc)
                    : "—"}
                </span>
              </div>
              <div className="edge-cycle-kpi">
                <span className="edge-cycle-kpi__label">Son Çevrim Süresi</span>
                <span className="edge-cycle-kpi__value">
                  {formatDurationMs(detail.cycleProjection?.lastCycleDurationMs)}
                </span>
              </div>
              <div className="edge-cycle-kpi edge-cycle-kpi--observed">
                <span className="edge-cycle-kpi__label">Makineden Gözlenen Çevrim</span>
                <span className="edge-cycle-kpi__value">
                  {detail.cycleProjection?.observedCycleCount ?? signal?.observedCycleCount ?? 0}
                </span>
              </div>
              <div className="edge-cycle-kpi">
                <span className="edge-cycle-kpi__label">Son Sayaç</span>
                <span className="edge-cycle-kpi__value">
                  {detail.cycleProjection?.latestObservedCounter ?? signal?.latestCounter ?? "—"}
                </span>
              </div>
            </div>
            {detail.cycleHistory.length === 0 ? (
              <div className="edge-empty-state edge-empty-state--compact">
                <p className="edge-empty-state__title">Çevrim geçmişi yok</p>
              </div>
            ) : (
              <table aria-label="Çevrim bilgileri" className="edge-cycle-table">
                <thead>
                  <tr>
                    <th>Zaman</th>
                    <th>Çevrim</th>
                    <th>Süre</th>
                    <th>Gözlenen Sayaç</th>
                    <th>Sequence</th>
                    <th>Kalite</th>
                  </tr>
                </thead>
                <tbody>
                  {detail.cycleHistory.slice(0, 15).map((c) => (
                    <tr key={c.id}>
                      <td>{new Date(c.sourceTimestampUtc).toLocaleString("tr-TR")}</td>
                      <td><span className="edge-cycle-identity">{c.cycleIdentity}</span></td>
                      <td>{formatDurationMs(c.cycleDurationMs)}</td>
                      <td>{c.observedCounter ?? "—"}</td>
                      <td>{c.sourceSequence != null ? `#${c.sourceSequence}` : "—"}</td>
                      <td><QualityBadge quality={c.quality} /></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </>
        )}

        {tab === "ops" && (
          <>
            <h3 className="edge-detail-subsection-title">Gözlenen Duruşlar</h3>
            <div className="edge-ops-summary-grid" style={{ marginBottom: 12 }}>
              <div className="edge-ops-summary-card">
                <span className="edge-ops-summary-card__label">Aktif Duruş</span>
                <span className="edge-ops-summary-card__value">
                  {detail.operationalSummary?.isInObservedDowntime ? "Evet" : "Hayır"}
                </span>
              </div>
              <div className="edge-ops-summary-card">
                <span className="edge-ops-summary-card__label">Toplam Gözlenen Duruş</span>
                <span className="edge-ops-summary-card__value">
                  {detail.operationalSummary?.observedDowntimeCount ?? detail.downtimeEpisodes.length}
                </span>
              </div>
              <div className="edge-ops-summary-card">
                <span className="edge-ops-summary-card__label">Mikro Duruş</span>
                <span className="edge-ops-summary-card__value">
                  {detail.operationalSummary?.observedMicroStopCount ?? 0}
                </span>
              </div>
            </div>
            {detail.downtimeEpisodes.length === 0 ? (
              <div className="edge-empty-state edge-empty-state--compact">
                <p className="edge-empty-state__title">Gözlenen duruş kaydı yok</p>
              </div>
            ) : (
              <table aria-label="Gözlenen duruşlar" className="edge-ops-table">
                <thead>
                  <tr>
                    <th>Başlangıç</th>
                    <th>Bitiş</th>
                    <th>Süre</th>
                    <th>Kategori</th>
                    <th>Neden</th>
                    <th>Mikro Duruş</th>
                    <th>Kalite</th>
                  </tr>
                </thead>
                <tbody>
                  {detail.downtimeEpisodes.map((e) => (
                    <tr key={e.id}>
                      <td>{new Date(e.startedAtSourceUtc).toLocaleString("tr-TR")}</td>
                      <td>{e.endedAtSourceUtc ? new Date(e.endedAtSourceUtc).toLocaleString("tr-TR") : "—"}</td>
                      <td>{formatDurationMs(e.durationMs)}</td>
                      <td>{e.reasonCategory ? getDowntimeReasonCategoryLabel(e.reasonCategory) : "—"}</td>
                      <td>{e.reasonText ?? e.reasonCode ?? "—"}</td>
                      <td>{e.isMicroStop ? "Evet" : "—"}</td>
                      <td><QualityBadge quality={e.quality} /></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
            <h3 className="edge-detail-subsection-title">Aktif Alarmlar</h3>
            {detail.activeAlarms.length === 0 ? (
              <div className="edge-empty-state edge-empty-state--compact">
                <p className="edge-empty-state__title">Aktif alarm yok</p>
              </div>
            ) : (
              <table aria-label="Aktif alarmlar" className="edge-ops-table edge-ops-table--alarms">
                <thead>
                  <tr>
                    <th>Kod</th>
                    <th>Mesaj</th>
                    <th>Seviye</th>
                    <th>Başlangıç</th>
                    <th>Durum</th>
                  </tr>
                </thead>
                <tbody>
                  {detail.activeAlarms.map((a) => (
                    <tr key={a.id} className={a.severity === "critical" ? "edge-alarm-row--critical" : undefined}>
                      <td><span className="edge-alarm-code">{a.externalAlarmCode}</span></td>
                      <td>{a.alarmText ?? "—"}</td>
                      <td><SeverityBadge severity={a.severity} /></td>
                      <td>{new Date(a.raisedAtSourceUtc).toLocaleString("tr-TR")}</td>
                      <td><span className="edge-alarm-status edge-alarm-status--active">Aktif</span></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
            <h3 className="edge-detail-subsection-title">Alarm Geçmişi</h3>
            {detail.alarmHistory.length === 0 ? (
              <div className="edge-empty-state edge-empty-state--compact">
                <p className="edge-empty-state__text">Geçmiş kayıt yok</p>
              </div>
            ) : (
              <table aria-label="Alarm geçmişi" className="edge-ops-table">
                <thead>
                  <tr>
                    <th>Kod</th>
                    <th>Mesaj</th>
                    <th>Seviye</th>
                    <th>Zaman</th>
                    <th>Durum</th>
                  </tr>
                </thead>
                <tbody>
                  {detail.alarmHistory.slice(0, 20).map((o) => (
                    <tr key={o.id}>
                      <td><span className="edge-alarm-code">{o.externalAlarmCode}</span></td>
                      <td>{o.alarmText ?? "—"}</td>
                      <td><SeverityBadge severity={o.severity} /></td>
                      <td>{new Date(o.sourceTimestampUtc).toLocaleString("tr-TR")}</td>
                      <td>{o.eventKind === "machine.alarm.cleared" ? "Temizlendi" : "Yükseltildi"}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </>
        )}

        {tab === "setupScrap" && (
          <>
            <h3 className="edge-detail-subsection-title">Gözlenen kurulum / değişimler</h3>
            {detail.setupEpisodes.length === 0 ? (
              <div className="edge-empty-state edge-empty-state--compact">
                <p className="edge-empty-state__title">Gözlenen setup / değişim kaydı yok</p>
              </div>
            ) : (
              <table aria-label="Gözlenen kurulum ve değişimler" className="edge-ops-table">
                <thead>
                  <tr>
                    <th>Başlangıç</th>
                    <th>Tamamlanma</th>
                    <th>Süre</th>
                    <th>Önceki Referans</th>
                    <th>Yeni Referans</th>
                    <th>Kalite</th>
                  </tr>
                </thead>
                <tbody>
                  {detail.setupEpisodes.map((e) => (
                    <tr key={e.id}>
                      <td>{new Date(e.startedAtSourceUtc).toLocaleString("tr-TR")}</td>
                      <td>{e.completedAtSourceUtc ? new Date(e.completedAtSourceUtc).toLocaleString("tr-TR") : "—"}</td>
                      <td>{formatDurationMs(e.durationMs)}</td>
                      <td>{e.fromReference ?? e.setupType ?? "—"}</td>
                      <td>{e.toReference ?? "—"}</td>
                      <td><QualityBadge quality={e.quality} /></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
            <h3 className="edge-detail-subsection-title">Makineden Gözlenen Fire</h3>
            <p className="edge-scrap-disclaimer">
              Bu değer makine gözlemidir; resmi üretim veya kalite kaydı değildir.
            </p>
            {detail.scrapObservations.length === 0 ? (
              <div className="edge-empty-state edge-empty-state--compact">
                <p className="edge-empty-state__title">Gözlenen fire kaydı yok</p>
              </div>
            ) : (
              <table aria-label="Makineden gözlenen fire" className="edge-ops-table">
                <thead>
                  <tr>
                    <th>Miktar</th>
                    <th>Birim</th>
                    <th>Neden</th>
                    <th>Kategori</th>
                    <th>Zaman</th>
                    <th>Kalite</th>
                  </tr>
                </thead>
                <tbody>
                  {detail.scrapObservations.map((s) => (
                    <tr key={s.id}>
                      <td className="edge-scrap-qty">{s.quantity.toLocaleString("tr-TR")}</td>
                      <td>{s.unit}</td>
                      <td>{s.reasonText ?? s.reasonCode ?? "—"}</td>
                      <td>{s.reasonCategory ?? "—"}</td>
                      <td>{new Date(s.sourceTimestampUtc).toLocaleString("tr-TR")}</td>
                      <td><QualityBadge quality={s.quality} /></td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </>
        )}

        {tab === "delivery" && (
          <>
            {!deliveryHealth.apiConnected && offlineMessage && (
              <div className="edge-reliability-banner edge-reliability-banner--offline">{offlineMessage}</div>
            )}
            {deliveryHealth.apiConnected && deliveryHealth.replayStatus === "replaying" && replayMessage && (
              <div className="edge-reliability-banner edge-reliability-banner--replay">{replayMessage}</div>
            )}
            <div className="edge-reliability-status-grid">
              <div className="edge-reliability-status-item">
                <span className="edge-reliability-status-item__label">Bağlantı durumu</span>
                <span className={`edge-reliability-status-item__value ${deliveryHealth.apiConnected ? "edge-reliability-status-item__value--ok" : "edge-reliability-status-item__value--warn"}`}>
                  {deliveryHealth.apiConnected ? "Bağlı" : "Kesik"}
                </span>
              </div>
              <div className="edge-reliability-status-item">
                <span className="edge-reliability-status-item__label">Teslimat Durumu</span>
                <span>{getEdgeDeliveryStatusLabel(deliveryHealth.deliveryStatus)}</span>
              </div>
              <div className="edge-reliability-status-item">
                <span className="edge-reliability-status-item__label">Bekleyen Olay</span>
                <span>{deliveryHealth.pendingCount + deliveryHealth.retryCount}</span>
              </div>
              <div className="edge-reliability-status-item">
                <span className="edge-reliability-status-item__label">Teslim edilemeyen</span>
                <span>{deliveryHealth.deadLetterCount}</span>
              </div>
              <div className="edge-reliability-status-item">
                <span className="edge-reliability-status-item__label">En Eski Bekleyen</span>
                <span>{deliveryHealth.oldestPendingAgeMs != null ? formatDurationMs(deliveryHealth.oldestPendingAgeMs) : "—"}</span>
              </div>
              <div className="edge-reliability-status-item">
                <span className="edge-reliability-status-item__label">Son Başarılı Teslim</span>
                <span>{deliveryHealth.lastSuccessfulDeliveryAtUtc ? new Date(deliveryHealth.lastSuccessfulDeliveryAtUtc).toLocaleString("tr-TR") : "—"}</span>
              </div>
              <div className="edge-reliability-status-item">
                <span className="edge-reliability-status-item__label">Kuyruk Kullanımı</span>
                <span>{formatBufferUtilization(deliveryHealth.bufferUtilization)}</span>
              </div>
              <div className="edge-reliability-status-item">
                <span className="edge-reliability-status-item__label">Yeniden gönderim</span>
                <span>{getEdgeReplayStatusLabel(deliveryHealth.replayStatus)}</span>
              </div>
            </div>
            {machineBuffered.length > 0 && (
              <table aria-label="Saha veri teslimi" className="edge-reliability-table" style={{ marginTop: 12 }}>
                <thead>
                  <tr>
                    <th>Olay</th>
                    <th>Kaynak Zamanı</th>
                    <th>Durum</th>
                    <th>Deneme</th>
                  </tr>
                </thead>
                <tbody>
                  {machineBuffered.map((e, i) => (
                    <tr key={`${e.eventType}-${i}`}>
                      <td>{getMachineEventKindLabel(e.eventType)}</td>
                      <td>{new Date(e.sourceTimestampUtc).toLocaleString("tr-TR")}</td>
                      <td>{getEdgeBufferStatusLabel(e.status)}</td>
                      <td>{e.attempts}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
            {machineDeadLetters.length > 0 && (
              <table aria-label="Teslim edilemeyen olaylar" className="edge-reliability-table edge-reliability-table--dead-letter" style={{ marginTop: 12 }}>
                <thead>
                  <tr>
                    <th>Olay</th>
                    <th>Neden</th>
                    <th>Deneme</th>
                    <th>Son Hata</th>
                  </tr>
                </thead>
                <tbody>
                  {machineDeadLetters.map((d, i) => (
                    <tr key={`dl-${i}`}>
                      <td>{getMachineEventKindLabel(d.eventType)}</td>
                      <td>{getEdgeBufferErrorLabel(d.reason)}</td>
                      <td>{d.attempts}</td>
                      <td>{d.lastFailureAtUtc ? new Date(d.lastFailureAtUtc).toLocaleString("tr-TR") : "—"}</td>
                    </tr>
                  ))}
                </tbody>
              </table>
            )}
          </>
        )}

        {tab === "timeline" && (
          <>
            {detail.recentEvents.length === 0 ? (
              <div className="edge-empty-state edge-empty-state--compact">
                <p className="edge-empty-state__title">Olay geçmişi yok</p>
              </div>
            ) : (
              <div className="edge-timeline edge-timeline--compact">
                {[...detail.recentEvents]
                  .sort((a, b) => new Date(b.sourceTimestampUtc).getTime() - new Date(a.sourceTimestampUtc).getTime())
                  .slice(0, 25)
                  .map((e) => (
                    <div key={e.id} className="edge-timeline__item">
                      <span className="edge-timeline__time">
                        {new Date(e.sourceTimestampUtc).toLocaleString("tr-TR")}
                      </span>
                      <span className="edge-timeline__kind">{getMachineEventKindLabel(e.eventKind)}</span>
                      <span className="edge-timeline__meta">
                        Ulaşma: {new Date(e.receivedAtUtc).toLocaleString("tr-TR")}
                        {e.isDuplicate && " · Yinelenen"}
                      </span>
                    </div>
                  ))}
              </div>
            )}
          </>
        )}
      </div>
    </section>
  );
}
