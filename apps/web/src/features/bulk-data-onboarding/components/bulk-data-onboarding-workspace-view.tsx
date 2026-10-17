"use client";

import { Inbox } from "lucide-react";

import {
  BULK_DATA_ONBOARDING_SUBTITLE,
  BULK_DATA_ONBOARDING_TITLE,
  BULK_ONBOARDING_WORKFLOW_STEPS,
  bulkJobStatusLabel,
  bulkProviderKindLabel,
  bulkSafetyModeLabel,
} from "../domain/bulk-data-onboarding-labels";
import type { BulkImportJobWorkspaceApi } from "../server/bulk-data-onboarding-api-client";
import { Notice } from "@/components/ui/notice";
import { PageHeader } from "@/components/ui/page";
import { BulkDataOnboardingWorkflowClient } from "./bulk-data-onboarding-workflow-client";

import "./bulk-data-onboarding-workspace.css";
import "./bulk-onboarding-workspace.css";

type BulkDataOnboardingWorkspaceViewProps = Readonly<{
  workspace: BulkImportJobWorkspaceApi | null;
  facilityId: string;
}>;

export function BulkDataOnboardingWorkspaceView({ workspace, facilityId }: BulkDataOnboardingWorkspaceViewProps) {
  const job = workspace?.job;

  return (
    <div className="bulk-onboarding-workspace" data-testid="bulk-data-onboarding-workspace">
      <PageHeader eyebrow="Ayarlar" title={BULK_DATA_ONBOARDING_TITLE} description={BULK_DATA_ONBOARDING_SUBTITLE} />
      {job ? (
        <div className="bulk-onboarding-workspace__status-row">
          <span className="bulk-onboarding-workspace__status-chip" data-testid="bulk-onboarding-job-status">
            {bulkJobStatusLabel(job.status)}
          </span>
          <span>{bulkProviderKindLabel(job.sourceProviderKind)}</span>
          <span className="bulk-onboarding-workspace__safety-mode">{bulkSafetyModeLabel(job.safetyMode)}</span>
        </div>
      ) : null}

      <section className="commint-panel bulk-onboarding-workflow-panel" data-testid="bulk-onboarding-workflow">
        <h2>İş Akışı</h2>
        <ol className="bulk-onboarding-workflow">
          {BULK_ONBOARDING_WORKFLOW_STEPS.map((step, index) => (
            <li key={step} className="bulk-onboarding-workflow__step" aria-current={!job && index === 0 ? "step" : undefined}>
              <span className="bulk-onboarding-workflow__index" aria-hidden="true">{index + 1}</span>
              <span className="bulk-onboarding-workflow__label">{step}</span>
            </li>
          ))}
        </ol>
      </section>

      <BulkDataOnboardingWorkflowClient facilityId={facilityId} initialWorkspace={workspace} />

      {job ? (
        <>
          {job.failedRows > 0 ? (
            <Notice tone="warning" alert data-testid="bulk-onboarding-failure-notice">
              {job.failedRows} kayıt içe aktarılamadı. Üst kayıtları (alan, hat, istasyon) ve karantina listesini kontrol edip yeniden deneyin.
            </Notice>
          ) : null}
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
            <article className="commint-kpi-card">
              <p className="commint-kpi-card__label">Güncellenen</p>
              <p className="commint-kpi-card__value">{job.updatedRows}</p>
            </article>
            <article className="commint-kpi-card">
              <p className="commint-kpi-card__label">Başarısız</p>
              <p className="commint-kpi-card__value" data-testid="bulk-onboarding-failed-rows">{job.failedRows}</p>
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
                Geçerli {workspace.reconciliation.validRows} · İçe aktarılan {workspace.reconciliation.importedRows} ·
                Güncellenen {workspace.reconciliation.updatedRows} · Başarısız {workspace.reconciliation.failedRows}
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
        <section className="commint-panel bulk-onboarding-empty" data-testid="bulk-onboarding-empty">
          <span className="bulk-onboarding-empty__icon" aria-hidden="true"><Inbox size={22} /></span>
          <h2>Henüz bir veri aktarım işi yok</h2>
          <p>
            Makine ve kaynakları toplu yüklemek için yukarıdan yeni bir içe aktarma başlatın ya da CSV şablonlarını
            indirin.
          </p>
        </section>
      )}
    </div>
  );
}
