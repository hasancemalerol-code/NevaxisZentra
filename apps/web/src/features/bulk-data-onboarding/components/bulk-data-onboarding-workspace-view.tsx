"use client";

import {
  BULK_DATA_ONBOARDING_SUBTITLE,
  BULK_DATA_ONBOARDING_TITLE,
  BULK_ONBOARDING_WORKFLOW_STEPS,
} from "../domain/bulk-data-onboarding-labels";
import type { BulkImportJobWorkspaceApi } from "../server/bulk-data-onboarding-api-client";
import { BulkDataOnboardingWorkflowClient } from "./bulk-data-onboarding-workflow-client";

import "./bulk-data-onboarding-workspace.css";

type BulkDataOnboardingWorkspaceViewProps = Readonly<{
  workspace: BulkImportJobWorkspaceApi | null;
  facilityId: string;
}>;

export function BulkDataOnboardingWorkspaceView({ workspace, facilityId }: BulkDataOnboardingWorkspaceViewProps) {
  const job = workspace?.job;

  return (
    <div className="bulk-onboarding-workspace" data-testid="bulk-data-onboarding-workspace">
      <header className="bulk-onboarding-workspace__hero commint-panel">
        <p className="commint-kpi-card__eyebrow">Fabrika Veri Aktarımı</p>
        <h1 className="bulk-onboarding-workspace__title">{BULK_DATA_ONBOARDING_TITLE}</h1>
        <p className="bulk-onboarding-workspace__subtitle">{BULK_DATA_ONBOARDING_SUBTITLE}</p>
        {job ? (
          <div className="bulk-onboarding-workspace__status-row">
            <span className="bulk-onboarding-workspace__status-chip" data-testid="bulk-onboarding-job-status">
              {job.status}
            </span>
            <span>{job.sourceSystem}</span>
            <span>{job.sourceProviderKind}</span>
            <span className="bulk-onboarding-workspace__safety-mode">{job.safetyMode}</span>
          </div>
        ) : null}
      </header>

      <section className="commint-panel" data-testid="bulk-onboarding-workflow">
        <h2>İş Akışı</h2>
        <ol className="bulk-onboarding-workflow">
          {BULK_ONBOARDING_WORKFLOW_STEPS.map((step) => (
            <li key={step}>{step}</li>
          ))}
        </ol>
      </section>

      <BulkDataOnboardingWorkflowClient facilityId={facilityId} initialWorkspace={workspace} />

      {job ? (
        <>
          <section className="commint-panel bulk-onboarding-kpi-grid" data-testid="bulk-onboarding-kpis">
            <article className="commint-kpi-card">
              <p className="commint-kpi-card__label">Kaynak Satır</p>
              <p className="commint-kpi-card__value">{job.totalSourceRows}</p>
            </article>
            <article className="commint-kpi-card">
              <p className="commint-kpi-card__label">Geçerli</p>
              <p className="commint-kpi-card__value">{job.validRows}</p>
            </article>
            <article className="commint-kpi-card">
              <p className="commint-kpi-card__label">Karantina</p>
              <p className="commint-kpi-card__value">{job.quarantinedRows}</p>
            </article>
            <article className="commint-kpi-card">
              <p className="commint-kpi-card__label">İçe Aktarılan</p>
              <p className="commint-kpi-card__value">{job.importedRows}</p>
            </article>
          </section>

          <section className="commint-panel" data-testid="bulk-onboarding-catalog">
            <h2>Kaynak Kataloğu</h2>
            <div className="bulk-onboarding-catalog-table">
              <div className="bulk-onboarding-catalog-table__header">
                <span>Tablo</span>
                <span>Rol</span>
                <span>Satır</span>
              </div>
              {workspace?.catalog.map((entry) => (
                <div key={entry.sourceTable} className="bulk-onboarding-catalog-table__row">
                  <span>{entry.sourceTable}</span>
                  <span>{entry.sourceSchema}</span>
                  <span>{entry.estimatedRowCount}</span>
                </div>
              ))}
            </div>
          </section>

          {workspace?.checkpoint ? (
            <section className="commint-panel" data-testid="bulk-onboarding-checkpoint">
              <h2>Checkpoint</h2>
              <p>
                {workspace.checkpoint.currentTable} · batch {workspace.checkpoint.currentBatch} ·{" "}
                {workspace.checkpoint.processedRows} satır
              </p>
            </section>
          ) : null}

          {workspace?.impactPreview ? (
            <section className="commint-panel" data-testid="bulk-onboarding-impact-preview">
              <h2>Etki Önizleme</h2>
              <ul>
                <li>Alan: {workspace.impactPreview.areasToCreate}</li>
                <li>Hat: {workspace.impactPreview.linesToCreate}</li>
                <li>İstasyon: {workspace.impactPreview.stationsToCreate}</li>
                <li>Kaynak: {workspace.impactPreview.resourcesToCreate}</li>
                <li>Malzeme: {workspace.impactPreview.materialsToCreate}</li>
                <li>Olay: {workspace.impactPreview.eventsToCreate}</li>
                <li>Karantina: {workspace.impactPreview.quarantinedRows}</li>
              </ul>
            </section>
          ) : null}

          {workspace?.reconciliation ? (
            <section className="commint-panel" data-testid="bulk-onboarding-reconciliation">
              <h2>Mutabakat</h2>
              <p>
                Kaynak {workspace.reconciliation.sourceRows} · Sahnelenen {workspace.reconciliation.stagedRows} ·
                Geçerli {workspace.reconciliation.validRows} · İçe aktarılan {workspace.reconciliation.importedRows}
              </p>
              <p>{workspace.reconciliation.balanced ? "Dengeli" : "Dengesiz"}</p>
            </section>
          ) : null}

          {workspace?.quarantine && workspace.quarantine.length > 0 ? (
            <section className="commint-panel" data-testid="bulk-onboarding-quarantine">
              <h2>Karantina</h2>
              <div className="bulk-onboarding-catalog-table">
                <div className="bulk-onboarding-catalog-table__header">
                  <span>Tablo</span>
                  <span>Anahtar</span>
                  <span>Neden</span>
                </div>
                {workspace.quarantine.map((entry) => (
                  <div key={entry.id} className="bulk-onboarding-catalog-table__row">
                    <span>{entry.sourceTable}</span>
                    <span>{entry.sourceKey}</span>
                    <span>{entry.safeReason}</span>
                  </div>
                ))}
              </div>
            </section>
          ) : null}

          {workspace?.mappingRules && workspace.mappingRules.length > 0 ? (
            <section className="commint-panel" data-testid="bulk-onboarding-mapping">
              <h2>Eşleme</h2>
              <p>{workspace.mappingRules.length} alan eşlemesi tanımlı.</p>
            </section>
          ) : null}
        </>
      ) : (
        <section className="commint-panel" data-testid="bulk-onboarding-empty">
          <p>Henüz bir toplu veri aktarım işi seçilmedi.</p>
          <p>
            Yeni müşteri kurulumunda makine ve kaynakları toplu yüklemek için aşağıdaki akışı başlatın veya örnek
            CSV şablonlarını indirin.
          </p>
        </section>
      )}
    </div>
  );
}
