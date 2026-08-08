"use client";

import { useCallback, useMemo, useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import { AlertTriangle, CheckCircle2, Scale, ShieldCheck } from "lucide-react";

import { Badge } from "@/components/ui/badge";

import type {
  ComplianceEvaluationResult,
  ExportComplianceRequirement,
  ExportComplianceSummary,
} from "../domain/export-3l";
import {
  getComplianceRequirementStatusLabel,
  getComplianceStatusLabel,
} from "../domain/export-3l";
import type {
  evaluateComplianceAction,
  revalidateComplianceAction,
  satisfyComplianceRequirementAction,
} from "../server/export-case-writer";

type BadgeTone =
  | "neutral"
  | "information"
  | "success"
  | "warning"
  | "danger"
  | "ai";

function complianceTone(status: ComplianceEvaluationResult): BadgeTone {
  switch (status) {
    case "ready":
      return "success";
    case "attention":
      return "warning";
    case "blocked":
      return "danger";
    case "unable-to-evaluate":
    default:
      return "neutral";
  }
}

type PanelState =
  | { kind: "idle" }
  | { kind: "pending"; action: string }
  | { kind: "error"; message: string }
  | { kind: "success"; message: string };

type ExportCompliancePanelProps = Readonly<{
  exportCaseId: string;
  caseVersion: number;
  compliance: ExportComplianceSummary | null;
  onEvaluate: typeof evaluateComplianceAction;
  onRevalidate: typeof revalidateComplianceAction;
  onSatisfyRequirement: typeof satisfyComplianceRequirementAction;
}>;

export function ExportCompliancePanel({
  exportCaseId,
  caseVersion,
  compliance,
  onEvaluate,
  onRevalidate,
  onSatisfyRequirement,
}: ExportCompliancePanelProps) {
  const router = useRouter();
  const [state, setState] = useState<PanelState>({ kind: "idle" });
  const [selectedRequirementId, setSelectedRequirementId] = useState("");
  const [resolutionType, setResolutionType] = useState("manual");
  const [resolutionNote, setResolutionNote] = useState("");

  const isPending = state.kind === "pending";
  const status = compliance?.complianceStatus ?? "unable-to-evaluate";
  const requirements = useMemo(
    () => compliance?.requirements ?? [],
    [compliance?.requirements],
  );

  const handleEvaluate = useCallback(async () => {
    setState({ kind: "pending", action: "evaluate" });
    const result = await onEvaluate(exportCaseId, { expectedCaseVersion: caseVersion });
    if (result.status === "failure") {
      setState({ kind: "error", message: result.message });
      return;
    }
    setState({ kind: "success", message: "Uygunluk değerlendirmesi tamamlandı." });
    router.refresh();
  }, [caseVersion, exportCaseId, onEvaluate, router]);

  const handleRevalidate = useCallback(async () => {
    setState({ kind: "pending", action: "revalidate" });
    const result = await onRevalidate(exportCaseId, caseVersion);
    if (result.status === "failure") {
      setState({ kind: "error", message: result.message });
      return;
    }
    setState({ kind: "success", message: "Uygunluk yeniden doğrulandı." });
    router.refresh();
  }, [caseVersion, exportCaseId, onRevalidate, router]);

  const handleSatisfy = useCallback(
    async (event: FormEvent) => {
      event.preventDefault();
      if (!selectedRequirementId) return;

      const requirement = requirements.find((r) => r.id === selectedRequirementId);
      if (!requirement) return;

      setState({ kind: "pending", action: "satisfy" });
      const result = await onSatisfyRequirement(exportCaseId, selectedRequirementId, {
        expectedRequirementVersion: requirement.version,
        resolutionType,
        resolutionNote,
      });
      if (result.status === "failure") {
        setState({ kind: "error", message: result.message });
        return;
      }
      setState({ kind: "success", message: "Gereksinim karşılandı olarak işaretlendi." });
      setResolutionNote("");
      router.refresh();
    },
    [
      exportCaseId,
      onSatisfyRequirement,
      requirements,
      resolutionNote,
      resolutionType,
      router,
      selectedRequirementId,
    ],
  );

  return (
    <section className="export-compliance-panel">
      <header className="export-compliance-panel__header">
        <div>
          <p>ÜLKE UYGUNLUK</p>
          <h2>Uygunluk değerlendirmesi</h2>
          <span>
            Hedef ülke kurallarına göre belge ve gereksinim uygunluğu
          </span>
        </div>
        <Badge tone={complianceTone(status)}>
          {getComplianceStatusLabel(status)}
        </Badge>
      </header>

      {compliance?.revalidationRequired ? (
        <div className="export-compliance-panel__alert" role="status">
          <AlertTriangle aria-hidden="true" />
          <span>Yeniden değerlendirme gerekli</span>
        </div>
      ) : null}

      {state.kind === "error" ? (
        <div className="export-compliance-panel__feedback export-compliance-panel__feedback--error" role="alert">
          {state.message}
        </div>
      ) : null}
      {state.kind === "success" ? (
        <div className="export-compliance-panel__feedback export-compliance-panel__feedback--success" role="status">
          <CheckCircle2 aria-hidden="true" />
          {state.message}
        </div>
      ) : null}

      <div className="export-compliance-panel__summary">
        <div>
          <Scale aria-hidden="true" />
          <div>
            <span>Hazırlık</span>
            <strong>%{compliance?.complianceReadinessPercent ?? 0}</strong>
          </div>
        </div>
        <div>
          <ShieldCheck aria-hidden="true" />
          <div>
            <span>Blokaj / Eksik</span>
            <strong>
              {compliance?.blockingCount ?? 0} / {compliance?.missingCount ?? 0}
            </strong>
          </div>
        </div>
      </div>

      <div className="export-compliance-panel__actions">
        <button
          type="button"
          disabled={isPending}
          onClick={() => void handleEvaluate()}
        >
          Değerlendir
        </button>
        <button
          type="button"
          className="export-compliance-panel__secondary"
          disabled={isPending || !compliance?.currentEvaluationId}
          onClick={() => void handleRevalidate()}
        >
          Yeniden doğrula
        </button>
      </div>

      <div className="export-compliance-panel__requirements">
        <h3>Gereksinimler</h3>
        {requirements.length === 0 ? (
          <p className="export-compliance-panel__empty">
            Henüz değerlendirilmiş bir gereksinim yok. Değerlendirme başlatın.
          </p>
        ) : (
          <ul>
            {requirements.map((requirement: ExportComplianceRequirement) => (
              <li key={requirement.id}>
                <div>
                  <strong>{requirement.title}</strong>
                  <small>{requirement.requirementCode}</small>
                </div>
                <Badge tone={requirement.blocking ? "danger" : "neutral"}>
                  {getComplianceRequirementStatusLabel(requirement.status)}
                </Badge>
              </li>
            ))}
          </ul>
        )}
      </div>

      {requirements.some((r) => r.status === "missing" || r.status === "pending") ? (
        <form className="export-compliance-panel__satisfy" onSubmit={handleSatisfy}>
          <h3>Gereksinimi karşıla</h3>
          <label>
            Gereksinim
            <select
              value={selectedRequirementId}
              onChange={(e) => setSelectedRequirementId(e.target.value)}
              required
            >
              <option value="">Seçin</option>
              {requirements
                .filter((r) => r.status === "missing" || r.status === "pending")
                .map((r) => (
                  <option key={r.id} value={r.id}>
                    {r.title}
                  </option>
                ))}
            </select>
          </label>
          <label>
            Çözüm tipi
            <input
              value={resolutionType}
              onChange={(e) => setResolutionType(e.target.value)}
              required
            />
          </label>
          <label>
            Not
            <textarea
              value={resolutionNote}
              onChange={(e) => setResolutionNote(e.target.value)}
              required
              rows={2}
            />
          </label>
          <button type="submit" disabled={isPending}>
            Karşılandı işaretle
          </button>
        </form>
      ) : null}
    </section>
  );
}
