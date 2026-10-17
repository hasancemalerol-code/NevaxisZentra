import {
  Activity,
  AlertTriangle,
  Bot,
  CheckCircle2,
  Cloud,
  Database,
  Plug,
  RadioTower,
  Server,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { StateRetryButton } from "@/components/ui/state-retry-button";

import "./system-status.css";

import {
  formatApplicationVersion,
  formatBuildDetail,
  formatSystemStatusDateTime,
  getComponentKindLabel,
  getDependencyLabel,
  getEdgeConnectivityLabel,
  getHealthStatusBadgeTone,
  getHealthStatusLabel,
  getIntegrationFreshnessLabel,
  getIntegrationHealthStatusLabel,
  getIntegrationUiStatusBadgeTone,
  getIntegrationUiStatusLabel,
  getOperatingModeBadgeTone,
  isSystemStatusVerified,
  getOperatingModeLabel,
  getTechnicalAlertSeverityBadgeTone,
  getTechnicalAlertSeverityLabel,
  getTechnicalAlertStatusLabel,
  resolveSystemStatusHeadline,
} from "../domain/enterprise-reliability-labels";
import type { SystemStatusWorkspaceApi } from "../server/enterprise-reliability-api-client";
import { findComponentByKind, readSystemStatusWorkspace } from "../server/enterprise-reliability-reader";

function resolveDependencyStatus(
  workspace: SystemStatusWorkspaceApi,
  key: "database" | "messageBroker" | "aiProvider" | "backgroundJobs",
): string {
  const dependencyStatus = workspace.dependencies[key];
  if (dependencyStatus && dependencyStatus !== "unknown") {
    return dependencyStatus;
  }

  const componentKind =
    key === "database"
      ? "database"
      : key === "messageBroker"
        ? "message-broker"
        : key === "aiProvider"
          ? "ai-provider"
          : "background-jobs";

  return findComponentByKind(workspace, componentKind)?.healthStatus ?? "unknown";
}

export async function SystemStatusWorkspacePanel() {
  const result = await readSystemStatusWorkspace();
  if (result.status === "failure") {
    return null;
  }

  const { workspace } = result;
  const apiComponent = findComponentByKind(workspace, "api");
  const databaseStatus = resolveDependencyStatus(workspace, "database");
  const aiStatus = resolveDependencyStatus(workspace, "aiProvider");
  const messageBrokerStatus = resolveDependencyStatus(workspace, "messageBroker");
  const backgroundJobsStatus = resolveDependencyStatus(workspace, "backgroundJobs");

  const coreServices = [
    {
      id: "api",
      label: "Uygulama Servisi",
      status: apiComponent?.healthStatus ?? "unknown",
      detail: "Uygulama servisinin yanıt durumu",
      icon: Server,
    },
    {
      id: "database",
      label: getDependencyLabel("database"),
      status: databaseStatus,
      detail: "Operasyonel veri altyapısı",
      icon: Database,
    },
    {
      id: "ai-provider",
      label: getDependencyLabel("aiProvider"),
      status: aiStatus,
      detail: "Yapay zekâ görev ve analiz sağlayıcısı",
      icon: Bot,
    },
    {
      id: "message-broker",
      label: getDependencyLabel("messageBroker"),
      status: messageBrokerStatus,
      detail: "Arka planda çalışan mesajlaşma altyapısı",
      icon: Cloud,
    },
    {
      id: "background-jobs",
      label: getDependencyLabel("backgroundJobs"),
      status: backgroundJobsStatus,
      detail: "Arka plan iş yürütücüsü",
      icon: Activity,
    },
  ];

  const coreStatuses = coreServices.map((service) => service.status);
  const allCoreUnknown = coreStatuses.every((status) => status === "unknown");
  const headline = resolveSystemStatusHeadline({ ...workspace, coreStatuses });
  const verified = isSystemStatusVerified({ components: workspace.components, coreStatuses });
  const integrationIssueCount = workspace.integrations.filter(
    (integration) => getIntegrationUiStatusLabel(integration.uiStatusLabel) !== "Bağlı",
  ).length;
  const edgeIssueCount = workspace.edgeAgents.filter(
    (agent) => agent.connectivityStatus !== "healthy",
  ).length;

  const versionText = formatApplicationVersion(workspace.applicationVersion);
  const buildText = formatBuildDetail(workspace.buildSha);
  const versionMissing = versionText === "Sürüm bilgisi yok";
  const buildMissing = buildText === "Derleme bilgisi yok";

  return (
    <div className="sysstat" data-testid="system-status-workspace-panel">
      <div className="sysstat__summary" data-testid="system-status-headline">
        <div className="sysstat__summary-copy">
          <Badge tone={headline.tone}>{headline.label === "Doğrulanamadı" ? "Sağlık bilgisi bekleniyor" : headline.label}</Badge>
          {headline.label === "Doğrulanamadı" ? <span className="sysstat__trace">Bileşenlerden henüz sağlık bilgisi alınmadı.</span> : null}
          <span className="sysstat__trace">
            Son güncelleme: {formatSystemStatusDateTime(workspace.generatedAtUtc)}
          </span>
        </div>
        {headline.tone !== "success" ? <StateRetryButton primary={false} /> : null}
      </div>

      <div className="sysstat__kpis" data-testid="system-status-executive-summary">
        <article className="sysstat__kpi">
          <p className="sysstat__kpi-label">Çalışma Modu</p>
          <p className="sysstat__kpi-value">
            {verified || workspace.operatingMode !== "fully-operational" ? (
              <Badge tone={getOperatingModeBadgeTone(workspace.operatingMode)}>
                {getOperatingModeLabel(workspace.operatingMode)}
              </Badge>
            ) : (
              <span className="sysstat__unverified">—</span>
            )}
          </p>
          {verified || workspace.operatingMode !== "fully-operational" ? null : (
            <p className="sysstat__kpi-detail">Çalışma modu bildirilmedi</p>
          )}
        </article>
        <article className="sysstat__kpi">
          <p className="sysstat__kpi-label">Açık Uyarılar</p>
          <p className="sysstat__kpi-value">{workspace.alerts.length}</p>
          <p className="sysstat__kpi-detail">Açık teknik uyarı sayısı</p>
        </article>
        <article className="sysstat__kpi">
          <p className="sysstat__kpi-label">Entegrasyon Sorunları</p>
          <p className="sysstat__kpi-value">{integrationIssueCount}</p>
          <p className="sysstat__kpi-detail">Bağlı olmayan entegrasyon</p>
        </article>
        <article className="sysstat__kpi">
          <p className="sysstat__kpi-label">Saha Ajanı Sorunları</p>
          <p className="sysstat__kpi-value">{edgeIssueCount}</p>
          <p className="sysstat__kpi-detail">Bağlantısı sağlıklı olmayan ajan</p>
        </article>
        <article className="sysstat__kpi">
          <p className="sysstat__kpi-label">Uygulama Sürümü</p>
          <p className="sysstat__kpi-value sysstat__kpi-value--text">{versionMissing ? "—" : versionText}</p>
          <p className="sysstat__kpi-detail">{versionMissing && buildMissing ? "Sürüm ve derleme bilgisi bildirilmedi" : buildText}</p>
        </article>
      </div>

      {workspace.showsFalseHealth && (
        <div className="sysstat__banner" role="alert" data-testid="system-status-false-health">
          <AlertTriangle aria-hidden="true" />
          <div>
            <h3>Sağlık göstergesi tutarsız</h3>
            <p>
              Bazı bileşenler erişilemez görünüyor ancak sistem tam operasyonel modda raporlanıyor.
              Güvenilir karar için detayları inceleyin.
            </p>
          </div>
        </div>
      )}

      <section className="sysstat__section" aria-labelledby="system-status-core-heading">
        <h2 id="system-status-core-heading" className="sysstat__heading">Temel Bileşenler</h2>
        {allCoreUnknown ? (
          <p className="sysstat__empty" data-testid="system-status-core-unknown">
            <AlertTriangle aria-hidden="true" size={14} />
            {coreServices.length} bileşenin sağlık bilgisi alınamadı; durumlar doğrulanana kadar gösterilmez.
          </p>
        ) : null}
        <div className="sysstat__kpis" data-testid="system-status-core-services">
          {coreServices.map((service) => {
            const Icon = service.icon;
            const tone = getHealthStatusBadgeTone(service.status);

            return (
              <article key={service.id} className="sysstat__kpi">
                <div className="sysstat__kpi-header">
                  <p className="sysstat__kpi-label">{service.label}</p>
                  <span className="sysstat__kpi-icon" aria-hidden="true">
                    <Icon size={16} />
                  </span>
                </div>
                {allCoreUnknown ? (
                  <p className="sysstat__kpi-detail sysstat__unverified">Bilinmiyor</p>
                ) : null}
                {allCoreUnknown ? null : (
                  <p className="sysstat__kpi-value">
                    {service.status === "unknown" ? (
                      <span className="sysstat__unverified sysstat__unverified--label">Bilinmiyor</span>
                    ) : (
                      <Badge tone={tone}>{getHealthStatusLabel(service.status)}</Badge>
                    )}
                  </p>
                )}
                <p className="sysstat__kpi-detail">{service.detail}</p>
              </article>
            );
          })}
        </div>
      </section>

      <div className="sysstat__section sysstat__group">
      <section className="sysstat__subsection" aria-labelledby="system-status-integrations-heading">
        <header className="sysstat__section-header">
          <h3 id="system-status-integrations-heading" className="sysstat__heading">
            <Plug aria-hidden="true" size={16} />
            Entegrasyonlar
          </h3>
        </header>
        {workspace.integrations.length === 0 ? (
          <p className="sysstat__empty"><Plug aria-hidden="true" size={14} />Kayıtlı entegrasyon bulunamadı.</p>
        ) : (
          <div className="sysstat__table-wrap" tabIndex={0} role="region" aria-label="Entegrasyon durumu (kaydırılabilir)" data-testid="system-status-integrations-table">
            <table aria-label="Entegrasyon durumu" className="sysstat__table">
              <thead>
                <tr>
                  <th>Entegrasyon</th>
                  <th>Tür</th>
                  <th>Bağlantı Durumu</th>
                  <th>Sağlık</th>
                  <th>Güncellik</th>
                  <th>Son Başarılı Eşitleme</th>
                </tr>
              </thead>
              <tbody>
                {workspace.integrations.map((integration) => (
                  <tr key={integration.connectorId}>
                    <td>
                      <span className="sysstat__primary">{integration.name}</span>
                    </td>
                    <td>{getComponentKindLabel(integration.connectorKind)}</td>
                    <td>
                      <Badge tone={getIntegrationUiStatusBadgeTone(integration.uiStatusLabel)}>
                        {getIntegrationUiStatusLabel(integration.uiStatusLabel)}
                      </Badge>
                    </td>
                    <td>{getIntegrationHealthStatusLabel(integration.healthStatus)}</td>
                    <td>{getIntegrationFreshnessLabel(integration.freshnessState)}</td>
                    <td>{formatSystemStatusDateTime(integration.lastSuccessfulSyncAtUtc)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <section className="sysstat__subsection" aria-labelledby="system-status-edge-heading">
        <header className="sysstat__section-header">
          <h3 id="system-status-edge-heading" className="sysstat__heading">
            <RadioTower aria-hidden="true" size={16} />
            Saha Ajanları
          </h3>
        </header>
        {workspace.edgeAgents.length === 0 ? (
          <p className="sysstat__empty"><RadioTower aria-hidden="true" size={14} />Kayıtlı saha ajanı bulunamadı.</p>
        ) : (
          <div className="sysstat__table-wrap" tabIndex={0} role="region" aria-label="Saha ajanı durumu (kaydırılabilir)" data-testid="system-status-edge-agents-table">
            <table aria-label="Saha ajanı durumu" className="sysstat__table">
              <thead>
                <tr>
                  <th>Saha Ajanı</th>
                  <th>Bağlantı</th>
                  <th>Tampon</th>
                  <th>Sürüm</th>
                  <th>Son Nabız</th>
                  <th>Son Eşitleme</th>
                </tr>
              </thead>
              <tbody>
                {workspace.edgeAgents.map((agent) => (
                  <tr key={agent.id}>
                    <td>
                      <span className="sysstat__primary">{agent.displayName}</span>
                      <span className="sysstat__trace">{agent.agentKey}</span>
                    </td>
                    <td>
                      <Badge tone={getHealthStatusBadgeTone(agent.connectivityStatus)}>
                        {getEdgeConnectivityLabel(agent.connectivityStatus)}
                      </Badge>
                    </td>
                    <td>
                      {agent.bufferBacklogCount}/{agent.bufferCapacity}
                    </td>
                    <td>{agent.agentVersion}</td>
                    <td>{formatSystemStatusDateTime(agent.lastHeartbeatAtUtc)}</td>
                    <td>{formatSystemStatusDateTime(agent.lastSyncAtUtc)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>

      <section className="sysstat__subsection" aria-labelledby="system-status-alerts-heading">
        <header className="sysstat__section-header">
          <h3 id="system-status-alerts-heading" className="sysstat__heading">
            <AlertTriangle aria-hidden="true" size={16} />
            Teknik Uyarılar
          </h3>
        </header>
        {workspace.alerts.length === 0 ? (
          <p className="sysstat__empty" data-testid="system-status-no-alerts">
            <CheckCircle2 aria-hidden="true" size={14} />
            Açık teknik uyarı yok.
          </p>
        ) : (
          <div className="sysstat__table-wrap" tabIndex={0} role="region" aria-label="Sistem uyarıları (kaydırılabilir)" data-testid="system-status-alerts-table">
            <table aria-label="Sistem uyarıları" className="sysstat__table">
              <thead>
                <tr>
                  <th>Uyarı</th>
                  <th>Önem</th>
                  <th>Durum</th>
                  <th>Açılış</th>
                </tr>
              </thead>
              <tbody>
                {workspace.alerts.map((alert) => (
                  <tr key={alert.id}>
                    <td>
                      <span className="sysstat__primary">{alert.title}</span>
                    </td>
                    <td>
                      <Badge tone={getTechnicalAlertSeverityBadgeTone(alert.severity)}>
                        {getTechnicalAlertSeverityLabel(alert.severity)}
                      </Badge>
                    </td>
                    <td>{getTechnicalAlertStatusLabel(alert.status)}</td>
                    <td>{formatSystemStatusDateTime(alert.openedAtUtc)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </section>
      </div>

      {workspace.components.length > 0 && (
        <section className="sysstat__section" aria-labelledby="system-status-components-heading">
          <header className="sysstat__section-header">
            <h3 id="system-status-components-heading" className="sysstat__heading">
              Bileşen Sağlık Kayıtları
            </h3>
          </header>
          <div className="sysstat__table-wrap" tabIndex={0} role="region" aria-label="Bileşen Sağlık Kayıtları (kaydırılabilir)" data-testid="system-status-components-table">
            <table aria-label="Bileşen Sağlık Kayıtları" className="sysstat__table">
              <thead>
                <tr>
                  <th>Bileşen</th>
                  <th>Teknik Ad</th>
                  <th>Durum</th>
                  <th>Son Gözlem</th>
                </tr>
              </thead>
              <tbody>
                {workspace.components.map((component) => (
                  <tr key={`${component.componentKind}-${component.componentKey}`}>
                    <td>{getComponentKindLabel(component.componentKind)}</td>
                    <td className="sysstat__trace">{component.componentKey}</td>
                    <td>
                      <Badge tone={getHealthStatusBadgeTone(component.healthStatus)}>
                        {getHealthStatusLabel(component.healthStatus)}
                      </Badge>
                    </td>
                    <td>{formatSystemStatusDateTime(component.observedAtUtc)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </section>
      )}
    </div>
  );
}
