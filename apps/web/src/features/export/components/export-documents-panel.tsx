"use client";

import { useCallback, useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import {
  CheckCircle2,
  FilePlus2,
  FileText,
  XCircle,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";

import type { ExportDocument, ExportDocumentStatus } from "../domain/export-case";
import {
  getExportDocumentStatusLabel,
  getExportDocumentTypeLabel,
} from "../domain/export-case";
import type {
  registerExportDocumentAction,
  rejectExportDocumentAction,
  verifyExportDocumentAction,
} from "../server/export-case-writer";
import { customerMessage } from "@/lib/presentation/customer-message";

const DOCUMENT_TYPE_OPTIONS = [
  "commercial-invoice",
  "packing-list",
  "customer-purchase-order",
  "certificate-of-origin",
  "circulation-document",
  "transport-document",
  "material-certificate",
  "quality-certificate",
  "customs-document",
  "other",
] as const;

type BadgeTone =
  | "neutral"
  | "information"
  | "success"
  | "warning"
  | "danger"
  | "ai";

function getDocumentStatusTone(status: ExportDocumentStatus): BadgeTone {
  switch (status) {
    case "verified":
      return "success";
    case "rejected":
      return "danger";
    case "received":
      return "information";
    case "draft":
      return "warning";
    case "missing":
    default:
      return "neutral";
  }
}

function formatUtcDate(iso: string | undefined): string {
  if (!iso) return "—";

  try {
    return new Date(iso).toLocaleDateString("tr-TR", {
      day: "2-digit",
      month: "short",
      year: "numeric",
    });
  } catch {
    return iso;
  }
}

type ExportDocumentsPanelProps = Readonly<{
  exportCaseId: string;
  documents: readonly ExportDocument[];
  onRegister: typeof registerExportDocumentAction;
  onVerify: typeof verifyExportDocumentAction;
  onReject: typeof rejectExportDocumentAction;
}>;

type PanelState =
  | { kind: "idle" }
  | { kind: "pending"; action: string }
  | { kind: "error"; message: string }
  | { kind: "success"; message: string };

export function ExportDocumentsPanel({
  exportCaseId,
  documents,
  onRegister,
  onVerify,
  onReject,
}: ExportDocumentsPanelProps) {
  const router = useRouter();
  const [state, setState] = useState<PanelState>({ kind: "idle" });
  const [documentType, setDocumentType] = useState<string>("commercial-invoice");
  const [displayName, setDisplayName] = useState("");
  const [rejectReasons, setRejectReasons] = useState<Record<string, string>>({});

  const isPending = state.kind === "pending";

  const handleRegister = useCallback(
    async (event: FormEvent) => {
      event.preventDefault();
      setState({ kind: "pending", action: "register" });

      const result = await onRegister(exportCaseId, {
        documentType,
        displayName,
      });

      if (result.status === "failure") {
        setState({ kind: "error", message: result.message });
        return;
      }

      setDisplayName("");
      setState({ kind: "success", message: "Belge kaydedildi." });
      router.refresh();
    },
    [documentType, displayName, exportCaseId, onRegister, router],
  );

  const handleVerify = useCallback(
    async (document: ExportDocument) => {
      setState({ kind: "pending", action: `verify-${document.id}` });

      const result = await onVerify(
        exportCaseId,
        document.id,
        document.version,
      );

      if (result.status === "failure") {
        setState({ kind: "error", message: result.message });
        return;
      }

      setState({ kind: "success", message: "Belge doğrulandı." });
      router.refresh();
    },
    [exportCaseId, onVerify, router],
  );

  const handleReject = useCallback(
    async (document: ExportDocument) => {
      const reason = (rejectReasons[document.id] ?? "").trim();

      if (!reason) {
        setState({
          kind: "error",
          message: "Red nedeni zorunludur.",
        });
        return;
      }

      setState({ kind: "pending", action: `reject-${document.id}` });

      const result = await onReject(
        exportCaseId,
        document.id,
        document.version,
        reason,
      );

      if (result.status === "failure") {
        setState({ kind: "error", message: result.message });
        return;
      }

      setRejectReasons((prev) => {
        const next = { ...prev };
        delete next[document.id];
        return next;
      });
      setState({ kind: "success", message: "Belge reddedildi." });
      router.refresh();
    },
    [exportCaseId, onReject, rejectReasons, router],
  );

  return (
    <section className="export-documents-panel">
      <div className="export-documents-panel__heading">
        <div>
          <p>BELGELER</p>
          <h2>İhracat belgeleri</h2>
          <span>
            Belgeleri kaydedin, doğrulayın veya reddedin. Doğrulama ve red
            işlemleri belge sürümü ile korunur.
          </span>
        </div>
      </div>

      <form className="export-documents-panel__register" onSubmit={handleRegister}>
        <div className="export-documents-panel__register-title">
          <FilePlus2 aria-hidden="true" />
          <strong>Yeni belge kaydet</strong>
        </div>

        <div className="export-documents-panel__register-fields">
          <label>
            Belge türü
            <select
              className="input"
              value={documentType}
              onChange={(e) => setDocumentType(e.target.value)}
              disabled={isPending}
            >
              {DOCUMENT_TYPE_OPTIONS.map((type) => (
                <option key={type} value={type}>
                  {getExportDocumentTypeLabel(type)}
                </option>
              ))}
            </select>
          </label>

          <label>
            Görünen ad <span className="zx-required" aria-hidden="true">*</span>
            <input
              className="input"
              value={displayName}
              onChange={(e) => setDisplayName(e.target.value)}
              placeholder="Örn. Ticari fatura — REV-01"
              disabled={isPending}
              required
            />
          </label>
        </div>

        <button
          type="submit"
          className="button button-primary button-small"
          disabled={isPending || !displayName.trim()}
        >
          {state.kind === "pending" && state.action === "register"
            ? "Kaydediliyor…"
            : "Belgeyi kaydet"}
        </button>
      </form>

      {state.kind === "error" && (
        <div className="export-documents-panel__flash export-documents-panel__flash--error" role="alert">
          {customerMessage(state.message)}
        </div>
      )}

      {state.kind === "success" && (
        <div className="export-documents-panel__flash export-documents-panel__flash--success" role="status">
          {customerMessage(state.message)}
        </div>
      )}

      {documents.length === 0 ? (
        <div className="export-documents-panel__empty" role="status">
          <FileText aria-hidden="true" />
          <p>Henüz kayıtlı belge yok.</p>
        </div>
      ) : (
        <ul className="export-documents-panel__list">
          {documents.map((document) => {
            const canVerify =
              document.documentStatus === "draft" ||
              document.documentStatus === "received";
            const canReject =
              document.documentStatus !== "verified" &&
              document.documentStatus !== "rejected";

            return (
              <li key={document.id} className="export-documents-panel__item">
                <div className="export-documents-panel__item-main">
                  <FileText aria-hidden="true" />
                  <div>
                    <strong>{document.displayName}</strong>
                    <span>
                      {getExportDocumentTypeLabel(document.documentType)} · v
                      {document.version} · {formatUtcDate(document.updatedAtUtc)}
                    </span>
                    {document.rejectionReason ? (
                      <small>Red: {document.rejectionReason}</small>
                    ) : null}
                  </div>
                  <Badge tone={getDocumentStatusTone(document.documentStatus)}>
                    {getExportDocumentStatusLabel(document.documentStatus)}
                  </Badge>
                </div>

                {(canVerify || canReject) && (
                  <div className="export-documents-panel__item-actions">
                    {canVerify && (
                      <button
                        type="button"
                        className="button button-primary button-small"
                        disabled={isPending}
                        onClick={() => void handleVerify(document)}
                      >
                        <CheckCircle2 aria-hidden="true" />
                        Doğrula
                      </button>
                    )}

                    {canReject && (
                      <div className="export-documents-panel__reject">
                        <input
                          className="input"
                          placeholder="Red nedeni"
                          value={rejectReasons[document.id] ?? ""}
                          onChange={(e) =>
                            setRejectReasons((prev) => ({
                              ...prev,
                              [document.id]: e.target.value,
                            }))
                          }
                          disabled={isPending}
                        />
                        <button
                          type="button"
                          className="button button-destructive button-small"
                          disabled={isPending}
                          onClick={() => void handleReject(document)}
                        >
                          <XCircle aria-hidden="true" />
                          Reddet
                        </button>
                      </div>
                    )}
                  </div>
                )}
              </li>
            );
          })}
        </ul>
      )}
    </section>
  );
}
