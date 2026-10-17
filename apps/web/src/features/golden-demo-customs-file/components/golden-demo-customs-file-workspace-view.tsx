"use client";

import { useRouter } from "next/navigation";
import { useTransition, useState } from "react";

import "@/features/golden-demo-p1/components/golden-demo-p1-workspace.css";

import { PresentationModeReturnLink } from "@/features/presentation-mode/components/presentation-mode-return-link";
import {
  formatCustomsNumber,
  GOLDEN_DEMO_CUSTOMS_FILE_SUBTITLE,
  GOLDEN_DEMO_CUSTOMS_FILE_TITLE,
} from "../domain/golden-demo-customs-file-labels";
import {
  buildCustomsEvidenceSources,
  formatCustomsApprovalAuthority,
  formatCustomsApprovalTimestamp,
  getCustomsBlockerTitle,
  getCustomsDemoStep,
  getCustomsDocumentChecklist,
  getCustomsPrimaryOutcomeLabel,
  getCustomsStatusChipLabel,
  getCustomsSupportingCopy,
  getCustomsWorkflowPhase,
  translateCustomsApiText,
  workflowStepClass,
} from "../domain/golden-demo-customs-file-presentation-copy";
import type { GoldenDemoCustomsFileWorkspaceApi } from "../server/golden-demo-customs-file-api-client";
import {
  goldenDemoCustomsApproveAction,
  goldenDemoCustomsAssembleAction,
  goldenDemoCustomsAssignAction,
  goldenDemoCustomsCompleteWorkAction,
  goldenDemoCustomsEvaluateAction,
  goldenDemoCustomsRecheckAction,
  goldenDemoCustomsResetAction,
  goldenDemoCustomsVerifyAction,
} from "../server/golden-demo-customs-file-actions";

import "./golden-demo-customs-file-workspace.css";
import { customerMessage } from "@/lib/presentation/customer-message";

function checklistItemClass(icon: string): string {
  if (icon === "✓") {
    return "golden-demo-customs-checklist__item golden-demo-customs-checklist__item--ready";
  }
  if (icon === "!") {
    return "golden-demo-customs-checklist__item golden-demo-customs-checklist__item--review";
  }
  return "golden-demo-customs-checklist__item golden-demo-customs-checklist__item--pending";
}

export function GoldenDemoCustomsFileWorkspaceView({
  workspace,
}: Readonly<{ workspace: GoldenDemoCustomsFileWorkspaceApi }>) {
  const router = useRouter();
  const [message, setMessage] = useState<string | null>(null);
  const [pending, startTransition] = useTransition();
  const outcomeId = workspace.outcomeId;
  const openWork = workspace.correctiveWorks.find((work) => work.status !== "completed");
  const activePhase = getCustomsWorkflowPhase(workspace.status);
  const { stepNumber, totalSteps } = getCustomsDemoStep(workspace.status);
  const primaryOutcome = getCustomsPrimaryOutcomeLabel(workspace);
  const evidenceSources = buildCustomsEvidenceSources(workspace);
  const checklist = getCustomsDocumentChecklist(workspace);
  const approvalRequestedAt = workspace.activityTrace.find((item) =>
    item.stageCode.includes("approval-requested"),
  )?.occurredAtUtc;

  function run(action: () => Promise<{ status: string; message?: string }>) {
    setMessage(null);
    startTransition(async () => {
      const result = await action();
      if (result.status === "failure") {
        setMessage(result.message ?? "İşlem başarısız.");
      } else {
        setMessage("İşlem tamamlandı.");
        router.refresh();
      }
    });
  }

  function resetDemo() {
    if (window.confirm("Gümrük dosyası senaryosunu sıfırlamak istiyor musunuz?")) {
      run(() => goldenDemoCustomsResetAction(outcomeId));
    }
  }

  return (
    <div className="commint-page golden-demo-p1-page golden-demo-customs-page" data-testid="golden-demo-customs-file-workspace">
      <PresentationModeReturnLink />
      <header className="golden-demo-status-bar" data-testid="golden-demo-customs-file-control">
        <div className="golden-demo-status-bar__intro">
          <p className="commint-executive__eyebrow">GÜMRÜK DOSYASI · ALTIN DEMO</p>
          <p className="golden-demo-status-bar__step">
            Adım {stepNumber} / {totalSteps}
          </p>
          <span className="golden-demo-status-bar__active">{getCustomsStatusChipLabel(workspace)}</span>
        </div>
        <div className="golden-demo-status-bar__actions">
          <button
            type="button"
            className="commint-button"
            disabled={pending}
            data-testid="golden-demo-customs-file-reset"
            onClick={resetDemo}
          >
            {pending ? "Sıfırlanıyor…" : "Senaryoyu Sıfırla"}
          </button>
        </div>
        {message ? (
          <p
            role="status"
            className={`golden-demo-feedback ${message.includes("başarısız") ? "golden-demo-feedback--error" : "golden-demo-feedback--success"}`}
          >
            {customerMessage(message)}
          </p>
        ) : null}
      </header>

      <section className="golden-demo-hero" data-testid="golden-demo-customs-file-hero">
        <div className="golden-demo-hero__content">
          <p className="commint-executive__eyebrow">İHRACAT / SEVKİYAT İHTİYACI</p>
          <h2 className="golden-demo-hero__title">{GOLDEN_DEMO_CUSTOMS_FILE_TITLE}</h2>
          <p className="golden-demo-hero__context">
            {workspace.orderNumber} · {workspace.customerName} · {translateCustomsApiText(workspace.destinationCountry)}
          </p>
          <p className="golden-demo-hero__copy">{getCustomsSupportingCopy(workspace)}</p>
          <p className="commint-panel__hint">{GOLDEN_DEMO_CUSTOMS_FILE_SUBTITLE}</p>
        </div>
        <div className="golden-demo-hero__value">
          <p className="golden-demo-hero__value-label">Dosya Hazırlık Durumu</p>
          <p
            className={`golden-demo-customs-hero__outcome${workspace.hero.showSuccessHero ? " golden-demo-customs-hero__outcome--success" : ""}`}
            data-verified={workspace.hero.showSuccessHero ? "true" : "false"}
          >
            {primaryOutcome}
          </p>
          <span className="golden-demo-hero__status">{workspace.workerLabelTr}</span>
        </div>
      </section>

      <section className="commint-panel golden-demo-machine-panel" data-testid="golden-demo-customs-file-order-context">
        <div className="golden-demo-machine-panel__header">
          <h2 className="commint-panel__title">Sipariş / Sevkiyat Özeti</h2>
        </div>
        <div className="golden-demo-machine-grid">
          <article className="golden-demo-metric golden-demo-machine-grid__emphasis golden-demo-metric--emphasis">
            <p className="golden-demo-metric__label">Sipariş</p>
            <p className="golden-demo-metric__value">{workspace.orderNumber}</p>
          </article>
          <article className="golden-demo-metric">
            <p className="golden-demo-metric__label">Sevkiyat</p>
            <p className="golden-demo-metric__value">{workspace.shipmentReference}</p>
          </article>
          <article className="golden-demo-metric">
            <p className="golden-demo-metric__label">Ülke</p>
            <p className="golden-demo-metric__value">{translateCustomsApiText(workspace.destinationCountry)}</p>
          </article>
          <article className="golden-demo-metric">
            <p className="golden-demo-metric__label">Müşteri</p>
            <p className="golden-demo-metric__value">{workspace.customerName}</p>
          </article>
          <article className="golden-demo-metric">
            <p className="golden-demo-metric__label">Net Ağırlık</p>
            <p className="golden-demo-metric__value">{formatCustomsNumber(workspace.shipmentNetWeightKg, 1)} kg</p>
          </article>
          <article className="golden-demo-metric">
            <p className="golden-demo-metric__label">Brüt Ağırlık</p>
            <p className="golden-demo-metric__value">{formatCustomsNumber(workspace.shipmentGrossWeightKg, 1)} kg</p>
          </article>
          <article className="golden-demo-metric">
            <p className="golden-demo-metric__label">Paket / Incoterm</p>
            <p className="golden-demo-metric__value">
              {workspace.packageCount} · {workspace.incoterm}
            </p>
          </article>
          <article className="golden-demo-metric">
            <p className="golden-demo-metric__label">Belgeler Hazır</p>
            <p className="golden-demo-metric__value" data-testid="golden-demo-customs-file-documents-ready">
              {workspace.hero.documentsReadyCount} / {workspace.hero.documentsTotalCount}
            </p>
          </article>
        </div>
      </section>

      <section className="golden-demo-workflow-rail">
        <article className={workflowStepClass(activePhase, "tespit", workspace.status)}>
          <p className="golden-demo-workflow-rail__label">01 · TESPİT</p>
          <p className="golden-demo-workflow-rail__title">Belge ve veri kontrolü</p>
          <p className="golden-demo-workflow-rail__detail">Gümrük Dosyası Dijital Çalışanı gerekli belgeleri toplar.</p>
        </article>
        <article className={workflowStepClass(activePhase, "karar", workspace.status)}>
          <p className="golden-demo-workflow-rail__label">02 · KARAR</p>
          <p className="golden-demo-workflow-rail__title">Eksik / tutarsızlık değerlendirmesi</p>
          <p className="golden-demo-workflow-rail__detail">Tutarsızlıklar ve eksik belgeler değerlendirilir.</p>
        </article>
        <article className={workflowStepClass(activePhase, "aksiyon", workspace.status)}>
          <p className="golden-demo-workflow-rail__label">03 · AKSİYON</p>
          <p className="golden-demo-workflow-rail__title">Dosya hazırlama koordinasyonu</p>
          <p className="golden-demo-workflow-rail__detail">Düzeltme ve onay adımları yürütülür.</p>
        </article>
        <article className={workflowStepClass(activePhase, "dogrulama", workspace.status)}>
          <p className="golden-demo-workflow-rail__label">04 · DOĞRULAMA</p>
          <p className="golden-demo-workflow-rail__title">Müşavir devrine hazır dosya</p>
          <p className="golden-demo-workflow-rail__detail">Kanıtlar doğrulanır ve dosya kapatılır.</p>
        </article>
      </section>

      <section className="commint-panel" data-testid="golden-demo-customs-file-documents">
        <h2 className="commint-panel__title">Belge ve Kontrol Listesi</h2>
        <ul className="golden-demo-customs-checklist">
          {checklist.map((item) => (
            <li key={item.key} className={checklistItemClass(item.icon)}>
              <span className="golden-demo-customs-checklist__icon" aria-hidden="true">
                {item.icon}
              </span>
              <span>{item.name}</span>
              <span className="golden-demo-status-chip">{item.statusLabel}</span>
            </li>
          ))}
        </ul>
      </section>

      {workspace.blockers.length > 0 ? (
        <section className="commint-panel" data-testid="golden-demo-customs-file-blockers">
          <h2 className="commint-panel__title">Engelleyici Sorunlar</h2>
          <p data-testid="golden-demo-customs-file-blocker-count">{workspace.hero.blockingIssueCount} açık sorun</p>
          <ul className="golden-demo-customs-blocker-list">
            {workspace.blockers.map((blocker) => (
              <li key={blocker.id} className="golden-demo-customs-blocker">
                <strong>{getCustomsBlockerTitle(blocker.blockerCode)}</strong>
                {translateCustomsApiText(blocker.reason)} · {translateCustomsApiText(blocker.status)}
              </li>
            ))}
          </ul>
        </section>
      ) : null}

      {workspace.correctiveWorks.length > 0 ? (
        <section className="commint-panel" data-testid="golden-demo-customs-file-corrective-work">
          <h2 className="commint-panel__title">Düzeltme İşleri</h2>
          <ul className="golden-demo-customs-checklist">
            {workspace.correctiveWorks.map((work) => (
              <li key={work.id} className="golden-demo-customs-checklist__item">
                <span className="golden-demo-customs-checklist__icon" aria-hidden="true">
                  {work.status === "completed" ? "✓" : "⏳"}
                </span>
                <span>
                  {translateCustomsApiText(work.title)} · {formatCustomsApprovalAuthority(work.responsibleRole)}
                </span>
                <span className="golden-demo-status-chip">{translateCustomsApiText(work.status)}</span>
              </li>
            ))}
          </ul>
          <div className="golden-demo-action-row">
            {openWork ? (
              <button
                type="button"
                className="commint-button commint-button--primary"
                disabled={pending || openWork.status === "completed"}
                data-testid="golden-demo-customs-file-complete-work"
                onClick={() => run(() => goldenDemoCustomsCompleteWorkAction(outcomeId, openWork.id))}
              >
                Düzeltmeyi Tamamla
              </button>
            ) : null}
            <button
              type="button"
              className="commint-button"
              disabled={pending || workspace.correctiveWorks.every((work) => work.status !== "completed")}
              data-testid="golden-demo-customs-file-recheck"
              onClick={() => run(() => goldenDemoCustomsRecheckAction(outcomeId))}
            >
              Kanıtı Yeniden Kontrol Et
            </button>
          </div>
        </section>
      ) : null}

      <section
        className={`golden-demo-approval-card${workspace.approval.isApproved ? " golden-demo-approval-card--approved" : ""}`}
        data-testid="golden-demo-customs-file-approval"
      >
        <div className="golden-demo-approval-card__header">
          <div className="golden-demo-approval-card__header-copy">
            <p className="golden-demo-approval-card__eyebrow">
              {workspace.approval.isApproved ? "ONAY KAYDI" : "ONAY GEREKLİ"}
            </p>
            {workspace.approval.isApproved ? (
              <span className="golden-demo-status-chip golden-demo-status-chip--approval-granted">ONAYLANDI</span>
            ) : workspace.approval.isApprovalRequired ? (
              <span className="golden-demo-status-chip golden-demo-status-chip--approval-pending">İNSAN ONAYI BEKLİYOR</span>
            ) : null}
          </div>
          <p className="golden-demo-approval-card__boundary-note">
            Gümrük Dosyası Dijital Çalışanı güvenli aksiyon sınırına ulaştı; yetkili onay olmadan müşavir devri yapılamaz.
          </p>
        </div>

        <div className="golden-demo-approval-card__layout">
          <div className="golden-demo-approval-card__main">
            <h2 className="golden-demo-approval-card__title">Kontrollü Gümrük Dosyası Onayı</h2>
            <p className="golden-demo-approval-card__message">
              {translateCustomsApiText(workspace.verification.summary)}
            </p>

            <ol className="golden-demo-approval-story" aria-label="Gümrük dosyası karar akışı">
              <li className="golden-demo-approval-story__step golden-demo-approval-story__step--complete">
                <span>Belgeler toplandı</span>
              </li>
              <li className="golden-demo-approval-story__step golden-demo-approval-story__step--complete">
                <span>Tutarsızlık kontrol edildi</span>
              </li>
              <li
                className={`golden-demo-approval-story__step ${
                  workspace.approval.isApproved
                    ? "golden-demo-approval-story__step--complete"
                    : "golden-demo-approval-story__step--active"
                }`}
              >
                <span>{workspace.approval.isApproved ? "Onay verildi" : "Onay istendi"}</span>
              </li>
              <li
                className={`golden-demo-approval-story__step ${
                  workspace.package ? "golden-demo-approval-story__step--complete" : "golden-demo-approval-story__step--pending"
                }`}
              >
                <span>Müşavir devrine hazırlık</span>
              </li>
            </ol>

            <div className="golden-demo-approval-kpi-grid">
              <article className="golden-demo-approval-kpi">
                <p className="golden-demo-approval-kpi__label">Belgeler Hazır</p>
                <p className="golden-demo-approval-kpi__value">
                  {workspace.hero.documentsReadyCount} / {workspace.hero.documentsTotalCount}
                </p>
              </article>
              <article className="golden-demo-approval-kpi">
                <p className="golden-demo-approval-kpi__label">Engelleyici Sorun</p>
                <p className="golden-demo-approval-kpi__value">{workspace.hero.blockingIssueCount}</p>
              </article>
              <article className="golden-demo-approval-kpi">
                <p className="golden-demo-approval-kpi__label">Tutarlılık</p>
                <p className="golden-demo-approval-kpi__value">
                  {translateCustomsApiText(workspace.hero.consistencyStatus)}
                </p>
              </article>
              <article className="golden-demo-approval-kpi">
                <p className="golden-demo-approval-kpi__label">Onay Durumu</p>
                <p className="golden-demo-approval-kpi__value">
                  {translateCustomsApiText(workspace.hero.approvalStatus)}
                </p>
              </article>
              <article className="golden-demo-approval-kpi golden-demo-approval-kpi--emphasis">
                <p className="golden-demo-approval-kpi__label">Müşavir Devri</p>
                <p className="golden-demo-approval-kpi__value" data-testid="golden-demo-customs-file-broker-handoff">
                  {translateCustomsApiText(workspace.hero.brokerHandoffStatus)}
                </p>
              </article>
            </div>

            <div className="golden-demo-approval-guardrail">
              <p className="golden-demo-approval-guardrail__eyebrow">Operasyonel Güvence</p>
              <p className="golden-demo-approval-guardrail__title">Kontrollü gümrük dosyası hazırlığı</p>
              <p className="golden-demo-approval-guardrail__detail">Doğrulama koşulu aktif</p>
            </div>

            <div className="golden-demo-evidence-pack">
              <div className="golden-demo-evidence-pack__header">
                <p className="golden-demo-evidence-pack__eyebrow">Kanıt Paketi</p>
                <p className="golden-demo-evidence-pack__summary">
                  {evidenceSources.length} doğrulanmış kanıt kaynağı dosyaya bağlandı.
                </p>
              </div>
              <ul className="golden-demo-evidence-pack__chips">
                {evidenceSources.map((source) => (
                  <li key={source}>
                    <span className="golden-demo-evidence-chip golden-demo-evidence-chip--static">
                      <span aria-hidden="true">✓</span>
                      {source}
                    </span>
                  </li>
                ))}
              </ul>
            </div>
          </div>

          <aside className="golden-demo-approval-card__aside">
            <div className="golden-demo-approval-meta">
              <div className="golden-demo-approval-meta__item">
                <p className="golden-demo-approval-meta__label">Onay Yetkisi</p>
                <p className="golden-demo-approval-meta__value">
                  {formatCustomsApprovalAuthority(workspace.approval.approvingRole)}
                </p>
              </div>
              <div className="golden-demo-approval-meta__item">
                <p className="golden-demo-approval-meta__label">Talep Eden</p>
                <p className="golden-demo-approval-meta__value">{workspace.workerLabelTr}</p>
              </div>
              <div className="golden-demo-approval-meta__item">
                <p className="golden-demo-approval-meta__label">Talep Zamanı</p>
                <p className="golden-demo-approval-meta__value">
                  {formatCustomsApprovalTimestamp(approvalRequestedAt ?? workspace.approval.approvedAtUtc)}
                </p>
              </div>
            </div>

            {workspace.approval.isApproved ? (
              <div className="golden-demo-approval-record" data-testid="golden-demo-customs-file-approval-record">
                <p className="golden-demo-approval-record__label">Onay Durumu</p>
                <p className="golden-demo-approval-record__value">Onaylandı</p>
                <p className="golden-demo-approval-record__detail">
                  Onay zamanı: {formatCustomsApprovalTimestamp(workspace.approval.approvedAtUtc)}
                </p>
              </div>
            ) : workspace.approval.isApprovalRequired ? (
              <div className="golden-demo-approval-actions">
                <button
                  type="button"
                  className="commint-button commint-button--primary"
                  disabled={pending || workspace.hero.blockingIssueCount > 0}
                  data-testid="golden-demo-customs-file-approve"
                  onClick={() => run(() => goldenDemoCustomsApproveAction(outcomeId))}
                >
                  Onayla ve Devam Et
                </button>
              </div>
            ) : null}
          </aside>
        </div>
      </section>

      <section className="commint-panel" data-testid="golden-demo-customs-file-package">
        <h2 className="commint-panel__title">Gümrük Dosyası Paketi</h2>
        {workspace.package ? (
          <div className="golden-demo-customs-package">
            <p className="golden-demo-customs-package__label" data-testid="golden-demo-customs-file-package-id">
              {workspace.package.artifactLabel}
            </p>
            <ul>
              {workspace.package.documentManifest.map((entry) => (
                <li key={entry}>{translateCustomsApiText(entry)}</li>
              ))}
            </ul>
          </div>
        ) : (
          <button
            type="button"
            className="commint-button commint-button--primary"
            disabled={pending || !workspace.approval.isApproved}
            data-testid="golden-demo-customs-file-assemble"
            onClick={() => run(() => goldenDemoCustomsAssembleAction(outcomeId))}
          >
            Paketi Birleştir
          </button>
        )}
      </section>

      <section className="golden-demo-customs-control-panel">
        <h2>Senaryo İlerletme</h2>
        <p>{translateCustomsApiText(workspace.assignedOutcomeText)}</p>
        <div className="golden-demo-action-row">
          <button
            type="button"
            className="commint-button"
            disabled={pending}
            data-testid="golden-demo-customs-file-assign"
            onClick={() => run(() => goldenDemoCustomsAssignAction(outcomeId))}
          >
            İşi Ata
          </button>
          <button
            type="button"
            className="commint-button commint-button--primary"
            disabled={pending}
            data-testid="golden-demo-customs-file-evaluate"
            onClick={() => run(() => goldenDemoCustomsEvaluateAction(outcomeId))}
          >
            Dijital Çalışanı Çalıştır
          </button>
          <button
            type="button"
            className="commint-button commint-button--primary"
            disabled={
              pending ||
              !workspace.package ||
              workspace.approval.isApproved === false ||
              workspace.hero.blockingIssueCount > 0
            }
            data-testid="golden-demo-customs-file-verify"
            onClick={() => run(() => goldenDemoCustomsVerifyAction(outcomeId))}
          >
            Sonucu Doğrula
          </button>
        </div>
        {pending ? (
          <p role="status" className="golden-demo-feedback golden-demo-feedback--status">
            İşlem devam ediyor…
          </p>
        ) : null}
      </section>

      {workspace.activityTrace.length > 0 ? (
        <section className="commint-panel" data-testid="golden-demo-customs-file-activity">
          <h2 className="commint-panel__title">Dijital Çalışan Aktivite Zaman Çizelgesi</h2>
          <ul className="golden-demo-customs-activity">
            {workspace.activityTrace.map((item) => (
              <li key={`${item.stageCode}-${item.occurredAtUtc}`}>
                <strong>{translateCustomsApiText(item.stageLabel)}</strong>
                <span>
                  {translateCustomsApiText(item.action)} · {translateCustomsApiText(item.status)}
                </span>
              </li>
            ))}
          </ul>
        </section>
      ) : null}
    </div>
  );
}
