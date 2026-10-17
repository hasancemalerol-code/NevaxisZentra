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
import { customerMessage } from "@/lib/presentation/customer-message";

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
    <section className={`export-country-knowledge-panel${entries.length === 0 ? " export-country-knowledge-panel--empty" : ""}`}>
      <header className="export-country-knowledge-panel__header">
        <div>
          <div className="export-workspace__title-row">
            <h2>Doğrulanmış kayıtlar</h2>
            <Badge tone="information">{entries.length} kayıt</Badge>
          </div>
          <span>
            Araştırma bulgularından ve kurallardan türetilen kalıcı bilgi kayıtları
          </span>
        </div>
      </header>

      {state.kind === "error" ? (
        <div className="export-country-knowledge-panel__feedback export-country-knowledge-panel__feedback--error" role="alert">
          {customerMessage(state.message)}
        </div>
      ) : null}
      {state.kind === "success" ? (
        <div className="export-country-knowledge-panel__feedback export-country-knowledge-panel__feedback--success" role="status">
          <CheckCircle2 aria-hidden="true" />
          {customerMessage(state.message)}
        </div>
      ) : null}

      <div className="export-country-knowledge-panel__list">
        {entries.length === 0 ? (
          <div className="export-empty export-empty--compact" role="status">
            <div className="export-empty__icon">
              <BookOpen aria-hidden="true" />
            </div>
            <div>
              <p>BİLGİ KAYDI YOK</p>
              <h2>Henüz bilgi kaydı bulunmuyor</h2>
            </div>
            <p className="export-empty__desc">Aşağıdan yeni bir kayıt oluşturduğunuzda burada listelenir ve doğrulanabilir.</p>
          </div>
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
        <h3 className="export-country-knowledge-panel__form-title">Yeni bilgi kaydı</h3>
        <label>
          Ülke kodu <span className="zx-required" aria-hidden="true">*</span>
          <input
            value={countryCode}
            onChange={(e) => setCountryCode(e.target.value.toUpperCase())}
            maxLength={2}
            placeholder="Örn. DE"
            autoComplete="off"
            required
          />
          <small className="export-field-hint">İki harfli ülke kodu (TR, DE, FR gibi).</small>
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
          <small className="export-field-hint">Bilginin ait olduğu konu başlığı.</small>
        </label>
        <label>
          Kaynak otorite
          <select value={authority} onChange={(e) => setAuthority(e.target.value)}>
            {KNOWLEDGE_AUTHORITY_OPTIONS.map((option) => (
              <option key={option} value={option}>
                {getKnowledgeAuthorityLabel(option)}
              </option>
            ))}
          </select>
          <small className="export-field-hint">Bilginin dayandığı kaynağın türü.</small>
        </label>
        <label>
          Başlık <span className="zx-required" aria-hidden="true">*</span>
          <input
            value={title}
            onChange={(e) => setTitle(e.target.value)}
            placeholder="Örn. Gümrük beyanı gereksinimleri"
            required
          />
          <small className="export-field-hint">Kaydın kısa ve açık adı.</small>
        </label>
        <label className="export-country-knowledge-panel__wide">
          Özet <span className="zx-required" aria-hidden="true">*</span>
          <textarea
            value={summary}
            onChange={(e) => setSummary(e.target.value)}
            required
            rows={3}
            placeholder="Örn. Gümrük beyannamesi için gerekli belgelerin kısa özeti"
          />
        </label>
        <label>
          Yayıncı kurum <span className="zx-required" aria-hidden="true">*</span>
          <input
            value={sourcePublisher}
            onChange={(e) => setSourcePublisher(e.target.value)}
            placeholder="Örn. T.C. Ticaret Bakanlığı"
            required
          />
          <small className="export-field-hint">Bilgiyi yayımlayan kurum.</small>
        </label>
        <label>
          Kaynak başlığı <span className="zx-required" aria-hidden="true">*</span>
          <input
            value={sourceTitle}
            onChange={(e) => setSourceTitle(e.target.value)}
            placeholder="Örn. İhracat rejimi tebliği"
            required
          />
          <small className="export-field-hint">Bilginin alındığı belge ya da tebliğ.</small>
        </label>
        <button type="submit" disabled={isPending} className="export-country-knowledge-panel__submit">
          Kayıt oluştur
        </button>
      </form>
    </section>
  );
}
