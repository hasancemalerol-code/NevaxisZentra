"use client";

import Link from "next/link";
import { useRouter } from "next/navigation";
import { useState, useTransition } from "react";
import { RefreshCw, ShieldCheck } from "lucide-react";

import { Badge } from "@/components/ui/badge";
import { Card } from "@/components/ui/card";
import type { MaintenanceAiDraftDetail } from "../domain/maintenance-ai-agent";
import {
  getMaintenanceAiConfidenceLabel,
  getMaintenanceAiDraftStatusLabel,
  getMaintenanceAiPriorityLabel,
  getMaintenanceAiRequestTypeLabel,
  MAINTENANCE_AI_LOCAL_PROVIDER_DISCLAIMER,
  MAINTENANCE_AI_RULES_BASED_LABEL,
  MAINTENANCE_AI_SAFETY_COPY,
} from "../presentation/maintenance-ai-agent-labels";
import {
  approveMaintenanceAiDraftAction,
  generateMaintenanceAiDraftAction,
  rejectMaintenanceAiDraftAction,
  updateMaintenanceAiDraftAction,
} from "../server/maintenance-ai-agent-writer";
import { MaintenanceSectionHeader } from "./maintenance-table-primitives";

type MaintenanceAiAssistantSectionProps = Readonly<{
  assetReference: string;
  draft?: MaintenanceAiDraftDetail;
  draftError?: string;
  canManage: boolean;
}>;

export function MaintenanceAiAssistantSection({
  assetReference,
  draft,
  draftError,
  canManage,
}: MaintenanceAiAssistantSectionProps) {
  const router = useRouter();
  const [isPending, startTransition] = useTransition();
  const [message, setMessage] = useState<string | null>(null);
  const [editTitle, setEditTitle] = useState(draft?.humanEditedTitle ?? draft?.suggestedTitle ?? "");
  const [editDescription, setEditDescription] = useState(draft?.humanEditedDescription ?? draft?.suggestedDescription ?? "");
  const [rejectReason, setRejectReason] = useState("");

  const effectiveTitle = draft?.humanEditedTitle ?? draft?.suggestedTitle ?? "—";
  const effectiveDescription = draft?.humanEditedDescription ?? draft?.suggestedDescription ?? "—";
  const effectivePriority = draft?.humanEditedPriority ?? draft?.suggestedPriority ?? "—";
  const effectiveType = draft?.humanEditedRequestType ?? draft?.suggestedRequestType ?? "—";

  function refresh() {
    startTransition(() => {
      router.refresh();
    });
  }

  function run(action: () => Promise<{ status: string; message?: string }>) {
    setMessage(null);
    startTransition(async () => {
      const result = await action();
      setMessage(result.status === "success" ? "İşlem tamamlandı." : (result.message ?? "İşlem başarısız."));
      refresh();
    });
  }

  return (
    <Card className="maintenance-ai-section">
      <MaintenanceSectionHeader
        title="AI Bakım Asistanı"
        subtitle="Güvenilir bakım kanıtına dayalı bakım talebi taslağı — yalnızca yetkili onay sonrası oluşturulur."
      />

      <ul className="maintenance-ai-safety-list">
        {MAINTENANCE_AI_SAFETY_COPY.map((line) => (
          <li key={line}>{line}</li>
        ))}
      </ul>

      {draft?.isDevelopmentProvider ? (
        <p className="maintenance-ai-disclaimer">{MAINTENANCE_AI_LOCAL_PROVIDER_DISCLAIMER}</p>
      ) : null}

      {draftError ? <p className="maintenance-ai-message error">{draftError}</p> : null}
      {message ? <p className="maintenance-ai-message">{message}</p> : null}

      {draft ? (
        <div className="maintenance-ai-grid">
          <div className="maintenance-ai-meta">
            <Badge tone="neutral">{getMaintenanceAiDraftStatusLabel(draft.status)}</Badge>
            <Badge tone="neutral">Kanıt Güveni: {getMaintenanceAiConfidenceLabel(draft.qualitativeConfidence)}</Badge>
            <span className="maintenance-ai-meta-line">
              Sağlayıcı: {draft.isDevelopmentProvider ? MAINTENANCE_AI_RULES_BASED_LABEL : draft.providerIdentifier}
            </span>
            <span className="maintenance-ai-meta-line">Şablon: {draft.promptVersion}</span>
          </div>

          {draft.status === "stale" ? (
            <p className="maintenance-ai-stale-warning">Taslak güncelliğini yitirdi. Güncel verilerle yeniden hazırlayın.</p>
          ) : null}

          <div className="maintenance-ai-block">
            <h4>AI Özeti</h4>
            <p>{draft.validatedOutput.summary}</p>
            <p className="maintenance-ai-muted">{draft.validatedOutput.uncertaintyExplanation}</p>
          </div>

          <div className="maintenance-ai-block">
            <h4>Önerilen Bakım Talebi</h4>
            <p><strong>{effectiveTitle}</strong></p>
            <p className="maintenance-ai-pre">{effectiveDescription}</p>
            <p>Öncelik: {getMaintenanceAiPriorityLabel(effectivePriority)} · Kategori: {getMaintenanceAiRequestTypeLabel(effectiveType)}</p>
          </div>

          {draft.validatedOutput.missingInformation.length > 0 ? (
            <div className="maintenance-ai-block">
              <h4>Eksik Bilgi</h4>
              <ul>{draft.validatedOutput.missingInformation.map((item) => <li key={item}>{item}</li>)}</ul>
            </div>
          ) : null}

          {draft.validatedOutput.evidenceReferences.length > 0 ? (
            <div className="maintenance-ai-block">
              <h4>Destekleyici Kanıt</h4>
              <ul>{draft.validatedOutput.evidenceReferences.map((e) => <li key={`${e.evidenceType}-${e.evidenceReference}`}>{e.summary}</li>)}</ul>
            </div>
          ) : null}

          {draft.createdMaintenanceRequestNumber ? (
            <p className="maintenance-ai-link-row">
              Oluşturulan talep:{" "}
              <Link href="/bakim">{draft.createdMaintenanceRequestNumber}</Link>
            </p>
          ) : null}

          {canManage && (draft.status === "draft" || draft.status === "under-review") ? (
            <div className="maintenance-ai-actions">
              <div className="maintenance-ai-edit">
                <label htmlFor="ai-draft-title">Başlık</label>
                <input id="ai-draft-title" value={editTitle} onChange={(e) => setEditTitle(e.target.value)} />
                <label htmlFor="ai-draft-description">Açıklama</label>
                <textarea id="ai-draft-description" value={editDescription} onChange={(e) => setEditDescription(e.target.value)} rows={4} />
              </div>
              <div className="maintenance-ai-buttons">
                <button type="button" disabled={isPending} onClick={() => run(() => updateMaintenanceAiDraftAction({ draftId: draft.id, expectedVersion: draft.version, title: editTitle, description: editDescription }))}>
                  Taslağı Düzenle
                </button>
                <button type="button" className="primary" disabled={isPending} onClick={() => run(() => approveMaintenanceAiDraftAction({ draftId: draft.id, expectedVersion: draft.version }))}>
                  <ShieldCheck aria-hidden="true" /> Onayla ve Bakım Talebi Oluştur
                </button>
                <input placeholder="Red gerekçesi" value={rejectReason} onChange={(e) => setRejectReason(e.target.value)} />
                <button type="button" disabled={isPending || !rejectReason.trim()} onClick={() => run(() => rejectMaintenanceAiDraftAction({ draftId: draft.id, expectedVersion: draft.version, rejectionReason: rejectReason }))}>
                  Reddet
                </button>
                <button type="button" disabled={isPending} onClick={() => run(() => generateMaintenanceAiDraftAction(assetReference))}>
                  <RefreshCw aria-hidden="true" /> Güncel Verilerle Yeniden Hazırla
                </button>
              </div>
            </div>
          ) : null}

          {!canManage ? <p className="maintenance-ai-muted">Salt okunur erişim: taslak oluşturma ve onay devre dışı.</p> : null}
        </div>
      ) : (
        <div className="maintenance-ai-empty">
          <p>Seçili varlık için AI bakım taslağı bulunmuyor.</p>
          {canManage ? (
            <button type="button" className="primary" disabled={isPending} onClick={() => run(() => generateMaintenanceAiDraftAction(assetReference))}>
              AI Taslağı Hazırla
            </button>
          ) : null}
        </div>
      )}
    </Card>
  );
}
