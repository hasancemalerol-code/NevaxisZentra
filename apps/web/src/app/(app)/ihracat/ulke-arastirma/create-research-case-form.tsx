"use client";

import { useRouter } from "next/navigation";
import { useCallback, useState, type FormEvent } from "react";

import { createResearchCaseAction } from "@/features/export/server/export-case-writer";
import { customerMessage } from "@/lib/presentation/customer-message";

export function CreateResearchCaseForm() {
  const router = useRouter();
  const [countryCode, setCountryCode] = useState("");
  const [productDescription, setProductDescription] = useState("");
  const [purposeNotes, setPurposeNotes] = useState("");
  const [error, setError] = useState<string | null>(null);
  const [pending, setPending] = useState(false);

  const handleSubmit = useCallback(
    async (event: FormEvent) => {
      event.preventDefault();
      setPending(true);
      setError(null);

      const result = await createResearchCaseAction({
        countryCode,
        productDescription: productDescription || undefined,
        purposeNotes: purposeNotes || undefined,
      });

      if (result.status === "failure") {
        setError(result.message);
        setPending(false);
        return;
      }

      router.push(`/ihracat/ulke-arastirma/${result.value.researchCaseId}`);
      router.refresh();
    },
    [countryCode, productDescription, purposeNotes, router],
  );

  return (
    <form className="export-country-research-workspace__form" onSubmit={handleSubmit}>
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
        Ürün açıklaması <span className="export-field-optional">(isteğe bağlı)</span>
        <input
          placeholder="Örn. Plastik enjeksiyon parça"
          value={productDescription}
          onChange={(e) => setProductDescription(e.target.value)}
        />
        <small className="export-field-hint">Ürünü kısaca tanımlayın.</small>
      </label>
      <label>
        Amaç notları <span className="export-field-optional">(isteğe bağlı)</span>
        <textarea
          value={purposeNotes}
          onChange={(e) => setPurposeNotes(e.target.value)}
          rows={3}
          placeholder="Örn. Pazar girişi için gümrük ve etiket gereksinimleri"
        />
        <small className="export-field-hint">Araştırmanın amacını yazın.</small>
      </label>
      {error ? (
        <p className="export-country-research-workspace__feedback export-country-research-workspace__feedback--error" role="alert">
          {customerMessage(error)}
        </p>
      ) : null}
      <button type="submit" disabled={pending}>
        Araştırmayı başlat
      </button>
    </form>
  );
}
