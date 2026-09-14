"use client";

import { useRouter } from "next/navigation";
import { useTransition, useState } from "react";

import { Badge } from "@/components/ui/badge";
import "@/features/commercial-intelligence/components/commercial-intelligence-workspace.css";

import {
  formatCustomsNumber,
  getCustomsDocumentLabel,
  GOLDEN_DEMO_CUSTOMS_FILE_SUBTITLE,
  GOLDEN_DEMO_CUSTOMS_FILE_TITLE,
} from "../domain/golden-demo-customs-file-labels";
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

export function GoldenDemoCustomsFileWorkspaceView({
  workspace,
}: Readonly<{ workspace: GoldenDemoCustomsFileWorkspaceApi }>) {
  const router = useRouter();
  const [message, setMessage] = useState<string | null>(null);
  const [pending, startTransition] = useTransition();
  const outcomeId = workspace.outcomeId;
  const openWork = workspace.correctiveWorks.find((w) => w.status !== "completed");
  const heroClass = workspace.hero.showSuccessHero ? "commint-kpi-card" : "commint-panel";

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

  return (
    <main className="commint-page" data-testid="golden-demo-customs-file-workspace">
      <header className="commint-executive">
        <div className="commint-executive__intro">
          <p className="commint-executive__eyebrow">ALTIN DEMO · {workspace.workerCode}</p>
          <h1>{GOLDEN_DEMO_CUSTOMS_FILE_TITLE}</h1>
          <p className="commint-executive__subtitle">{GOLDEN_DEMO_CUSTOMS_FILE_SUBTITLE}</p>
          <p>{workspace.workerLabelTr}</p>
          <Badge>{workspace.statusLabel}</Badge>
        </div>
      </header>

      <section className="commint-panel" data-testid="golden-demo-customs-file-control">
        <h2>Gümrük Dosyası Kontrolü</h2>
        <p>{workspace.assignedOutcomeText}</p>
        <div className="commint-action-row">
          <button
            type="button"
            disabled={pending}
            data-testid="golden-demo-customs-file-reset"
            onClick={() => {
              if (window.confirm("Gümrük dosyası senaryosunu sıfırlamak istiyor musunuz?")) {
                run(() => goldenDemoCustomsResetAction(outcomeId));
              }
            }}
          >
            {pending ? "Sıfırlanıyor…" : "Senaryoyu Sıfırla"}
          </button>
          <button
            type="button"
            disabled={pending}
            data-testid="golden-demo-customs-file-assign"
            onClick={() => run(() => goldenDemoCustomsAssignAction(outcomeId))}
          >
            İşi Ata
          </button>
          <button
            type="button"
            disabled={pending}
            data-testid="golden-demo-customs-file-evaluate"
            onClick={() => run(() => goldenDemoCustomsEvaluateAction(outcomeId))}
          >
            Dijital Çalışanı Çalıştır
          </button>
        </div>
        {message ? <p role="status">{message}</p> : null}
        {pending ? <p role="status">İşlem devam ediyor…</p> : null}
      </section>

      <section className="commint-panel" data-testid="golden-demo-customs-file-order-context">
        <h2>Sipariş / Sevkiyat</h2>
        <p>
          {workspace.orderNumber} · {workspace.customerName} · {workspace.destinationCountry}
        </p>
        <p>
          Shipment {workspace.shipmentReference} · Net {formatCustomsNumber(workspace.shipmentNetWeightKg, 1)} kg ·
          Gross {formatCustomsNumber(workspace.shipmentGrossWeightKg, 1)} kg
        </p>
      </section>

      <section className="commint-panel" data-testid="golden-demo-customs-file-documents">
        <h2>Documents</h2>
        <table>
          <thead>
            <tr>
              <th>Document</th>
              <th>Status</th>
              <th>Version</th>
              <th>Net Weight</th>
            </tr>
          </thead>
          <tbody>
            {workspace.documents.map((doc) => (
              <tr key={doc.id} data-current={doc.isCurrent ? "true" : "false"}>
                <td>{getCustomsDocumentLabel(doc.documentType)}</td>
                <td>{doc.status.toUpperCase()}</td>
                <td>v{doc.versionNumber}</td>
                <td>{formatCustomsNumber(doc.content.netWeightKg, 1)} kg</td>
              </tr>
            ))}
          </tbody>
        </table>
      </section>

      <section className="commint-panel" data-testid="golden-demo-customs-file-blockers">
        <h2>Blocking Issues</h2>
        <p data-testid="golden-demo-customs-file-blocker-count">{workspace.hero.blockingIssueCount}</p>
        <ul>
          {workspace.blockers.map((blocker) => (
            <li key={blocker.id}>
              <strong>{blocker.blockerCode}</strong> · {blocker.status} · {blocker.reason}
            </li>
          ))}
        </ul>
      </section>

      <section className="commint-panel" data-testid="golden-demo-customs-file-corrective-work">
        <h2>Corrective Work</h2>
        <ul>
          {workspace.correctiveWorks.map((work) => (
            <li key={work.id}>
              {work.title} · {work.status} · {work.responsibleRole}
            </li>
          ))}
        </ul>
        <div className="commint-action-row">
          {openWork ? (
            <button
              type="button"
              disabled={pending || openWork.status === "completed"}
              data-testid="golden-demo-customs-file-complete-work"
              onClick={() => run(() => goldenDemoCustomsCompleteWorkAction(outcomeId, openWork.id))}
            >
              Düzeltmeyi Tamamla
            </button>
          ) : null}
          <button
            type="button"
            disabled={pending || workspace.correctiveWorks.every((w) => w.status !== "completed")}
            data-testid="golden-demo-customs-file-recheck"
            onClick={() => run(() => goldenDemoCustomsRecheckAction(outcomeId))}
          >
            Kanıtı Yeniden Kontrol Et
          </button>
        </div>
      </section>

      <section className="commint-panel" data-testid="golden-demo-customs-file-approval">
        <h2>Customs Package Approval</h2>
        <p>Approval: {workspace.hero.approvalStatus}</p>
        <button
          type="button"
          disabled={pending || workspace.approval.isApproved || workspace.hero.blockingIssueCount > 0}
          data-testid="golden-demo-customs-file-approve"
          onClick={() => run(() => goldenDemoCustomsApproveAction(outcomeId))}
        >
          Paketi Onayla
        </button>
      </section>

      <section className="commint-panel" data-testid="golden-demo-customs-file-package">
        <h2>Customs Clearance Package</h2>
        {workspace.package ? (
          <>
            <p data-testid="golden-demo-customs-file-package-id">{workspace.package.artifactLabel}</p>
            <ul>
              {workspace.package.documentManifest.map((entry) => (
                <li key={entry}>{entry}</li>
              ))}
            </ul>
          </>
        ) : (
          <button
            type="button"
            disabled={pending || !workspace.approval.isApproved}
            data-testid="golden-demo-customs-file-assemble"
            onClick={() => run(() => goldenDemoCustomsAssembleAction(outcomeId))}
          >
            Paketi Birleştir
          </button>
        )}
      </section>

      <section className="commint-panel" data-testid="golden-demo-customs-file-activity">
        <h2>Digital Worker Execution Trace</h2>
        <ul>
          {workspace.activityTrace.map((item) => (
            <li key={`${item.stageCode}-${item.occurredAtUtc}`}>
              <strong>{item.stageLabel}</strong> · {item.action} · {item.status}
            </li>
          ))}
        </ul>
      </section>

      <section
        className={heroClass}
        data-testid="golden-demo-customs-file-hero"
        data-verified={workspace.hero.showSuccessHero ? "true" : "false"}
      >
        <h1>{workspace.hero.headline}</h1>
        <p>{workspace.hero.verificationResult}</p>
        <p data-testid="golden-demo-customs-file-documents-ready">
          Documents {workspace.hero.documentsReadyCount} / {workspace.hero.documentsTotalCount} READY
        </p>
        <p>Blocking Issues {workspace.hero.blockingIssueCount}</p>
        <p>Consistency {workspace.hero.consistencyStatus}</p>
        <p>Approval {workspace.hero.approvalStatus}</p>
        <p data-testid="golden-demo-customs-file-broker-handoff">
          Customs Broker Handoff {workspace.hero.brokerHandoffStatus}
        </p>
        <button
          type="button"
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
      </section>
    </main>
  );
}
