"use client";

import { useCallback, useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import Link from "next/link";
import { CheckCircle2, Globe2, Plus } from "lucide-react";

import { Badge } from "@/components/ui/badge";

import type { CountryResearchDetail } from "../domain/export-3l";
import {
  getResearchCaseStatusLabel,
  getResearchFindingCategoryLabel,
  RESEARCH_FINDING_CATEGORY_OPTIONS,
} from "../domain/export-3l";
import type {
  addResearchFindingAction,
  completeResearchCaseAction,
  reviewResearchCaseAction,
} from "../server/export-case-writer";
import type {
  findResearchMissingSourcesAction,
  summarizeResearchAction,
} from "../server/export-4l-writer";
import { customerMessage } from "@/lib/presentation/customer-message";

type BadgeTone =
  | "neutral"
  | "information"
  | "success"
  | "warning"
  | "danger"
  | "ai";

type PanelState =
  | { kind: "idle" }
  | { kind: "pending"; action: string }
  | { kind: "error"; message: string }
  | { kind: "success"; message: string };

type ExportCountryResearchWorkspaceProps = Readonly<{
  detail: CountryResearchDetail;
  onAddFinding: typeof addResearchFindingAction;
  onReview: typeof reviewResearchCaseAction;
  onComplete: typeof completeResearchCaseAction;
  researchAssistant?: Readonly<{
    onSummarize: typeof summarizeResearchAction;
    onFindMissingSources: typeof findResearchMissingSourcesAction;
  }>;
}>;

function researchTone(status: string): BadgeTone {
  switch (status) {
    case "verified":
    case "completed":
      return "success";
    case "review-required":
      return "warning";
    case "researching":
      return "information";
    default:
      return "neutral";
  }
}

export function ExportCountryResearchWorkspace({
  detail,
  onAddFinding,
  onReview,
  onComplete,
  researchAssistant,
}: ExportCountryResearchWorkspaceProps) {
  const router = useRouter();
  const { researchCase, findings } = detail;
  const [state, setState] = useState<PanelState>({ kind: "idle" });
  const [category, setCategory] = useState("customs");
  const [title, setTitle] = useState("");
  const [summary, setSummary] = useState("");
  const [sourcePublisher, setSourcePublisher] = useState("");
  const [sourceTitle, setSourceTitle] = useState("");
  const [confidence, setConfidence] = useState("70");

  const isPending = state.kind === "pending";

  const handleAddFinding = useCallback(
    async (event: FormEvent) => {
      event.preventDefault();
      setState({ kind: "pending", action: "add-finding" });

      const result = await onAddFinding(researchCase.id, {
        expectedResearchVersion: researchCase.version,
        category,
        title,
        summary,
        sourcePublisher,
        sourceTitle,
        confidence: Number(confidence),
        blockingPotential: false,
      });

      if (result.status === "failure") {
        setState({ kind: "error", message: result.message });
        return;
      }

      setState({ kind: "success", message: "Bulgu eklendi." });
      setTitle("");
      setSummary("");
      router.refresh();
    },
    [
      category,
      confidence,
      onAddFinding,
      researchCase.id,
      researchCase.version,
      router,
      sourcePublisher,
      sourceTitle,
      summary,
      title,
    ],
  );

  const handleReview = useCallback(async () => {
    setState({ kind: "pending", action: "review" });
    const result = await onReview(researchCase.id, researchCase.version);
    if (result.status === "failure") {
      setState({ kind: "error", message: result.message });
      return;
    }
    setState({ kind: "success", message: "İncelemeye alındı." });
    router.refresh();
  }, [onReview, researchCase.id, researchCase.version, router]);

  const handleComplete = useCallback(async () => {
    setState({ kind: "pending", action: "complete" });
    const result = await onComplete(researchCase.id, researchCase.version);
    if (result.status === "failure") {
      setState({ kind: "error", message: result.message });
      return;
    }
    setState({ kind: "success", message: "Araştırma tamamlandı." });
    router.refresh();
  }, [onComplete, researchCase.id, researchCase.version, router]);

  const handleSummarize = useCallback(async () => {
    if (!researchAssistant) return;
    setState({ kind: "pending", action: "summarize" });
    const result = await researchAssistant.onSummarize(researchCase.id);
    if (result.status === "failure") {
      setState({ kind: "error", message: result.message });
      return;
    }
    setState({ kind: "success", message: "Araştırma özeti oluşturuldu." });
    router.refresh();
  }, [researchAssistant, researchCase.id, router]);

  const handleFindMissingSources = useCallback(async () => {
    if (!researchAssistant) return;
    setState({ kind: "pending", action: "missing-sources" });
    const result = await researchAssistant.onFindMissingSources(researchCase.id);
    if (result.status === "failure") {
      setState({ kind: "error", message: result.message });
      return;
    }
    setState({ kind: "success", message: "Eksik kaynak analizi tamamlandı." });
    router.refresh();
  }, [researchAssistant, researchCase.id, router]);

  return (
    <section className="export-country-research-workspace">
      <header className="export-country-research-workspace__header">
        <div>
          <p>ÜLKE ARAŞTIRMASI</p>
          <h2>{researchCase.researchNumber}</h2>
          <span>
            {researchCase.countryCode}
            {researchCase.productDescription
              ? ` · ${researchCase.productDescription}`
              : ""}
          </span>
        </div>
        <Badge tone={researchTone(researchCase.status)}>
          {getResearchCaseStatusLabel(researchCase.status)}
        </Badge>
      </header>

      {researchCase.linkedExportCaseId ? (
        <p className="export-country-research-workspace__link">
          Bağlı ihracat dosyası:{" "}
          <Link href={`/ihracat/${researchCase.linkedExportCaseId}`}>
            Dosyayı aç
          </Link>
        </p>
      ) : null}

      {state.kind === "error" ? (
        <div className="export-country-research-workspace__feedback export-country-research-workspace__feedback--error" role="alert">
          {customerMessage(state.message)}
        </div>
      ) : null}
      {state.kind === "success" ? (
        <div className="export-country-research-workspace__feedback export-country-research-workspace__feedback--success" role="status">
          <CheckCircle2 aria-hidden="true" />
          {customerMessage(state.message)}
        </div>
      ) : null}

      <div className="export-country-research-workspace__meta">
        <div>
          <Globe2 aria-hidden="true" />
          <div>
            <span>Ülke</span>
            <strong>{researchCase.countryCode}</strong>
          </div>
        </div>
        {researchCase.hsCode ? (
          <div>
            <span>GTİP</span>
            <strong>{researchCase.hsCode}</strong>
          </div>
        ) : null}
      </div>

      <div className="export-country-research-workspace__actions">
        <button type="button" disabled={isPending} onClick={() => void handleReview()}>
          İncelemeye al
        </button>
        <button
          type="button"
          className="export-country-research-workspace__secondary"
          disabled={isPending}
          onClick={() => void handleComplete()}
        >
          Tamamla
        </button>
      </div>

      {researchAssistant ? (
        <aside className="export-country-research-workspace__assistant">
          <h3>Araştırma asistanı</h3>
          <p>Kaynak tabanlı özet ve eksik kaynak analizi</p>
          <div className="export-country-research-workspace__actions">
            <button
              type="button"
              disabled={isPending}
              onClick={() => void handleSummarize()}
            >
              Özetle
            </button>
            <button
              type="button"
              className="export-country-research-workspace__secondary"
              disabled={isPending}
              onClick={() => void handleFindMissingSources()}
            >
              Eksik kaynakları bul
            </button>
          </div>
        </aside>
      ) : null}

      <div className="export-country-research-workspace__findings">
        <h3>Bulgular ({findings.length})</h3>
        {findings.length === 0 ? (
          <p className="export-country-research-workspace__empty">
            Henüz bulgu eklenmedi.
          </p>
        ) : (
          <ul>
            {findings.map((finding) => (
              <li key={finding.id}>
                <div>
                  <strong>{finding.title}</strong>
                  <small>
                    {getResearchFindingCategoryLabel(finding.category)} · %
                    {finding.confidence}
                  </small>
                  <p>{finding.summary}</p>
                </div>
              </li>
            ))}
          </ul>
        )}
      </div>

      <form
        className="export-country-research-workspace__form"
        onSubmit={handleAddFinding}
      >
        <h3>
          <Plus aria-hidden="true" />
          Yeni bulgu
        </h3>
        <label>
          Kategori
          <select value={category} onChange={(e) => setCategory(e.target.value)}>
            {RESEARCH_FINDING_CATEGORY_OPTIONS.map((option) => (
              <option key={option} value={option}>
                {getResearchFindingCategoryLabel(option)}
              </option>
            ))}
          </select>
        </label>
        <label>
          Başlık <span className="zx-required" aria-hidden="true">*</span>
          <input value={title} onChange={(e) => setTitle(e.target.value)} required />
        </label>
        <label>
          Özet <span className="zx-required" aria-hidden="true">*</span>
          <textarea
            value={summary}
            onChange={(e) => setSummary(e.target.value)}
            required
            rows={3}
          />
        </label>
        <label>
          Yayıncı kurum <span className="zx-required" aria-hidden="true">*</span>
          <input
            value={sourcePublisher}
            onChange={(e) => setSourcePublisher(e.target.value)}
            required
          />
        </label>
        <label>
          Kaynak başlığı <span className="zx-required" aria-hidden="true">*</span>
          <input
            value={sourceTitle}
            onChange={(e) => setSourceTitle(e.target.value)}
            required
          />
        </label>
        <label>
          Güven (%) <span className="zx-required" aria-hidden="true">*</span>
          <input
            type="number"
            min={0}
            max={100}
            value={confidence}
            onChange={(e) => setConfidence(e.target.value)}
            required
          />
        </label>
        <button type="submit" disabled={isPending}>
          Bulgu ekle
        </button>
      </form>
    </section>
  );
}
