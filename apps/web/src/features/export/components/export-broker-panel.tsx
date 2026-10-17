"use client";

import { useCallback, useMemo, useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import {
  BriefcaseBusiness,
  CheckCircle2,
  FileStack,
  Send,
  ShieldAlert,
} from "lucide-react";

import { Badge } from "@/components/ui/badge";

import type {
  BrokerWorkspaceStatus,
  ExportBrokerWorkspace,
} from "../domain/export-2l";
import {
  getBrokerCorrectionSeverityLabel,
  getBrokerCorrectionStatusLabel,
  getBrokerPackageVersionStatusLabel,
  getBrokerWorkspaceStatusLabel,
} from "../domain/export-2l";
import type { ExportDocument } from "../domain/export-case";
import type {
  acknowledgeBrokerAction,
  createBrokerCorrectionAction,
  createBrokerWorkspaceAction,
  prepareBrokerPackageAction,
  resolveBrokerCorrectionAction,
  sendBrokerPackageAction,
} from "../server/export-case-writer";
import { customerMessage } from "@/lib/presentation/customer-message";

type BadgeTone =
  | "neutral"
  | "information"
  | "success"
  | "warning"
  | "danger"
  | "ai";

function workspaceTone(status: BrokerWorkspaceStatus): BadgeTone {
  switch (status) {
    case "ready":
    case "acknowledged":
      return "success";
    case "sent":
    case "package-ready":
      return "information";
    case "correction-requested":
      return "warning";
    case "cancelled":
      return "danger";
    case "closed":
    case "draft":
    default:
      return "neutral";
  }
}

type PanelState =
  | { kind: "idle" }
  | { kind: "pending"; action: string }
  | { kind: "error"; message: string }
  | { kind: "success"; message: string };

type ExportBrokerPanelProps = Readonly<{
  exportCaseId: string;
  workspace: ExportBrokerWorkspace | null;
  documents: readonly ExportDocument[];
  shipmentIds: readonly string[];
  onCreateWorkspace: typeof createBrokerWorkspaceAction;
  onPreparePackage: typeof prepareBrokerPackageAction;
  onSendPackage: typeof sendBrokerPackageAction;
  onAcknowledge: typeof acknowledgeBrokerAction;
  onCreateCorrection: typeof createBrokerCorrectionAction;
  onResolveCorrection: typeof resolveBrokerCorrectionAction;
}>;

export function ExportBrokerPanel({
  exportCaseId,
  workspace,
  documents,
  shipmentIds,
  onCreateWorkspace,
  onPreparePackage,
  onSendPackage,
  onAcknowledge,
  onCreateCorrection,
  onResolveCorrection,
}: ExportBrokerPanelProps) {
  const router = useRouter();
  const [state, setState] = useState<PanelState>({ kind: "idle" });
  const [brokerCompanyName, setBrokerCompanyName] = useState("");
  const [brokerContactName, setBrokerContactName] = useState("");
  const [brokerEmail, setBrokerEmail] = useState("");
  const [selectedShipmentId, setSelectedShipmentId] = useState(
    shipmentIds[0] ?? "",
  );
  const [selectedDocumentIds, setSelectedDocumentIds] = useState<string[]>([]);
  const [brokerReference, setBrokerReference] = useState("");
  const [correctionCategory, setCorrectionCategory] = useState("document");
  const [correctionTitle, setCorrectionTitle] = useState("");
  const [correctionDescription, setCorrectionDescription] = useState("");
  const [correctionSeverity, setCorrectionSeverity] = useState("warning");
  const [resolutionNotes, setResolutionNotes] = useState<
    Record<string, string>
  >({});

  const isPending = state.kind === "pending";

  const verifiedDocuments = useMemo(
    () => documents.filter((d) => d.documentStatus === "verified"),
    [documents],
  );

  const latestPackage = workspace?.packageVersions[0] ?? null;

  const handleCreateWorkspace = useCallback(
    async (event: FormEvent) => {
      event.preventDefault();
      setState({ kind: "pending", action: "create-workspace" });

      const result = await onCreateWorkspace(exportCaseId, {
        brokerCompanyName,
        brokerContactName: brokerContactName || undefined,
        brokerEmail: brokerEmail || undefined,
        shipmentId: selectedShipmentId || undefined,
      });

      if (result.status === "failure") {
        setState({ kind: "error", message: result.message });
        return;
      }

      setBrokerCompanyName("");
      setBrokerContactName("");
      setBrokerEmail("");
      setState({ kind: "success", message: "Broker çalışma alanı oluşturuldu." });
      router.refresh();
    },
    [
      brokerCompanyName,
      brokerContactName,
      brokerEmail,
      exportCaseId,
      onCreateWorkspace,
      router,
      selectedShipmentId,
    ],
  );

  const toggleDocument = useCallback((documentId: string) => {
    setSelectedDocumentIds((prev) =>
      prev.includes(documentId)
        ? prev.filter((id) => id !== documentId)
        : [...prev, documentId],
    );
  }, []);

  const handlePrepare = useCallback(async () => {
    if (!workspace) return;
    setState({ kind: "pending", action: "prepare" });

    const result = await onPreparePackage(exportCaseId, workspace.id, {
      expectedWorkspaceVersion: workspace.version,
      documentIds: selectedDocumentIds,
    });

    if (result.status === "failure") {
      setState({ kind: "error", message: result.message });
      return;
    }

    setState({ kind: "success", message: "Broker paketi hazırlandı." });
    router.refresh();
  }, [
    exportCaseId,
    onPreparePackage,
    router,
    selectedDocumentIds,
    workspace,
  ]);

  const handleSend = useCallback(async () => {
    if (!workspace) return;
    setState({ kind: "pending", action: "send" });

    const result = await onSendPackage(
      exportCaseId,
      workspace.id,
      workspace.version,
    );

    if (result.status === "failure") {
      setState({ kind: "error", message: result.message });
      return;
    }

    setState({ kind: "success", message: "Paket broker'a gönderildi." });
    router.refresh();
  }, [exportCaseId, onSendPackage, router, workspace]);

  const handleAcknowledge = useCallback(async () => {
    if (!workspace) return;
    setState({ kind: "pending", action: "ack" });

    const result = await onAcknowledge(
      exportCaseId,
      workspace.id,
      workspace.version,
      brokerReference || undefined,
    );

    if (result.status === "failure") {
      setState({ kind: "error", message: result.message });
      return;
    }

    setBrokerReference("");
    setState({ kind: "success", message: "Broker alındı onayı kaydedildi." });
    router.refresh();
  }, [brokerReference, exportCaseId, onAcknowledge, router, workspace]);

  const handleCreateCorrection = useCallback(
    async (event: FormEvent) => {
      event.preventDefault();
      if (!workspace) return;
      setState({ kind: "pending", action: "create-correction" });

      const result = await onCreateCorrection(exportCaseId, workspace.id, {
        category: correctionCategory,
        title: correctionTitle,
        description: correctionDescription,
        severity: correctionSeverity,
      });

      if (result.status === "failure") {
        setState({ kind: "error", message: result.message });
        return;
      }

      setCorrectionTitle("");
      setCorrectionDescription("");
      setState({ kind: "success", message: "Düzeltme talebi oluşturuldu." });
      router.refresh();
    },
    [
      correctionCategory,
      correctionDescription,
      correctionSeverity,
      correctionTitle,
      exportCaseId,
      onCreateCorrection,
      router,
      workspace,
    ],
  );

  const handleResolve = useCallback(
    async (correctionId: string) => {
      if (!workspace) return;
      setState({ kind: "pending", action: `resolve-${correctionId}` });

      const result = await onResolveCorrection(
        exportCaseId,
        workspace.id,
        correctionId,
        {
          expectedWorkspaceVersion: workspace.version,
          resolution: "resolved",
          resolutionNote: resolutionNotes[correctionId]?.trim() || undefined,
        },
      );

      if (result.status === "failure") {
        setState({ kind: "error", message: result.message });
        return;
      }

      setState({ kind: "success", message: "Düzeltme çözüldü." });
      router.refresh();
    },
    [exportCaseId, onResolveCorrection, resolutionNotes, router, workspace],
  );

  return (
    <section className="export-broker-panel">
      <div className="export-broker-panel__heading">
        <div>
          <p>Gümrük Müşaviri Teslim</p>
          <h2>Gümrük müşaviri paketi</h2>
          <span>
            Paket hazırlama, teslim ve evrak düzeltme talebi takibi. Fabrika
            tarafı devir kaydıdır; gümrük müşavirliği operasyonu yönetilmez.
          </span>
        </div>
        {workspace ? (
          <Badge tone={workspaceTone(workspace.status)}>
            {getBrokerWorkspaceStatusLabel(workspace.status)}
          </Badge>
        ) : null}
      </div>

      <div className="export-broker-panel__disclaimer" role="note">
        <ShieldAlert aria-hidden="true" />
        <p>
          Bu alan gümrük danışmanlığı veya yasal uygunluk değerlendirmesi
          sağlamaz; yalnızca broker iş akışı kaydı içindir.
        </p>
      </div>

      {state.kind === "error" && (
        <div
          className="export-broker-panel__flash export-broker-panel__flash--error"
          role="alert"
        >
          {customerMessage(state.message)}
        </div>
      )}
      {state.kind === "success" && (
        <div
          className="export-broker-panel__flash export-broker-panel__flash--success"
          role="status"
        >
          {customerMessage(state.message)}
        </div>
      )}

      {!workspace ? (
        <form
          className="export-broker-panel__form"
          onSubmit={handleCreateWorkspace}
        >
          <div className="export-broker-panel__form-title">
            <BriefcaseBusiness aria-hidden="true" />
            <strong>Broker çalışma alanı oluştur</strong>
          </div>
          <div className="export-broker-panel__fields">
            <label>
              Şirket adı <span className="zx-required" aria-hidden="true">*</span>
              <input
                className="input"
                value={brokerCompanyName}
                onChange={(e) => setBrokerCompanyName(e.target.value)}
                disabled={isPending}
                required
              />
            </label>
            <label>
              İletişim
              <input
                className="input"
                value={brokerContactName}
                onChange={(e) => setBrokerContactName(e.target.value)}
                disabled={isPending}
              />
            </label>
            <label>
              E-posta
              <input
                className="input"
                type="email"
                value={brokerEmail}
                onChange={(e) => setBrokerEmail(e.target.value)}
                disabled={isPending}
              />
            </label>
            {shipmentIds.length > 0 ? (
              <label>
                Sevkiyat
                <select
                  className="input"
                  value={selectedShipmentId}
                  onChange={(e) => setSelectedShipmentId(e.target.value)}
                  disabled={isPending}
                >
                  <option value="">Bağlama</option>
                  {shipmentIds.map((id) => (
                    <option key={id} value={id}>
                      {id.slice(0, 8)}…
                    </option>
                  ))}
                </select>
              </label>
            ) : null}
          </div>
          <button
            type="submit"
            className="button button-primary button-small"
            disabled={isPending || !brokerCompanyName.trim()}
          >
            Çalışma alanı oluştur
          </button>
        </form>
      ) : (
        <>
          <div className="export-broker-panel__summary">
            <div>
              <span>Broker</span>
              <strong>{workspace.brokerCompanyName}</strong>
            </div>
            <div>
              <span>Sürüm</span>
              <strong>v{workspace.version}</strong>
            </div>
            <div>
              <span>Paketler</span>
              <strong>{workspace.packageVersions.length}</strong>
            </div>
            <div>
              <span>Düzeltmeler</span>
              <strong>{workspace.corrections.length}</strong>
            </div>
          </div>

          <div className="export-broker-panel__package">
            <div className="export-broker-panel__form-title">
              <FileStack aria-hidden="true" />
              <strong>Paket hazırla / gönder</strong>
            </div>

            {verifiedDocuments.length === 0 ? (
              <p className="export-broker-panel__hint">
                Pakete eklenecek doğrulanmış belge yok.
              </p>
            ) : (
              <ul className="export-broker-panel__doc-pick">
                {verifiedDocuments.map((doc) => (
                  <li key={doc.id}>
                    <label>
                      <input
                        type="checkbox"
                        checked={selectedDocumentIds.includes(doc.id)}
                        onChange={() => toggleDocument(doc.id)}
                        disabled={isPending}
                      />
                      {doc.displayName}
                    </label>
                  </li>
                ))}
              </ul>
            )}

            <div className="export-broker-panel__actions">
              {(workspace.status === "draft" ||
                workspace.status === "correction-requested" ||
                workspace.status === "acknowledged") && (
                <button
                  type="button"
                  className="button button-primary button-small"
                  disabled={isPending || selectedDocumentIds.length === 0}
                  onClick={() => void handlePrepare()}
                >
                  <FileStack aria-hidden="true" />
                  Paketi hazırla
                </button>
              )}
              {workspace.status === "package-ready" && (
                <button
                  type="button"
                  className="button button-primary button-small"
                  disabled={isPending}
                  onClick={() => void handleSend()}
                >
                  <Send aria-hidden="true" />
                  Paketi gönder
                </button>
              )}
              {workspace.status === "sent" && (
                <>
                  <label>
                    Broker referansı
                    <input
                      className="input"
                      value={brokerReference}
                      onChange={(e) => setBrokerReference(e.target.value)}
                      disabled={isPending}
                    />
                  </label>
                  <button
                    type="button"
                    className="button button-primary button-small"
                    disabled={isPending}
                    onClick={() => void handleAcknowledge()}
                  >
                    <CheckCircle2 aria-hidden="true" />
                    Alındı onayla
                  </button>
                </>
              )}
            </div>

            {latestPackage ? (
              <div className="export-broker-panel__package-meta">
                <span>
                  Son paket v{latestPackage.versionNumber} ·{" "}
                  {getBrokerPackageVersionStatusLabel(latestPackage.status)}
                </span>
                <span>{latestPackage.documents.length} belge</span>
              </div>
            ) : null}
          </div>

          <form
            className="export-broker-panel__form"
            onSubmit={handleCreateCorrection}
          >
            <div className="export-broker-panel__form-title">
              <ShieldAlert aria-hidden="true" />
              <strong>Düzeltme talebi</strong>
            </div>
            <div className="export-broker-panel__fields">
              <label>
                Kategori <span className="zx-required" aria-hidden="true">*</span>
                <input
                  className="input"
                  value={correctionCategory}
                  onChange={(e) => setCorrectionCategory(e.target.value)}
                  disabled={isPending}
                  required
                />
              </label>
              <label>
                Başlık <span className="zx-required" aria-hidden="true">*</span>
                <input
                  className="input"
                  value={correctionTitle}
                  onChange={(e) => setCorrectionTitle(e.target.value)}
                  disabled={isPending}
                  required
                />
              </label>
              <label>
                Önem
                <select
                  className="input"
                  value={correctionSeverity}
                  onChange={(e) => setCorrectionSeverity(e.target.value)}
                  disabled={isPending}
                >
                  <option value="info">Bilgi</option>
                  <option value="warning">Uyarı</option>
                  <option value="blocking">Blokaj</option>
                </select>
              </label>
              <label className="export-broker-panel__wide">
                Açıklama <span className="zx-required" aria-hidden="true">*</span>
                <textarea
                  className="input"
                  value={correctionDescription}
                  onChange={(e) => setCorrectionDescription(e.target.value)}
                  disabled={isPending}
                  required
                  rows={2}
                />
              </label>
            </div>
            <button
              type="submit"
              className="button button-secondary button-small"
              disabled={isPending}
            >
              Düzeltme ekle
            </button>
          </form>

          {workspace.corrections.length === 0 ? (
            <div className="export-broker-panel__empty" role="status">
              <p>Açık veya geçmiş düzeltme yok.</p>
            </div>
          ) : (
            <ul className="export-broker-panel__corrections">
              {workspace.corrections.map((correction) => (
                <li key={correction.id}>
                  <div>
                    <strong>{correction.title}</strong>
                    <span>{correction.description}</span>
                    <small>
                      {correction.category} ·{" "}
                      {getBrokerCorrectionSeverityLabel(correction.severity)} ·{" "}
                      {getBrokerCorrectionStatusLabel(correction.status)}
                    </small>
                  </div>
                  {correction.status === "open" ? (
                    <div className="export-broker-panel__resolve">
                      <input
                        className="input"
                        placeholder="Çözüm notu"
                        value={resolutionNotes[correction.id] ?? ""}
                        onChange={(e) =>
                          setResolutionNotes((prev) => ({
                            ...prev,
                            [correction.id]: e.target.value,
                          }))
                        }
                        disabled={isPending}
                      />
                      <button
                        type="button"
                        className="button button-primary button-small"
                        disabled={isPending}
                        onClick={() => void handleResolve(correction.id)}
                      >
                        Çöz
                      </button>
                    </div>
                  ) : null}
                </li>
              ))}
            </ul>
          )}
        </>
      )}
    </section>
  );
}
