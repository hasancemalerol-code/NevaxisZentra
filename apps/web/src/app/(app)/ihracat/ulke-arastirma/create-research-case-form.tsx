"use client";

import { useRouter } from "next/navigation";
import { useCallback, useState, type FormEvent } from "react";

import { createResearchCaseAction } from "@/features/export/server/export-case-writer";

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
        Ülke kodu
        <input
          value={countryCode}
          onChange={(e) => setCountryCode(e.target.value.toUpperCase())}
          maxLength={2}
          required
        />
      </label>
      <label>
        Ürün açıklaması
        <input
          value={productDescription}
          onChange={(e) => setProductDescription(e.target.value)}
        />
      </label>
      <label>
        Amaç notları
        <textarea
          value={purposeNotes}
          onChange={(e) => setPurposeNotes(e.target.value)}
          rows={2}
        />
      </label>
      {error ? (
        <p className="export-country-research-workspace__feedback export-country-research-workspace__feedback--error" role="alert">
          {error}
        </p>
      ) : null}
      <button type="submit" disabled={pending}>
        Yeni Ülke Araştır
      </button>
    </form>
  );
}
