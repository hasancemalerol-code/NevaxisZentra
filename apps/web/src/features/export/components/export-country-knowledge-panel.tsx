"use client";

import { useCallback, useState, type FormEvent } from "react";
import { useRouter } from "next/navigation";
import { BookOpen, CheckCircle2, ShieldCheck } from "lucide-react";

import { Badge } from "@/components/ui/badge";

import type { CountryKnowledgeEntry } from "../domain/export-3l";
import {
  getKnowledgeAuthorityLabel,
  getResearchFindingCategoryLabel,
  getSourceVerificationStatusLabel,
  KNOWLEDGE_AUTHORITY_OPTIONS,
  RESEARCH_FINDING_CATEGORY_OPTIONS,
} from "../domain/export-3l";
import type {
  createKnowledgeEntryAction,
  verifyKnowledgeEntryAction,
} from "../server/export-case-writer";

type PanelState =
  | { kind: "idle" }
  | { kind: "pending"; action: string }
  | { kind: "error"; message: string }
  | { kind: "success"; message: string };

type ExportCountryKnowledgePanelProps = Readonly<{
  entries: readonly CountryKnowledgeEntry[];
  defaultCountryCode?: string;
  onCreate: typeof createKnowledgeEntryAction;
  onVerify: typeof verifyKnowledgeEntryAction;
}>;

export function ExportCountryKnowledgePanel({
  entries,
  defaultCountryCode = "",
  onCreate,
  onVerify,
}: ExportCountryKnowledgePanelProps) {
  const router = useRouter();
  const [state, setState] = useState<PanelState>({ kind: "idle" });
  const [countryCode, setCountryCode] = useState(defaultCountryCode);
  const [category, setCategory] = useState("customs");
  const [title, setTitle] = useState("");
  const [summary, setSummary] = useState("");
  const [sourcePublisher, setSourcePublisher] = useState("");
  const [sourceTitle, setSourceTitle] = useState("");
  const [authority, setAuthority] = useState("manual-entry");

  const isPending = state.kind === "pending";

  const handleCreate = useCallback(
    async (event: FormEvent) => {
      event.preventDefault();
      setState({ kind: "pending", action: "create" });

      const result = await onCreate({
        countryCode,
        category,
        title,
        summary,
        sourcePublisher,
        sourceTitle,
        authority,
      });

      if (result.status === "failure") {
        setState({ kind: "error", message: result.message });
        return;
      }

      setState({ kind: "success", message: "Bilgi kaydı oluşturuldu." });
      setTitle("");
      setSummary("");
      router.refresh();
    },
    [
      authority,
      category,
      countryCode,
      onCreate,
      router,
      sourcePublisher,
      sourceTitle,
      summary,
      title,
    ],
  );

  const handleVerify = useCallback(
    async (entryId: string, version: number) => {
      setState({ kind: "pending", action: "verify" });
      const result = await onVerify(entryId, version);
      if (result.status === "failure") {
        setState({ kind: "error", message: result.message });
        return;
      }
      setState({ kind: "success", message: "Kayıt doğrulandı." });
      router.refresh();
    },
    [onVerify, router],
  );

  return (
    <section className="export-country-knowledge-panel">
      <header className="export-country-knowledge-panel__header">
        <div>
          <p>ÜLKE BİLGİ BANKASI</p>
          <h2>Doğrulanmış ülke bilgisi</h2>
          <span>
            Araştırma bulgularından ve kurallardan türetilen kalıcı bilgi kayıtları
          </span>
        </div>
        <Badge tone="information">{entries.length} kayıt</Badge>
      </header>

      {state.kind === "error" ? (
        <div className="export-country-knowledge-panel__feedback export-country-knowledge-panel__feedback--error" role="alert">
          {state.message}
        </div>
      ) : null}
      {state.kind === "success" ? (
        <div className="export-country-knowledge-panel__feedback export-country-knowledge-panel__feedback--success" role="status">
          <CheckCircle2 aria-hidden="true" />
          {state.message}
        </div>
      ) : null}

      <div className="export-country-knowledge-panel__list">
        {entries.length === 0 ? (
          <p className="export-country-knowledge-panel__empty" role="status">
            <BookOpen aria-hidden="true" />
            Henüz bilgi kaydı bulunmuyor.
          </p>
        ) : (
          <ul>
            {entries.map((entry) => (
              <li key={entry.id}>
                <div>
                  <strong>{entry.title}</strong>
                  <small>
                    {entry.countryCode} ·{" "}
                    {getResearchFindingCategoryLabel(entry.category)} ·{" "}
                    {getKnowledgeAuthorityLabel(entry.authority)}
                  </small>
                  <p>{entry.summary}</p>
                </div>
                <div className="export-country-knowledge-panel__entry-actions">
                  <Badge
                    tone={
                      entry.verificationStatus === "verified"
                        ? "success"
                        : "warning"
                    }
                  >
                    {getSourceVerificationStatusLabel(entry.verificationStatus)}
                  </Badge>
                  {entry.verificationStatus !== "verified" ? (
                    <button
                      type="button"
                      disabled={isPending}
                      onClick={() => void handleVerify(entry.id, entry.version)}
                    >
                      <ShieldCheck aria-hidden="true" />
                      Doğrula
                    </button>
                  ) : null}
                </div>
              </li>
            ))}
          </ul>
        )}
      </div>

      <form className="export-country-knowledge-panel__form" onSubmit={handleCreate}>
        <h3>Yeni bilgi kaydı</h3>
        <label>
          Ülke kodu
          <input
            value={countryCode}
            onChange={(e) => setCountryCode(e.target.value.toUpperCase())}
            maxLength={2}
            required
          />
        </label>
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
          Otorite
          <select value={authority} onChange={(e) => setAuthority(e.target.value)}>
            {KNOWLEDGE_AUTHORITY_OPTIONS.map((option) => (
              <option key={option} value={option}>
                {getKnowledgeAuthorityLabel(option)}
              </option>
            ))}
          </select>
        </label>
        <label>
          Başlık
          <input value={title} onChange={(e) => setTitle(e.target.value)} required />
        </label>
        <label>
          Özet
          <textarea
            value={summary}
            onChange={(e) => setSummary(e.target.value)}
            required
            rows={3}
          />
        </label>
        <label>
          Kaynak yayıncı
          <input
            value={sourcePublisher}
            onChange={(e) => setSourcePublisher(e.target.value)}
            required
          />
        </label>
        <label>
          Kaynak başlığı
          <input
            value={sourceTitle}
            onChange={(e) => setSourceTitle(e.target.value)}
            required
          />
        </label>
        <button type="submit" disabled={isPending}>
          Kayıt oluştur
        </button>
      </form>
    </section>
  );
}
