"use client";

import { useMemo, useState } from "react";
import Link from "next/link";
import { Activity, AlertTriangle, ChevronRight, Gauge, Info } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import type {
  MaintenanceConditionAssetRow,
  MaintenanceConditionProfile,
  MaintenanceConditionWorkspace,
} from "../domain/maintenance-condition";
import {
  formatMaintenanceConditionAssetLabel,
  getMaintenanceAnomalySeverityLabel,
  getMaintenanceDataFreshnessLabel,
  getMaintenanceDataSufficiencyLabel,
  getMaintenancePredictiveConditionStatusLabel,
  getMaintenancePredictiveConditionStatusTone,
  getMaintenanceTrendStateLabel,
  MAINTENANCE_CONDITION_ANOMALY_EXPLANATION,
  MAINTENANCE_CONDITION_SIMULATOR_DISCLAIMER,
  MAINTENANCE_CONDITION_STALE_WARNING,
} from "../presentation/maintenance-condition-labels";
import { MaintenanceDashboardKpi } from "./maintenance-dashboard-kpi";
import { MaintenanceSectionHeader, MaintenanceTableActionLink } from "./maintenance-table-primitives";
import { MaintenanceWorkspaceNav } from "./maintenance-workspace-nav";

type MaintenanceConditionViewProps = Readonly<{
  workspace: MaintenanceConditionWorkspace;
  initialProfile?: MaintenanceConditionProfile;
  initialAssetReference?: string;
}>;

function formatDateTime(iso?: string): string {
  if (!iso) {
    return "—";
  }
  return new Date(iso).toLocaleString("tr-TR", {
    day: "2-digit",
    month: "short",
    hour: "2-digit",
    minute: "2-digit",
  });
}

function ConditionStatusBadge({ status }: Readonly<{ status: MaintenanceConditionAssetRow["conditionStatus"] }>) {
  return (
    <Badge tone={getMaintenancePredictiveConditionStatusTone(status)}>
      {getMaintenancePredictiveConditionStatusLabel(status)}
    </Badge>
  );
}

export function MaintenanceConditionView({
  workspace,
  initialProfile,
  initialAssetReference,
}: MaintenanceConditionViewProps) {
  const [selectedReference, setSelectedReference] = useState(
    initialAssetReference ?? workspace.assets[0]?.assetReference ?? "",
  );
  const [profile, setProfile] = useState<MaintenanceConditionProfile | undefined>(initialProfile);

  const selectedRow = useMemo(
    () => workspace.assets.find((row) => row.assetReference === selectedReference),
    [workspace.assets, selectedReference],
  );

  const overview = workspace.overview;

  return (
    <main className="production-execution-page maintenance-dashboard-page maintenance-condition-page">
      <header className="maintenance-dashboard-header production-execution-header">
        <div className="maintenance-dashboard-header__intro">
          <p>BAKIM &amp; ARIZA</p>
          <h1>Kestirici Bakım</h1>
          <span>Kestirici bakım altyapısı — gözlemlenen makine davranışından deterministik anomali tespiti.</span>
          <p className="maintenance-condition-disclaimer">{MAINTENANCE_CONDITION_SIMULATOR_DISCLAIMER}</p>
        </div>
      </header>

      <MaintenanceWorkspaceNav active="condition" />

      <section
        className="production-execution-kpis maintenance-dashboard-kpis maintenance-condition-kpis"
        aria-label="Makine ve varlık durumu"
      >
        <MaintenanceDashboardKpi label="İzlenen Varlık" value={overview.monitoredAssetCount} description="Eşleştirilmiş makine/varlık" tone="ready" icon={<Gauge />} />
        <MaintenanceDashboardKpi label="Normal" value={overview.normalCount} description="Normal durum profili" tone="ready" icon={<Activity />} />
        <MaintenanceDashboardKpi label="İzlenmeli" value={overview.watchCount} description="İzleme gerektiren" tone="paused" icon={<Info />} />
        <MaintenanceDashboardKpi label="Bozulma Eğilimi" value={overview.degradedCount} description="Sapma eğilimi" tone="in-progress" icon={<AlertTriangle />} />
        <MaintenanceDashboardKpi label="Kritik" value={overview.criticalCount} description="Kritik sapma" tone="critical" icon={<AlertTriangle />} />
        <MaintenanceDashboardKpi label="Yetersiz Veri" value={overview.insufficientDataCount} description="Yetersiz örnek" tone="paused" icon={<Info />} />
      </section>

      <Card className="maintenance-condition-table-card">
        <MaintenanceSectionHeader
          title="Durum Zekâsı"
          subtitle="Edge gözlemlerinden türetilen bakım durum profilleri."
        />
        <div className="maintenance-table-scroll">
          <table className="maintenance-table">
            <thead>
              <tr>
                <th>Varlık</th>
                <th>Makine</th>
                <th>Durum</th>
                <th>Aktif Anomali</th>
                <th>En Yüksek Seviye</th>
                <th>Veri Güncelliği</th>
                <th>Veri Yeterliliği</th>
                <th>Son Değerlendirme</th>
                <th />
              </tr>
            </thead>
            <tbody>
              {workspace.assets.map((row) => (
                <tr key={row.assetReference}>
                  <td>{formatMaintenanceConditionAssetLabel(row.assetReference, row.assetName)}</td>
                  <td>{row.machineCode ?? row.machineName ?? "—"}</td>
                  <td>
                    <ConditionStatusBadge status={row.conditionStatus} />
                  </td>
                  <td>{row.activeAnomalyCount}</td>
                  <td>{row.highestAnomalySeverity ? getMaintenanceAnomalySeverityLabel(row.highestAnomalySeverity) : "—"}</td>
                  <td>{getMaintenanceDataFreshnessLabel(row.dataFreshness)}</td>
                  <td>{getMaintenanceDataSufficiencyLabel(row.dataSufficiency)}</td>
                  <td>{formatDateTime(row.lastEvaluatedAtUtc)}</td>
                  <td>
                    <button
                      type="button"
                      className="maintenance-table-inline-action"
                      onClick={() => {
                        setSelectedReference(row.assetReference);
                        if (initialProfile?.assetReference === row.assetReference) {
                          setProfile(initialProfile);
                        } else {
                          setProfile(undefined);
                        }
                      }}
                    >
                      İncele
                      <ChevronRight aria-hidden="true" />
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      </Card>

      {selectedRow ? (
        <section className="maintenance-condition-detail-grid">
          <Card className="maintenance-condition-detail-card">
            <MaintenanceSectionHeader
              title="Güncel Durum"
              subtitle={formatMaintenanceConditionAssetLabel(
                selectedRow.assetReference,
                selectedRow.assetName,
                selectedRow.machineCode,
              )}
            />
            {selectedRow.dataFreshness === "stale" ? (
              <p className="maintenance-condition-stale-warning">{MAINTENANCE_CONDITION_STALE_WARNING}</p>
            ) : null}
            <div className="maintenance-condition-detail-summary">
              <ConditionStatusBadge status={selectedRow.conditionStatus} />
              <span>Trend: {profile ? getMaintenanceTrendStateLabel(profile.trendState) : "—"}</span>
              <span>Veri yeterliliği: {getMaintenanceDataSufficiencyLabel(selectedRow.dataSufficiency)}</span>
              {profile?.overallConditionScore !== undefined ? (
                <span>Durum skoru: {profile.overallConditionScore.toFixed(0)}</span>
              ) : null}
            </div>
            <p className="maintenance-condition-explanation">{MAINTENANCE_CONDITION_ANOMALY_EXPLANATION}</p>
            <Link
              href={`/bakim/varliklar/${encodeURIComponent(selectedRow.assetReference)}`}
              className="maintenance-dashboard-action maintenance-dashboard-action--tertiary"
            >
              Bakım kaydını incele
            </Link>
          </Card>

          <Card className="maintenance-condition-detail-card">
            <MaintenanceSectionHeader title="Metrik Sinyalleri" />
            <ul className="maintenance-condition-metric-list">
              {(profile?.context.metricSnapshots ?? []).map((metric) => (
                <li key={metric.metricKey}>
                  <strong>{metric.displayName}</strong>
                  <span>
                    {metric.observedValue ?? "—"}
                    {metric.expectedUnit ? ` ${metric.expectedUnit}` : ""}
                  </span>
                  <span>Referans: {metric.baselineValue ?? "—"}</span>
                  <span>Trend: {metric.trendState ? getMaintenanceTrendStateLabel(metric.trendState) : "—"}</span>
                </li>
              ))}
            </ul>
          </Card>

          <Card className="maintenance-condition-detail-card maintenance-condition-detail-card--wide">
            <MaintenanceSectionHeader title="Anomali Kanıtları" />
            {(profile?.activeAnomalies ?? []).length === 0 ? (
              <p className="maintenance-empty-inline">Aktif anomali kaydı yok.</p>
            ) : (
              <div className="maintenance-condition-anomaly-list">
                {(profile?.activeAnomalies ?? []).map((anomaly) => (
                  <article key={anomaly.id} className="maintenance-condition-anomaly-card">
                    <header>
                      <strong>{anomaly.metricDisplayName}</strong>
                      <Badge tone={anomaly.severity === "critical" || anomaly.severity === "high" ? "danger" : "warning"}>
                        {getMaintenanceAnomalySeverityLabel(anomaly.severity)}
                      </Badge>
                    </header>
                    <p>{anomaly.evidenceSummary}</p>
                    <dl>
                      <div>
                        <dt>Gözlenen</dt>
                        <dd>{anomaly.observedValue}</dd>
                      </div>
                      <div>
                        <dt>Referans</dt>
                        <dd>{anomaly.baselineValue}</dd>
                      </div>
                      <div>
                        <dt>Sapma</dt>
                        <dd>%{anomaly.deviationPercent.toFixed(1)}</dd>
                      </div>
                      <div>
                        <dt>Algılama</dt>
                        <dd>{formatDateTime(anomaly.detectedAtUtc)}</dd>
                      </div>
                    </dl>
                  </article>
                ))}
              </div>
            )}
          </Card>

          <Card className="maintenance-condition-detail-card">
            <MaintenanceSectionHeader title="Edge & Bakım Bağlamı" />
            <ul className="maintenance-condition-context-list">
              <li>Son 24 saat alarm: {profile?.context.alarmContext.recentAlarmCount24h ?? 0}</li>
              <li>Son 24 saat duruş: {profile?.context.downtimeContext.episodeCount24h ?? 0}</li>
              <li>Son 90 gün arıza kaydı: {profile?.context.maintenanceHistory.recentFailureCount90d ?? 0}</li>
              <li>
                Son bakım:{" "}
                {formatDateTime(profile?.context.maintenanceHistory.lastMaintenanceCompletedAtUtc)}
              </li>
            </ul>
          </Card>
        </section>
      ) : null}

      {!profile && selectedReference ? (
        <p className="maintenance-condition-hint">
          Detay için önce değerlendirme çalıştırın veya{" "}
          <MaintenanceTableActionLink href={`/bakim/durum-zekasi?asset=${encodeURIComponent(selectedReference)}`}>
            sayfayı yenileyin
          </MaintenanceTableActionLink>
          .
        </p>
      ) : null}
    </main>
  );
}
