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

import {
  ENTERPRISE_RELIABILITY_SUBTITLE,
  ENTERPRISE_RELIABILITY_TITLE,
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
  const headline = resolveSystemStatusHeadline(workspace);
  const apiComponent = findComponentByKind(workspace, "api");
  const databaseStatus = resolveDependencyStatus(workspace, "database");
  const aiStatus = resolveDependencyStatus(workspace, "aiProvider");
  const messageBrokerStatus = resolveDependencyStatus(workspace, "messageBroker");
  const backgroundJobsStatus = resolveDependencyStatus(workspace, "backgroundJobs");

  const coreServices = [
    {
      id: "api",
      label: "API",
      status: apiComponent?.healthStatus ?? "unknown",
      detail: apiComponent?.detail ?? "Uygulama servis sınırı",
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
      detail: "AI görev ve analiz sağlayıcısı",
      icon: Bot,
    },
    {
      id: "message-broker",
      label: getDependencyLabel("messageBroker"),
      status: messageBrokerStatus,
      detail: "Asenkron mesajlaşma altyapısı",
      icon: Cloud,
    },
    {
      id: "background-jobs",
      label: getDependencyLabel("backgroundJobs"),
      status: backgroundJobsStatus,
      detail: "Arka plan işlem hostu",
      icon: Activity,
    },
  ];

  const integrationIssueCount = workspace.integrations.filter(
    (integration) => getIntegrationUiStatusLabel(integration.uiStatusLabel) !== "Bağlı",
  ).length;
  const edgeIssueCount = workspace.edgeAgents.filter(
    (agent) => agent.connectivityStatus !== "healthy",
  ).length;

  return (
    <section className="commint-panel" data-testid="system-status-workspace-panel">
      <header className="commint-panel__header">
        <div>
          <p className="commint-executive__eyebrow">OPERASYONEL GÜVENİLİRLİK</p>
          <h2 className="commint-panel__title">{ENTERPRISE_RELIABILITY_TITLE}</h2>
          <p className="commint-panel__subtitle">{ENTERPRISE_RELIABILITY_SUBTITLE}</p>
        </div>
        <div className="commint-panel__meta" data-testid="system-status-headline">
          <Badge tone={headline.tone}>{headline.label}</Badge>
          <span className="commint-panel__trace">
            Son güncelleme: {formatSystemStatusDateTime(workspace.generatedAtUtc)}
          </span>
        </div>
      </header>

      <div className="commint-kpi-grid" data-testid="system-status-executive-summary">
        <article className="commint-kpi-card">
          <p className="commint-kpi-card__label">Çalışma Modu</p>
          <p className="commint-kpi-card__value">
            <Badge tone={getOperatingModeBadgeTone(workspace.operatingMode)}>
              {getOperatingModeLabel(workspace.operatingMode)}
            </Badge>
          </p>
        </article>
        <article className="commint-kpi-card">
          <p className="commint-kpi-card__label">Açık Uyarılar</p>
          <p className="commint-kpi-card__value">{workspace.alerts.length}</p>
        </article>
        <article className="commint-kpi-card">
          <p className="commint-kpi-card__label">Entegrasyon Sorunları</p>
          <p className="commint-kpi-card__value">{integrationIssueCount}</p>
        </article>
        <article className="commint-kpi-card">
          <p className="commint-kpi-card__label">Kenar Ajan Sorunları</p>
          <p className="commint-kpi-card__value">{edgeIssueCount}</p>
        </article>
        <article className="commint-kpi-card">
          <p className="commint-kpi-card__label">Uygulama Sürümü</p>
          <p className="commint-kpi-card__value">{workspace.applicationVersion}</p>
          <p className="commint-kpi-card__detail">Build {workspace.buildSha}</p>
        </article>
      </div>

      {workspace.showsFalseHealth && (
        <div className="commint-error-banner" role="alert" data-testid="system-status-false-health">
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

      <section aria-labelledby="system-status-core-heading">
        <h3 id="system-status-core-heading">Temel Bileşenler</h3>
        <div className="commint-kpi-grid" data-testid="system-status-core-services">
          {coreServices.map((service) => {
            const Icon = service.icon;
            const tone = getHealthStatusBadgeTone(service.status);

            return (
              <article key={service.id} className="commint-kpi-card">
                <div className="commint-kpi-card__header">
                  <p className="commint-kpi-card__label">{service.label}</p>
                  <span className="commint-kpi-card__icon" aria-hidden="true">
                    <Icon size={16} />
                  </span>
                </div>
                <p className="commint-kpi-card__value">
                  <Badge tone={tone}>{getHealthStatusLabel(service.status)}</Badge>
                </p>
                <p className="commint-kpi-card__detail">{service.detail}</p>
              </article>
            );
          })}
        </div>
      </section>

      <section aria-labelledby="system-status-integrations-heading">
        <header className="commint-panel__header">
          <h3 id="system-status-integrations-heading" className="commint-panel__title">
            <Plug aria-hidden="true" size={16} />
            Entegrasyonlar
          </h3>
        </header>
        {workspace.integrations.length === 0 ? (
          <p className="commint-table__secondary">Kayıtlı entegrasyon bulunamadı.</p>
        ) : (
          <div className="commint-table-wrap" data-testid="system-status-integrations-table">
            <table className="commint-table">
              <thead>
                <tr>
                  <th>Entegrasyon</th>
                  <th>Tür</th>
                  <th>UI Durumu</th>
                  <th>Sağlık</th>
                  <th>Güncellik</th>
                  <th>Son Başarılı Senkron</th>
                </tr>
              </thead>
              <tbody>
                {workspace.integrations.map((integration) => (
                  <tr key={integration.connectorId}>
                    <td>
                      <span className="commint-panel__primary">{integration.name}</span>
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

      <section aria-labelledby="system-status-edge-heading">
        <header className="commint-panel__header">
          <h3 id="system-status-edge-heading" className="commint-panel__title">
            <RadioTower aria-hidden="true" size={16} />
            Kenar Ajanlar
          </h3>
        </header>
        {workspace.edgeAgents.length === 0 ? (
          <p className="commint-table__secondary">Kayıtlı kenar ajan bulunamadı.</p>
        ) : (
          <div className="commint-table-wrap" data-testid="system-status-edge-agents-table">
            <table className="commint-table">
              <thead>
                <tr>
                  <th>Ajan</th>
                  <th>Bağlantı</th>
                  <th>Tampon</th>
                  <th>Sürüm</th>
                  <th>Son Nabız</th>
                  <th>Son Senkron</th>
                </tr>
              </thead>
              <tbody>
                {workspace.edgeAgents.map((agent) => (
                  <tr key={agent.id}>
                    <td>
                      <span className="commint-panel__primary">{agent.displayName}</span>
                      <span className="commint-panel__trace">{agent.agentKey}</span>
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

      <section aria-labelledby="system-status-alerts-heading">
        <header className="commint-panel__header">
          <h3 id="system-status-alerts-heading" className="commint-panel__title">
            <AlertTriangle aria-hidden="true" size={16} />
            Teknik Uyarılar
          </h3>
        </header>
        {workspace.alerts.length === 0 ? (
          <p className="commint-table__secondary" data-testid="system-status-no-alerts">
            <CheckCircle2 aria-hidden="true" size={14} />
            Açık teknik uyarı yok.
          </p>
        ) : (
          <div className="commint-table-wrap" data-testid="system-status-alerts-table">
            <table className="commint-table">
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
                      <span className="commint-panel__primary">{alert.title}</span>
                      <span className="commint-panel__trace">{alert.alertKind}</span>
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

      {workspace.components.length > 0 && (
        <section aria-labelledby="system-status-components-heading">
          <header className="commint-panel__header">
            <h3 id="system-status-components-heading" className="commint-panel__title">
              Bileşen Sağlık Kayıtları
            </h3>
          </header>
          <div className="commint-table-wrap" data-testid="system-status-components-table">
            <table className="commint-table">
              <thead>
                <tr>
                  <th>Bileşen</th>
                  <th>Anahtar</th>
                  <th>Durum</th>
                  <th>Gözlemlenme</th>
                </tr>
              </thead>
              <tbody>
                {workspace.components.map((component) => (
                  <tr key={`${component.componentKind}-${component.componentKey}`}>
                    <td>{getComponentKindLabel(component.componentKind)}</td>
                    <td className="commint-panel__trace">{component.componentKey}</td>
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
    </section>
  );
}
